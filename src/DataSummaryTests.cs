using System.Text.Json;
namespace MRS;
internal static class DataSummaryTests
{
    internal static void Run(Action<bool,string> check)
    {
        var dmrPanel=new PacketPanel();
        var dmr=new PacketObservation(DateTimeOffset.UtcNow,"Receiver A",false,451600000,null,"Slot Golay + BPTC valid","","","","Slot identification","CC 0 slot 1 data type 3",null,"ABCD","DMR",true);
        for(int i=0;i<10000;i++)dmrPanel.Add(dmr with{RawHex=i%2==0?"ABCD":"ABCE"});
        using(var report=JsonDocument.Parse(dmrPanel.ExportJson()))check(dmrPanel.VisibleCount==1&&dmrPanel.Count==500&&report.RootElement[0].GetProperty("ReceptionCount").GetInt64()==10000,"DMR flood keeps one stable row, full repeat count and bounded raw history");
        dmrPanel.Add(dmr with{FrequencyHz=452000000});dmrPanel.Add(dmr with{Replay=true});check(dmrPanel.VisibleCount==3,"DMR grouping separates frequency and live/replay");
        dmrPanel.DmrGrouped=false;check(dmrPanel.VisibleCount==500,"Ungrouped DMR exposes bounded individual history");
        var panel=new PacketPanel();var start=DateTimeOffset.Parse("2026-09-30T12:00:00Z");
        var site=new PacketObservation(start,"Receiver A",false,774000000,null,"TSBK CRC valid","NAC 345","","","Site status","System 348 · RFSS 1 · Site 22 · control channel ID 17B0",null,"3A00010123011617B01234","P25 Phase I",true);
        for(int n=0;n<1000;n++)panel.Add(site with{ObservedUtc=start.AddSeconds(n)});
        using(var json=JsonDocument.Parse(panel.ExportJson()))
            check(panel.VisibleCount==1&&panel.Count==500&&json.RootElement[0].GetProperty("ReceptionCount").GetInt64()==1000&&json.RootElement[0].GetProperty("FirstSeenUtc").GetDateTimeOffset()==start,"P25 repeats retain one fixed summary row and first-seen time while raw history remains bounded");
        panel.Add(site with{ObservedUtc=start.AddSeconds(1000),Information="System 348 · RFSS 1 · Site 22 · control channel ID 17B1",RawHex="3A00010123011617B15678"});
        using(var json=JsonDocument.Parse(panel.ExportJson()))
            check(panel.VisibleCount==1&&json.RootElement[0].GetProperty("Information").GetString()!.Contains("17B1")&&json.RootElement[0].GetProperty("ContentChanges").GetInt64()==1,"Changed P25 site fields update the current row and increment the content-change counter");
        panel.SetFilter("17B1");check(panel.VisibleCount==1,"Changed summary content is searchable");
        panel.Add(site with{ObservedUtc=start.AddSeconds(1001)});check(panel.VisibleCount==0,"Filtered summary disappears when its updated content no longer matches");panel.SetFilter("");
        panel.Add(site with{FrequencyHz=775000000});panel.Add(site with{Source="Receiver B"});panel.Add(site with{Replay=true});panel.Add(site with{Callsign="NAC 346"});
        check(panel.VisibleCount==5,"P25 summary separates frequency, receiver, replay and NAC");
        var unknown=site with{Kind="Manufacturer TSBK",Information="Opcode 09 · MFID 90 · unimplemented manufacturer format",RawHex="099001"};
        panel.Add(unknown);panel.Add(unknown);panel.Add(unknown with{RawHex="099002"});
        check(panel.VisibleCount==6,"Unknown P25 payload variants update one category row");
        var grant=site with{Kind="Group voice grant",Information="Talkgroup 123 · Source 456 · channel ID 17B0",RawHex="000001"};
        panel.Add(grant);panel.Add(grant);panel.Add(grant with{Information="Talkgroup 124 · Source 789 · channel ID 17B0",RawHex="000002"});
        check(panel.VisibleCount==8,"Identical P25 grants are counted while changed activity stays separate");
        panel.Add(site with{Protocol="AX.25"});panel.Add(site with{Protocol="AX.25"});check(panel.VisibleCount==10,"Non-P25 observations retain separate history rows");
        using(var variants=JsonDocument.Parse(panel.ExportJson()))check(variants.RootElement.EnumerateArray().Single(r=>r.GetProperty("Kind").GetString()=="Manufacturer TSBK").GetProperty("ReceptionCount").GetInt64()==3,"Category grouping counts all payload variants");
        panel.Grouped=false;check(panel.VisibleCount==500,"Individual-message view retains the latest 500 observations");
        panel.Grouped=true;check(panel.VisibleCount==10,"Switching back restores summary counts without reconstructing them from truncated history");
        panel.ClearRows();panel.Add(site);
        for(int n=0;n<550;n++)panel.Add(grant with{RawHex=n.ToString("X6")});
        panel.SetFilter("Site 22");check(panel.VisibleCount==1,"Active status summary survives activity-row eviction");panel.SetFilter("");
        check(panel.VisibleCount==500,"Summary storage remains bounded even with changing payloads");
        panel.ClearRows();panel.Add(site);
        using(var json=JsonDocument.Parse(panel.ExportJson()))check(panel.VisibleCount==1&&json.RootElement[0].GetProperty("ReceptionCount").GetInt64()==1,"Clear resets both raw history and summary counters");
        panel.Add(site with{SourceKey="session-2"});check(panel.VisibleCount==2,"Different source sessions retain distinct summary rows");
        panel.ClearRows();
        var header=site with{Kind="NID header",Information="NAC 345 · DUID 7 · header verified; payload not decoded"};
        var tsbk=site with{Kind="TSBK",Information="NAC 345 · Opcode 30 · MFID 00 · CRC verified; message fields not implemented"};
        for(int n=0;n<1000;n++){
            panel.Add(header with{RawHex=n.ToString("X16"),Validation=$"NID BCH valid ({n%5} corrected bits); header only"});
            panel.Add(tsbk with{RawHex=n.ToString("X24")});
        }
        check(panel.VisibleCount==2&&panel.Count==500,"Changing header bits and undecoded TSBK payloads do not flood grouped view");
        using(var json=JsonDocument.Parse(panel.ExportJson()))check(json.RootElement.EnumerateArray().All(r=>r.GetProperty("ReceptionCount").GetInt64()==1000&&r.GetProperty("ContentChanges").GetInt64()==999),"Category rows preserve reception and payload-change counts");
        panel.Add(tsbk with{Information="NAC 345 · Opcode 31 · MFID 00 · CRC verified; message fields not implemented"});
        panel.Add(header with{Information="NAC 345 · DUID 5 · header verified; payload not decoded"});
        check(panel.VisibleCount==4,"Different opcodes and DUIDs remain separate");
        panel.Grouped=false;check(panel.VisibleCount==500,"Individual view retains changing raw observations");        panel.ClearRows();panel.Grouped=true;panel.Add(header);panel.Add(tsbk);panel.Add(grant);panel.Add(site);
        panel.HiddenP25Types=["NID header","TSBK"];
        check(panel.VisibleCount==2&&panel.Count==4,"P25 type filters hide header/TSBK but retain grant/status and raw history");
        panel.Add(header);panel.Add(tsbk);
        using(var json=JsonDocument.Parse(panel.ExportJson()))check(json.RootElement.GetArrayLength()==2,"Export respects message type filters");
        panel.Grouped=false;check(panel.VisibleCount==2,"Type filters apply to individual messages too");
        panel.Add(header with{Protocol="Other protocol"});check(panel.VisibleCount==3,"P25 filters do not hide another protocol with the same kind");
        panel.HiddenP25Types=[];check(panel.VisibleCount==7,"Show all restores hidden recent history");
        panel.Grouped=true;check(panel.VisibleCount==5,"Hidden rows continue accumulating summary counts");    }
}


