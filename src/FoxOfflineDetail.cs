using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;

internal sealed record FoxMapFeature(string Name,string Kind,StreamGeometry Geometry,Rect Bounds);
internal sealed class FoxOfflineDetail
{
 internal string Name{get;private set;}="";internal string Source{get;private set;}="Not supplied";internal string License{get;private set;}="Not supplied";internal string Version{get;private set;}="Not supplied";
 internal long Bytes{get;private set;}
 internal readonly List<FoxMapFeature> Features=[];
 internal Rect Bounds{get;private set;}=Rect.Empty;
 internal static FoxOfflineDetail Load(string path)
 {
  using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
  if(stream.Length>64*1024*1024)throw new IOException("Choose a regional GeoJSON file smaller than 64 MB");
  using var json=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=32});var root=json.RootElement;
  if(root.TryGetProperty("crs",out _))throw new IOException("Export the map as WGS84 longitude/latitude GeoJSON (RFC 7946), without a custom CRS");
  if(root.GetProperty("type").GetString()!="FeatureCollection")throw new IOException("Choose a GeoJSON FeatureCollection");
  var result=new FoxOfflineDetail{Name=Path.GetFileName(path),Bytes=stream.Length};
  string Text(JsonElement value,string key,string fallback=""){return value.ValueKind==JsonValueKind.Object&&value.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?(v.GetString()??fallback)[..Math.Min(240,v.GetString()?.Length??0)]:fallback;}
  if(root.TryGetProperty("mrs",out var meta)){result.Name=Text(meta,"name",result.Name);result.Source=Text(meta,"source",result.Source);result.License=Text(meta,"license",result.License);result.Version=Text(meta,"version",result.Version);}
  int count=0,total=0;
  Point Coordinate(JsonElement value){if(value.ValueKind!=JsonValueKind.Array||value.GetArrayLength()<2)throw new IOException("Invalid GeoJSON coordinate");double lon=value[0].GetDouble(),lat=value[1].GetDouble();if(!double.IsFinite(lat)||!double.IsFinite(lon)||Math.Abs(lat)>85||Math.Abs(lon)>180)throw new IOException("Map coordinates must be WGS84 longitude/latitude between 85° south and north");if(++total>500000)throw new IOException("Regional map exceeds 500,000 coordinates; split into smaller files");return AircraftMap.Project(lat,lon);}
  void Add(JsonElement coords,string name,string kind,bool close=false){var values=coords.EnumerateArray().Select(Coordinate).ToArray();if(values.Length<(close?3:2))return;for(int i=1;i<values.Length;i++)values[i].X=values[i-1].X+AircraftMap.Wrap(values[i].X-values[i-1].X+.5)-.5;var geo=new StreamGeometry();using(var dc=geo.Open()){dc.BeginFigure(values[0],false,close);dc.PolyLineTo(values.Skip(1).ToArray(),true,false);}geo.Freeze();result.Features.Add(new(name,kind,geo,geo.Bounds));result.Bounds=Rect.Union(result.Bounds,geo.Bounds);}
  foreach(var feature in root.GetProperty("features").EnumerateArray()){
   if(++count>50000)throw new IOException("Regional map exceeds 50,000 features; split into smaller files");
   if(!feature.TryGetProperty("geometry",out var geometry)||geometry.ValueKind==JsonValueKind.Null)continue;
   var properties=feature.TryGetProperty("properties",out var prop)?prop:default;string name=Text(properties,"name",Text(properties,"FULLNAME",Text(properties,"NAME"))),kind=Text(properties,"kind","road"),type=geometry.GetProperty("type").GetString()??"";
   var coords=geometry.GetProperty("coordinates");
   switch(type){
    case "Point":var p=Coordinate(coords);var dot=new StreamGeometry();using(var d=dot.Open()){d.BeginFigure(p,false,false);d.LineTo(new(p.X+1e-10,p.Y),true,false);}dot.Freeze();result.Features.Add(new(name,"place",dot,new Rect(p,new Size(0,0))));result.Bounds=Rect.Union(result.Bounds,new Rect(p,new Size(0,0)));break;
    case "LineString":Add(coords,name,kind);break;
    case "MultiLineString":foreach(var part in coords.EnumerateArray())Add(part,name,kind);break;
    case "Polygon":foreach(var ring in coords.EnumerateArray())Add(ring,name,"boundary",true);break;
    case "MultiPolygon":foreach(var polygon in coords.EnumerateArray())foreach(var ring in polygon.EnumerateArray())Add(ring,name,"boundary",true);break;
   }
  }
  if(result.Features.Count==0)throw new IOException("No supported road/line/boundary/place features were found");
  return result;
 }
 internal void Draw(Canvas canvas,double cx,double cy,int zoom)
 {
  double size=256*Math.Pow(2,zoom);var layer=new DetailLayer(this,cx,cy,size){Width=canvas.ActualWidth,Height=canvas.ActualHeight,IsHitTestVisible=false};canvas.Children.Add(layer);
  if(zoom<12)return;var boxes=new List<Rect>();
  foreach(var f in Features.Where(f=>f.Name.Length>0)){
   double x=canvas.ActualWidth/2+(AircraftMap.Wrap(f.Bounds.X+f.Bounds.Width/2-cx+.5)-.5)*size,y=canvas.ActualHeight/2+(f.Bounds.Y+f.Bounds.Height/2-cy)*size;var box=new Rect(x,y,Math.Min(180,f.Name.Length*6+8),16);
   if(x<0||y<0||box.Right>canvas.ActualWidth||box.Bottom>canvas.ActualHeight||boxes.Any(b=>b.IntersectsWith(box)))continue;
   boxes.Add(box);var label=new TextBlock{Text=f.Name,Foreground=Brushes.Gainsboro,Background=new SolidColorBrush(Color.FromArgb(180,10,20,32)),FontSize=11,IsHitTestVisible=false};Canvas.SetLeft(label,x);Canvas.SetTop(label,y);canvas.Children.Add(label);if(boxes.Count>=40)break;
  }
 }
 internal bool Covers(double cx,double cy)=>Features.Any(f=>{double x=f.Bounds.X+AircraftMap.Wrap(cx-f.Bounds.X+.5)-.5;return x>=f.Bounds.Left-.002&&x<=f.Bounds.Right+.002&&cy>=f.Bounds.Top-.002&&cy<=f.Bounds.Bottom+.002;});
 private sealed class DetailLayer(FoxOfflineDetail map,double cx,double cy,double size):FrameworkElement
 {
  protected override void OnRender(DrawingContext dc){dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));foreach(var f in map.Features){double offset=AircraftMap.Wrap(f.Bounds.X-cx+.5)-.5-f.Bounds.X;var transform=new Matrix(size,0,0,size,ActualWidth/2+offset*size,ActualHeight/2-cy*size);var bounds=new MatrixTransform(transform).TransformBounds(f.Bounds);if(!bounds.IntersectsWith(new Rect(0,0,ActualWidth,ActualHeight)))continue;dc.PushTransform(new MatrixTransform(transform));dc.DrawGeometry(null,new Pen(f.Kind=="boundary"?Brushes.DarkSlateGray:f.Kind=="water"?Brushes.SteelBlue:Brushes.SlateGray,1.5/size),f.Geometry);dc.Pop();}dc.Pop();}
 }
}
