using System.IO;
namespace MRS;
public partial class MainWindow
{
    private async Task RenderVoiceIdSmoke(Action<string> capture)
    {
        Stop();AnalysisPackets.ClearRows();P25Packets.VoiceToggle.IsChecked=true;Packets.CwToggle.IsChecked=true;
        if(!receiver.CaptureData||!voiceIds.Voice||!voiceIds.Cw)throw new InvalidOperationException("Voice/CW toggles not connected");
        var context=new PacketContext("synthetic-voice-smoke","Synthetic test",true,160230000,0);
        var signal=VoiceIdTests.Morse("KE8VPC",20);
        for(int i=0;i<signal.Length;i+=960){voiceIds.Feed(signal.Skip(i).Take(960).ToArray(),context,false);await Task.Delay(3);}
        for(int i=0;i<100;i++){await Task.Delay(20);RefreshPackets();if(AnalysisPackets.ExportJson().Contains("KE8VPC"))break;}
        if(!AnalysisPackets.ExportJson().Contains("KE8VPC"))throw new InvalidOperationException("CW worker candidate missing from Data");
        SelectWorkspace(ObservationsTab);AnalysisPackets.SelectFirst();UpdateLayout();capture("ui-cw-id.png");
        // Persist only generated fixtures in the isolated smoke profile.
        var symbols=VoiceFixtures.Hdu.Concat(VoiceFixtures.Ldu1).Concat(VoiceFixtures.Ldu2).ToArray();
        PacketTests.WriteIq(Path.Combine(dataFolder,"synthetic-p25-voice.mrsiq"),P25Tests.Iq(3,symbols:symbols),frequency:160230000);
        var pcm=signal.SelectMany(v=>BitConverter.GetBytes((short)Math.Clamp(v,short.MinValue,short.MaxValue))).ToArray();
        using(var wav=new NAudio.Wave.WaveFileWriter(Path.Combine(dataFolder,"synthetic-cw-id.wav"),AudioEngine.Format))wav.Write(pcm,0,pcm.Length);
        // Exercise the receiver -> discriminator -> asynchronous voice -> audio engine path.
        PlayIq(Path.Combine(dataFolder,"synthetic-p25-voice.mrsiq"));
        bool decoded=false;
        for(int i=0;i<200;i++){await Task.Delay(20);if(voiceIds.Status.Contains("voice frames")){decoded=true;break;}}
        if(!decoded)throw new InvalidOperationException("P25 I/Q replay did not reach voice synthesis: "+voiceIds.Status);
        RefreshPackets();SelectWorkspace(P25SystemsTab);UpdateLayout();capture("ui-p25-voice.png");
        Stop();P25Packets.VoiceToggle.IsChecked=false;Packets.CwToggle.IsChecked=false;AnalysisPackets.ClearRows();
    }
}
