using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;

namespace MRS;

// Each receiver owns a bounded, isolated decoder. Capture never waits for a process or pipe.
internal sealed class PagingPipeline : IDisposable
{
    private sealed record Block(double[] Samples, PacketContext Context);
    private sealed class Session(PacketContext context)
    {
        internal PacketContext Context = context;
        internal readonly CancellationTokenSource Cancel = new();
        internal readonly Channel<Block> Input = Channel.CreateBounded<Block>(new BoundedChannelOptions(64) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        internal Task Work = Task.CompletedTask;
        internal bool Failed;
    }
    private readonly object gate = new();
    private readonly Queue<PacketObservation> results = new();
    private Session? active;
    private bool enabled, disposed;
    private long decoded, rejected, dropped, flexPhases, flexUncorrectable;
    private string state = "Waiting for NFM discriminator or flat radio audio";
    internal bool Enabled { get { lock(gate)return enabled; } set { lock(gate){ if(enabled==value)return; enabled=value;ResetCore(); } } }
    internal string Status { get { lock(gate)return !enabled?"Paging off":$"Paging: {state} · decoded {decoded} · unvalidated {rejected} · dropped {dropped} · FLEX BCH uncorrectable {flexUncorrectable} / {flexPhases} phases"; } }
    private void ResetCore() { var old=active;active=null;old?.Input.Writer.TryComplete();old?.Cancel.Cancel();state="Waiting for NFM discriminator or flat radio audio"; }
    internal void Reset(){lock(gate)ResetCore();}
    internal void Clear(){lock(gate){ResetCore();results.Clear();decoded=rejected=dropped=flexPhases=flexUncorrectable=0;}}
    internal Task CompleteAsync(){lock(gate){active?.Input.Writer.TryComplete();return active?.Work??Task.CompletedTask;}}
    internal void Feed(double[] samples,PacketContext context)
    {
        lock(gate)
        {
            if(!enabled||disposed)return;
            if(active==null || active.Context.Key!=context.Key)
            {
                ResetCore();var created=new Session(context);active=created;
                created.Work=Task.Run(()=>Run(created));
            }
            if(active.Failed)return;
            // Bound individual blocks as well as queue length. A discontinuity must reacquire sync.
            if(samples.Length>48000 || !active.Input.Writer.TryWrite(new Block((double[])samples.Clone(),context)))
            {dropped++;ResetCore();state="Input overrun; reacquiring paging sync";}
        }
    }
    internal void FeedPcm(ReadOnlySpan<byte> pcm,int channels,PacketContext context)
    {
        if(!Enabled || channels<1)return;
        var samples=new double[pcm.Length/(2*channels)];
        for(int n=0;n<samples.Length;n++){double sum=0;for(int c=0;c<channels;c++)sum+=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.Slice((n*channels+c)*2,2));samples[n]=sum/channels/2.5;}
        Feed(samples,context);
    }
    internal PacketObservation[] Drain(int max=64){lock(gate){var list=new List<PacketObservation>();while(results.Count>0&&list.Count<max)list.Add(results.Dequeue());return list.ToArray();}}
    private async Task Run(Session session)
    {
        using var process=new Process();var ct=session.Cancel.Token;
        Task output=Task.CompletedTask,error=Task.CompletedTask;
        try
        {
            var exe=Path.Combine(AppContext.BaseDirectory,"Decoders","Paging","multimon-ng.exe");
            if(!File.Exists(exe))throw new FileNotFoundException("Paging worker missing; extract the whole release ZIP",exe);
            process.StartInfo=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(exe)!};
            foreach(string arg in new[]{"--json","-q","-v","0","-c","-a","POCSAG512","-a","POCSAG1200","-a","POCSAG2400","-a","FLEX_NEXT","-t","raw","-"})process.StartInfo.ArgumentList.Add(arg);
            ct.ThrowIfCancellationRequested();process.Start();
            using var kill=ct.Register(()=>{try{process.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}});
            lock(gate)if(active==session)state="Searching / receiving · POCSAG 512/1200/2400 + FLEX";
            output=Task.Run(async()=>
            {
                while(await process.StandardOutput.ReadLineAsync(ct) is {} line)
                {
                    if(line.Length>32768)continue;
                    lock(gate)
                    {
                        if(active!=session || ct.IsCancellationRequested)continue;
                        if(PagingJson.BchErrors(line) is int errors){flexPhases++;flexUncorrectable+=errors;continue;}
                        var row=PagingJson.Parse(line,session.Context);if(row==null)continue;
                        if(row.IsValidated)decoded++;else rejected++;
                        if(results.Count==256){results.Dequeue();dropped++;}results.Enqueue(row);
                    }
                }
            });
            // Drain stderr without retaining an unbounded diagnostic log.
            error=Task.Run(async()=>{var buffer=new char[2048];while(await process.StandardError.ReadAsync(buffer.AsMemory(),ct)>0){};});
            var resampler=new PagingResampler();
            await foreach(var block in session.Input.Reader.ReadAllAsync(ct))
            {
                lock(gate)session.Context=block.Context;
                var pcm=resampler.Convert(block.Samples);
                await process.StandardInput.BaseStream.WriteAsync(pcm,ct);
                await process.StandardInput.BaseStream.FlushAsync(ct);
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromSeconds(5),ct);
            await Task.WhenAll(output,error);
            if(process.ExitCode!=0)throw new IOException($"Paging worker exited ({process.ExitCode})");
            lock(gate)if(active==session)state="Source ended";
        }
        catch(OperationCanceledException){}
        catch(Exception ex){lock(gate)if(active==session){session.Failed=true;state=ex.Message+"; toggle Paging to retry";}}
        finally
        {
            try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
            try{await Task.WhenAll(output,error).WaitAsync(TimeSpan.FromSeconds(2));}catch{}
            // Cancellation source remains valid for concurrent Reset; it owns no wait handle.
        }
    }
    public void Dispose(){lock(gate){disposed=true;enabled=false;ResetCore();}}
}

