using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MRS;

internal sealed class WeakAudioWaterfall : FrameworkElement
{
    internal const int MaximumHz = 3000;
    private const int Columns = 512;
    private const int Rows = 96;
    private const double PlotTop = 52;
    private readonly byte[] pixels = new byte[Columns * Rows];
    private readonly WriteableBitmap history = new(Columns, Rows, 96, 96, PixelFormats.Indexed8,
        new BitmapPalette(Enumerable.Range(0, 256).Select(i => DisplayPalette.Sample("Hot signals", i / 255.0)).ToList()));
    private readonly double bandwidth;
    private int? txHz;
    private double? rxHz;
    private bool hasAudio;
    internal bool CanSelectTransmit { get; set; } = true;
    internal event Action<int>? TransmitSelected;

    internal WeakAudioWaterfall(string mode)
    {
        bandwidth = mode == "FT4" ? 4 / .048 : 8 / .16;
        Height = 188;
        ToolTip = "Click between 300 and 2500 Hz to set TX audio offset before arming.";
    }

    internal void SetMarkers(int? transmitHz, double? receiveHz)
    {
        txHz = transmitHz;
        rxHz = receiveHz;
        InvalidateVisual();
    }

    internal void Append(float[] levels)
    {
        // The weak-signal decoder supplies 12 kHz audio, so a 2048-point FFT
        // resolves the passband at approximately 5.9 Hz per bin.
        int visibleBins = levels.Length / 2;
        var sorted = levels.Take(visibleBins).OrderBy(level => level).ToArray();
        float floor = sorted[sorted.Length / 2] - 4;
        Buffer.BlockCopy(pixels, 0, pixels, Columns, pixels.Length - Columns);
        for (int x = 0; x < Columns; x++)
        {
            float level = levels[x * visibleBins / Columns];
            pixels[x] = (byte)Math.Clamp((level - floor) * 255 / 34, 0, 255);
        }
        history.WritePixels(new Int32Rect(0, 0, Columns, Rows), pixels, Columns, 0);
        hasAudio = true;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Point point = e.GetPosition(this);
        if (!CanSelectTransmit || point.Y < 19 || ActualWidth <= 0) return;
        int offset = (int)Math.Round(point.X * MaximumHz / ActualWidth);
        if (offset is < 300 or > 2500) return;
        TransmitSelected?.Invoke(offset);
        e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth <= 0 || ActualHeight <= PlotTop) return;
        dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, ActualWidth, ActualHeight));
        dc.DrawImage(history, new Rect(0, PlotTop, ActualWidth, ActualHeight - PlotTop));
        for (int hz = 0; hz < MaximumHz; hz += 500)
        {
            double x = X(hz);
            dc.DrawLine(new Pen(Brushes.SlateGray, 1), new Point(x, 16), new Point(x, 20));
            DrawText(dc, hz.ToString(CultureInfo.InvariantCulture), x + 2, 1, Brushes.LightGray);
        }
        DrawText(dc, "3000 Hz", Math.Max(0, ActualWidth - 47), 1, Brushes.LightGray);
        DrawMarker(dc, txHz, 25, "TX", Brushes.Salmon);
        DrawMarker(dc, rxHz, 40, "RX", Brushes.LightGreen);
        if (!hasAudio) DrawText(dc, "Waiting for live USB audio", 8, PlotTop + 8, Brushes.LightGray);
    }

    private double X(double hz) => hz * ActualWidth / MaximumHz;

    private void DrawMarker(DrawingContext dc, double? hz, double y, string label, Brush brush)
    {
        if (hz is not double offset || offset is < 0 or > MaximumHz) return;
        double start = X(offset), end = X(Math.Min(MaximumHz, offset + bandwidth));
        var pen = new Pen(brush, 2);
        dc.DrawLine(pen, new Point(start, y), new Point(end, y));
        dc.DrawLine(pen, new Point(start, y), new Point(start, y + 5));
        dc.DrawLine(pen, new Point(end, y), new Point(end, y + 5));
        DrawText(dc, $"{label} {offset:0}", Math.Clamp(end + 4, 0, Math.Max(0, ActualWidth - 62)), y - 5, brush);
    }

    private void DrawText(DrawingContext dc, string text, double x, double y, Brush color) =>
        dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 11, color, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
}
