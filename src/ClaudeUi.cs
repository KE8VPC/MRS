// =====================================================================================
//  M.R.S. UI refresh "ui1"  —  added by Claude on 2026-11-13 (based on 0.51.0-preview2); merged into 0.54.0-preview1 on 2026-11-16
//  (slider widths reduced 120/110 → 90 px so the radio bar fits a 1040 px window next to the docked-spectrum layout)
//
//  Everything for this refresh lives in this file plus Themes/ClaudeTheme.xaml.
//  The only edits to existing files are small hooks, listed in CLAUDE-UI-NOTES.md:
//    * App.xaml            – merges Themes/ClaudeTheme.xaml (old Button/TabItem/ComboBox styles moved there)
//    * MainWindow.xaml     – x:Name="HeaderPanel", x:Name="FooterGrid", x:Name="FooterStatus", version text
//    * MainWindow.xaml.cs  – one call: InitializeClaudeUi();
//    * MRS.csproj          – Version 0.51.0-preview2-ui1
//
//  Design rule: the new controls only *mirror* existing controls (FrequencyBox, FmMode, VolumeSlider,
//  RfSquelchSlider, StartRtlButton, RecordButton, RxTxLabel …) and call the existing handlers
//  (StartRtl, StopSource, LiveTune, StartRecording, FinishRecording, NavigationStep).
//  No receiver, decoder, settings-file or smoke-test behaviour was changed.
// =====================================================================================
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MRS;

public partial class MainWindow
{
    private FrequencyDigits? radioFrequency;
    private ComboBox? radioMode;
    private Button? radioStart, radioStop, radioRecord;
    private Border? radioTxPill;
    private TextBlock? radioTxText;
    private TextBlock? statusSource, statusGate, statusLevel, statusRecording, statusMessage;
    private readonly DispatcherTimer claudeUiTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private DispatcherTimer? wheelTuneTimer;
    private long pendingWheelHz;
    private DateTime lastWheelUtc = DateTime.MinValue;
    private bool syncingRadioMode;
    private Style? primaryButtonStyle, dangerButtonStyle;

    private void InitializeClaudeUi()
    {
        ClaudeTheme.Register();
        primaryButtonStyle = TryFindResource("Mrs.PrimaryButton") as Style;
        dangerButtonStyle = TryFindResource("Mrs.DangerButton") as Style;
        if (TryFindResource("Mrs.SidebarTabs") is Style sidebar) SidebarTabs.Style = sidebar;
        if (primaryButtonStyle != null) StartRtlButton.Style = primaryButtonStyle;

        BuildRadioBar();
        BuildStatusBar();

        // Only show the HackRF section header when the HackRF family is selected (its content already hides itself).
        if (HackRfControls.Parent is Expander hackExpander)
        {
            void Sync() => hackExpander.Visibility = HackRfControls.Visibility;
            DependencyPropertyDescriptor.FromProperty(VisibilityProperty, typeof(StackPanel)).AddValueChanged(HackRfControls, (_, _) => Sync());
            Sync();
        }
        ClaudeHint.Attach(RtlDevices, "No receiver selected · click Find", () => RtlDevices.SelectedItem == null);

        PreviewKeyDown += ClaudeShortcuts;
        claudeUiTimer.Tick += (_, _) => RefreshClaudeUi();
        claudeUiTimer.Start();
        Closing += (_, _) => { claudeUiTimer.Stop(); wheelTuneTimer?.Stop(); };
        RefreshClaudeUi();
    }

