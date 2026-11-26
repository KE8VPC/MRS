// =====================================================================================
//  M.R.S. spectrum-layout preview  —  Claude, 2026-11-16.  Revision "layout2" after the user's station review
//  (outputs/MRS/CLAUDE-LAYOUT1-REVIEW-NOTES.md). See CLAUDE-SPECTRUM-NOTES.md for the full change list.
//
//  User decisions implemented here:
//    • The main navigation tab row is anchored: one fixed row, full width, never wraps, never reorders, never moves.
//      (WorkspaceTabs gets a template whose header is a single horizontal row inside a non-wrapping strip.)
//    • The docked spectrum sits BELOW that tab row, between the tabs and the page (part of the same template).
//    • No right-side docking (monitoring needs spectrum width, not height).
//    • No preset per-workspace overrides. Choosing a main layout applies to every workspace and clears exceptions,
//      so the visible result always matches the last choice. An exception can still be set for the open workspace.
//
//  The single RF spectrum (SharedSpectrum / Spectrum) is moved, never copied:
//    • Spectrum tab only – original behaviour (Spectrum tab; Broadcast AM/FM hosts)
//    • Docked top        – below the main tab row on every other workspace; splitter, compact option, height cap
//    • Separate window   – own window; closing it returns the spectrum to the Spectrum tab
//  Settings: <receiver data folder>\spectrum-layout.json (separate from settings.json on purpose).
// =====================================================================================
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MRS;

internal static class SpectrumPlacements
{
    internal const string Tab = "Tab", DockTop = "DockTop", Window = "Window", Default = "Default";
    internal static readonly (string Key, string Label)[] Choices =
        [(Tab, "Spectrum tab only"), (DockTop, "Docked under tabs"), (Window, "Separate window")];
    internal static readonly (string Key, string Label)[] OverrideChoices =
        [(Default, "Same as all"), (DockTop, "Docked under tabs"), (Tab, "Spectrum tab only")];
    internal static bool Valid(string? value) => value is Tab or DockTop or Window;
}

/// <summary>Saved spectrum layout for one receiver workspace.</summary>
internal sealed record SpectrumLayout
{
    public string Placement { get; init; } = SpectrumPlacements.DockTop;
    public bool Compact { get; init; } = true;
    public double CompactHeight { get; init; } = 180;
    public double FullHeight { get; init; } = 300;
    /// <summary>Largest share of the workspace area the docked spectrum may take (percent).</summary>
    public double HeightCapPercent { get; init; } = 35;
    /// <summary>Exceptions for individual top-level workspace tabs. Empty by default (no preset exceptions).</summary>
    public Dictionary<string, string> Overrides { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public double[]? WindowBounds { get; init; }

    // SpectrumView draws nothing (and ignores click-to-tune) below 140 px tall (SpectrumView.cs OnRender / line ~119).
    // The layout row and border take about 35 px, so a docked area under ~176 px would show a blank box.
    internal const double CompactMinimum = 176, FullMinimum = 240, WorkspaceMinimum = 200;

    internal static SpectrumLayout Defaults() => new();

    internal SpectrumLayout Normalize()
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Overrides ?? [])
            if (!string.IsNullOrWhiteSpace(key) && value is SpectrumPlacements.Tab or SpectrumPlacements.DockTop) overrides[key] = value;
        static double Clamp(double v, double lo, double hi, double fallback) => double.IsFinite(v) ? Math.Clamp(v, lo, hi) : fallback;
        return this with
        {
            // layout1 files may say "DockRight"; right docking was removed, so those become docked under the tabs.
            Placement = SpectrumPlacements.Valid(Placement) ? Placement : SpectrumPlacements.DockTop,
            CompactHeight = Clamp(CompactHeight, CompactMinimum, 1200, 180),
            FullHeight = Clamp(FullHeight, FullMinimum, 1400, 300),
            HeightCapPercent = Clamp(HeightCapPercent, 20, 80, 35),
            Overrides = overrides,
            WindowBounds = WindowBounds is { Length: 4 } b && b.All(double.IsFinite) ? b : null
        };
    }
}

