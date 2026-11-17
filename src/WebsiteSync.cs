using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace MRS;

internal static class SyncJson
{
 internal static string Text(JsonNode? node)=>node is JsonValue v?v.ToString():"";
 internal static long Number(JsonNode? node)=>long.TryParse(Text(node),out long value)?value:0;
 internal static JsonObject Copy(JsonObject value)=>(JsonObject)value.DeepClone();
 internal static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
 internal static Guid GuidFor(string value)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0,16));
 internal static void Atomic(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path+".tmp",text,new UTF8Encoding(false));File.Move(path+".tmp",path,true);}
 internal static string Safe(string text)=>System.Text.RegularExpressions.Regex.Replace(text,@"k8v_[a-fA-F0-9]{8,}","[token hidden]");
}
internal sealed record SyncDraft(string Ref,JsonObject Record,string LocalId="",string Warning="",string LocalScope="",bool LocalVerification=false)
{public string Type=>SyncJson.Text(Record["type"]);public string Label=>SyncJson.Text(Record["label"]);}
internal sealed class SyncBinding
{public long Id{get;set;}public long Rev{get;set;}public bool Gone{get;set;}public JsonObject Baseline{get;set;}=new();public string LocalId{get;set;}="";public string LocalScope{get;set;}="";}
internal sealed class SyncPending
{public string Op{get;set;}="upsert";public JsonObject Record{get;set;}=new();public string Reason{get;set;}="Downloaded — review before applying";public string Stamp=>SyncJson.Hash(Op+Record.ToJsonString());public string Display=>$"{Op} · {SyncJson.Text(Record["type"])} · {SyncJson.Text(Record["label"])} · id {SyncJson.Number(Record["id"])} · {Reason}";}
internal sealed class SyncJournal
{public string Ref{get;set;}="";public string State{get;set;}="prepared";public string Detail{get;set;}="";public JsonObject Item{get;set;}=new();}
internal sealed class SyncDocument
{
 public int Version{get;set;}=1;public string Account{get;set;}="";public long Cursor{get;set;}public bool InitialScanComplete{get;set;}public DateTimeOffset? LastContact{get;set;}
 public Dictionary<string,SyncBinding> Bindings{get;set;}=[];public Dictionary<long,JsonObject> Remote{get;set;}=[];public Dictionary<long,SyncPending> Pending{get;set;}=[];public List<SyncJournal> Journal{get;set;}=[];
}
internal sealed class SyncStore(string root)
{
 private string path="";private string? revision;internal SyncDocument Data{get;private set;}=new();
 internal void Load(string account){path=Path.Combine(root,SyncJson.Hash(account)+".json");if(!File.Exists(path)){Data=new(){Account=account};revision=null;return;}if(new FileInfo(path).Length>64000000)throw new IOException("Website sync state exceeds 64 MB; original retained.");string snapshot=File.ReadAllText(path);var d=JsonSerializer.Deserialize<SyncDocument>(snapshot)??throw new IOException("Empty sync state");if(d.Version!=1||d.Account!=account||d.Cursor<0||d.Bindings==null||d.Remote==null||d.Pending==null||d.Journal==null||d.Bindings.Count>20000||d.Remote.Count>20000||d.Pending.Count>20000||d.Journal.Count>5000||d.Bindings.Any(p=>p.Value==null||p.Value.Id<=0||p.Value.Rev<1||p.Value.Baseline==null||p.Value.LocalId==null)||d.Remote.Any(p=>p.Value==null||p.Key<=0)||d.Pending.Any(p=>p.Value==null||p.Value.Record==null||p.Key<=0)||d.Journal.Any(j=>j==null||j.Item==null||j.Ref==null||j.State==null||j.Detail==null))throw new IOException("Invalid website sync state; original retained.");Data=d;revision=snapshot;}
 internal void Save(){string json=JsonSerializer.Serialize(Data,new JsonSerializerOptions{WriteIndented=true});Directory.CreateDirectory(Path.GetDirectoryName(path)!);if(Encoding.UTF8.GetByteCount(json)>64000000)throw new IOException("Website sync state exceeds 64 MB; cursor not advanced.");using var fileLock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.Write,FileShare.None);string? disk=File.Exists(path)?File.ReadAllText(path):null;if(disk!=revision)throw new IOException("Website sync state changed in another window; test connection again before continuing.");SyncJson.Atomic(path,json);revision=json;}
 internal void Stage(JsonObject response)
 {
  if(response["changes"] is not JsonArray changes||response["cursor"]==null)throw new IOException("Invalid changes response; cursor retained.");if(!long.TryParse(SyncJson.Text(response["cursor"]),out long cursor))throw new IOException("Invalid changes cursor; saved state retained.");if(cursor<Data.Cursor)throw new IOException("Server cursor moved backwards; review the account/server before continuing.");
  // Work on a copy: queue and cursor become durable in the same atomic file replacement.
  var previous=Data;var next=JsonSerializer.Deserialize<SyncDocument>(JsonSerializer.Serialize(Data))!;
  var affected=new HashSet<long>();foreach(var value in changes){if(value is not JsonObject change)throw new IOException("Invalid change item");string op=SyncJson.Text(change["op"]);JsonObject record=op=="upsert"&&change["record"] is JsonObject r?SyncJson.Copy(r):op=="delete"?SyncJson.Copy(change):throw new IOException("Unknown change operation");long id=SyncJson.Number(record["id"]);if(id<=0||op=="upsert"&&SyncJson.Number(record["rev"])<1)throw new IOException("Invalid record id/revision");
   if(op=="upsert"&&next.Remote.TryGetValue(id,out var old)&&SyncJson.Number(old["rev"])>SyncJson.Number(record["rev"]))continue;
   bool same=op=="upsert"&&next.Remote.TryGetValue(id,out old)&&JsonNode.DeepEquals(old,record);if(op=="upsert")next.Remote[id]=record;else next.Remote.Remove(id);
   // Same-revision mapping_conflict changes are still queued. Content equality alone suppresses echo duplicates.
   if(!same)affected.Add(id);if(!same||next.Pending.ContainsKey(id))next.Pending[id]=new(){Op=op,Record=record,Reason=op=="delete"?"Deleted on website; local copy will be retained unresolved":"Downloaded — review before applying"};

  }
  for(int depth=0;depth<64;depth++){var children=next.Remote.Where(p=>!affected.Contains(p.Key)&&(affected.Contains(SyncJson.Number(p.Value["parent"]))||affected.Contains(SyncJson.Number(p.Value["target"])))).ToArray();if(children.Length==0)break;foreach(var child in children){affected.Add(child.Key);if(!next.Pending.ContainsKey(child.Key))next.Pending[child.Key]=new(){Record=SyncJson.Copy(child.Value),Reason="Linked record changed — review relationship/context"};}}
  if(next.Remote.Count>20000||next.Pending.Count>20000)throw new IOException("Download review limit reached; cursor retained.");next.Cursor=cursor;next.LastContact=DateTimeOffset.UtcNow;Data=next;try{Save();}catch{Data=previous;throw;}
 }
}

