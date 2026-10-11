using System.Diagnostics;
using System.IO;
namespace MRS;
internal static class HdStreamTests
{
    internal static async Task<string> Run(string executable,string fixture)
    {
        long bytes=0;var metadata=new HdMetadata();var clock=Stopwatch.StartNew();
        await HdStreamDecoder.Run(executable,0,(stream,ct)=>HdStreamDecoder.FeedRecording(fixture,stream,ct),
            (_,n)=>Interlocked.Add(ref bytes,n),line=>metadata.Accept(line),CancellationToken.None);
        if(bytes<176400 || metadata.Station.Length==0 || metadata.Services.Count==0)throw new IOException($"HD fixture did not recover sufficient audio and metadata: {bytes} bytes; {metadata.Summary}");
        double seconds=clock.Elapsed.TotalSeconds;
        long receiverBytes=0;
        using(var receiver=new RtlReceiver())
        using(var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60)))
        {
            using var reader=new IqReader(fixture);var frame=reader.Read()??throw new IOException("Empty fixture.");
            var input=new HdLiveInput(frame.Frequency);
            receiver.RawIq+=input.Offer;receiver.Ended+=_=>input.Complete();
            var decode=HdStreamDecoder.Run(executable,0,input.Feed,(_,n)=>Interlocked.Add(ref receiverBytes,n),_=>{},deadline.Token);
            try{receiver.StartReplay(fixture);await decode;}
            finally{receiver.Stop();deadline.Cancel();}
            if(receiverBytes<176400)throw new IOException("Receiver raw-I/Q event path failed to produce HD audio.");
        }
        using var canceled=new CancellationTokenSource(300);clock.Restart();bool stopped=false;
        try{await HdStreamDecoder.Run(executable,0,(stream,ct)=>HdStreamDecoder.FeedRecording(fixture,stream,ct),(_,_)=>{},_=>{},canceled.Token);}
        catch(Exception)when(canceled.IsCancellationRequested){stopped=true;}
        if(!stopped || clock.Elapsed.TotalSeconds>5)throw new IOException("HD cancellation did not finish promptly.");
        return $"PASS: HD stdin/stdout streaming recovered {bytes} stereo PCM bytes in {seconds:F2}s.\n{metadata.Summary}\nPASS: Receiver replay/raw-I/Q event path recovered {receiverBytes} PCM bytes.\nPASS: owned decoder cancellation completed within five seconds.\nNo audio endpoint or physical receiver opened.";
    }
}
