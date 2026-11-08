using System.IO;
using System.Text.Json;
using System.Xml.Linq;
namespace MRS;
internal static class FoxAnalysisTests
{
 internal static FoxPoint Point(double east,double north,double db,DateTimeOffset at)=>new(at,new(40+north/111195,-100+east/(111195*Math.Cos(40*Math.PI/180)),at,"Synthetic NMEA",250,20,0,9,.8,at),FoxHuntTests.Scope,db,db+2,100,true,-100,1);
 internal static FoxPoint[] Route(DateTimeOffset now)=>Enumerable.Range(0,64).Select(i=>{double x=(i%8-3.5)*450,y=(i/8-3.5)*450,d=Math.Sqrt((x-170)*(x-170)+(y+230)*(y+230));return Point(x,y,-40-24*Math.Log10(Math.Max(80,d)/100),now.AddSeconds(i-64));}).ToArray();
 internal static void Run(Action<bool,string> check)
 {
  var now=DateTimeOffset.UtcNow;var trend=Enumerable.Range(0,21).Select(i=>Point(i*10,0,-70+i*.4,now.AddSeconds(i-20))).ToArray();
  check(FoxStrengthTrend.Evaluate(trend,now).State.Contains("Stronger"),"Fox trend compares robust recent windows for increasing power");
  check(FoxStrengthTrend.Evaluate(trend.Select(p=>p with{MeanDb=-60-(p.Utc-trend[0].Utc).TotalSeconds*.4,PeakDb=-45}).ToArray(),now).State.Contains("Weaker"),"Fox trend distinguishes decreasing power");
  check(FoxStrengthTrend.Evaluate(trend.Select(p=>p with{MeanDb=-60}).ToArray(),now).State.Contains("Steady"),"Fox trend reports steady reception");
  check(FoxStrengthTrend.Evaluate(trend,now.AddSeconds(13)).State=="Paused","Fox trend expires after a reception gap");
  check(FoxStrengthTrend.Evaluate(trend.Select(p=>p with{FloorDb=null}).ToArray(),now).Change==null,"Fox does not infer a reliable trend without noise evidence");
  check(FoxStrengthTrend.Evaluate(trend.Select(p=>p with{MeanDb=-98,PeakDb=-95}).ToArray(),now).Change==null,"Fox noise-only observations cannot show stronger/closer guidance");
  check(FoxStrengthTrend.Evaluate(trend.Take(3).ToArray(),now).Change==null,"Fox needs enough temporal coverage for a trend");
  check(FoxStrengthTrend.Evaluate(trend.Select((p,i)=>i>15?p with{Segment=2}:p).ToArray(),now).Change==null,"Fox does not bridge route gaps for trend comparison");
  check(FoxStrengthTrend.Evaluate(trend.Where((_,i)=>i%5==0).ToArray(),now).State.Contains("Stronger"),"Fox trend also supports five-second logging intervals");
  var route=Route(now);var estimate=FoxLocator.Estimate(route);
  check(estimate.State=="Rough estimate","Synthetic surrounding route yields a rough source area: "+estimate.State+" "+estimate.Detail);
  check(estimate.Latitude!=null&&FoxNmea.Distance(40-230/111195.0,-100+170/(111195*Math.Cos(40*Math.PI/180)),estimate.Latitude.Value,estimate.Longitude!.Value)<400,"Synthetic estimated area is close to the model source without exact-coordinate promises");
  check(estimate.RadiusMetres>0&&estimate.Detail.Contains("not a calibrated"),"Fox estimate reports model spread without invented confidence percentage");
  check(FoxLocator.Estimate(route.Select(p=>p with{MeanDb=-60}).ToArray()).Latitude==null,"Flat power cannot establish a transmitter location");
  check(FoxLocator.Estimate(route.Select(p=>p with{FloorDb=null}).ToArray()).Latitude==null,"Unknown noise reference withholds a source area");
  check(FoxLocator.Estimate(route.Select(p=>p with{MeanDb=-97,PeakDb=-95}).ToArray()).Latitude==null,"Near-floor route cannot yield a source pin");
  check(FoxLocator.Estimate(route.Select(p=>p with{PeakDb=-1}).ToArray()).Latitude==null,"Possible clipping withholds source estimates");
  check(FoxLocator.Estimate(route.Select(p=>p with{Fix=p.Fix with{Hdop=8}}).ToArray()).Latitude==null,"Poor GPS quality does not support a source estimate");
  check(FoxLocator.Estimate(route.Select(p=>p with{Fix=p.Fix with{ReceivedUtc=p.Utc.AddSeconds(-8)}}).ToArray()).Latitude==null,"Stale fixes cannot support historical source estimates");
  check(FoxLocator.Estimate(route.Select(p=>p with{Fix=p.Fix with{Source="Manual stationary"}}).ToArray()).Latitude==null,"Manual locations are excluded from automatic source estimates");
  check(FoxLocator.Estimate(route.Select((p,i)=>i==0?p with{Scope=p.Scope with{Gain=300}}:p).ToArray()).State=="Unavailable","Gain changes reject a mixed source-estimation session");
  check(FoxLocator.Estimate(route.Select(p=>p with{Scope=p.Scope with{Gain=-1000}}).ToArray()).State=="Unavailable","Automatic RF gain is rejected by the estimator");
  var line=Enumerable.Range(0,80).Select(i=>Point(i*100,0,-45-i*.3,now.AddSeconds(i-80))).ToArray();check(FoxLocator.Estimate(line).State=="Not enough coverage","A long single-road route cannot masquerade as triangulation");
  check(FoxLocator.Estimate(Enumerable.Repeat(route[0],500).ToArray()).State=="Not enough coverage","Stationary duplicates do not inflate source coverage");
  var noisy=new Random(711);check(FoxLocator.Estimate(route.Select(p=>p with{MeanDb=-55+noisy.NextDouble()*30,PeakDb=-15}).ToArray()).Latitude==null,"Random spatial power is not accepted as a stable source model");
  var offset=route.Select(p=>p with{MeanDb=p.MeanDb+7,PeakDb=p.PeakDb+7}).ToArray();var shifted=FoxLocator.Estimate(offset);check(shifted.Latitude!=null&&estimate.Latitude!=null&&FoxNmea.Distance(estimate.Latitude.Value,estimate.Longitude!.Value,shifted.Latitude.Value,shifted.Longitude!.Value)<1,"Unknown constant receiver/transmitter power offset does not move the model area");
  var canceled=new CancellationToken(true);bool canceledOk=false;try{FoxLocator.Estimate(route,canceled);}catch(OperationCanceledException){canceledOk=true;}check(canceledOk,"Background source search supports cancellation");
  var snapshot=new FoxEstimateSnapshot(estimate,now,now,64);check(snapshot.Describe(false,now.AddHours(1)).Contains("historical session snapshot"),"Stopped Fox session estimates are explicitly historical");
  check(snapshot.Overlay(true,now.AddSeconds(13))==null&&snapshot.Describe(true,now.AddSeconds(13)).Contains("Estimate paused"),"Live Fox estimate hides its area after measurements pause");
  check(snapshot.Overlay(false,now.AddHours(1))!=null,"A stopped session remains available for deliberate historical review");
  check(snapshot.Overlay(true,now.AddSeconds(-3))==null&&snapshot.Describe(true,now.AddSeconds(-3)).Contains("clock moved"),"Backwards clock changes cannot make an estimate appear freshly calculated");
  check(snapshot.Describe(true,now).Contains("64 points")&&snapshot.Describe(true,now).Contains("measured through"),"Source-area status retains sample count and measurement time");
  string folder=Path.Combine(Path.GetTempPath(),"MRS-Fox-Tools-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  try{
   string gpx=Path.Combine(folder,"route.gpx");FoxSession.ExportGpx(gpx,route);var xml=XDocument.Load(gpx);XNamespace ext="urn:mrs:foxhunt:1";
   check(xml.Descendants(ext+"gainToken").Count()==64&&xml.Descendants(ext+"noiseFloorRelativeDb").Count()==64&&xml.Descendants(ext+"hdop").Count()==64,"GPX preserves per-observation RF gain/noise/GPS context");
   string file=Path.Combine(folder,"roads.geojson");File.WriteAllText(file,SyntheticRoads);var map=FoxOfflineDetail.Load(file);check(map.Features.Count==3&&map.Features[0].Name=="Synthetic Road"&&map.Version=="test","Local GeoJSON imports road/place/boundary names and provenance");
   check(map.Features.All(f=>f.Geometry.IsFrozen),"Offline geometries are frozen for safe background import");
   File.WriteAllText(file,SyntheticRoads.Replace("-100.02","999999"));bool bad=false;try{FoxOfflineDetail.Load(file);}catch(IOException){bad=true;}check(bad,"Projected/out-of-range map coordinates are rejected");
   File.WriteAllText(file,SyntheticRoads.Replace("\"features\":","\"crs\":{},\"features\":"));bad=false;try{FoxOfflineDetail.Load(file);}catch(IOException){bad=true;}check(bad,"Custom-CRS map files require explicit WGS84 conversion");
   File.WriteAllText(file,"{\"type\":\"FeatureCollection\",\"features\":[]}");bad=false;try{FoxOfflineDetail.Load(file);}catch(IOException){bad=true;}check(bad,"Empty local maps do not replace usable offline detail");
  }finally{foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);}
 }
 internal const string SyntheticRoads="""
 {"type":"FeatureCollection","mrs":{"name":"Synthetic local detail","source":"Synthetic test only","license":"Test fixture","version":"test"},"features":[{"type":"Feature","properties":{"FULLNAME":"Synthetic Road"},"geometry":{"type":"LineString","coordinates":[[-100.02,39.98],[-100,40],[-99.98,40.02]]}},{"type":"Feature","properties":{"name":"Test Town"},"geometry":{"type":"Point","coordinates":[-100,40]}},{"type":"Feature","properties":{"name":"Test boundary"},"geometry":{"type":"Polygon","coordinates":[[[-100.03,39.97],[-99.97,39.97],[-99.97,40.03],[-100.03,40.03],[-100.03,39.97]]]}}]}
 """;
}