    // ---------------------------------------------------------------- radio bar
    private void BuildRadioBar()
    {
        var bar = new DockPanel { LastChildFill = false };
        var host = new Border
        {
            Background = ClaudeTheme.Hex("#0B121C"), BorderBrush = ClaudeTheme.Hex("#26364A"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 8, 0, 2), Child = bar
        };
        AutomationProperties.SetName(host, "Radio controls");

        radioFrequency = new FrequencyDigits { VerticalAlignment = VerticalAlignment.Center };
        radioFrequency.ToolTip = "Mouse wheel over a digit tunes by that digit.\nDouble-click or Ctrl+F to type a frequency in MHz.\nPgUp / PgDn step by the spectrum step size.";
        radioFrequency.WheelTuned += WheelTune;
        radioFrequency.Entered += async hz => await EnterFrequency(hz);
        bar.Children.Add(radioFrequency);
        bar.Children.Add(new TextBlock { Text = "MHz", Foreground = ClaudeTheme.Hex("#9EB1C9"), Margin = new Thickness(6, 8, 16, 0), VerticalAlignment = VerticalAlignment.Center });

        radioMode = new ComboBox { Width = 120, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Receive mode (same setting as Receiver tab)" };
        AutomationProperties.SetName(radioMode, "Receive mode");
        foreach (var item in FmMode.Items) radioMode.Items.Add(item is ComboBoxItem c ? c.Content : item);
        radioMode.SelectedIndex = FmMode.SelectedIndex;
        radioMode.SelectionChanged += (_, _) => { if (!syncingRadioMode && radioMode.SelectedIndex >= 0 && FmMode.SelectedIndex != radioMode.SelectedIndex) FmMode.SelectedIndex = radioMode.SelectedIndex; };
        FmMode.SelectionChanged += (_, _) => { syncingRadioMode = true; try { radioMode.SelectedIndex = FmMode.SelectedIndex; } finally { syncingRadioMode = false; } };
        bar.Children.Add(radioMode);

        radioStart = new Button { Content = "▶  Start", Margin = new Thickness(12, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Start the selected SDR receiver, or apply changed settings  (F5)", Style = primaryButtonStyle };
        radioStart.Click += (_, e) => StartRtl(radioStart, e);
        radioStop = new Button { Content = "■  Stop", Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Stop the current source  (Shift+F5)", Style = dangerButtonStyle };
        radioStop.Click += (_, e) => StopSource(radioStop, e);
        radioRecord = new Button { Content = "●  Rec", Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Record demodulated audio to WAV; click again to finish  (Ctrl+R)" };
        radioRecord.Click += (_, _) => ToggleRecording();
        bar.Children.Add(radioStart); bar.Children.Add(radioStop); bar.Children.Add(radioRecord);

        // Right side (first added = right-most)
        radioTxText = new TextBlock { Text = "Idle", FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
        radioTxPill = new Border { CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 3, 12, 3), MinWidth = 70, Child = radioTxText, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0), ToolTip = "Receive / transmit state" };
        DockPanel.SetDock(radioTxPill, Dock.Right); bar.Children.Add(radioTxPill);
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock)).AddValueChanged(RxTxLabel, (_, _) => RefreshTxPill());
        RxTxPanel.Visibility = Visibility.Collapsed; // replaced by the larger pill; RxTxLabel still drives it

        var squelch = new Slider { Width = 90, Minimum = RfSquelchSlider.Minimum, Maximum = RfSquelchSlider.Maximum, VerticalAlignment = VerticalAlignment.Center, ToolTip = "RF squelch threshold (Receiver → RF squelch). Left looser, right tighter." };
        AutomationProperties.SetName(squelch, "RF squelch threshold");
        squelch.SetBinding(RangeBase.ValueProperty, new Binding(nameof(Slider.Value)) { Source = RfSquelchSlider, Mode = BindingMode.TwoWay });
        squelch.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled)) { Source = RfSquelchSlider });
        SliderWheel.Enable(squelch, .5);
        var volume = new Slider { Width = 90, Minimum = 0, Maximum = 1, IsMoveToPointEnabled = true, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Monitor volume (Audio → Listening)" };
        AutomationProperties.SetName(volume, "Monitor volume");
        volume.SetBinding(RangeBase.ValueProperty, new Binding(nameof(Slider.Value)) { Source = VolumeSlider, Mode = BindingMode.TwoWay });
        SliderWheel.Enable(volume, .01);
        foreach (UIElement element in new UIElement[] { squelch, ClaudeCaption("Sql"), volume, ClaudeCaption("Vol") }) { DockPanel.SetDock(element, Dock.Right); bar.Children.Add(element); }

        HeaderPanel.Children.Insert(1, host);
    }
    private static TextBlock ClaudeCaption(string text) => new() { Text = text, Foreground = ClaudeTheme.Hex("#9EB1C9"), FontSize = 12, Margin = new Thickness(10, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };

    private void WheelTune(long hz)
    {
        lastWheelUtc = DateTime.UtcNow;
        FrequencyBox.Text = (hz / 1000000m).ToString("F6", CultureInfo.InvariantCulture);
        if (!rfMode || receiver.IsReplay) return;
        pendingWheelHz = hz;
        if (wheelTuneTimer == null)
        {
            wheelTuneTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(110) };
            wheelTuneTimer.Tick += async (_, _) => { wheelTuneTimer.Stop(); await LiveTune(pendingWheelHz); };
        }
        wheelTuneTimer.Stop(); wheelTuneTimer.Start();
    }
    private async Task EnterFrequency(long hz)
    {
        lastWheelUtc = DateTime.UtcNow;
        FrequencyBox.Text = (hz / 1000000m).ToString("F6", CultureInfo.InvariantCulture);
        if (rfMode && !receiver.IsReplay) await LiveTune(hz);
        else Log($"Frequency set to {FrequencyBox.Text} MHz. Press Start (F5) to receive.");
    }
    private void ToggleRecording()
    {
        if (FinishRecordButton.IsEnabled) FinishRecording(this, new RoutedEventArgs());
        else if (RecordButton.IsEnabled) StartRecording(this, new RoutedEventArgs());
    }
    private void RefreshTxPill()
    {
        if (radioTxPill == null || radioTxText == null) return;
        string state = RxTxLabel.Text;
        (string text, string back, string border, string fore) look = state switch
        {
            "TX" => ("●  TX", "#B3262E", "#FF7A80", "#FFFFFF"),
            "RX" => ("●  RX", "#1D5646", "#66DEBA", "#E9FFF7"),
            _ => ("Idle", "#1A2636", "#35445B", "#9EB1C9")
        };
        radioTxText.Text = look.text;
        radioTxText.Foreground = ClaudeTheme.Hex(look.fore);
        radioTxPill.Background = ClaudeTheme.Hex(look.back);
        radioTxPill.BorderBrush = ClaudeTheme.Hex(look.border);
        radioTxPill.BorderThickness = new Thickness(1);
    }

