using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;

internal sealed record OfflinePlace(string Name,double Lat,double Lon,int Rank,int Population);
internal sealed record OfflineShape(int Kind,string Name,StreamGeometry Geometry,Rect Bounds);
internal sealed class OfflineBasemap
{
    internal readonly List<OfflineShape> Shapes=[];
    internal readonly List<OfflinePlace> Places=[];
    internal string Error="";
    internal static readonly Lazy<OfflineBasemap> Shared=new(()=>new());
    private OfflineBasemap()
    {
        try
        {
            using var resource=typeof(OfflineBasemap).Assembly.GetManifestResourceStream("MRS.OfflineBasemap")??throw new IOException("Bundled map resource is missing");
            using var zip=new GZipStream(resource,CompressionMode.Decompress);using var json=JsonDocument.Parse(zip);
            foreach(var shape in json.RootElement.GetProperty("shapes").EnumerateArray())
            {
                var points=new List<Point>();double previous=0;
                foreach(var v in shape.GetProperty("ring").EnumerateArray())
                {
                    var p=AircraftMap.Project(v[1].GetDouble(),v[0].GetDouble());
                    if(points.Count>0)p.X=previous+AircraftMap.Wrap(p.X-previous+.5)-.5;
                    points.Add(p);previous=p.X;
                }
                if(points.Count<3)continue;
                var geometry=new StreamGeometry();using(var context=geometry.Open()){context.BeginFigure(points[0],true,true);context.PolyLineTo(points.Skip(1).ToArray(),true,false);}geometry.Freeze();
                Shapes.Add(new(shape.GetProperty("kind").GetInt32(),shape.GetProperty("name").GetString()??"",geometry,geometry.Bounds));
            }
            foreach(var place in json.RootElement.GetProperty("places").EnumerateArray())Places.Add(new(place.GetProperty("name").GetString()??"",place.GetProperty("lat").GetDouble(),place.GetProperty("lon").GetDouble(),place.GetProperty("rank").GetInt32(),place.GetProperty("population").GetInt32()));
            Places.Sort((a,b)=>b.Population.CompareTo(a.Population));
        }
        catch(Exception ex){Error="Offline basemap unavailable: "+ex.Message;}
    }
    internal void Draw(Canvas canvas,double cx,double cy,int zoom)
    {
        double w=canvas.ActualWidth,h=canvas.ActualHeight,size=256*Math.Pow(2,zoom);
        var layer=new MapLayer(this,cx,cy,size){Width=w,Height=h,IsHitTestVisible=false};canvas.Children.Add(layer);
        var labels=new List<Rect>();int count=0;
        foreach(var p in Places)
        {
            if(p.Rank>Math.Clamp(zoom+1,1,10))continue;
            var v=AircraftMap.Project(p.Lat,p.Lon);double x=w/2+(AircraftMap.Wrap(v.X-cx+.5)-.5)*size,y=h/2+(v.Y-cy)*size;
            var box=new Rect(x+4,y-8,Math.Clamp(p.Name.Length*6,18,160),15);
            if(x<0||x>w-20||y<10||y>h-20||labels.Any(b=>b.IntersectsWith(box)))continue;
            labels.Add(box);var label=new TextBlock{Text="· "+p.Name,Foreground=Brushes.LightSteelBlue,FontSize=11,IsHitTestVisible=false};Canvas.SetLeft(label,x);Canvas.SetTop(label,y-8);canvas.Children.Add(label);if(++count>=65)break;
        }
        if(zoom is >=4 and <=9)
        foreach(var s in Shapes.Where(s=>s.Kind==1))
        {
            double x=w/2+(AircraftMap.Wrap(s.Bounds.X+s.Bounds.Width/2-cx+.5)-.5)*size,y=h/2+(s.Bounds.Y+s.Bounds.Height/2-cy)*size;
            if(x<10||x>w-80||y<10||y>h-20)continue;
            var box=new Rect(x,y,100,15);if(labels.Any(b=>b.IntersectsWith(box)))continue;labels.Add(box);
            var label=new TextBlock{Text=s.Name,Foreground=new SolidColorBrush(Color.FromRgb(100,139,161)),FontSize=11,IsHitTestVisible=false};Canvas.SetLeft(label,x);Canvas.SetTop(label,y);canvas.Children.Add(label);
        }
    }
    private sealed class MapLayer(OfflineBasemap data,double cx,double cy,double size):FrameworkElement
    {
        private static readonly Brush Land=new SolidColorBrush(Color.FromRgb(26,45,53)),Water=new SolidColorBrush(Color.FromRgb(10,26,43)),Border=new SolidColorBrush(Color.FromRgb(81,115,131));
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Water,null,new Rect(0,0,ActualWidth,ActualHeight));dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));
            foreach(var s in data.Shapes.OrderBy(s=>s.Kind==1?2:s.Kind==2?1:0))
            {
                double shift=Math.Round(cx-(s.Bounds.X+s.Bounds.Width/2));
                double x=(s.Bounds.X+shift-cx)*size+ActualWidth/2,y=(s.Bounds.Y-cy)*size+ActualHeight/2;
                if(x>ActualWidth||x+s.Bounds.Width*size<0||y>ActualHeight||y+s.Bounds.Height*size<0)continue;
                dc.PushTransform(new MatrixTransform(size,0,0,size,(shift-cx)*size+ActualWidth/2,-cy*size+ActualHeight/2));
                dc.DrawGeometry(s.Kind==1?null:s.Kind==2?Water:Land,new Pen(Border,(s.Kind==1?.55:.7)/size),s.Geometry);dc.Pop();
            }
            dc.Pop();
        }
    }
}