internal sealed class WebsiteApi:IDisposable
{
 internal readonly Uri Endpoint;private readonly string token;private readonly HttpClient http;private readonly bool fixture;private DateTimeOffset nextRequest;
 internal WebsiteApi(string endpoint,string secret,HttpMessageHandler? handler=null)
 {
  if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.UserInfo.Length>0||uri.Query.Length>0||uri.Fragment.Length>0)throw new IOException("Use an HTTPS API address without a query, fragment or embedded credentials.");
  if(!System.Text.RegularExpressions.Regex.IsMatch(secret,@"^k8v_[a-fA-F0-9]{48}$"))throw new IOException("Enter a complete device token (k8v_ followed by 48 hexadecimal characters).");
  Endpoint=uri;token=secret;fixture=handler!=null;http=new(handler??new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(25)};
 }
 internal async Task<JsonObject> Request(string route,JsonObject? body=null,CancellationToken cancellation=default)
 {
  if(!fixture){var wait=nextRequest-DateTimeOffset.UtcNow;if(wait>TimeSpan.Zero)await Task.Delay(wait,cancellation);nextRequest=DateTimeOffset.UtcNow.AddMilliseconds(650);}
  using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation);deadline.CancelAfter(TimeSpan.FromSeconds(25));cancellation=deadline.Token;
  using var request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,new Uri(Endpoint.AbsoluteUri+"?r="+route));request.Headers.Add("X-KE8-Token",token);request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  if(body!=null){byte[] bytes=Encoding.UTF8.GetBytes(body.ToJsonString());if(bytes.Length>1900000)throw new IOException("Upload exceeds the safe request-size limit; select fewer records.");request.Content=new ByteArrayContent(bytes);request.Content.Headers.ContentType=new("application/json");}
  HttpResponseMessage response;try{response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancellation);}catch(OperationCanceledException){throw new IOException("Website request cancelled or timed out. Pull before retrying an uncertain upload.");}catch(HttpRequestException){throw new IOException("Cannot reach the website. No automatic upload retry was made.");}
  using(response){if(response.StatusCode==HttpStatusCode.TooManyRequests){string retry=response.Headers.RetryAfter?.ToString()??"the server's retry interval";throw new IOException("Website rate limit reached; retry after "+SyncJson.Safe(retry)+". No automatic retry.");}if((int)response.StatusCode is >=300 and <400)throw new IOException("The API redirected the request; redirects are blocked to protect your token. Check the API address.");
   using var stream=await response.Content.ReadAsStreamAsync(cancellation);using var buffer=new MemoryStream();byte[] block=new byte[8192];int n;while((n=await stream.ReadAsync(block,cancellation))>0){if(buffer.Length+n>8000000)throw new IOException("Website response exceeds 8 MB.");buffer.Write(block,0,n);}
   JsonObject result;try{result=JsonNode.Parse(buffer.ToArray(),documentOptions:new(){MaxDepth=48}) as JsonObject??throw new JsonException();}catch(JsonException){throw new IOException("The API returned invalid JSON; local data retained.");}
   string cleaned=SyncJson.Safe(result.ToJsonString());if(cleaned!=result.ToJsonString())result=JsonNode.Parse(cleaned)!.AsObject();
   if(SyncJson.Number(result["api"])!=1)throw new IOException("Unsupported API response/version.");if(!response.IsSuccessStatusCode)throw new IOException("Website HTTP "+(int)response.StatusCode+": "+SyncJson.Safe(SyncJson.Text(result["error"])).Replace(token,"[token hidden]"));return result;
  }
 }
 public void Dispose()=>http.Dispose();
}