public partial class MainWindow
{
    private SpectrumLayout spectrumLayout = SpectrumLayout.Defaults();
    private string? spectrumLayoutPath;
    private Grid dockTopHost = null!;
    private RowDefinition dockTopRow = null!, dockTopSplitRow = null!;
    private GridSplitter dockTopSplitter = null!;
    private ScrollViewer mainTabScroll = null!;
    private Panel mainTabRow = null!;
    private RepeatButton mainTabLeft = null!, mainTabRight = null!;
    private Panel? spectrumToolbarStack;
    private Window? spectrumWindow;
    private Grid? spectrumWindowHost;
    private bool closingSpectrumWindow, refreshingLayoutBar, placingSpectrum;
    private ComboBox layoutPlacementBox = null!, layoutOverrideBox = null!;
    private CheckBox layoutCompactBox = null!;
    private TextBlock layoutOverrideLabel = null!;
    private Button layoutReturnButton = null!;
    private readonly ContentControl spectrumElsewhere = new();

    /// <summary>Where the spectrum currently is (for tests and the layout check).</summary>
    internal string CurrentSpectrumPlacement { get; private set; } = SpectrumPlacements.Tab;

    private void InitializeSpectrumPlacement()
    {
        ApplyWorkspaceTemplate();
        BuildLayoutBar();
        BuildLayoutMenu();
        spectrumElsewhere.Content = SpectrumElsewhereNotice();
        spectrumLayoutPath = Path.Combine(dataFolder, "spectrum-layout.json");
        try
        {
            if (File.Exists(spectrumLayoutPath))
                spectrumLayout = (JsonSerializer.Deserialize<SpectrumLayout>(File.ReadAllText(spectrumLayoutPath)) ?? SpectrumLayout.Defaults()).Normalize();
            else if (diagnosticWorkspace)
                spectrumLayout = SpectrumLayout.Defaults() with { Placement = SpectrumPlacements.Tab }; // existing smoke tests expect the original layout
        }
        catch (Exception ex) { Log("Spectrum layout not restored: " + ex.Message); spectrumLayout = SpectrumLayout.Defaults(); }
        Closing += (_, _) => { SaveSpectrumLayout(); if (spectrumWindow != null) { closingSpectrumWindow = true; spectrumWindow.Close(); } };
        PlaceSpectrum();
    }

