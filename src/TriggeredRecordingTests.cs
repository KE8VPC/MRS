using System.IO;
namespace MRS;
internal static class TriggeredRecordingTests
{
    internal static void Run(Action<bool,string> check)
    {
        string root=Path.Combine(AppContext.BaseDirectory,"trigger-tests",Guid.NewGuid().ToString("N"));
        IqFrame Frame(byte value=128,long hz=160230000)=>new(0,hz,240000,Enumerable.Repeat(value,32768).ToArray(),240000,IqSampleFormat.Unsigned8,new(hz,"NFM",12500),0,0);
        var meta=new IqMetadata(3,DateTimeOffset.UtcNow,false,0,"synthetic","NFM");
        void Drain(TriggeredIqRecorder r)=>r.DrainAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        List<IqFrame> Read(string file){using var reader=new IqReader(file);var list=new List<IqFrame>();while(reader.Read() is {} f)list.Add(f);return list;}
        string Folder(string name)=>Path.Combine(root,name);
        using(var r=new TriggeredIqRecorder())
        {
            r.Arm(Folder("quiet"),"A",meta,new(.2,.2,2));for(int i=0;i<8;i++)r.Feed(Frame(),false);Drain(r);
            check(r.Clips==0&&!Directory.GetFiles(Folder("quiet")).Any(),"Automatic I/Q does not record noise below threshold");
            r.Feed(Frame(140),true);for(int i=0;i<4;i++)r.Feed(Frame(130),false);Drain(r);
            var frames=Read(Directory.GetFiles(Folder("quiet")).Single());
            check(frames.Count>=6&&frames[0].Data[0]==128&&frames.Any(f=>f.Data[0]==140)&&frames[^1].Data[0]==130,"Automatic clip preserves pre-trigger, signal and quiet tail samples");
            check(frames.Zip(frames.Skip(1)).All(pair=>pair.Second.ElapsedTicks>pair.First.ElapsedTicks),"Buffered pre-trigger frames retain increasing sample-derived replay timestamps");
            check(frames.All(f=>f.Channel?.Frequency==160230000)&&frames[0].Capture.UsableBandwidthHz==240000,"Automatic clips preserve capture and tuned-channel metadata");
            r.Disarm();Drain(r);r.Feed(Frame(),true);Drain(r);check(r.Clips==1,"Disarmed automatic recorder ignores new activity");
        }
        using(var r=new TriggeredIqRecorder())
        {
            r.Arm(Folder("maximum"),"B",meta,new(0,.2,1));for(int i=0;i<35;i++)r.Feed(Frame(),true);Drain(r);r.Disarm();Drain(r);
            var files=Directory.GetFiles(Folder("maximum"));check(files.Length==3&&files.All(f=>Read(f).Count<=15),"Continuous signal rotates clips at maximum duration and finalizes on disarm");
        }
        using(var r=new TriggeredIqRecorder())
        {
            r.Arm(Folder("retune"),"A",meta,new(.2,.2,2));r.Feed(Frame(),true);r.Feed(Frame(hz:160245000),true);Drain(r);r.Disarm();Drain(r);
            var files=Directory.GetFiles(Folder("retune"));check(files.Length==2&&files.All(f=>Read(f).Select(v=>v.Channel!.Frequency).Distinct().Count()==1),"Retuning splits recordings without mixing channel identities");
        }
        using(var r=new TriggeredIqRecorder())
        {
            string bad=Folder("blocked");r.Arm(bad,"A",meta,new());Directory.Delete(bad);File.WriteAllText(bad,"blocked");r.Feed(Frame(),true);Drain(r);
            check(!r.Armed&&r.Status.Contains("stopped",StringComparison.OrdinalIgnoreCase),"Automatic recording disk errors disarm with a visible failure");
        }
        using(var r=new TriggeredIqRecorder())
        {
            r.Arm(Folder("budget"),"A",meta,new(0,.2,300,64));
            for(int batch=0;batch<65&&r.Armed;batch++){for(int i=0;i<32;i++)r.Feed(Frame(),true);Drain(r);}
            check(!r.Armed&&r.Status.Contains("limit")&&r.TotalBytes<=64000000,"Session payload budget disarms before exceeding storage limit");
            check(Directory.GetFiles(Folder("budget")).All(f=>Read(f).Count>0),"Budget stop retains clean readable clips");
        }
        check(new TriggeredIqOptions(double.NaN,double.PositiveInfinity,-1,-1).Normalize()==new TriggeredIqOptions(1,2,1,64),"Automatic recording bounds invalid duration and storage settings");
        var plans=new CsvBandPlans(Folder("plans"));CsvBandPlans.Seed(plans.Root);plans.Reload();var rail=plans.Markers.Where(m=>m.Plan=="reference/railroad"&&m.Low==m.High).ToArray();
        check(rail.Length==93&&rail.Any(m=>m.Low==160215000&&m.Label=="AAR 007")&&rail.Any(m=>m.Low==161565000&&m.Label=="AAR 097"),"Railroad CSV includes 91 exact AAR reference centers with leading zero channel labels");
        check(rail.Any(m=>m.Low==161205000&&m.Label.Contains("073"))&&rail.Any(m=>m.Low==452937500&&m.Label.StartsWith("HOT"))&&rail.Any(m=>m.Low==457937500&&m.Label.StartsWith("EOT")),"Railroad CSV identifies police mutual aid and separate HOT/EOT telemetry references");
        string csv=Path.Combine(plans.Root,"reference","railroad","channels.csv");File.AppendAllText(csv,"\n");string before=File.ReadAllText(csv);CsvBandPlans.Seed(plans.Root);check(File.ReadAllText(csv)==before,"Reference seeding preserves user-edited railroad CSV");
    }
}