internal static class PagingJson
{
    internal static int? BchErrors(string line)
    {
        try {using var doc=JsonDocument.Parse(line);var j=doc.RootElement;
            if(j.ValueKind==JsonValueKind.Object && j.TryGetProperty("msg_type",out var type) && type.ValueKind==JsonValueKind.String && type.GetString()=="bch_stats" && j.TryGetProperty("bch_uncorr",out var v) && v.TryGetInt32(out int n) && n>=0 && n<=88)return n;
        }catch(JsonException){}catch(InvalidOperationException){}return null;
    }
    internal static PacketObservation? Parse(string line,PacketContext source)
    {
        try
        {
            using var doc=JsonDocument.Parse(line);var j=doc.RootElement;
            if(j.ValueKind!=JsonValueKind.Object)return null;
            string S(string name)=>j.TryGetProperty(name,out var v)?v.ValueKind==JsonValueKind.String?v.GetString()??"":v.ValueKind==JsonValueKind.Number?v.ToString():"":"";
            bool? B(string name)=>j.TryGetProperty(name,out var v)&&v.ValueKind is JsonValueKind.True or JsonValueKind.False?v.GetBoolean():null;
            var demod=S("demod_name");string protocol,kind,text,validation,address;string? display=null;bool valid;
            if(demod is "POCSAG512" or "POCSAG1200" or "POCSAG2400")
            {
                protocol="POCSAG";address=S("address");
                string encoding=j.TryGetProperty("alpha",out _)?"alpha":j.TryGetProperty("numeric",out _)?"numeric":"tone";
                kind=encoding=="tone"?"Tone page":"Page · "+encoding;
                display=encoding=="tone"?"Address only / tone page":S(encoding);
                valid=encoding!="tone" && B("sync")==true && long.TryParse(address,out var cap)&&cap>=0&&cap<=2097151;
                validation=encoding=="tone"?"Address only · no message CRC":valid?"BCH decoded · no message CRC":"Incomplete / lost sync · no message CRC";
                text=$"{demod[6..]} baud · Function {S("function")} · "+(encoding=="tone"?"Tone only":S(encoding));
            }
            else if(j.TryGetProperty("baud",out _) && j.TryGetProperty("msg_type",out _))
            {
                protocol="FLEX";address=S("capcode");kind=S("msg_type");
                if(kind=="bch_stats")return null; // Diagnostic frames are not pages.
                var k=B("k_ok");var signature=B("sig_ok");
                valid=k==true && signature!=false && B("frag_seq_error")!=true;
                string fragment=S("fragment");
                validation=(k==true?"K checksum passed":k==false?"K checksum FAILED":"Checksum unavailable")+(signature==false?" · signature FAILED":"")+(B("frag_seq_error")==true?" · sequence error":"");
                if(fragment.Length>0)validation+=" · "+fragment;
                text=$"{S("baud")} symbols/s · {S("level")}-FSK · Phase {S("phase")} · Cycle {S("cycle")}, frame {S("frame")} · {S("message")}";
                if(fragment.Length>0)kind+=" · "+fragment;
                display=S("message");
            }
            else return null;
            return new PacketObservation(DateTimeOffset.UtcNow,source.Source,source.Replay,source.FrequencyHz,source.PositionSeconds,validation,"",address,"",kind,text,null,"",protocol,valid,SourceKey:source.Key,DecoderJson:line,DisplaySummary:string.IsNullOrWhiteSpace(display)?null:display.Replace('\r',' ').Replace('\n',' '));
        }
        catch(JsonException){return null;}
    }
}

// 48 kHz discriminator Hz -> 22,050 Hz signed PCM. State spans callback boundaries.
internal sealed class PagingResampler
{
    private static readonly double[] taps=MakeTaps();
    private readonly double[] ring=new double[81];
    private int cursor;
    private long index=-1,next;
    private double previous;
    private static double[] MakeTaps()
    {
        var a=new double[81];double sum=0;
        for(int n=0;n<a.Length;n++){double x=n-40;a[n]=(x==0?1.0/3:Math.Sin(Math.PI*x/3)/(Math.PI*x))*(.54-.46*Math.Cos(2*Math.PI*n/80));sum+=a[n];}
        for(int n=0;n<a.Length;n++)a[n]/=sum;return a;
    }
    internal byte[] Convert(ReadOnlySpan<double> samples)
    {
        using var output=new MemoryStream();
        foreach(double sample in samples)
        {
            ring[cursor]=double.IsFinite(sample)?sample:0;double filtered=0;int at=cursor;
            for(int n=0;n<taps.Length;n++){filtered+=ring[at]*taps[n];if(--at<0)at=80;}
            cursor=(cursor+1)%81;index++;
            while(next<=index*147)
            {
                double fraction=next/147.0-(index-1);
                short value=(short)Math.Clamp(Math.Round((previous+(filtered-previous)*fraction)*2.5),short.MinValue,short.MaxValue);
                output.WriteByte((byte)value);output.WriteByte((byte)(value>>8));next+=320;
            }
            previous=filtered;
        }
        return output.ToArray();
    }
}
