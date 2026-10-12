using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MRS;
public sealed class SpectrumView : FrameworkElement
{
    private readonly WaterfallHistory history = new();
    private float[]? current, peak;
    private Point? pointer;
    private Point? dragOrigin;
    private FrequencyViewport dragView;
    private bool dragged;
    private double captureStart, captureSpan = 24000, usableSpan = 24000, lastFrame, lastRender;
    private ChannelSettings? channel;
    private double autoLow = -100, autoHigh = 0;
    private bool rf;
    // Preserve room for the plot, axis and waterfall in compact receiver windows.
    // Settings remain enabled; reference rows return when the panel grows.
    private double RequestedPlanHeight => (settings.ShowBandPlan ? 44 : 0) + (settings.ShowArrlPlan ? 62 : 0) + (settings.ShowNifogPlan ? 44 : 0) + (settings.ShowTvPlan ? 44 : 0);
    private double PlanHeight => rf && ActualHeight >= Math.Max(240,RequestedPlanHeight+140) ? RequestedPlanHeight : 0;
    private double MpxReferenceHeight => !rf && basebandName == "MPX" ? 42 : 0;
    private double PlotHeight => Math.Max(20, ActualHeight * .38 - PlanHeight - MpxReferenceHeight);
    private double PlotTop => 16 + MpxReferenceHeight + (settings.BandPlanAtBottom ? 0 : PlanHeight);
    private double PlanTop => settings.BandPlanAtBottom ? 16 + PlotHeight + 38 : 16;
    private double WaterTop => 16 + PlotHeight + 38 + PlanHeight + MpxReferenceHeight;
    private bool InPlan(Point p) => PlanHeight > 0 && p.Y >= PlanTop && p.Y < PlanTop + PlanHeight;
    private string basebandName = "Audio";
    private string receiveMode = "NFM";
    internal void SetReceiveMode(string mode) { receiveMode = mode; InvalidateVisual(); }
    private bool canTune = true;
    private DisplaySettings settings = new();
    private IReadOnlyList<FrequencyItem> markers = [];
    private double UsableStart => captureStart + (captureSpan - usableSpan) / 2;
    private double DisplayStart => rf ? Math.Max(0, UsableStart) : UsableStart;
    private FrequencyViewport View => new(DisplayStart, FullSpan, settings.Zoom, settings.Pan);
    private double Start => View.Start;
    private double Span => View.Span;
    public DisplaySettings Settings => settings;
    internal double VisibleSpan => Span;
    internal int BinCount => current?.Length ?? 0;
    internal double VisibleCenter => Start + Span / 2;
    internal double CaptureCenter => captureStart + captureSpan / 2;
    internal double FullSpan => Math.Max(1, UsableStart + usableSpan - DisplayStart);
    internal double TunedCenter => channel?.Frequency ?? CaptureCenter;
    internal void SetChannel(ChannelSettings value) { channel = value; receiveMode = value.Mode; InvalidateVisual(); }
    internal bool IsRf => rf;
    internal void CenterView() { var view = View.CenterOn(TunedCenter); Configure(settings with { Pan = view.Pan }); }
    public event Action<DisplaySettings>? SettingsChanged;
    public event Action<long>? SaveFrequencyRequested;
    public event Action<Guid>? MarkerSelected;
    public event Action<long>? TuneRequested;
    public event Action<double>? CaptureSpanChanged;
    private static SolidColorBrush ColorBrush(string color) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }
    internal void SetMarkers(IEnumerable<FrequencyItem> values) { markers = values.ToArray(); InvalidateVisual(); }
    public void Configure(DisplaySettings value)
    {
        var next = value.Normalize();
        if (next.PeakHold != settings.PeakHold) peak = current == null ? null : (float[])current.Clone();
        settings = next; InvalidateVisual(); SettingsChanged?.Invoke(settings);
    }
    public void ResetPeak() { peak = current == null ? null : (float[])current.Clone(); InvalidateVisual(); }
    public void ResetView() => Configure(settings with { Zoom = 1, Pan = .5 });
    internal void SetRf(double center, int rate = RtlNative.SampleRate, bool allowTuning = true, double usableBandwidth = 0)
    {
        if (!double.IsFinite(center) || rate <= 0 || !double.IsFinite(usableBandwidth) || usableBandwidth < 0 || usableBandwidth > rate) throw new ArgumentOutOfRangeException(nameof(usableBandwidth));
        rf = true; canTune = allowTuning; captureStart = center - rate / 2.0; captureSpan = rate; usableSpan = usableBandwidth == 0 ? rate : usableBandwidth;
        channel = null; Clear(); CaptureSpanChanged?.Invoke(FullSpan);
    }
    internal void SetAudio() { rf = false; basebandName = "Audio"; channel = null; captureStart = 0; captureSpan = usableSpan = AudioEngine.Rate / 2.0; Clear(); CaptureSpanChanged?.Invoke(captureSpan); }
    internal void SetBaseband(int rate, string name = "Audio") { SetAudio(); basebandName = name; captureSpan = usableSpan = rate / 2.0; CaptureSpanChanged?.Invoke(captureSpan); }
    private double? FrequencyAt(Point p)
    {
        if (ActualWidth < 100 || ActualHeight < 140 || p.X < 52 || p.X > ActualWidth - 20) return null;
        double bottom = PlotTop + PlotHeight;
        if (!(InPlan(p) || p.Y >= PlotTop && p.Y <= bottom || p.Y >= WaterTop && p.Y <= ActualHeight - 46)) return null;
        return View.Frequency((p.X - 52) / (ActualWidth - 72));
    }
    public SpectrumView()
    {
        Focusable = true;
        ToolTipService.SetShowDuration(this,30000);
        MouseMove += (_, e) => { var point = e.GetPosition(this); ContinueDrag(point); SetPointer(point); };
        MouseLeftButtonDown += (_, e) => { if (FrequencyAt(e.GetPosition(this)) == null) return; BeginDrag(e.GetPosition(this)); CaptureMouse(); Focus(); e.Handled = true; };
        LostMouseCapture += (_, _) => dragOrigin = null;
        MouseLeave += (_, _) => SetPointer(null);
        MouseWheel += (_, e) =>
        {
            var p = e.GetPosition(this); if (FrequencyAt(p) == null) return;
            if(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)&&InPlan(p)&&ToolTip is ReferenceToolTip {IsOpen:true} detail)
            {detail.Scroll(e.Delta);e.Handled=true;return;}
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) Configure(settings with { Pan = settings.Pan + (e.Delta > 0 ? -.08 : .08) });
            else { var zoom = View.ZoomAt(settings.Zoom * (e.Delta > 0 ? 1.5 : 1 / 1.5), (p.X - 52) / (ActualWidth - 72)); Configure(settings with { Zoom = zoom.Zoom, Pan = zoom.Pan }); }
            e.Handled = true;
        };
        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Add: case Key.OemPlus: CenterZoom(1.5); break;
                case Key.Subtract: case Key.OemMinus: CenterZoom(1 / 1.5); break;
                case Key.Left: Configure(settings with { Pan = settings.Pan - .05 }); break;
                case Key.Right: Configure(settings with { Pan = settings.Pan + .05 }); break;
                case Key.Home: ResetView(); break;
                default: return;
            }
            e.Handled = true;
        };
        MouseRightButtonUp += (_, e) =>
        {
            if (!rf || FrequencyAt(e.GetPosition(this)) is not { } hz) return;
            var menu = new ContextMenu(); var save = new MenuItem { Header = $"Save {hz / 1000000:F6} MHz…" };
            save.Click += (_, _) => SaveFrequencyRequested?.Invoke((long)Math.Round(hz)); menu.Items.Add(save);
            menu.PlacementTarget = this; menu.IsOpen = true; e.Handled = true;
        };
        MouseLeftButtonUp += (_, e) =>
        {
            Focus();
            EndDrag(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Control)); ReleaseMouseCapture(); e.Handled = true;
        };
        SizeChanged += (_, _) => { pointer = IsMouseOver ? Mouse.GetPosition(this) : null; InvalidateVisual(); };
    }
    private bool InView(double frequency) => frequency >= Start && frequency <= Start + Span;
    internal void BeginDrag(Point point) { dragOrigin = point; dragView = View; dragged = false; }
    internal void ContinueDrag(Point point)
    {
        if (dragOrigin is not { } origin) return;
        if (Math.Abs(point.X - origin.X) >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(point.Y - origin.Y) >= SystemParameters.MinimumVerticalDragDistance) dragged = true;
        if (!dragged || ActualWidth <= 72) return;
        var next = dragView.Drag((point.X - origin.X) / (ActualWidth - 72));
        Configure(settings with { Pan = next.Pan });
    }
    internal void EndDrag(Point point, bool selectMarker = false)
    {
        if (dragOrigin == null) return;
        ContinueDrag(point); dragOrigin = null;
        if (!dragged) ClickAt(point, selectMarker);
    }
    internal void ClickAt(Point point, bool selectMarker = false)
    {
        if (!rf || FrequencyAt(point) is not { } hz) return;
        if (!selectMarker) { if (canTune) TuneRequested?.Invoke((long)Math.Round(hz)); return; }
        var nearest = markers.Where(m => InView(m.FrequencyHz)).MinBy(m => Math.Abs(m.FrequencyHz - hz));
        if (nearest != null && Math.Abs(nearest.FrequencyHz - hz) / Span * (ActualWidth - 72) <= 6) MarkerSelected?.Invoke(nearest.Id);
    }
    private void CenterZoom(double factor) { var view = View.ZoomAt(settings.Zoom * factor, .5); Configure(settings with { Zoom = view.Zoom, Pan = view.Pan }); }
    internal void SetPointer(Point? position)
    {
        pointer = position;
        ToolTip = null;
        if (rf && PlanHeight > 0 && position is { } p && InPlan(p) && FrequencyAt(p) is { } hz)
        {
            var lines = (settings.ShowBandPlan ? UsBandPlan.VisibleBands(hz, hz + 1) : []).Select(b => $"{b.Name}: {b.Low/1000000.0:0.######}–{b.High/1000000.0:0.######} MHz\n{b.Source}").ToList();
            var nearest = (settings.ShowBandPlan && settings.ShowChannelMarkers ? UsBandPlan.Channels : []).Where(c => InView(c.Frequency)).OrderBy(c => Math.Abs(c.Frequency-hz)).FirstOrDefault();
            if (nearest != null && Math.Abs(nearest.Frequency-hz)/Span*(ActualWidth-72) <= 8)
                lines.Add($"{nearest.Name}: {nearest.Frequency/1000000.0:0.######} MHz (center)\n{nearest.Source}");
            if(settings.ShowArrlPlan)
                foreach(var entry in ArrlPlan.Visible(hz-Span*8/(ActualWidth-72),hz+Span*8/(ActualWidth-72)).Where(x=>x.IsPoint || (x.Low<=hz && x.High>hz)))
                    lines.Add($"ARRL {entry.Band}: {entry.Label}\n{(entry.IsPoint ? $"{entry.Low/1000000.0:0.######} MHz (marker)" : $"{entry.Low/1000000.0:0.######}–{entry.High/1000000.0:0.######} MHz")}\n{entry.Note}");
            if(settings.ShowArrlPlan) lines.Add(ArrlPlan.Source);
            if(settings.ShowNifogPlan)
            {
                var nifog=NifogPlan.Visible(Start,Start+Span).MinBy(c=>Math.Abs(c.Frequency-hz));
                if(nifog!=null && Math.Abs(nifog.Frequency-hz)/Span*(ActualWidth-72)<=8)lines.Add(nifog.Source);
            }
            if(settings.ShowTvPlan)
                foreach(var tv in TvBandPlan.Visible(hz,hz+1))lines.Add(tv.Details(settings.ShowTvPilots));
            string detail=lines.Count > 0 ? string.Join("\n",lines) : "Reference layers · no listed entry here\nPublished reference, not local activity or transmit authorization.";
            ToolTip=settings.ShowNifogPlan?new ReferenceToolTip(detail):detail;
        }
        InvalidateVisual();
    }
    public void Push(float[] frame) => PushAt(frame, DisplaySettings.Clock);
    internal void PushAt(float[] frame, double timestamp)
    {
        if ((frame.Length != 1024 && !RfSpectrumAnalyzer.Sizes.Contains(frame.Length)) || frame.Any(v => !float.IsFinite(v)) || timestamp < lastFrame) return;
        if (current != null && current.Length != frame.Length) Clear();
        double dt = Math.Max(0, timestamp - lastFrame);
        if (current == null) current = (float[])frame.Clone();
        else
        {
            double alpha = settings.AveragingSeconds <= 0 ? 1 : 1 - Math.Exp(-dt / settings.AveragingSeconds);
            for (int i = 0; i < frame.Length; i++) current[i] += (float)(alpha * (frame[i] - current[i]));
        }
        peak ??= (float[])frame.Clone();
        for (int i = 0; i < frame.Length; i++) peak[i] = Math.Max(peak[i], frame[i]);
        if (settings.AutoSpectrum || settings.AutoWater)
        {
        var sorted = frame.Order().ToArray();
        double low = sorted[sorted.Length / 5] - 8, high = Math.Max(low + 30, sorted[^1] + 6);
        double smoothing = lastFrame == 0 ? 1 : 1 - Math.Exp(-dt / .5);
        autoLow += smoothing * (low - autoLow); autoHigh += smoothing * (high - autoHigh);
        }
        lastFrame = timestamp; history.Add(frame, timestamp); Tick();
    }
    public void Tick()
    {
        if (current == null) return;
        double now = DisplaySettings.Clock;
        if (now - lastRender >= 1.0 / settings.RefreshFps) { lastRender = now; InvalidateVisual(); }
    }
    public void Clear() { history.Clear(); current = peak = null; lastFrame = 0; autoLow = -100; autoHigh = 0; InvalidateVisual(); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double width = ActualWidth, height = ActualHeight;
        if (width < 100 || height < 140) return;
        var background = ColorBrush(settings.Background); var textBrush = ColorBrush(settings.Text);
        dc.DrawRectangle(background, null, new Rect(0, 0, width, height));
        double left = 52, plotWidth = width - 72, top = PlotTop, plotHeight = PlotHeight;
        if (PlanHeight > 0)
        {
            double rowTop=PlanTop;
            if(settings.ShowBandPlan){DrawBandPlan(dc,left,plotWidth,rowTop);rowTop+=44;}
            if(settings.ShowArrlPlan){DrawArrlPlan(dc,left,plotWidth,rowTop);rowTop+=62;}
            if(settings.ShowNifogPlan){DrawNifogPlan(dc,left,plotWidth,rowTop);rowTop+=44;}
            if(settings.ShowTvPlan)DrawTvPlan(dc,left,plotWidth,rowTop);
        }
        double waterTop = WaterTop, waterHeight = Math.Max(1, height - waterTop - 46);
        double levelTop = settings.AutoSpectrum ? autoHigh : settings.SpectrumTop;
        double levelRange = settings.AutoSpectrum ? Math.Max(20, autoHigh - autoLow) : settings.SpectrumRange;
        var gridPen = new Pen(ColorBrush(settings.Grid), 1);
        for (int tick = 0; tick <= 5; tick++)
        {
            double y = top + tick / 5.0 * plotHeight;
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotWidth, y));
            Label(dc, (levelTop - tick / 5.0 * levelRange).ToString("0", CultureInfo.InvariantCulture), 7, y - 7);
        }
        for (int tick = 0; tick <= 6; tick++)
        {
            double x = left + tick / 6.0 * plotWidth, frequency = View.Frequency(tick / 6.0);
            dc.DrawLine(gridPen, new Point(x, top), new Point(x, top + plotHeight));
            string label = rf ? (frequency / 1000000).ToString(Span < 40000 ? "F4" : "F3", CultureInfo.InvariantCulture) : (frequency / 1000).ToString("0.##", CultureInfo.InvariantCulture) + "k";
            var formatted = Text(label, 10, textBrush);
            dc.DrawText(formatted, new Point(Math.Clamp(x - formatted.Width / 2, 0, width - formatted.Width), top + plotHeight + 6));
        }
        void Trace(float[] data, string color, double thickness)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                int pixels = Math.Max(2, (int)plotWidth);
                for (int x = 0; x <= pixels; x++)
                {
                    float value = BinValue(data, x / (double)pixels, 1.0 / pixels);
                    var point = new Point(left + x * plotWidth / pixels, top + Math.Clamp((levelTop - value - settings.GainDb) / levelRange, 0, 1) * plotHeight);
                    if (x == 0) context.BeginFigure(point, false, false); else context.LineTo(point, true, false);
                }
            }
            geometry.Freeze(); dc.DrawGeometry(null, new Pen(ColorBrush(color), thickness), geometry);
        }
        if (settings.PeakHold && peak != null) Trace(peak, settings.Peak, .8);
        if (current != null) Trace(current, settings.Trace, 1.2);
        DrawWaterfall(dc, new Rect(left, waterTop, plotWidth, waterHeight));
        for (int tick = 0; tick <= 4; tick++) Label(dc, (settings.HistorySeconds * tick / 4).ToString("0.#", CultureInfo.InvariantCulture) + "s", 6, waterTop + waterHeight * tick / 4 - 6);
        if (current == null) Label(dc, rf ? "Waiting for receiver data…" : "Start a source or load a test tone", left + 12, waterTop + 25);
        DrawMarkers(dc, left, plotWidth, top, plotHeight, waterTop, height - 46);
        if(MpxReferenceHeight>0) DrawMpxReferences(dc,left,plotWidth,top,plotHeight,waterTop,waterTop+waterHeight);
        DrawPointer(dc, left, plotWidth, top, plotHeight, waterTop, height - 46);
        string units = rf ? "RF MHz · relative display dB" : basebandName == "MPX" ? "MPX kHz · relative display dB" : "Audio kHz · display dBFS";
        Label(dc, $"{units} · {settings.Zoom:0.#}× zoom{(rf && current != null ? $" · {captureSpan/current.Length:0.##} Hz/bin · {current.Length:N0} bins" : "")} · {settings.HistorySeconds:0.#}s history{(MpxReferenceHeight>0 ? " · Markers: reference positions, not detections" : "")}", left, height - 30);
        Label(dc, rf ? (canTune ? "Click: tune  |  Drag: pan  |  Ctrl+click: marker  |  Wheel: zoom" : "I/Q replay: recorded coverage  |  Drag: pan  |  Wheel: zoom") : "Drag: pan  |  Wheel: zoom  |  Home: full span", left, height - 16);
    }
    private void DrawMpxReferences(DrawingContext dc,double left,double width,double top,double plotHeight,double waterTop,double bottom)
    {
        double X(double hz)=>left+(hz-Start)/Span*width;
        var brush=ColorBrush(settings.Text);
        foreach(var band in new[]{(Low:0d,High:15000d,Name:"L+R audio · 0–15 kHz"),(Low:23000d,High:53000d,Name:"L−R stereo · 23–53 kHz")})
        {
            double a=Math.Max(left,X(band.Low)),b=Math.Min(left+width,X(band.High));
            if(b<=a)continue;
            var pen=new Pen(brush,1);
            dc.DrawLine(pen,new Point(a,31),new Point(b,31));
            dc.DrawLine(pen,new Point(a,28),new Point(a,34));dc.DrawLine(pen,new Point(b,28),new Point(b,34));
            var label=Text(band.Name,10,brush);
            if(label.Width+6<b-a)dc.DrawText(label,new Point((a+b-label.Width)/2,15));
        }
        double lastRight=left-4;
        foreach(var marker in new[]{(Hz:19000d,Name:"19k · Stereo pilot"),(Hz:38000d,Name:"38k · L−R center"),(Hz:57000d,Name:"57k · RDS/RBDS")})
        {
            if(!InView(marker.Hz))continue;
            double x=X(marker.Hz);var pen=new Pen(brush,.8){DashStyle=DashStyles.Dash};
            dc.DrawLine(pen,new Point(x,top),new Point(x,top+plotHeight));
            dc.PushOpacity(.45);dc.DrawLine(pen,new Point(x,waterTop),new Point(x,bottom));dc.Pop();
            var label=Text(marker.Name,10,brush);double lx=Math.Clamp(x-label.Width/2,left,Math.Max(left,left+width-label.Width));
            if(lx>=lastRight+4 && label.Width<=width){dc.DrawText(label,new Point(lx,37));lastRight=lx+label.Width;}
        }
    }
    private void DrawBandPlan(DrawingContext dc, double left, double width, double top)
    {
        var bounds = new Rect(left,top,width,40);
        dc.PushClip(new RectangleGeometry(bounds));
        dc.DrawRectangle(ColorBrush(settings.Background),new Pen(ColorBrush(settings.Grid),1),bounds);
        bool any = false;
        foreach (var band in UsBandPlan.VisibleBands(Start,Start+Span))
        {
            any = true;
            double x=left+View.Fraction(Math.Max(Start,band.Low))*width;
            double right=left+View.Fraction(Math.Min(Start+Span,band.High))*width;
            var fill=ColorBrush(settings.Marker).Clone(); fill.Opacity=.22;
            var rect=new Rect(x,top,Math.Max(0,right-x),20);
            dc.DrawRectangle(fill,new Pen(ColorBrush(settings.Marker),1),rect);
            dc.PushClip(new RectangleGeometry(rect));
            DrawPlanLabel(dc,band.Name,rect,11); dc.Pop();
        }
        double occupied=left;
        foreach(var entry in settings.ShowChannelMarkers ? UsBandPlan.VisibleChannels(Start,Start+Span) : [])
        {
            any=true;
            double x=left+View.Fraction(entry.Frequency)*width;
            dc.DrawLine(new Pen(ColorBrush(settings.Marker),1),new Point(x,top+21),new Point(x,top+39));
            var label=Text(entry.Name,10,ColorBrush(settings.Text));
            if(x+3>=occupied && x+label.Width+5<=left+width)
            {
                dc.DrawText(label,new Point(x+3,top+22)); occupied=x+label.Width+9;
            }
        }
        if(!any) dc.DrawText(Text("U.S. reference · no listed band/channel here",10,ColorBrush(settings.Text)),new Point(left+4,top+2));
        dc.Pop();
    }
    private void DrawTvPlan(DrawingContext dc,double left,double width,double top)
    {
        var bounds=new Rect(left,top,width,40);
        dc.PushClip(new RectangleGeometry(bounds));
        dc.DrawRectangle(ColorBrush(settings.Background),new Pen(ColorBrush(settings.Grid),1),bounds);
        var entries=TvBandPlan.Visible(Start,Start+Span).ToArray();
        foreach(var entry in entries)
        {
            double x=left+View.Fraction(Math.Max(Start,entry.Low))*width;
            double right=left+View.Fraction(Math.Min(Start+Span,entry.High))*width;
            var rect=new Rect(x,top,Math.Max(0,right-x),19);
            var fill=ColorBrush(settings.Marker).Clone();fill.Opacity=.18;
            dc.DrawRectangle(fill,new Pen(ColorBrush(settings.Marker),1),rect);
            dc.PushClip(new RectangleGeometry(rect));
            DrawPlanLabel(dc,$"TV RF {entry.Number} · {entry.Low/1e6:0.###}–{entry.High/1e6:0.###} MHz",rect,10);
            dc.Pop();
        }
        double occupied=left;
        var ticks=entries.SelectMany(c=>settings.ShowTvPilots
            ? new[]{(Hz:c.Pilot,Label:$"1.0 pilot {c.Pilot/1e6:0.######}",Pilot:true),(Hz:(double)c.Center,Label:$"Center {c.Center/1e6:0.###}",Pilot:false)}
            : new[]{(Hz:(double)c.Center,Label:$"Center {c.Center/1e6:0.###}",Pilot:false)}).OrderBy(t=>t.Hz);
        foreach(var tick in ticks.Where(t=>InView(t.Hz)))
        {
            double x=left+View.Fraction(tick.Hz)*width;
            dc.DrawLine(new Pen(ColorBrush(tick.Pilot?settings.Peak:settings.Marker),1),new Point(x,top+20),new Point(x,top+39));
            var label=Text(tick.Label,10,ColorBrush(settings.Text));
            if(x+3>=occupied && x+label.Width+5<=left+width)
            {dc.DrawText(label,new Point(x+3,top+22));occupied=x+label.Width+9;}
        }
        if(entries.Length==0)DrawPlanLabel(dc,"TV RF 2–36 · no channel allocation here",bounds,10);
        dc.Pop();
    }
    private void DrawNifogPlan(DrawingContext dc,double left,double width,double top)
    {
        var bounds=new Rect(left,top,width,40);
        dc.PushClip(new RectangleGeometry(bounds));
        dc.DrawRectangle(ColorBrush(settings.Background),new Pen(ColorBrush(settings.Grid),1),bounds);
        var entries=NifogPlan.Visible(Start,Start+Span).ToArray();
        DrawPlanLabel(dc,entries.Length==0?"CISA NIFOG · no listed channel here":$"CISA NIFOG 2.02 · {entries.Length} RF centers · hover for details · Alt+wheel scrolls details",new Rect(left,top,width,20),10);
        double occupied=left;
        foreach(var entry in entries)
        {
            double x=left+View.Fraction(entry.Frequency)*width;
            dc.DrawLine(new Pen(ColorBrush(settings.Tuned),1),new Point(x,top+21),new Point(x,top+39));
            var label=Text(entry.Name,10,ColorBrush(settings.Text));
            if(x+3>=occupied && x+label.Width+5<=left+width)
            {dc.DrawText(label,new Point(x+3,top+22));occupied=x+label.Width+9;}
        }
        dc.Pop();
    }
    private void DrawArrlPlan(DrawingContext dc,double left,double width,double top)
    {
        dc.PushClip(new RectangleGeometry(new Rect(left,top,width,60)));
        dc.DrawRectangle(ColorBrush(settings.Background),new Pen(ColorBrush(settings.Grid),1),new Rect(left,top,width,60));
        var ends=new double[]{double.NegativeInfinity,double.NegativeInfinity};
        var entries=ArrlPlan.Visible(Start,Start+Span).ToArray();
        foreach(var entry in entries.Where(x=>!x.IsPoint))
        {
            int lane=Array.FindIndex(ends,x=>x<=entry.Low); if(lane<0)continue;
            ends[lane]=entry.High;
            double x=left+View.Fraction(Math.Max(Start,entry.Low))*width;
            double right=left+View.Fraction(Math.Min(Start+Span,entry.High))*width;
            var rect=new Rect(x,top+lane*20,Math.Max(0,right-x),19);
            var brush=ColorBrush(settings.Peak).Clone();brush.Opacity=.2;
            dc.DrawRectangle(brush,new Pen(ColorBrush(settings.Peak),.7),rect);
            dc.PushClip(new RectangleGeometry(rect));
            DrawPlanLabel(dc,entry.Label,rect,10);dc.Pop();
        }
        double occupied=left;
        foreach(var entry in entries.Where(x=>x.IsPoint))
        {
            double x=left+View.Fraction(entry.Low)*width;
            dc.DrawLine(new Pen(ColorBrush(settings.Peak),1),new Point(x,top+40),new Point(x,top+59));
            var label=Text(entry.Label,10,ColorBrush(settings.Text));
            if(x+3>=occupied && x+label.Width+5<=left+width){dc.DrawText(label,new Point(x+3,top+42));occupied=x+label.Width+9;}
        }
        if(entries.Length==0)dc.DrawText(Text("ARRL · no listed recommendation here",10,ColorBrush(settings.Text)),new Point(left+3,top+2));
        dc.Pop();
    }
    private void DrawPlanLabel(DrawingContext dc,string label,Rect bounds,double size)
    {
        // Keep narrow segments visible without fragments of unreadable text.
        if(bounds.Width < 42)return;
        var text=Text(label,size,ColorBrush(settings.Text));
        text.MaxTextWidth=bounds.Width-8;
        text.MaxLineCount=1;
        text.Trimming=TextTrimming.CharacterEllipsis;
        dc.DrawText(text,new Point(bounds.Left+4,bounds.Top+2));
    }
    private float BinValue(float[] values, double fraction, double pixelFraction)
    {
        double start = (View.Frequency(fraction) - captureStart) / captureSpan * values.Length;
        double end = (View.Frequency(Math.Min(1, fraction + pixelFraction)) - captureStart) / captureSpan * values.Length;
        int first = Math.Clamp((int)Math.Floor(start), 0, values.Length - 1), last = Math.Clamp((int)Math.Ceiling(end) - 1, first, values.Length - 1);
        float value = values[first]; for (int i = first + 1; i <= last; i++) value = Math.Max(value, values[i]);
        return value;
    }
    private float HistoryBinValue(WaterfallHistory.Row row, double fraction, double pixelFraction)
    {
        int length=row.Levels.Length;
        double start=(View.Frequency(fraction)-captureStart)/captureSpan*length;
        double end=(View.Frequency(Math.Min(1,fraction+pixelFraction))-captureStart)/captureSpan*length;
        int first=Math.Clamp((int)Math.Floor(start),0,length-1),last=Math.Clamp((int)Math.Ceiling(end)-1,first,length-1);
        byte value=row.Levels[first];for(int i=first+1;i<=last;i++)value=Math.Max(value,row.Levels[i]);return value-160;
    }
    private void DrawWaterfall(DrawingContext dc, Rect bounds)
    {
        var rows = history.Snapshot(); if (rows.Length == 0) return;
        int columns = Math.Clamp((int)Math.Ceiling(bounds.Width * VisualTreeHelper.GetDpi(this).DpiScaleX), 128, 4096), count = Math.Clamp((int)Math.Ceiling(bounds.Height * VisualTreeHelper.GetDpi(this).DpiScaleY), 32, 1600);
        var pixels = new byte[columns * count * 4];
        Color empty = (Color)ColorConverter.ConvertFromString(settings.Background);
        var palette = Enumerable.Range(0, 256).Select(i => DisplayPalette.Sample(settings.Palette, i / 255.0)).ToArray();
        double low = settings.AutoWater ? autoLow : settings.WaterLow;
        double high = settings.AutoWater ? autoHigh : settings.WaterHigh;
        double now = Math.Max(DisplaySettings.Clock, rows[^1].Time);
        var renderedRows=new Dictionary<int,int>();
        for (int y = 0; y < count; y++)
        {
            int row = WaterfallHistory.RowAt(rows, now - y / (double)(count - 1) * settings.HistorySeconds);
            if(renderedRows.TryGetValue(row,out int prior)) { Buffer.BlockCopy(pixels,prior*columns*4,pixels,y*columns*4,columns*4);continue; }
            renderedRows[row]=y;
            for (int x = 0; x < columns; x++)
            {
                Color color = empty;
                if (row >= 0)
                {
                    float value = HistoryBinValue(rows[row], x / (double)columns, 1.0 / columns);
                    int index = (int)Math.Clamp((value + settings.GainDb - low) / Math.Max(5, high - low) * 255, 0, 255); color = palette[index];
                }
                int p = (y * columns + x) * 4; pixels[p] = color.B; pixels[p + 1] = color.G; pixels[p + 2] = color.R; pixels[p + 3] = 255;
            }
        }
        var bitmap = BitmapSource.Create(columns, count, 96, 96, PixelFormats.Bgra32, null, pixels, columns * 4); bitmap.Freeze(); dc.DrawImage(bitmap, bounds);
    }
    private void DrawMarkers(DrawingContext dc, double left, double width, double top, double plotHeight, double waterTop, double bottom)
    {
        if (!rf) return;
        double center = TunedCenter;
        var (low, high) = (channel ?? new ChannelSettings((long)center, receiveMode)).Edges;
        double bandLeft = Math.Max(Start, center + low), bandRight = Math.Min(Start + Span, center + high);
        if (bandRight > bandLeft)
        {
            var fill = ColorBrush(settings.Tuned).Clone(); fill.Opacity = .12;
            double x = left + View.Fraction(bandLeft) * width, bandWidth = (bandRight - bandLeft) / Span * width;
            dc.DrawRectangle(fill, null, new Rect(x, top, bandWidth, Math.Max(0, bottom - top)));
        }
        if (InView(center))
        {
            double x = left + View.Fraction(center) * width;
            dc.DrawLine(new Pen(ColorBrush(settings.Tuned), 1) { DashStyle = DashStyles.Dash }, new Point(x, top), new Point(x, bottom));
        }
        var occupied = new[] { left, left, left };
        foreach (var group in markers.Where(m => InView(m.FrequencyHz)).OrderBy(m => m.FrequencyHz).GroupBy(m => m.FrequencyHz))
        {
            var entry = group.First(); double x = left + View.Fraction(entry.FrequencyHz) * width;
            var brush = ColorBrush(settings.UseEntryColors ? entry.Color : settings.Marker);
            dc.DrawLine(new Pen(brush, 1), new Point(x, top), new Point(x, top + plotHeight)); dc.DrawLine(new Pen(brush, .7), new Point(x, waterTop), new Point(x, bottom));
            string name = entry.Name.Length > 22 ? entry.Name[..21] + "…" : entry.Name; if (group.Count() > 1) name += $" (+{group.Count() - 1})";
            var text = Text(name, 11, brush); double labelX = Math.Clamp(x + 3, left, Math.Max(left, left + width - text.Width - 8));
            int lane = Array.FindIndex(occupied, end => end <= labelX);
            if (lane >= 0) { occupied[lane] = labelX + text.Width + 12; dc.DrawRectangle(ColorBrush(settings.Background), null, new Rect(labelX, top + 2 + lane * 19, text.Width + 6, 18)); dc.DrawText(text, new Point(labelX + 3, top + 3 + lane * 19)); }
        }
    }
    private void DrawPointer(DrawingContext dc, double left, double width, double top, double plotHeight, double waterTop, double bottom)
    {
        if (pointer is { } pp && InPlan(pp)) return;
        if (pointer is not { } p || FrequencyAt(p) is not { } hz) return;
        var pen = new Pen(ColorBrush(settings.Cursor), 1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(pen, new Point(p.X, top), new Point(p.X, top + plotHeight)); dc.DrawLine(pen, new Point(p.X, waterTop), new Point(p.X, bottom));
        string content = rf ? $"RF  {hz / 1000000:F6} MHz" : $"{basebandName}  {hz:N0} Hz";
        if (p.Y >= waterTop) content += $" · {(p.Y - waterTop) / (bottom - waterTop) * settings.HistorySeconds:0.0}s ago";
        if (rf) foreach (var item in markers.Where(m => Math.Abs(m.FrequencyHz - hz) / Span * width <= 6).Take(3)) { string summary = $"{item.Name} · {item.Mode} {item.Protocols}"; content += "\n" + (summary.Length > 45 ? summary[..44] + "…" : summary); }
        var label = Text(content, 12, ColorBrush(settings.Text)); double boxWidth = label.Width + 20, boxHeight = label.Height + 12;
        double x = Math.Clamp(p.X + 12, left, Math.Max(left, left + width - boxWidth)), y = Math.Clamp(p.Y - boxHeight - 10, top, Math.Max(top, bottom - boxHeight));
        dc.DrawRoundedRectangle(ColorBrush(settings.Background), new Pen(ColorBrush(settings.Cursor), 1), new Rect(x, y, boxWidth, boxHeight), 4, 4); dc.DrawText(label, new Point(x + 10, y + 6));
    }
    private FormattedText Text(string text, double size, Brush brush) => new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    private void Label(DrawingContext dc, string text, double x, double y) => dc.DrawText(Text(text, 10, ColorBrush(settings.Text)), new Point(x, y));
}


