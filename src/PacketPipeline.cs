namespace MRS;

internal sealed record PacketContext(string Key,string Source,bool Replay,long? FrequencyHz,double? PositionSeconds=null);
internal sealed record PacketObservation(DateTimeOffset ObservedUtc,string Source,bool Replay,long? FrequencyHz,
    double? PositionSeconds,string Validation,string Callsign,string Destination,string Path,string Kind,string Information,
    AprsData? Aprs,string RawHex,string Protocol="AX.25",bool? Validated=null,long ReceptionCount=1,DateTimeOffset? FirstSeenUtc=null,long ContentChanges=0,string? SourceKey=null,P25BandPlan? P25BandPlan=null,P25Channel? P25Channel=null,P25GrantAssignment[]? P25Assignments=null,P25SiteAnnouncement? P25SiteAnnouncement=null,P25Identity? P25Identity=null)
{
    public bool IsValidated=>Validated ?? Validation.StartsWith("FCS valid",StringComparison.Ordinal);
    public string Time=>ObservedUtc.ToString("HH:mm:ss.fff");
    public string Input=>Source+(Replay?" · replay":"");
    public string From=>Callsign;
    public string To=>Destination;
    public string Status=>IsValidated?Validation:"Unvalidated · "+Validation;
    public string Description=>Aprs?.Summary ?? Information;
    public string Type=>Aprs?.Type ?? Kind;
}

internal sealed class PacketPipeline
{
    private readonly object sync=new();
    private readonly Queue<PacketObservation> queue=new();
    private Afsk1200 modem=new();
    private string? key;
    private bool enabled;
    private PacketContext? current;
    private long blockStart;
    private long good,bad,dropped;
    internal PacketPipeline(){Attach();}
    internal bool Enabled { get {lock(sync)return enabled;} set {lock(sync){if(enabled==value)return;enabled=value;ResetCore();}} }
    internal (long Good,long Bad,long Dropped) Counts {get{lock(sync)return(good,bad,dropped);}}
    private void Attach()=>modem.Frame+=(raw,end)=>
    {
        if(current==null)return;
        var packet=Ax25Parser.Parse(raw);if(packet.FcsValid)good++;else bad++;
        double? position=current.PositionSeconds is double start?start+(end-blockStart)/48000.0:null;
        var observation=new PacketObservation(DateTimeOffset.UtcNow,current.Source,current.Replay,current.FrequencyHz,position,packet.Validation,packet.Source,packet.Destination,packet.Path,packet.FrameType,packet.Information,packet.Aprs,Convert.ToHexString(packet.Raw));
        if(queue.Count==256){queue.Dequeue();dropped++;}queue.Enqueue(observation);
    };
    private void ResetCore(){modem=new();Attach();key=null;current=null;}
    internal void Reset(){lock(sync)ResetCore();}
    internal void Clear(){lock(sync){ResetCore();queue.Clear();good=bad=dropped=0;}}
    internal PacketObservation[] Drain(int maximum=64)
    {lock(sync){var result=new List<PacketObservation>();while(queue.Count>0 && result.Count<maximum)result.Add(queue.Dequeue());return result.ToArray();}}
    internal void Feed(ReadOnlySpan<byte> pcm,int channels,PacketContext context)
    {
        lock(sync)
        {
            if(!enabled)return;
            if(key!=context.Key){ResetCore();key=context.Key;}
            current=context;blockStart=modem.Samples;modem.Process(pcm,channels);
        }
    }
}



