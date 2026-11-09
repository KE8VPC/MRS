using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
internal sealed record ReceiverProfile(string Id,string Name,string Role="Independent",string Group="",int Priority=0,string? ControlId=null){public string Display=>$"{Name} [{Id}] - {Role}"+(ControlId!=null?" · control receiver "+ControlId:Group.Length>0&&!Group.StartsWith("receiver:")?" · legacy group "+Group:"");}
public partial class MainWindow
{
 private ReceiverProfile fleetProfile=null!;private string fleetFile="";private bool fleetVoiceBusy,fleetPaused;private long fleetLeaseEpoch,fleetScope;private MainWindow? fleetOwner;private string fleetLease="";private DateTimeOffset fleetAssigned;private bool fleetAudioReported;private TrunkCall? fleetCall;private string fleetCallLabel="";private readonly Dictionary<MainWindow,string> fleetLeases=[];
 private static string CallKey(TrunkCall c)=>$"{c.Protocol}:{c.Hz}:{c.Slot}:{c.Talkgroup}:{c.Nac}";
 private IEnumerable<MainWindow> FleetWindows()=>Application.Current.Windows.OfType<MainWindow>().Where(w=>!w.closing&&w.diagnosticWorkspace==diagnosticWorkspace);
 private Dictionary<string,ReceiverProfile> ReadFleet(){try{return File.Exists(fleetFile)?JsonSerializer.Deserialize<Dictionary<string,ReceiverProfile>>(File.ReadAllText(fleetFile))??[]:[];}catch(Exception ex){throw new IOException("Receiver definitions could not be read; left unchanged: "+ex.Message);}}
 private void SaveFleet(ReceiverProfile p){var all=ReadFleet();all[p.Id]=p;Directory.CreateDirectory(Path.GetDirectoryName(fleetFile)!);string temp=fleetFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(all,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,fleetFile,true);}
 private void InitializeFleet(){fleetFile=Path.Combine(diagnosticWorkspace?dataFolder:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MRS"),"receiver-workspaces.json");try{fleetProfile=ReadFleet().GetValueOrDefault(workspaceSlot)??new(workspaceSlot,"Receiver "+workspaceSlot);}catch(Exception ex){fleetProfile=new(workspaceSlot,"Receiver "+workspaceSlot);Log(ex.Message);}ReflectFleet();}
 private void ReflectFleet(){WorkspaceLabel.Text=fleetProfile.Name;Title="M.R.S. - "+fleetProfile.Name;}
 private MainWindow OpenFleetReceiver(ReceiverProfile p){var old=FleetWindows().FirstOrDefault(w=>w.workspaceSlot==p.Id);if(old!=null){old.Activate();return old;}string? diagnosticRoot=diagnosticWorkspace?Path.Combine(dataFolder,"fleet",p.Id):null;var next=new MainWindow(diagnosticWorkspace,p.Id,diagnosticRoot);next.fleetProfile=p;next.fleetFile=fleetFile;next.ReflectFleet();next.Show();return next;}
 private void OpenFleetManager(){try{CreateFleetManager().Show();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Receiver definitions");}}
 private void ReleaseFleetAssignments(){foreach(var w in FleetWindows().Where(w=>w.fleetOwner==this).ToArray())w.EndFleetVoice("Control receiver stopped");if(fleetOwner!=null){fleetPaused=true;fleetLeaseEpoch++;EndFleetVoice("Voice receiver stopped or changed");}fleetLeases.Clear();}
 private void RecordFleetEvent(MainWindow owner,TrunkCall call,string kind,string detail)
 {
  var row=new TrunkEvent(DateTimeOffset.UtcNow,kind,detail,call.Hz,call.Talkgroup,fleetProfile.Name,owner.fleetProfile.Name,fleetCallLabel,call.Radio,call.Nac,call.Protocol,call.Protocol=="P25"?null:call.Slot,call.ColorCode);
  AppendTrunkHistory(row,true);owner.AppendTrunkHistory(row,true);
 }
 private void EndFleetVoice(string reason,bool retireGrant=false)
 {
  var owner=fleetOwner;var call=fleetCall;fleetLeaseEpoch++;fleetOwner=null;fleetLease="";fleetVoiceBusy=false;fleetCall=null;
  if(owner!=null){owner.fleetLeases.Remove(this);if(call!=null&&retireGrant)lock(owner.trunkRadio.Sync)owner.trunkRadio.Model.RetireAvailable(call);if(call!=null)RecordFleetEvent(owner,call,"Released",$"{reason}; lease {(DateTimeOffset.UtcNow-fleetAssigned).TotalSeconds:F1} s");}
  StopTrunk(false);trunkStatus.Text=reason;FollowingCallSummary.Text="";FollowingCallSummary.Visibility=Visibility.Collapsed;
 }
 private async Task BeginFleetVoice(MainWindow owner,TrunkCall call)
 {
  long reservation=++fleetLeaseEpoch;fleetVoiceBusy=true;fleetOwner=owner;fleetLease=CallKey(call);fleetAssigned=DateTimeOffset.UtcNow;fleetCall=call;fleetAudioReported=false;lock(owner.trunkRadio.Sync)fleetCallLabel=owner.trunkRadio.Model.Labels.GetValueOrDefault(call.Talkgroup,"");
  RecordFleetEvent(owner,call,"Assignment requested","Preparing voice receiver; tuning not yet confirmed");try{StopActivity("Trunk voice assignment");StopFolderScan("Trunk voice assignment");P25Packets.VoiceToggle.IsChecked=false;digitalMode.SelectedIndex=0;Aviation.Stop("Trunk voice assignment");HdRecordings.StopLive("Trunk voice assignment");Television.StopLive("Trunk voice assignment");var channel=new ChannelSettings(call.Hz,"NFM",call.Protocol=="P25"?16000:12500);if(!receiver.CanTune(call.Hz)||channel.Width>receiver.AppliedBandwidth)throw new IOException("Assigned voice channel exceeds this receiver's coverage.");if(channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))receiver.SetChannel(channel);else await receiver.Retune(call.Hz,channel);for(int n=0;n<250&&fleetLeaseEpoch==reservation&&fleetOwner==owner&&(receiver.AppliedChannel?.Frequency!=call.Hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth));n++)await Task.Delay(20);if(fleetLeaseEpoch!=reservation||fleetOwner!=owner||closing||owner.closing)return;if(receiver.AppliedChannel?.Frequency!=call.Hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))throw new IOException("Receiver did not report the assigned capture within five seconds.");engine.RetuneBoundary();trunkAudioKey="";trunkRadio.StartExternal(call,owner.fleetProfile.Name);lock(trunkRadio.Sync){trunkRadio.Model.HangSeconds=owner.trunkRadio.Model.HangSeconds;trunkRadio.Model.MaximumSeconds=owner.trunkRadio.Model.MaximumSeconds;trunkRadio.Model.Labels=new(owner.trunkRadio.Model.Labels);}RecordFleetEvent(owner,call,"Tuned","Receiver reported assigned frequency and capture; awaiting matching decoded voice");trunkStatus.Text=$"Voice assigned by {owner.fleetProfile.Name}: TG {call.Talkgroup}";}catch(Exception ex){if(reservation==fleetLeaseEpoch){RecordFleetEvent(owner,call,"Assignment failed",ex.Message);EndFleetVoice("Voice assignment failed: "+ex.Message);fleetPaused=true;}}finally{if(reservation==fleetLeaseEpoch)fleetVoiceBusy=false;}
 }
 private bool FleetControlActive=>fleetProfile?.Role=="Trunk control"&&fleetProfile.Group.Length>0&&trunkRadio.Running&&fleetOwner==null;
 private void TickFleet()
 {
  if(fleetOwner!=null){if(fleetOwner.closing||!fleetOwner.FleetControlActive||fleetOwner.fleetProfile.Group!=fleetProfile.Group||!rfMode||receiver.IsReplay){EndFleetVoice("Assignment released");return;}bool ended;lock(trunkRadio.Sync)ended=!trunkRadio.Running||trunkRadio.Model.Active==null;if(!fleetVoiceBusy&&ended)EndFleetVoice("Voice call ended",true);return;}
  if(!FleetControlActive){if(fleetLeases.Count>0)ReleaseFleetAssignments();return;}
  if(fleetScope!=trunkRadio.Model.ScopeVersion){ReleaseFleetAssignments();fleetScope=trunkRadio.Model.ScopeVersion;}
  var now=DateTimeOffset.UtcNow;
  foreach(var pair in fleetLeases.ToArray()){
   var w=pair.Key;if(w.closing||w.fleetOwner!=this){fleetLeases.Remove(w);continue;}
   bool allowed;double maximum;lock(trunkRadio.Sync){allowed=w.fleetCall is {} active&&trunkRadio.Model.AllowsTalkgroup(active.Talkgroup);maximum=trunkRadio.Model.MaximumSeconds;}
   if(!allowed)w.EndFleetVoice("Control talkgroup filter changed",true);
   else if((now-w.fleetAssigned).TotalSeconds>=maximum)w.EndFleetVoice("Maximum call timer",true);
  }
  // Take the available-call snapshot after retiring released leases. A prior
  // snapshot can still contain the just-ended call and immediately redispatch it.
  TrunkCall[] calls;lock(trunkRadio.Sync)calls=trunkRadio.Model.AvailableCalls(now);
  var assigned=fleetLeases.Values.ToHashSet();foreach(var call in calls){string key=CallKey(call);if(assigned.Contains(key))continue;var target=FleetWindows().Where(w=>w!=this&&w.fleetProfile.Role=="Trunk voice"&&w.fleetProfile.Group==fleetProfile.Group&&w.fleetOwner==null&&w.dmrFleetOwner==null&&!w.dmrFleetVoiceBusy&&!w.fleetVoiceBusy&&!w.fleetPaused&&w.rfMode&&!w.receiver.IsReplay&&!w.trunkRadio.Running&&!w.dmrTrunkRadio.Running&&w.receiver.SamplingMode==0&&w.receiver.CanTune(call.Hz)&&w.receiver.AppliedBandwidth>=(call.Protocol=="P25"?16000:12500)).OrderBy(w=>w.fleetProfile.Priority).ThenBy(w=>w.workspaceSlot).FirstOrDefault();if(target==null)continue;fleetLeases[target]=key;assigned.Add(key);_=target.BeginFleetVoice(this,call);}
 }
}
