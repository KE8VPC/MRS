using System.IO;
using System.Text.Json;
namespace MRS;
internal static class AviationTests
{
    internal const string PositionJson="""{"decode":{"crc_ok":true},"body":{"type":"mode_s","icao":"ABCDEF","df":17,"callsign":"MRS TEST","lat":40.5,"lon":-84.2,"altitude_ft":12000,"speed_kt":210},"raw":"8dabcdef"}""";
    internal static PacketContext Context=>new("fixture","Generated aviation",true,1090000000,2.5);
    internal static void Run(Action<bool,string> check)
    {
        var m=AviationData.Parse(PositionJson,"adsb",Context)!;
        check(m.Identity=="ICAO:ABCDEF"&&m.Latitude==40.5&&m.Packet.Replay&&m.Packet.PositionSeconds==2.5,"Aviation retains decoded identity position and replay provenance");
        check(AviationData.Parse("broken","adsb",Context)==null,"Aviation rejects malformed worker JSON");
        var bad=AviationData.Parse(PositionJson.Replace("40.5","140.5"),"adsb",Context)!;
        check(bad.Latitude==null&&bad.Longitude==null,"Aviation rejects out-of-range positions");
        var table=new AircraftTable();table.Observe(m);table.Observe(AviationData.Parse(PositionJson.Replace("true","false"),"adsb",Context)!);
        check(table.Rows.Length==1&&table.Rows[0].Count==1,"Aircraft table does not promote failed validation");
        table.Observe(m with{Packet=m.Packet with{Replay=false}});check(table.Rows.Length==2,"Aircraft table separates live and replay identities");
        var unknown=AviationData.Parse("""{"decode":{"crc_ok":true},"body":{"type":"mode_s","icao":"ABCDEF","df":17}}""","adsb",Context)!;
        var fresh=new AircraftTable();fresh.Observe(unknown);check(fresh.Rows[0].Position=="—","Aircraft without decoded positions remain unmapped");
        table.Observe(m with{Packet=m.Packet with{ObservedUtc=DateTimeOffset.UtcNow.AddMinutes(-16)}});table.Expire();check(table.Rows.Length==1,"Aircraft rows expire independently of another source");
        var uat=AviationData.Parse("""{"decode":{"crc_ok":true,"fec_corrected":2},"body":{"type":"uat","kind":"adsb","details":{"address":"ABCDEF","address_qualifier":"adsb_other","position":{"lat":40,"lon":-84}}}}""","uat",Context)!;
        check(uat.Identity=="adsb_other:ABCDEF"&&uat.Packet.Validation.StartsWith("RS-FEC"),"UAT preserves non-ICAO address class and RS validation terminology");
        var frame=new IqFrame(0,1090000000,2400000,new byte[8],2400000);
        check(AviationPipeline.Check("adsb",1090000000,frame)==null,"Aviation accepts full ADS-B capture at 2.4 MS/s");
        check(AviationPipeline.Check("adsb",1090000000,frame with{Frequency=1091000000})!=null,"Aviation rejects a wideband target outside usable capture");
        check(AviationPipeline.Check("uat",978000000,frame with{Frequency=978000000,Rate=1920000})!=null,"Aviation rejects insufficient UAT sample rate");
        check(AviationPipeline.Check("vdl2",136975000,frame with{Frequency=136725000,Rate=960000,UsableBandwidthHz=960000})==null,"VDL2 accepts an offset channel fully inside capture");
    }
    internal static async Task<string> Fixtures(string folder)
    {
        var report=new List<string>();
        foreach(var mode in new[]{"acars","vdl2","adsb","uat"})
        foreach(bool signed in new[]{false,true})
        {
            long target=mode switch{"acars"=>131550000,"vdl2"=>136975000,"adsb"=>1090000000,_=>978000000};
            long offset=mode is "acars" or "vdl2"?250000:mode=="uat"?100000:0;
            using var pipeline=new AviationPipeline();pipeline.Start(mode,target);
            byte[] bytes=File.ReadAllBytes(Path.Combine(folder,mode+".u8")); if(signed)for(int i=0;i<bytes.Length;i++)bytes[i]=unchecked((byte)(bytes[i]-128));
            for(int n=0;n<bytes.Length;n+=16384){pipeline.Feed(new IqFrame(0,target-offset,2400000,bytes.AsSpan(n,Math.Min(16384,bytes.Length-n)).ToArray(),2400000,signed?IqSampleFormat.Signed8:IqSampleFormat.Unsigned8),Context with{FrequencyHz=target});await Task.Delay(5);}
            await pipeline.CompleteAsync().WaitAsync(TimeSpan.FromSeconds(30));var messages=pipeline.Drain();
            if(messages.Length!=(mode=="adsb"?8:1)||!messages.Any(m=>m.Packet.IsValidated)||!pipeline.Status.EndsWith("dropped 0"))throw new Exception($"{mode} fixture failed: {pipeline.Status}");
            report.Add($"PASS {mode} {(signed?"s8":"u8")}: {messages.Length} messages; {pipeline.Status}");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory,$"aviation-{mode}-fixture.json"),JsonSerializer.Serialize(messages,new JsonSerializerOptions{WriteIndented=true}));
        }
        // Real-time paced HackRF-sized blocks, noisy 19.2 MS/s with +750 kHz capture offset.
        string stress=Path.Combine(folder,"adsb-19m2.u8");
        if(File.Exists(stress))
        {
            using var pipeline=new AviationPipeline();pipeline.Start("adsb",1090000000);
            using var input=File.OpenRead(stress);var clock=System.Diagnostics.Stopwatch.StartNew();long sent=0;
            var block=new byte[32768];int count;
            while((count=input.Read(block))>0)
            {
                pipeline.Feed(new IqFrame(0,1090750000,19200000,block.AsSpan(0,count).ToArray(),19200000),Context);
                sent+=count;double delay=sent/38400.0-clock.Elapsed.TotalMilliseconds;
                if(delay>=1)await Task.Delay((int)delay);
            }
            await pipeline.CompleteAsync().WaitAsync(TimeSpan.FromSeconds(15));var messages=pipeline.Drain();
            if(messages.Length!=8||!pipeline.Status.EndsWith("dropped 0"))throw new Exception("19.2 MS/s sustained fixture failed: "+pipeline.Status);
            report.Add($"PASS ADS-B 19.2 MS/s +750 kHz capture offset: {messages.Length} messages, 3 seconds paced noisy input; {pipeline.Status}");
        }
        return string.Join(Environment.NewLine,report);
    }
}


