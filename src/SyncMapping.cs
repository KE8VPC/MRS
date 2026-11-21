using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record SyncIdentity(string Protocol,string SystemKey,string SiteKey,JsonObject SystemIds,JsonObject SiteIds,string SystemLabel,string SiteLabel);
internal static class SyncMapping
{
 internal static readonly HashSet<string> IdKeys=["wacn","sysid","nac","rfss","site","tgid","color_code","ran","dmr_id","unit","network"];
 internal static string Ref(string kind,string key)=>"mrs:"+kind+":"+SyncJson.Hash(key)[..32];
 internal static string Value(string text,string label){var m=Regex.Match(text,@"\b"+label+@"\s+([A-Za-z0-9]+)",RegexOptions.IgnoreCase);return m.Success?m.Groups[1].Value:"";}
 internal static string Decimal(string text)=>ulong.TryParse(text,out var n)?n.ToString(CultureInfo.InvariantCulture):text;
 internal static string Hex(string text){text=text.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?text[2..]:text;if(!Regex.IsMatch(text,@"^[0-9a-fA-F]{1,12}$"))return "";return text.TrimStart('0').ToUpperInvariant() is {Length:>0} h?h:"0";}
 internal static string Protocol(FrequencyItem c){if(c.SiteScope.StartsWith("P25:",StringComparison.OrdinalIgnoreCase)||c.Protocols.Contains("P25",StringComparison.OrdinalIgnoreCase))return "P25";foreach(string variant in new[]{"DMR Tier III","Connect Plus","Capacity Plus"})if(c.SiteScope.Contains(variant,StringComparison.OrdinalIgnoreCase)||c.Protocols.Equals(variant,StringComparison.OrdinalIgnoreCase))return variant;return c.Protocols.Equals("DMR",StringComparison.OrdinalIgnoreCase)?"DMR":c.Protocols;}

