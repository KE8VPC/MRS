using System.IO;
using System.Text.Json;
using System.Windows;
namespace MRS;
public partial class MainWindow
{
    private void RefreshSubaudible()
    {
        bool enabled=subaudibleDecoder.Enabled;
        string status=!enabled?"CTCSS / DCS off":!engine.Active?"CTCSS / DCS · waiting for a source":
            rfMode&&receiver.ReplayFinished?"CTCSS / DCS · replay finished":
            rfMode&&receiver.ReplayPaused?"CTCSS / DCS · replay paused":
            rfMode&&receiver.AppliedChannel?.Mode!="NFM"?"CTCSS / DCS · select NFM":
            subaudibleDecoder.Current is {} signal?$"{signal.Protocol} · {signal.Value}"+(signal.Protocol=="DCS"?" · equivalent N/I names":" · PL tone"):
            "CTCSS / DCS · searching";
        if(enabled && !rfMode)status+=" · audio must retain subaudible frequencies";
        Packets.SetSubaudibleStatus(status);SubaudibleLabel.Text=status;
        SubaudiblePanel.Visibility=enabled?Visibility.Visible:Visibility.Collapsed;
    }
    private async Task RenderSubaudibleSmoke(Action<string> capture)
    {
        Stop();Packets.DecoderEnabled=false;Packets.P25Enabled=true;Packets.SubaudibleEnabled=true;
        Packets.P25Enabled=false;
        if(!receiver.CaptureData)throw new InvalidOperationException("Disabling P25 disabled the subaudible discriminator.");
        subaudibleDecoder.Clear();Packets.ClearRows();Packets.SetFilter("");
        string wav=Path.Combine(dataFolder,"synthetic-ctcss.wav"),iq=Path.Combine(dataFolder,"synthetic-dcs.mrsiq");
        using(var writer=new NAudio.Wave.WaveFileWriter(wav,AudioEngine.Format))
        {byte[] pcm=SubaudibleTests.Pcm(SubaudibleTests.Tone(100,seconds:4));writer.Write(pcm,0,pcm.Length);}
        PlayFile(wav,true);WorkspaceTabs.SelectedIndex=0;
        for(int n=0;n<250 && subaudibleDecoder.Current==null;n++)await Task.Delay(20);
        RefreshPackets();
        if(subaudibleDecoder.Current?.Value!="100.0 Hz" || !SubaudibleLabel.Text.Contains("100.0 Hz"))throw new InvalidOperationException("CTCSS WAV detection/readout failed.");
        UpdateLayout();capture("ui-ctcss-spectrum.png");Stop();
        if(subaudibleDecoder.Current!=null)throw new InvalidOperationException("Stopping retained a live tone.");
        subaudibleDecoder.Clear();Packets.ClearRows();
        PacketTests.WriteIq(iq,PacketTests.Iq(SubaudibleTests.Pcm(SubaudibleTests.Dcs(0x763813,seconds:5)),240000),frequency:446000000);
        PlayIq(iq);receiver.SquelchEnabled=true;receiver.SquelchThreshold=0;WorkspaceTabs.SelectedItem=PacketsTab;
        for(int n=0;n<250 && subaudibleDecoder.Current==null;n++)await Task.Delay(20);
        RefreshPackets();
        if(subaudibleDecoder.Current?.Value!="023N / 047I")throw new InvalidOperationException("DCS I/Q replay failed behind closed listening squelch.");
        Packets.SetFilter("DCS");
        using(var json=JsonDocument.Parse(Packets.ExportJson()))
            if(json.RootElement.GetArrayLength()!=1 || !json.RootElement[0].GetProperty("Replay").GetBoolean() || json.RootElement[0].GetProperty("FrequencyHz").GetInt64()!=446000000)
                throw new InvalidOperationException("DCS history/export lost replay or frequency attribution.");
        Packets.SelectFirst();UpdateLayout();capture("ui-dcs-data.png");
        receiver.ReplayPaused=true;RefreshSubaudible();
        if(!SubaudibleLabel.Text.Contains("paused"))throw new InvalidOperationException("Paused DCS replay appeared live.");
        double previousWidth=Width,previousHeight=Height;Width=MinWidth;Height=MinHeight;UpdateLayout();capture("ui-dcs-compact.png");Width=previousWidth;Height=previousHeight;UpdateLayout();
        Stop();Packets.SetFilter("");Packets.SubaudibleEnabled=false;Packets.P25Enabled=true;
        if(!receiver.CaptureData)throw new InvalidOperationException("P25 did not retain discriminator access.");
        Packets.P25Enabled=false;WorkspaceTabs.SelectedIndex=0;
    }
}
