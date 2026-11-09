using System.Text.RegularExpressions;
namespace MRS;
internal sealed record LcnCandidate(string Scope,int Lcn,long Hz,int Slot,int Talkgroup,int ColorCode,int Matches,DateTimeOffset Seen,string Status)
{public string MHz=>PlanCsv.MHz(Hz);}
internal sealed class LcnResearch
{
 private sealed record Grant(int Lcn,int Slot,uint Group,uint? Radio,DateTimeOffset Seen);
 private readonly object gate=new();private readonly List<Grant> grants=[];private readonly Dictionary<(int,long),LcnCandidate> candidates=[];private CapStatusDecoder cap=new();private HashSet<long> frequencies=[];
 internal string Scope{get;private set;}="";internal long ControlHz{get;private set;}internal bool Enabled{get;private set;}
 internal void Start(string scope,long control,IEnumerable<long> channels){lock(gate){Scope=scope;ControlHz=control;frequencies=channels.ToHashSet();grants.Clear();candidates.Clear();cap=new();Enabled=true;}}
 internal void Stop(){lock(gate)Enabled=false;}
 internal LcnCandidate[] Snapshot(){lock(gate){return candidates.Values.OrderBy(c=>c.Lcn).ThenBy(c=>c.Hz).Select(c=>c with{Status=candidates.Values.Any(o=>o.Lcn==c.Lcn&&o.Hz!=c.Hz||o.Hz==c.Hz&&o.Lcn!=c.Lcn)?"Conflicting associations — review":"Correlated candidate — unconfirmed"}).ToArray();}}
 internal void Observe(DmrReception observation)
 {
  lock(gate){if(!Enabled||observation.Context.Replay||observation.Context.FrequencyHz is not long hz||!observation.Fields.LinkValid||observation.Fields.SlotCorrections>1)return;var f=observation.Fields;var now=observation.Seen;
   int? ScopeNumber(string label){var m=Regex.Match(Scope,label+@" (\d+)",RegexOptions.IgnoreCase);return m.Success?int.Parse(m.Groups[1].Value):null;}
   if(f.Network is int net&&ScopeNumber("Network") is int expectedNet&&net!=expectedNet||f.Site is int site&&ScopeNumber("Site") is int expectedSite&&site!=expectedSite)return;
   grants.RemoveAll(g=>(now-g.Seen).TotalSeconds>2);void Add(int lcn,int slot,uint group){if(lcn<1||lcn>4094||slot is not(1 or 2)||group==0)return;if(grants.Count>=128)grants.RemoveAt(0);grants.Add(new(lcn,slot,group,f.Source,now));}
   if(Math.Abs(hz-ControlHz)<=1000){
    if(f.Group&&f.Destination is uint group&&f.GrantChannel is int lcn&&f.GrantSlot is int slot)Add(lcn,slot,group);
    if(cap.Feed(f,now) is {} status)foreach(var call in status.Groups)Add((call.Lsn+1)/2,(call.Lsn-1)%2+1,(uint)call.Talkgroup);
    return;
   }
   if(f.DataType!=1||!f.Group||f.Destination is not uint tg||f.Slot is not int voiceSlot||!frequencies.Any(freq=>Math.Abs(freq-hz)<=1000))return;
   long frequency=frequencies.OrderBy(freq=>Math.Abs(freq-hz)).First();
   foreach(var g in grants.Where(g=>g.Group==tg&&g.Slot==voiceSlot&&(now-g.Seen).TotalSeconds is >=0 and <=1.5&&(!g.Radio.HasValue||!f.Source.HasValue||g.Radio==f.Source))){
    var key=(g.Lcn,frequency);candidates.TryGetValue(key,out var old);if(old!=null&&(now-old.Seen).TotalSeconds<5)continue;if(candidates.Count>=512&&!candidates.ContainsKey(key))continue;
    candidates[key]=new(Scope,g.Lcn,frequency,voiceSlot,(int)tg,f.ColorCode,(old?.Matches??0)+1,now,"Correlated candidate — unconfirmed");
   }
  }
 }
}
