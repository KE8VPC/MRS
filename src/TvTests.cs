using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
namespace MRS;
internal static class TvTests
{
    internal static void Run(Action<bool,string> check)
    {
        var packages=File.ReadAllLines(Path.Combine(TvRuntime.Assets,"runtime-lock.txt"));
        int glib=Array.FindIndex(packages,p=>p.Contains("/libglib-")),pixbuf=Array.FindIndex(packages,p=>p.Contains("/gdk-pixbuf-"));
        check(glib>0&&pixbuf>glib,"TV setup installs GLib before running the GDK post-link helper");
        bool Reject(Action action){try{action();return false;}catch(IOException){return true;}}
        foreach(int channel in new[]{2,4,5,6,7,13,14,36})
            check(TvCapture.ChannelCenter(channel)==(channel switch{2=>57,4=>69,5=>79,6=>85,7=>177,13=>213,14=>473,_=>605})*1000000L,$"TV physical RF channel {channel} center mapping");
        var capture=new TvCapture(474000000,9600000,9000000,473000000);capture.Validate();
        check(Reject(()=>(capture with{Rate=3200000}).Validate()),"TV rejects narrow RTL sample rate");
        check(Reject(()=>(capture with{Station=470000000}).Validate()),"TV rejects partial channel coverage with offset");
        check(Reject(()=>(capture with{Bandwidth=double.NaN}).Validate()),"TV rejects unknown usable coverage");
        (capture with{Rate=20000000,Bandwidth=15000000}).Validate();
        check(true,"TV accepts 9.6 and 20 MS/s with 1 MHz offset");
        var frame=new IqFrame(0,474000000,9600000,new byte[4096],9000000,Channel:new ChannelSettings(473000000,"NFM"));
        check(Reject(()=>capture.Check(frame with{Frequency=475000000})),"TV rejects capture retune midstream");
        check(Reject(()=>capture.Check(frame with{Rate=20000000})),"TV rejects capture sample-rate change midstream");
        var queue=new TvLiveInput(capture);for(int i=0;i<=TvLiveInput.Capacity;i++)queue.Offer(frame);
        check(Reject(()=>queue.Feed(System.IO.Stream.Null,CancellationToken.None).GetAwaiter().GetResult()),"TV bounded queue reports overload without blocking sample pump");
    }
    internal static async Task<string> Stream(string recording,string output)
    {
        var capture=TvDecoder.RecordingCapture(recording);long good=0;var clock=Stopwatch.StartNew();
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await TvDecoder.Run(capture,output,(stream,ct)=>TvDecoder.FeedRecording(recording,capture,stream,ct),line=>{
            try{using var json=JsonDocument.Parse(line);if(json.RootElement.GetProperty("kind").GetString()=="status")good=json.RootElement.GetProperty("packets").GetInt64()-json.RootElement.GetProperty("bad_packets").GetInt64();}catch(JsonException){}
        },deadline.Token);
        if(good<1000||new FileInfo(output).Length<188000)throw new IOException("Generated TV fixture did not recover sufficient transport packets.");
        string probe=await TvRuntime.Run(TvRuntime.Command(Path.Combine(TvRuntime.Runtime,"Library","bin","ffprobe.exe"),"-v","quiet","-show_streams","-of","json",output),deadline.Token);
        if(!probe.Contains("mpeg2video")||!probe.Contains("ac3"))throw new IOException("TV transport did not expose expected test video/audio.");
        double elapsed=clock.Elapsed.TotalSeconds;
        // Exercise the same raw-I/Q event and bounded queue used by live receivers.
        long udpBytes=0;
        using(var receiver=new RtlReceiver())
        using(var socket=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))
        using(var receiveCancel=new CancellationTokenSource())
        {
            int port=((IPEndPoint)socket.Client.LocalEndPoint!).Port;
            var input=new TvLiveInput(capture);
            receiver.RawIq+=input.Offer;receiver.Ended+=_=>input.Complete();
            async Task Receive(){try{while(true){var packet=await socket.ReceiveAsync(receiveCancel.Token);Interlocked.Add(ref udpBytes,packet.Buffer.Length);}}catch(OperationCanceledException){}}
            var receive=Receive();
            try{await TvDecoder.Run(capture,output+".live.ts",async(stream,ct)=>{receiver.StartReplay(recording);await input.Feed(stream,ct);},_=>{},deadline.Token,port);}
            finally{receiver.Stop();receiveCancel.Cancel();await receive;}
            if(udpBytes<188000)throw new IOException("TV live UDP path did not recover transport data.");
            File.Delete(output+".live.ts");
        }
        using var canceled=new CancellationTokenSource();
        clock.Restart();bool stopped=false;
        try{await TvDecoder.Run(capture,output+".cancel.ts",async(_,ct)=>{canceled.Cancel();await Task.Delay(Timeout.Infinite,ct);},_=>{},canceled.Token);}
        catch(Exception)when(canceled.IsCancellationRequested){stopped=true;}
        if(!stopped||clock.Elapsed.TotalSeconds>15)throw new IOException("TV cancellation failed.");
        File.Delete(output+".cancel.ts");
        return $"PASS: {capture.Rate} S/s, {capture.Station-capture.Center} Hz offset, C# stdin worker recovered {good:N0} accepted packets in {elapsed:F2}s.\nPASS: MPEG-2 video and AC-3 audio identified.\nPASS: receiver replay/raw-IQ queue and localhost playback stream recovered {udpBytes:N0} bytes.\nPASS: cancellation after worker startup completed in {clock.Elapsed.TotalSeconds:F2}s.\nNo physical receiver opened.\n";
    }
}
