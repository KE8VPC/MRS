using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace MRS;
public partial class MainWindow
{
 private bool dmrFleetVoiceBusy,dmrFleetAudioReported;private long dmrFleetLeaseEpoch,dmrFleetScope;private MainWindow? dmrFleetOwner;private string dmrFleetLease="",dmrFleetCallLabel="";private DateTimeOffset dmrFleetAssigned;private TrunkCall? dmrFleetCall;private readonly Dictionary<MainWindow,string> dmrFleetLeases=[];
 private void ReleaseDmrFleetAssignments(){foreach(var w in FleetWindows().Where(w=>w.dmrFleetOwner==this).ToArray())w.EndDmrFleetVoice("Control receiver stopped");if(dmrFleetOwner!=null){fleetPaused=true;dmrFleetLeaseEpoch++;EndDmrFleetVoice("Voice receiver stopped or changed");}dmrFleetLeases.Clear();}
 private void RecordDmrFleetEvent(MainWindow owner,TrunkCall call,string kind,string detail)
 {
  var row=new TrunkEvent(DateTimeOffset.UtcNow,kind,detail,call.Hz,call.Talkgroup,fleetProfile.Name,owner.fleetProfile.Name,dmrFleetCallLabel,call.Radio,call.Nac,call.Protocol,call.Protocol=="P25"?null:call.Slot,call.ColorCode);
  AppendDmrTrunkHistory(row,true);owner.AppendDmrTrunkHistory(row,true);
 }
 private void EndDmrFleetVoice(string reason,bool retireGrant=false)
 {
  var owner=dmrFleetOwner;var call=dmrFleetCall;dmrFleetLeaseEpoch++;dmrFleetOwner=null;dmrFleetLease="";dmrFleetVoiceBusy=false;dmrFleetCall=null;
  if(owner!=null){owner.dmrFleetLeases.Remove(this);if(call!=null&&retireGrant)lock(owner.dmrTrunkRadio.Sync)owner.dmrTrunkRadio.Model.RetireAvailable(call);if(call!=null)RecordDmrFleetEvent(owner,call,"Released",$"{reason}; lease {(DateTimeOffset.UtcNow-dmrFleetAssigned).TotalSeconds:F1} s");}
  StopDmrTrunk(false);dmrTrunkStatus.Text=reason;DmrFollowingCallSummary.Text="";DmrFollowingCallSummary.Visibility=Visibility.Collapsed;
 }
 private async Task BeginDmrFleetVoice(MainWindow owner,TrunkCall call)
 {
  long reservation=++dmrFleetLeaseEpoch;dmrFleetVoiceBusy=true;dmrFleetOwner=owner;dmrFleetLease=CallKey(call);dmrFleetAssigned=DateTimeOffset.UtcNow;dmrFleetCall=call;dmrFleetAudioReported=false;lock(owner.dmrTrunkRadio.Sync)dmrFleetCallLabel=owner.dmrTrunkRadio.Model.Labels.GetValueOrDefault(call.Talkgroup,"");
  RecordDmrFleetEvent(owner,call,"Assignment requested","Preparing voice receiver; tuning not yet confirmed");try{StopActivity("Trunk voice assignment");StopFolderScan("Trunk voice assignment");P25Packets.VoiceToggle.IsChecked=false;digitalMode.SelectedIndex=0;Aviation.Stop("Trunk voice assignment");HdRecordings.StopLive("Trunk voice assignment");Television.StopLive("Trunk voice assignment");var channel=new ChannelSettings(call.Hz,"NFM",call.Protocol=="P25"?16000:12500);if(!receiver.CanTune(call.Hz)||channel.Width>receiver.AppliedBandwidth)throw new IOException("Assigned voice channel exceeds this receiver's coverage.");if(channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))receiver.SetChannel(channel);else await receiver.Retune(call.Hz,channel);for(int n=0;n<250&&dmrFleetLeaseEpoch==reservation&&dmrFleetOwner==owner&&(receiver.AppliedChannel?.Frequency!=call.Hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth));n++)await Task.Delay(20);if(dmrFleetLeaseEpoch!=reservation||dmrFleetOwner!=owner||closing||owner.closing)return;if(receiver.AppliedChannel?.Frequency!=call.Hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))throw new IOException("Receiver did not report the assigned capture within five seconds.");engine.RetuneBoundary();dmrTrunkAudioKey="";dmrTrunkRadio.StartExternal(call,owner.fleetProfile.Name);lock(dmrTrunkRadio.Sync){dmrTrunkRadio.Model.HangSeconds=owner.dmrTrunkRadio.Model.HangSeconds;dmrTrunkRadio.Model.MaximumSeconds=owner.dmrTrunkRadio.Model.MaximumSeconds;dmrTrunkRadio.Model.Labels=new(owner.dmrTrunkRadio.Model.Labels);}RecordDmrFleetEvent(owner,call,"Tuned","Receiver reported assigned frequency and capture; awaiting matching decoded voice");dmrTrunkStatus.Text=$"Voice assigned by {owner.fleetProfile.Name}: TG {call.Talkgroup}";}catch(Exception ex){if(reservation==dmrFleetLeaseEpoch){RecordDmrFleetEvent(owner,call,"Assignment failed",ex.Message);EndDmrFleetVoice("Voice assignment failed: "+ex.Message);fleetPaused=true;}}finally{if(reservation==dmrFleetLeaseEpoch)dmrFleetVoiceBusy=false;}
 }
 private bool DmrFleetControlActive=>fleetProfile?.Role=="Trunk control"&&fleetProfile.Group.Length>0&&dmrTrunkRadio.Running&&dmrFleetOwner==null;
 private void TickDmrFleet()
 {
  if(dmrFleetOwner!=null){if(dmrFleetOwner.closing||!dmrFleetOwner.DmrFleetControlActive||dmrFleetOwner.fleetProfile.Group!=fleetProfile.Group||!rfMode||receiver.IsReplay){EndDmrFleetVoice("Assignment released");return;}bool ended;lock(dmrTrunkRadio.Sync)ended=!dmrTrunkRadio.Running||dmrTrunkRadio.Model.Active==null;if(!dmrFleetVoiceBusy&&ended)EndDmrFleetVoice("Voice call ended",true);return;}
  if(!DmrFleetControlActive){if(dmrFleetLeases.Count>0)ReleaseDmrFleetAssignments();return;}
  if(dmrFleetScope!=dmrTrunkRadio.Model.ScopeVersion){ReleaseDmrFleetAssignments();dmrFleetScope=dmrTrunkRadio.Model.ScopeVersion;}
  var now=DateTimeOffset.UtcNow;
  foreach(var pair in dmrFleetLeases.ToArray()){
   var w=pair.Key;if(w.closing||w.dmrFleetOwner!=this){dmrFleetLeases.Remove(w);continue;}
   bool allowed;double maximum;lock(dmrTrunkRadio.Sync){allowed=w.dmrFleetCall is {} active&&dmrTrunkRadio.Model.AllowsTalkgroup(active.Talkgroup);maximum=dmrTrunkRadio.Model.MaximumSeconds;}
   if(!allowed)w.EndDmrFleetVoice("Control talkgroup filter changed",true);
   else if((now-w.dmrFleetAssigned).TotalSeconds>=maximum)w.EndDmrFleetVoice("Maximum call timer",true);
  }
  // Take the available-call snapshot after retiring released leases. A prior
  // snapshot can still contain the just-ended call and immediately redispatch it.
  TrunkCall[] calls;lock(dmrTrunkRadio.Sync)calls=dmrTrunkRadio.Model.AvailableCalls(now);
  var assigned=dmrFleetLeases.Values.ToHashSet();foreach(var call in calls){string key=CallKey(call);if(assigned.Contains(key))continue;var target=FleetWindows().Where(w=>w!=this&&w.fleetProfile.Role=="Trunk voice"&&w.fleetProfile.Group==fleetProfile.Group&&w.dmrFleetOwner==null&&w.fleetOwner==null&&!w.fleetVoiceBusy&&!w.dmrFleetVoiceBusy&&!w.fleetPaused&&w.rfMode&&!w.receiver.IsReplay&&!w.dmrTrunkRadio.Running&&!w.trunkRadio.Running&&w.receiver.SamplingMode==0&&w.receiver.CanTune(call.Hz)&&w.receiver.AppliedBandwidth>=(call.Protocol=="P25"?16000:12500)).OrderBy(w=>w.fleetProfile.Priority).ThenBy(w=>w.workspaceSlot).FirstOrDefault();if(target==null)continue;dmrFleetLeases[target]=key;assigned.Add(key);_=target.BeginDmrFleetVoice(this,call);}
 }
}
