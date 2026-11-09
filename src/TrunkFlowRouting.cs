using System.IO;
namespace MRS;
public partial class MainWindow
{
 private bool visibleDmrFlow;
 private long FollowingAudioHz=>trunkRadio.Running&&trunkAudioFrequency>0?trunkAudioFrequency:dmrTrunkRadio.Running&&dmrTrunkAudioFrequency>0?dmrTrunkAudioFrequency:receiver.AppliedChannel?.Frequency??0;
 private void StopAllTrunk(){StopTrunk(false);StopDmrTrunk(false);}
 private void ShowTrunkChannel(bool dmr,ChannelSettings channel){if(trunkRadio.Running&&dmrTrunkRadio.Running&&dmr!=visibleDmrFlow)return;Spectrum.SetChannel(channel);}
 private void CheckConcurrentFlow(bool dmr,long hz,double width){var other=dmr?trunkRadio:dmrTrunkRadio;if(!other.Running)return;if(fleetProfile.Role!="Trunk control")throw new IOException("Use a Trunk control receiver and dedicated voice receivers for concurrent P25/DMR flows.");if(!new ChannelSettings(hz,"NFM",width).Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))throw new IOException("Both control channels must fit this capture. Use another control receiver for this frequency.");}
}
