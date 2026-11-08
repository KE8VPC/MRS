using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
 private PskReporter pskReporter=null!;private Expander pskSection=null!;private readonly CheckBox pskEnabled=new(){Content="Enable reception reports",Foreground=Brushes.Gainsboro};private readonly TextBox pskCall=new(){Width=110},pskGrid=new(){Width=85},pskRig=new(){Width=220},pskAntenna=new(){Width=220};private readonly TextBlock pskStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};private string pskReceiverKey="";
 private string PskReceiverKey=>SelectedBackend+"|"+savedReceiverIdentity+"|"+activeDevice;
 private void InitializePskReporting(Panel parent)
 {
  pskReporter=new(allowNetwork:!diagnosticWorkspace,utcNow:()=>DateTimeOffset.UtcNow.AddSeconds(NistWeakOffset));string path=Path.Combine(dataFolder,"psk-reporter.json");try{if(File.Exists(path)){var value=JsonSerializer.Deserialize<PskSettings>(File.ReadAllText(path));if(value!=null){pskCall.Text=value.Callsign;pskGrid.Text=value.Grid;pskRig.Text=value.Rig;pskAntenna.Text=value.Antenna;}}}catch(Exception e){Log("Reporter settings: "+e.Message);}
  var body=new StackPanel();var row=new WrapPanel();body.Children.Add(row);foreach(var pair in new[]{("Callsign",pskCall),("Grid",pskGrid),("Rig (blank = SDR family)",pskRig),("Antenna (optional)",pskAntenna)}){var field=new StackPanel{Margin=new(0,0,8,5)};field.Children.Add(new TextBlock{Text=pair.Item1,Foreground=Brushes.LightSteelBlue});field.Children.Add(pair.Item2);row.Children.Add(field);}body.Children.Add(pskEnabled);body.Children.Add(pskStatus);body.Children.Add(new TextBlock{Text="Opt-in: shares receiver callsign/grid, valid live FT8/FT4 receptions, M.R.S. version and optional rig/antenna. No replay reports. Ignored senders are excluded by default (Rules / colors controls this). UDP sent does not confirm delivery.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,FontSize=11});parent.Children.Add(pskSection=new Expander{Header="PSK Reporter · off",Content=body,Foreground=Brushes.Gainsboro,IsExpanded=false});
  pskEnabled.Checked+=(_,_)=>{try{if(!rfMode||receiver.IsReplay||receiver.AppliedChannel?.Mode!="USB")throw new IOException("Start live SDR USB reception before enabling reporting.");var settings=new PskSettings(pskCall.Text.Trim(),pskGrid.Text.Trim(),pskRig.Text.Trim(),pskAntenna.Text.Trim());File.WriteAllText(path,JsonSerializer.Serialize(settings));pskReceiverKey=PskReceiverKey;pskReporter.Enable(settings with{Rig=settings.Rig.Length>0?settings.Rig:SelectedHackRf?"HackRF One / PortaPack":"RTL-SDR"});foreach(var field in new[]{pskCall,pskGrid,pskRig,pskAntenna})field.IsEnabled=false;}catch(Exception e){pskEnabled.IsChecked=false;pskStatus.Text=e.Message;}};
  pskEnabled.Unchecked+=(_,_)=>{pskReporter.Disable();foreach(var field in new[]{pskCall,pskGrid,pskRig,pskAntenna})field.IsEnabled=true;};Closed+=(_,_)=>pskReporter.Dispose();
 }
 private void QueuePskReport(PacketObservation p){if(pskReporter==null||!pskReporter.Enabled)return;if(pskReceiverKey!=PskReceiverKey){pskEnabled.IsChecked=false;pskStatus.Text="Receiver changed · enable reporting again";return;}if(!rfMode||receiver.IsReplay||receiver.AppliedChannel?.Mode!="USB"||p.SourceKey!=RadioContext("weak",receiver.AppliedChannel.Frequency).Key||p.Weak.Epoch!=weakData.TimingEpoch)return;pskReporter.Queue(p,weakPanel.WeakSettings.Ignored(p.Weak),weakPanel.WeakSettings.ExcludeIgnoredReports,DateTimeOffset.UtcNow.AddSeconds(NistWeakOffset));}
 private void TickPskReporting(){if(pskReporter==null)return;pskReporter.Tick();pskSection.Header=!pskReporter.Enabled?"PSK Reporter · off":$"PSK Reporter · {(pskReporter.Status.Contains("failed")?"send failed":"enabled")} · queued {pskReporter.Queued} · UDP sent {pskReporter.Sent}";if(pskEnabled.IsChecked==true)pskStatus.Text=pskReporter.Status+$" · queued {pskReporter.Queued}";}
}
