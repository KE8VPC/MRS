using System.Diagnostics;
using System.IO;
using System.Threading.Channels;
namespace MRS;
internal sealed record TvCapture(long Center,int Rate,double Bandwidth,long Station)
{
    internal static TvCapture From(IqFrame frame,long station)=>new(frame.Frequency,frame.Rate,frame.Capture.UsableBandwidthHz,station);
    internal void Validate()
    {
        if(Rate<8000000 || Rate>20000000)throw new IOException("ATSC 1.0 requires 8-20 MS/s. For HackRF choose 9.6, 19.2 or 20 MS/s.");
        if(!double.IsFinite(Bandwidth) || Bandwidth<6000000 || Math.Abs(Station-(double)Center)+3000000>Math.Min(Bandwidth,Rate)/2)throw new IOException("The entire 6 MHz TV channel must fit inside capture coverage. Reduce capture offset or increase sample rate.");
    }
    internal void Check(IqFrame frame)
    {
        if(frame.Format!=IqSampleFormat.Unsigned8 || frame.Frequency!=Center || frame.Rate!=Rate || frame.Capture.UsableBandwidthHz!=Bandwidth || frame.Channel is {} ch && ch.Frequency!=Station)
            throw new IOException("Receiver frequency, rate or channel changed. Restart TV decoding on the new capture.");
    }
    internal static long ChannelCenter(int channel)=>channel switch
    {>=2 and <=4=>57000000L+(channel-2)*6000000L,>=5 and <=6=>79000000L+(channel-5)*6000000L,>=7 and <=13=>177000000L+(channel-7)*6000000L,>=14 and <=36=>473000000L+(channel-14)*6000000L,_=>throw new ArgumentOutOfRangeException(nameof(channel))};
}
internal sealed class TvLiveInput(TvCapture capture)
{
    // Up to 64 MiB with current 32 KiB receiver frames; bounded, never block RF acquisition.
    internal const int Capacity=2048;
    private readonly Channel<IqFrame> frames=Channel.CreateBounded<IqFrame>(new BoundedChannelOptions(Capacity){SingleReader=true,SingleWriter=true});
    private bool failed;
    internal void Offer(IqFrame frame)
    {
        if(failed)return;
        // Frames own immutable arrays; retain them without blocking the receiver sample pump.
        if(!frames.Writer.TryWrite(frame)){failed=true;frames.Writer.TryComplete(new IOException("TV input queue overflow: decoding cannot keep up. Receiver remains running. Try recording I/Q and decoding the file."));}
    }
    internal void Complete()=>frames.Writer.TryComplete();
    internal async Task Feed(Stream stream,CancellationToken token)
    {await foreach(var frame in frames.Reader.ReadAllAsync(token)){capture.Check(frame);await stream.WriteAsync(frame.Data,token);}}
}
internal static class TvDecoder
{
    internal static async Task Run(TvCapture capture,string output,Func<Stream,CancellationToken,Task> feed,Action<string> report,CancellationToken cancel,int udpPort=0)
    {
        capture.Validate();await TvRuntime.Probe(cancel);
        using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(cancel);var token=lifetime.Token;
        var info=TvRuntime.Command(TvRuntime.Python,Path.Combine(TvRuntime.Assets,"atsc_worker.py"),"--rate",capture.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture),"--offset",(capture.Station-capture.Center).ToString(System.Globalization.CultureInfo.InvariantCulture),"--output",output,"--udp-port",udpPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
        info.RedirectStandardInput=true;
        using var process=new Process{StartInfo=info};process.Start();
        void Kill(){try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}}
        using var registration=token.Register(Kill);
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Read(StreamReader reader){while(await reader.ReadLineAsync(token) is {} line){if(line.Contains("\"kind\": \"started\""))ready.TrySetResult();report(line);}}
        bool complete=false;
        async Task Write(){try{await ready.Task.WaitAsync(TimeSpan.FromSeconds(30),token);await feed(process.StandardInput.BaseStream,token);complete=true;}finally{process.StandardInput.Close();}}
        Exception? failure=null;
        async Task Guard(Func<Task> action){try{await action();}catch(Exception ex){Interlocked.CompareExchange(ref failure,ex,null);lifetime.Cancel();throw;}}
        async Task Exit(){await process.WaitForExitAsync();if(!complete&&!token.IsCancellationRequested)throw new IOException("TV decoder stopped before its I/Q input finished.");}
        try{await Task.WhenAll(Guard(()=>Read(process.StandardOutput)),Guard(()=>Read(process.StandardError)),Guard(Write),Guard(Exit));cancel.ThrowIfCancellationRequested();if(process.ExitCode!=0)throw new IOException("TV decoder exited with code "+process.ExitCode);}
        catch when(!cancel.IsCancellationRequested && failure!=null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();throw;}
        finally{Kill();await process.WaitForExitAsync();}
    }
    internal static TvCapture RecordingCapture(string path)
    {using var reader=new IqReader(path);var frame=reader.Read()??throw new IOException("Recording has no I/Q samples.");var capture=TvCapture.From(frame,frame.Channel?.Frequency??frame.Frequency);capture.Validate();capture.Check(frame);return capture;}
    internal static async Task FeedRecording(string path,TvCapture capture,Stream stream,CancellationToken token)
    {
        using var reader=new IqReader(path);long? previous=null;
        while(reader.Read() is {} frame)
        {token.ThrowIfCancellationRequested();capture.Check(frame);if(previous is {} time&&(frame.ElapsedTicks-time>2500000 || frame.ElapsedTicks<time))throw new IOException("I/Q recording contains a timing gap. Use a continuous TV capture.");previous=frame.ElapsedTicks;await stream.WriteAsync(frame.Data,token);}
    }
}
