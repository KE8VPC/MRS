using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MRS;
public sealed class FrequencyPanel : UserControl
{
    private FrequencyLibrary? library;
    private static int openEditors;
    private string path = "";
    private readonly TreeView tree = new() { Background = new SolidColorBrush(Color.FromRgb(10, 17, 27)), Foreground = Brushes.White, BorderBrush = Brushes.SlateGray };
    private readonly TextBox search = new() { Padding = new Thickness(6), Margin = new Thickness(0, 6, 0, 8) };
    private readonly TextBlock details = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 10, 0, 0), FontSize = 12 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 12, Margin = new Thickness(0, 8, 0, 0) };
    private Point dragStart;
    private FrequencyItem? dragItem;
    public event Action? Changed;
    public event Action<FrequencyItem>? TuneRequested;
    public Func<(long Frequency, string Mode)>? CurrentSettings { get; set; }
    public Func<double>? CurrentBandwidth { get; set; }
    public IReadOnlyList<FrequencyItem> Markers => library?.VisibleEntries().ToList() ?? [];
    private FrequencyItem? Selected => (tree.SelectedItem as TreeViewItem)?.Tag as FrequencyItem;
    public FrequencyPanel()
    {
        tree.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(43, 75, 103));
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
        Button("Edit", () => { if (Selected is { } item) Edit(item); });
        Button("Tune", () => { if (Selected is { IsFolder: false } item) TuneRequested?.Invoke(item); });
        Button("Delete", () => { if (Selected is { } item) { RequireLibrary().Delete(item.Id); Rebuild(); Notify("Deleted. Undo can restore the entry and its children."); } });
        Button("Undo", () => { RequireLibrary().Undo(); Rebuild(); Notify("Undo complete (available until Reload or closing). "); });
        Button("↑", () => Reorder(-1)); Button("↓", () => Reorder(1));
        Button("Show/hide", () => { if (Selected is { } item) { RequireLibrary().Upsert(item with { Visible = !item.Visible }); Rebuild(item.Id); Notify("Marker visibility saved."); } });
        Button("Reload", () => Initialize(path));
        var filter = new StackPanel(); DockPanel.SetDock(filter, Dock.Top); dock.Children.Add(filter);
        filter.Children.Add(new TextBlock { Text = "Search frequencies, names, notes or protocols", Foreground = Brushes.LightSteelBlue, FontSize = 12, TextWrapping = TextWrapping.Wrap }); filter.Children.Add(search);
        search.TextChanged += (_, _) => Rebuild();
        var lower = new StackPanel(); DockPanel.SetDock(lower, Dock.Bottom); dock.Children.Add(lower);
        lower.Children.Add(new ScrollViewer { Content = details, MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); lower.Children.Add(status);
        dock.Children.Add(tree);
        tree.SelectedItemChanged += (_, _) => ShowDetails();
        tree.PreviewMouseLeftButtonDown += (_, e) => { dragStart = e.GetPosition(tree); dragItem = ItemAt(e.OriginalSource as DependencyObject)?.Tag as FrequencyItem; };
        tree.PreviewMouseLeftButtonUp += (_, e) =>
        {
            var pressed=dragItem;dragItem=null;
            var released=ItemAt(e.OriginalSource as DependencyObject)?.Tag as FrequencyItem;
            Point p=e.GetPosition(tree);
            if(e.ClickCount==1 && Math.Abs(p.X-dragStart.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y-dragStart.Y)<SystemParameters.MinimumVerticalDragDistance)
                ActivateClick(pressed,released);
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
    internal void ActivateClick(FrequencyItem? pressed,FrequencyItem? released)
    {
        if(pressed is {IsFolder:false} && released?.Id==pressed.Id)Attempt(()=>TuneRequested?.Invoke(pressed));
    }
    private FrequencyLibrary RequireLibrary() => library ?? throw new InvalidOperationException("Library is unavailable. Resolve the file error and Reload.");
    private string? legacyPath;
    public void Release() => FrequencyLibrary.Saved -= LibrarySaved;
    private void LibrarySaved(FrequencyLibrary sender, string file)
    {
        if (ReferenceEquals(sender, library) || !string.Equals(Path.GetFullPath(path), file, StringComparison.OrdinalIgnoreCase)) return;
        Initialize(path, legacyPath);
    }
    public void Initialize(string file, string? legacy = null)
    {
        Release(); path = file; legacyPath = legacy ?? legacyPath;
        try { library = new FrequencyLibrary(file); if (legacyPath != null) library.ImportReceiverB(legacyPath); FrequencyLibrary.Saved += LibrarySaved; Rebuild(); Notify("Click a saved signal to tune this receiver. Drag to organize; folders do not tune."); }
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
        selected ??= Selected?.Id;
        var expanded = new Dictionary<Guid, bool>();
        void Remember(ItemsControl parent) { foreach (TreeViewItem child in parent.Items) { if (child.Tag is FrequencyItem item) expanded[item.Id] = child.IsExpanded; Remember(child); } }
        Remember(tree); tree.Items.Clear();
        if (library == null) return;
        string query = search.Text.Trim();
        var root = new TreeViewItem { Header = "Frequency list", IsExpanded = true, Foreground = Brushes.White };
        tree.Items.Add(root);
        var children = library.Items.GroupBy(n => n.ParentId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.ToList());
        var ancestors = new HashSet<Guid>();
        var selectedNode = library.Items.FirstOrDefault(n => n.Id == selected);
        while (selectedNode?.ParentId is { } parent) { ancestors.Add(parent); selectedNode = library.Items.First(n => n.Id == parent); }
        TreeViewItem? Build(FrequencyItem item, bool includeChildren)
        {
            string searchable = $"{item} {(item.FrequencyHz / 1000000m).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)} {item.FrequencyHz} {item.Notes} {item.Tags} {item.Protocols} {item.Modulation} {item.Signaling}";
            bool match = query.Length == 0 || searchable.Contains(query, StringComparison.OrdinalIgnoreCase);
            var row = new TreeViewItem { Tag = item, Header = item + (item.Visible ? "" : " [hidden]"), Foreground = item.Visible ? Brushes.White : Brushes.Gray,
                IsExpanded = query.Length > 0 || ancestors.Contains(item.Id) || expanded.GetValueOrDefault(item.Id, false), ToolTip = item.IsFolder ? item.Name : $"{item}\n{item.Signaling}\n{item.Notes}" };
            if (children.TryGetValue(item.Id, out var nested)) foreach (var child in nested) { var built = Build(child, includeChildren || match && query.Length > 0); if (built != null) row.Items.Add(built); }
            if (!match && !includeChildren && row.Items.Count == 0) return null;
            if (item.Id == selected) { row.IsSelected = true; row.Loaded += (_, _) => row.BringIntoView(); }
            return row;
        }
        if (children.TryGetValue(Guid.Empty, out var top)) foreach (var item in top) { var row = Build(item, false); if (row != null) root.Items.Add(row); }
        ShowDetails();
    }
    private void ShowDetails()
    {
        if (Selected is not { } item) { details.Text = "Click a saved signal to tune. Keyboard selection uses the Tune button."; return; }
        details.Text = item.IsFolder ? $"{item.Name}\nFolder visibility also controls child RF markers." : $"{item}\nModulation: {item.Modulation}\n{item.Signaling}\nTags: {item.Tags}\n{item.Notes}\nProtocol labels do not enable decoding.";
        if(item.Activity is {} activity)details.Text+=$"\nRF activity: {activity.Hits} bursts · {activity.AboveFloorDb:0.0} dB above estimated floor\nOccupied estimate: {activity.OccupiedHz/1000:0.##} kHz\nFirst UTC: {activity.FirstSeen:u}\nLast UTC: {activity.LastSeen:u} · Receiver {activity.Receiver}";
    }
}