    // ------------------------------------------------------------------ anchored tab row + dock area
    /// <summary>
    /// WorkspaceTabs template: [fixed single-row main tabs] [docked spectrum] [splitter] [selected page].
    /// The tab row is always the first row, so docking or undocking never moves it. Headers never wrap and never reorder.
    /// If the window is too narrow for every tab, small ‹ › buttons scroll the row instead of wrapping it.
    /// </summary>
    private void ApplyWorkspaceTemplate()
    {
        const string xaml = """
<ControlTemplate TargetType="TabControl" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Grid x:Name="PART_Root" KeyboardNavigation.TabNavigation="Local" SnapsToDevicePixels="True">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="0"/>
      <RowDefinition Height="0"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <DockPanel Grid.Row="0" Panel.ZIndex="1" LastChildFill="True">
      <RepeatButton x:Name="PART_TabRight" DockPanel.Dock="Right" Content="›" Width="20" Padding="0" Visibility="Collapsed" ToolTip="More tabs"/>
      <RepeatButton x:Name="PART_TabLeft" DockPanel.Dock="Right" Content="‹" Width="20" Padding="0" Visibility="Collapsed" ToolTip="More tabs"/>
      <ScrollViewer x:Name="PART_TabScroll" HorizontalScrollBarVisibility="Hidden" VerticalScrollBarVisibility="Disabled" CanContentScroll="False" Focusable="False">
        <StackPanel x:Name="PART_TabRow" Orientation="Horizontal" IsItemsHost="True" Margin="2,2,2,0" KeyboardNavigation.TabIndex="1"/>
      </ScrollViewer>
    </DockPanel>
    <Grid x:Name="PART_DockHost" Grid.Row="1" Margin="0,6,0,0"/>
    <GridSplitter x:Name="PART_DockSplitter" Grid.Row="2" Height="6" HorizontalAlignment="Stretch" VerticalAlignment="Center" ResizeDirection="Rows" ResizeBehavior="PreviousAndNext" Background="#34495F" ToolTip="Drag to resize the docked spectrum" Visibility="Collapsed"/>
    <Border Grid.Row="3" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" Padding="{TemplateBinding Padding}">
      <ContentPresenter x:Name="PART_SelectedContentHost" ContentSource="SelectedContent"/>
    </Border>
  </Grid>
</ControlTemplate>
""";
        var template = (ControlTemplate)XamlReader.Parse(xaml);
        WorkspaceTabs.Template = template;
        WorkspaceTabs.ApplyTemplate();
        T Part<T>(string name) where T : class => template.FindName(name, WorkspaceTabs) as T ?? throw new InvalidOperationException("Workspace template part missing: " + name);
        dockTopHost = Part<Grid>("PART_DockHost");
        var root = Part<Grid>("PART_Root");
        dockTopRow = root.RowDefinitions[1];
        dockTopSplitRow = root.RowDefinitions[2];
        dockTopSplitter = Part<GridSplitter>("PART_DockSplitter");
        mainTabScroll = Part<ScrollViewer>("PART_TabScroll");
        mainTabRow = Part<StackPanel>("PART_TabRow");
        mainTabLeft = Part<RepeatButton>("PART_TabLeft");
        mainTabRight = Part<RepeatButton>("PART_TabRight");
        System.Windows.Automation.AutomationProperties.SetName(dockTopSplitter, "Resize docked spectrum");
        dockTopSplitter.DragCompleted += (_, _) =>
        {
            double height = dockTopRow.ActualHeight;
            spectrumLayout = spectrumLayout.Compact ? spectrumLayout with { CompactHeight = height } : spectrumLayout with { FullHeight = height };
            SaveSpectrumLayout();
        };
        mainTabLeft.Click += (_, _) => mainTabScroll.ScrollToHorizontalOffset(mainTabScroll.HorizontalOffset - 120);
        mainTabRight.Click += (_, _) => mainTabScroll.ScrollToHorizontalOffset(mainTabScroll.HorizontalOffset + 120);
        // Show ‹ › only when the tabs really do not fit; the buttons' own width must not keep them visible.
        void UpdateTabButtons() { var v = MainTabOverflow > 0.5 ? Visibility.Visible : Visibility.Collapsed; if (mainTabLeft.Visibility != v) mainTabLeft.Visibility = mainTabRight.Visibility = v; }
        mainTabScroll.ScrollChanged += (_, _) => UpdateTabButtons();
        mainTabScroll.SizeChanged += (_, _) => UpdateTabButtons();
        WorkspaceTabs.SizeChanged += (_, _) => ApplyDockSizes();
        // Slightly tighter main-tab padding (native 6 px → 3 px each side) so all ten tabs fit in one row at 1040×740.
        void Tighten() { foreach (var tab in WorkspaceTabs.Items.OfType<TabItem>()) tab.Padding = new Thickness(3, 2, 3, 2); }
        Tighten();
        ((System.Collections.Specialized.INotifyCollectionChanged)WorkspaceTabs.Items).CollectionChanged += (_, _) => Tighten();
    }

    /// <summary>Main tab row position relative to the window (for the layout check).</summary>
    internal Point MainTabRowOrigin => mainTabRow.TranslatePoint(new Point(0, 0), this);
    internal double MainTabRowHeight => mainTabRow.ActualHeight;
    /// <summary>How many pixels the main tabs need beyond the full width available to them (0 = they fit).</summary>
    internal double MainTabOverflow
    {
        get
        {
            double buttons = mainTabLeft.Visibility == Visibility.Visible ? mainTabLeft.ActualWidth + mainTabRight.ActualWidth : 0;
            return Math.Max(0, mainTabScroll.ExtentWidth - (mainTabScroll.ViewportWidth + buttons));
        }
    }
    internal string MainTabDiagnostics => $"extent {mainTabScroll.ExtentWidth:0}, viewport {mainTabScroll.ViewportWidth:0}, tabs " +
        string.Join(" ", WorkspaceTabs.Items.OfType<TabItem>().Select(t => $"{t.Header}={t.ActualWidth:0}(pad {t.Padding.Left:0})"));

    // ------------------------------------------------------------------ placement
    private static string WorkspaceKey(TabItem? tab) => tab?.Header as string ?? "";

    private bool BroadcastPageSelected => BroadcastTab.IsSelected && (AmTab.IsSelected || FmTab.IsSelected);

