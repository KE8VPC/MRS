using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
namespace MRS;

internal sealed record P25Identity(int Nac,int? Wacn,int? SystemId,int? RfssId,int? SiteId);
internal sealed record P25Discovery(string Key,string Category,string Label,long? FrequencyHz,DateTimeOffset FirstSeen,DateTimeOffset LastSeen,long Receptions,PacketObservation Observation,string Name="",string Notes="");
internal sealed record P25CatalogFile(int Version,P25Discovery[] Entries);
internal sealed class P25DiscoveryRow : INotifyPropertyChanged
{
    internal P25Discovery Value {get;private set;}
    public string System=>Value.Observation.P25Identity is { } id?$"{(id.Wacn is int w?$"{w:X5}":"?")} / {(id.SystemId is int s?$"{s:X3}":"?")} · NAC {id.Nac:X3}":"Unidentified";
    public string Site=>Value.Observation.P25Identity is {RfssId:int r,SiteId:int s}?$"{r} / {s}":"Provisional";
    public string Category=>Value.Category;
    public string Label=>string.IsNullOrWhiteSpace(Value.Name)?Value.Label:Value.Name;
    public string Frequency=>Value.FrequencyHz is long hz?(hz/1000000m).ToString("F6",CultureInfo.InvariantCulture):"Unresolved";
    public string LastSeen=>Value.LastSeen.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    public long Count=>Value.Receptions;
    public event PropertyChangedEventHandler? PropertyChanged;
    internal P25DiscoveryRow(P25Discovery value)=>Value=value;
    internal void Update(P25Discovery value){Value=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(null));}
}

// Called on the UI thread after decoding, independent of Data visibility filters.
internal sealed class P25Catalog
{
    internal const int Limit=5000;
    internal readonly ObservableCollection<P25DiscoveryRow> Rows=new();
    private readonly Dictionary<string,P25DiscoveryRow> index=new();
    private readonly string run=Guid.NewGuid().ToString("N");
    private readonly string? path;
    private bool dirty,readOnly;
    private DateTimeOffset nextSave;
    internal string? Error {get;private set;}
    internal P25Catalog(string? path=null)
    {
        this.path=path;if(path==null||!File.Exists(path))return;
        try
        {
            if(new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException("Discovery file exceeds 64 MB.");
            var file=JsonSerializer.Deserialize<P25CatalogFile>(File.ReadAllText(path));
            if(file is not {Version:1,Entries:not null}||file.Entries.Length>Limit)throw new InvalidDataException("Unsupported discovery file.");
            foreach(var item in file.Entries)
            {
                if(item==null||string.IsNullOrEmpty(item.Key)||item.Observation?.P25Identity==null||item.Observation.Replay||item.Receptions<1||item.LastSeen<item.FirstSeen||index.ContainsKey(item.Key))throw new InvalidDataException("Invalid discovery entry.");
                var row=new P25DiscoveryRow(item);Rows.Add(row);index.Add(item.Key,row);
            }
        }
        catch(Exception ex){Rows.Clear();index.Clear();readOnly=true;Error="Discovery file left unchanged: "+ex.Message;}
    }
    internal void Observe(PacketObservation observation)
    {
        if(observation.Protocol!="P25 Phase I"||!observation.IsValidated||observation.P25Identity is not { } id||observation.Kind=="NID header"||path!=null&&observation.Replay)return;
        // Missing identifiers stay explicitly provisional and cannot collide with
        // an unrelated future reception epoch on the same RF/NAC.
        bool identified=id.Wacn.HasValue&&id.SystemId.HasValue&&id.RfssId.HasValue&&id.SiteId.HasValue;
        string scope=JsonSerializer.Serialize(new object?[]{observation.Replay,observation.FrequencyHz,id,
            observation.Replay||!identified?run+":"+observation.SourceKey:null});
        if(observation.P25BandPlan is { } band)Add("Band plan",$"Identifier {band.Identifier:X1}",$"band:{band.Identifier}",band.BaseHz);
        if(observation.Kind is "Network status" or "Site status")Add("Control channel",observation.Kind,observation.Kind,observation.P25Channel?.DownlinkHz);
        if(observation.P25Assignments is { } assignments)
            foreach(var assignment in assignments)Add("Voice channel",$"TG {assignment.TalkgroupId} · channel {assignment.Channel.ChannelId:X4}"+(assignment.Channel.Slot is int slot?$" · slot {slot}":""),$"voice:{assignment.TalkgroupId}:{assignment.Channel.ChannelId}",assignment.Channel.DownlinkHz);
        if(observation.P25SiteAnnouncement is { } site)
            foreach(var channel in site.Channels)Add(observation.Kind,$"{(site.SystemId is int system?$"System {system:X3} · ":"")}RFSS {site.RfssId} · Site {site.SiteId} · channel {channel.Channel.ChannelId:X4}",
                $"{observation.Kind}:{site.SystemId}:{site.RfssId}:{site.SiteId}:{channel.Channel.ChannelId}",channel.Channel.DownlinkHz);
        void Add(string category,string label,string entity,long? frequency)
        {
            string key=scope+":"+entity;var time=observation.ObservedUtc;
            if(index.TryGetValue(key,out var old))
            {
                var value=old.Value;
                old.Update(time>=value.LastSeen?new(key,category,label,frequency,value.FirstSeen<time?value.FirstSeen:time,time,value.Receptions+1,observation,value.Name,value.Notes):value with{FirstSeen=time<value.FirstSeen?time:value.FirstSeen,Receptions=value.Receptions+1});
            }
            else
            {
                if(Rows.Count>=Limit){Error=$"Discovery limit ({Limit}) reached; existing entries still update. Export discoveries to retain a separate snapshot.";return;}
                var row=new P25DiscoveryRow(new(key,category,label,frequency,time,time,1,observation));Rows.Add(row);index.Add(key,row);
            }
            if(!dirty)nextSave=DateTimeOffset.UtcNow.AddSeconds(10);dirty=true;
        }
    }
    internal void Annotate(string key,string name,string notes)
    {
        if(name.Length>200||notes.Length>8000)throw new ArgumentException("Use at most 200 characters for the name and 8,000 for notes.");
        if(!index.TryGetValue(key,out var row))throw new InvalidOperationException("The discovery is no longer available.");
        row.Update(row.Value with{Name=name.Trim(),Notes=notes});
        if(!dirty)nextSave=DateTimeOffset.UtcNow.AddSeconds(10);dirty=true;
    }
    internal string Export()=>JsonSerializer.Serialize(new P25CatalogFile(1,Rows.Select(r=>r.Value).ToArray()),new JsonSerializerOptions{WriteIndented=true});
    internal void Save(bool force=false)
    {
        if(path==null||readOnly||!dirty||!force&&DateTimeOffset.UtcNow<nextSave)return;
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporary,Export());File.Move(temporary,path,true);dirty=false;
            if(Error?.StartsWith("Could not save") == true)Error=null;
        }
        catch(Exception ex){Error="Could not save discoveries: "+ex.Message;nextSave=DateTimeOffset.UtcNow.AddSeconds(10);}
        finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch{/* Reported save failure remains visible. */}}
    }
}