internal sealed class WebsiteSync(WebsiteApi api,SyncStore store)
{
 internal string Account{get;private set;}="";internal string UserLabel{get;private set;}="";internal string Scope{get;private set;}="";
 internal async Task Test(CancellationToken token=default){var me=await api.Request("me",cancellation:token);var user=me["user"]??throw new IOException("Account response has no user identity");string id=user is JsonObject obj?SyncJson.Text(obj["id"]):SyncJson.Text(user);if(id.Length==0)throw new IOException("Account response needs a stable user id.");string account=api.Endpoint.AbsoluteUri+"|"+id;if(Account.Length>0&&account!=Account)throw new IOException("Account changed; reopen sync for this account.");store.Load(account);Account=account;UserLabel=user is JsonObject u?SyncJson.Text(u["callsign"]??u["name"]??u["id"]):id;Scope=SyncJson.Text(me["scope"]);if(Scope is not("read" or "write"))throw new IOException("Unknown API token scope");store.Data.LastContact=DateTimeOffset.UtcNow;store.Save();}
 internal async Task Pull(CancellationToken token=default){await Test(token);for(int page=0;page<40;page++){long before=store.Data.Cursor;var response=await api.Request($"changes&since={before}&limit=500&echo=1",cancellation:token);store.Stage(response);if(response["more"]?.GetValue<bool>()!=true){store.Data.InitialScanComplete=true;store.Save();RecoverJournal();return;}if(store.Data.Cursor<=before)throw new IOException("Changes pagination made no progress; saved pages retained.");}throw new IOException("40 pages saved; pull again to continue.");}
 private void RecoverJournal(){bool changed=false;foreach(var j in store.Data.Journal.Where(j=>j.State.StartsWith("uncertain"))){var matches=store.Data.Remote.Values.Where(r=>SyncJson.Text(r["ref"])==j.Ref||store.Data.Bindings.TryGetValue(j.Ref,out var existing)&&SyncJson.Number(r["id"])==existing.Id).ToArray();if(matches.Length!=1){j.State=store.Data.Pending.Values.Any(p=>p.Op=="delete"&&SyncJson.Text(p.Record["ref"])==j.Ref)?"gone":"not found after pull — fresh preview required";changed=true;continue;}var record=matches[0];string local=j.Ref.StartsWith("mrs:f:")&&Guid.TryParse(j.Ref[6..],out var id)?id.ToString():"";string scope=WebsiteImport.Context(record,store.Data,SyncJson.Hash(store.Data.Account)[..16]).Scope;store.Data.Bindings[j.Ref]=new(){Id=SyncJson.Number(record["id"]),Rev=SyncJson.Number(record["rev"]),LocalId=local,LocalScope=scope,Baseline=SyncJson.Copy(record)};j.State="recovered — review downloaded record";changed=true;}if(changed)store.Save();}
 internal JsonObject Prepare(SyncDraft draft)
 {
  SyncMapping.ValidateOutgoing(draft.Record);var record=SyncJson.Copy(draft.Record);var binding=store.Data.Bindings.GetValueOrDefault(draft.Ref);
  if(draft.Type=="lcn"&&binding?.Baseline["label"]!=null&&SyncJson.Text(record["label"])=="Channel "+SyncJson.Text(record["chan_no"]))record["label"]=binding.Baseline["label"]!.DeepClone();
  // ids replaces the whole object; retain unknown-to-this-client keys already known on the server.
  if(record["ids"] is JsonObject ids&&binding?.Baseline["ids"] is JsonObject previous){var merged=SyncJson.Copy(previous);foreach(var pair in ids)merged[pair.Key]=pair.Value?.DeepClone();record["ids"]=merged;}
  if(record["attrs"] is JsonObject attrs&&binding?.Baseline["attrs"] is JsonObject oldAttrs){var merged=SyncJson.Copy(oldAttrs);foreach(var pair in attrs)merged[pair.Key]=pair.Value?.DeepClone();record["attrs"]=merged;}
  foreach(string name in new[]{"parent","target"}){string reference=SyncJson.Text(record[name+"_ref"]);if(reference.Length>0&&store.Data.Bindings.TryGetValue(reference,out var parent)){record.Remove(name+"_ref");record[name]=parent.Id;}}
  var item=new JsonObject{["record"]=record};if(binding!=null){item["id"]=binding.Id;item["base_rev"]=binding.Rev;}else item["ref"]=draft.Ref;return item;
 }
 internal async Task<JsonArray> Push(IReadOnlyList<SyncDraft> drafts,bool dry,CancellationToken token=default,IReadOnlyDictionary<string,(long Id,long Rev)>? expectedBindings=null)
 {
  await Test(token);if(!store.Data.InitialScanComplete)throw new IOException("Pull the website records before previewing or uploading, so existing identifiers can be checked.");if(Scope!="write")throw new IOException("This token is read-only. Configure a write token for upload previews and uploads.");if(drafts.Count is 0 or >200)throw new IOException("Select 1–200 records including their dependencies.");
  if(expectedBindings!=null)foreach(var draft in drafts){var current=store.Data.Bindings.GetValueOrDefault(draft.Ref);if(!expectedBindings.TryGetValue(draft.Ref,out var expected)||(current?.Id??0)!=expected.Id||(current?.Rev??0)!=expected.Rev)throw new IOException("Upload bindings/revisions changed since preview. Pull, review and preview again; no request sent for this batch.");}
  foreach(var draft in drafts){if(draft.Warning.Length>0)throw new IOException(draft.Warning);if(store.Data.Bindings.GetValueOrDefault(draft.Ref)?.Gone==true||store.Data.Journal.Any(j=>j.Ref==draft.Ref&&(j.State.StartsWith("uncertain")||j.State is "gone" or "conflict")))throw new IOException("This record has an unresolved upload result; pull and review before retrying.");if(store.Data.Pending.Values.Any(p=>SyncJson.Number(p.Record["id"])==store.Data.Bindings.GetValueOrDefault(draft.Ref)?.Id))throw new IOException("Review downloaded changes/conflicts for selected records before uploading.");}
  var items=new JsonArray(drafts.Select(d=>(JsonNode)Prepare(d)).ToArray());if(Encoding.UTF8.GetByteCount(new JsonObject{["items"]=items.DeepClone()}.ToJsonString())>1900000)throw new IOException("Upload selection exceeds the safe request-size limit; select fewer records.");token.ThrowIfCancellationRequested();
  if(!dry){store.Data.Journal.RemoveAll(j=>j.State=="completed");if(store.Data.Journal.Count+drafts.Count>5000)throw new IOException("Upload journal needs review before more requests.");foreach(var (draft,index) in drafts.Select((d,i)=>(d,i)))store.Data.Journal.Add(new(){Ref=draft.Ref,State="uncertain — request prepared",Item=SyncJson.Copy((JsonObject)items[index]!)});store.Save();}
  JsonObject response=await api.Request("push"+(dry?"&dry=1":""),new(){["items"]=items},token);if(response["results"] is not JsonArray results||results.Count!=drafts.Count)throw new IOException("Unexpected push result count; pull before retrying.");
  for(int i=0;i<results.Count;i++){
   var r=results[i] as JsonObject??throw new IOException("Invalid push result");if(dry)continue;string status=SyncJson.Text(r["status"]);if(status is not("created" or "updated" or "unchanged" or "conflict" or "gone" or "error"))throw new IOException("Unknown push result; request remains uncertain until a pull/review.");var draft=drafts[i];long id=SyncJson.Number(r["id"]),rev=SyncJson.Number(r["rev"]);
   if(status is "created" or "updated" or "unchanged"){if(store.Data.Bindings.TryGetValue(draft.Ref,out var previousBinding)&&previousBinding.Id!=id)throw new IOException("Server result id does not match the requested record; pull and review.");if(id<=0||rev<1)throw new IOException("Push result lacks id/revision; pull before retrying.");store.Data.Bindings[draft.Ref]=new(){Id=id,Rev=rev,LocalId=draft.LocalId,LocalScope=draft.LocalScope,Baseline=SyncJson.Copy(draft.Record)};}
   else if(status is "conflict" or "gone"){var server=r["server"] is JsonObject s?SyncJson.Copy(s):new JsonObject{["id"]=store.Data.Bindings.GetValueOrDefault(draft.Ref)?.Id??id,["ref"]=draft.Ref};long key=SyncJson.Number(server["id"]);if(key>0)store.Data.Pending[key]=new(){Op=status=="gone"?"delete":"upsert",Record=server,Reason=status=="gone"?"Gone on website — no automatic recreation":"Revision conflict — review server and local values"};}
   var journal=store.Data.Journal.Last(j=>j.Ref==draft.Ref&&j.State.StartsWith("uncertain"));journal.State=status is "created" or "updated" or "unchanged"?"completed":status;journal.Detail=SyncJson.Safe(r.ToJsonString());store.Save(); // Save every success before processing the next item.
  }
  return results;
 }
}
