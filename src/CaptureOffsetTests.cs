using System.IO;
using System.Text.Json;
namespace MRS;
internal static class CaptureOffsetTests
{
    internal static void Run(Action<bool,string> check)
    {
        var station = new ChannelSettings(162400000, "NFM", 12500);
        check(new CaptureOffset().Center(station,9600000)==162650000, "Capture offset +250 kHz places weather station below hardware center");
        check(new CaptureOffset(true,-250).Center(station,2400000)==162150000, "Negative capture offset supported");
        check(new CaptureOffset(false).Center(station,2400000)==162400000, "Disabled capture offset preserves direct tuning");
        foreach(var test in new[]{(new CaptureOffset(),station with{Frequency=6000000000L},9600000),(new CaptureOffset(true,-250),station with{Frequency=1000000},9600000),(new CaptureOffset(true,1000),station,2400000)})
        {bool rejected=false;try{test.Item1.Center(test.Item2,test.Item3);}catch(IOException){rejected=true;}check(rejected,"Capture offset rejects hardware or bandwidth limit without silent fallback");}
        var prefs = new Dictionary<string,CaptureOffset>{{"HackRF-A",new(true,-350)},{"HackRF-B",new(false,250)}};
        var restored=JsonSerializer.Deserialize<Dictionary<string,CaptureOffset>>(JsonSerializer.Serialize(prefs))!;
        check(restored["HackRF-A"]==prefs["HackRF-A"]&&restored["HackRF-B"]==prefs["HackRF-B"],"Independent receiver offsets survive preference serialization");
        string file=Path.Combine(Path.GetTempPath(),"mrs-offset-"+Guid.NewGuid()+".mrsiq");
        bool Wait(Func<bool> predicate)=>SpinWait.SpinUntil(predicate,6000);
        try
        {
            using var rx=new RtlReceiver();
            rx.Start(0,162400000,new HackRfGain().Token,false,fixture:true,sampleRate:2400000,fixtureSeconds:20,bandwidth:12500,backend:"HackRF",captureOffset:new());
            check(Wait(()=>rx.AppliedChannel?.Frequency==162400000&&rx.AppliedFrequency==162650000),"HackRF startup acknowledges offset center and listening frequency separately");
            int worker=rx.WorkerId;
            rx.RecordIq(file,new IqMetadata(3,DateTimeOffset.UtcNow,false,new HackRfGain().Token,null));
            rx.Retune(162425000).GetAwaiter().GetResult();
            check(Wait(()=>rx.AppliedFrequency==162675000&&rx.AppliedChannel?.Frequency==162425000),"Live HackRF offset retune preserves listening frequency");
            rx.SetGain(new HackRfGain(24,24).Token).GetAwaiter().GetResult();
            check(Wait(()=>rx.AppliedGain==new HackRfGain(24,24).Token)&&rx.AppliedChannel?.Frequency==162425000,"Gain change preserves offset channel");
            rx.SetRate(9600000).GetAwaiter().GetResult();
            check(Wait(()=>rx.AppliedRate==9600000)&&rx.AppliedFrequency==162675000&&rx.AppliedChannel?.Frequency==162425000&&rx.WorkerId==worker&&rx.IsRecording,"Rate change preserves offset, worker and active recording");
            Thread.Sleep(100);rx.Stop();
            using(var reader=new IqReader(file))
            { IqFrame? frame;int count=0;bool valid=true;while((frame=reader.Read())!=null){count++;valid&=frame.Channel!=null&&frame.Frequency-frame.Channel.Frequency==250000;}check(count>0&&valid,"Recorded offset frames pair raw capture center with correct listening channel"); }
            rx.StartReplay(file);check(Wait(()=>rx.ReplayFinished)&&rx.AppliedFrequency==162675000&&rx.AppliedChannel?.Frequency==162425000,"Replay restores offset metadata without applying offset twice");
        }
        finally {if(File.Exists(file)) File.Delete(file);}
    }
}
