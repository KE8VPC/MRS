using System.Threading.Channels;
namespace MRS;
// One bounded worker per receiver; capture callbacks only enqueue. Reset discards stale context and audio.
internal sealed class VoiceIdPipeline:IDisposable
{
    private sealed record Block(double[] Samples,PacketContext Context,bool Voice);
    private sealed class Session(string key){internal string Key=key;internal Channel<Block> Queue=Channel.CreateBounded<Block>(32);internal CancellationTokenSource Cancel=new();}
    private readonly object gate=new();private Session? active;private bool disposed;private bool voice,cw;private string state="Off";private long dropped;private readonly Queue<PacketObservation> results=[];
    internal event Action<byte[]>? Audio;
    internal bool Voice {get{lock(gate)return voice;}set{lock(gate){voice=value;ResetCore();}}}
    internal bool Cw {get{lock(gate)return cw;}set{lock(gate){cw=value;ResetCore();}}}
    internal string Status{get{lock(gate)return state+$" · dropped {dropped}";}}
    internal void Reset(){lock(gate)ResetCore();}
    private void ResetCore(){active?.Queue.Writer.TryComplete();active?.Cancel.Cancel();active=null;state=voice||cw?"Waiting for NFM/CW audio":"Off";results.Clear();}
    internal void Feed(double[] values,PacketContext context,bool forVoice)
    {
        lock(gate){if(disposed||!(forVoice?voice:cw))return;
            if(active==null||active.Key!=context.Key){ResetCore();var s=new Session(context.Key);active=s;Task.Run(()=>Run(s));}
            if(values.Length>48000||!active.Queue.Writer.TryWrite(new((double[])values.Clone(),context,forVoice))){dropped++;ResetCore();state="Input overrun; reacquiring";}}
    }
    private async Task Run(Session s)
    {
        using var p25=new P25VoiceDecoder();var morse=new MorseDecoder();PacketContext? context=null;
        p25.Audio+=pcm=>{lock(gate)if(active==s&&!s.Cancel.IsCancellationRequested&&voice)Audio?.Invoke(pcm);};
        morse.Result+=r=>{lock(gate){if(active!=s||context==null)return;if(results.Count==128)results.Dequeue();
            results.Enqueue(new(DateTimeOffset.UtcNow,context.Source,context.Replay,context.FrequencyHz,context.PositionSeconds,"Morse timing estimate; no checksum",r.Text,"","","CW station ID",$"{r.Text} · {r.Wpm:0} WPM · {r.ToneHz:0} Hz · {(r.TimingPlausible?"candidate ID — verify by ear":"uncertain/partial")}",null,"","CW / Morse",false,SourceKey:context.Key));}};
        try{await foreach(var block in s.Queue.Reader.ReadAllAsync(s.Cancel.Token)){context=block.Context;if(block.Voice)p25.Feed(block.Samples);else morse.Feed(block.Samples);lock(gate)if(active==s)state=(voice?p25.Status:"P25 voice off")+(cw?" · CW listening · candidates are unverified":"");}}
        catch(OperationCanceledException){}catch(Exception ex){lock(gate)if(active==s)state="Voice/CW decoder stopped: "+ex.Message;}
    }
    internal PacketObservation[] Drain(){lock(gate){var rows=results.ToArray();results.Clear();return rows;}}
    public void Dispose(){lock(gate){disposed=true;ResetCore();}}
}
