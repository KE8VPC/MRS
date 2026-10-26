namespace MRS;
internal static class DigitalVoiceTests
{
    internal static void Run(Action<bool,string> check)
    {
        var raw=DigitalVoicePipeline.InputPcm(new[]{double.NaN,double.PositiveInfinity,1e9,-1e9,1000});
        check(BitConverter.ToInt16(raw,0)==0&&BitConverter.ToInt16(raw,2)==0&&BitConverter.ToInt16(raw,4)==32767&&BitConverter.ToInt16(raw,6)==-32768&&BitConverter.ToInt16(raw,8)==2500,"Digital discriminator conversion bounds invalid and overrange values");
        var stereo=DigitalVoicePipeline.ExpandPcm(new byte[]{1,0,255,255},2);
        check(stereo.Length==24&&Enumerable.Range(0,6).All(i=>BitConverter.ToInt16(stereo,i*4)==1&&BitConverter.ToInt16(stereo,i*4+2)==-1),"DMR output preserves slot stereo and expands 8k to 48k");
        var mono=DigitalVoicePipeline.ExpandPcm(new byte[]{232,3},1);
        check(mono.Length==24&&Enumerable.Range(0,12).All(i=>BitConverter.ToInt16(mono,i*2)==1000),"NXDN mono duplicates into 48k stereo");
        using var pipeline=new DigitalVoicePipeline();pipeline.Configure(2,3,false);pipeline.Reset();check(pipeline.Enabled&&pipeline.Status.Contains("Waiting"),"Digital reset preserves selected mode");pipeline.Configure(0,3,false);check(!pipeline.Enabled&&pipeline.Status.StartsWith("Off"),"Digital voice explicitly disables without changing receiver");
    }
}
public partial class MainWindow
{
    private async Task RenderDigitalVoiceSmoke(Action<string> capture)
    {
        Stop();var context=new PacketContext("synthetic-digital-smoke","Synthetic test",true,450000000,0);
        for(int mode=1;mode<=3;mode++)
        {
            digitalMode.SelectedIndex=mode;
            if(!digitalVoice.Enabled||!receiver.CaptureData||voiceIds.Voice)throw new InvalidOperationException("Digital voice selection did not configure capture/exclusive audio");
            for(int i=0;i<30;i++){digitalVoice.Feed(new double[960],context);await Task.Delay(20);}
            for(int i=0;i<100&&!digitalVoice.Diagnostics.Contains("Audio In");i++)await Task.Delay(20);
            if(!digitalVoice.Diagnostics.Contains("Audio In"))throw new InvalidOperationException("Digital worker failed to start: "+digitalVoice.Status+" "+digitalVoice.Diagnostics);
            Packets.CwToggle.IsChecked=true;
            if(!voiceIds.Cw||!digitalVoice.Enabled)throw new InvalidOperationException("CW and digital voice cannot run together");
            RefreshDigitalVoice();SelectWorkspace(DmrTab);UpdateLayout();capture($"ui-digital-mode-{mode}.png");
            digitalVoice.Reset();if(digitalVoice.Diagnostics.Length!=0)throw new InvalidOperationException("Digital reset retained previous channel diagnostics");
        }
        digitalMode.SelectedIndex=0;Packets.CwToggle.IsChecked=false;
        if(digitalVoice.Enabled)throw new InvalidOperationException("Digital worker did not stop");
    }
}
