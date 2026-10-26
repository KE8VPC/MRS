using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Channels;
namespace MRS;
internal sealed class AviationPipeline : IDisposable
{
    private sealed class Session(string mode,long target,IqFrame first,PacketContext context)
    {
        internal readonly string Mode=mode;internal readonly long Target=target;internal readonly IqFrame First=first;
        internal PacketContext Context=context;
        internal readonly CancellationTokenSource Cancel=new();
        internal readonly Channel<IqFrame> Queue=Channel.CreateBounded<IqFrame>(new BoundedChannelOptions(Math.Clamp((int)Math.Ceiling(first.Rate * 2.0 / 32768), 64, 2048)){SingleReader=true,FullMode=BoundedChannelFullMode.Wait});
        internal bool Failed;
        internal Task Work=Task.CompletedTask;
    }
    private readonly object gate=new();private readonly Queue<AviationMessage> results=[];private Session? active;
    private string? mode;private long target;private string status="Aviation off";private long dropped,decoded;private bool disposed;
    internal string Status{get{lock(gate)return $"{status} · decoded {decoded} · dropped {dropped}";}}
    internal bool Enabled{get{lock(gate)return mode!=null;}}
    internal void Start(string selected,long frequency){lock(gate){ResetCore();mode=selected;target=frequency;status="Waiting for SDR or I/Q replay";}}
    private void ResetCore(){var old=active;active=null;old?.Queue.Writer.TryComplete();old?.Cancel.Cancel();}
    internal void Stop(string reason="Aviation off"){lock(gate){mode=null;ResetCore();status=reason;}}
    internal static string? Check(string mode,long target,IqFrame frame)
    {
        if(mode is not ("acars" or "vdl2" or "adsb" or "uat"))return "Unknown aviation mode";
        if(frame.Format is not (IqSampleFormat.Unsigned8 or IqSampleFormat.Signed8))return "Aviation requires 8-bit complex I/Q";
        double width=mode switch{"acars"=>15000,"vdl2"=>25000,"adsb"=>2000000,_=>1600000};
        int minimum=mode switch{"acars"=>24000,"vdl2"=>50000,"adsb"=>2000000,_=>2083334};
        if(frame.Rate<minimum)return $"{mode}: needs at least {minimum:N0} samples/s";
        if(Math.Abs((double)target-frame.Frequency)+width/2>Math.Min(frame.Rate,frame.Capture.UsableBandwidthHz)/2)return $"Target channel does not fit: center {frame.Frequency/1e6:0.######} MHz · usable {frame.Capture.UsableBandwidthHz/1e6:0.###} MHz · required {width/1e6:0.###} MHz around {target/1e6:0.######} MHz. "+(frame.Capture.UsableBandwidthHz==1750000?"Check the HF hardware filter (1.75 MHz); disable it and use 9.6 MS/s on HackRF. ":"")+"Use Check / setup receiver for recommendations.";
        if(mode=="adsb"&&target!=1090000000||mode=="uat"&&target!=978000000)return "Use 1090 MHz for ADS-B or 978 MHz for UAT";
        return null;
    }
    internal void Feed(IqFrame frame,PacketContext context)
    {
        lock(gate)
        {
            if(mode==null||disposed)return;
            if(Check(mode,target,frame) is {} error){ResetCore();status=error;return;}
            if(active!=null&&(active.First.Frequency!=frame.Frequency||active.First.Rate!=frame.Rate||active.First.Format!=frame.Format||active.Context.Key!=context.Key))ResetCore();
            if(active==null){active=new(mode,target,frame with{Data=[]},context);var session=active;session.Work=Task.Run(()=>Run(session));}
            if(active.Failed)return;
            active.Context=context;
            if(frame.Data.Length>262144||frame.Data.Length%2!=0||!active.Queue.Writer.TryWrite(frame with{Data=(byte[])frame.Data.Clone()})){dropped++;ResetCore();status="Input overrun; reacquiring decoder";}
        }
    }
    internal AviationMessage[] Drain(){lock(gate){var list=results.ToArray();results.Clear();return list;}}
    internal Task CompleteAsync(){lock(gate){active?.Queue.Writer.TryComplete();return active?.Work??Task.CompletedTask;}}
    internal async Task FinishAsync()
    {
        Session? s;lock(gate){mode=null;s=active;s?.Queue.Writer.TryComplete();}
        if(s!=null)await s.Work;
        lock(gate)if(active==s&&(s==null||!s.Failed))status="Source ended; queued aviation data processed. Start again to resume";
    }
    private async Task Run(Session s)
    {
        using var process=new Process();var ct=s.Cancel.Token;
        try
        {
            string exe=Path.Combine(AppContext.BaseDirectory,"Decoders","Aviation","mrs-aviation.exe");
            process.StartInfo=new(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(exe)!};
            foreach(var arg in new[]{s.Mode,s.First.Rate.ToString(CultureInfo.InvariantCulture),(s.Target-s.First.Frequency).ToString(CultureInfo.InvariantCulture),s.Target.ToString(CultureInfo.InvariantCulture),s.First.Format==IqSampleFormat.Signed8?"s8":"u8"})process.StartInfo.ArgumentList.Add(arg);
            ct.ThrowIfCancellationRequested();process.Start();
            using var kill=ct.Register(()=>{try{process.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}});
            lock(gate)if(active==s)status=$"{s.Mode} searching / receiving at {s.Target/1e6:F6} MHz";
            string workerError="";
            var errors=Task.Run(async()=>{while(await process.StandardError.ReadLineAsync(ct) is {} line)workerError=line.Length>300?line[..300]:line;},ct);
            var output=Task.Run(async()=>
            {
                while(await process.StandardOutput.ReadLineAsync(ct) is {} line)
                {
                    if(line.Length>262144)continue;
                    PacketContext context;lock(gate)context=s.Context with{FrequencyHz=s.Target};
                    var message=AviationData.Parse(line,s.Mode,context);if(message==null)continue;
                    lock(gate){if(active!=s)continue;if(results.Count>=512){results.Dequeue();dropped++;}results.Enqueue(message);decoded++;}
                }
            },ct);
            try
            {
                await foreach(var frame in s.Queue.Reader.ReadAllAsync(ct))
                {await process.StandardInput.BaseStream.WriteAsync(BitConverter.GetBytes(frame.Data.Length),ct);await process.StandardInput.BaseStream.WriteAsync(frame.Data,ct);await process.StandardInput.BaseStream.FlushAsync(ct);}
                process.StandardInput.Close();await process.WaitForExitAsync(ct);await Task.WhenAll(output,errors);
                if(process.ExitCode!=0)throw new IOException(workerError.Length>0?workerError:$"Worker exited {process.ExitCode}");
            }
            catch{try{if(!process.HasExited)process.Kill(true);}catch{}try{await Task.WhenAll(output,errors);}catch{}throw;}
        }
        catch(OperationCanceledException){}
        catch(Exception ex){lock(gate)if(active==s){s.Failed=true;status="Aviation worker: "+ex.Message;}}
    }
    public void Dispose(){lock(gate){disposed=true;mode=null;ResetCore();}}
}

