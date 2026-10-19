using System.IO;
namespace MRS;
internal static class HfTests
{
    internal static void Run(Action<bool,string> check)
    {
        var gain=new HackRfGain(16,16,false,true);check(HackRfGain.Decode(gain.Token)==gain,"HF setting survives transport token without changing gains");
        check(!HackRfGain.Decode(new HackRfGain().Token).HfFilter,"Older gain tokens retain automatic filter selection");
        using var stream=new MemoryStream();HackRfProtocol.Write(stream,new byte[4096],7350000,9600000,gain.Token);
        var h=HackRfProtocol.Header(stream.ToArray()[..32]);check(h.Rate==9600000&&h.UsableBandwidth==1750000,"HF transport retains high sampling rate and narrow actual capture coverage");
        var channel=new ChannelSettings(7100000,"USB",2400,true);
        check(new CaptureOffset(true,250).Center(channel,9600000,gain.Token)==7350000,"HF filter accepts offset channel inside hardware coverage");
        bool rejected=false;try{new CaptureOffset(true,1000).Center(channel,9600000,gain.Token);}catch(IOException){rejected=true;}check(rejected,"HF filter rejects one MHz offset before receiver start");
        var zero=ReceiverInputLevel.Measure(Enumerable.Repeat((byte)128,1024).ToArray(),1);check(zero.RmsDb==-120&&zero.RailPercent==0,"Input monitor distinguishes digital silence from clipping");
        var half=ReceiverInputLevel.Measure(new byte[]{64,192,64,192},2);check(Math.Abs(half.RmsDb+6.0206)<.001&&half.RailPercent==0,"Input monitor reports component RMS relative to ADC full scale");
        var rails=ReceiverInputLevel.Measure(new byte[]{0,255,128,128},3);check(rails.RailPercent==50&&rails.PeakDb==0,"Input monitor counts both ADC rails");
        string file=Path.Combine(Path.GetTempPath(),"mrs-hf-"+Guid.NewGuid()+".mrsiq");
        try
        {
            using var r=new RtlReceiver();using var ready=new ManualResetEventSlim();r.Applied+=(_,_)=>ready.Set();
            r.Start(0,7100000,gain.Token,false,fixture:true,sampleRate:9600000,fixtureSeconds:15,mode:"USB",bandwidth:2400,backend:"HackRF",captureOffset:new(true,250));
            check(ready.Wait(6000)&&r.AppliedBandwidth==1750000&&r.AppliedChannel?.Frequency==7100000,"HF worker fixture acknowledges narrow coverage and selected station");
            r.RecordIq(file,new IqMetadata(3,DateTimeOffset.UtcNow,false,gain.Token,null,Backend:"HackRF"));Thread.Sleep(250);r.FinishIq();r.Stop();
            using var reader=new IqReader(file);var frame=reader.Read();check(frame!=null&&frame.Capture.UsableBandwidthHz==1750000&&frame.Channel?.Frequency==7100000&&HackRfGain.Decode(frame.Gain!.Value).HfFilter,"HF I/Q recording preserves filter coverage and station metadata");
        }
        finally{if(File.Exists(file))File.Delete(file);}
    }
}
