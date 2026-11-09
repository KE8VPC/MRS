using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace MRS;
internal sealed record LibraryChange(string Action,string Entry,string Detail);
internal sealed record LibraryReview(string Stamp,List<FrequencyItem> Channels,List<DiscoveryRecord> Records,List<LibraryChange> Changes)
{
 internal int Conflicts=>Changes.Count(c=>c.Action=="Conflict");
 internal string Summary=>$"{Changes.Count(c=>c.Action=="Add")} new · {Changes.Count(c=>c.Action=="Update")} updates · {Changes.Count(c=>c.Action=="Merge")} duplicates merged · {Conflicts} conflicts preserved";
}
public sealed partial class FrequencyLibrary
{
 internal string ReviewStamp=>JsonSerializer.Serialize(new{Items=items,Records=records});
 internal string ApplyReview(LibraryReview plan)
 {
  if(plan.Stamp!=ReviewStamp)throw new IOException("The list changed after this preview. Generate a new preview.");
  Validate(plan.Channels);DiscoveryExchange.ValidateRecords(plan.Records);
  bool existed=revision!=null;string backup=path+".before-review-"+DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8]+".json";
  // Commit still checks the disk revision under its write lock. Backup preserves
  // the exact pre-review snapshot; it never reads/replaces a different window's edit.
  if(revision!=null)File.WriteAllText(backup,revision);
  Commit(plan.Channels,true,plan.Records);return existed?backup:"New library; Undo is available.";
 }
 internal DiscoveryBundle ExportDiscoveries(IEnumerable<Guid>? selected=null,IEnumerable<DiscoveryRecord>? extra=null,bool includeNotes=false,bool backup=false)
 {
  var ids=selected?.ToHashSet();var channels=items.ToList();
  if(ids is {Count:>0}){
   bool added;do{added=false;foreach(var c in items)if(c.ParentId is {} parent&&ids.Contains(parent))added|=ids.Add(c.Id);}while(added);
   foreach(var c in items.Where(c=>ids.Contains(c.Id)).ToArray()){var parent=c.ParentId;while(parent is {} id){ids.Add(id);parent=items.First(p=>p.Id==id).ParentId;}}
   channels=items.Where(c=>ids.Contains(c.Id)).ToList();
  }
  var catalog=records.Concat(extra??[]).GroupBy(r=>r.Key).Select(g=>g.First()).ToList();
  foreach(var c in channels.Where(c=>!c.IsFolder&&c.SiteScope.Length>0)){
   string system=c.SiteScope;var split=system.Split(':');if(split.Length>=2)system=string.Join(':',split.Take(split[0]=="DMR"&&split.Length>=4?3:2));
   catalog.Add(DiscoveryRecord.Create("system",system,system,"",c.Protocols));catalog.Add(DiscoveryRecord.Create("site",system,c.SiteScope,"",c.Protocols,new(){{"identity_scope",c.SiteScope},{"source","Derived from saved channel scope"}}));
  }
  catalog=catalog.GroupBy(r=>r.Key).Select(g=>g.First()).ToList();
  if(ids is {Count:>0}){var scopes=channels.Where(c=>!c.IsFolder&&c.SiteScope.Length>0).Select(c=>c.SiteScope).ToArray();catalog=catalog.Where(r=>scopes.Any(s=>s==r.Scope||s.StartsWith(r.Scope+":",StringComparison.Ordinal)||r.Identifier==s)).ToList();}
  if(!backup){channels=channels.Select(c=>c with{Notes=includeNotes?c.Notes:"",Activity=c.Activity is {} a?a with{Receiver=""}:null}).ToList();catalog=catalog.Select(r=>r with{Fields=r.Fields.Where(p=>includeNotes||p.Key is not("notes" or "receiver")).ToDictionary(p=>p.Key,p=>p.Value)}).ToList();}
  var result=new DiscoveryBundle(DiscoveryExchange.Format,1,channels,catalog);DiscoveryExchange.Validate(result);return result;
 }
 internal LibraryReview PreviewImport(DiscoveryBundle bundle)
 {
  DiscoveryExchange.Validate(bundle);var next=items.ToList();var changes=new List<LibraryChange>();var mapping=new Dictionary<Guid,Guid>();
  var folders=bundle.Channels.Where(c=>c.IsFolder).ToList();for(int depth=0;folders.Count>0&&depth<64;depth++)foreach(var folder in folders.ToArray()){
   if(folder.ParentId is {} p&&!mapping.ContainsKey(p))continue;Guid? parent=folder.ParentId is {} oldParent?mapping[oldParent]:null;
   var existing=folder.BindingKey.Length>0?next.FirstOrDefault(c=>c.IsFolder&&c.ParentId==parent&&c.BindingKey==folder.BindingKey):null;existing??=next.FirstOrDefault(c=>c.IsFolder&&c.ParentId==parent&&c.Name.Equals(folder.Name,StringComparison.OrdinalIgnoreCase)&&(folder.BindingKey.Length==0||c.BindingKey.Length==0||c.BindingKey==folder.BindingKey));
   if(existing==null){existing=folder with{Id=next.Any(c=>c.Id==folder.Id)?Guid.NewGuid():folder.Id,ParentId=parent};next.Add(existing);changes.Add(new("Add",folder.Name,"Folder"));}mapping[folder.Id]=existing.Id;folders.Remove(folder);
  }
  foreach(var source in bundle.Channels.Where(c=>!c.IsFolder)){
   var incoming=source with{ParentId=source.ParentId is {} p?mapping[p]:null};var sameId=next.FirstOrDefault(c=>c.Id==incoming.Id);
   if(sameId!=null&&(sameId.IsFolder||sameId.FrequencyHz!=incoming.FrequencyHz||sameId.SiteScope!=incoming.SiteScope||!CompatibleProtocol(sameId.Protocols,incoming.Protocols))){changes.Add(new("Conflict",incoming.Name,"Stable ID refers to a different channel/scope; skipped."));continue;}
   var matches=next.Where(c=>!c.IsFolder&&c.FrequencyHz==incoming.FrequencyHz&&c.SiteScope==incoming.SiteScope&&(c.SiteScope.Length>0||c.ParentId==incoming.ParentId)&&CompatibleProtocol(c.Protocols,incoming.Protocols)).ToArray();
   if(sameId==null&&matches.Length>1){changes.Add(new("Conflict",incoming.Name,"Multiple possible matches; skipped."));continue;}
   var target=sameId??matches.SingleOrDefault();if(target==null){next.Add(incoming);changes.Add(new("Add",incoming.Name,DiscoveryNames.Frequency(incoming.FrequencyHz)+" MHz"));continue;}
   var merged=MergeItem(target,incoming,out var conflicts);if(conflicts.Count>0)changes.Add(new("Conflict",target.Name,"Kept existing: "+string.Join(", ",conflicts)));
   if(merged!=target){next[next.IndexOf(target)]=merged;changes.Add(new("Update",target.Name,"Filled missing fields; existing identifiers and custom values retained."));}
  }
  var catalog=records.ToList();foreach(var r in bundle.Records){var existing=catalog.FirstOrDefault(c=>c.Key==r.Key);if(existing==null){if(catalog.Any(c=>c.Id==r.Id)){changes.Add(new("Conflict",r.Kind+" "+r.Identifier,"Catalog ID collision; skipped."));continue;}catalog.Add(r);changes.Add(new("Add",r.Kind+" "+r.Identifier,r.Scope));continue;}
   var fields=new Dictionary<string,string>(existing.Fields);var conflicts=new List<string>();foreach(var kv in r.Fields){if(!fields.TryGetValue(kv.Key,out string? value)||value.Length==0)fields[kv.Key]=kv.Value;else if(kv.Value.Length>0&&value!=kv.Value)conflicts.Add(kv.Key);}
   string label=existing.Label.Length==0?r.Label:existing.Label;if(existing.Label.Length>0&&r.Label.Length>0&&existing.Label!=r.Label)conflicts.Add("label");if(existing.Protocol.Length>0&&r.Protocol.Length>0&&existing.Protocol!=r.Protocol)conflicts.Add("protocol");
   var updated=existing with{Label=label,Protocol=existing.Protocol.Length==0?r.Protocol:existing.Protocol,Fields=fields};if(conflicts.Count>0)changes.Add(new("Conflict",r.Kind+" "+r.Identifier,"Kept existing: "+string.Join(", ",conflicts)));
   if(JsonSerializer.Serialize(updated)!=JsonSerializer.Serialize(existing)){catalog[catalog.IndexOf(existing)]=updated;changes.Add(new("Update",r.Kind+" "+r.Identifier,"Filled missing catalog fields."));}
  }
  Validate(next);DiscoveryExchange.ValidateRecords(catalog);return new(ReviewStamp,next,catalog,changes);
 }
 private static bool CompatibleProtocol(string a,string b)=>a==b||a is "" or "Unknown"||b is "" or "Unknown";
 private static FrequencyItem MergeItem(FrequencyItem local,FrequencyItem incoming,out List<string> conflicts)
 {
  conflicts=[];var a=JsonSerializer.SerializeToNode(local)!.AsObject();var b=JsonSerializer.SerializeToNode(incoming)!.AsObject();foreach(var kv in b){if(kv.Key is "Id" or "ParentId" or "FrequencyHz" or "SiteScope")continue;
   var current=a[kv.Key];bool Empty(JsonNode? n)=>n==null||n is JsonValue v&&v.TryGetValue<string>(out var text)&&text.Length==0;
   if(Empty(kv.Value))continue;
   bool missing=Empty(current)||(kv.Key is "Protocols" or "Modulation")&&current?.ToString()=="Unknown"||kv.Key=="BandwidthHz"&&current?.ToString()=="0";
   if(kv.Key=="Name"&&DiscoveryNames.Generated(local.Name)&&DiscoveryNames.Generated(incoming.Name))continue;
   if(kv.Key=="Name"&&DiscoveryNames.Generated(local.Name)&&!DiscoveryNames.Generated(incoming.Name))missing=true;
   if(missing)a[kv.Key]=kv.Value?.DeepClone();else if(!JsonNode.DeepEquals(current,kv.Value))conflicts.Add(kv.Key);
  }
  return a.Deserialize<FrequencyItem>()!;
 }
 internal LibraryReview PreviewCleanup(bool rename,bool organize,bool merge,int spacing,int tolerance,IEnumerable<Guid>? selected=null)
 {
  var next=items.ToList();var changes=new List<LibraryChange>();var chosen=selected?.ToHashSet();if(chosen is {Count:>0}){bool added;do{added=false;foreach(var node in items)if(node.ParentId is {} parent&&chosen.Contains(parent))added|=chosen.Add(node.Id);}while(added);}
  foreach(var c in items.Where(c=>!c.IsFolder&&(chosen==null||chosen.Contains(c.Id)))){
   var updated=c;long hz=DiscoveryNames.Snap(c.FrequencyHz,spacing,tolerance);if(hz!=c.FrequencyHz){updated=updated with{FrequencyHz=hz,OriginalFrequencyHz=c.OriginalFrequencyHz??c.FrequencyHz};changes.Add(new("Update",c.Name,$"Frequency {DiscoveryNames.Frequency(c.FrequencyHz)} → {DiscoveryNames.Frequency(hz)} MHz; original retained."));}
   if(rename&&DiscoveryNames.Generated(c.Name)){string label=DiscoveryNames.Label(c);if(label!=c.Name){updated=updated with{Name=label};changes.Add(new("Update",c.Name,"Name → "+label));}}
   if(organize&&c.SiteScope.Length>0){var parts=c.SiteScope.Split(':');string[] folders=parts[0]=="P25"&&parts.Length>=4?["P25",parts[1],"RFSS "+parts[2]+" - Site "+parts[3]]:parts[0]=="NXDN"&&parts.Length>=3?["NXDN",parts[1],parts[2]]:parts.Length>=4?[parts[0],parts[1],parts[2],parts[3]]:[];Guid? parent=folders.Length>0?DiscoveryFolder(next,new SiteChannelData(parts[0],folders,c.SiteScope,c.FrequencyHz,c.ChannelIds,c.ChannelRoles,c.Signaling)):null;if(folders.Length>0&&parent!=c.ParentId){updated=updated with{ParentId=parent};changes.Add(new("Update",c.Name,"Folder → "+string.Join(" / ",folders)));}}
   next[next.FindIndex(i=>i.Id==c.Id)]=updated;
  }
  if(merge)foreach(var group in next.Where(c=>!c.IsFolder&&(chosen==null||chosen.Contains(c.Id))).GroupBy(c=>new{c.FrequencyHz,c.SiteScope,Parent=c.SiteScope.Length>0?(Guid?)null:c.ParentId,c.Protocols}).Where(g=>g.Count()>1).ToArray()){
   var target=group.First();foreach(var other in group.Skip(1)){var combined=MergeItem(target,other,out var conflicts);if(conflicts.Count>0){changes.Add(new("Conflict",other.Name,"Duplicate kept separate: "+string.Join(", ",conflicts)));continue;}next[next.IndexOf(target)]=combined;next.Remove(other);target=combined;changes.Add(new("Merge",other.Name,"Exact frequency/scope match; compatible fields retained."));}
  }
  Validate(next);return new(ReviewStamp,next,records.ToList(),changes);
 }
}
