namespace MRS;
public partial class PacketPanel
{
    internal System.Windows.Controls.CheckBox VoiceToggle=>VoiceCheck;
    internal System.Windows.Controls.CheckBox CwToggle=>CwCheck;
    internal void SetVoiceStatus(string text)=>VoiceIdStatus.Text=text;
}
public partial class MainWindow
{
    private readonly VoiceIdPipeline voiceIds=new();
    private void InitializeVoiceIds()
    {
        InitializeDigitalVoice();
        void Changed(){if(P25Packets.VoiceToggle.IsChecked==true&&trunkRadio.Running)StopTrunk(false);voiceIds.Voice=P25Packets.VoiceToggle.IsChecked==true;voiceIds.Cw=Packets.CwToggle.IsChecked==true;UpdateDataCapture();engine.RetuneBoundary();}
        P25Packets.VoiceToggle.Checked+=(_,_)=>Changed();P25Packets.VoiceToggle.Unchecked+=(_,_)=>Changed();
        Packets.CwToggle.Checked+=(_,_)=>Changed();Packets.CwToggle.Unchecked+=(_,_)=>Changed();
        voiceIds.Audio+=pcm=>engine.FeedStereo(VoiceAudioGain.Apply(pcm,voiceGainDb),true);
        void ResetVoice(){voiceIds.Reset();engine.RetuneBoundary();}
        receiver.ChannelChanged+=ResetVoice;receiver.ReplayGap+=ResetVoice;receiver.Ended+=_=>ResetVoice();
        receiver.Audio+=pcm=>{
            if(!voiceIds.Cw||receiver.AppliedChannel is not {} channel||channel.Mode is not ("NFM" or "CW"))return;
            var data=new double[pcm.Length/2];for(int i=0;i<data.Length;i++)data[i]=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i*2,2));
            voiceIds.Feed(data,VoiceContext(channel.Frequency),false);
        };
    }
    private PacketContext VoiceContext(long hz)=>new($"voice-id:{Interlocked.Read(ref session)}:{hz}",$"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q replay":receiver.ReceiverName),receiver.IsReplay,hz,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null);
    private void UpdateDataCapture()=>receiver.CaptureData=Dmr.Enabled||p25Decoder.Enabled||pagingDecoder.Enabled||subaudibleDecoder.Enabled||voiceIds.Voice||digitalVoice.Enabled||signalingData.Enabled||aniData.Enabled;
}
