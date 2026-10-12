using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MRS;
public sealed class DisplayPanel : UserControl
{
    private sealed record State(int Version, DisplaySettings Current, Dictionary<string, DisplaySettings> Presets);
    private DisplaySettings settings = new();
    private Dictionary<string, DisplaySettings> presets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<DisplaySettings>> refresh = new();
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 8, 0, 0) };
    private readonly ComboBox presetList = new();
    private readonly ComboBox viewBandwidth = new();
    private double captureSpan = 24000;
    private sealed record SpanChoice(double Hz, string Label) { public override string ToString() => Label; }
    internal void SetCaptureSpan(double span) { captureSpan = span; Reflect(settings); }
    internal void SelectViewSpan(double hz) => Publish(settings with { Zoom = Math.Clamp(captureSpan / hz, 1, 32) });
    private void RefreshBandwidth(DisplaySettings value)
    {
        double visible = captureSpan / value.Zoom;
        var choices = new List<SpanChoice> { new(captureSpan, $"Full span ({captureSpan / 1000:0.##} kHz)") };
        foreach (double span in Enumerable.Range(0, 13).SelectMany(power => new[] { 1.0, 2.0, 5.0 }.Select(m => m * Math.Pow(10, power))).Concat(new[] { captureSpan / 2, captureSpan / 4, captureSpan / 8, captureSpan / 16, captureSpan / 32 }).Distinct().Order())
            if (span >= captureSpan / 32 && span < captureSpan) choices.Add(new(span, $"{span / 1000:0.##} kHz"));
        var selected = choices.FirstOrDefault(c => Math.Abs(c.Hz - visible) < .01);
        if (selected == null) { selected = new(visible, $"Custom ({visible / 1000:0.##} kHz)"); choices.Add(selected); }
        viewBandwidth.ItemsSource = choices; viewBandwidth.SelectedItem = selected;
    }
    private string path = "";
    private bool updating, writable = true;
    public event Action<DisplaySettings>? Changed;
    public event Action? ClearPeakRequested;
    public DisplayPanel()
    {
        var body = new StackPanel { Margin = new Thickness(8) };
        Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        body.Children.Add(new TextBlock { Text = "Display only · does not retune or change audio", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 12 });
        StackPanel Group(string title)
        {
            var stack = new StackPanel(); body.Children.Add(new GroupBox { Header = title, Content = stack, Margin = new Thickness(0, 12, 0, 0) }); return stack;
        }
        void Button(Panel panel, string title, Action action)
        {
            var button = new Button { Content = title, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 6, 5, 0) };
            button.Click += (_, _) => action(); panel.Children.Add(button);
        }
        void Slider(Panel panel, string title, double min, double max, Func<DisplaySettings, double> get, Func<DisplaySettings, double, DisplaySettings> set, string units = "")
        {
            var label = new TextBlock { Margin = new Thickness(0, 8, 0, 0) }; panel.Children.Add(label);
            var slider = new Slider { Minimum = min, Maximum = max, SmallChange = (max - min) / 100, LargeChange = (max - min) / 10, Value = get(settings) };
            System.Windows.Automation.AutomationProperties.SetName(slider, title);
            panel.Children.Add(slider);
            refresh.Add(value => { slider.Value = get(value); label.Text = $"{title}: {get(value):0.##}{units}"; });
            slider.ValueChanged += (_, _) => { if (!updating) Publish(set(settings, slider.Value)); };
        }
        void Check(Panel panel, string title, Func<DisplaySettings, bool> get, Func<DisplaySettings, bool, DisplaySettings> set)
        {
            var check = new CheckBox { Content = title, Foreground = Brushes.White, Margin = new Thickness(0, 8, 0, 0) }; panel.Children.Add(check);
            refresh.Add(value => check.IsChecked = get(value));
            check.Checked += (_, _) => { if (!updating) Publish(set(settings, true)); }; check.Unchecked += (_, _) => { if (!updating) Publish(set(settings, false)); };
        }
        var detail = Group("RF spectrum detail");
        var resolution = new ComboBox { ItemsSource = RfSpectrumAnalyzer.Sizes, ItemStringFormat = "{0:N0} points" };
        System.Windows.Automation.AutomationProperties.SetName(resolution,"RF FFT points");
        detail.Children.Add(resolution);
        var resolutionText=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,FontSize=11,Margin=new Thickness(0,5,0,0)};detail.Children.Add(resolutionText);
        refresh.Add(s=>{resolution.SelectedItem=s.RfFftSize;resolutionText.Text="More points reveal finer RF detail and use more CPU/memory. Actual Hz/bin is shown below the RF plot. Changing detail clears history; audio and recording continue.";});
        resolution.SelectionChanged+=(_,_)=>{if(!updating && resolution.SelectedItem is int size)Publish(settings with{RfFftSize=size});};
        var view = Group("Zoom / pan");
        view.Children.Add(new TextBlock { Text = "View bandwidth", Margin = new Thickness(0, 6, 0, 4) });
        view.Children.Add(viewBandwidth);
        System.Windows.Automation.AutomationProperties.SetName(viewBandwidth, "Visible spectrum and waterfall bandwidth");
        refresh.Add(RefreshBandwidth);
        viewBandwidth.SelectionChanged += (_, _) => { if (!updating && viewBandwidth.SelectedItem is SpanChoice choice) SelectViewSpan(choice.Hz); };
        Slider(view, "Zoom", 1, 32, s => s.Zoom, (s, v) => s with { Zoom = v }, "×");
        Slider(view, "Pan across captured span", 0, 100, s => s.Pan * 100, (s, v) => s with { Pan = v / 100 }, "%");
        Button(view, "Full span / center", () => Publish(settings with { Zoom = 1, Pan = .5 }));
        Button(view, "Center on tuned RF frequency", () => Publish(settings with { Pan = .5 }));
        view.Children.Add(new TextBlock { Text = "Wheel: zoom at pointer. Shift+wheel: pan. RF click: tune; Ctrl+click: select marker. Tab-focus the plot for +/- or arrows; Home resets.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 11, Margin = new Thickness(0, 8, 0, 0) });
        var plan = Group("U.S. band reference");
        Check(plan, "Show U.S. band reference bar", s => s.ShowBandPlan, (s, v) => s with { ShowBandPlan = v });
        Check(plan, "Include service channel markers", s => s.ShowChannelMarkers, (s, v) => s with { ShowChannelMarkers = v });
        Check(plan, "Show ARRL operating recommendations", s => s.ShowArrlPlan, (s, v) => s with { ShowArrlPlan = v });
        Check(plan, "Show CISA NIFOG channels", s => s.ShowNifogPlan, (s, v) => s with { ShowNifogPlan = v });
        Check(plan, "Show TV RF channels (2–36)", s => s.ShowTvPlan, (s, v) => s with { ShowTvPlan = v });
        Check(plan, "Show ATSC 1.0 pilot references (TV layer)", s => s.ShowTvPilots, (s, v) => s with { ShowTvPilots = v });
        plan.Children.Add(new TextBlock {Text="NIFOG 2.02: nationwide/federal interoperability, medical and SAR references. Hover for mobile RX/TX, tones/NAC and page. Markers do not indicate local activity.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,FontSize=11});
        plan.Children.Add(new TextBlock { Text = "Reference bar position", Margin = new Thickness(0, 8, 0, 4) });
        var planPosition = new ComboBox { ItemsSource = new[] { "Above spectrum", "Below spectrum" } };
        System.Windows.Automation.AutomationProperties.SetName(planPosition, "Reference bar position");
        plan.Children.Add(planPosition);
        refresh.Add(s => planPosition.SelectedIndex = s.BandPlanAtBottom ? 1 : 0);
        planPosition.SelectionChanged += (_, _) => { if (!updating) Publish(settings with { BandPlanAtBottom = planPosition.SelectedIndex == 1 }); };
        plan.Children.Add(new TextBlock { Text = "ARRL: LF through microwave; local plans may differ. Hover to see overlapping uses. Recommendations do not set receiver mode or transmit privileges.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 11 });        plan.Children.Add(new TextBlock { Text = "Selected FCC bands and channel centers; NOAA weather. Hover for source. Region 2 reference, not TX privileges. See BAND-PLAN.md for coverage and source dates.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, FontSize = 11 });
        var level = Group("Display gain / contrast");
        Slider(level, "Display gain", -40, 40, s => s.GainDb, (s, v) => s with { GainDb = v }, " dB");
        Check(level, "Auto spectrum scale", s => s.AutoSpectrum, (s, v) => s with { AutoSpectrum = v });
        Slider(level, "Spectrum top (manual)", -100, 40, s => s.SpectrumTop, (s, v) => s with { SpectrumTop = v }, " dB");
        Slider(level, "Spectrum range (manual)", 20, 140, s => s.SpectrumRange, (s, v) => s with { SpectrumRange = v }, " dB");
        Check(level, "Auto waterfall levels", s => s.AutoWater, (s, v) => s with { AutoWater = v });
        Slider(level, "Waterfall floor (manual)", -140, 10, s => s.WaterLow, (s, v) => s with { WaterLow = v }, " dB");
        Slider(level, "Waterfall ceiling (manual)", -130, 20, s => s.WaterHigh, (s, v) => s with { WaterHigh = v }, " dB");
        var speed = Group("Speed / smoothing");
        Slider(speed, "Visible history (smaller = faster)", 5, 120, s => s.HistorySeconds, (s, v) => s with { HistorySeconds = v }, " s");
        Slider(speed, "Display refresh limit", 2, 30, s => s.RefreshFps, (s, v) => s with { RefreshFps = (int)Math.Round(v) }, " fps");
        Slider(speed, "Spectrum averaging", 0, 5, s => s.AveragingSeconds, (s, v) => s with { AveragingSeconds = v }, " s");
        Check(speed, "Peak hold", s => s.PeakHold, (s, v) => s with { PeakHold = v });
        Button(speed, "Clear held peak", () => ClearPeakRequested?.Invoke());
        var themes = Group("Themes / palettes");
        var theme = new ComboBox { ItemsSource = new[] { "Dark", "Light", "High contrast", "Amber" } }; themes.Children.Add(theme);
        theme.SelectionChanged += (_, _) => { if (theme.SelectedItem is string name) Publish(settings.ColorsFrom(DisplaySettings.Theme(name))); };
        themes.Children.Add(new TextBlock { Text = "Waterfall palette", Margin = new Thickness(0, 8, 0, 4) });
        var palette = new ComboBox { ItemsSource = DisplaySettings.Palettes }; themes.Children.Add(palette);
        refresh.Add(s => palette.SelectedItem = s.Palette);
        palette.SelectionChanged += (_, _) => { if (!updating && palette.SelectedItem is string value) Publish(settings with { Palette = value }); };
        var colors = new StackPanel(); themes.Children.Add(new Expander { Header = "Custom colors (#RRGGBB)", Content = colors, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 0) });
        void Color(string title, Func<DisplaySettings, string> get, Func<DisplaySettings, string, DisplaySettings> set)
        {
            colors.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 7, 0, 3) });
            var input = new TextBox { Padding = new Thickness(5) }; colors.Children.Add(input); refresh.Add(s => { if (!input.IsKeyboardFocused) input.Text = get(s); });
            input.TextChanged += (_, _) =>
            {
                if (updating) return;
                if (System.Text.RegularExpressions.Regex.IsMatch(input.Text, "^#[0-9a-fA-F]{6}$")) { input.Background = Brushes.White; Publish(set(settings, input.Text)); }
                else input.Background = Brushes.MistyRose;
            };
            input.LostKeyboardFocus += (_, _) => { updating = true; input.Text = get(settings); input.Background = Brushes.White; updating = false; };
        }
        Color("Plot background", s => s.Background, (s, v) => s with { Background = v }); Color("Grid", s => s.Grid, (s, v) => s with { Grid = v });
        Color("Text / tick labels", s => s.Text, (s, v) => s with { Text = v }); Color("Spectrum trace", s => s.Trace, (s, v) => s with { Trace = v });
        Color("Held peak", s => s.Peak, (s, v) => s with { Peak = v }); Color("Hover cursor", s => s.Cursor, (s, v) => s with { Cursor = v });
        Color("Tuned frequency", s => s.Tuned, (s, v) => s with { Tuned = v }); Color("Saved markers / labels", s => s.Marker, (s, v) => s with { Marker = v });
        Check(themes, "Use each saved entry’s marker color", s => s.UseEntryColors, (s, v) => s with { UseEntryColors = v });
        var preset = Group("Saved display presets"); preset.Children.Add(presetList);
        var presetName = new TextBox { Padding = new Thickness(5), Margin = new Thickness(0, 8, 0, 0), ToolTip = "Name for your display preset" }; preset.Children.Add(presetName);
        Button(preset, "Save as named preset", () =>
        {
            string name = presetName.Text.Trim(); if (name.Length is < 1 or > 80) { status.Text = "Enter a preset name (1–80 characters)."; return; }
            SavePreset(name);
        });
        Button(preset, "Load selected preset", () => { if (presetList.SelectedItem is string name) LoadPreset(name); });
        Button(preset, "Reset display defaults", () => Publish(new DisplaySettings()));
        body.Children.Add(status);
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); Save(); };
        Reflect(settings);
    }
    public void Initialize(string file)
    {
        path = file;
        try
        {
            if (File.Exists(path))
            {
                var state = JsonSerializer.Deserialize<State>(File.ReadAllText(path)) ?? throw new IOException("Empty display settings file.");
                if (state.Version != 1 || state.Current == null) throw new IOException("Unsupported display settings file.");
                settings = state.Current.Normalize(); presets = new Dictionary<string, DisplaySettings>(StringComparer.OrdinalIgnoreCase);
                if (state.Presets != null) foreach (var item in state.Presets) if (item.Value != null) presets[item.Key] = item.Value.Normalize();
            }
            Reflect(settings); UpdatePresetList(); Changed?.Invoke(settings); status.Text = "Changes preview live and save automatically.";
        }
        catch (Exception ex) { writable = false; status.Text = "Could not load display settings: " + ex.Message + " Original file preserved; preview changes will not be saved."; }
    }
    private void UpdatePresetList() { string? selected = presetList.SelectedItem as string; presetList.ItemsSource = presets.Keys.Order().ToArray(); presetList.SelectedItem = selected; }
    internal void SavePreset(string name) { presets[name] = settings; UpdatePresetList(); presetList.SelectedItem = name; Save(); }
    internal void LoadPreset(string name) { if (presets.TryGetValue(name, out var value)) Publish(value); }
    public void Reflect(DisplaySettings value) { settings = value.Normalize(); updating = true; foreach (var action in refresh) action(settings); updating = false; }
    public void AcceptViewChange(DisplaySettings value) { Reflect(value); saveTimer.Stop(); saveTimer.Start(); }
    private void Publish(DisplaySettings value) { Reflect(value); Changed?.Invoke(settings); saveTimer.Stop(); saveTimer.Start(); }
    public void Save()
    {
        saveTimer.Stop(); if (path.Length == 0 || !writable) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new State(1, settings, presets), new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak", true); else File.Move(path + ".tmp", path);
            status.Text = "Display preferences saved.";
        }
        catch (Exception ex) { status.Text = "Could not save display preferences: " + ex.Message; }
    }
}


