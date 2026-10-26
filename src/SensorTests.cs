using System.IO;
using System.Text.Json;
namespace MRS;
internal static class SensorTests
{
    internal static void Run(Action<bool,string> check)
    {
        var ctx=new PacketContext("sensor-fixture","Synthetic receiver",true,433920000,12);
        var r=SensorResult.Parse("{\"model\":\"Weather-fixture\",\"id\":42,\"temperature_C\":21.5,\"mic\":\"CHECKSUM\",\"time\":\"@1.25s\"}",ctx)!;
        check(r.Id=="42"&&r.Context.PositionSeconds==13.25&&r.Summary.Contains("21.5")&&r.Integrity.Contains("CHECKSUM"),"Sensor JSON retains identity, measurement, backend check and replay-relative time");
        check(SensorResult.Parse("not json",ctx)==null&&SensorResult.Parse("{\"temperature_C\":2}",ctx)==null&&SensorResult.Parse("[]",ctx)==null,"Malformed and diagnostic JSON do not become device observations");
        check(SensorResult.Parse(new string('x',70000),ctx)==null,"Sensor JSON length is bounded");
        var unvalidated=SensorResult.Parse("{\"model\":\"Unknown\"}",ctx)!;check(unvalidated.Integrity.Contains("unspecified")&&unvalidated.Id=="unknown","Missing integrity and identity remain explicit unknowns");
        check(r.Key!=(r with{Context=ctx with{Replay=false}}).Key&&r.Key!=(r with{Context=ctx with{Source="Receiver B"}}).Key,"Sensor grouping separates live/replay and physical source");
        var floats=new[]{0f,.001f,1f,-1f,float.NaN}.SelectMany(BitConverter.GetBytes).ToArray();var normalized=SensorPipeline.DecoderInput(floats,16);check(normalized[0]==128&&normalized[1]>128&&normalized[2]==255&&normalized[3]==0&&normalized[4]==128,"Float-to-decoder gain preserves faint signals with bounded clipping and nonfinite handling");
        var f=new IqFrame(0,434170000,9600000,[],9600000,IqSampleFormat.Unsigned8,new(433920000,"NFM",12500));
        check(SensorPipeline.Check(f,433920000,0)==null&&SensorPipeline.Plan(f.Rate,0).Rate==1200000&&SensorPipeline.Plan(f.Rate,1).Rate==2400000,"Sensor and meter spans fit offset HackRF capture and choose bounded native rates");
        check(SensorPipeline.Check(f with{UsableBandwidthHz=1750000},433920000,1)!=null,"Meter setup rejects a span truncated by the HF filter");
        check(SensorPipeline.Check(f with{Format=IqSampleFormat.Signed16LittleEndian},433920000,0)!=null,"Unsupported sample formats are rejected before starting workers");
        var a=new BurstInspector(240000);var bursts=new List<BurstResult>();a.Burst+=bursts.Add;var bytes=new byte[240000];for(int i=0;i<bytes.Length;i+=2){bool high=i>=60000&&i<90000&&(i/120)%2==0;bytes[i]=(byte)(high?200:128);bytes[i+1]=128;}a.Process(bytes);a.Finish();check(bursts.Any(b=>b.PulseCount>10&&b.Timings.Length<=2048&&b.DurationMs>10),"Experimental burst inspection retains bounded pulse timing across an OOK burst");
        var quiet=new BurstInspector(240000);int count=0;quiet.Burst+=_=>count++;quiet.Process(Enumerable.Repeat((byte)128,120000).ToArray());quiet.Finish();check(count==0,"Quiet constant I/Q does not create a validated device or burst");
        var noise=new BurstInspector(240000,8,true);int noiseBursts=0;noise.Burst+=_=>noiseBursts++;var random=new Random(124);var noiseBytes=new byte[480000];for(int i=0;i<noiseBytes.Length;i++)noiseBytes[i]=(byte)random.Next(125,131);noise.Process(noiseBytes);noise.Finish();check(noiseBursts==0,"Integrated burst gate rejects quiet varying noise rather than triggering on individual peaks");
        string path=Path.Combine(AppContext.BaseDirectory,"railroad-expanded-test",Guid.NewGuid().ToString("N"));RailroadPlan.SeedExpanded(path);var plans=new CsvBandPlans(path);plans.Reload();check(plans.Error==""&&plans.Plans.Count==8,"Expanded railroad resources seed eight valid CSV layers");var csv=Directory.GetFiles(path,"channels.csv",SearchOption.AllDirectories)[0];File.WriteAllText(csv,"user custom data");RailroadPlan.SeedExpanded(path);check(File.ReadAllText(csv)=="user custom data","Expanded railroad migration preserves existing custom files");
        var view=new SpectrumView();view.SetRf(433920000,9600000);view.Configure(new(){Zoom=4096});view.SetChannel(new(433463000,"NFM",12500));check(Math.Abs(view.VisibleCenter-433463000)<1,"Follow tuned keeps the channel visible at maximum zoom");view.Configure(view.Settings with{ViewMode=1});view.SetChannel(new(434000000,"NFM",12500));check(Math.Abs(view.VisibleCenter-434000000)<1,"Center lock centers the tuned channel independently of capture center");view.Configure(view.Settings with{ViewMode=2});double center=view.VisibleCenter;view.SetChannel(new(432000000,"NFM",12500));check(view.VisibleCenter==center,"Free view preserves independent viewport when tuning");
    }
    internal static async Task<string> Fixtures(string folder)
    {
        var lines=new List<string>();foreach(var test in new[]{("acurite-acurite_th_609_001.cu8",250000,0,"Acurite-609TXC"),("ert-g001_912.6M_2400k.cu8",2400000,1,"ERT-SCM"),("scmplus-g002_912.6M_2359.3k.cu8",2359296,1,"SCMplus"),("idm-g002_912.6M_2359.3k.cu8",2359296,1,"IDM")})
        {using var pipeline=new SensorPipeline();pipeline.Start(test.Item3,8);long hz=test.Item3==1?912600000:433920000;var bytes=File.ReadAllBytes(Path.Combine(folder,test.Item1));var ctx=new PacketContext("fixture","Public upstream recording",true,hz,0);
            // Quiet padding gives the pulse analyzer calibration; public fixture bytes stay unchanged.
            var padded=Enumerable.Repeat((byte)128,test.Item2/2).Concat(bytes).Concat(Enumerable.Repeat((byte)128,test.Item2/8)).ToArray();
            for(int n=0;n<padded.Length;n+=32768){pipeline.Feed(new(0,hz,test.Item2,padded.Skip(n).Take(Math.Min(32768,padded.Length-n)).ToArray(),test.Item2,IqSampleFormat.Unsigned8,new(hz,"NFM",12500)),ctx);}
            await pipeline.CompleteAsync().WaitAsync(TimeSpan.FromSeconds(30));var rows=pipeline.Drain();if(!rows.Any(r=>r.Model==test.Item4))throw new IOException($"{test.Item1}: no {test.Item4}; {pipeline.Status}\n{pipeline.Diagnostics}");lines.Add($"PASS: {test.Item4}: {rows.Length} observations via complete M.R.S. native bridge and JSON pipeline; {rows[0].Integrity}");}
        return string.Join("\n",lines);
    }
}
