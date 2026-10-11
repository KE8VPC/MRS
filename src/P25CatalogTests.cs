using System.IO;
using System.Text.Json;
namespace MRS;
internal static class P25CatalogTests
{
    internal static void Run(Action<bool,string> check)
    {
        string directory=Path.Combine(Path.GetTempPath(),"mrs-p25-catalog-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            string path=Path.Combine(directory,"catalog.json");
            var pipeline=new P25Pipeline{Enabled=true};pipeline.Feed(P25Tests.Signal(symbols:P25ControlFixtures.Symbols),new("fixture","Receiver A",true,774000000));
            var observations=pipeline.Drain();
            check(observations.Last(r=>r.Kind=="Site status").P25Identity is {SystemId:0x123,RfssId:2,SiteId:3},"Pipeline exports current system/site identity independently of advertised neighbors");
            var identity=new P25Identity(0x293,0xABCDE,0x123,2,3);
            var now=DateTimeOffset.UtcNow;
            var grant=observations.First(r=>r.Kind=="Group voice grant update") with{Replay=false,P25Identity=identity,ObservedUtc=now};
            var catalog=new P25Catalog(path);
            catalog.Observe(grant);catalog.Observe(grant with{ObservedUtc=now.AddSeconds(1)});
            check(catalog.Rows.Count==2 && catalog.Rows.All(r=>r.Value.Receptions==2),"Persistent catalog tracks both grant assignments without duplicating repeated announcements");
            catalog.Save(true);var restored=new P25Catalog(path);
            check(restored.Rows.Count==2&&restored.Rows.All(r=>r.Value.FirstSeen==now&&r.Value.LastSeen==now.AddSeconds(1)),"Discovery save/reopen preserves first/last-seen times and counts");
            restored.Observe(grant with{SourceKey="new-session",ObservedUtc=now.AddSeconds(2)});restored.Save(true);
            check(restored.Rows.Count==2&&restored.Rows.All(r=>r.Value.Receptions==3),"Fully identified live discoveries merge across receiver sessions");
            restored.Observe(grant with{Replay=true});check(restored.Rows.Count==2&&restored.Rows.All(r=>r.Value.Receptions==3),"Live persistence rejects replay observations");
            restored.Observe(grant with{P25Identity=identity with{SystemId=0x124}});
            restored.Observe(grant with{P25Identity=identity with{SiteId=4}});
            restored.Observe(grant with{FrequencyHz=775000000});
            check(restored.Rows.Count==8,"Different systems, sites and monitored frequencies cannot overwrite one another");
            var provisional=grant with{P25Identity=identity with{Wacn=null}};
            restored.Observe(provisional);restored.Save(true);var later=new P25Catalog(path);later.Observe(provisional);
            check(later.Rows.Count==12,"Incomplete identities remain provisional across sessions rather than guessed into one system");
            int before=later.Rows.Count;later.Observe(grant with{Kind="NID header"});later.Observe(grant with{Validated=false});later.Observe(grant with{Protocol="AX.25"});
            check(later.Rows.Count==before,"Headers, failed validation and unrelated protocols do not become P25 discoveries");
            var ordered=new P25Catalog();ordered.Observe(grant with{ObservedUtc=now.AddSeconds(10)});ordered.Observe(grant with{ObservedUtc=now});
            check(ordered.Rows.All(r=>r.Value.LastSeen==now.AddSeconds(10)&&r.Value.FirstSeen==now),"Out-of-order observations do not regress last-seen metadata");
            var panel=new P25CatalogPanel();panel.Initialize(Path.Combine(directory,"panel.json"));panel.Observe(grant);panel.Observe(grant with{Replay=true});panel.Save();
            var reopened=new P25CatalogPanel();reopened.Initialize(Path.Combine(directory,"panel.json"));
            check(panel.LiveCount==2&&panel.ReplayCount==2&&reopened.LiveCount==2&&reopened.ReplayCount==0,"Panel keeps live persistence and replay-only discoveries separate across reopen");
            int draftRequests=0;panel.SaveFrequencyRequested+=_=>draftRequests++;
            panel.CheckActions(true);panel.ShowReplay();panel.CheckActions(true);
            check(draftRequests==1,"Selected live channel enables frequency review; replay selection cannot request it");
            var records=new P25Catalog();foreach(var row in observations)records.Observe(row);
            check(records.Rows.Any(r=>r.Category=="Band plan")&&records.Rows.Any(r=>r.Category=="Secondary control channels")&&records.Rows.Any(r=>r.Category=="Adjacent site status"),"Catalog retains band tables and both discovery announcement types");
            using(var export=JsonDocument.Parse(records.Export()))check(export.RootElement.GetProperty("Version").GetInt32()==1&&export.RootElement.GetProperty("Entries").GetArrayLength()==records.Rows.Count,"Discovery export is versioned and preserves all entries");
            string corrupt=Path.Combine(directory,"corrupt.json");File.WriteAllText(corrupt,"{broken");var blocked=new P25Catalog(corrupt);blocked.Observe(grant);blocked.Save(true);
            check(blocked.Error!=null&&File.ReadAllText(corrupt)=="{broken","Unreadable catalog is preserved and reported rather than overwritten");
            string future=Path.Combine(directory,"future.json");File.WriteAllText(future,"{\"Version\":99,\"Entries\":[]}");var unsupported=new P25Catalog(future);unsupported.Observe(grant);unsupported.Save(true);
            check(unsupported.Error!=null&&File.ReadAllText(future).Contains("99"),"Unknown catalog versions are preserved");
            var editable=new P25Catalog(Path.Combine(directory,"editable.json"));editable.Observe(grant);
            var entry=editable.Rows[0];string originalLabel=entry.Value.Label;
            editable.Annotate(entry.Value.Key,"County dispatch","Observed on local control channel.");
            editable.Observe(grant with{ObservedUtc=now.AddSeconds(20)});editable.Save(true);
            check(entry.Label=="County dispatch"&&entry.Value.Label==originalLabel&&entry.Value.Notes.Contains("local")&&entry.Value.Receptions==2,"Operator annotations survive incoming updates without changing decoded labels or counts");
            var labelsRestored=new P25Catalog(Path.Combine(directory,"editable.json"));
            check(labelsRestored.Rows[0].Label=="County dispatch"&&labelsRestored.Rows[0].Value.Notes==entry.Value.Notes,"Names and notes survive save/reopen");
            var draft=P25FrequencyDraft.Create(entry.Value);
            check(draft.Name=="County dispatch"&&draft.FrequencyHz==851025000&&draft.Mode=="NFM"&&draft.Protocols=="P25"&&draft.Signaling.Contains("NAC 293")&&draft.Notes.Contains("Observed on local"),"Frequency draft uses selected resolved downlink, annotations and source metadata");
            var frequencies=new FrequencyLibrary(Path.Combine(directory,"frequencies.json"));
            frequencies.Upsert(draft);frequencies.Upsert(P25FrequencyDraft.Create(entry.Value));
            check(frequencies.Items.Count==2&&frequencies.Items[0].Id!=frequencies.Items[1].Id,"Review drafts have fresh identities and cannot silently overwrite an existing saved station");
            foreach(var unsuitable in new[]{entry.Value with{FrequencyHz=null},entry.Value with{FrequencyHz=0},entry.Value with{Category="Band plan"},entry.Value with{Observation=entry.Value.Observation with{Replay=true}}})
                check(!P25FrequencyDraft.CanCreate(unsuitable),"Unresolved, invalid, band-base or replay entries cannot become live frequency drafts");
            var neighborEntry=records.Rows.First(r=>r.Category=="Adjacent site status").Value;
            var neighborDraft=P25FrequencyDraft.Create(neighborEntry with{Observation=neighborEntry.Observation with{Replay=false}});
            check(neighborDraft.Signaling.Contains("Site 4")&&neighborDraft.Signaling.Contains("target NAC not supplied")&&!neighborDraft.Signaling.Contains("NAC 293"),"Neighbor frequency draft does not mislabel the target with receiving-site NAC");
            editable.Annotate(entry.Value.Key,"","");check(entry.Label==originalLabel&&entry.Value.Notes=="","Clearing an operator name restores the decoded label");
            bool oversized=false;try{editable.Annotate(entry.Value.Key,new string('x',201),"");}catch(ArgumentException){oversized=true;}
            check(oversized&&entry.Label==originalLabel,"Annotation length rejection leaves the previous values intact");
            var bounded=new P25Catalog();
            var one=grant with{P25Assignments=[grant.P25Assignments![0]]};
            for(int n=0;n<P25Catalog.Limit+1;n++)bounded.Observe(one with{FrequencyHz=100000000+n});
            bounded.Observe(one with{FrequencyHz=100000000});
            check(bounded.Rows.Count==P25Catalog.Limit&&bounded.Error!=null&&bounded.Rows[0].Value.Receptions==2,"Catalog limit reports skipped new entries while existing discoveries keep updating");
        }
        finally
        {
            // Only remove the exact generated test files inside this dedicated directory.
            foreach(var file in Directory.GetFiles(directory))File.Delete(file);
            Directory.Delete(directory);
        }
    }
}