    // ---------------------------------------------------------------- status bar
    private void BuildStatusBar()
    {
        var panel = new WrapPanel { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 16, 0) };
        TextBlock Segment(string tip)
        {
            var text = new TextBlock { FontSize = 12, Foreground = ClaudeTheme.Hex("#9EB1C9"), Margin = new Thickness(0, 0, 18, 0), ToolTip = tip, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 430 };
            panel.Children.Add(text); return text;
        }
        statusSource = Segment("Active source");
        statusGate = Segment("RF squelch gate");
        statusLevel = Segment("Audio level");
        statusRecording = Segment("Recording in progress");
        statusRecording.Foreground = ClaudeTheme.Hex("#FF8A8A");
        statusMessage = Segment("Latest receiver message");
        FooterStatus.Visibility = Visibility.Collapsed;
        Grid.SetColumn(panel, 0);
        FooterGrid.Children.Add(panel);
    }

    private void RefreshClaudeUi()
    {
        if (radioFrequency == null || radioStart == null || radioStop == null || radioRecord == null) return;
        if (!radioFrequency.IsEditing && (DateTime.UtcNow - lastWheelUtc).TotalMilliseconds > 700
            && decimal.TryParse(FrequencyBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mhz) && mhz >= 0 && mhz < 10000)
            radioFrequency.Value = (long)Math.Round(mhz * 1000000m);

        bool live = rfMode && !receiver.IsReplay;
        radioStart.Content = live ? "✓  Apply" : "▶  Start";
        radioStart.IsEnabled = StartRtlButton.IsEnabled;
        radioStop.IsEnabled = rfMode || engine.Active;
        bool finishing = FinishRecordButton.IsEnabled;
        radioRecord.IsEnabled = RecordButton.IsEnabled || finishing;
        string recordText = finishing ? "■  Finish rec" : "●  Rec";
        if (!Equals(radioRecord.Content, recordText)) { radioRecord.Content = recordText; radioRecord.Style = finishing ? dangerButtonStyle : null; }

        if (statusSource != null) statusSource.Text = SourceLabel.Text;
        if (statusGate != null) { statusGate.Text = RfSquelchStatus.Text; statusGate.Visibility = rfMode ? Visibility.Visible : Visibility.Collapsed; }
        if (statusLevel != null) statusLevel.Text = LevelLabel.Text;
        if (statusRecording != null)
        {
            string recording = finishing ? "● " + RecordLabel.Text : "";
            if (receiver.IsRecording) recording = (recording.Length > 0 ? recording + "  ·  " : "● ") + IqRecordLabel.Text;
            statusRecording.Text = recording;
            statusRecording.Visibility = recording.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        if (statusMessage != null) { statusMessage.Text = RtlStatus.Text; statusMessage.ToolTip = RtlStatus.Text; }
        RefreshTxPill();
    }

    // ---------------------------------------------------------------- keyboard shortcuts
    private void ClaudeShortcuts(object sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var mods = Keyboard.Modifiers;
        var focused = Keyboard.FocusedElement;
        if (key == Key.F5 && mods == ModifierKeys.None) { e.Handled = true; StartRtl(this, new RoutedEventArgs()); return; }
        if (key == Key.F5 && mods == ModifierKeys.Shift) { e.Handled = true; StopSource(this, new RoutedEventArgs()); return; }
        if (mods == ModifierKeys.Control && key == Key.F) { e.Handled = true; radioFrequency?.BeginEdit(); return; }
        if (mods == ModifierKeys.Control && key == Key.R) { e.Handled = true; ToggleRecording(); return; }
        if (mods == ModifierKeys.Control && key >= Key.D0 && key <= Key.D9)
        {
            var tabs = WorkspaceTabs.Items.OfType<TabItem>().Where(t => t.Visibility == Visibility.Visible && t.IsEnabled).ToList();
            int index = key == Key.D0 ? tabs.Count - 1 : key - Key.D1;
            if (index >= 0 && index < tabs.Count) { tabs[index].IsSelected = true; e.Handled = true; }
            return;
        }
        bool quietFocus = focused == null || focused is Window or Button or TabItem or FrequencyDigits or SpectrumView;
        if (mods == ModifierKeys.None && quietFocus && (key == Key.PageUp || key == Key.PageDown) && rfMode && !receiver.IsReplay)
        {
            e.Handled = true;
            NavigationStep(new Button { Tag = key == Key.PageUp ? "1" : "-1" }, new RoutedEventArgs());
        }
    }
}

