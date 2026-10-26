using System.IO;
using System.Text.Json;
namespace MRS;
internal sealed class P25BandCollector(string root)
{
    private static readonly object fileGate=new();
    private readonly List<PacketObservation> waiting=[];
    private readonly Dictionary<string,List<Dictionary<string,string>>> changes=new();
    private readonly string run=Guid.NewGuid().ToString("N");private int epoch;private readonly Dictionary<string,Dictionary<int,P25BandPlan>> bands=new();
    private DateTimeOffset saveAt;internal bool Enabled;internal string Error="";internal int PendingCount=>waiting.Count;internal int SavedChannels;private bool pendingDirty;
    private readonly Dictionary<string,P25Identity> identities=new();
    internal void Reset(){identities.Clear();bands.Clear();epoch++; /* Keep pending evidence; epoch keys prevent unrelated association. */}
    private static string Scope(PacketObservation p)=>$"{p.SourceKey}|{p.FrequencyHz}|{p.P25Identity?.Nac}";
    internal void Observe(PacketObservation p)
    {
        if(!Enabled||p.Replay||!p.IsValidated||p.Protocol!="P25 Phase I"||p.P25Identity is not {} id)return;
        if(p.P25BandPlan==null&&p.P25Channel==null&&p.P25Assignments==null&&p.P25SiteAnnouncement==null)return;
        string originalSource=p.SourceKey??"";p=p with{SourceKey=$"{epoch}:{originalSource}"};
        string key=Scope(p);
        if(identities.TryGetValue(key,out var prior)&&((id.Wacn.HasValue&&id.Wacn!=prior.Wacn)||(id.SystemId.HasValue&&id.SystemId!=prior.SystemId)||(id.RfssId.HasValue&&id.RfssId!=prior.RfssId)||(id.SiteId.HasValue&&id.SiteId!=prior.SiteId))){Reset();p=p with{SourceKey=$"{epoch}:{originalSource}"};key=Scope(p);}
        if(p.P25BandPlan is {Usable:true} plan){if(!bands.ContainsKey(key))bands[key]=new();bands[key][plan.Identifier]=plan;}
        bool complete=id is{Wacn:not null,SystemId:not null,RfssId:not null,SiteId:not null};
        if(complete){identities[key]=id;foreach(var old in waiting.Where(x=>Scope(x)==key).ToArray()){waiting.Remove(old);if(!Collect(old with{P25Identity=id}))waiting.Add(old);}pendingDirty=true;}
        else if(identities.TryGetValue(key,out var known))id=known;
        if(id is not{Wacn:not null,SystemId:not null,RfssId:not null,SiteId:not null}||!Collect(p with{P25Identity=id})){
            if(waiting.Count==512)waiting.RemoveAt(0);waiting.Add(p);pendingDirty=true;
        }
    }
    private bool Collect(PacketObservation p)
    {
        var id=p.P25Identity!;string site=$"p25/{id.Wacn:X5}-{id.SystemId:X3}/site-{id.RfssId:000}-{id.SiteId:000}";
        if(p.P25SiteAnnouncement is {} target){if(target.SystemId.HasValue&&target.SystemId!=id.SystemId)return false;site=$"p25/{id.Wacn:X5}-{id.SystemId:X3}/site-{target.RfssId:000}-{target.SiteId:000}";}
        string stamp=p.ObservedUtc.ToString("O");
        void Queue(string name,Dictionary<string,string> row){string file=Path.Combine(root,site,name);row["first_seen"]=row["last_seen"]=stamp;row["first_raw_hex"]=row["last_raw_hex"]=p.RawHex;row["source"]=$"{p.Source}; monitored {PlanCsv.MHz(p.FrequencyHz)} MHz; NAC {id.Nac:X3}; {p.Validation}";if(!changes.TryGetValue(file,out var rows))changes[file]=rows=[];string signature=Signature(row,name);var old=rows.FindIndex(r=>Signature(r,name)==signature);if(old>=0){row["first_seen"]=rows[old]["first_seen"];row["first_raw_hex"]=rows[old]["first_raw_hex"];rows[old]=row;}else if(rows.Count<10000)rows.Add(row);}
        bool resolved=true;
        void Channel(P25Channel c,string role){
            if(c.ChannelId==0xFFFF){resolved=false;return;}
            if(c.DownlinkHz==null&&bands.TryGetValue(Scope(p),out var tables)&&tables.TryGetValue(c.Identifier,out var table)){
                long down=table.BaseHz+(c.ChannelNumber/table.SlotsPerCarrier)*table.SpacingHz;long? up=table.OffsetHz.HasValue?down+table.OffsetHz:null;
                if(c.UplinkChannelId is int uplink){up=tables.TryGetValue(uplink>>12,out var ub)?ub.BaseHz+((uplink&4095)/ub.SlotsPerCarrier)*ub.SpacingHz:null;}
                c=c with{DownlinkHz=down,UplinkHz=up,SlotsPerCarrier=table.SlotsPerCarrier,Slot=table.SlotsPerCarrier>1?c.ChannelNumber%table.SlotsPerCarrier:null};
            }
            if(c.DownlinkHz is not >0){resolved=false;return;}Queue("channels.csv",new(){["lcn"]=$"{c.Identifier:00}-{c.ChannelNumber:0000}",["downlink_mhz"]=PlanCsv.MHz(c.DownlinkHz),["uplink_mhz"]=PlanCsv.MHz(c.UplinkHz),["slot"]=c.Slot?.ToString()??"",["uplink_lcn"]=c.UplinkChannelId is int uid?$"{uid>>12:00}-{uid&4095:0000}":"",["role"]=role,["label"]=""});}
        if(p.P25BandPlan is {Usable:true} b)Queue("spacing.csv",new(){["identifier"]=b.Identifier.ToString(),["base_mhz"]=PlanCsv.MHz(b.BaseHz),["spacing_khz"]=(b.SpacingHz/1000m).ToString(System.Globalization.CultureInfo.InvariantCulture),["offset_mhz"]=PlanCsv.MHz(b.OffsetHz),["slots_per_carrier"]=b.SlotsPerCarrier.ToString(),["bandwidth_hz"]=b.BandwidthHz?.ToString()??"",["channel_type"]=b.ChannelType?.ToString()??"",["opcode"]=b.Opcode.ToString(),["first_channel"]="0",["last_channel"]="4095"});
        if(p.P25SiteAnnouncement is {} a){foreach(var c in a.Channels)Channel(c.Channel,p.Kind);}
        else {if(p.P25Channel is {} c)Channel(c,p.Kind);if(p.P25Assignments is {} assignments)foreach(var assignment in assignments)Channel(assignment.Channel,"Voice grant");}
        return resolved;
    }
    private static string Signature(Dictionary<string,string> r,string name)=>name=="channels.csv"?$"{PlanCsv.Get(r,"lcn")}|{PlanCsv.Get(r,"slot")}|{PlanCsv.Hz(r,"downlink")}|{PlanCsv.Hz(r,"uplink",true)}":$"{PlanCsv.Get(r,"identifier")}|{PlanCsv.Hz(r,"base")}|{PlanCsv.Hz(r,"spacing")}|{PlanCsv.Hz(r,"offset",true)}|{PlanCsv.Get(r,"slots_per_carrier")}";
    internal bool Flush(bool force=false)
    {
        if(!force&&DateTimeOffset.UtcNow<saveAt)return false;saveAt=DateTimeOffset.UtcNow.AddSeconds(5);bool changed=false;Error="";
        lock(fileGate){foreach(var entry in changes.ToArray()){try{
            string name=Path.GetFileName(entry.Key);var rows=File.Exists(entry.Key)?PlanCsv.Read(entry.Key):[];
            // Build every signature before writing: malformed edited files are never overwritten.
            var signatures=rows.Select(r=>Signature(r,name)).ToList();
            foreach(var incoming in entry.Value){string signature=Signature(incoming,name);int i=signatures.IndexOf(signature);
                if(i<0&&name=="channels.csv")i=rows.FindIndex(r=>PlanCsv.Get(r,"lcn")==PlanCsv.Get(incoming,"lcn")&&PlanCsv.Get(r,"slot")==PlanCsv.Get(incoming,"slot")&&PlanCsv.Hz(r,"downlink")==PlanCsv.Hz(incoming,"downlink")&&PlanCsv.Hz(r,"uplink",true)==null);
                if(i>=0){var old=rows[i];foreach(var field in incoming)if(field.Key is not("label" or "first_seen" or "first_raw_hex")){
                    if(field.Key=="last_seen"&&DateTimeOffset.TryParse(PlanCsv.Get(old,"last_seen"),out var previous)&&DateTimeOffset.Parse(field.Value)<previous)continue;
                    old[field.Key]=field.Value;
                }}
                else{if(rows.Count==10000)throw new InvalidDataException("Site CSV reached 10000 rows");rows.Add(new(incoming));signatures.Add(signature);}}
            string identity=name=="channels.csv"?"lcn":"identifier";foreach(var group in rows.GroupBy(r=>PlanCsv.Get(r,identity)))if(group.Select(r=>name=="channels.csv"?$"{PlanCsv.Hz(r,"downlink")}/{PlanCsv.Hz(r,"uplink",true)}":Signature(r,name)).Distinct().Count()>1)foreach(var r in group)r["conflict"]="Multiple observed definitions; review evidence";
            PlanCsv.Write(entry.Key,rows,name=="channels.csv"?["lcn","downlink_mhz","uplink_mhz","slot","role","label","first_seen","last_seen","source","conflict"]:["identifier","base_mhz","spacing_khz","offset_mhz","slots_per_carrier","first_channel","last_channel","first_seen","last_seen","source","conflict"]);
            if(name=="channels.csv")SavedChannels+=entry.Value.Count;changes.Remove(entry.Key);changed=true;
        }catch(Exception ex){Error=$"{entry.Key}: {ex.Message}";}}
        if(pendingDirty){try{string path=Path.Combine(root,"p25","unresolved",run+".json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);string tmp=path+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(waiting,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,path,true);pendingDirty=false;}catch(Exception ex){Error="Unresolved evidence: "+ex.Message;}}}
        return changed;
    }
}
