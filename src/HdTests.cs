using System.IO;
namespace MRS;
internal static class HdTests
{
    internal static void Run(Action<bool,string> check)
    {
        var metadata=new HdMetadata();metadata.Accept("12:34:56 Synchronized");metadata.Accept("12:34:56 Station name: TEST ");metadata.Accept("12:34:56 Audio service 1: public");metadata.Accept("12:34:56 Title: A title");
        check(metadata.Locked && metadata.Station=="TEST" && metadata.Title=="A title" && metadata.Services.SetEquals(new[]{1}),"HD parses pinned decoder metadata and advertised services");
        check(!metadata.Accept("12:34:56 Title: A title"),"HD suppresses duplicate metadata events");
        metadata.Accept("12:34:56 Lost synchronization");
        check(!metadata.Locked && metadata.Station=="" && metadata.Title=="" && metadata.Services.Count==0,"HD synchronization loss clears stale program information");
        var frame=new IqFrame(0,102100000,1920000,Enumerable.Repeat((byte)128,4096).ToArray(),1920000,IqSampleFormat.Unsigned8,new ChannelSettings(102100000,"WFM"));
        using(var sink=new MemoryStream())
        {
            var input=new HdLiveInput(102100000);input.Offer(frame);input.Complete();input.Feed(sink,CancellationToken.None).GetAwaiter().GetResult();
            check(sink.Length==2048L*HdIqConverter.OutputRate/1920000*2,"HD receiver input preserves complex-sample rate conversion");
        }
        bool liveRejected=false;
        try{var input=new HdLiveInput(102100000);input.Offer(frame with {Frequency=103300000});input.Complete();using var sink=new MemoryStream();input.Feed(sink,CancellationToken.None).GetAwaiter().GetResult();}catch(IOException){liveRejected=true;}
        check(liveRejected,"HD receiver input rejects retuned frames");
        liveRejected=false;
        try{var input=new HdLiveInput(102100000);for(int n=0;n<33;n++)input.Offer(frame);using var sink=new MemoryStream();input.Feed(sink,CancellationToken.None).GetAwaiter().GetResult();}catch(Exception ex)when(ex is IOException or System.Threading.Channels.ChannelClosedException){liveRejected=true;}
        check(liveRejected,"HD bounded receiver queue reports overflow instead of silently dropping I/Q");
        foreach(int rate in new[]{960000,1920000,3200000})
        {
            var random=new Random(93);var data=new byte[65536];random.NextBytes(data);
            var whole=new HdIqConverter(rate).Process(data);
            var streaming=new HdIqConverter(rate);var pieces=new List<byte>();
            for(int offset=0;offset<data.Length;offset+=1024)pieces.AddRange(streaming.Process(data[offset..Math.Min(offset+1024,data.Length)]));
            check(whole.SequenceEqual(pieces) && whole.Length==(long)data.Length/2*HdIqConverter.OutputRate/rate*2,$"HD {rate}: exact output count and continuous state across blocks");
            double Energy(double frequency)
            {
                var tone=new byte[131072];
                for(int n=0;n<tone.Length/2;n++){double angle=2*Math.PI*frequency*n/rate;tone[2*n]=(byte)Math.Round(127.5+70*Math.Cos(angle));tone[2*n+1]=(byte)Math.Round(127.5+70*Math.Sin(angle));}
                var output=new HdIqConverter(rate).Process(tone);double power=0;
                for(int n=1024;n<output.Length;n++)power+=Math.Pow(output[n]-127.5,2);
                return power/(output.Length-1024);
            }
            double pass=Energy(200000),stop=Energy(450000);
            check(pass>2000 && stop<pass*.002,$"HD {rate}: preserves digital sidebands and rejects out-of-band energy");
        }
        bool rejected=false;try{new HdIqConverter(240000);}catch(IOException){rejected=true;}
        check(rejected,"HD rejects narrowband recordings");
        rejected=false;try{new HdIqConverter(1920000).Process(new byte[3]);}catch(IOException){rejected=true;}
        check(rejected,"HD rejects incomplete complex samples");
        string folder=Path.Combine(Path.GetTempPath(),"mrs-hd-test-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=Path.Combine(folder,"input.mrsiq"),output=Path.Combine(folder,"output.cu8");
            void Record(bool changeRate=false,bool offCenter=false)
            {
                using var writer=new IqRecording(source,new IqMetadata(3,DateTimeOffset.UtcNow,true,-1000,null,"WFM"));
                writer.Write(new byte[4096],new CaptureDescriptor(102100000,1920000,1920000,IqSampleFormat.Unsigned8),new ChannelSettings(offCenter?102200000:102100000,"WFM"));
                if(changeRate)writer.Write(new byte[4096],new CaptureDescriptor(102100000,3200000,3200000,IqSampleFormat.Unsigned8),new ChannelSettings(102100000,"WFM"));
            }
            bool Reject()
            {
                if(File.Exists(output))File.Delete(output);
                try{HdIqConverter.Convert(source,output,CancellationToken.None);return false;}catch(IOException){return true;}
            }
            Record(changeRate:true);check(Reject(),"HD file conversion rejects sample-rate changes");
            Record(offCenter:true);check(Reject(),"HD file conversion rejects off-center channels");
            Record();using(var file=File.OpenWrite(source))file.SetLength(file.Length-8);
            check(Reject(),"HD file conversion rejects missing clean completion marker");
            File.Delete(output);using var canceled=new CancellationTokenSource();canceled.Cancel();
            bool canceledEarly=false;try{HdIqConverter.Convert(source,output,canceled.Token);}catch(OperationCanceledException){canceledEarly=true;}
            check(canceledEarly && !File.Exists(output),"HD cancellation before conversion creates no temporary output");
        }
        finally{foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
    }
}