// ===================================================================================== frequency readout
/// <summary>Large tunable frequency display. Wheel over a digit = ± that digit; double-click = type MHz.</summary>
internal sealed class FrequencyDigits : Border
{
    private const long Max = 9_999_999_999;
    private readonly StackPanel digits = new() { Orientation = Orientation.Horizontal };
    private readonly TextBox editor = new() { FontSize = 22, MinWidth = 200, Visibility = Visibility.Collapsed, FontFamily = new FontFamily("Consolas"), Padding = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly List<(TextBlock Cell, long Place)> cells = [];
    private long value = -1;
    internal event Action<long>? WheelTuned;
    internal event Action<long>? Entered;
    internal bool IsEditing => editor.Visibility == Visibility.Visible;

    internal long Value
    {
        get => Math.Max(0, value);
        set { long next = Math.Clamp(value, 0, Max); if (next == this.value) return; this.value = next; Render(); }
    }

    public FrequencyDigits()
    {
        Background = Brushes.Transparent;
        Focusable = true;
        AutomationProperties.SetName(this, "Tuned frequency");
        AutomationProperties.SetName(editor, "Type frequency in MHz, Enter to tune");
        var grid = new Grid(); grid.Children.Add(digits); grid.Children.Add(editor); Child = grid;
        for (int k = 9; k >= 0; k--)
        {
            if (k == 5) digits.Children.Add(Separator(1));
            if (k == 2) digits.Children.Add(Separator(.55));
            long place = (long)Math.Pow(10, k);
            var cell = new TextBlock { Text = "0", FontFamily = new FontFamily("Consolas"), FontSize = 28, FontWeight = FontWeights.SemiBold, Padding = new Thickness(1, 0, 1, 0), Background = Brushes.Transparent, Cursor = Cursors.SizeNS };
            cell.MouseEnter += (_, _) => cell.Background = ClaudeTheme.Hex("#1E2B3D");
            cell.MouseLeave += (_, _) => cell.Background = Brushes.Transparent;
            cell.MouseWheel += (_, e) =>
            {
                e.Handled = true;
                long next = Math.Clamp(Value + (e.Delta > 0 ? place : -place), 0, Max);
                if (next == Value) return;
                Value = next; WheelTuned?.Invoke(next);
            };
            digits.Children.Add(cell); cells.Add((cell, place));
        }
        MouseLeftButtonDown += (_, e) => { if (e.ClickCount == 2) { e.Handled = true; BeginEdit(); } else Focus(); };
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { e.Handled = true; EndEdit(); }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (decimal.TryParse(editor.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mhz) && mhz > 0 && mhz < 10000 && decimal.Truncate(mhz * 1000000m) == mhz * 1000000m)
                { long hz = (long)(mhz * 1000000m); EndEdit(); Value = hz; Entered?.Invoke(hz); }
                else { editor.BorderBrush = ClaudeTheme.Hex("#E5737A"); editor.ToolTip = "Enter MHz with a decimal point and up to six decimals, e.g. 146.52"; }
            }
        };
        editor.LostKeyboardFocus += (_, _) => EndEdit();
        Value = 0;
    }
    private static TextBlock Separator(double opacity) => new() { Text = ".", FontFamily = new FontFamily("Consolas"), FontSize = 28, Foreground = ClaudeTheme.Hex("#66DEBA"), Opacity = opacity, Margin = new Thickness(-3, 0, -3, 0) };
    private void Render()
    {
        long v = Value; bool leading = true;
        foreach (var (cell, place) in cells)
        {
            int digit = (int)(v / place % 10);
            if (digit != 0 || place <= 1_000_000) leading = false;
            cell.Text = digit.ToString(CultureInfo.InvariantCulture);
            cell.Foreground = ClaudeTheme.Hex(leading ? "#1F2B3B" : place >= 1_000_000 ? "#E9FFF7" : "#66DEBA");
        }
    }
    internal void BeginEdit()
    {
        editor.Text = (Value / 1000000m).ToString("0.000000", CultureInfo.InvariantCulture);
        editor.ClearValue(Control.BorderBrushProperty); editor.ToolTip = null;
        digits.Visibility = Visibility.Hidden; editor.Visibility = Visibility.Visible;
        editor.Focus(); editor.SelectAll();
    }
    private void EndEdit() { if (!IsEditing) return; editor.Visibility = Visibility.Collapsed; digits.Visibility = Visibility.Visible; }
}

