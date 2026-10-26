using System.Diagnostics;

using System.IO;

using System.Text;

using System.Text.RegularExpressions;

using System.Threading.Channels;

namespace MRS;

// Process is isolated per receiver/context. Capture never waits on a decoder pipe.

internal sealed class DigitalVoicePipeline : IDisposable

{

    private static long serial;
    private sealed class Session(PacketContext context,int mode,int slot,bool invert)

    {

        internal long Epoch=Interlocked.Increment(ref serial);internal PacketContext Context=context; internal int Mode=mode,Slot=slot;internal bool Invert=invert,Failed;

        internal NxdnControlParser Parser=new(); internal CancellationTokenSource Cancel=new();internal Channel<double[]> Queue=Channel.CreateBounded<double[]>(32);

    }

    private readonly object gate=new();private Session? active;private int mode,slot=3;private bool invert,disposed;

    private string state="Off";private long dropped,audioFrames;private readonly Queue<string> lines=[];

    internal event Action<byte[]>? Audio;

    internal event Action<NxdnReception>? Control;

    internal bool Enabled{get{lock(gate)return mode!=0;}}

    internal string Status{get{lock(gate)return state+$" · audio frames {audioFrames} · dropped {dropped}";}}

    internal string Diagnostics{get{lock(gate)return string.Join(Environment.NewLine,lines);}}

    internal void Configure(int value,int slots,bool inverted){lock(gate){mode=value;slot=slots;invert=inverted;ResetCore();}}

    internal void ClearDiagnostics(){lock(gate)lines.Clear();}
    internal void Reset(){lock(gate)ResetCore();}

    private void ResetCore(){active?.Cancel.Cancel();active?.Queue.Writer.TryComplete();active=null;state=mode==0?"Off":"Waiting for NFM discriminator";lines.Clear();audioFrames=0;}

    internal void Feed(double[] samples,PacketContext context)

    {

        lock(gate){if(disposed||mode==0)return;if(active==null||active.Context.Key!=context.Key){ResetCore();active=new(context,mode,slot,invert);var s=active;_=Task.Run(()=>Run(s));}

        if(active.Failed)return;

        if(samples.Length>48000||!active.Queue.Writer.TryWrite((double[])samples.Clone())){dropped++;ResetCore();state="Input overrun; reacquiring digital voice";}}

    }

    internal static byte[] InputPcm(double[] samples)

    {

        var bytes=new byte[samples.Length*2];for(int i=0;i<samples.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i*2,2),(short)Math.Clamp(double.IsFinite(samples[i])?samples[i]*2.5:0,short.MinValue,short.MaxValue));return bytes;

    }

    internal static byte[] ExpandPcm(ReadOnlySpan<byte> input,int channels)

    {

        var output=new byte[input.Length/(channels*2)*24];int p=0;

        for(int i=0;i+channels*2<=input.Length;i+=channels*2){short l=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(input.Slice(i,2));short r=channels==2?System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(input.Slice(i+2,2)):l;

            for(int j=0;j<6;j++){System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(p,2),l);System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(p+2,2),r);p+=4;}}return output;

    }

    private async Task Run(Session s)

    {

        using var process=new Process();var ct=s.Cancel.Token;Task audio=Task.CompletedTask,diagnostics=Task.CompletedTask;

        try{

            var exe=Path.Combine(AppContext.BaseDirectory,"Decoders","Digital","dsd-fme.exe");if(!File.Exists(exe))throw new IOException("Missing Decoders/Digital/dsd-fme.exe");

            process.StartInfo=new(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(exe)!};

            foreach(var arg in new[]{s.Mode==1?"-fs":s.Mode==2?"-fi":"-fn","-i","-","-o","-","-s","48000","-g","0","-V",s.Slot.ToString()})process.StartInfo.ArgumentList.Add(arg);

            if(s.Invert)process.StartInfo.ArgumentList.Add(s.Mode==1?"-xd":"-xn");

            if(!process.Start())throw new IOException("Could not start digital decoder");

            using var registration=ct.Register(()=>{try{if(!process.HasExited)process.Kill(true);}catch{}});

            lock(gate)if(active==s)state="Searching / receiving · clear voice";

            audio=Task.Run(async()=>{var buffer=new byte[4096];int held=0,channels=s.Mode==1?2:1,frame=channels*2;

                while(true){int n=await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(held),ct);if(n==0)break;int total=held+n,usable=total-total%frame;var pcm=ExpandPcm(buffer.AsSpan(0,usable),channels);lock(gate)if(active==s&&!ct.IsCancellationRequested){audioFrames+=usable/frame;Audio?.Invoke(pcm);}held=total-usable;if(held>0)Buffer.BlockCopy(buffer,usable,buffer,0,held);}});

            diagnostics=Task.Run(async()=>{var buffer=new char[1024];var line=new StringBuilder();int n;while((n=await process.StandardError.ReadAsync(buffer.AsMemory(),ct))>0){for(int i=0;i<n;i++){char c=buffer[i];if(c=='\n'||c=='\r'){if(line.Length>0){var text=Regex.Replace(line.ToString(),"\u001b\\[[0-9;]*[A-Za-z]","");lock(gate)if(active==s){if(lines.Count==80)lines.Dequeue();lines.Enqueue(text);if(s.Mode is 2 or 3 && s.Parser.Feed(text) is {} id){state="Control data received · NXDN";Control?.Invoke(new(id,s.Context,DateTimeOffset.UtcNow,s.Epoch));}}line.Clear();}}else if(line.Length<512)line.Append(c);}}});

            await foreach(var samples in s.Queue.Reader.ReadAllAsync(ct)){if(audio.IsFaulted)await audio;if(diagnostics.IsFaulted)await diagnostics;if(process.HasExited)throw new IOException($"Decoder exited ({process.ExitCode})");await process.StandardInput.BaseStream.WriteAsync(InputPcm(samples),ct);await process.StandardInput.BaseStream.FlushAsync(ct);}

        }catch(OperationCanceledException){}catch(Exception ex){lock(gate)if(active==s){s.Failed=true;state=ex.Message+"; select Off then retry";}}

        finally{try{if(!process.HasExited)process.Kill(true);}catch{}try{await Task.WhenAll(audio,diagnostics).WaitAsync(TimeSpan.FromSeconds(2));}catch{}}

    }

    public void Dispose(){lock(gate){disposed=true;mode=0;ResetCore();}}

}