    /// <summary>Placement that applies to the workspace currently on screen.</summary>
    private string EffectivePlacement()
    {
        if (spectrumLayout.Placement == SpectrumPlacements.Window) return SpectrumPlacements.Window;
        var top = WorkspaceTabs.SelectedItem as TabItem;
        if (top == null || ReferenceEquals(top, SpectrumTab) || BroadcastPageSelected) return SpectrumPlacements.Tab;
        if (spectrumLayout.Overrides.TryGetValue(WorkspaceKey(top), out var chosen) && chosen != SpectrumPlacements.Default) return chosen;
        return spectrumLayout.Placement;
    }

    /// <summary>Moves the one shared spectrum to where the current layout wants it. Replaces BroadcastWorkspace Move()'s re-parenting.</summary>
    private void PlaceSpectrum()
    {
        if (placingSpectrum || dockTopHost == null) return;
        placingSpectrum = true;
        try
        {
            string placement = EffectivePlacement();
            Panel? target = placement switch
            {
                SpectrumPlacements.DockTop => dockTopHost,
                SpectrumPlacements.Window => EnsureSpectrumWindow(),
                _ => BroadcastPageSelected ? (FmTab.IsSelected ? FmSpectrumHost : AmSpectrumHost) : null
            };
            bool alreadyThere = target == null ? ReferenceEquals(SpectrumTab.Content, SharedSpectrum) : ReferenceEquals(SharedSpectrum.Parent, target);
            if (!alreadyThere)
            {
                if (Mouse.Captured is DependencyObject captured && (ReferenceEquals(captured, SharedSpectrum) || IsInside(captured, SharedSpectrum))) Mouse.Capture(null);
                DetachSpectrum();
                if (target == null) SpectrumTab.Content = SharedSpectrum;
                else target.Children.Add(SharedSpectrum);
            }
            if (target != null && !ReferenceEquals(SpectrumTab.Content, spectrumElsewhere)) SpectrumTab.Content = spectrumElsewhere;
            if (placement != SpectrumPlacements.Window && spectrumWindow != null) { closingSpectrumWindow = true; spectrumWindow.Close(); }
            CurrentSpectrumPlacement = placement;
            if (spectrumToolbarStack != null)
                spectrumToolbarStack.Visibility = placement == SpectrumPlacements.DockTop && spectrumLayout.Compact ? Visibility.Collapsed : Visibility.Visible;
            ApplyDockSizes();
            RefreshLayoutBar();
        }
        finally { placingSpectrum = false; }
    }