// ===================================================================================== global theme helpers
internal static class ClaudeTheme
{
    private static bool registered;
    private static readonly Dictionary<string, SolidColorBrush> brushes = [];
    private static readonly DependencyProperty ProcessedProperty = DependencyProperty.RegisterAttached("ClaudeProcessed", typeof(bool), typeof(ClaudeTheme));

    internal static SolidColorBrush Hex(string hex)
    {
        if (brushes.TryGetValue(hex, out var cached)) return cached;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); brush.Freeze();
        brushes[hex] = brush; return brush;
    }

    /// <summary>Class-wide hooks: placeholder hints on text boxes, dark rows and readable headers on tables.</summary>
    internal static void Register()
    {
        if (registered) return; registered = true;
        EventManager.RegisterClassHandler(typeof(TextBox), FrameworkElement.LoadedEvent, new RoutedEventHandler(TextBoxLoaded));
        // Loaded is only broadcast to elements with their own Loaded handlers, so tables use their first layout pass instead.
        EventManager.RegisterClassHandler(typeof(DataGrid), FrameworkElement.SizeChangedEvent, new SizeChangedEventHandler((sender, e) => GridLoaded(sender, e)));
    }

    private static void TextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || box.IsReadOnly || box.AcceptsReturn || box.TemplatedParent != null) return;
        string? hint = AutomationProperties.GetName(box);
        if (string.IsNullOrWhiteSpace(hint)) hint = box.ToolTip as string;
        if (string.IsNullOrWhiteSpace(hint)) return;
        hint = hint.Split(';')[0].Trim();
        if (hint.Length > 64) return;
        ClaudeHint.Attach(box, hint, () => box.Text.Length == 0);
    }

    private static void GridLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (!(bool)grid.GetValue(ProcessedProperty))
        {
            grid.SetValue(ProcessedProperty, true);
            grid.AutoGeneratedColumns += (_, _) => Prettify(grid);
            // Only light tables without custom row/cell colouring are darkened, so status colours elsewhere are untouched.
            // Rows with no RowBackground render white by default, so those count as light too.
            if (grid.RowStyle == null && grid.CellStyle == null && (grid.RowBackground == null || IsLight(grid.RowBackground)))
                Darken(grid);
        }
        Prettify(grid);
    }

    private static bool IsLight(Brush? brush) => brush is SolidColorBrush s && s.Color.A > 200 && 0.299 * s.Color.R + 0.587 * s.Color.G + 0.114 * s.Color.B > 170;

    private static void Darken(DataGrid grid)
    {
        grid.Background = Hex("#0F1722");
        grid.RowBackground = Hex("#131D2A");
        grid.AlternatingRowBackground = Hex("#172435");
        grid.Foreground = Hex("#E4EDF8");
        grid.HorizontalGridLinesBrush = Hex("#223247");
        grid.VerticalGridLinesBrush = Hex("#223247");
        grid.BorderBrush = Hex("#35445B");
        grid.Resources[SystemColors.HighlightBrushKey] = Hex("#2E6A8E");
        grid.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
        grid.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = Hex("#27455F");
        grid.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = Brushes.White;
    }

    private static void Prettify(DataGrid grid)
    {
        foreach (var column in grid.Columns)
        {
            if (!column.IsAutoGenerated || column.Header is not string header || header.Contains(' ')) continue;
            column.Header = Humanize(header);
            if (double.IsPositiveInfinity(column.MaxWidth)) column.MaxWidth = 460;
        }
    }

    /// <summary>"FrequencyMHz" → "Frequency (MHz)", "BandwidthHz" → "Bandwidth (Hz)", "LastSeenUtc" → "Last seen (UTC)".</summary>
    internal static string Humanize(string name)
    {
        string? unit = null;
        foreach (var suffix in new[] { "MHz", "kHz", "Hz", "dBm", "dB", "UTC", "Utc" })
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            { unit = suffix == "Utc" ? "UTC" : suffix; name = name[..^suffix.Length]; break; }
        var words = new List<string>(); int start = 0;
        for (int i = 1; i < name.Length; i++)
        {
            char p = name[i - 1], c = name[i];
            bool boundary = (char.IsLower(p) && char.IsUpper(c)) || (char.IsLetter(p) && char.IsDigit(c)) || (char.IsDigit(p) && char.IsLetter(c))
                            || (char.IsUpper(p) && char.IsUpper(c) && i + 1 < name.Length && char.IsLower(name[i + 1]));
            if (boundary) { words.Add(name[start..i]); start = i; }
        }
        words.Add(name[start..]);
        for (int i = 1; i < words.Count; i++) if (words[i].Any(char.IsLower)) words[i] = words[i].ToLowerInvariant();
        string text = string.Join(" ", words.Where(w => w.Length > 0));
        return unit == null ? text : $"{text} ({unit})";
    }
}

