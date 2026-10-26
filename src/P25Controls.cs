using System.IO;
using System.Text.Json;
namespace MRS;
public partial class MainWindow
{
    private async Task RenderP25Smoke(Action<string> capture)
    {
        Stop();P25Packets.Grouped=false;P25Packets.DecoderEnabled=false;P25Packets.P25Enabled=true;p25Decoder.Clear();P25Packets.ClearRows();P25Packets.SetFilter("");P25Packets.IncludeFailed(false);
        string path=Path.Combine(dataFolder,"synthetic-p25-control.mrsiq");
        PacketTests.WriteIq(path,P25Tests.Iq(8),frequency:774000000);
        PlayIq(path);SelectWorkspace(P25SystemsTab);
        // Decoder must remain independent of listening volume and squelch.
        receiver.SquelchEnabled=true;receiver.SquelchThreshold=0;
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        if(p25Decoder.Counts.Messages!=24 || p25Decoder.Counts.Headers!=8 || P25Packets.VisibleCount!=32)
            throw new InvalidOperationException($"P25 replay counts failed: {p25Decoder.Counts}, visible {P25Packets.VisibleCount}.");
        if(Packets.ExportJson().Contains("P25 Phase I"))throw new InvalidOperationException("P25 observations leaked into general Data");
        P25Packets.SetFilter("WACN");
        using(var json=JsonDocument.Parse(P25Packets.ExportJson()))
        {
            if(json.RootElement.GetArrayLength()!=8 || json.RootElement.EnumerateArray().Any(r=>!r.GetProperty("Replay").GetBoolean()||r.GetProperty("FrequencyHz").GetInt64()!=774000000||r.GetProperty("Protocol").GetString()!="P25 Phase I"))
                throw new InvalidOperationException("P25 Data filter/export lost protocol or replay source.");
        }
        P25Packets.SetFilter("");P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-data.png");
        P25Packets.Grouped=true;P25Packets.SetFilter("WACN");
        using(var json=JsonDocument.Parse(P25Packets.ExportJson()))
            if(P25Packets.VisibleCount!=1||json.RootElement[0].GetProperty("ReceptionCount").GetInt64()!=8)throw new InvalidOperationException("P25 summary did not combine repeated network status.");
        P25Packets.SetFilter("");P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-summary.png");
        double priorWidth=Width,priorHeight=Height;Width=MinWidth;Height=MinHeight;UpdateLayout();capture("ui-p25-summary-compact.png");Width=priorWidth;Height=priorHeight;UpdateLayout();
        Stop();p25Decoder.Clear();P25Packets.ClearRows();P25Packets.HiddenP25Types=[];
        string channelsPath=Path.Combine(dataFolder,"synthetic-p25-channels.mrsiq");
        PacketTests.WriteIq(channelsPath,P25Tests.Iq(4,symbols:P25ChannelFixtures.Symbols),frequency:774000000);
        PlayIq(channelsPath);
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        using(var channelJson=JsonDocument.Parse(P25Packets.ExportJson()))
        {
            if(!channelJson.RootElement.EnumerateArray().Any(r=>r.GetProperty("P25Channel").ValueKind==JsonValueKind.Object && r.GetProperty("P25Channel").GetProperty("DownlinkHz").GetInt64()==762025000))
                throw new InvalidOperationException("P25 identifier I/Q replay did not resolve TDMA carrier.");
        }
        P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-channels.png");
        P25Packets.HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update"];
        UpdateLayout();if(P25Packets.VisibleCount!=3)throw new InvalidOperationException("P25 grants/status filter should show three rows.");
        P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-channel-activity.png");P25Packets.HiddenP25Types=[];
        Stop();p25Decoder.Clear();P25Packets.ClearRows();
        string controlPath=Path.Combine(dataFolder,"synthetic-p25-expanded-control.mrsiq");
        PacketTests.WriteIq(controlPath,P25Tests.Iq(3,symbols:P25ControlFixtures.Symbols),frequency:774000000);
        PlayIq(controlPath);
        for(int n=0;n<500 && !receiver.ReplayFinished;n++)await Task.Delay(20);
        await Task.Delay(100);RefreshPackets();
        using(var controlJson=JsonDocument.Parse(P25Packets.ExportJson()))
        {
            var dual=controlJson.RootElement.EnumerateArray().Single(r=>r.GetProperty("Kind").GetString()=="Group voice grant update");
            if(dual.GetProperty("P25Assignments").GetArrayLength()!=2||dual.GetProperty("ReceptionCount").GetInt64()!=3)
                throw new InvalidOperationException("Expanded P25 control replay lost dual-grant payload or repeat count.");
        }
        P25Packets.HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update","Secondary control channels","Adjacent site status"];
        if(P25Packets.VisibleCount!=4)throw new InvalidOperationException("Expanded P25 activity view should show three grant formats and site status.");
        P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-expanded-activity.png");
        P25Packets.HiddenP25Types=[];P25Packets.SetFilter("RFSS");P25Packets.SelectFirst();UpdateLayout();capture("ui-p25-site-announcements.png");
        P25Packets.SetFilter("");
        if(P25Systems.ReplayCount==0||P25Systems.LiveCount!=0)throw new InvalidOperationException("P25 replay polluted live discovery storage or produced no discoveries.");
        SelectWorkspace(P25SystemsTab);P25Systems.ShowReplay();UpdateLayout();await Task.Delay(100);P25Systems.AssertReadable();P25Systems.CheckActions();capture("ui-p25-systems.png");
        Stop();P25Packets.P25Enabled=false;WorkspaceTabs.SelectedIndex=0;
    }
}