    private static bool IsInside(DependencyObject node, DependencyObject ancestor)
    {
        for (DependencyObject? current = node; current != null; current = current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    private void DetachSpectrum()
    {
        if (ReferenceEquals(SpectrumTab.Content, SharedSpectrum)) SpectrumTab.Content = spectrumElsewhere;
        else if (SharedSpectrum.Parent is Panel panel) panel.Children.Remove(SharedSpectrum);
        else if (SharedSpectrum.Parent is ContentControl control) control.Content = null;
    }

    private void ApplyDockSizes()
    {
        if (dockTopHost == null) return;
        bool docked = CurrentSpectrumPlacement == SpectrumPlacements.DockTop;
        dockTopHost.Visibility = dockTopSplitter.Visibility = docked ? Visibility.Visible : Visibility.Collapsed;
        if (docked)
        {
            double minimum = spectrumLayout.Compact ? SpectrumLayout.CompactMinimum : SpectrumLayout.FullMinimum;
            double available = Math.Max(0, WorkspaceTabs.ActualHeight - mainTabRow.ActualHeight);
            if (available <= 0) available = ActualHeight - 160;
            double cap = Math.Max(minimum, available * spectrumLayout.HeightCapPercent / 100.0);
            double wanted = spectrumLayout.Compact ? spectrumLayout.CompactHeight : spectrumLayout.FullHeight;
            dockTopRow.MinHeight = minimum; dockTopRow.MaxHeight = cap;
            dockTopRow.Height = new GridLength(Math.Clamp(wanted, minimum, cap));
            dockTopSplitRow.Height = new GridLength(6);
        }
        else { dockTopRow.MinHeight = 0; dockTopRow.MaxHeight = double.PositiveInfinity; dockTopRow.Height = new GridLength(0); dockTopSplitRow.Height = new GridLength(0); }
    }

    // ------------------------------------------------------------------ separate window
    private Panel EnsureSpectrumWindow()
    {
        if (spectrumWindow != null && spectrumWindowHost != null) return spectrumWindowHost;
        spectrumWindowHost = new Grid { Margin = new Thickness(8) };
        var window = new Window
        {
            Title = $"M.R.S. Spectrum — Receiver {workspaceSlot}", Owner = this, Content = spectrumWindowHost, Width = 900, Height = 560, MinWidth = 520, MinHeight = 320,
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x17, 0x22)), Foreground = new SolidColorBrush(Color.FromRgb(0xE4, 0xED, 0xF8)), FontFamily = FontFamily, FontSize = FontSize,
            ShowInTaskbar = !diagnosticWorkspace, WindowStartupLocation = WindowStartupLocation.Manual, Icon = Icon
        };
        if (spectrumLayout.WindowBounds is { } b)
        {
            var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            var wanted = new Rect(b[0], b[1], Math.Max(520, b[2]), Math.Max(320, b[3]));
            if (screen.IntersectsWith(wanted) && wanted.Left >= screen.Left - wanted.Width / 2 && wanted.Top >= screen.Top) { window.Left = wanted.Left; window.Top = wanted.Top; window.Width = wanted.Width; window.Height = wanted.Height; }
            else { window.Left = Left + 60; window.Top = Top + 60; }
        }
        else { window.Left = Left + 60; window.Top = Top + 60; }
        if (diagnosticWorkspace) window.Left = -16000;
        window.PreviewKeyDown += (_, e) => { if (e.Key == Key.F1) { e.Handled = true; OpenHelp(); } };
        window.Closing += (_, _) =>
        {
            if (!diagnosticWorkspace && window.WindowState == WindowState.Normal)
                spectrumLayout = spectrumLayout with { WindowBounds = [window.Left, window.Top, window.Width, window.Height] };
        };
        window.Closed += (_, _) =>
        {
            bool userClosed = !closingSpectrumWindow;
            closingSpectrumWindow = false;
            if (ReferenceEquals(SharedSpectrum.Parent, spectrumWindowHost)) spectrumWindowHost!.Children.Remove(SharedSpectrum);
            spectrumWindow = null; spectrumWindowHost = null;
            if (closing) return;
            if (userClosed) spectrumLayout = spectrumLayout with { Placement = SpectrumPlacements.Tab };
            SaveSpectrumLayout();
            Dispatcher.BeginInvoke(new Action(PlaceSpectrum));
        };
        spectrumWindow = window;
        window.Show();
        return spectrumWindowHost;
    }

