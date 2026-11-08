using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace MRS;
internal sealed class TimeOffsetGauge:FrameworkElement
{
 internal double? Value;internal void Update(double? value){Value=value;InvalidateVisual();}
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);double width=ActualWidth;var pen=new Pen(Brushes.SlateGray,1);dc.DrawLine(pen,new(8,9),new(width-8,9));for(int tick=-2;tick<=2;tick++){double x=8+(tick+2)*(width-16)/4;dc.DrawLine(pen,new(x,4),new(x,14));if(tick%2==0){var t=new FormattedText(tick==0?"0":$"{tick:+0;-0} s",CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),10,Brushes.LightSteelBlue,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(t,new(Math.Clamp(x-t.Width/2,0,width-t.Width),17));}}if(Value is double value){double x=8+(Math.Clamp(value,-2,2)+2)*(width-16)/4;dc.DrawEllipse(Brushes.Aquamarine,null,new(x,9),4,4);}}
}
public partial class MainWindow
{
 private readonly TextBlock compactReference=new(){Foreground=Brushes.Gainsboro,VerticalAlignment=VerticalAlignment.Center},compactOffset=new(){Foreground=Brushes.Aquamarine,VerticalAlignment=VerticalAlignment.Center},compactCorrection=new(){Foreground=Brushes.Gainsboro,VerticalAlignment=VerticalAlignment.Center},compactCycle=new(){Foreground=Brushes.LightSteelBlue,VerticalAlignment=VerticalAlignment.Center},compactCounts=new(){Foreground=Brushes.Aquamarine,VerticalAlignment=VerticalAlignment.Center};
 private readonly Ellipse compactLight=new(){Width=9,Height=9,Fill=Brushes.Gray,Margin=new(0,0,6,0),VerticalAlignment=VerticalAlignment.Center};private readonly TimeOffsetGauge compactGauge=new(){Width=145,Height=33,Margin=new(8,0,8,0)};private readonly ProgressBar compactProgress=new(){Width=160,Height=7,Maximum=15,Margin=new(8,0,8,0),VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.Aquamarine};
 private void InitializeCompactWeakTiming(Panel parent)
 {
  weakStatus.Visibility=Visibility.Collapsed;var body=new StackPanel();var row=new WrapPanel();body.Children.Add(row);foreach(var control in new FrameworkElement[]{compactLight,compactReference,compactGauge,compactOffset,compactCorrection}){control.Margin=new(control.Margin.Left,2,10,2);row.Children.Add(control);}row=new WrapPanel();body.Children.Add(row);foreach(var control in new FrameworkElement[]{compactCycle,compactProgress,compactCounts})row.Children.Add(control);parent.Children.Add(new Border{Child=body,BorderBrush=new SolidColorBrush(Color.FromRgb(53,73,96)),BorderThickness=new(1),CornerRadius=new(3),Padding=new(8,4,8,4),Margin=new(0,4,0,3)});
 }
 private void RefreshCompactWeakTiming()
 {
  var r=NistTime.Reading;bool fresh=r is{Fresh:true}&&Math.Abs(r.CurrentOffset)<=86400;compactReference.Text=WeakReplay?"Replay timing":fresh?(useNistTime.IsChecked==true?"NIST active":"NIST ready · not applied"):r==null?"NIST not checked":"NIST stale · not applied";compactLight.Fill=WeakReplay?Brushes.LightSteelBlue:fresh?Brushes.Aquamarine:r==null?Brushes.Gray:Brushes.Gold;compactGauge.Update(fresh&&!WeakReplay?r!.CurrentOffset:null);compactOffset.Text=fresh&&!WeakReplay?$"PC offset {r!.CurrentOffset:+0.000;-0.000;0.000} s":"PC offset —";compactCorrection.Text=$"Decoder {weakData.AppliedOffset:+0.000;-0.000;0.000} s";compactCorrection.ToolTip="Decoder-only correction. Windows clock unchanged.";var progress=weakData.Progress;compactProgress.Maximum=progress.Period;compactProgress.Value=progress.Progress;compactCycle.Text=$"{weakMode.SelectedItem} · {progress.State} · {progress.Progress:0.0} / {progress.Period:0.#} s";compactCounts.Text=$"Decoded {progress.Decoded} · Dropped {progress.Dropped}";
 }
}
