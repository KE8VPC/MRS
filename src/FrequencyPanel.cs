using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MRS;
public sealed partial class FrequencyPanel : UserControl
{
    private FrequencyLibrary? library;internal int SavedEntryCount=>library?.Items.Count??0;
    private static int openEditors;
    private string path = "";
    private readonly TreeView tree = new() { Background = new SolidColorBrush(Color.FromRgb(10, 17, 27)), Foreground = Brushes.White, BorderBrush = Brushes.SlateGray };
    private readonly TextBox search = new() { Padding = new Thickness(6), Margin = new Thickness(0, 6, 0, 8) };
    private readonly TextBlock details = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 10, 0, 0), FontSize = 12 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 12, Margin = new Thickness(0, 8, 0, 0) };
    private readonly HashSet<Guid> selection=[]; private Guid? anchor; private Guid? focusedId; private bool selecting;
    private Point dragStart;
    private int pressClicks;
    private FrequencyItem? dragItem;
    public event Action? Changed;
    internal event Action<FrequencyItem,FrequencyItem[]>? FolderScanRequested;
    internal readonly FolderScanPanel ScanControls=new();
    public event Action<FrequencyItem>? TuneRequested;
    public Func<(long Frequency, string Mode)>? CurrentSettings { get; set; }
    public Func<double>? CurrentBandwidth { get; set; }
    public IReadOnlyList<FrequencyItem> Markers => library?.VisibleEntries().ToList() ?? [];
    private FrequencyItem? Selected => library?.Items.FirstOrDefault(i=>i.Id==focusedId) ?? (tree.SelectedItem as TreeViewItem)?.Tag as FrequencyItem;
    public FrequencyPanel()
    {
        tree.Resources[SystemColors.HighlightBrushKey] = Brushes.Transparent;
        tree.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = Brushes.Transparent;
        tree.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = Brushes.White;
        var dock = new DockPanel { Margin = new Thickness(8) }; Content = dock;
        var toolbar = new WrapPanel(); DockPanel.SetDock(toolbar, Dock.Top); dock.Children.Add(toolbar);
        void Button(string title, Action action)
        {
            var button = new Button { Content = title, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 5, 5) };
            button.Click += (_, _) => Attempt(action); toolbar.Children.Add(button);
        }
        Button("+ Folder", () => Edit(new FrequencyItem { IsFolder = true, Name = "New folder", ParentId = Destination() }));
        Button("+ Signal", () => AddSignal(null));
        Button("Edit", EditSelected);
        Button("Use tuned frequency", () =>
        {
            if(selection.Count>1)throw new InvalidOperationException("Select one signal to correct its frequency.");
            if(Selected is not {IsFolder:false} item)return;
            long frequency=CurrentSettings?.Invoke().Frequency??0;
            if(frequency<=0)throw new InvalidOperationException("Tune an SDR channel to the desired center first.");
            string name=item.Name==$"Signal {item.FrequencyHz/1e6:0.######} MHz"?$"Signal {frequency/1e6:0.######} MHz":item.Name;
            RequireLibrary().Upsert(item with{FrequencyHz=frequency,Name=name});Rebuild(item.Id);
            Notify($"Saved center corrected to {frequency/1e6:0.######} MHz. Notes and history preserved; Undo can restore it.");
        });
        Button("Tune", () => { if (Selected is { IsFolder: false } item) TuneRequested?.Invoke(item); });
        Button("Delete", DeleteSelection);
        Button("Undo", () => { RequireLibrary().Undo(); Rebuild(); Notify("Undo complete (available until Reload or closing). "); });
        Button("↑", () => Reorder(-1)); Button("↓", () => Reorder(1));
        Button("Show markers", () => SetSelectedVisibility(true));
        Button("Hide markers", () => SetSelectedVisibility(false));
        Button("Reload", () => Initialize(path));
        Button("Import / export",()=>CreateSharingWindow().ShowDialog());
        Button("Clean up…",()=>CreateCleanupWindow().ShowDialog());
        Button("Review discoveries",()=>CreatePendingDiscoveryWindow().ShowDialog());
        toolbar.Children.Add(discoveryPolicy);discoveryPolicy.SelectionChanged+=(_,_)=>{if(!syncingPolicy&&library!=null)Attempt(()=>{library.SetDiscoveryPolicy(discoveryPolicy.SelectedIndex==1);Notify("Auto-save policy saved");});};
        var filter = new StackPanel(); DockPanel.SetDock(filter, Dock.Top); dock.Children.Add(filter);
        filter.Children.Add(new TextBlock { Text = "Search frequencies, names, notes or protocols", Foreground = Brushes.LightSteelBlue, FontSize = 12, TextWrapping = TextWrapping.Wrap }); filter.Children.Add(search);
        search.TextChanged += (_, _) => {selection.Clear();anchor=null;Rebuild();};
        var lower = new StackPanel(); var lowerScroll=new ScrollViewer{Content=lower,MaxHeight=220,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}; DockPanel.SetDock(lowerScroll, Dock.Bottom); dock.Children.Add(lowerScroll);
        lower.Children.Add(new Expander{Header="Saved-folder scan controls",Content=ScanControls,IsExpanded=true,Foreground=Brushes.LightSteelBlue});
        lower.Children.Add(new ScrollViewer { Content = details, MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); lower.Children.Add(status);
        dock.Children.Add(tree);
        tree.SelectedItemChanged += (_, _) => { if(!selecting && (tree.SelectedItem as TreeViewItem)?.Tag is FrequencyItem item){focusedId=item.Id;SelectMany(item.Id,Keyboard.Modifiers);} ShowDetails(); };
        tree.PreviewKeyDown += SelectionKey;
        tree.PreviewMouseLeftButtonDown += (_,e) =>
        {
            pressClicks=e.ClickCount;dragStart=e.GetPosition(tree);var row=ItemAt(e.OriginalSource as DependencyObject);dragItem=row?.Tag as FrequencyItem;
            if(dragItem is {} item){SelectMany(item.Id,Keyboard.Modifiers);FocusRow(row!);if(Keyboard.Modifiers!=ModifierKeys.None){dragItem=null;e.Handled=true;}}
        };
        tree.PreviewMouseRightButtonDown += (_,e) =>
        {
            var row=ItemAt(e.OriginalSource as DependencyObject);if(row?.Tag is not FrequencyItem item)return;
            if(!selection.Contains(item.Id))SelectMany(item.Id,ModifierKeys.None);FocusRow(row);ShowDetails();
            var menu=new ContextMenu();
            void Action(string title,Action action,bool enabled){var option=new MenuItem{Header=title,IsEnabled=enabled};option.Click+=(_,_)=>Attempt(action);menu.Items.Add(option);}
            Action("Show markers",()=>SetSelectedVisibility(true),selection.Count>0);Action("Hide markers",()=>SetSelectedVisibility(false),selection.Count>0);
            Action("Edit",EditSelected,selection.Count==1);Action("Delete",DeleteSelection,selection.Count>0);
            Action("Tune",()=>TuneRequested?.Invoke(item),selection.Count==1&&!item.IsFolder);
            Action("Research LCN map",()=>ResearchFolder(item.Id),selection.Count==1&&item.IsFolder);
            Action("Scan folder",()=>FolderScanRequested?.Invoke(item,FolderScanPlan.Create(RequireLibrary().Items,item.Id,ScanControls.Recursive.IsChecked==true,ScanControls.Hidden.IsChecked==true)),selection.Count==1&&item.IsFolder);
            menu.PlacementTarget=row;menu.IsOpen=true;e.Handled=true;
        };
        tree.PreviewMouseLeftButtonUp += (_, e) =>
        {
            var pressed=dragItem;dragItem=null;
            var released=ItemAt(e.OriginalSource as DependencyObject)?.Tag as FrequencyItem;
            Point p=e.GetPosition(tree);
            if(pressClicks==2 && Math.Abs(p.X-dragStart.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y-dragStart.Y)<SystemParameters.MinimumVerticalDragDistance)
                ActivateClick(pressed,released,pressClicks);
        };
        tree.MouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || dragItem == null) return;
            Point p = e.GetPosition(tree);
            if (Math.Abs(p.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            var source = dragItem; dragItem = null;
            DragDrop.DoDragDrop(tree, new DataObject("MRS.FrequencyItem", source.Id.ToString()), DragDropEffects.Move);
        };
        tree.AllowDrop = true;
        tree.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent("MRS.FrequencyItem") ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
        tree.Drop += (_, e) => Attempt(() =>
        {
            if (!Guid.TryParse(e.Data.GetData("MRS.FrequencyItem") as string, out var id)) return;
            var target = ItemAt(e.OriginalSource as DependencyObject)?.Tag as FrequencyItem;
            if (target?.Id == id) return;
            RequireLibrary().Move(id, target is { IsFolder: true } ? target.Id : target?.ParentId);
            search.Clear(); Rebuild(id); Notify("Moved. Drop onto Frequency list to move to the root."); e.Handled = true;
        });
    }
    private static TreeViewItem? ItemAt(DependencyObject? value)
    {
        while (value != null && value is not TreeViewItem) value = value is Visual ? VisualTreeHelper.GetParent(value) : LogicalTreeHelper.GetParent(value);
        return value as TreeViewItem;
    }
    private IEnumerable<TreeViewItem> Rows(ItemsControl? parent=null,bool visibleOnly=true)
    {
        foreach(TreeViewItem row in (parent??tree).Items)
        {
            if(row.Tag is FrequencyItem)yield return row;
            if(!visibleOnly||row.IsExpanded)foreach(var child in Rows(row,visibleOnly))yield return child;
        }
    }
    private void PaintSelection()
    {
        foreach(var row in Rows(visibleOnly:false))
        {
            if(row.Header is not TextBlock label){label=new TextBlock();row.Header=label;}
            label.Text=((FrequencyItem)row.Tag)+(((FrequencyItem)row.Tag).Visible?"":" [hidden]");
            label.Background=selection.Contains(((FrequencyItem)row.Tag).Id)?Brushes.DarkSlateBlue:Brushes.Transparent;
        }
        if(selection.Count>1)details.Text=$"{selection.Count} selected. Delete removes selected folders and their contents; Undo restores the batch.";
    }
    private void SelectMany(Guid id,ModifierKeys modifiers)
    {
        bool control=modifiers.HasFlag(ModifierKeys.Control),shift=modifiers.HasFlag(ModifierKeys.Shift);
        if(shift&&anchor is {} start)
        {
            var ids=Rows().Select(r=>((FrequencyItem)r.Tag).Id).ToList();int a=ids.IndexOf(start),b=ids.IndexOf(id);
            if(!control)selection.Clear();
            if(a>=0&&b>=0)foreach(var next in ids.Skip(Math.Min(a,b)).Take(Math.Abs(a-b)+1))selection.Add(next);
            else selection.Add(id);
        }
        else {if(!control)selection.Clear();if(!control||!selection.Remove(id))selection.Add(id);anchor=id;}
        PaintSelection();
    }
    private void DeleteSelection()
    {
        if(selection.Count==0)return;
        int count=selection.Count;RequireLibrary().DeleteMany(selection.ToArray());selection.Clear();anchor=null;
        Rebuild();Notify($"Deleted {count} selected entries (including any folder contents). Undo restores the batch.");
    }
    private void FocusRow(TreeViewItem row)
    {
        focusedId=(row.Tag as FrequencyItem)?.Id;selecting=true;
        try{row.IsSelected=true;row.Focus();row.BringIntoView();}finally{selecting=false;}
    }
    private void SelectionKey(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.F2){Attempt(EditSelected);e.Handled=true;return;}
        if(e.Key==Key.Delete){Attempt(DeleteSelection);e.Handled=true;return;}
        var modifiers=Keyboard.Modifiers;
        if(e.Key==Key.Space&&modifiers.HasFlag(ModifierKeys.Control)&&Selected is {} toggle){SelectMany(toggle.Id,modifiers);e.Handled=true;return;}
        if(e.Key is not (Key.Up or Key.Down))return;
        MoveSelection(e.Key==Key.Down?1:-1,modifiers);e.Handled=true;
    }
    internal void MoveSelection(int delta,ModifierKeys modifiers)
    {
        var rows=Rows().ToList();if(rows.Count==0)return;int current=rows.FindIndex(r=>(r.Tag as FrequencyItem)?.Id==focusedId);
        var next=rows[Math.Clamp(current+delta,0,rows.Count-1)];var id=((FrequencyItem)next.Tag).Id;
        if(!modifiers.HasFlag(ModifierKeys.Control)||modifiers.HasFlag(ModifierKeys.Shift))SelectMany(id,modifiers);
        FocusRow(next);ShowDetails();
    }
    internal int SelectedCount=>selection.Count;
    internal int HeldDiscoveryCount=>library?.PendingDiscoveries.Count??0;
    internal void ActivateClick(FrequencyItem? pressed,FrequencyItem? released,int clickCount=1)
    {
        if(clickCount==2 && pressed is {IsFolder:false} && released?.Id==pressed.Id)Attempt(()=>TuneRequested?.Invoke(pressed));
    }
    internal void CorrectMarker(Guid id,long original,long frequency)=>Attempt(()=>
    {
        var item=RequireLibrary().Items.Single(i=>i.Id==id&&!i.IsFolder);
        if(item.FrequencyHz!=original)throw new InvalidOperationException("This marker changed during adjustment. Select it again.");
        string name=item.Name==$"Signal {item.FrequencyHz/1e6:0.######} MHz"?$"Signal {frequency/1e6:0.######} MHz":item.Name;
        RequireLibrary().Upsert(item with{FrequencyHz=frequency,Name=name});Rebuild(id);Notify("Marker frequency corrected. Undo restores the previous center.");
    });
    private FrequencyLibrary RequireLibrary() => library ?? throw new InvalidOperationException("Library is unavailable. Resolve the file error and Reload.");
    private string? legacyPath;
    internal int ImportReferences(string folder,IEnumerable<ListeningPreset> entries){int count=RequireLibrary().AddReferences(folder,entries);Rebuild();Notify($"Added {count} references; existing matches preserved.");return count;}
    public void Release() => FrequencyLibrary.Saved -= LibrarySaved;
    private void LibrarySaved(FrequencyLibrary sender, string file)
    {
        if (ReferenceEquals(sender, library) || !string.Equals(Path.GetFullPath(path), file, StringComparison.OrdinalIgnoreCase)) return;
        Initialize(path, legacyPath);
    }
    public void Initialize(string file, string? legacy = null)
    {
        Release(); path = file; legacyPath = legacy ?? legacyPath;
        try { library = new FrequencyLibrary(file); if (legacyPath != null) library.ImportReceiverB(legacyPath); FrequencyLibrary.Saved += LibrarySaved; syncingPolicy=true;discoveryPolicy.SelectedIndex=library.UpdateExistingOnly?1:0;syncingPolicy=false;Rebuild(); Notify("Single-click to select; double-click to tune. Edit or F2 opens the selected entry. Ctrl-click toggles; Shift-click/arrows select a range. Ctrl-arrows move focus; Ctrl-Space toggles. Delete removes selection; Undo restores. Drag single entries to organize."); }
        catch (Exception ex) { library = null; tree.Items.Clear(); status.Text = "Library not loaded: " + ex.Message + " Original file preserved."; Changed?.Invoke(); }
    }
    private Guid? Destination() => Selected is { } item ? item.IsFolder ? item.Id : item.ParentId : null;
    public void AddSignal(long? frequency)
    {
        Attempt(() =>
        {
            var settings = CurrentSettings?.Invoke() ?? (0L, "NFM");
            Edit(new FrequencyItem { ParentId = Destination(), FrequencyHz = frequency ?? settings.Item1, Mode = settings.Item2, BandwidthHz = CurrentBandwidth?.Invoke() ?? 0 });
        });
    }
    internal void ReviewDiscovery(FrequencyItem draft)=>Attempt(()=>Edit(draft with{ParentId=Destination()}));
    internal bool SaveDiscoveries(IEnumerable<ActivityDiscovery> observations,string receiver)
    {
        if(openEditors>0)return false;
        // Reload under the library's revision/atomic-write checks; never overwrite a stale user edit.
        var fresh=new FrequencyLibrary(path);fresh.SaveDiscoveries(observations,receiver);
        return true;
    }
    internal (int Added,int Updated)? SaveSiteChannels(IEnumerable<SiteChannelData> channels){if(openEditors>0||string.IsNullOrEmpty(path))return null;return new FrequencyLibrary(path).SaveSiteChannels(channels);}
    internal bool SaveNxdn(NxdnReception observation,string receiver){if(openEditors>0||observation.Context.Replay)return false;new FrequencyLibrary(path).SaveNxdn(observation,receiver);Rebuild();return true;}
    internal bool SaveDmr(DmrReception observation,string receiver)
    {
        if(openEditors>0||observation.Context.Replay)return false;
        new FrequencyLibrary(path).SaveDmr(observation,receiver);return true;
    }
    internal void SetSelectedVisibility(bool visible)
    {
        if(selection.Count==0){Notify("Select one or more entries first.");return;}
        RequireLibrary().SetVisibility(selection,visible);Rebuild();
        Notify(visible?"Markers enabled. A hidden parent folder still hides its children. Entries remain in the list.":"Selected markers hidden from spectrum/waterfall. Entries remain in the list; Show markers restores them. Undo restores this batch.");
    }
    internal void EditSelected()
    {
        if(selection.Count>1){Notify("Select one entry to edit.");return;}
        if(selection.Count==1 && RequireLibrary().Items.FirstOrDefault(i=>selection.Contains(i.Id)) is {} item)Edit(item);else Notify("Select an entry first, then choose Edit or press F2.");
    }
    private void Edit(FrequencyItem item)
    {
        var store = RequireLibrary();
        var dialog = new FrequencyEditor(item, store.Items) { Owner = Window.GetWindow(this) };
        bool? accepted;openEditors++;
        try{accepted=dialog.ShowDialog();}finally{openEditors--;}
        if (accepted != true || dialog.Result is not { } result) return;
        bool duplicate = !result.IsFolder && store.Items.Any(n => !n.IsFolder && n.Id != result.Id && n.FrequencyHz == result.FrequencyHz);
        store.Upsert(result); search.Clear(); Rebuild(result.Id);
        Notify(duplicate ? "Saved as a separate entry. Another entry has the same frequency." : "Saved.");
    }
    private void Reorder(int direction) { if (Selected is { } item) { RequireLibrary().Reorder(item.Id, direction); Rebuild(item.Id); Notify("Order saved."); } }
    private void Notify(string message) { status.Text = message; Changed?.Invoke(); }
    private void Attempt(Action action) { try { action(); } catch (Exception ex) { status.Text = "Could not apply change: " + ex.Message; } }
    public void SelectEntry(Guid id)
    {
        search.Clear(); Rebuild(id);
    }
    private void Rebuild(Guid? selected = null)
    {
        if(selected is {} explicitId){selection.Clear();selection.Add(explicitId);anchor=explicitId;}
        selected ??= Selected?.Id; focusedId=selected; selecting=true;
        var expanded = new Dictionary<Guid, bool>();
        void Remember(ItemsControl parent) { foreach (TreeViewItem child in parent.Items) { if (child.Tag is FrequencyItem item) expanded[item.Id] = child.IsExpanded; Remember(child); } }
        Remember(tree); tree.Items.Clear();
        if (library == null) {selecting=false;return;}
        string query = search.Text.Trim();
        var root = new TreeViewItem { Header = "Frequency list", IsExpanded = true, Foreground = Brushes.White };
        tree.Items.Add(root);
        var children = library.Items.GroupBy(n => n.ParentId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.ToList());
        var ancestors = new HashSet<Guid>();
        var selectedNode = library.Items.FirstOrDefault(n => n.Id == selected);
        while (selectedNode?.ParentId is { } parent) { ancestors.Add(parent); selectedNode = library.Items.First(n => n.Id == parent); }
        TreeViewItem? Build(FrequencyItem item, bool includeChildren)
        {
            string searchable = $"{item} {(item.FrequencyHz / 1000000m).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)} {item.FrequencyHz} {item.DmrIdentifiers} {item.Notes} {item.Tags} {item.Protocols} {item.Modulation} {item.Signaling}";
            bool match = query.Length == 0 || searchable.Contains(query, StringComparison.OrdinalIgnoreCase);
            var row = new TreeViewItem { Tag = item, Header = item + (item.Visible ? "" : " [hidden]"), Foreground = item.Visible ? Brushes.White : Brushes.Gray,
                IsExpanded = query.Length > 0 || ancestors.Contains(item.Id) || expanded.GetValueOrDefault(item.Id, false), ToolTip = item.IsFolder ? item.Name : $"{item}\n{item.Signaling}\n{item.Notes}" };
            if (children.TryGetValue(item.Id, out var nested)) foreach (var child in nested) { var built = Build(child, includeChildren || match && query.Length > 0); if (built != null) row.Items.Add(built); }
            if (!match && !includeChildren && row.Items.Count == 0) return null;
            if (item.Id == selected) { row.IsSelected = true; row.Loaded += (_, _) => row.BringIntoView(); }
            return row;
        }
        if (children.TryGetValue(Guid.Empty, out var top)) foreach (var item in top) { var row = Build(item, false); if (row != null) root.Items.Add(row); }
        selecting=false;selection.IntersectWith(library.Items.Select(i=>i.Id));PaintSelection();
        ShowDetails();
    }
    private void ShowDetails()
    {
        if(selection.Count>1){details.Text=$"{selection.Count} selected. Delete removes selected folders and contents. Undo restores the batch.";return;}
        if (Selected is not { } item) { details.Text = "Single-click to select. Edit or F2 opens the editor; double-click or Tune tunes the receiver."; return; }
        details.Text = item.IsFolder ? $"{item.Name}\nFolder visibility also controls child RF markers." : $"{item}\nModulation: {item.Modulation}\n{item.Signaling}\nTags: {item.Tags}\n{item.Notes}\nProtocol labels do not enable decoding.";
        if(item.DmrIdentifiers?.Length>0)details.Text+="\nDMR observations: "+item.DmrIdentifiers.Replace("\n","; ");
        if(item.Activity is {} activity)details.Text+=$"\nRF activity: {activity.Hits} bursts · {activity.AboveFloorDb:0.0} dB above estimated floor\nOccupied estimate: {activity.OccupiedHz/1000:0.##} kHz\nFirst UTC: {activity.FirstSeen:u}\nLast UTC: {activity.LastSeen:u} · Receiver {activity.Receiver}";
    }
}