/// <summary>Grey placeholder text drawn over an empty field. Adds no layout and ignores the mouse.</summary>
internal static class ClaudeHint
{
    private static readonly DependencyProperty AttachedProperty = DependencyProperty.RegisterAttached("ClaudeHintAttached", typeof(bool), typeof(ClaudeHint));

    internal static void Attach(Control control, string text, Func<bool> isEmpty)
    {
        if ((bool)control.GetValue(AttachedProperty)) return;
        control.SetValue(AttachedProperty, true);
        HintAdorner? adorner = null;
        void Ensure()
        {
            var layer = AdornerLayer.GetAdornerLayer(control);
            if (layer == null) return;
            if (adorner != null && layer.GetAdorners(control)?.Contains(adorner) == true) { adorner.InvalidateVisual(); return; }
            adorner = new HintAdorner(control, text, isEmpty) { IsHitTestVisible = false };
            layer.Add(adorner);
        }
        void Refresh() => adorner?.InvalidateVisual();
        control.Loaded += (_, _) => Ensure();
        control.GotKeyboardFocus += (_, _) => Refresh();
        control.LostKeyboardFocus += (_, _) => Refresh();
        control.IsVisibleChanged += (_, _) => Refresh();
        if (control is TextBox box) box.TextChanged += (_, _) => Refresh();
        if (control is Selector selector) selector.SelectionChanged += (_, _) => Refresh();
        if (control.IsLoaded) Ensure();
    }

    private sealed class HintAdorner(Control control, string text, Func<bool> isEmpty) : Adorner(control)
    {
        private static readonly Brush Ink = ClaudeTheme.Hex("#6F8299");
        protected override void OnRender(DrawingContext dc)
        {
            if (!control.IsVisible || control.IsKeyboardFocusWithin || !isEmpty()) return;
            double width = control.ActualWidth - (control is ComboBox ? 30 : 10);
            if (width < 20) return;
            var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(control.FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal), Math.Max(10, control.FontSize - 1), Ink,
                VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = width, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
            dc.DrawText(formatted, new Point(control is ComboBox ? 9 : 6, Math.Max(0, (control.ActualHeight - formatted.Height) / 2)));
        }
    }
}

/// <summary>Theme helper: legacy Black / DarkRed foregrounds (made for light buttons) become readable on dark surfaces.</summary>
public sealed class ClaudeForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush) return value;
        var c = brush.Color;
        double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        if (luminance >= 90) return value;
        if (c.R > c.G + 60 && c.R > c.B + 60) return ClaudeTheme.Hex("#FF8A8A");
        return ClaudeTheme.Hex("#E4EDF8");
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Theme helper: returns cell text for a tooltip only when it is long enough to be cut off.</summary>
public sealed class ClaudeLongTextConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string s && s.Length >= 28 ? s : null;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
