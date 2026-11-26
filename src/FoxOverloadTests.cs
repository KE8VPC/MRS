using System.IO;
using System.Xml.Linq;
namespace MRS;
internal static class FoxOverloadTests
{
    internal static void Run(Action<bool,string> check)
    {
        var rail=ReceiverInputLevel.Measure(new byte[]{0,255,128,128},1);
        check(rail.Components==4&&rail.RailComponents==2,"Fox ADC rail counts retain measured component denominator");
        check(FoxOverload.State(rail,false),"Fox rail evidence raises overload");
        check(FoxOverload.State(new(-10,-4,0,2),true)&&!FoxOverload.State(new(-10,-4,0,2),false),"Fox input warning uses hysteresis rather than flickering at threshold");
        check(!FoxOverload.State(new(-20,-7,0,3),true),"Fox input warning clears below recovery threshold");
        check(!FoxOverload.Mark(null,null),"Legacy Fox data does not invent ADC evidence");
        var now=DateTimeOffset.UtcNow;var points=FoxAnalysisTests.Route(now);
        var overloaded=points.Select(p=>p with{Overloaded=true,InputPeakDbfs=-1,ClippedComponents=2,InputComponents=100}).ToArray();
        var result=FoxLocator.Estimate(overloaded);
        check(result.Latitude==null&&result.Detail.Contains("excluded"),"Fox estimator excludes flagged overload and explains exclusion");
        var flat=overloaded.Take(6).Select((p,i)=>p with{Utc=now.AddSeconds(i),MeanDb=-15,Fix=p.Fix with{Latitude=40+i*.002,Longitude=-84}}).ToArray();
        check(FoxOverload.FlatHint(flat)!=null,"Fox high-input flat moving route gets a cautious lower-gain hint");
        check(FoxOverload.FlatHint(flat.Select((p,i)=>p with{MeanDb=-15-i*3}).ToArray())==null,"Fox varying readings do not trigger flat-strength hint");
        string folder=Path.Combine(AppContext.BaseDirectory,"fox-overload-tests");Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,Guid.NewGuid()+".gpx");
        try{FoxSession.ExportGpx(file,overloaded.Take(1));var xml=XDocument.Load(file);XNamespace ext="urn:mrs:foxhunt:1";
            check(xml.Descendants(ext+"clippedComponents").Single().Value=="2"&&xml.Descendants(ext+"overloaded").Single().Value=="true","Fox GPX preserves overload evidence without replacing relative signal");}
        finally{File.Delete(file);}
        check(FoxSession.Csv(overloaded[0]).EndsWith(string.Join(',',new[]{"-1","2","100","true"}.Select(PlanCsv.Quote))),"Fox CSV appends ADC quality fields with explicit overload state");
        string csv=Path.Combine(folder,Guid.NewGuid()+".csv");
        try
        {
            using(var session=new FoxSession())
            {
                var fix=new FoxFix(40,-84,now,"Synthetic GPS");session.Start(csv,FoxHuntTests.Scope,fix);
                var point=session.Record(now,fix,[new(now,FoxHuntTests.Scope,-20,true,rail)],-80)!;
                check(point.Overloaded&&point.ClippedComponents==2&&point.InputComponents==4&&point.PeakDb==-20,"Fox log separates actual ADC overload from post-filter channel power");
            }
            var row=PlanCsv.Read(csv).Single();
            check(row["overloaded"]=="true"&&row["clipped_components"]=="2","Fox log headers and ADC quality values round-trip through CSV parser");
        }
        finally{File.Delete(csv);}
    }
}
