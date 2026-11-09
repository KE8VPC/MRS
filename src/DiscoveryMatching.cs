using System.IO;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record PendingDiscovery(string Key,SiteChannelData Data,string Reason){public string Display=>$"{DiscoveryNames.Frequency(Data.Hz)} MHz · {Data.Protocol} · {Data.Scope} · {Reason}";}
public sealed partial class FrequencyLibrary
{
 internal static string[] FolderKeys(SiteChannelData c)=>c.Folders.Select((_,i)=>i==0?"root:"+c.Protocol:i==c.Folders.Length-1?"site:"+c.Scope:"folder:"+string.Join(':',c.Folders.Take(i+1))).ToArray();
 private static bool Below(FrequencyItem child,Guid folder,IReadOnlyList<FrequencyItem> values){var parent=child.ParentId;for(int n=0;parent!=null&&n<64;n++){if(parent==folder)return true;parent=values.FirstOrDefault(v=>v.Id==parent)?.ParentId;}return false;}
 internal static Guid? DiscoveryFolder(List<FrequencyItem> values,SiteChannelData data)
 {
  Guid? parent=null;var keys=FolderKeys(data);
  for(int level=0;level<data.Folders.Length;level++){
   string name=data.Folders[level],key=keys[level];var siblings=values.Where(i=>i.IsFolder&&i.ParentId==parent).ToArray();
   var folder=siblings.FirstOrDefault(f=>f.BindingKey==key);
   if(folder==null){var aliases=siblings.Where(f=>values.Any(c=>!c.IsFolder&&Below(c,f.Id,values)&&ScopeMatchesLevel(c.SiteScope,data,level))).ToArray();var renamed=aliases.Where(f=>!f.Name.Equals(name,StringComparison.OrdinalIgnoreCase)).ToArray();folder=renamed.Length==1?renamed[0]:aliases.FirstOrDefault();}
   folder??=siblings.FirstOrDefault(i=>i.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
   if(folder==null){folder=new(){IsFolder=true,Name=name,ParentId=parent,BindingKey=key};values.Add(folder);}else if(folder.BindingKey!=key){var bound=folder with{BindingKey=key};values[values.IndexOf(folder)]=bound;folder=bound;}
   parent=folder.Id;
  }return parent;
 }
 private static bool ScopeMatchesLevel(string scope,SiteChannelData data,int level)
 {
  var a=scope.Split(':');var b=data.Scope.Split(':');if(a[0]!=b[0])return false;
  int count=data.Protocol=="P25"?level==0?1:level==1?2:b.Length:Math.Min(level+1,b.Length);
  return a.Length>=count&&b.Length>=count&&a.Take(count).SequenceEqual(b.Take(count));
 }
 internal static bool CompatibleIdentity(FrequencyItem old,SiteChannelData incoming)
 {
  if(old.Protocols.Length>0&&old.Protocols!="Unknown"&&!old.Protocols.Contains(incoming.Protocol,StringComparison.OrdinalIgnoreCase))return false;
  string[] patterns=[@"\b(?:NET(?:WORK)?)\s+(\d+)",@"\bSITE\s+(\d+)",@"\bCC\s+(\d+)",@"\bRAN\s+(\d+)",@"\bAREA\s+(\d+)",@"\bHOME\s+(\d+)",@"\bNAC\s+([A-F0-9]+)",@"\bWACN\s+([A-F0-9]+)",@"\bSYSTEM\s+([A-F0-9]+)"];
  foreach(string pattern in patterns){var a=Regex.Match(old.Signaling,pattern,RegexOptions.IgnoreCase);var b=Regex.Match(incoming.Identifiers,pattern,RegexOptions.IgnoreCase);if(a.Success&&b.Success&&!a.Groups[1].Value.Equals(b.Groups[1].Value,StringComparison.OrdinalIgnoreCase))return false;}
  if(old.SiteScope.Length>0&&old.SiteScope!=incoming.Scope&&!old.EvidenceStatus.StartsWith("Provisional")&&!incoming.Evidence.StartsWith("Provisional")){
   // Generic DMR can be refined to a subtype, but two complete different sites
   // are never treated as the same channel solely because they share a frequency.
   var a=old.SiteScope.Split(':');var b=incoming.Scope.Split(':');if(incoming.Protocol!="DMR"||a.Length<4||b.Length<4||a[2]!=b[2]||a[3]!=b[3])return false;
   bool Unknown(string v)=>v is "DMR" or "Unidentified system";if(a[1]!=b[1]&&!Unknown(a[1])&&!Unknown(b[1]))return false;
  }
  return true;
 }
 private static string MergeIdentityText(string before,string after){string text=string.Join("; ",(before+"; "+after).Split(';',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Where(s=>!Regex.IsMatch(s,@"^(Network|Site|NAC|System|WACN)\s*$",RegexOptions.IgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase));return text[..Math.Min(4000,text.Length)];}
 internal (int Added,int Updated) SaveSiteChannels(IEnumerable<SiteChannelData> data,bool forceNew=false,Guid? selectedMatch=null)
 {
  var next=items.ToList();var pending=pendingDiscoveries.ToList();int added=0,updated=0;
  foreach(var c in data){if(c.Hz<=0||c.Hz>6000000000)continue;
   var nearby=next.Where(i=>!i.IsFolder&&Math.Abs(i.FrequencyHz-c.Hz)<=5000&&(i.Protocols is "" or "Unknown"||i.Protocols.Contains(c.Protocol,StringComparison.OrdinalIgnoreCase))).ToArray();
   var exact=nearby.Where(i=>i.SiteScope==c.Scope&&Math.Abs(i.FrequencyHz-c.Hz)<=1000&&CompatibleIdentity(i,c)).OrderBy(i=>Math.Abs(i.FrequencyHz-c.Hz)).ToArray();
   var compatible=nearby.Where(i=>Math.Abs(i.FrequencyHz-c.Hz)<=1000&&CompatibleIdentity(i,c)).ToArray();
   FrequencyItem? old=forceNew?null:selectedMatch is {} chosen?next.Single(i=>i.Id==chosen&&!i.IsFolder):exact.Length==1?exact[0]:compatible.Length==1?compatible[0]:null;
   if(selectedMatch!=null&&old!=null&&Math.Abs(old.FrequencyHz-c.Hz)>5000)throw new IOException("The selected entry is more than 5 kHz away. Review its tuning before merging.");
   if(old!=null&&!CompatibleIdentity(old,c))throw new IOException("Selected entry has conflicting identity. Keep it separate or review its identifiers first.");
   string? reason=!forceNew&&old==null?(nearby.Length>0?"Possible duplicate or conflicting identity; choose an existing entry or keep separate.":updateExistingOnly?"Research mode: new channel held for review.":null):null;
   if(reason!=null){var similar=pending.Where(p=>Math.Abs(p.Data.Hz-c.Hz)<=1000&&CompatibleIdentity(p.Data.Draft(),c)).ToArray();if(similar.Length==1)pending.Remove(similar[0]);pending.RemoveAll(p=>p.Key==c.Key);if(pending.Count>=500)pending.RemoveAt(0);pending.Add(new(c.Key,c,reason));continue;}
   pending.RemoveAll(p=>p.Key==c.Key);
   if(old==null){next.Add(c.Draft() with{ParentId=DiscoveryFolder(next,c),LastObservedFrequencyHz=c.Hz});added++;continue;}
   int Richness(string value)=>value.Split(':').Count(v=>v.Length>0&&v!="DMR"&&!v.Contains("unknown",StringComparison.OrdinalIgnoreCase)&&!v.Contains("Unidentified")&&!v.Contains("Unassigned")&&!v.Contains("Unresolved")&&!v.Contains("Provisional"));bool richer=old.SiteScope.Length==0||((!c.Evidence.StartsWith("Provisional")||old.EvidenceStatus.StartsWith("Provisional"))&&(Richness(c.Scope)>Richness(old.SiteScope)||c.Scope==old.SiteScope||!c.Evidence.StartsWith("Provisional")&&!old.EvidenceStatus.StartsWith("Provisional")&&Richness(c.Scope)>=Richness(old.SiteScope)));string scope=richer?c.Scope:old.SiteScope;
   string text=MergeIdentityText(old.Signaling,c.Identifiers);var newer=old with{SiteScope=scope,Signaling=text,LastObservedFrequencyHz=c.Hz,ChannelLastSeen=c.Seen>old.ChannelLastSeen||old.ChannelLastSeen==null?c.Seen:old.ChannelLastSeen,ChannelRoles=string.Join("; ",old.ChannelRoles.Split("; ",StringSplitOptions.RemoveEmptyEntries).Append(c.Role).Distinct().Take(32)),ChannelIds=string.Join(", ",old.ChannelIds.Split(", ",StringSplitOptions.RemoveEmptyEntries).Append(c.Channel).Where(v=>v.Length>0).Distinct().Take(64)),UplinkHz=old.UplinkHz??c.Uplink,EvidenceStatus=richer?c.Evidence:old.EvidenceStatus,Protocols=old.Protocols is "" or "Unknown"?c.Protocol:old.Protocols};
   if(DiscoveryNames.Generated(old.Name))newer=newer with{Name=DiscoveryNames.Label(newer)};
   // Preserve a user's unscoped/manual placement; known site entries follow their
   // identity-bound folder even if that folder has been renamed.
   next[next.FindIndex(i=>i.Id==old.Id)]=newer;
   if(scope==c.Scope&&old.SiteScope.Length>0&&old.Tags.Contains("Site channel discovery"))newer=newer with{ParentId=DiscoveryFolder(next,c)};
   if(newer!=old){next[next.FindIndex(i=>i.Id==old.Id)]=newer;updated++;}
  }
  bool pendingChanged=System.Text.Json.JsonSerializer.Serialize(pending)!=System.Text.Json.JsonSerializer.Serialize(pendingDiscoveries);if(added+updated>0||pendingChanged){var previous=pendingDiscoveries;pendingDiscoveries=pending;try{Commit(next,false);}catch{pendingDiscoveries=previous;throw;}}return(added,updated);
 }
 internal void ResolveDiscovery(string key,Guid? existing,bool separate)
 {var pending=pendingDiscoveries.FirstOrDefault(p=>p.Key==key)??throw new IOException("Held discovery changed; Reload this review before saving.");SaveSiteChannels([pending.Data],separate,existing);}
}
