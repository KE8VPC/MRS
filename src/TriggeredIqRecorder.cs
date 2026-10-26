using System.IO;
using System.Threading.Channels;
namespace MRS;

internal sealed record TriggeredIqOptions(double PreSeconds=1,double TailSeconds=2,double MaximumSeconds=30,int BudgetMb=1024)
{
    internal TriggeredIqOptions Normalize()=>new(double.IsFinite(PreSeconds)?Math.Clamp(PreSeconds,0,3):1,double.IsFinite(TailSeconds)?Math.Clamp(TailSeconds,.2,15):2,double.IsFinite(MaximumSeconds)?Math.Clamp(MaximumSeconds,1,300):30,Math.Clamp(BudgetMb,64,16384));
}

// Capture only copies into a bounded queue. File operations and pre-roll run on this worker.
internal sealed class TriggeredIqRecorder : IDisposable
{
    private sealed record Run(int Epoch,string Folder,string Slot,IqMetadata Metadata,TriggeredIqOptions Options);
    private sealed record Input(Run? Run,IqFrame? Frame,bool Signal,TaskCompletionSource? Done=null);
    private readonly object gate=new();
    private readonly Channel<Input> queue=Channel.CreateBounded<Input>(new BoundedChannelOptions(256){SingleReader=true});
    private readonly Task worker;
    private Run? armed;private int epoch;private bool disposed;
    private string status="Disarmed";private long totalBytes;private int clips;
    internal bool Armed {get{lock(gate)return armed!=null;}}
    internal string Status=>Volatile.Read(ref status);
    internal long TotalBytes=>Interlocked.Read(ref totalBytes);
    internal int Clips=>Volatile.Read(ref clips);
    internal TriggeredIqRecorder(){worker=Task.Run(Work);}
    internal void Arm(string folder,string slot,IqMetadata metadata,TriggeredIqOptions options)
    {
        Directory.CreateDirectory(folder);
        lock(gate){if(disposed)throw new ObjectDisposedException(nameof(TriggeredIqRecorder));if(armed!=null)throw new InvalidOperationException("Disarm before changing automatic recording settings.");armed=new(++epoch,Path.GetFullPath(folder),slot,metadata,options.Normalize());Interlocked.Exchange(ref totalBytes,0);Volatile.Write(ref clips,0);status="Armed · waiting for calibrated RF activity";}
    }
    internal void Disarm(string reason="Disarmed")
    {
        lock(gate){armed=null;epoch++;status=reason;queue.Writer.TryWrite(new(null,null,false));}
    }
    internal async Task DrainAsync()
    {
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await queue.Writer.WriteAsync(new(null,null,false,done)).ConfigureAwait(false);await done.Task.ConfigureAwait(false);
    }
    internal void Feed(IqFrame frame,bool signal)
    {
        lock(gate)
        {
            if(armed==null||disposed)return;
            if(frame.Data.Length is <=0 or >32768 || frame.Data.Length%2!=0){Disarm("Stopped: invalid capture block");return;}
            if(!queue.Writer.TryWrite(new(armed,frame with{Data=(byte[])frame.Data.Clone()},signal)))Disarm("Stopped: automatic recording queue overrun; complete clips retained");
        }
    }
    private async Task Work()
    {
        var pre=new Queue<IqFrame>();long preBytes=0;double preTime=0,clipTime=0,quiet=0;
        Run? current=null;IqFrame? last=null;IqRecording? recording=null;
        void Clear(){pre.Clear();preBytes=0;preTime=0;last=null;}
        void Finish(){if(recording==null)return;var r=recording;recording=null;clipTime=0;quiet=0;r.Dispose();if(r.Error!=null)throw new IOException(r.Error);}
        bool Same(IqFrame a,IqFrame b)=>a.Frequency==b.Frequency&&a.Rate==b.Rate&&a.UsableBandwidthHz==b.UsableBandwidthHz&&a.Channel==b.Channel&&a.Gain==b.Gain;
        async Task Write(IqFrame f)
        {
            if(TotalBytes+f.Data.Length>current!.Options.BudgetMb*1000000L)throw new IOException("Session storage limit reached; disarmed");
            await recording!.WriteBuffered(f,(long)(clipTime*TimeSpan.TicksPerSecond));
            Interlocked.Add(ref totalBytes,f.Data.Length);clipTime+=f.Data.Length/2.0/f.Rate;
        }
        try
        {
            await foreach(var input in queue.Reader.ReadAllAsync())
            {
                try
                {
                    Run? live;lock(gate)live=armed;
                    if(current!=live){Finish();Clear();current=live;}
                    if(input.Done!=null){input.Done.TrySetResult();continue;}
                    if(live==null||input.Run!=live||input.Frame is not {} f)continue;
                    if(last!=null&&!Same(last,f)){Finish();Clear();status="Armed · channel changed; new pre-buffer";}
                    last=f;
                    if(recording==null&&input.Signal)
                    {
                        if(TotalBytes+f.Data.Length>live.Options.BudgetMb*1000000L)throw new IOException("Session storage limit reached; disarmed");
                        if(new DriveInfo(Path.GetPathRoot(live.Folder)!).AvailableFreeSpace<256000000)throw new IOException("Less than 256 MB free on recording drive");
                        string path=Path.Combine(live.Folder,$"MRS-AUTO-{live.Slot}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{f.Channel?.Frequency??f.Frequency}-{Guid.NewGuid():N}.mrsiq");
                        while(pre.Count>0&&preTime+f.Data.Length/2.0/f.Rate>live.Options.MaximumSeconds){var old=pre.Dequeue();preTime-=old.Data.Length/2.0/old.Rate;}
                        double duration=preTime;
                        recording=new(path,live.Metadata with{StartedUtc=DateTimeOffset.UtcNow.AddSeconds(-duration)});Interlocked.Increment(ref clips);
                        foreach(var p in pre)await Write(p);pre.Clear();preBytes=0;preTime=0;quiet=0;
                        status="Recording · "+Path.GetFileName(path);
                    }
                    if(recording!=null)
                    {
                        await Write(f);quiet=input.Signal?0:quiet+f.Data.Length/2.0/f.Rate;
                        if(quiet>=live.Options.TailSeconds||clipTime>=live.Options.MaximumSeconds){Finish();status="Armed · clip saved; waiting for activity";}
                    }
                    else
                    {
                        pre.Enqueue(f);preBytes+=f.Data.Length;preTime+=f.Data.Length/2.0/f.Rate;
                        while(pre.Count>0&&(preTime>live.Options.PreSeconds||preBytes>64000000)){var old=pre.Dequeue();preBytes-=old.Data.Length;preTime-=old.Data.Length/2.0/old.Rate;}
                    }
                }
                catch(Exception ex)
                {
                    try{Finish();}catch{}Clear();Disarm("Automatic I/Q stopped: "+ex.Message);current=null;input.Done?.TrySetException(ex);
                }
            }
        }
        finally{try{Finish();}catch(Exception ex){status="Finalization failed: "+ex.Message;}Clear();}
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;armed=null;queue.Writer.TryComplete();}worker.GetAwaiter().GetResult();}
}
