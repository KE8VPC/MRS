using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Channels;
namespace MRS;
internal sealed class SensorPipeline:IDisposable
{
    internal static readonly int[] MeterProtocols=[149,154,160,223,228,282,290];
    private sealed class Session(IqFrame first,PacketContext context,int profile,double margin,double gain)
    {
        internal readonly IqFrame First=first with{Data=[]};internal readonly PacketContext Context=context;internal readonly int Profile=profile;internal readonly double Margin=margin,Gain=gain;
        internal readonly Channel<byte[]> Input=Channel.CreateBounded<byte[]>(1024);internal readonly CancellationTokenSource Cancel=new();internal Task Work=Task.CompletedTask;internal bool Failed;
    }
    private readonly object gate=new();private Session? active;private int profile;private double margin=8,gain;private bool enabled,disposed;private string state="Off";private long decoded,dropped,bursts;private readonly Queue<SensorResult> results=[];private readonly Queue<(BurstResult Burst,PacketContext Context)> pulse=[];private readonly Queue<string> diagnostics=[];
    internal string Status{get{lock(gate)return $"{state} · messages {decoded} · bursts {bursts} · dropped {dropped}";}}
    internal string Diagnostics{get{lock(gate)return string.Join("\n",diagnostics);}}
    internal void Start(int value,double thresholdMargin,double decoderGainDb=0){lock(gate){ResetCore();results.Clear();pulse.Clear();decoded=dropped=bursts=0;enabled=true;profile=Math.Clamp(value,0,2);margin=thresholdMargin;gain=Math.Pow(10,Math.Clamp(double.IsFinite(decoderGainDb)?decoderGainDb:0,0,36)/20);state="Waiting for live SDR or I/Q replay";}}
    private void ResetCore(){var old=active;active=null;old?.Input.Writer.TryComplete();old?.Cancel.Cancel();diagnostics.Clear();}
    internal void Stop(string reason="Stopped"){lock(gate){enabled=false;ResetCore();state=reason;}}
    internal void Reset(){lock(gate){ResetCore();results.Clear();pulse.Clear();state=enabled?"Receiver changed; reacquiring":"Stopped";}}
    internal static (int Factor,int Rate,double Width) Plan(int rate,int profile){int target=profile==1?2400000:profile==2?240000:1200000;int factor=Math.Max(1,rate/target);return(factor,rate/factor,profile==2?12500:rate/(double)factor*.8);}
    internal static string? Check(IqFrame frame,long target,int profile){if(frame.Format is not (IqSampleFormat.Unsigned8 or IqSampleFormat.Signed8))return "Requires 8-bit quadrature I/Q";if(frame.Rate<240000||frame.Rate>20000000)return "Unsupported input rate";var plan=Plan(frame.Rate,profile);if(profile==1&&plan.Rate<1200000)return "Meter profile needs at least 1.2 MS/s; 2.4 MS/s or more recommended";if(Math.Abs((double)target-frame.Frequency)+plan.Width/2>Math.Min(frame.Rate,frame.Capture.UsableBandwidthHz)/2)return "Selected analysis span does not fit capture. Move capture closer, reduce offset or increase usable bandwidth (disable HF filter for meters).";return null;}
    internal void Feed(IqFrame frame,PacketContext context){lock(gate){if(!enabled||disposed)return;long target=frame.Channel?.Frequency??frame.Frequency;if(Check(frame,target,profile) is {} error){ResetCore();state=error;return;}
        if(active is {} old&&(old.First.Frequency!=frame.Frequency||old.First.Rate!=frame.Rate||old.First.Format!=frame.Format||old.First.Channel!=frame.Channel||old.First.Gain!=frame.Gain||old.First.UsableBandwidthHz!=frame.UsableBandwidthHz||old.Context.Key!=context.Key))ResetCore();
        if(active==null){var s=active=new(frame,context with{FrequencyHz=target},profile,margin,gain);s.Work=Task.Run(()=>Run(s));}if(active.Failed)return;
        if(frame.Data.Length>65536||frame.Data.Length%2!=0||!active.Input.Writer.TryWrite((byte[])frame.Data.Clone())){dropped++;enabled=false;ResetCore();state="Input overrun; stopped to preserve discontinuity. Reduce receiver rate and Start again.";}}}
    internal SensorResult[] Drain(){lock(gate){var r=results.ToArray();results.Clear();return r;}}
    internal (BurstResult Burst,PacketContext Context)[] DrainBursts(){lock(gate){var r=pulse.ToArray();pulse.Clear();return r;}}
    internal Task CompleteAsync(){lock(gate){enabled=false;active?.Input.Writer.TryComplete();return active?.Work??Task.CompletedTask;}}
    private void Diagnostic(Session s,string line){lock(gate)if(active==s){if(diagnostics.Count>=30)diagnostics.Dequeue();diagnostics.Enqueue(line.Length>500?line[..500]:line);}}
    private static Process Worker(string name,IEnumerable<string> args){string path=Path.Combine(AppContext.BaseDirectory,"Decoders","Sensors",name);if(!File.Exists(path))throw new FileNotFoundException("Extract full ZIP; missing "+name);var p=new Process{StartInfo=new(path){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(path)!}};foreach(var a in args)p.StartInfo.ArgumentList.Add(a);return p;}
    // Read bounded lines without allocating an unbounded ReadLine buffer.
    internal static async Task Lines(StreamReader reader,Action<string> line,CancellationToken ct){var chars=new char[2048];var text=new System.Text.StringBuilder();bool discard=false;int n;while((n=await reader.ReadAsync(chars.AsMemory(),ct))>0)for(int i=0;i<n;i++){char c=chars[i];if(c=='\n'){if(!discard&&text.Length>0)line(text.ToString().TrimEnd('\r'));text.Clear();discard=false;}else if(text.Length<65536&&!discard)text.Append(c);else{discard=true;text.Clear();}}if(!discard&&text.Length>0)line(text.ToString());}
    internal static byte[] DecoderInput(ReadOnlySpan<byte> floats,double gain){var bytes=new byte[floats.Length/4];for(int i=0;i<bytes.Length;i++){double value=BitConverter.ToSingle(floats.Slice(i*4,4));bytes[i]=(byte)Math.Clamp(Math.Round((double.IsFinite(value)?value:0)*gain*128+127.5),0,255);}return bytes;}
    private async Task Run(Session s)
    {
        var ct=s.Cancel.Token;var jobs=new List<Task>();Process? bridge=null,decoder=null;
        try{var plan=Plan(s.First.Rate,s.Profile);bridge=Worker("mrs-iq-channel.exe",[s.First.Rate.ToString(),plan.Factor.ToString(),((s.Context.FrequencyHz??s.First.Frequency)-s.First.Frequency).ToString(),s.First.Format==IqSampleFormat.Signed8?"1":"0",s.Profile==2?"6250":"0"]);
            if(s.Profile!=2){var args=new List<string>{"-c","0","-r","cu8:-","-s",plan.Rate.ToString(CultureInfo.InvariantCulture),"-f",s.Context.FrequencyHz!.Value.ToString(CultureInfo.InvariantCulture),"-F","json","-M","protocol","-M","time:rel","-M","level"};if(s.Profile==1)foreach(int id in MeterProtocols){args.Add("-R");args.Add(id.ToString());}decoder=Worker("rtl_433.exe",args);}
            ct.ThrowIfCancellationRequested();bridge.Start();decoder?.Start();using var kill=ct.Register(()=>{foreach(var p in new[]{bridge,decoder})try{p?.Kill(true);}catch{}});
            lock(gate)if(active==s)state=$"Receiving {(s.Profile==1?"meters":s.Profile==2?"burst inspection":"sensors")} · {plan.Rate:N0} samples/s · {(s.Context.Replay?"replay":"live")}";
            var inspector=new BurstInspector(plan.Rate,s.Margin,s.Profile==2);inspector.Burst+=b=>{lock(gate)if(active==s&&!ct.IsCancellationRequested){if(pulse.Count>=64){pulse.Dequeue();dropped++;}pulse.Enqueue((b,s.Context));bursts++;}};
            jobs.Add(Lines(bridge.StandardError,l=>Diagnostic(s,l),ct));
            if(decoder!=null){jobs.Add(Lines(decoder.StandardError,l=>Diagnostic(s,l),ct));jobs.Add(Lines(decoder.StandardOutput,l=>{var r=SensorResult.Parse(l,s.Context);if(r==null)return;lock(gate)if(active==s&&!ct.IsCancellationRequested){if(results.Count>=512){results.Dequeue();dropped++;}results.Enqueue(r);decoded++;}},ct));}
            var output=Task.Run(async()=>{var b=new byte[65536];int carry=0,n;while((n=await bridge.StandardOutput.BaseStream.ReadAsync(b.AsMemory(carry),ct))>0){int total=n+carry,usable=total-total%8;inspector.Process(b.AsSpan(0,usable),true);if(decoder!=null)await decoder.StandardInput.BaseStream.WriteAsync(DecoderInput(b.AsSpan(0,usable),s.Gain),ct);carry=total-usable;if(carry>0)Buffer.BlockCopy(b,usable,b,0,carry);}inspector.Finish();if(decoder!=null){decoder.StandardInput.Close();await decoder.WaitForExitAsync(ct);if(decoder.ExitCode!=0)throw new IOException($"rtl_433 exited {decoder.ExitCode}; see diagnostics");}},ct);jobs.Add(output);
            await foreach(var b in s.Input.Reader.ReadAllAsync(ct)){if(output.IsFaulted)await output;await bridge.StandardInput.BaseStream.WriteAsync(b,ct);}bridge.StandardInput.Close();await bridge.WaitForExitAsync(ct);await Task.WhenAll(jobs);if(bridge.ExitCode!=0)throw new IOException("I/Q bridge failed "+bridge.ExitCode);lock(gate)if(active==s)state="Source finished; Start to resume";
        }catch(OperationCanceledException){}catch(Exception ex){lock(gate)if(active==s){s.Failed=true;state="Decoder stopped: "+ex.Message;}}
        finally{foreach(var p in new[]{bridge,decoder})try{if(p!=null&&!p.HasExited)p.Kill(true);}catch{}try{await Task.WhenAll(jobs).WaitAsync(TimeSpan.FromSeconds(2));}catch{}bridge?.Dispose();decoder?.Dispose();}
    }
    public void Dispose(){lock(gate){disposed=true;enabled=false;ResetCore();}}
}
