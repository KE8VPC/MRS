using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
namespace MRS;
internal sealed class TalkgroupRecord
{
 public string Key{get;set;}="";public string System{get;set;}="";public int Talkgroup{get;set;}public string Label{get;set;}="";public long Count{get;set;}public DateTimeOffset Seen{get;set;}
 public DateTimeOffset? FirstSeen{get;set;}public int? Wacn{get;set;}public int? SystemId{get;set;}public int? Nac{get;set;}public long? ObservedHz{get;set;}public string EvidenceContext{get;set;}="";public string IdentityReview{get;set;}="";
 [JsonIgnore] public string DisplaySystem=>P25TalkgroupHistory.Display(this);
}
internal sealed class P25TalkgroupHistory
{
 private sealed record Context(P25Identity Identity,string Token);
 private readonly Dictionary<string,Context> contexts=[];private readonly string run=Guid.NewGuid().ToString("N");private int epoch;
 internal void Reset(){contexts.Clear();epoch++;}
 internal static string Display(TalkgroupRecord row)
 {
  if(!row.System.StartsWith("P25:",StringComparison.OrdinalIgnoreCase))return row.System;
  var m=Regex.Match(row.System,@"^P25:([0-9a-fA-F]*)-([0-9a-fA-F]*)(?::NAC ([0-9a-fA-F]+):(\d+))?$");
  string w=row.Wacn?.ToString("X5")??(m.Success?m.Groups[1].Value:""),s=row.SystemId?.ToString("X3")??(m.Success?m.Groups[2].Value:"");
  if(w.Length>0&&s.Length>0)return $"P25 · {w}-{s}"+(row.IdentityReview.Length>0?" · review labels":"");
  string nac=row.Nac?.ToString("X3")??(m.Success?m.Groups[3].Value:""),frequency=row.ObservedHz is long hz?PlanCsv.MHz(hz):m.Success&&long.TryParse(m.Groups[4].Value,out long legacy)?PlanCsv.MHz(legacy):"";
  return "P25 · "+(w.Length>0?"WACN "+w+" · System unknown":(s.Length>0?"System "+s+" · ":"")+"WACN unknown")+(nac.Length>0?" · NAC "+nac:"")+(frequency.Length>0?" · observed "+frequency+" MHz":"")+" · provisional"+(row.IdentityReview.Length>0?" · review labels":"");
 }
 private static bool Conflicts(P25Identity a,P25Identity b)=>a.Wacn.HasValue&&b.Wacn.HasValue&&a.Wacn!=b.Wacn||a.SystemId.HasValue&&b.SystemId.HasValue&&a.SystemId!=b.SystemId||a.RfssId.HasValue&&b.RfssId.HasValue&&a.RfssId!=b.RfssId||a.SiteId.HasValue&&b.SiteId.HasValue&&a.SiteId!=b.SiteId;
 internal bool Observe(IList<TalkgroupRecord> rows,PacketObservation p)
 {
  if(!p.IsValidated||p.Replay||p.P25Identity is not {} id)return false;
  string key=$"{p.SourceKey}|{p.FrequencyHz}|{id.Nac}";Context? context=null;
  if(!string.IsNullOrEmpty(p.SourceKey)&&p.FrequencyHz is >0){if(contexts.TryGetValue(key,out var old)&&!Conflicts(old.Identity,id))context=new(new(id.Nac,id.Wacn??old.Identity.Wacn,id.SystemId??old.Identity.SystemId,id.RfssId??old.Identity.RfssId,id.SiteId??old.Identity.SiteId),old.Token);else context=new(id,run+":"+(++epoch));if(contexts.Count>=512)contexts.Clear();contexts[key]=context;id=context.Identity;}
  string evidence=context?.Token??run+":unassociated:"+(++epoch);bool complete=id is{Wacn:not null,SystemId:not null};string scope=complete?$"P25:{id.Wacn:X5}-{id.SystemId:X3}":$"P25:provisional:{evidence}";bool changed=false;
  if(complete&&context!=null){foreach(var provisional in rows.Where(r=>r.EvidenceContext==evidence&&r.System.StartsWith("P25:provisional:")).ToArray()){
    var target=rows.FirstOrDefault(r=>r.Key==scope+":"+provisional.Talkgroup);
    if(target!=null&&target.Label.Length>0&&provisional.Label.Length>0&&target.Label!=provisional.Label){provisional.IdentityReview="Conflicting labels; same-session identity decoded. Manual review required.";provisional.Wacn=id.Wacn;provisional.SystemId=id.SystemId;changed=true;continue;}
    if(target!=null){if(target.Label.Length==0)target.Label=provisional.Label;target.Count=target.Count>long.MaxValue-provisional.Count?long.MaxValue:target.Count+provisional.Count;target.Seen=target.Seen>provisional.Seen?target.Seen:provisional.Seen;target.FirstSeen=Earliest(target.FirstSeen,provisional.FirstSeen);rows.Remove(provisional);}else{provisional.Key=scope+":"+provisional.Talkgroup;provisional.System=scope;provisional.Wacn=id.Wacn;provisional.SystemId=id.SystemId;}changed=true;
   }}
  foreach(var assignment in p.P25Assignments??[]){if(assignment.TalkgroupId<=0)continue;string identity=scope+":"+assignment.TalkgroupId;var row=rows.FirstOrDefault(r=>r.Key==identity);if(row==null){if(rows.Count>=10000)continue;row=new(){Key=identity,System=scope,Talkgroup=assignment.TalkgroupId,FirstSeen=p.ObservedUtc,EvidenceContext=evidence,Wacn=id.Wacn,SystemId=id.SystemId,Nac=id.Nac,ObservedHz=p.FrequencyHz};rows.Add(row);}if(row.Count<long.MaxValue)row.Count++;if(p.ObservedUtc>row.Seen)row.Seen=p.ObservedUtc;row.FirstSeen=Earliest(row.FirstSeen,p.ObservedUtc);changed=true;}
  return changed;
 }
 private static DateTimeOffset? Earliest(DateTimeOffset? a,DateTimeOffset? b)=>a==null?b:b==null?a:a<b?a:b;
}
