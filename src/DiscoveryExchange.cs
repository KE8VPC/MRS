using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace MRS;
internal sealed record DiscoveryRecord(Guid Id,string Kind,string Scope,string Identifier,string Label,string Protocol,Dictionary<string,string> Fields)
{
 internal string Key=>Kind+"|"+Scope+"|"+Identifier;
 internal static DiscoveryRecord Create(string kind,string scope,string identifier,string label,string protocol,Dictionary<string,string>? fields=null)=>new(new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(kind+"|"+scope+"|"+identifier)).AsSpan(0,16)),kind,scope,identifier,label,protocol,fields??[]);
}
internal sealed record DiscoveryBundle(string Format,int Version,List<FrequencyItem> Channels,List<DiscoveryRecord> Records);
internal static class DiscoveryExchange
{
 internal const string Format="MRS discoveries";
 internal static readonly JsonSerializerOptions Json=new(){WriteIndented=true,MaxDepth=80};
 internal static void ValidateRecords(IEnumerable<DiscoveryRecord> records)
 {
  var all=records.ToArray();if(all.Length>20000||all.Any(r=>r==null||r.Id==Guid.Empty||r.Kind is not("system" or "site" or "talkgroup" or "radio" or "bandplan" or "channel_map")||string.IsNullOrWhiteSpace(r.Scope)||string.IsNullOrWhiteSpace(r.Identifier)||r.Scope.Length>1000||r.Identifier.Length>1000||r.Label==null||r.Label.Length>1000||r.Protocol==null||r.Protocol.Length>100||r.Fields==null||r.Fields.Count>64||r.Fields.Any(p=>p.Key.Length>120||p.Value==null||p.Value.Length>12000))||all.Select(r=>r.Id).Distinct().Count()!=all.Length||all.Select(r=>r.Key).Distinct().Count()!=all.Length)throw new IOException("Invalid, duplicate or oversized discovery catalog records.");
 }
 internal static void Validate(DiscoveryBundle b)
 {
  if(b.Format!=Format||b.Version!=1||b.Channels==null||b.Records==null||b.Channels.Count>20000)throw new IOException("Unsupported discovery format/version or too many records.");
  FrequencyLibrary.Validate(b.Channels);ValidateRecords(b.Records);
 }
 internal static DiscoveryBundle Read(string file)
 {
  if(new FileInfo(file).Length>32*1024*1024)throw new IOException("Import exceeds 32 MB.");
  DiscoveryBundle bundle;
  if(Path.GetExtension(file).Equals(".csv",StringComparison.OrdinalIgnoreCase))bundle=ReadCsv(file);
  else if(Path.GetExtension(file).Equals(".json",StringComparison.OrdinalIgnoreCase)){
   string text=File.ReadAllText(file);using var doc=JsonDocument.Parse(text,new JsonDocumentOptions{MaxDepth=80});
   if(!doc.RootElement.TryGetProperty("Format",out _)&&doc.RootElement.TryGetProperty("Items",out var legacy)&&doc.RootElement.TryGetProperty("Version",out var version)&&version.GetInt32() is 1 or 2)bundle=new(Format,1,legacy.Deserialize<List<FrequencyItem>>(Json)??[],doc.RootElement.TryGetProperty("Records",out var catalog)?catalog.Deserialize<List<DiscoveryRecord>>(Json)??[]:[]);
   else bundle=JsonSerializer.Deserialize<DiscoveryBundle>(text,Json)??throw new IOException("Empty discovery document.");
  }
  else{
   using var zip=ZipFile.OpenRead(file);if(zip.Entries.Count>3||zip.Entries.Select(e=>e.FullName).Distinct().Count()!=zip.Entries.Count||zip.Entries.Count(e=>e.FullName=="discoveries.json")!=1||zip.Entries.Any(e=>e.FullName is not("discoveries.json" or "channels.csv" or "catalog.csv")||e.Length>16*1024*1024))throw new IOException("Expected a bounded M.R.S. discovery bundle.");
   using var stream=zip.GetEntry("discoveries.json")!.Open();using var bounded=new MemoryStream();byte[] block=new byte[8192];int count;while((count=stream.Read(block))>0){if(bounded.Length+count>16*1024*1024)throw new IOException("Discovery JSON exceeds 16 MB.");bounded.Write(block,0,count);}bundle=JsonSerializer.Deserialize<DiscoveryBundle>(bounded.ToArray(),Json)??throw new IOException("Empty discovery document.");
  }
  Validate(bundle);return bundle;
 }
 internal static DiscoveryBundle ReadCsv(string file)
 {
  var channels=new List<FrequencyItem>();var records=new List<DiscoveryRecord>();
  foreach(var row in PlanCsv.Read(file)){
   string Get(string key)=>PlanCsv.Get(row,key);if(Get("record_kind").Length>0){records.Add(new(Guid.Parse(Get("id")),Get("record_kind"),Get("scope"),Get("identifier"),Get("label"),Get("protocol"),JsonSerializer.Deserialize<Dictionary<string,string>>(Get("fields_json"))??[]));continue;}
   var item=Get("details_json").Length>0?JsonSerializer.Deserialize<FrequencyItem>(Get("details_json"),Json)??throw new IOException("Invalid channel details"):new FrequencyItem{Name=Get("name")};
   bool folder=bool.TryParse(Get("is_folder"),out bool f)&&f;Guid id=Guid.TryParse(Get("id"),out var parsed)?parsed:item.Id;Guid? parent=Guid.TryParse(Get("parent_id"),out var par)?par:null;
   item=item with{Id=id,ParentId=parent,IsFolder=folder,Name=Get("name"),FrequencyHz=folder?0:PlanCsv.Hz(row,"frequency")!.Value,Mode=Get("mode").Length>0?Get("mode"):item.Mode,Protocols=Get("protocol"),SiteScope=Get("site_scope"),Signaling=Get("identifiers"),Notes=Get("notes")};channels.Add(item);
  }
  return new(Format,1,channels,records);
 }
 internal static string ChannelsCsv(IEnumerable<FrequencyItem> channels)
 {
  string[] header=["id","parent_id","is_folder","name","frequency_mhz","mode","protocol","site_scope","identifiers","notes","details_json"];
  return Csv(header,channels.Select(c=>new[]{c.Id.ToString(),c.ParentId?.ToString()??"",c.IsFolder.ToString(),c.Name,c.IsFolder?"":PlanCsv.MHz(c.FrequencyHz),c.Mode,c.Protocols,c.SiteScope,c.Signaling,c.Notes,JsonSerializer.Serialize(c)}));
 }
 internal static string CatalogCsv(IEnumerable<DiscoveryRecord> records)=>Csv(["id","record_kind","scope","identifier","label","protocol","fields_json"],records.Select(r=>new[]{r.Id.ToString(),r.Kind,r.Scope,r.Identifier,r.Label,r.Protocol,JsonSerializer.Serialize(r.Fields)}));
 private static string Csv(string[] columns,IEnumerable<string[]> rows)=>string.Join(',',columns)+"\r\n"+string.Join("\r\n",rows.Select(r=>string.Join(',',r.Select(PlanCsv.Quote))))+"\r\n";
 internal static void Write(string file,DiscoveryBundle bundle,bool csv=false,bool catalog=false)
 {
  Validate(bundle);string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   if(csv)File.WriteAllText(temp,catalog?CatalogCsv(bundle.Records):ChannelsCsv(bundle.Channels),new UTF8Encoding(false));
   else if(Path.GetExtension(file).Equals(".json",StringComparison.OrdinalIgnoreCase))File.WriteAllText(temp,JsonSerializer.Serialize(bundle,Json),new UTF8Encoding(false));
   else{using var stream=File.Create(temp);using(var zip=new ZipArchive(stream,ZipArchiveMode.Create)){void Entry(string name,string value){using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(value);}Entry("discoveries.json",JsonSerializer.Serialize(bundle,Json));Entry("channels.csv",ChannelsCsv(bundle.Channels));Entry("catalog.csv",CatalogCsv(bundle.Records));}}
   File.Move(temp,file,true);
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
