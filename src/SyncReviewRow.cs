using System.Text.Json.Nodes;
namespace MRS;
internal sealed record SyncFieldDifference(string Field,string Local,string Website,string Change);
internal sealed class SyncReviewRow
{
 internal long Id{get;}internal SyncPending Item{get;}
 public string Action=>Item.Op;public string Type=>SyncJson.Text(Item.Record["type"]);public string MHz{get;}public string Protocol=>SyncJson.Text(Item.Record["protocol"]);public string Label=>SyncJson.Text(Item.Record["label"]);
 public string SystemSite{get;}public string Identifiers{get;}
 public string Channel=>string.Join(" · ",new[]{SyncJson.Text(Item.Record["chan_conv"]),SyncJson.Text(Item.Record["chan_no"]),SyncJson.Text(Item.Record["slot"]) is {Length:>0} slot?"slot "+slot:""}.Where(s=>s.Length>0));
 public string Comparison{get;}public long Revision=>SyncJson.Number(Item.Record["rev"]);public string Reason=>Item.Reason;
 internal List<SyncFieldDifference> Differences{get;}=[];
 internal SyncReviewRow(long id,SyncPending item,SyncDocument state,IReadOnlyList<SyncDraft> local)
 {
  Id=id;Item=item;MHz=SyncJson.Text(item.Record["mhz"]);if(MHz.Length==0&&state.Remote.TryGetValue(SyncJson.Number(item.Record["target"]),out var target))MHz=SyncJson.Text(target["mhz"]);
  var identifiers=new List<string>();var ancestor=item.Record;for(int depth=0;depth<3;depth++){foreach(var pair in ancestor["ids"] as JsonObject??new())identifiers.Add(pair.Key+" "+SyncJson.Text(pair.Value));if(!state.Remote.TryGetValue(SyncJson.Number(ancestor["parent"]),out var parent))break;ancestor=parent;}Identifiers=SyncJson.Safe(string.Join(" · ",identifiers.Distinct()));
  var context=WebsiteImport.Context(item.Record,state,SyncJson.Hash(state.Account)[..16]);SystemSite=context.Unresolved?"Unresolved parent":string.Join(" / ",context.Folders);
  var binding=state.Bindings.FirstOrDefault(p=>p.Value.Id==id);var current=local.FirstOrDefault(d=>d.Ref==binding.Key||binding.Value?.LocalId.Length>0&&d.LocalId==binding.Value.LocalId&&d.Type==Type);
  bool conflict=false,localChanged=false,remoteChanged=false;
  var fields=new[]{"label","mhz","protocol","description","ident","system","notes","parent","parent_ref","target","target_ref","chan_no","chan_conv","slot","ids","attrs","evidence","prov","verify_status","verify_rule","verify_count","first_seen","last_seen","heard","mapping_conflict","conflicts_with"};
  foreach(string field in fields){string remote=Value(item.Record[field],field),baseline=Value(binding.Value?.Baseline[field],field);string here=current==null?"":Value(current.Record[field],field);if(current!=null&&current.Record[field]==null&&binding.Value!=null)here=baseline;bool lc=binding.Value!=null&&here!=baseline,rc=binding.Value==null||remote!=baseline;bool differs=here!=remote;bool clash=current!=null&&lc&&rc&&differs;conflict|=clash;localChanged|=lc;remoteChanged|=rc;if(here.Length>0||remote.Length>0)Differences.Add(new(field,current==null?"No linked local value":here,remote,clash?"Conflicting edits":!differs?"Same":lc&&!rc?"Local edit":rc?"Website change":"Different"));}
  Comparison=item.Op=="delete"?"Deleted remotely — local retained":current==null?binding.Value==null?"New / not linked":"Local record unavailable":conflict?"Conflicting local edits":localChanged&&!remoteChanged?"Local edits retained":remoteChanged?"Website update":"Matches local / metadata only";
  if(context.Unresolved&&item.Op!="delete")Comparison+=" · unresolved relationship";
 }
 private static string Value(JsonNode? node,string field){if(field=="mhz"&&SyncMapping.ParseHz(node) is >0 and var hz)return PlanCsv.MHz(hz);if(node is JsonObject obj)return "{"+string.Join(", ",obj.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+": "+Value(p.Value,p.Key)))+"}";return node is JsonArray?node.ToJsonString():SyncJson.Text(node);}
 internal string Search=>string.Join(" ",Type,MHz,Protocol,Label,SystemSite,Identifiers,Channel,Comparison,Id.ToString());
}
