using System.IO;
using System.Text.Json;
namespace MRS;
internal static class PagingManagementTests
{
    internal static void Run(Action<bool,string> check)
    {
        var a=new PacketObservation(DateTimeOffset.UtcNow,"Synthetic",false,929937500,null,"K checksum passed","","1001","","alphanumeric · complete","frame details",null,"","FLEX",true,SourceKey:"test",DisplaySummary:"Synthetic equipment alert");
        var b=a with{Destination="1002",Kind="alphanumeric · reassembled",ObservedUtc=a.ObservedUtc.AddSeconds(2)};
        check(PagingRules.GroupKey(a)==PagingRules.GroupKey(b),"Paging groups identical complete/reassembled payloads across recipient capcodes");
        check(PagingRules.GroupKey(a)!=PagingRules.GroupKey(a with{FrequencyHz=930000000}) && PagingRules.GroupKey(a)!=PagingRules.GroupKey(a with{SourceKey="other"}) && PagingRules.GroupKey(a)!=PagingRules.GroupKey(a with{Replay=true}),"Paging grouping preserves frequency, receiver session and replay boundaries");
        check(PagingRules.GroupKey(a with{Kind="alphanumeric · fragment_start"})==null && PagingRules.GroupKey(a with{Validated=false})==null,"Incomplete fragments and failed validation remain individual observations");
        var panel=new PacketPanel();panel.Add(a);panel.Add(b);
        using(var doc=JsonDocument.Parse(panel.ExportJson()))check(panel.VisibleCount==1 && doc.RootElement[0].GetProperty("ReceptionCount").GetInt64()==2 && doc.RootElement[0].GetProperty("PagingRecipients").GetArrayLength()==2,"Grouped paging exports count and all recipient capcodes");
        panel.SetPagingRules([new("FLEX",a.FrequencyHz,"Capcode","1001","Equipment"),new("FLEX",a.FrequencyHz,"Capcode","1002","Maintenance")]);panel.SetFilter("Equipment");
        check(panel.VisibleCount==1 && panel.ExportJson().Contains("Maintenance"),"Paging aliases remain searchable across grouped recipients and preserve original addresses");panel.SetFilter("");
        panel.SetPagingRules([new("FLEX",a.FrequencyHz,"Capcode","1001","Equipment",true)]);
        using(var doc=JsonDocument.Parse(panel.ExportJson()))check(panel.VisibleCount==1 && doc.RootElement[0].GetProperty("Destination").GetString()=="1002" && doc.RootElement[0].GetProperty("ReceptionCount").GetInt64()==1,"Ignoring one recipient preserves nonignored duplicate recipients without inflating counts");
        panel.SetPagingRules([new("FLEX",a.FrequencyHz,"Message",a.Description,"",true)]);check(panel.VisibleCount==0,"Exact-message ignore hides all matching recipient copies");
        panel.IncludeIgnored(true);check(panel.VisibleCount==1,"Show ignored restores grouped observations for inspection");
        panel.SetPagingRules([]);panel.IncludeIgnored(false);panel.PagingGrouped=false;check(panel.VisibleCount==2,"Disabling paging grouping restores individual recent receptions independently of P25");
        string dir=Path.Combine(Path.GetTempPath(),"mrs-paging-rules-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"rules.json");
        try
        {
            var rules=new PagingRules();rules.Load(path);rules.Save([new("FLEX",a.FrequencyHz,"Capcode","1001","Equipment",true)]);var reopened=new PagingRules();reopened.Load(path);
            check(reopened.Ignored(a) && reopened.Apply(a).Label=="Equipment" && !reopened.Ignored(a with{FrequencyHz=930000000}) && !reopened.Ignored(a with{Protocol="POCSAG"}),"Paging label/ignore persistence is scoped to frequency and protocol");
            File.WriteAllText(path,"broken");var broken=new PagingRules();try{broken.Load(path);}catch{}bool refused=false;try{broken.Save([]);}catch(IOException){refused=true;}
            check(refused && File.ReadAllText(path)=="broken","Unreadable paging rules are preserved rather than overwritten");
        }
        finally{Directory.Delete(dir,true);}
    }
}