    // ------------------------------------------------------------------ controls
    private void BuildLayoutBar()
    {
        spectrumToolbarStack = SubaudiblePanel.Parent as Panel;
        var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 3) };
        var dim = new SolidColorBrush(Color.FromRgb(0x9E, 0xB1, 0xC9));
        TextBlock Label(string text) => new() { Text = text, Foreground = dim, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0), FontSize = 12 };
        bar.Children.Add(Label("Spectrum"));
        layoutPlacementBox = new ComboBox { Width = 140, MinHeight = 24, FontSize = 12, Margin = new Thickness(0, 0, 8, 0), ToolTip = "Spectrum layout for all workspaces. Choosing one clears any per-workspace exceptions." };
        System.Windows.Automation.AutomationProperties.SetName(layoutPlacementBox, "Spectrum layout for all workspaces");
        foreach (var (_, label) in SpectrumPlacements.Choices) layoutPlacementBox.Items.Add(label);
        layoutPlacementBox.SelectionChanged += (_, _) =>
        {
            if (refreshingLayoutBar || layoutPlacementBox.SelectedIndex < 0) return;
            SetSpectrumPlacement(SpectrumPlacements.Choices[layoutPlacementBox.SelectedIndex].Key);
        };
        bar.Children.Add(layoutPlacementBox);
        layoutCompactBox = new CheckBox { Content = "Compact", Foreground = new SolidColorBrush(Color.FromRgb(0xE4, 0xED, 0xF8)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0), FontSize = 12, ToolTip = "Compact when docked: hides the tuning toolbar and uses less height. Click-to-tune, markers and zoom still work." };
        layoutCompactBox.Checked += (_, _) => { if (!refreshingLayoutBar) SetSpectrumCompact(true); };
        layoutCompactBox.Unchecked += (_, _) => { if (!refreshingLayoutBar) SetSpectrumCompact(false); };
        bar.Children.Add(layoutCompactBox);
        layoutOverrideLabel = Label("Only this workspace");
        bar.Children.Add(layoutOverrideLabel);
        layoutOverrideBox = new ComboBox { Width = 130, MinHeight = 24, FontSize = 12, Margin = new Thickness(0, 0, 8, 0), ToolTip = "An exception for the workspace tab that is open now. Choosing a layout for all workspaces clears exceptions." };
        System.Windows.Automation.AutomationProperties.SetName(layoutOverrideBox, "Spectrum layout exception for this workspace");
        foreach (var (_, label) in SpectrumPlacements.OverrideChoices) layoutOverrideBox.Items.Add(label);
        layoutOverrideBox.SelectionChanged += (_, _) =>
        {
            if (refreshingLayoutBar || layoutOverrideBox.SelectedIndex < 0) return;
            SetWorkspaceOverride(WorkspaceKey(WorkspaceTabs.SelectedItem as TabItem), SpectrumPlacements.OverrideChoices[layoutOverrideBox.SelectedIndex].Key);
        };
        bar.Children.Add(layoutOverrideBox);
        layoutReturnButton = new Button { Content = "Return to main window", Padding = new Thickness(8, 3, 8, 3), FontSize = 12, Visibility = Visibility.Collapsed };
        layoutReturnButton.Click += (_, _) => SetSpectrumPlacement(SpectrumPlacements.Tab);
        bar.Children.Add(layoutReturnButton);
        Grid.SetRow(bar, 0);
        SharedSpectrum.Children.Add(bar);
        WorkspaceTabs.SelectionChanged += (_, e) => { if (ReferenceEquals(e.Source, WorkspaceTabs)) RefreshLayoutBar(); };
    }

    /// <summary>"Layout" button beside Receivers / ? / About, so the choice is reachable when the spectrum is hidden.</summary>
    private void BuildLayoutMenu()
    {
        if (OtherReceiverButton.Parent is not Panel header) return;
        var button = new Button { Content = "Layout ▾", ToolTip = "Spectrum layout: Spectrum tab only, docked under the tabs, or a separate window" };
        var menu = new ContextMenu();
        button.Click += (_, _) =>
        {
            menu.Items.Clear();
            menu.Items.Add(new MenuItem { Header = "All workspaces", IsEnabled = false });
            foreach (var (key, label) in SpectrumPlacements.Choices)
            {
                var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = spectrumLayout.Placement == key && spectrumLayout.Overrides.Count == 0 };
                item.Click += (_, _) => SetSpectrumPlacement(key);
                menu.Items.Add(item);
            }
            menu.Items.Add(new Separator());
            var compact = new MenuItem { Header = "Compact when docked", IsCheckable = true, IsChecked = spectrumLayout.Compact };
            compact.Click += (_, _) => SetSpectrumCompact(!spectrumLayout.Compact);
            menu.Items.Add(compact);
            var top = WorkspaceTabs.SelectedItem as TabItem;
            if (top != null && !ReferenceEquals(top, SpectrumTab) && !BroadcastPageSelected && spectrumLayout.Placement != SpectrumPlacements.Window)
            {
                string key = WorkspaceKey(top);
                var sub = new MenuItem { Header = $"Only {key}" };
                string current = spectrumLayout.Overrides.GetValueOrDefault(key, SpectrumPlacements.Default);
                foreach (var (choice, label) in SpectrumPlacements.OverrideChoices)
                {
                    var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = current == choice };
                    item.Click += (_, _) => SetWorkspaceOverride(key, choice);
                    sub.Items.Add(item);
                }
                menu.Items.Add(sub);
            }
            if (spectrumLayout.Overrides.Count > 0)
            {
                var clear = new MenuItem { Header = $"Clear exceptions ({string.Join(", ", spectrumLayout.Overrides.Keys)})" };
                clear.Click += (_, _) => SetSpectrumPlacement(spectrumLayout.Placement);
                menu.Items.Add(clear);
            }
            menu.PlacementTarget = button; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        };
        header.Children.Add(button);
    }

    private void RefreshLayoutBar()
    {
        if (layoutPlacementBox == null) return;
        refreshingLayoutBar = true;
        try
        {
            layoutPlacementBox.SelectedIndex = Array.FindIndex(SpectrumPlacements.Choices, c => c.Key == spectrumLayout.Placement);
            layoutCompactBox.IsChecked = spectrumLayout.Compact;
            var top = WorkspaceTabs.SelectedItem as TabItem;
            bool workspace = top != null && !ReferenceEquals(top, SpectrumTab) && !BroadcastPageSelected && spectrumLayout.Placement != SpectrumPlacements.Window;
            layoutOverrideLabel.Visibility = layoutOverrideBox.Visibility = workspace ? Visibility.Visible : Visibility.Collapsed;
            if (workspace)
            {
                string key = WorkspaceKey(top);
                string current = spectrumLayout.Overrides.GetValueOrDefault(key, SpectrumPlacements.Default);
                bool exception = current != SpectrumPlacements.Default;
                layoutOverrideLabel.Text = exception ? $"{key} only (exception):" : $"{key} only:";
                layoutOverrideLabel.Foreground = exception ? new SolidColorBrush(Color.FromRgb(0xFF, 0xC8, 0x57)) : new SolidColorBrush(Color.FromRgb(0x9E, 0xB1, 0xC9));
                layoutOverrideBox.SelectedIndex = Math.Max(0, Array.FindIndex(SpectrumPlacements.OverrideChoices, c => c.Key == current));
            }
            layoutReturnButton.Visibility = spectrumLayout.Placement == SpectrumPlacements.Window ? Visibility.Visible : Visibility.Collapsed;
        }
        finally { refreshingLayoutBar = false; }
    }

    /// <summary>Main choice: applies to every workspace and clears per-workspace exceptions, so the result matches what was chosen.</summary>
    internal void SetSpectrumPlacement(string placement)
    {
        if (!SpectrumPlacements.Valid(placement)) return;
        int cleared = spectrumLayout.Overrides.Count;
        spectrumLayout = spectrumLayout with { Placement = placement, Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) };
        SaveSpectrumLayout(); PlaceSpectrum();
        Log($"Spectrum layout for all workspaces: {SpectrumPlacements.Choices.First(c => c.Key == placement).Label}" + (cleared > 0 ? $" (cleared {cleared} workspace exception(s))." : "."));
    }
    internal void SetSpectrumCompact(bool compact) { spectrumLayout = spectrumLayout with { Compact = compact }; SaveSpectrumLayout(); PlaceSpectrum(); }
    internal void SetWorkspaceOverride(string key, string placement)
    {
        if (string.IsNullOrEmpty(key)) return;
        var overrides = new Dictionary<string, string>(spectrumLayout.Overrides, StringComparer.OrdinalIgnoreCase);
        if (placement == SpectrumPlacements.Default) overrides.Remove(key); else overrides[key] = placement;
        spectrumLayout = spectrumLayout with { Overrides = overrides };
        SaveSpectrumLayout(); PlaceSpectrum();
    }
    /// <summary>Used by the layout check to apply a whole layout at once.</summary>
    internal void ApplySpectrumLayoutForTest(SpectrumLayout layout) { spectrumLayout = layout.Normalize(); PlaceSpectrum(); }

    private void SaveSpectrumLayout()
    {
        if (spectrumLayoutPath == null) return;
        try { File.WriteAllText(spectrumLayoutPath + ".tmp", JsonSerializer.Serialize(spectrumLayout, new JsonSerializerOptions { WriteIndented = true })); File.Move(spectrumLayoutPath + ".tmp", spectrumLayoutPath, true); }
        catch (Exception ex) { Log("Spectrum layout not saved: " + ex.Message); }
    }

    private FrameworkElement SpectrumElsewhereNotice()
    {
        var panel = new StackPanel { Margin = new Thickness(16), VerticalAlignment = VerticalAlignment.Top };
        panel.Children.Add(new TextBlock { Text = "The spectrum is open in its own window.", Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0xB1, 0xC9)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        var back = new Button { Content = "Show the spectrum here (Spectrum tab only)", HorizontalAlignment = HorizontalAlignment.Left };
        back.Click += (_, _) => SetSpectrumPlacement(SpectrumPlacements.Tab);
        panel.Children.Add(back);
        return panel;
    }
}
