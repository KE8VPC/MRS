using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace MRS;
public sealed record DisplaySettings
{
    public int SnapMode {get;init;}
    public long SnapStepHz {get;init;}=12500;
    public int ViewMode { get; init; } // 0 follow tuned, 1 center lock, 2 free view
    public double SpectrumFraction { get; init; } = .4;
    public bool HidePlans { get; init; }
    public bool ExpandPlans { get; init; }
    public int RfFftSize { get; init; } = 32768;
    public double GainDb { get; init; }
    public double SpectrumTop { get; init; } = 0;
    public double SpectrumRange { get; init; } = 100;
    public double WaterLow { get; init; } = -100;
    public double WaterHigh { get; init; } = -10;
    public bool AutoSpectrum { get; init; }
    public bool AutoWater { get; init; }
    public double HistorySeconds { get; init; } = 30;
    public int RefreshFps { get; init; } = 15;
    public double AveragingSeconds { get; init; } = .15;
    public bool PeakHold { get; init; }
    public bool ShowBandPlan { get; init; }
    public bool ShowChannelMarkers { get; init; } = true;
    public bool ShowArrlPlan { get; init; }
    public bool ShowNifogPlan { get; init; }
    public bool ShowTvPlan { get; init; }
    public bool ShowTvPilots { get; init; }
    public bool BandPlanAtBottom { get; init; }
    public double Zoom { get; init; } = 1;
    public double Pan { get; init; } = .5;
    public bool GradientTrace { get; init; }
    public string Palette { get; init; } = "Blue/cyan";
    public string Background { get; init; } = "#090F19";
    public string Grid { get; init; } = "#263649";
    public string Text { get; init; } = "#B7C9DC";
    public string Trace { get; init; } = "#57DFC2";
    public string Peak { get; init; } = "#ED8DFF";
    public string Cursor { get; init; } = "#FFD700";
    public string Tuned { get; init; } = "#FF9E64";
    public string Marker { get; init; } = "#71CDF5";
    public bool UseEntryColors { get; init; } = true;
    public static readonly string[] Palettes = ["Blue/cyan", "Grayscale", "Heatmap", "High contrast", "Hot signals"];
    public static DisplaySettings Theme(string name) => name switch
    {
        "Hot signals" => new() { Palette = "Hot signals", GradientTrace = true },
        "Light" => new() { Background = "#F4F7FA", Grid = "#B9C6D1", Text = "#142B40", Trace = "#006355", Peak = "#A12091", Cursor = "#8A5200", Tuned = "#C33200", Marker = "#164BB0", UseEntryColors = false },
        "High contrast" => new() { Background = "#000000", Grid = "#777777", Text = "#FFFFFF", Trace = "#FFFF00", Peak = "#FF77FF", Cursor = "#FFFFFF", Tuned = "#00FFFF", Marker = "#00FF66", Palette = "High contrast", UseEntryColors = false },
        "Amber" => new() { Background = "#140E06", Grid = "#4C3920", Text = "#EED7AE", Trace = "#FFC04D", Peak = "#FFF4CC", Cursor = "#FFFFFF", Tuned = "#FF6F40", Marker = "#E2B66E", Palette = "Heatmap", UseEntryColors = false },
        _ => new()
    };
    public DisplaySettings ColorsFrom(DisplaySettings theme) => this with { Background = theme.Background, Grid = theme.Grid, Text = theme.Text, Trace = theme.Trace, Peak = theme.Peak, Cursor = theme.Cursor, Tuned = theme.Tuned, Marker = theme.Marker, UseEntryColors = theme.UseEntryColors, Palette = theme.Palette, GradientTrace = theme.GradientTrace };
    public DisplaySettings Normalize()
    {
        double Clamp(double v, double min, double max, double fallback) => double.IsFinite(v) ? Math.Clamp(v, min, max) : fallback;
        string Color(string value, string fallback) => value != null && Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$") ? value : fallback;
        double low = Clamp(WaterLow, -140, 10, -100), high = Clamp(WaterHigh, -130, 20, -10);
        if (high < low + 5) high = low + 5;
        return this with { ViewMode = Math.Clamp(ViewMode,0,2), ShowTvPilots = false, RfFftSize = RfSpectrumAnalyzer.Sizes.Contains(RfFftSize) ? RfFftSize : 32768, GainDb = Clamp(GainDb, -40, 40, 0), SpectrumTop = Clamp(SpectrumTop, -100, 40, 0), SpectrumRange = Clamp(SpectrumRange, 20, 140, 100), WaterLow = low, WaterHigh = high,
            SpectrumFraction = Clamp(SpectrumFraction,.15,.8,.4), HistorySeconds = Clamp(HistorySeconds, 5, 120, 30), RefreshFps = Math.Clamp(RefreshFps, 2, 30), AveragingSeconds = Clamp(AveragingSeconds, 0, 5, .15), Zoom = Clamp(Zoom, 1, 4096, 1), Pan = Clamp(Pan, 0, 1, .5),
            Palette = Palettes.Contains(Palette) ? Palette : Palettes[0], Background = Color(Background, "#090F19"), Grid = Color(Grid, "#263649"), Text = Color(Text, "#B7C9DC"), Trace = Color(Trace, "#57DFC2"), Peak = Color(Peak, "#ED8DFF"), Cursor = Color(Cursor, "#FFD700"), Tuned = Color(Tuned, "#FF9E64"), Marker = Color(Marker, "#71CDF5") };
    }
    public static double Clock => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
}
internal readonly record struct FrequencyViewport(double CaptureStart, double CaptureSpan, double Zoom, double Pan)
{
    public double Span => CaptureSpan / Zoom;
    public double Start => CaptureStart + (CaptureSpan - Span) * Pan;
    public double Frequency(double fraction) => Start + Math.Clamp(fraction, 0, 1) * Span;
    public double Fraction(double frequency) => (frequency - Start) / Span;
    public FrequencyViewport CenterOn(double frequency) => this with { Pan = Zoom <= 1 ? .5 : Math.Clamp((frequency - Span / 2 - CaptureStart) / (CaptureSpan - Span), 0, 1) };
    public FrequencyViewport Drag(double fraction) => CenterOn(Start + Span / 2 - fraction * Span);
    public FrequencyViewport ZoomAt(double factor, double fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        double target = Frequency(fraction), zoom = Math.Clamp(factor, 1, 4096), width = CaptureSpan / zoom;
        double start = Math.Clamp(target - fraction * width, CaptureStart, CaptureStart + CaptureSpan - width);
        return this with { Zoom = zoom, Pan = zoom == 1 ? .5 : (start - CaptureStart) / (CaptureSpan - width) };
    }
}
internal sealed class WaterfallHistory
{
    internal sealed class Row
    {
        internal double CaptureStart { get; }
        internal double CaptureSpan { get; }
        internal double Time { get; }
        internal byte[] Levels { get; }
        internal float Value(int i) => Levels[i] - 160;
        internal Row(double time,float[] values, double captureStart=0, double captureSpan=1) { CaptureStart=captureStart; CaptureSpan=captureSpan; Time=time; Levels=values.Select(v=>(byte)Math.Clamp((int)Math.Round(v)+160,0,255)).ToArray(); }
    }
    private readonly Queue<Row> rows = new();
    private double lastTime = double.NegativeInfinity;
    private long retainedBytes;
    internal long RetainedBytes => retainedBytes;
    public void Add(float[] values, double time, double captureStart=0, double captureSpan=1)
    {
        if (time < lastTime) return;
        lastTime = time;
        var row=new Row(time,values,captureStart,captureSpan);rows.Enqueue(row);retainedBytes+=row.Levels.Length;
        while (rows.Count > 6000 || retainedBytes > 256L*1024*1024 || rows.Count > 0 && time - rows.Peek().Time > 125) retainedBytes-=rows.Dequeue().Levels.Length;
    }
    public Row[] Snapshot() => rows.ToArray();
    public void Clear() { rows.Clear(); retainedBytes=0; lastTime = double.NegativeInfinity; }
    internal static int RowAt(Row[] values, double target)
    {
        int low = 0, high = values.Length - 1;
        while (low <= high) { int mid = (low + high) / 2; if (values[mid].Time <= target) low = mid + 1; else high = mid - 1; }
        return high >= 0 && target - values[high].Time <= .25 ? high : -1;
    }
}
internal static class DisplayPalette
{
    public static Color Sample(string palette, double value)
    {
        double t = Math.Clamp(value, 0, 1);
        Color[] stops = palette switch
        {
            "Hot signals" => [Colors.Black, Colors.DarkBlue, Colors.Cyan, Colors.Yellow, Colors.Red, Colors.White],
            "Grayscale" => [Colors.Black, Colors.White],
            "Heatmap" => [Color.FromRgb(9, 4, 24), Color.FromRgb(105, 16, 93), Color.FromRgb(222, 55, 47), Color.FromRgb(255, 166, 39), Color.FromRgb(255, 255, 210)],
            "High contrast" => [Colors.Black, Colors.Blue, Colors.Cyan, Colors.Yellow, Colors.White],
            _ => [Color.FromRgb(3, 9, 24), Color.FromRgb(15, 53, 137), Color.FromRgb(0, 192, 215), Color.FromRgb(226, 255, 246)]
        };
        double scaled = t * (stops.Length - 1); int i = Math.Min((int)scaled, stops.Length - 2); double mix = scaled - i;
        byte Blend(byte a, byte b) => (byte)Math.Round(a + (b - a) * mix);
        return Color.FromRgb(Blend(stops[i].R, stops[i + 1].R), Blend(stops[i].G, stops[i + 1].G), Blend(stops[i].B, stops[i + 1].B));
    }
}

