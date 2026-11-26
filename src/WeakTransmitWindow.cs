using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace MRS;
internal sealed class WeakTransmitWindow : Window
{
 private readonly string mode,identity;private readonly long dial;private readonly Func<WeakTxPlan,TxDeclaration,CancellationToken,Task> transmit;private readonly Func<bool> live;private readonly Action<long?,double> requestReplyDecode;
 private readonly Func<float[]?> readAudioSpectrum;
 private readonly WeakAudioWaterfall? waterfall;
 private double? receiveMarker;private readonly TextBox call=new(){Width=100},grid=new(){Width=65},peer=new(){Width=105},gain=new(){Width=55,Text="0"},power=new(){Width=55,Text="0"},limit=new(){Width=55,Text="4"};private readonly ComboBox audio=new(){Width=100,IsEditable=true,ItemsSource=new[]{"500","1000","1500","2000","2500"},Text="1500"};
 private readonly ComboBox txSlot=new(){Width=210,SelectedIndex=0};
 private long? waitingReplySlot,completedReplySlot;
 private string slotNote="";
 private WeakTxStation station;private readonly Action<WeakTxStation> saveStation;private readonly TextBlock stationStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};private readonly CheckBox pc=Box("Use PC UTC: I checked clock synchronization independently"),auto=Box("Repeat on selected slot and advance the selected peer exchange");private readonly TextBlock state=new(){Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap},preview=new(){Foreground=Brushes.Gainsboro,TextWrapping=TextWrapping.Wrap};private readonly StackPanel fields=new();private readonly Button arm=new(){Content="Arm TX"};private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(50)};private PacketObservation? candidate;private readonly TextBlock lastDecode=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};private WeakQso? qso;private CancellationTokenSource? cancellation;private bool armed,busy,closePending;private int bursts;private DateTimeOffset next,expires;private double clockOffset;private long clockStamp;private DateTimeOffset clockUtc;private int allowed;
 private static CheckBox Box(string text)=>new(){Content=text,Foreground=Brushes.Gainsboro,Margin=new(4)};
 internal WeakTransmitWindow(string mode,long dial,string identity,Func<WeakTxPlan,TxDeclaration,CancellationToken,Task> transmit,Func<bool> live,PacketObservation? selected,Action<long?,double>? requestReplyDecode=null,Func<float[]?>? readAudioSpectrum=null,WeakTxStation? station=null,Action<WeakTxStation>? saveStation=null)
 {
  Background=new SolidColorBrush(Color.FromRgb(16,23,34));Foreground=Brushes.Gainsboro;FontSize=14;FontFamily=new FontFamily("Segoe UI");candidate=selected is {Replay:false,IsValidated:true}&&selected.Protocol==mode&&selected.FrequencyHz==dial&&DateTimeOffset.UtcNow-selected.Weak.Utc<TimeSpan.FromSeconds(35)?selected:null;this.mode=mode;this.dial=dial;this.identity=identity;this.transmit=transmit;this.live=live;this.requestReplyDecode=requestReplyDecode??((_,_)=>{});this.readAudioSpectrum=readAudioSpectrum??(()=>null);this.station=station??new();this.saveStation=saveStation??(_=>{});call.Text=this.station.Callsign;grid.Text=this.station.Grid;Title=mode+" transmit · operator controlled";Width=800;Height=670;MinHeight=520;var root=new DockPanel{Margin=new(12)};Content=root;var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);var actions=new WrapPanel();actions.Children.Add(arm);var halt=new Button{Content="HALT TX / disarm",Foreground=Brushes.DarkRed};actions.Children.Add(halt);bottom.Children.Add(actions);bottom.Children.Add(state);bottom.Children.Add(stationStatus);bottom.Children.Add(preview);bottom.Children.Add(lastDecode);root.Children.Add(new ScrollViewer{Content=fields,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  fields.Children.Add(new TextBlock{Text=$"HackRF · USB dial {dial/1e6:0.000000} MHz · {mode}\nSession ends after the burst limit or five minutes.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap});
  var row=new WrapPanel();fields.Children.Add(row);foreach(var pair in new (string,FrameworkElement)[]{("Your call",call),("Grid (4 chars)",grid),("TX audio offset (Hz)",audio),("TX gain dB",gain),("Burst limit 1–10",limit)}){var box=new StackPanel{Margin=new(3)};box.Children.Add(new TextBlock{Text=pair.Item1,Foreground=Brushes.Gainsboro});box.Children.Add(pair.Item2);row.Children.Add(box);}if(mode=="WSPR"){var p=new WrapPanel();p.Children.Add(new TextBlock{Text="Measured output dBm (0/3/7 steps, max 37)",Foreground=Brushes.Gainsboro});p.Children.Add(power);fields.Children.Add(p);fields.Children.Add(new TextBlock{Text="WSPR repeats no more than once per ten minutes. This session permits one beacon; re-arm to send another. Enter actual measured output power, not the HackRF gain setting.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});limit.Text="1";limit.IsEnabled=false;}else{var exchange=new WrapPanel();exchange.Children.Add(new TextBlock{Text="Peer call",Foreground=Brushes.Gainsboro});exchange.Children.Add(peer);var answer=new Button{Content="Answer peer"};exchange.Children.Add(answer);var cq=new Button{Content="Call CQ"};exchange.Children.Add(cq);var latest=new Button{Content="Use latest decoded caller"};exchange.Children.Add(latest);latest.Click+=(_,_)=>{if(!armed&&candidate?.Weak.SenderResolved==true)peer.Text=candidate.Weak.Sender;};fields.Children.Add(exchange);var steps=new WrapPanel();var step=new ComboBox{ItemsSource=new[]{"Grid","Report","R-report","RR73","73"},SelectedIndex=0,Width=100};var report=new TextBox{Text="-10",Width=55};var apply=new Button{Content="Set manual exchange step"};steps.Children.Add(step);steps.Children.Add(report);steps.Children.Add(apply);fields.Children.Add(steps);apply.Click+=(_,_)=>{try{if(armed)return;if(qso==null)throw new IOException("Select Answer peer first.");qso.Manual(step.SelectedItem.ToString()!,int.Parse(report.Text));RefreshPreview();}catch(Exception ex){state.Text=ex.Message;}};txSlot.ItemsSource=mode=="FT8"?new[]{"Even · starts :00 / :30 UTC","Odd · starts :15 / :45 UTC"}:new[]{"Even FT4 slots","Odd FT4 slots"};
   txSlot.SelectedIndex=0;
   var slotControls=new WrapPanel{Margin=new(3)};
   slotControls.Children.Add(new TextBlock{Text=mode=="FT8"?"TX slot (15 s) ":"TX slot ",VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.Gainsboro});
   slotControls.Children.Add(txSlot);
   var useRx=new Button{Content="Use decoded frequency and reply slot"};
   slotControls.Children.Add(useRx);
   fields.Children.Add(slotControls);
   useRx.Click+=(_,_)=>{if(candidate is { } decoded&&Utc()-decoded.Weak.Utc<TimeSpan.FromSeconds(35))ApplyDecodedSelectors(decoded);else state.Text="Select or receive a recent live decode first.";};
   waterfall=new WeakAudioWaterfall(mode){Margin=new Thickness(3,4,3,4)};
   receiveMarker=candidate?.Weak.AudioHz;
   waterfall.TransmitSelected+=offset=>audio.Text=offset.ToString(CultureInfo.InvariantCulture);
   fields.Children.Add(waterfall);
   fields.Children.Add(auto);answer.Click+=(_,_)=>{if(armed)return;try{qso=NewQso();qso.Answer(peer.Text.Trim().ToUpperInvariant(),candidate?.Weak.Sender==peer.Text.Trim().ToUpperInvariant()&&candidate.Weak.Snr is double n?(int)Math.Round(n):-10,Utc().AddSeconds(-35));if(candidate!=null)qso.Observe(candidate,Utc());if(candidate is { } received&&received.Weak.Sender==peer.Text.Trim().ToUpperInvariant()&&Utc()-received.Weak.Utc<TimeSpan.FromSeconds(35))ApplyDecodedSelectors(received,copyFrequency:false);RefreshPreview();}catch(Exception ex){state.Text=ex.Message;}};cq.Click+=(_,_)=>{if(armed)return;qso=null;RefreshPreview();};if(candidate?.Weak.SenderResolved==true)peer.Text=candidate.Weak.Sender;}
  call.TextChanged+=(_,_)=>{if(!armed){qso=null;RefreshPreview();}};
  grid.TextChanged+=(_,_)=>{if(!armed){qso=null;RefreshPreview();}};
  audio.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler((_,_)=>RefreshPreview()));
  txSlot.SelectionChanged+=(_,_)=>RefreshPreview();
  fields.Children.Add(pc);
  fields.Children.Add(new TextBlock{Text="Arm TX confirms station authority for the selected frequency, mode and equipment.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap});fields.Children.Add(new TextBlock{Text="Fresh NIST timing is used when available. RX timing estimates are not used for TX. Missed slots and timing/device/backend errors disarm the session. Start with a dummy load and monitor on a separate receiver.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap});arm.Click+=(_,_)=>Arm();halt.Click+=(_,_)=>Halt();timer.Tick+=async(_,_)=>await Tick();timer.Start();Closing+=(_,e)=>{Halt();if(busy){closePending=true;e.Cancel=true;}else{timer.Stop();RememberIdentity();}};RefreshPreview();
 }
 private void ApplyDecodedSelectors(PacketObservation decoded,bool copyFrequency=true)
 {
  receiveMarker=decoded.Weak.AudioHz;
  if(copyFrequency&&decoded.Weak.AudioHz is >=300 and <=2500)
   audio.Text=Math.Round(decoded.Weak.AudioHz.Value).ToString(CultureInfo.InvariantCulture);
  if(decoded.Weak.Slot is long receivedSlot)
   txSlot.SelectedIndex=(receivedSlot&1)==0?1:0;
  RefreshPreview();
 }
 internal void ReceiveCycle(long slot)
 {
  if(slot==waitingReplySlot)completedReplySlot=slot;
 }
 private void RememberIdentity()
 {
  var updated=station.WithIdentity(call.Text,grid.Text);
  if(!updated.HasIdentity||updated==station)return;
  try{saveStation(updated);station=updated;stationStatus.Text="";}
  catch(Exception ex){stationStatus.Text="Could not save station identity: "+ex.Message;}
 }
 private DateTimeOffset Utc()=>DateTimeOffset.UtcNow.AddSeconds(clockOffset);
 private WeakQso NewQso(){string c=call.Text.Trim().ToUpperInvariant(),g=grid.Text.Trim().ToUpperInvariant();if(!System.Text.RegularExpressions.Regex.IsMatch(g,@"^[A-R]{2}[0-9]{2}$"))throw new IOException("Enter a four-character Maidenhead grid.");var result=new WeakQso(c,g);result.Cq();return result;}
 private string Message=>mode=="WSPR"?$"{call.Text.Trim().ToUpperInvariant()} {grid.Text.Trim().ToUpperInvariant()} {power.Text.Trim()}":qso?.Message??$"CQ {call.Text.Trim().ToUpperInvariant()} {grid.Text.Trim().ToUpperInvariant()}";
 private TxDeclaration ArmedDeclaration()=>new("United States",call.Text.Trim().ToUpperInvariant(),station.LicenseClass,true,true,true);
 private WeakTxPlan Plan()=>new(mode,dial,int.Parse(audio.Text,CultureInfo.InvariantCulture),Message,int.Parse(gain.Text,CultureInfo.InvariantCulture),next,clockOffset);
 private void Arm(){try{if(armed||busy)return;if(!live())throw new IOException("Start live USB reception with the selected HackRF before arming.");clockOffset=0;if(NistTime.Reading is {Fresh:true,Age:<900,UncertaintySeconds:<.1} reference&&Math.Abs(reference.CurrentOffset)<5)clockOffset=reference.CurrentOffset;else if(pc.IsChecked!=true)throw new IOException("Sync to NIST first, or explicitly confirm your PC clock is synchronized.");allowed=int.Parse(limit.Text);if(allowed<1||allowed>10)throw new IOException("Burst limit must be 1–10.");if(mode!="WSPR"&&(qso==null||qso.Complete))qso=NewQso();next=WeakTxPlan.NextSlot(Utc(),mode,txSlot.SelectedIndex==1);WeakTxAuthorization.Validate(ArmedDeclaration(),Plan(),identity);_=WeakWaveform.Encode(Plan());next=WeakTxPlan.NextSlot(Utc(),mode,txSlot.SelectedIndex==1,WeakTxPlan.InitialHandoffLeadSeconds);RememberIdentity();expires=Utc().AddMinutes(5);bursts=0;waitingReplySlot=completedReplySlot=null;slotNote="";clockStamp=Stopwatch.GetTimestamp();clockUtc=Utc();cancellation=new();armed=true;fields.IsEnabled=false;if(waterfall!=null)waterfall.CanSelectTransmit=false;arm.IsEnabled=false;state.Text=$"Armed · next slot {next:HH:mm:ss.fff} UTC";RefreshPreview();}catch(Exception ex){state.Text=ex.Message;}}
 internal void Observe(PacketObservation p){if(p.Protocol==mode&&p.FrequencyHz==dial&&p.IsValidated&&!p.Replay){candidate=p;lastDecode.Text="Latest RX: "+p.Weak.Message;}if(armed&&!busy&&mode!="WSPR"&&auto.IsChecked==true&&p.Protocol==mode&&p.FrequencyHz==dial&&qso?.Observe(p,Utc())==true){state.Text="Exchange advanced · "+qso.Stage;RefreshPreview();if(qso.Complete)Halt("QSO complete");}}
 private async Task Tick()
 {
  if(waterfall!=null&&readAudioSpectrum() is {} levels)waterfall.Append(levels);
  if(!armed)return;
  var now=Utc();
  if(Math.Abs((now-(clockUtc+Stopwatch.GetElapsedTime(clockStamp))).TotalSeconds)>.1){Halt("Session ended: clock changed");return;}
  if(busy)return;
  if(now>=expires||!live()){Halt("Session ended: source or time limit changed");return;}
  if(now<next.AddSeconds(-(bursts==0?WeakTxPlan.InitialHandoffLeadSeconds:WeakTxPlan.Ft8HandoffLeadSeconds))){
   try{_=WeakWaveform.Encode(Plan());}catch(Exception ex){Halt("Message preparation failed: "+ex.Message);return;}
   state.Text=$"RX · TX message prepared · next TX {next:HH:mm:ss.fff} UTC · {Message} · {bursts}/{allowed}"+slotNote;
   return;
  }
  if(now>=next){Halt("Missed transmit slot");return;}

  // FT8 uses the same parity every 30 seconds. A peer exchange needs its
  // receive job to finish before the handoff can freeze the outgoing message.
  if(waitingReplySlot is long expected&&completedReplySlot!=expected){
   slotNote=$" · Held {next:HH:mm:ss} UTC while waiting for the reply decode";
   next=WeakTxPlan.FollowingFt8Slot(next);
   waitingReplySlot=WeakTxPlan.Ft8ReplySlot(next);
   requestReplyDecode(waitingReplySlot,clockOffset);
   return;
  }

  busy=true;
  try{
   var sent=Plan();
   bool repeats=auto.IsChecked==true;
   bool captureFt8Reply=mode=="FT8"&&repeats&&bursts+1<allowed;
   var following=WeakTxPlan.FollowingFt8Slot(sent.SlotUtc);
   long? replySlot=captureFt8Reply?WeakTxPlan.Ft8ReplySlot(following):null;
   waitingReplySlot=qso?.Peer.Length>0?replySlot:null;
   completedReplySlot=null;
   requestReplyDecode(replySlot,clockOffset);
   state.Text="Preparing slot · "+sent.Message;
   await transmit(sent,ArmedDeclaration(),cancellation!.Token);
   bursts++;
   qso?.Sent();
   if(!armed)return;
   if(mode=="WSPR"||bursts>=allowed||qso?.Complete==true||qso?.Attempts>=3){Halt("Session complete / retry limit · receive restored");return;}
   if(!repeats){Halt("Burst complete · review received exchange, then re-arm");return;}
   next=mode=="FT8"?following:WeakTxPlan.NextSlot(Utc().AddSeconds(15),mode,txSlot.SelectedIndex==1);
   slotNote="";
  }catch(Exception ex){Halt("TX stopped: "+ex.Message);}
  finally{
   busy=false;
   if(!armed){arm.IsEnabled=true;fields.IsEnabled=true;}
   if(closePending){timer.Stop();Close();}
  }
 }
 internal void Halt(string reason="Disarmed"){armed=false;requestReplyDecode(null,clockOffset);cancellation?.Cancel();fields.IsEnabled=!busy;if(waterfall!=null)waterfall.CanSelectTransmit=true;arm.IsEnabled=!busy;state.Text=reason;}
 private void RefreshPreview()
 {
  bool validOffset=int.TryParse(audio.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out int offset)&&offset is >=300 and <=2500;
  waterfall?.SetMarkers(validOffset?offset:null,receiveMarker);
  string rf=validOffset?$" · RF {(dial+offset)/1e6:0.000000} MHz":"";
  preview.Text="Next message: "+Message+rf;
 }
}
public partial class MainWindow
{
 private WeakTransmitWindow? weakTransmitWindow;private bool weakTxHandoff;private static readonly Dictionary<string,DateTimeOffset> lastWsprByDevice=[];
 private void OpenWeakTransmit(){try{if(fleetOwner!=null||dmrFleetOwner!=null||trunkRadio.Running||dmrTrunkRadio.Running||fleetLeases.Count>0||dmrFleetLeases.Count>0)throw new IOException("Stop trunk following and release this receiver from active assignments before digital TX.");if(!rfMode||receiver.IsReplay||!SelectedHackRf||receiver.AppliedChannel is not {Mode:"USB"} ch||RtlDevices.SelectedItem is not RtlNative.Device device)throw new IOException("Start live HackRF USB reception on the desired dial frequency first.");string identity=savedReceiverIdentity??throw new IOException("Stable HackRF identity required");var mode=weakMode.SelectedItem?.ToString()??"FT8";if(!weakData.Enabled)weakData.Configure(mode,EffectiveWeakOffset);weakTransmitWindow=new(mode,ch.Frequency,identity,SendWeakBurst,()=>rfMode&&!receiver.IsReplay&&receiver.AppliedChannel is {Mode:"USB"} live&&live.Frequency==ch.Frequency&&savedReceiverIdentity==identity,weakPanel.MonitorGrid.SelectedItem as PacketObservation,weakData.SetFt8ReplySlot,weakData.TakeAudioSpectrum,ReadWeakTxStation(),value=>value.Save(WeakTxStationPath)){Owner=this};weakData.EnableAudioPlot(mode is "FT8" or "FT4");try{weakTransmitWindow.ShowDialog();}finally{weakData.EnableAudioPlot(false);weakTransmitWindow=null;}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Digital transmit");}}
 private async Task SendWeakBurst(WeakTxPlan p,TxDeclaration declaration,CancellationToken token)
 {
  token.ThrowIfCancellationRequested();if(diagnosticWorkspace)throw new IOException("RF transmit is disabled in diagnostic workspaces");if(p.Mode=="WSPR"&&lastWsprByDevice.TryGetValue(savedReceiverIdentity??"",out var last)&&DateTimeOffset.UtcNow-last<TimeSpan.FromMinutes(10))throw new IOException("Wait ten minutes between WSPR beacons.");var restore=recoveryPlan??throw new IOException("Receiver configuration cannot be restored; restart it first.");if(receiver.AppliedChannel is not {Mode:"USB"} current)throw new IOException("Live USB receive channel is required");restore=restore with{Frequency=current.Frequency,Mode=current.Mode,Bandwidth=current.Width,Agc=current.Agc,Rate=receiver.AppliedRate,Gain=activeGain,Offset=receiver.Offset,Ppm=receiver.AppliedPpm,Wide=false};if(RtlDevices.SelectedItem is not RtlNative.Device device||restore.Backend!="HackRF"||restore.Frequency!=p.DialHz)throw new IOException("Receiver changed before transmission");byte[] preparedTones=WeakWaveform.Encode(p);string permit=WeakTxAuthorization.Issue(declaration,p,restore.Identity,preparedTones);Log($"Digital TX handoff: {(p.SlotUtc-DateTimeOffset.UtcNow.AddSeconds(p.ClockOffset)).TotalMilliseconds:0} ms until slot; message symbols prepared.");weakTxHandoff=true;Stop();
  try{
   if(!ReceiverOwnership.Acquire(this,device.Index,restore.Identity,"Digital transmit",out string ownership,"HackRF"))throw new IOException(ownership);var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(string arg in new[]{"--hackrf-weak-tx",device.Index.ToString(),p.DialHz.ToString(),p.Gain.ToString(),"0",Convert.ToBase64String(Encoding.UTF8.GetBytes(restore.Identity)),Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p)))})start.ArgumentList.Add(arg);start.Environment[WeakTxAuthorization.Variable]=permit;using var worker=Process.Start(start)??throw new IOException("Cannot start digital TX worker");using var stop=token.Register(()=>{try{worker.StandardInput.WriteLine("STOP");worker.StandardInput.Flush();}catch{}});var errors=worker.StandardError.ReadToEndAsync();var reader=Task.Run(async()=>{while(await worker.StandardOutput.ReadLineAsync() is {} line){if(line=="TX_READY")await Dispatcher.InvokeAsync(()=>Log($"Digital TX worker ready: {(p.SlotUtc-DateTimeOffset.UtcNow.AddSeconds(p.ClockOffset)).TotalMilliseconds:0} ms until slot."));if(line=="TX_STARTED")await Dispatcher.InvokeAsync(()=>{txIndicatorActive=true;if(p.Mode=="WSPR")lastWsprByDevice[restore.Identity]=DateTimeOffset.UtcNow;RefreshRxTxIndicator();});if(line=="TX_STOPPED")await Dispatcher.InvokeAsync(()=>{txIndicatorActive=false;RefreshRxTxIndicator();});}});try{await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(p.Duration+12),token);}catch{try{if(!worker.HasExited){worker.StandardInput.WriteLine("STOP");await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(700));}}catch{if(!worker.HasExited){worker.Kill(true);await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));}}throw;}await reader;if(worker.ExitCode!=0)throw new IOException((await errors).Trim());token.ThrowIfCancellationRequested();
  }finally{txIndicatorActive=false;ReceiverOwnership.Release(this);RefreshRxTxIndicator();try{if(!closing&&ClaimReceiver(device,restore.Identity)){recoveryPlan=restore;activeDevice=device.Index;activeRate=restore.Rate;activeGain=restore.Gain;tunedHz=restore.Frequency;tunedMode=restore.Mode;rfMode=true;engine.StartExternal(2);receiver.Start(device.Index,restore.Frequency,restore.Gain,restore.Wide,sampleRate:restore.Rate,identity:restore.Identity,mode:restore.Mode,bandwidth:restore.Bandwidth,agc:restore.Agc,samplingMode:restore.SamplingMode,backend:restore.Backend,captureOffset:restore.Offset,ppm:restore.Ppm);for(int n=0;n<100&&rfMode&&receiver.AppliedChannel?.Frequency!=restore.Frequency;n++)await Task.Delay(50);if(!rfMode||receiver.AppliedChannel?.Mode!="USB"||receiver.AppliedChannel?.Frequency!=restore.Frequency)throw new IOException("Receiver did not acknowledge USB reception after TX");Spectrum.SetRf(restore.Frequency,restore.Rate);SpectrumTitle.Text="RF SPECTRUM / WATERFALL";Started();RtlStatus.Text="RX restored after digital transmit; recordings remain disarmed.";}}catch{Stop();throw;}finally{weakTxHandoff=false;}}
 }
}
