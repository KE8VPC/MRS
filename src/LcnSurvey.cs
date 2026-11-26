using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;

internal sealed record SurveyGrant(int Lcn,int Slot,uint Group,uint? Radio,int ColorCode,string Call,DateTimeOffset Seen);
internal sealed record SurveyEvidence(int Lcn,long Hz,int Slot,uint Group,uint? Radio,int ColorCode,string Call,DateTimeOffset Seen,string Result);
internal sealed record SurveyRow(int Lcn,long Hz,int Calls,int Required,string Status,string Evidence)
{public string MHz=>Hz==0?"Unknown":PlanCsv.MHz(Hz);public string Attempts=>$"{Calls}/{Required}";}
internal sealed class SurveyDocument
{
 public int Version{get;set;}=1;public string Scope{get;set;}="";public long ControlHz{get;set;}public long[] Frequencies{get;set;}=[];
 public long[] Excluded{get;set;}=[];
 public int Required{get;set;}=3;public int DwellSeconds{get;set;}=4;public int FollowSeconds{get;set;}=20;
 public int? ControlColorCode{get;set;}
 public bool WaitForEnd{get;set;}=true;
 public bool SaveToLibrary{get;set;}=true;
 public bool Follow{get;set;}public bool TryTentative{get;set;}public string Protocol{get;set;}="";
 public Dictionary<string,string> Calls{get;set;}=[];public Dictionary<int,int> NextCandidate{get;set;}=[];
 public List<SurveyEvidence> Evidence{get;set;}=[];public List<int> Observed{get;set;}=[];public Dictionary<long,string> Roles{get;set;}=[];
 public Dictionary<string,long> Rejected{get;set;}=[];
}

