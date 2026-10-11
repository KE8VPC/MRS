using System.Buffers.Binary;

namespace MRS;
internal sealed class SubaudiblePipeline
{
    private readonly object sync=new();
    private readonly Queue<PacketObservation> queue=new();
    private SubaudibleDecoder decoder=new();
    private PacketContext? current;
    private string? key;
    private bool enabled;
    internal SubaudiblePipeline(){Attach();}
    internal bool Enabled {get{lock(sync)return enabled;}set{lock(sync){if(enabled==value)return;enabled=value;ResetCore();}}}
    internal SubaudibleDetection? Current {get{lock(sync)return decoder.Current;}}
    private void Attach()=>decoder.Changed+=(previous,next)=>
    {
        if(current==null)return;
        if(previous!=null)Enqueue(previous,"Signal lost",$"{previous.Protocol} {previous.Value} no longer confirmed.");
        if(next!=null)Enqueue(next,"Signal acquired",$"{next.Protocol} {next.Value}. {next.Evidence}");
    };
    private void Enqueue(SubaudibleDetection result,string kind,string text)
    {
        if(queue.Count==256)queue.Dequeue();
        queue.Enqueue(new(DateTimeOffset.UtcNow,current!.Source,current.Replay,current.FrequencyHz,current.PositionSeconds,
            kind=="Signal acquired"?"Stable tone/code":"Detection ended","","","",kind,text,null,"",result.Protocol,true));
    }
    private void ResetCore(){decoder=new();Attach();current=null;key=null;}
    internal void Reset(){lock(sync)ResetCore();}
    internal void Clear(){lock(sync){ResetCore();queue.Clear();}}
    internal PacketObservation[] Drain(){lock(sync){var result=queue.Take(64).ToArray();foreach(var _ in result)queue.Dequeue();return result;}}
    private void Context(PacketContext context){if(key!=context.Key){ResetCore();key=context.Key;}current=context;}
    internal void FeedDiscriminator(ReadOnlySpan<double> samples,PacketContext context)
    {
        lock(sync)
        {
            if(!enabled)return;Context(context);
            // The production FM discriminator expresses instantaneous deviation in Hz.
            Span<double> normalized=stackalloc double[1024];
            for(int at=0;at<samples.Length;at+=normalized.Length)
            {
                int length=Math.Min(normalized.Length,samples.Length-at);
                for(int n=0;n<length;n++)normalized[n]=samples[at+n]/5000;
                decoder.Process(normalized[..length]);
            }
        }
    }
    internal void FeedPcm(ReadOnlySpan<byte> bytes,int channels,PacketContext context)
    {
        lock(sync)
        {
            if(!enabled || channels is not (1 or 2))return;Context(context);
            Span<double> normalized=stackalloc double[1024];int count=0;
            for(int at=0;at+channels*2<=bytes.Length;at+=channels*2)
            {
                double sample=0;for(int channel=0;channel<channels;channel++)sample+=BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(at+channel*2,2))/32768.0;
                normalized[count++]=sample/channels;
                if(count==normalized.Length){decoder.Process(normalized);count=0;}
            }
            decoder.Process(normalized[..count]);
        }
    }
}