 internal static SyncIdentity Identity(FrequencyItem c)
 {
  string protocol=Protocol(c),scope=c.SiteScope;var parts=scope.Split(':');var sys=new JsonObject();var site=new JsonObject();string label=protocol;
  if(protocol=="P25"){
   var pair=parts.Length>1?Regex.Match(parts[1],@"^([0-9A-Fa-f]+)-([0-9A-Fa-f]+)$"):Match.Empty;
   string wacn=Hex(Value(c.Signaling,"WACN")),sid=Hex(Value(c.Signaling,"SYSTEM"));if(pair.Success){wacn=Hex(pair.Groups[1].Value);sid=Hex(pair.Groups[2].Value);}if(wacn.Length>0)sys["wacn"]=wacn;if(sid.Length>0)sys["sysid"]=sid;
   string rfss=Value(c.Signaling,"RFSS"),number=Value(c.Signaling,"SITE");if(parts.Length==4&&pair.Success){rfss=parts[2];number=parts[3];}if(Regex.IsMatch(rfss,@"^\d+$"))site["rfss"]=Decimal(rfss);if(Regex.IsMatch(number,@"^\d+$"))site["site"]=Decimal(number);label=protocol+(sid.Length>0?" "+wacn+"-"+sid:"");
  }else if(protocol is "DMR" or "DMR Tier III" or "Capacity Plus" or "Connect Plus"){
   string net=Value(scope+"; "+c.Signaling,"NETWORK"),number=Value(scope+"; "+c.Signaling,"SITE");if(Regex.IsMatch(net,@"^\d+$"))sys["network"]=Decimal(net);if(Regex.IsMatch(number,@"^\d+$"))site["site"]=Decimal(number);label=protocol+(net.Length>0?" Network "+net:"");
  }
  bool complete=protocol=="P25"?sys.ContainsKey("wacn")&&sys.ContainsKey("sysid"):sys.ContainsKey("network")&&protocol!="DMR";string systemKey=protocol+"|"+(sys.Count>0?sys.ToJsonString()+(complete?"":"|partial:"+scope):"unknown:"+scope);string siteKey=systemKey+"|"+(site.Count>0?site.ToJsonString():"unresolved:"+scope);
  return new(protocol,systemKey,siteKey,sys,site,label,site.Count>0?"Site "+SyncJson.Text(site["site"]):"Unresolved site");
 }
 internal static List<SyncDraft> Build(DiscoveryBundle bundle,SyncDocument state,bool notes=false,bool radios=false)
 {
  DiscoveryExchange.Validate(bundle);var drafts=new Dictionary<string,SyncDraft>();var scopes=new Dictionary<string,(string System,string Site,SyncIdentity Identity)>();
  JsonObject Base(string type,string protocol,string label)=>new(){["type"]=type,["protocol"]=protocol,["label"]=label,["prov"]=new JsonArray("manual")};
  (string System,string Site,SyncIdentity Identity) Scope(FrequencyItem channel){var identity=Identity(channel);string system=Ref("s",identity.SystemKey),site=Ref("t",identity.SiteKey);
   if(!drafts.ContainsKey(system)){var parentRecord=Base("system",identity.Protocol,identity.SystemLabel);parentRecord["prov"]=new JsonArray();drafts[system]=new(system,parentRecord);}drafts[system].Record["ids"]=identity.SystemIds.DeepClone();
   if(!drafts.ContainsKey(site)){var r=Base("site",identity.Protocol,identity.SiteLabel);r["prov"]=new JsonArray();r["parent_ref"]=system;r["ids"]=identity.SiteIds.DeepClone();drafts[site]=new(site,r);}
   foreach(string reference in new[]{system,site}){var provenance=drafts[reference].Record["prov"]!.AsArray();string source=Origin(channel);if(!provenance.Any(p=>SyncJson.Text(p)==source))provenance.Add(source);}
   var result=(system,site,identity);scopes[channel.SiteScope]=result;string[] parts=channel.SiteScope.Split(':');if(parts.Length>=2)scopes[string.Join(':',parts.Take(parts[0]=="DMR"?3:2))]=result;return result;
  }
  foreach(var channel in bundle.Channels.Where(c=>!c.IsFolder)){
   string reference="mrs:f:"+channel.Id.ToString("N");string protocol=Protocol(channel);var r=Base("frequency",protocol,channel.Name);r["mhz"]=PlanCsv.MHz(channel.FrequencyHz);r["prov"]=new JsonArray(Origin(channel));
   if(notes)r["notes"]=channel.Notes;r["attrs"]=new JsonObject{["mode"]=channel.Mode,["bandwidth_hz"]=channel.BandwidthHz};
   var ids=new JsonObject();string cc=Value(channel.Signaling+"; "+channel.DmrIdentifiers,"CC"),nac=Hex(Value(channel.Signaling,"NAC")),ran=Value(channel.Signaling,"RAN");if(Regex.IsMatch(cc,@"^\d+$"))ids["color_code"]=Decimal(cc);if(nac.Length>0)ids["nac"]=nac;if(Regex.IsMatch(ran,@"^\d+$"))ids["ran"]=Decimal(ran);r["ids"]=ids;
   bool provisional=channel.EvidenceStatus.StartsWith("Provisional",StringComparison.OrdinalIgnoreCase);
   bool legacyReceive=protocol is "DMR" or "DMR Tier III" or "Capacity Plus" or "Connect Plus" or "LTR" or "NXDN"&&
    channel.Tags.Contains("Site channel discovery",StringComparison.Ordinal)&&
    channel.EvidenceStatus is "Observed" or "Provisional identity";
   var received=channel.LastReceivedOnFrequencyUtc??(legacyReceive?channel.ChannelLastSeen:null);
   if(channel.EvidenceStatus.StartsWith("Website")&&state.Bindings.TryGetValue(reference,out var importedBinding)){string confidence=SyncJson.Text(importedBinding.Baseline["evidence"]);if(new[]{"","observed","advertised","provisional","reference"}.Contains(confidence))r["evidence"]=confidence;}
   else if(channel.Activity is {} activity)
   {
    r["evidence"]="observed";
    r["first_seen"]=activity.FirstSeen.ToUnixTimeSeconds();
    r["last_seen"]=(received>activity.LastSeen?received.Value:activity.LastSeen).ToUnixTimeSeconds();
    r["heard"]=activity.Hits;
   }
   else if(received is {} seen)
   {
    r["evidence"]=provisional?"provisional":"observed";
    r["last_seen"]=seen.ToUnixTimeSeconds();
   }
   else if(channel.EvidenceStatus.StartsWith("Advertised",StringComparison.OrdinalIgnoreCase)||
           channel.Signaling.Contains("advertised target",StringComparison.OrdinalIgnoreCase))
    r["evidence"]="advertised";
   else if(channel.EvidenceStatus.Length>0)r["evidence"]=provisional?"provisional":"observed";
   if(channel.SiteScope.Length>0){var context=Scope(channel);r["parent_ref"]=context.Site;
    foreach(var mapping in Channels(channel.ChannelIds,protocol)){
     string key=Ref("m",context.Site+"|"+mapping.Convention+"|"+mapping.Channel+"|"+reference);var m=Base("lcn",protocol,"Channel "+mapping.Channel);m["prov"]=new JsonArray(Origin(channel));m["parent_ref"]=context.Site;m["target_ref"]=reference;m["chan_no"]=mapping.Channel;m["chan_conv"]=mapping.Convention;m["slot"]="";
     var count=Regex.Match(channel.EvidenceStatus,@"Passed:\s*(\d+)/(\d+) separate calls");bool passed=count.Success&&channel.Tags.Contains("LCN survey")&&!mapping.Tentative;
     m["verify_status"]=passed?"passed":"tentative";if(passed){m["verify_rule"]=$"M.R.S. single-receiver discovery: {count.Groups[2].Value} separate matching calls; TG/slot/CC and available radio ID; no unresolved conflict";m["verify_count"]=int.Parse(count.Groups[1].Value);}
     drafts[key]=new(key,m,LocalVerification:channel.Tags.Contains("LCN survey"));
    }
   }
   drafts[reference]=new(reference,r,channel.Id.ToString(),LocalScope:channel.SiteScope);
  }
  // Saved following profiles can provide a system/site context even when no frequency folder was selected.
  foreach(var record in bundle.Records.Where(r=>r.Kind=="system"&&scopes.ContainsKey(r.Scope)&&r.Label.Length>0))drafts[scopes[record.Scope].System].Record["label"]=record.Label;
  foreach(var record in bundle.Records.Where(r=>r.Kind=="site"&&r.Label.Length>0)){string identity=record.Fields.GetValueOrDefault("identity_scope",record.Identifier);if(scopes.TryGetValue(identity,out var context))drafts[context.Site].Record["label"]=record.Label;}
  foreach(var record in bundle.Records.Where(r=>r.Kind=="system"&&!scopes.ContainsKey(r.Scope))){string net=record.Fields.GetValueOrDefault("network_id","");string site=record.Fields.GetValueOrDefault("site_id","");string scope=record.Protocol=="P25"?record.Scope:$"DMR:{record.Protocol}:Network {net}:Site {site}";var context=Scope(new(){SiteScope=scope,Protocols=record.Protocol});scopes[record.Scope]=context;if(record.Label.Length>0)drafts[context.System].Record["label"]=record.Label;}
  foreach(var record in bundle.Records.Where(r=>r.Kind is "talkgroup" or "radio")){
   if(record.Kind=="radio"&&!radios)continue;var r=Base(record.Kind,record.Protocol,record.Label);r["prov"]=new JsonArray("imported");string ident=record.Identifier;if(!Regex.IsMatch(ident,@"^\d+$"))continue;r["ids"]=new JsonObject{[record.Kind=="radio"?"dmr_id":"tgid"]=Decimal(ident)};
   if(record.Kind=="talkgroup"){if(!scopes.TryGetValue(record.Scope,out var context))continue;r["parent_ref"]=context.System;r["protocol"]=context.Identity.Protocol;}
   string key=Ref(record.Kind=="radio"?"r":"g",record.Key);drafts[key]=new(key,r);
  }
  foreach(var record in bundle.Records.Where(r=>r.Kind=="channel_map")){
   if(!scopes.TryGetValue(record.Scope,out var context)||!long.TryParse(record.Fields.GetValueOrDefault("frequency_hz"),out var hz))continue;
   string frequency=drafts.Values.FirstOrDefault(d=>d.Type=="frequency"&&SyncJson.Text(d.Record["parent_ref"])==context.Site&&ParseHz(d.Record["mhz"])==hz)?.Ref??Ref("f",context.Site+"|"+hz);
   if(!drafts.ContainsKey(frequency)){var f=Base("frequency",context.Identity.Protocol,"Carrier");f["mhz"]=PlanCsv.MHz(hz);f["parent_ref"]=context.Site;f["prov"]=new JsonArray("imported");drafts[frequency]=new(frequency,f);}
   string convention=record.Fields.GetValueOrDefault("convention","lcn-1");string number=record.Fields.GetValueOrDefault("chan_no",record.Identifier);string key=Ref("m",context.Site+"|"+convention+"|"+number+"|"+frequency);if(drafts.TryGetValue(key,out var existingMap)&&existingMap.LocalVerification)continue;var m=Base("lcn",context.Identity.Protocol,record.Label.Length>0?record.Label:"Channel "+number);m["parent_ref"]=context.Site;m["target_ref"]=frequency;m["chan_no"]=number;m["chan_conv"]=convention;m["slot"]=record.Fields.GetValueOrDefault("slot","");m["verify_status"]=record.Fields.GetValueOrDefault("verification","tentative");if(SyncJson.Text(m["verify_status"])=="passed"){m["verify_rule"]=record.Fields.GetValueOrDefault("verify_rule","");m["verify_count"]=long.TryParse(record.Fields.GetValueOrDefault("verify_count"),out var count)?count:0;}m["prov"]=new JsonArray("imported");drafts[key]=new(key,m);
  }
  // A known semantic match is never silently recreated under a different ref.
  var list=drafts.Values.ToList();foreach(var d in list){ValidateOutgoing(d.Record);if(state.Bindings.GetValueOrDefault(d.Ref)?.Gone==true){list[list.IndexOf(d)]=d with{Warning="Deleted on website; local record retained. This client will not recreate it."};continue;}if(state.Bindings.ContainsKey(d.Ref))continue;var matches=state.Remote.Values.Where(r=>Semantic(d.Record,r,drafts,state)).ToArray();if(matches.Length>0){int index=list.IndexOf(d);list[index]=d with{Warning="The website already has a possible "+d.Type+" match. Download and apply/link it before uploading; no new duplicate will be created."};}}
  return list.OrderBy(d=>Order(d.Type)).ThenBy(d=>d.Ref).ToList();
 }
 internal static string Origin(FrequencyItem channel)=>channel.Tags.Contains("reference",StringComparison.OrdinalIgnoreCase)?"reference":channel.Tags.Contains("Site channel discovery")||channel.Tags.Contains("LCN survey")?"decoded":channel.Tags.Contains("import",StringComparison.OrdinalIgnoreCase)||channel.Tags.Contains("Website sync")?"imported":"manual";
 internal static int Order(string type)=>type switch{"system"=>0,"site"=>1,"frequency" or "talkgroup" or "radio" or "other"=>2,"lcn"=>3,_=>9};
 internal static long ParseHz(JsonNode? value)=>decimal.TryParse(SyncJson.Text(value),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var mhz)&&mhz>0&&mhz<=6000&&mhz*1000000==decimal.Truncate(mhz*1000000)?(long)(mhz*1000000):0;
 internal static IEnumerable<(string Channel,string Convention,bool Tentative)> Channels(string text,string protocol)
 {
  foreach(string raw in text.Split(", ",StringSplitOptions.RemoveEmptyEntries)){
   var p25=Regex.Match(raw,@"^(\d{1,2})-(\d{1,4})$");if(protocol=="P25"&&p25.Success&&int.Parse(p25.Groups[1].Value)<=15&&int.Parse(p25.Groups[2].Value)<=4095){yield return (((int.Parse(p25.Groups[1].Value)<<12)|int.Parse(p25.Groups[2].Value)).ToString("X4"),"p25-chan",false);continue;}
   var lcn=Regex.Match(raw,@"^LCN (\??)(\d+)$");if(lcn.Success)yield return(lcn.Groups[2].Value,"lcn-1",lcn.Groups[1].Value=="?");
  }
 }
 private static bool Semantic(JsonObject local,JsonObject remote,Dictionary<string,SyncDraft> drafts,SyncDocument state)
 {
  if(SyncJson.Text(local["type"])!=SyncJson.Text(remote["type"])||SyncJson.Text(local["protocol"])!=SyncJson.Text(remote["protocol"]))return false;
  string type=SyncJson.Text(local["type"]);if(type=="system")return local["ids"] is JsonObject ids&&ids.Count>0&&ids.All(p=>SyncJson.Text(remote["ids"]?[p.Key])==SyncJson.Text(p.Value));
  string parent=SyncJson.Text(local["parent_ref"]);if(!state.Bindings.TryGetValue(parent,out var binding)||SyncJson.Number(remote["parent"])!=binding.Id)return false;
  if(type=="frequency")return ParseHz(local["mhz"])==ParseHz(remote["mhz"]);
  if(type is "site" or "talkgroup")return local["ids"] is JsonObject identity&&identity.Count>0&&identity.All(p=>SyncJson.Text(remote["ids"]?[p.Key])==SyncJson.Text(p.Value));
  return type=="lcn"&&SyncJson.Text(local["chan_no"])==SyncJson.Text(remote["chan_no"])&&SyncJson.Text(local["chan_conv"])==SyncJson.Text(remote["chan_conv"])&&SyncJson.Text(local["slot"])==SyncJson.Text(remote["slot"]);
 }
 internal static void ValidateOutgoing(JsonObject record)
 {
  if(Regex.IsMatch(record.ToJsonString(),@"k8v_[a-fA-F0-9]{48}"))throw new IOException("A device token was found in record content; remove it before uploading.");
  string type=SyncJson.Text(record["type"]);if(type is not("frequency" or "system" or "site" or "talkgroup" or "lcn" or "radio" or "other"))throw new IOException("Unknown website record type");
  string[] Enum(JsonObject _,string field)=>field switch{"chan_conv"=>["","lcn-1","lcn-0","p25-chan","dmr-lcn","nxdn-ch","other"],"slot"=>["","1","2"],"verify_status"=>["","tentative","passed","failed"],_=>["","observed","advertised","provisional","reference"]};
  foreach(string field in new[]{"chan_conv","slot","verify_status","evidence"})if(record.ContainsKey(field)&&!Enum(record,field).Contains(SyncJson.Text(record[field])))throw new IOException("Invalid "+field+"; upload blocked to prevent silent clearing.");
  if(record["ids"] is JsonObject ids&&ids.Any(p=>!IdKeys.Contains(p.Key)||!Regex.IsMatch(SyncJson.Text(p.Value),@"^[A-Za-z0-9._:-]{1,24}$")))throw new IOException("Invalid website identifier");
  if(type=="frequency"&&ParseHz(record["mhz"])==0)throw new IOException("Invalid website frequency");if(type=="lcn"&&(!Regex.IsMatch(SyncJson.Text(record["chan_no"]),@"^[A-Za-z0-9.-]{1,16}$")||SyncJson.Text(record["chan_conv"])=="p25-chan"&&!Regex.IsMatch(SyncJson.Text(record["chan_no"]),@"^[0-9A-F]{4}$")))throw new IOException("Invalid channel mapping number");
  if(SyncJson.Text(record["verify_status"])=="passed"&&(SyncJson.Text(record["verify_rule"]).Length is 0 or >160||SyncJson.Number(record["verify_count"])<1))throw new IOException("Passed mapping needs its actual verification rule/count");
  if(record.ContainsKey("force")||record.ContainsKey("revive")||record.ContainsKey("visibility"))throw new IOException("Client never forces, revives or changes visibility in this preview.");
 }
}
