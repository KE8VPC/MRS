namespace MRS;

internal sealed record FoxTrend(string State,string Detail,double? Change=null);
internal static class FoxStrengthTrend
{
 internal static FoxTrend Evaluate(IReadOnlyList<FoxPoint> points,DateTimeOffset now)
 {
  if(points.Count==0)return new("Waiting","Collecting comparable signal measurements");
  var last=points[^1];if(now-last.Utc>TimeSpan.FromSeconds(12)||now<last.Utc)return new("Paused","No recent measurement; previous trend is stale");
  var recent=points.Where(p=>p.Scope==last.Scope&&p.Segment==last.Segment&&p.Fix.Source==last.Fix.Source&&p.Utc>=last.Utc.AddSeconds(-20)&&p.Utc<=last.Utc).ToArray();
  if(recent.Length<4||recent[^1].Utc-recent[0].Utc<TimeSpan.FromSeconds(8))return new("Collecting","Need at least eight seconds of recent comparable measurements");
  var older=recent.Where(p=>p.Utc<=last.Utc.AddSeconds(-8)).ToArray();var newer=recent.Where(p=>p.Utc>=last.Utc.AddSeconds(-5)).ToArray();
  if(older.Length<2||newer.Length<2)return new("Collecting","Waiting for recent and earlier signal windows");
  if(newer.Any(p=>p.FloorDb==null)||older.Any(p=>p.FloorDb==null))return new("Uncertain","Noise reference unavailable; strength trend withheld");
  if(newer.Concat(older).Any(p=>p.PeakDb>=-3)||FoxStatistics.Median(newer.Select(p=>p.MeanDb-p.FloorDb!.Value))<6||FoxStatistics.Median(older.Select(p=>p.MeanDb-p.FloorDb!.Value))<6)return new("Uncertain","Near noise floor or possible clipping; strength trend withheld");
  double delta=FoxStatistics.Median(newer.Select(p=>p.MeanDb))-FoxStatistics.Median(older.Select(p=>p.MeanDb));
  return new(delta>=2?"↑ Stronger":delta<=-2?"↓ Weaker":"→ Steady",$"{delta:+0.0;-0.0;0.0} dB across recent windows · strength change, not direction or distance",delta);
 }
}
internal static class FoxStatistics
{
 internal static double Median(IEnumerable<double> values){var a=values.Order().ToArray();return a.Length==0?double.NaN:(a[(a.Length-1)/2]+a[a.Length/2])/2;}
}
internal sealed record FoxEstimate(string State,string Detail,double? Latitude=null,double? Longitude=null,double RadiusMetres=0,int Groups=0);
internal static class FoxLocator
{
 private sealed record Sample(double X,double Y,double Db);
 private sealed record Candidate(double X,double Y,double Loss,double Score,int I,int J);
 internal static FoxEstimate Estimate(IReadOnlyList<FoxPoint> source,CancellationToken cancel=default)
 {
  if(source.Count<20)return new("Collecting","Need more spatially distinct measurements");
  if(source.Any(p=>p.Scope.Backend=="RTL"&&p.Scope.Gain==-1000||p.Scope!=source[0].Scope||p.Fix.Source!=source[0].Fix.Source))return new("Unavailable","Mixed receiver/gain/frequency or position sources; use one comparable session");
  var points=source.Where(p=>!p.Fix.Manual&&p.Fix.Fresh(p.Utc)&&p.Fix.Hdop is >0 and <=3&&p.Fix.Satellites>=4&&p.Readings>0&&p.FloorDb is double floor&&double.IsFinite(floor)&&double.IsFinite(p.MeanDb)&&double.IsFinite(p.PeakDb)&&p.MeanDb-floor>=6&&p.PeakDb< -3).ToArray();
  if(points.Length<20)return new("Collecting","Need usable RF above noise and known GPS quality; weak/unknown/clipped observations excluded");
  double lat=FoxStatistics.Median(points.Select(p=>p.Fix.Latitude)),lon=points[0].Fix.Longitude,cos=Math.Cos(lat*Math.PI/180);
  if(Math.Abs(lat)>80)return new("Unavailable","Polar routes are outside this local estimator's supported area");
  double East(double longitude)=>(((longitude-lon+540)%360)-180)*111195*cos;
  var all=points.Select(p=>new Sample(East(p.Fix.Longitude),(p.Fix.Latitude-lat)*111195,p.MeanDb)).ToArray();
  if(all.Any(p=>Math.Abs(p.X)>100000||Math.Abs(p.Y)>100000))return new("Unavailable","Route exceeds the local search area; use a shorter comparable session");
  var cells=all.GroupBy(p=>((int)Math.Floor(p.X/100),(int)Math.Floor(p.Y/100))).Select(g=>new Sample(FoxStatistics.Median(g.Select(p=>p.X)),FoxStatistics.Median(g.Select(p=>p.Y)),FoxStatistics.Median(g.Select(p=>p.Db)))).ToArray();
  if(cells.Length<20)return new("Not enough coverage","Need at least 20 distinct 100 m areas; stationary repeats do not add coverage",Groups:cells.Length);
  // Deterministic spatial thinning bounds work and avoids weighting stationary bursts.
  var samples=cells.OrderBy(p=>p.X).ThenBy(p=>p.Y).Where((_,i)=>i%Math.Max(1,(int)Math.Ceiling(cells.Length/180.0))==0).ToArray();
  double mx=samples.Average(p=>p.X),my=samples.Average(p=>p.Y),xx=samples.Average(p=>(p.X-mx)*(p.X-mx)),yy=samples.Average(p=>(p.Y-my)*(p.Y-my)),xy=samples.Average(p=>(p.X-mx)*(p.Y-my)),disc=Math.Sqrt((xx-yy)*(xx-yy)+4*xy*xy),major=(xx+yy+disc)/2,minor=(xx+yy-disc)/2;
  double span=Math.Max(samples.Max(p=>p.X)-samples.Min(p=>p.X),samples.Max(p=>p.Y)-samples.Min(p=>p.Y));
  if(span<700||minor/Math.Max(1,major)<.10)return new("Not enough coverage","Route is too small or mostly along one line; collect measurements from different sides",Groups:cells.Length);
  if(samples.Max(p=>p.Db)-samples.Min(p=>p.Db)<8)return new("Collecting","Insufficient signal variation to distinguish candidate areas",Groups:cells.Length);
  double left=samples.Min(p=>p.X)-span*.5,bottom=samples.Min(p=>p.Y)-span*.5,width=samples.Max(p=>p.X)-left+span*.5,height=samples.Max(p=>p.Y)-bottom+span*.5;
  const int grid=32;var candidates=new List<Candidate>();
  double Score(double x,double y,double loss,Sample[] data){var levels=data.Select(p=>p.Db+10*loss*Math.Log10(Math.Max(80,Math.Sqrt((p.X-x)*(p.X-x)+(p.Y-y)*(p.Y-y))))).ToArray();double center=FoxStatistics.Median(levels);return FoxStatistics.Median(levels.Select(v=>Math.Abs(v-center)));}
  for(int i=0;i<=grid;i++){cancel.ThrowIfCancellationRequested();for(int j=0;j<=grid;j++){double x=left+width*i/grid,y=bottom+height*j/grid;foreach(double loss in new[]{1.5,2,2.5,3,3.5,4})candidates.Add(new(x,y,loss,Score(x,y,loss,samples),i,j));}}
  var best=candidates.MinBy(p=>p.Score)!;if(best.Score>4)return new("Ambiguous","Measurements do not fit a stable single-source model well enough",Groups:cells.Length);
  var plausible=candidates.Where(p=>p.Score<=best.Score+Math.Max(1,best.Score*.4)).ToArray();
  if(plausible.Any(p=>p.I is 0 or grid||p.J is 0 or grid))return new("Ambiguous","Similar candidate areas reach the search boundary; no reliable bounded area",Groups:cells.Length);
  double radius=plausible.Max(p=>Math.Sqrt((p.X-best.X)*(p.X-best.X)+(p.Y-best.Y)*(p.Y-best.Y)))+Math.Sqrt(width*width+height*height)/grid;
  if(radius>span*.65)return new("Ambiguous","Several widely separated locations fit similarly; collect more route coverage",Groups:cells.Length);
  // Independent spatial halves must not point to incompatible locations.
  for(int half=0;half<2;half++){cancel.ThrowIfCancellationRequested();var subset=samples.Where((_,i)=>i%2==half).ToArray();var alternative=candidates.MinBy(p=>Score(p.X,p.Y,p.Loss,subset))!;if(Math.Sqrt(Math.Pow(alternative.X-best.X,2)+Math.Pow(alternative.Y-best.Y,2))>Math.Max(radius,span*.35))return new("Ambiguous","Estimated area changes when spatial subsets are compared",Groups:cells.Length);}
  return new("Rough estimate",$"{cells.Length} spatial groups · model spread ≈ {radius/1000:0.0} km; not a calibrated confidence radius. Assumes one stationary transmitter with stable output.",lat+best.Y/111195,((lon+best.X/(111195*cos)+540)%360)-180,radius,cells.Length);
 }
}
