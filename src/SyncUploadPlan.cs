using System.IO;
using System.Text;
using System.Text.Json.Nodes;
namespace MRS;

internal enum SyncUpdateType { Frequencies, Observations, Identifiers, SystemSites, All }

// A preview covers an immutable, dependency-ordered selection, including all batches.
internal static class SyncUploadPlan
{
 internal static bool Includes(SyncUpdateType update,SyncDraft draft)=>update switch
 {
  SyncUpdateType.Frequencies=>draft.Type is "frequency" or "lcn",
  SyncUpdateType.Observations=>draft.IsObservation,
  SyncUpdateType.Identifiers=>draft.Type is "talkgroup" or "radio",
  SyncUpdateType.SystemSites=>draft.Type is "system" or "site",
  _=>true
 };

 internal static List<SyncDraft> Select(IEnumerable<SyncDraft> chosen,IReadOnlyList<SyncDraft> prepared,SyncDocument state)
 {
  var selected=chosen.ToDictionary(d=>d.Ref);if(selected.Count==0)throw new IOException("No upload records selected. Prepare the selection, then select at least one row.");
  void Include(SyncDraft draft,HashSet<string> visiting){if(!visiting.Add(draft.Ref))throw new IOException("Upload relationships contain a cycle.");foreach(string field in new[]{"parent_ref","target_ref"}){string reference=SyncJson.Text(draft.Record[field]);if(reference.Length==0)continue;if(state.Bindings.TryGetValue(reference,out var binding)&&!binding.Gone)continue;var dependency=prepared.SingleOrDefault(d=>d.Ref==reference)??throw new IOException("Missing upload dependency: "+reference);if(!selected.ContainsKey(reference)){selected.Add(reference,dependency);Include(dependency,visiting);}}visiting.Remove(draft.Ref);}
  foreach(var draft in selected.Values.ToArray())Include(draft,[]);
  return selected.Values.OrderBy(d=>SyncMapping.Order(d.Type)).ThenBy(d=>d.Ref,StringComparer.Ordinal).Select(d=>d with{Record=SyncJson.Copy(d.Record)}).ToList();
 }
 internal static List<List<SyncDraft>> Batches(IReadOnlyList<SyncDraft> selected,WebsiteSync client)
 {
  var batches=new List<List<SyncDraft>>();var batch=new List<SyncDraft>();int bytes=16;
  foreach(var draft in selected){int size=Encoding.UTF8.GetByteCount(client.Prepare(draft).ToJsonString())+1;if(size>1800000)throw new IOException("One record exceeds the upload size limit: "+draft.Ref);if(batch.Count==200||bytes+size>1800000){batches.Add(batch);batch=[];bytes=16;}batch.Add(draft);bytes+=size;}
  if(batch.Count>0)batches.Add(batch);return batches;
 }
 internal static string Stamp(IReadOnlyList<SyncDraft> selected,SyncDocument state)=>SyncJson.Hash(state.Account+"|"+string.Join("|",selected.Select(d=>d.Ref+":"+d.Record.ToJsonString()+":"+state.Bindings.GetValueOrDefault(d.Ref)?.Rev)));
 internal static bool Success(JsonArray results)=>results.All(r=>SyncJson.Text(r?["status"]) is "created" or "updated" or "unchanged");
}
