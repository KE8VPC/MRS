using System.IO;
using System.Text.Json;
namespace MRS;
public partial class MainWindow
{
    private async Task RenderP25Smoke(Action<string> capture)
    {
        Stop();Packets.Grouped=false;Packets.DecoderEnabled=false;Packets.P25Enabled=true;p25Decoder.Clear();Packets.ClearRows();Packets.SetFilter("");Packets.IncludeFailed(false);
        string path=Path.Combine(dataFolder,"synthetic-p25-control.mrsiq");
        PacketTests.WriteIq(path,P25Tests.Iq(8),frequency:774000000);
        PlayIq(path);WorkspaceTabs.SelectedItem=PacketsTab;
        // Decoder must remain independent of listening volume and squelch.
        receiver.SquelchEnabled=true;receiver.SquelchThreshold=0;
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        if(p25Decoder.Counts.Messages!=24 || p25Decoder.Counts.Headers!=8 || Packets.VisibleCount!=32)
            throw new InvalidOperationException($"P25 replay counts failed: {p25Decoder.Counts}, visible {Packets.VisibleCount}.");
        Packets.SetFilter("WACN");
        using(var json=JsonDocument.Parse(Packets.ExportJson()))
        {
            if(json.RootElement.GetArrayLength()!=8 || json.RootElement.EnumerateArray().Any(r=>!r.GetProperty("Replay").GetBoolean()||r.GetProperty("FrequencyHz").GetInt64()!=774000000||r.GetProperty("Protocol").GetString()!="P25 Phase I"))
                throw new InvalidOperationException("P25 Data filter/export lost protocol or replay source.");
        }
        Packets.SetFilter("");Packets.SelectFirst();UpdateLayout();capture("ui-p25-data.png");
        Packets.Grouped=true;Packets.SetFilter("WACN");
        using(var json=JsonDocument.Parse(Packets.ExportJson()))
            if(Packets.VisibleCount!=1||json.RootElement[0].GetProperty("ReceptionCount").GetInt64()!=8)throw new InvalidOperationException("P25 summary did not combine repeated network status.");
        Packets.SetFilter("");Packets.SelectFirst();UpdateLayout();capture("ui-p25-summary.png");
        double priorWidth=Width,priorHeight=Height;Width=MinWidth;Height=MinHeight;UpdateLayout();capture("ui-p25-summary-compact.png");Width=priorWidth;Height=priorHeight;UpdateLayout();
        Stop();p25Decoder.Clear();Packets.ClearRows();Packets.HiddenP25Types=[];
        string channelsPath=Path.Combine(dataFolder,"synthetic-p25-channels.mrsiq");
        PacketTests.WriteIq(channelsPath,P25Tests.Iq(4,symbols:P25ChannelFixtures.Symbols),frequency:774000000);
        PlayIq(channelsPath);
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        using(var channelJson=JsonDocument.Parse(Packets.ExportJson()))
        {
            if(!channelJson.RootElement.EnumerateArray().Any(r=>r.GetProperty("P25Channel").ValueKind==JsonValueKind.Object && r.GetProperty("P25Channel").GetProperty("DownlinkHz").GetInt64()==762025000))
                throw new InvalidOperationException("P25 identifier I/Q replay did not resolve TDMA carrier.");
        }
        Packets.SelectFirst();UpdateLayout();capture("ui-p25-channels.png");
        Packets.HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update"];
        UpdateLayout();if(Packets.VisibleCount!=3)throw new InvalidOperationException("P25 grants/status filter should show three rows.");
        Packets.SelectFirst();UpdateLayout();capture("ui-p25-channel-activity.png");Packets.HiddenP25Types=[];
        Stop();p25Decoder.Clear();Packets.ClearRows();
        string controlPath=Path.Combine(dataFolder,"synthetic-p25-expanded-control.mrsiq");
        PacketTests.WriteIq(controlPath,P25Tests.Iq(3,symbols:P25ControlFixtures.Symbols),frequency:774000000);
        PlayIq(controlPath);
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        using(var controlJson=JsonDocument.Parse(Packets.ExportJson()))
        {
            var dual=controlJson.RootElement.EnumerateArray().Single(r=>r.GetProperty("Kind").GetString()=="Group voice grant update");
            if(dual.GetProperty("P25Assignments").GetArrayLength()!=2||dual.GetProperty("ReceptionCount").GetInt64()!=3)
                throw new InvalidOperationException("Expanded P25 control replay lost dual-grant payload or repeat count.");
        }
        Packets.HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update","Secondary control channels","Adjacent site status"];
        if(Packets.VisibleCount!=4)throw new InvalidOperationException("Expanded P25 activity view should show three grant formats and site status.");
        Packets.SelectFirst();UpdateLayout();capture("ui-p25-expanded-activity.png");
        Packets.HiddenP25Types=[];Packets.SetFilter("RFSS");Packets.SelectFirst();UpdateLayout();capture("ui-p25-site-announcements.png");
        Packets.SetFilter("");
        if(P25Systems.ReplayCount==0||P25Systems.LiveCount!=0)throw new InvalidOperationException("P25 replay polluted live discovery storage or produced no discoveries.");
        WorkspaceTabs.SelectedItem=P25SystemsTab;P25Systems.ShowReplay();UpdateLayout();await Task.Delay(100);P25Systems.AssertReadable();P25Systems.CheckActions();capture("ui-p25-systems.png");
        Stop();Packets.P25Enabled=false;WorkspaceTabs.SelectedIndex=0;
    }
}






