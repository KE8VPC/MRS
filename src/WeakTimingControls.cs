using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
 private readonly RxTimingTracker rxTiming=new();private readonly Expander weakTimingExpander=new(){Header="Timing details / controls",Foreground=Brushes.Gainsboro};
 private readonly CheckBox useNistTime=new(){Content="Use NIST timing",Foreground=Brushes.Gainsboro},autoNistTime=new(){Content="Auto NIST refresh",Foreground=Brushes.Gainsboro},autoRxTime=new(){Content="Auto RX timing",Foreground=Brushes.Gainsboro};
 private readonly TextBlock timingStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};
 private NistReading? lastNistReference;private double manualWeakOffset,rxWeakOffset;private long nextNistCheck,lastRxApply;private bool nistChecking;private int resetRxTiming;
 private bool WeakReplay=>rfMode?receiver.IsReplay:packetAudioReplay;
 private double NistWeakOffset=>!WeakReplay&&useNistTime.IsChecked==true&&NistTime.Reading is {Fresh:true} value&&Math.Abs(value.CurrentOffset)<=86400?value.CurrentOffset:0;
 private double EffectiveWeakOffset=>manualWeakOffset+NistWeakOffset+rxWeakOffset;
 private void InitializeWeakTiming(Panel row)
 {
  weakData.ClockJump+=()=>Interlocked.Exchange(ref resetRxTiming,1);
  var parent=(Panel)row.Parent;var content=new StackPanel();weakTimingExpander.Content=content;InitializeCompactWeakTiming(parent);parent.Children.Add(weakTimingExpander);row=new WrapPanel();content.Children.Add(row);row.Children.Add(new TextBlock{Text="Manual offset s ",Foreground=Brushes.Gainsboro,VerticalAlignment=VerticalAlignment.Center});row.Children.Add(weakOffset);content.Children.Add(timingStatus);
  void Button(string title,Action action){var b=new Button{Content=title,Padding=new(7,4,7,4),Margin=new(3)};row.Children.Add(b);b.Click+=(_,_)=>action();}
  var check=new Button{Content="Sync decoder to NIST",Padding=new(7,4,7,4),Margin=new(3)};row.Children.Add(check);check.Click+=async(_,_)=>await CheckWeakNist();
  foreach(var box in new[]{useNistTime,autoNistTime,autoRxTime}){box.Margin=new(5,3,5,3);row.Children.Add(box);}
  useNistTime.Checked+=(_,_)=>{rxTiming.Reset();rxWeakOffset=0;};useNistTime.Unchecked+=(_,_)=>{rxTiming.Reset();rxWeakOffset=0;};
  autoNistTime.Checked+=async(_,_)=>{nextNistCheck=0;await CheckWeakNist();};
  Button("Calculate RX offset",()=>{var e=rxTiming.Estimate();timingStatus.Text=e==null?"No CRC-valid station timing yet — start FT8/FT4 reception and wait for complete slots.":FormatRx(e);});
  Button("Apply RX estimate",()=>ApplyWeakEstimate(false));
  Button("Reset timing",()=>{manualWeakOffset=rxWeakOffset=0;weakOffset.Text="0";rxTiming.Reset();useNistTime.IsChecked=autoNistTime.IsChecked=autoRxTime.IsChecked=false;weakData.SetTimeOffset(0);timingStatus.Text="Timing reset; Windows clock unchanged.";});

 }
 private async Task CheckWeakNist()
 {
  if(nistChecking)return;nistChecking=true;try{await NistTime.CheckAsync();useNistTime.IsChecked=true;rxTiming.Reset();rxWeakOffset=0;weakData.SetTimeOffset(EffectiveWeakOffset);}
  catch(Exception e){timingStatus.Text=e.Message;Log(e.Message);}
  finally{nistChecking=false;nextNistCheck=Stopwatch.GetTimestamp()+(long)((900+Random.Shared.Next(0,60))*Stopwatch.Frequency);}
 }
 private static string FormatRx(RxTimingEstimate e)=>$"RX estimate {e.Correction:+0.000;-0.000;0.000} s total · {e.Stations} stations / {e.Samples} decodes / {e.Slots} slots · spread {e.Spread:0.000} s · {e.Confidence}";
 private void ApplyWeakEstimate(bool automatic)
 {
  var estimate=rxTiming.Estimate();if(estimate==null||!estimate.Confidence.StartsWith("Ready")){if(!automatic)timingStatus.Text=estimate==null?"No station estimate available.":FormatRx(estimate);return;}
  if(weakMode.SelectedItem?.ToString() is not ("FT8" or "FT4"))return;
  double target=estimate.Correction-manualWeakOffset-NistWeakOffset;
  if(automatic){if(lastRxApply!=0&&Stopwatch.GetElapsedTime(lastRxApply).TotalSeconds<60)return;rxWeakOffset+=Math.Clamp(target-rxWeakOffset,-.1,.1);}else rxWeakOffset=Math.Clamp(target,-2,2);
  rxWeakOffset=Math.Clamp(rxWeakOffset,-2,2);lastRxApply=Stopwatch.GetTimestamp();weakData.SetTimeOffset(EffectiveWeakOffset);
 }
 private void TickWeakTiming()
 {
  if(Interlocked.Exchange(ref resetRxTiming,0)!=0){rxTiming.Reset();rxWeakOffset=0;lastRxApply=0;}
  var reference=useNistTime.IsChecked==true&&!WeakReplay&&NistTime.Reading is {Fresh:true} fresh&&Math.Abs(fresh.CurrentOffset)<=86400?fresh:null;if(reference!=lastNistReference){lastNistReference=reference;rxTiming.Reset();rxWeakOffset=0;weakData.SetTimeOffset(EffectiveWeakOffset);}
  if(autoNistTime.IsChecked==true&&!nistChecking&&Stopwatch.GetTimestamp()>=nextNistCheck)_=CheckWeakNist();
  var estimate=rxTiming.Estimate();if(autoRxTime.IsChecked==true)ApplyWeakEstimate(true);
  weakData.SetTimeOffset(EffectiveWeakOffset);
  string nist=NistTime.Reading is {} r?$"{(Math.Abs(r.CurrentOffset)>86400?"PC date jump too large / not applied":r.Fresh?"available":"STALE / not applied")} · PC correction {r.CurrentOffset:+0.000;-0.000;0.000} s · delay {r.DelaySeconds*1000:0} ms · estimated uncertainty {r.UncertaintySeconds*1000:0} ms · age {r.Age:0} s":"not checked";
  timingStatus.Text=$"NIST: {nist}{(WeakReplay?" · replay: NIST correction excluded":"")}\nApplied decoder correction {weakData.AppliedOffset:+0.000;-0.000;0.000} s · manual {manualWeakOffset:+0.000;-0.000;0.000} s · RX trim {rxWeakOffset:+0.000;-0.000;0.000} s · Windows clock unchanged\n"+(estimate==null?"RX: waiting for valid station timing":FormatRx(estimate))+"\n"+weakData.Status;RefreshCompactWeakTiming();
 }
}
