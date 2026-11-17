using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record OrganizationRules(string Layout="SubtypeColor",bool PreferKnownSites=true,bool RenameGenerated=true,string FolderTemplate="{protocol}/{subtype}/{cc}",string NameTemplate="{protocol} · {identity}")
{
 internal void Validate(){if(Layout is not("SubtypeColor" or "SystemSite" or "Protocol" or "Custom"))throw new IOException("Choose a supported organization layout.");foreach(var template in new[]{FolderTemplate,NameTemplate}){if(template==null||template.Length>500||template.Contains('\\')||template.Split('/').Any(p=>p is "." or ".."))throw new IOException("Templates must be short folder labels, without filesystem paths.");foreach(Match m in Regex.Matches(template,@"\{([^{}]+)\}"))if(!OrganizationFields.Tokens.Contains(m.Groups[1].Value))throw new IOException("Unknown template field: "+m.Groups[1].Value);string rest=Regex.Replace(template,@"\{[^{}]+\}","");if(rest.Contains('{')||rest.Contains('}'))throw new IOException("Unbalanced template braces.");}if(FolderTemplate.Split('/').Length>8||NameTemplate.Contains('/'))throw new IOException("Use at most eight folder levels and no slash in a name template.");}
}
internal sealed record OrganizationFields(Dictionary<string,string> Values,string[] KnownPath,string Warning)
{
 internal static readonly HashSet<string> Tokens=["protocol","subtype","cc","network","site","area","home","channel","identity","frequency"];
 internal string Render(string template)=>Regex.Replace(Regex.Replace(template,@"\{([^{}]+)\}",m=>Values.GetValueOrDefault(m.Groups[1].Value,"")),@"(?:\s*·\s*){2,}"," · ").Trim(' ','·','-');
 internal static OrganizationFields From(FrequencyItem c)
 {
  string protocol=(c.Protocols.Contains("DMR",StringComparison.OrdinalIgnoreCase)||c.Protocols is "Capacity Plus" or "Connect Plus"||c.SiteScope.StartsWith("DMR:"))?"DMR":c.Protocols.Contains("P25",StringComparison.OrdinalIgnoreCase)||c.SiteScope.StartsWith("P25:")?"P25":c.Protocols.StartsWith("NXDN",StringComparison.OrdinalIgnoreCase)?"NXDN":c.Protocols=="LTR"?"LTR":c.Protocols is "" or "Unknown"?"Unknown signal":c.Protocols;
  string source=c.SiteScope+"; "+c.Signaling;var warnings=new List<string>();
  string Field(string key,string pattern,string prefix=""){var values=Regex.Matches(c.Signaling+(key is "network" or "site"?"; "+c.SiteScope:""),pattern,RegexOptions.IgnoreCase).Select(m=>int.TryParse(m.Groups[1].Value,out int value)?value.ToString(System.Globalization.CultureInfo.InvariantCulture):m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();if(values.Length>1){warnings.Add("Conflicting "+key+" evidence");return "";}return values.Length==1?prefix+values[0]:"";}
  string cc=Field("color code",@"\bCC\s+(\d{1,2})\b","CC "),network=Field("network",@"\b(?:NET|NETWORK)\s+(\d+)\b","Network "),site=Field("site",@"\bSITE\s+(\d+)\b","Site "),area=Field("area",@"\bAREA\s+([01])\b","Area "),home=Field("home",@"\bHOME\s+(\d{1,2})\b","Home ");
  if(home.Length>0&&int.TryParse(home[5..],out int h))home="Home "+h.ToString("00");if(cc.Length>0&&(!int.TryParse(cc[3..],out int code)||code>15)){warnings.Add("Invalid color code");cc="";}
  string[] variants=new[]{"Capacity Plus","Connect Plus","DMR Tier III"}.Where(v=>source.Contains(v,StringComparison.OrdinalIgnoreCase)||c.Protocols.Equals(v,StringComparison.OrdinalIgnoreCase)).ToArray();if(variants.Length>1)warnings.Add("Conflicting DMR subtype evidence");string subtype=variants.Length==1?variants[0]:source.Contains("Motorola rest signaling",StringComparison.OrdinalIgnoreCase)?"Motorola rest signaling":"Generic DMR";
  if(protocol!="DMR")subtype="";string[] known=[];
  if(protocol=="DMR"&&network.Length>0&&site.Length>0&&variants.Length==1)known=[protocol,subtype,network,site];
  if(protocol=="P25"){var identity=SyncMapping.Identity(c);string w=SyncJson.Text(identity.SystemIds["wacn"]),s=SyncJson.Text(identity.SystemIds["sysid"]),rfss=SyncJson.Text(identity.SiteIds["rfss"]),number=SyncJson.Text(identity.SiteIds["site"]);if(w.Length>0&&s.Length>0&&rfss.Length>0&&number.Length>0)known=[protocol,w+"-"+s,"RFSS "+rfss+" · Site "+number];}
  if(protocol=="NXDN"){var match=Regex.Match(c.SiteScope,@"^NXDN:System (\d+):Site (\d+)$");if(match.Success)known=[protocol,"System "+match.Groups[1].Value,"Site "+match.Groups[2].Value];}
  string identityText=protocol=="LTR"?string.Join(" · ",new[]{area,home}.Where(v=>v.Length>0)):string.Join(" · ",new[]{network,site,cc}.Where(v=>v.Length>0));if(protocol=="DMR"&&variants.Length==1)identityText=variants[0]+(identityText.Length>0?" · "+identityText:"");if(identityText.Length==0)identityText=protocol=="P25"?DiscoveryNames.Label(c).Replace("P25 · ",""):c.ChannelIds.Length>0?"Ch "+c.ChannelIds:"Observed channel";
  return new(new(){["protocol"]=protocol,["subtype"]=subtype,["cc"]=cc,["network"]=network,["site"]=site,["area"]=area,["home"]=home,["channel"]=c.ChannelIds.Length>0?"Ch "+c.ChannelIds:"",["identity"]=identityText,["frequency"]=DiscoveryNames.Frequency(c.FrequencyHz)+" MHz"},known,string.Join("; ",warnings));
 }
 internal string[] Folders(OrganizationRules rules)
 {
  string protocol=Values["protocol"];if(KnownPath.Length>0&&(rules.Layout=="SystemSite"||rules.PreferKnownSites&&rules.Layout=="SubtypeColor"))return KnownPath;
  if(rules.Layout=="Protocol"||protocol=="LTR"&&rules.Layout!="Custom")return[protocol];
  string template=rules.Layout=="Custom"?rules.FolderTemplate:"{protocol}/{subtype}/{cc}";var parts=template.Split('/').Select(Render).Where(p=>p.Length>0).ToArray();return parts.Length==0?[protocol]:parts;
 }
}
internal sealed record OrganizationChange(string Action,string Entry,string Before,string After,string Detail,string MHz="",string RecordId="");
internal sealed record OrganizationPlan(string Stamp,List<FrequencyItem> Items,OrganizationRules Rules,bool SaveFuture,List<OrganizationChange> Changes);
internal sealed record OrganizationReceipt(string Library,string BeforeHash,string AfterHash);
public sealed partial class FrequencyLibrary
{
 internal OrganizationRules? Organization=>organization;
 internal string LibraryPath=>Path.GetFullPath(path);
 private string OrganizationStamp=>SyncJson.Hash(revision??"<new>");
 internal static Guid OrganizationFolder(List<FrequencyItem> values,string[] labels,string? knownScope=null)
 {
  Guid? parent=null;var prefix=new List<string>();foreach(string label in labels){if(string.IsNullOrWhiteSpace(label)||label.Length>160)throw new IOException("A generated folder label is empty or exceeds 160 characters.");prefix.Add(label);string key="organization:v1:"+SyncJson.Hash(string.Join('\u001f',prefix));string? legacy=null;if(knownScope!=null){var parts=knownScope.Split(':');int level=prefix.Count-1;legacy=level==0?"root:"+parts[0]:level==labels.Length-1?"site:"+knownScope:"folder:"+string.Join(':',parts.Take(level+1));}var folder=values.FirstOrDefault(f=>f.IsFolder&&f.ParentId==parent&&f.BindingKey==key)??(legacy==null?null:values.FirstOrDefault(f=>f.IsFolder&&f.ParentId==parent&&f.BindingKey==legacy))??values.FirstOrDefault(f=>f.IsFolder&&f.ParentId==parent&&f.Name.Equals(label,StringComparison.OrdinalIgnoreCase));if(folder==null){folder=new(){IsFolder=true,Name=label,ParentId=parent,BindingKey=key};values.Add(folder);}parent=folder.Id;}return parent!.Value;
 }
 private FrequencyItem OrganizeDiscovery(List<FrequencyItem> values,FrequencyItem item,bool rename=true)
 {if(organization==null)return item;var fields=OrganizationFields.From(item);if(fields.Warning.Length>0)return item;string name=item.Name;if(rename&&organization.RenameGenerated&&DiscoveryNames.Generated(name))name=fields.Render(organization.NameTemplate);if(name.Length==0||name.Length>160)throw new IOException("Generated name must contain 1–160 characters.");return item with{ParentId=OrganizationFolder(values,fields.Folders(organization),fields.KnownPath.SequenceEqual(fields.Folders(organization))?item.SiteScope:null),Name=name};}
 internal OrganizationPlan PreviewOrganization(OrganizationRules rules,IEnumerable<Guid>? selected,bool existing,bool future,bool prune)
 {
  rules.Validate();var next=items.ToList();var changes=new List<OrganizationChange>();HashSet<Guid>? chosen=selected?.ToHashSet();if(existing&&chosen is{Count:0})throw new IOException("Select entries or a folder first, or choose Entire library.");if(chosen!=null){bool added;do{added=false;foreach(var item in items)if(item.ParentId is {} parent&&chosen.Contains(parent))added|=chosen.Add(item.Id);}while(added);}
  string Location(FrequencyItem item,List<FrequencyItem> list){var names=new List<string>();var parent=item.ParentId;for(int depth=0;parent!=null&&depth<64;depth++){var folder=list.Single(f=>f.Id==parent);names.Insert(0,folder.Name);parent=folder.ParentId;}return string.Join(" / ",names);}
  var ancestors=new HashSet<Guid>();
  if(existing)foreach(var item in items.Where(i=>!i.IsFolder&&(chosen==null||chosen.Contains(i.Id)))){var fields=OrganizationFields.From(item);if(fields.Warning.Length>0){changes.Add(new("Held",item.Name,Location(item,items),Location(item,items),fields.Warning,DiscoveryNames.Frequency(item.FrequencyHz),item.Id.ToString()));continue;}var parent=item.ParentId;while(parent!=null){ancestors.Add(parent.Value);parent=items.Single(f=>f.Id==parent).ParentId;}string label=item.Name;if(rules.RenameGenerated&&DiscoveryNames.Generated(label))label=fields.Render(rules.NameTemplate);if(label.Length==0||label.Length>160){changes.Add(new("Held",item.Name,Location(item,items),Location(item,items),"Generated name must contain 1–160 characters",DiscoveryNames.Frequency(item.FrequencyHz),item.Id.ToString()));continue;}var updated=item with{ParentId=OrganizationFolder(next,fields.Folders(rules),fields.KnownPath.SequenceEqual(fields.Folders(rules))?item.SiteScope:null),Name=label};next[next.FindIndex(i=>i.Id==item.Id)]=updated;if(updated!=item)changes.Add(new("Move / label",item.Name,Location(item,items)+" / "+item.Name,Location(updated,next)+" / "+updated.Name,"Stable ID, frequency, scope, evidence and notes retained",DiscoveryNames.Frequency(item.FrequencyHz),item.Id.ToString()));}
  if(prune&&existing){bool removed;do{removed=false;foreach(var f in next.Where(f=>f.IsFolder&&f.ParentId!=null&&ancestors.Contains(f.Id)&&f.Notes.Length==0&&!f.ScannerFolder&&!next.Any(i=>i.ParentId==f.Id)&&RemovableFolder(f,next)).ToArray()){next.Remove(f);changes.Add(new("Empty folder",f.Name,Location(f,items),"Removed","Empty generated folder only"));removed=true;}}while(removed);}
  if((future?rules:null)!=organization)changes.Add(new("Saving rules","Future decoder / RF discoveries",organization?.Layout??"Legacy",future?rules.Layout:"Legacy","Stored locally in this library; not exported to website"));Validate(next);return new(OrganizationStamp,next,rules,future,changes);
 }
 private static bool RemovableFolder(FrequencyItem f,List<FrequencyItem> all){if(f.BindingKey.StartsWith("organization:")){var labels=new List<string>{f.Name};var parent=f.ParentId;while(parent!=null){var p=all.Single(i=>i.Id==parent);labels.Insert(0,p.Name);parent=p.ParentId;}return f.BindingKey=="organization:v1:"+SyncJson.Hash(string.Join('\u001f',labels));}return (f.BindingKey.StartsWith("folder:")||f.BindingKey.StartsWith("site:"))&&Regex.IsMatch(f.Name,@"^(Unassigned at [\d.]+|Unresolved site|Unidentified system|DMR|LTR Standard|Capacity Plus|Connect Plus|DMR Tier III|Motorola rest signaling|Network \d+|Site \d+|RFSS \d+ - Site \d+|[A-F0-9]{1,5}-[A-F0-9]{1,3}|Provisional system .*)$",RegexOptions.IgnoreCase);}
 internal string ApplyOrganization(OrganizationPlan plan)
 {
  if(plan.Stamp!=OrganizationStamp)throw new IOException("The library changed after preview. Preview again.");Validate(plan.Items);var oldChannels=items.Where(i=>!i.IsFolder).ToDictionary(i=>i.Id);var newChannels=plan.Items.Where(i=>!i.IsFolder).ToDictionary(i=>i.Id);if(!oldChannels.Keys.ToHashSet().SetEquals(newChannels.Keys)||oldChannels.Any(p=>(newChannels[p.Key] with{Name=p.Value.Name,ParentId=p.Value.ParentId})!=p.Value))throw new IOException("Organization cannot add/delete channels or change their frequencies, identifiers or evidence.");plan.Rules.Validate();return CommitWithRollback(plan.Items,plan.SaveFuture?plan.Rules:null,replacePolicy:true);
 }
 private string CommitWithRollback(List<FrequencyItem> channels, OrganizationRules? rules=null, bool replacePolicy=false)
 {
  string backup=path+".before-organization-"+DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8]+".json";
  Commit(channels,true,policy:rules,replacePolicy:replacePolicy,organizationBackup:backup);
  if(File.Exists(backup))SyncJson.Atomic(backup+".receipt.json",JsonSerializer.Serialize(new OrganizationReceipt(LibraryPath,SyncJson.Hash(File.ReadAllText(backup)),OrganizationStamp)));return backup;
 }
 internal string[] OrganizationBackups()=>Directory.Exists(Path.GetDirectoryName(LibraryPath))?Directory.GetFiles(Path.GetDirectoryName(LibraryPath)!,Path.GetFileName(path)+".before-organization-*.json.receipt.json").OrderByDescending(p=>p,StringComparer.Ordinal).Take(100).Select(p=>p[..^13]).ToArray():[];
 internal void RollbackOrganization(string backup)
 {
  if(!OrganizationBackups().Contains(backup))throw new IOException("Select an organization backup for this library.");var receipt=JsonSerializer.Deserialize<OrganizationReceipt>(File.ReadAllText(backup+".receipt.json"))??throw new IOException("Invalid rollback receipt");string content=File.ReadAllText(backup);if(receipt.Library!=LibraryPath||SyncJson.Hash(content)!=receipt.BeforeHash||OrganizationStamp!=receipt.AfterHash)throw new IOException("Library or backup changed after organization. Rollback held to protect later edits; use import preview to recover selected records.");var validated=new FrequencyLibrary(backup);using var guard=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);if(File.ReadAllText(path)!=revision)throw new IOException("Another window changed this library. Reload first.");string temp=path+".organization-rollback.tmp";File.WriteAllText(temp,content);File.Replace(temp,path,path+".before-rollback-"+Guid.NewGuid().ToString("N")+".json",true);items=validated.items;records=validated.records;pendingDiscoveries=validated.pendingDiscoveries;updateExistingOnly=validated.updateExistingOnly;organization=validated.organization;receiverBImported=validated.receiverBImported;revision=content;undo.Clear();guard.Dispose();Saved?.Invoke(this,LibraryPath);
 }
}
