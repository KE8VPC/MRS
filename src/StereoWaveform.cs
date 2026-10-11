using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace MRS;
public sealed class StereoWaveform : FrameworkElement
{
    private byte[]? audio;
    internal void SetAudio(byte[]? pcm) { audio = pcm; InvalidateVisual(); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(9,15,25)),null,new Rect(0,0,ActualWidth,ActualHeight));
        int count = Math.Min(960, (audio?.Length ?? 0) / 4);
        for (int channel=0;channel<2;channel++)
        {
            double center = ActualHeight * (channel == 0 ? .28 : .72), scale=ActualHeight*.18;
            dc.DrawLine(new Pen(Brushes.SlateGray,1),new Point(45,center),new Point(Math.Max(45,ActualWidth-15),center));
            var geometry = new StreamGeometry();
            using (var ctx=geometry.Open())
                for (int n=0;n<count;n++)
                {
                    double value=BitConverter.ToInt16(audio!,n*4+channel*2)/32768.0;
                    var point = new Point(45+(ActualWidth-60)*n/Math.Max(1,count-1),center-value*scale);
                    if(n==0)ctx.BeginFigure(point,false,false);else ctx.LineTo(point,true,false);
                }
            dc.DrawGeometry(null,new Pen(channel==0?Brushes.Turquoise:Brushes.Orange,1),geometry);
            var text=new FormattedText(channel==0?"LEFT":"RIGHT",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),12,Brushes.LightGray,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text,new Point(10,center-scale-20));
        }
        dc.DrawText(new FormattedText($"48 kHz audio · {count/48.0:0.0} ms · ±1 full scale · before volume/squelch",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),12,Brushes.LightGray,VisualTreeHelper.GetDpi(this).PixelsPerDip),new Point(15,Math.Max(0,ActualHeight-25)));
    }
}