// UI-thread state machine. A tune request is never considered applied until the receiver acknowledges it.
internal sealed class LcnSurvey
{
 internal SurveyDocument Data{get;private set;}=new();internal bool Running{get;private set;}internal long DesiredHz{get;private set;}
 internal long AppliedHz{get;private set;}internal string Phase{get;private set;}="Stopped";internal string Message{get;private set;}="Select a saved DMR site folder";
 internal int? PendingSlot=>pending?.Slot;internal bool Dirty{get;private set;}internal int Generation{get;private set;}internal bool IsProbe=>Phase is "Probe" or "Following";
 private SurveyGrant? pending;private CapStatusDecoder cap=new();private DateTimeOffset deadline,lastControl;private int recoveryIndex;private bool confirmedThisProbe;
 internal readonly Queue<string> Events=[];
 internal static string Role(DmrObservation f)
 {
  if(!f.LinkValid||f.SlotCorrections>1)return "";
  if(f.SystemType=="Connect Plus"&&f.Detail=="Control-channel identity")return "Control — decoded";
  if(f.SystemType=="Connect Plus"&&f.Detail=="Traffic-channel identity")return "Voice — observed";
  if(f.SystemType=="DMR Tier III"&&f.DataType==3&&(f.GrantChannel.HasValue||f.Detail.StartsWith("ALOHA")))return "Control — decoded";
  if(f.SystemType=="Capacity Plus"&&f.DataType==3&&f.Detail.StartsWith("Capacity Plus channel status"))return "Rest — last observed";
  return "";
 }
 private void Log(DateTimeOffset now,string text){if(Events.Count>=200)Events.Dequeue();Events.Enqueue(now.ToString("HH:mm:ss")+" · "+text);Message=text;}
 internal void Configure(string scope,long control,IEnumerable<long> frequencies,int required=3)
 {
  var list=frequencies.Distinct().Order().ToArray();if(string.IsNullOrWhiteSpace(scope)||scope.Length>1000||control<=0||control>6000000000||list.Length is 0 or >512||!list.Contains(control)||list.Any(h=>h<=0||h>6000000000))throw new IOException("Choose one site, with its control/rest frequency included in the saved candidate list (maximum 512).");
  Data=new(){Scope=scope,ControlHz=control,Frequencies=list,Required=Math.Clamp(required,1,20)};Dirty=true;Stop("Site selected");
 }
 internal void Start(DateTimeOffset now){Dirty=true;if(Data.Frequencies.Length==0)throw new IOException("Select a site first.");Running=true;pending=null;cap=new();lastControl=now;Request(Data.ControlHz,"Returning",now);Log(now,"Returning to selected control/rest; waiting for validated signaling");}
 internal void Stop(string reason="Stopped; progress retained"){Running=false;Generation++;AppliedHz=0;DesiredHz=0;pending=null;Phase="Stopped";Message=reason;}
 private void Request(long hz,string phase,DateTimeOffset now){DesiredHz=hz;AppliedHz=0;Phase=phase;Generation++;deadline=now.AddSeconds(6);cap=new();}
 internal void Applied(long hz,DateTimeOffset now){if(!Running||hz!=DesiredHz)return;AppliedHz=hz;if(Phase=="Tuning probe"){Phase="Probe";deadline=now.AddSeconds(Data.DwellSeconds);confirmedThisProbe=false;}else {Phase="Reacquiring";deadline=now.AddSeconds(5);}Log(now,Phase+" · "+PlanCsv.MHz(hz)+" MHz");}
 internal void Tick(DateTimeOffset now)
 {
  if(!Running)return;if(AppliedHz==0){if(now>=deadline)Stop("Tune acknowledgement timed out; progress retained");return;}
  if(IsProbe&&now>=deadline){Log(now,confirmedThisProbe?"Probe finished; returning to control":"Inconclusive — no matching full LC; returning to control");pending=null;Request(Data.ControlHz,"Returning",now);}
  else if(Phase=="Reacquiring"&&now>=deadline||Phase=="Control"&&(now-lastControl).TotalSeconds>12){var list=new[]{Data.ControlHz}.Concat(Data.Frequencies).Distinct().ToArray();long next=list[(++recoveryIndex)%list.Length];Request(next,"Searching control/rest",now);Log(now,"Searching control/rest · "+PlanCsv.MHz(next));}
 }
 private bool Compatible(DmrObservation f)
 {
  int? Id(string name){var m=Regex.Match(Data.Scope,@"\b"+name+@" (\d+)");return m.Success?int.Parse(m.Groups[1].Value):null;}
  if(f.Network.HasValue&&Id("Network") is int n&&n!=f.Network||f.Site.HasValue&&Id("Site") is int s&&s!=f.Site)return false;
  string type=Data.Protocol;if(type.Length>0&&f.SystemType is "Capacity Plus" or "Connect Plus" or "DMR Tier III"&&type!=f.SystemType)return false;
  return true;
 }
 private string Call(int lcn,int slot,uint tg,uint? radio)
 {
  string key=$"{lcn}:{slot}";string signature=$"{tg}:{radio}";if(Data.Calls.TryGetValue(key,out string? old)&&old.Split('|')[0].Split(':') is var previous&&previous[0]==tg.ToString()&&(previous.Length<2||previous[1].Length==0||!radio.HasValue||previous[1]==radio.ToString()))return old;
  string token=signature+"|"+Guid.NewGuid().ToString("N");Data.Calls[key]=token;Dirty=true;return token;
 }
 internal void Observe(DmrReception r)
 {
  if(!Running||AppliedHz==0||r.Context.Replay||r.Context.FrequencyHz!=AppliedHz)return;var f=r.Fields;var now=r.Seen;if(!f.LinkValid||f.SlotCorrections>1||f.ColorCode is <0 or >15||!Compatible(f))return;
  if(IsProbe){ObserveVoice(f,now);return;}
  if(Data.ControlColorCode is int expectedCc&&f.ColorCode!=expectedCc)return;
  string role=Role(f);bool manualControl=AppliedHz==Data.ControlHz&&Phase!="Searching control/rest";
  bool grant=f.DataType==3&&f.GrantChannel.HasValue&&f.GrantSlot.HasValue;
  if(role is "Control — decoded" or "Rest — last observed"||manualControl&&grant){
   Data.ControlColorCode??=f.ColorCode;Data.ControlHz=AppliedHz;if(role.Length>0)Data.Roles[AppliedHz]=role;else if(!Data.Roles.ContainsKey(AppliedHz))Data.Roles[AppliedHz]="Control — user assigned; grants observed";
   if(f.SystemType is "Capacity Plus" or "Connect Plus" or "DMR Tier III")Data.Protocol=f.SystemType;
   Dirty=true;lastControl=now;Phase="Control";
  }
  if(Phase!="Control")return;
  // Maintain call identity even for passed channels and complete Cap+ busy/idle transitions.
  var assignments=new List<(int Lcn,int Slot,uint Tg,uint? Radio)>();
  if(grant&&f.Group&&f.Destination is uint tg)assignments.Add((f.GrantChannel!.Value,f.GrantSlot!.Value,tg,f.Source));
  if(cap.Feed(f,now) is {} status){var active=status.Groups.Select(g=>$"{(g.Lsn+1)/2}:{(g.Lsn-1)%2+1}").ToHashSet();foreach(var key in Data.Calls.Keys.Where(k=>!active.Contains(k)).ToArray()){Data.Calls.Remove(key);Dirty=true;}foreach(var g in status.Groups)assignments.Add(((g.Lsn+1)/2,(g.Lsn-1)%2+1,(uint)g.Talkgroup,null));}
  SurveyGrant? choice=null;
  foreach(var a in assignments){if(a.Lcn is <1 or >4094||a.Slot is not(1 or 2)||a.Tg==0)continue;if(!Data.Observed.Contains(a.Lcn)){Data.Observed.Add(a.Lcn);Dirty=true;}uint? radio=a.Radio is >0?a.Radio:null;string token=Call(a.Lcn,a.Slot,a.Tg,radio);var next=new SurveyGrant(a.Lcn,a.Slot,a.Tg,radio,f.ColorCode,token,now);
   if(choice==null&&(!Passed(a.Lcn)||Data.Follow))choice=next;
  }
  if(choice!=null)BeginProbe(choice,now);
 }
 private void BeginProbe(SurveyGrant grant,DateTimeOffset now)
 {
  var available=Data.Frequencies.Where(h=>h!=Data.ControlHz&&!Data.Rejected.ContainsKey($"{grant.Lcn}:{h}")).ToArray();if(available.Length==0)return;var rows=Rows().Where(c=>c.Lcn==grant.Lcn&&c.Hz>0&&Data.Frequencies.Contains(c.Hz)&&!c.Status.StartsWith("Rejected")).OrderByDescending(c=>c.Calls).ToArray();
  bool conflict=rows.Any(c=>c.Status.StartsWith("Conflict"));if(conflict){Log(now,$"LCN {grant.Lcn}: conflicting evidence; review required");return;}
  long hz=rows.FirstOrDefault(c=>c.Status=="Passed")?.Hz??0;
  if(hz==0){int index=Data.NextCandidate.GetValueOrDefault(grant.Lcn);hz=available[index%available.Length];Data.NextCandidate[grant.Lcn]=(index+1)%available.Length;Dirty=true;
   // Explore all candidates once per call; a known tentative match is revisited on later calls.
   var tentative=rows.FirstOrDefault(c=>c.Calls>0&&!Data.Evidence.Any(e=>e.Call==grant.Call&&e.Lcn==grant.Lcn&&e.Hz==c.Hz&&e.Result=="Match"));if(tentative!=null)hz=tentative.Hz;
  }
  if(Data.Evidence.Any(e=>e.Call==grant.Call&&e.Lcn==grant.Lcn&&e.Hz==hz&&e.Result=="Match"))return;
  pending=grant;Request(hz,"Tuning probe",now);Log(now,$"Testing LCN {grant.Lcn} / slot {grant.Slot} / TG {grant.Group} at {PlanCsv.MHz(hz)}");
 }
 private void ObserveVoice(DmrObservation f,DateTimeOffset now)
 {
  if(pending is not {} g||now<g.Seen||(now-g.Seen).TotalSeconds>Math.Max(10,Data.FollowSeconds+8)||f.DataType is not(1 or 2)||!f.Group||f.Slot!=g.Slot)return;
  bool same=f.Destination==g.Group&&(!g.Radio.HasValue||f.Source is null or 0||g.Radio==f.Source)&&f.ColorCode==g.ColorCode;
  if(f.DataType==2){if(same&&confirmedThisProbe){Data.Calls.Remove($"{g.Lcn}:{g.Slot}");Dirty=true;Log(now,"Matching call terminator; next call may supply independent evidence");pending=null;Request(Data.ControlHz,"Returning",now);}return;}
  if(!same){AddEvidence(g,f,now,"Contradiction");Log(now,"Different decoded call — retained as contradictory evidence");if(Phase=="Following"){pending=null;Request(Data.ControlHz,"Returning",now);}return;}
  bool first=!confirmedThisProbe;AddEvidence(g,f,now,"Match");confirmedThisProbe=true;Data.Roles[AppliedHz]="Voice — observed";Dirty=true;
  Log(now,$"LCN {g.Lcn}: matching full LC; "+Rows().First(c=>c.Lcn==g.Lcn&&c.Hz==AppliedHz).Status);
  // Keep a short terminator-observation window. Following is bounded even if the terminator is missed.
  bool follow=Data.Follow&&(Passed(g.Lcn)||Data.TryTentative);if(first){deadline=now.AddSeconds(follow||Data.WaitForEnd?Data.FollowSeconds:2);if(follow)Phase="Following";}
 }
 private void AddEvidence(SurveyGrant g,DmrObservation f,DateTimeOffset now,string result)
 {
  if(Data.Evidence.Any(e=>e.Call==g.Call&&e.Lcn==g.Lcn&&e.Hz==AppliedHz&&e.Result==result))return;
  if(Data.Evidence.Count>=20000){Stop("Research evidence limit reached; export and archive before continuing");return;}
  Data.Evidence.Add(new(g.Lcn,AppliedHz,g.Slot,g.Group,f.Source,f.ColorCode,g.Call,now,result));Dirty=true;
 }
 internal bool Passed(int lcn)=>Rows().Any(c=>c.Lcn==lcn&&c.Status=="Passed");
 internal SurveyRow[] Rows()
 {
  var rows=new List<SurveyRow>();foreach(int lcn in Data.Observed.Order()){
   var groups=Data.Evidence.Where(e=>e.Lcn==lcn).GroupBy(e=>e.Hz).ToArray();if(groups.Length==0){rows.Add(new(lcn,0,0,Data.Required,"Searching","Assignment observed; frequency unresolved"));continue;}
   foreach(var group in groups){long hz=group.Key;bool rejected=Data.Rejected.ContainsKey($"{lcn}:{hz}");int calls=group.Where(e=>e.Result=="Match").Select(e=>e.Call).Distinct().Count();
    bool conflict=calls>0&&(Data.Evidence.Any(e=>e.Result=="Match"&&(e.Lcn==lcn&&e.Hz!=hz||e.Hz==hz&&e.Lcn!=lcn)&&!Data.Rejected.ContainsKey($"{e.Lcn}:{e.Hz}"))||group.Any(e=>e.Result=="Contradiction"));
    string state=rejected?"Rejected — user review":!Data.Frequencies.Contains(hz)?"Outside candidate list":conflict?"Conflict — review":calls>=Data.Required?"Passed":calls>0?"Tentative":"Inconclusive";
    rows.Add(new(lcn,hz,calls,Data.Required,state,$"{group.Count(e=>e.Result=="Contradiction")} contradictions; latest "+group.Max(e=>e.Seen).ToString("u")));
   }
  }return rows.ToArray();
 }
 internal string Summary{get{var ids=Data.Observed.Distinct().ToArray();int mapped=ids.Count(Passed);return ids.Length==0?"Inventory unknown — waiting for assignments":mapped==ids.Length?$"All {mapped} observed carriers mapped — total inventory unknown":$"LCN map incomplete — {mapped}/{ids.Length} observed carriers mapped";}}
 internal void Reject(int lcn,long hz){if(Running)throw new IOException("Stop discovery before reviewing a candidate.");Data.Rejected[$"{lcn}:{hz}"]=hz;Dirty=true;}
 internal void MarkDirty()=>Dirty=true;
 internal void Save(string file){if(!Dirty)return;Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllText(file+".tmp",JsonSerializer.Serialize(Data,new JsonSerializerOptions{WriteIndented=true}));File.Move(file+".tmp",file,true);Dirty=false;}
 internal void Load(string file){if(new FileInfo(file).Length>16000000)throw new IOException("Research file exceeds limit");var d=JsonSerializer.Deserialize<SurveyDocument>(File.ReadAllText(file))??throw new IOException("Empty research file");if(d.Version!=1||d.Excluded==null||d.Excluded.Length>512||d.Frequencies==null||d.Evidence==null||d.Calls==null||d.NextCandidate==null||d.Observed==null||d.Roles==null||d.Rejected==null||d.Roles.Count>512||d.Rejected.Count>20000||d.Required is <1 or >20||d.DwellSeconds is <2 or >15||d.FollowSeconds is <5 or >60||d.Evidence.Count>20000||d.Observed.Count>4094||d.Calls.Count>8188||d.Frequencies.Length>512)throw new IOException("Invalid research data; original left unchanged");if(d.Protocol==null||d.Protocol.Length>100||d.Frequencies.Distinct().Count()!=d.Frequencies.Length||d.Observed.Distinct().Count()!=d.Observed.Count||d.Excluded.Any(h=>h<=0||h>6000000000)||d.Calls.Any(p=>p.Key.Length>40||p.Value==null||p.Value.Length>200)||d.Roles.Any(p=>p.Key<=0||p.Key>6000000000||p.Value==null||p.Value.Length>500))throw new IOException("Invalid research fields; original left unchanged");new LcnSurvey().Configure(d.Scope,d.ControlHz,d.Frequencies,d.Required);if(d.Evidence.Any(e=>e==null||e.Lcn is <1 or >4094||(e.Hz<=0||e.Hz>6000000000)||e.Slot is not(1 or 2)||e.Result is not("Match" or "Contradiction")||string.IsNullOrEmpty(e.Call)||e.Call.Length>200||e.ColorCode is <0 or >15||e.Group is 0 or >16777215)||d.Observed.Any(l=>l is <1 or >4094)||d.NextCandidate.Values.Any(i=>i<0))throw new IOException("Invalid evidence; original left unchanged");if(d.ControlColorCode is <0 or >15)throw new IOException("Invalid control color code");Data=d;Dirty=false;Stop("Saved research loaded — Resume when ready");}
}
