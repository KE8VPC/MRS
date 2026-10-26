namespace MRS;

internal sealed class P25Pipeline
{
    private readonly object sync=new();
    private readonly Queue<PacketObservation> queue=new();
    private P25C4fm modem=new();
    private readonly P25ChannelPlans channelPlans=new();
    private PacketContext? current;
    private string? key;
    private bool enabled;private long generation;private readonly string run=Guid.NewGuid().ToString("N");
    private long headers,messages,dropped;
    internal P25Pipeline(){Attach();}
    internal bool Enabled {get{lock(sync)return enabled;}set{lock(sync){if(enabled==value)return;enabled=value;ResetCore();}}}
    internal (long Headers,long Messages,long Dropped) Counts {get{lock(sync)return(headers,messages,dropped);}}
    private void Attach()=>modem.Message+=message=>
    {
        if(current==null)return;
        message=channelPlans.Apply(message);
        if(message.PayloadValid)messages++;else headers++;
        string validation=message.PayloadValid?"TSBK CRC valid":$"NID BCH valid ({message.NidErrors} corrected bits); header only";
        var row=new PacketObservation(DateTimeOffset.UtcNow,current.Source,current.Replay,current.FrequencyHz,current.PositionSeconds,
            validation,$"NAC {message.Nac:X3}","","",message.Kind,message.Text,null,Convert.ToHexString(message.Raw),"P25 Phase I",true,SourceKey:$"{run}:{generation}:{current.Key}",P25BandPlan:message.BandPlan,P25Channel:message.Channel,P25Assignments:message.Assignments,P25SiteAnnouncement:message.SiteAnnouncement,P25Identity:message.PayloadValid?channelPlans.Identity:null);
        if(queue.Count==256){queue.Dequeue();dropped++;}queue.Enqueue(row);
    };
    private void ResetCore(){generation++;channelPlans.Clear();modem=new();Attach();current=null;key=null;}
    internal void Reacquire(){lock(sync){modem=new();Attach();current=null;}}
    internal void Reset(){lock(sync)ResetCore();}
    internal void Clear(){lock(sync){ResetCore();queue.Clear();headers=messages=dropped=0;}}
    internal PacketObservation[] Drain(){lock(sync){var rows=queue.Take(64).ToArray();foreach(var _ in rows)queue.Dequeue();return rows;}}
    internal void Feed(ReadOnlySpan<double> samples,PacketContext context)
    {
        lock(sync)
        {
            if(!enabled)return;
            if(key!=context.Key){ResetCore();key=context.Key;}
            current=context;modem.Process(samples);
        }
    }
}



