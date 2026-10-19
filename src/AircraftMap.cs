using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace MRS;
// All requests are optional, viewport-only and independent of capture/decoder threads.
internal sealed class AircraftMap : UserControl, IDisposable
{
    private readonly Canvas canvas=new(){Background=new SolidColorBrush(Color.FromRgb(10,20,32)),ClipToBounds=true};
    private readonly CheckBox online=new(){Content="Online street map",Foreground=Brushes.White};
    private readonly CheckBox follow=new(){Content="Follow selected",Foreground=Brushes.White};
    private readonly TextBlock info=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};
    private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(12)};
    private readonly CancellationTokenSource cancel=new();
    private readonly Dictionary<string,BitmapSource> tiles=new();
    private readonly HashSet<string> pending=[];
    private readonly Dictionary<string,DateTime> retry=new();
    private readonly SemaphoreSlim network=new(2);
    private readonly string cache=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MRS","MapTiles");
    private AircraftRow[] points=[];private AircraftRow? selected;
    private double cx=.266,cy=.376;private int zoom=7;private bool fitted,disposed;private Point? drag;
    internal static Point Project(double lat,double lon)=>new((lon+180)/360,(1-Math.Asinh(Math.Tan(Math.Clamp(lat,-85.05112878,85.05112878)*Math.PI/180))/Math.PI)/2);
    internal static double Wrap(double x)=>x-Math.Floor(x);
    internal AircraftMap()
    {
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MRS-RadioWorkbench/0.22.0 (Windows; KE8VPC)");
        var root=new DockPanel();var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var controls=new WrapPanel();header.Children.Add(controls);
        void Button(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(8,2,8,2)};b.Click+=(_,_)=>action();controls.Children.Add(b);}
        Button("−",()=>Zoom(-1,new Point(canvas.ActualWidth/2,canvas.ActualHeight/2)));Button("+",()=>Zoom(1,new Point(canvas.ActualWidth/2,canvas.ActualHeight/2)));Button("Fit aircraft",Fit);Button("Map attribution",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.openstreetmap.org/copyright"){UseShellExecute=true}));
        controls.Children.Add(online);controls.Children.Add(follow);header.Children.Add(info);root.Children.Add(canvas);Content=root;
        online.ToolTip="Downloads visible map tiles from OpenStreetMap. The provider receives your IP and viewed map area, not aircraft messages. No bulk download.";
        online.Checked+=(_,_)=>Draw();online.Unchecked+=(_,_)=>Draw();follow.Checked+=(_,_)=>Draw();
        canvas.SizeChanged+=(_,_)=>Draw();canvas.MouseWheel+=(_,e)=>{Zoom(e.Delta>0?1:-1,e.GetPosition(canvas));e.Handled=true;};
        canvas.MouseLeftButtonDown+=(_,e)=>{drag=e.GetPosition(canvas);follow.IsChecked=false;canvas.CaptureMouse();};
        canvas.MouseMove+=(_,e)=>{if(drag is {} p){var n=e.GetPosition(canvas);double size=256*Math.Pow(2,zoom);cx=Wrap(cx-(n.X-p.X)/size);cy=Math.Clamp(cy-(n.Y-p.Y)/size,0,1);drag=n;Draw();}};
        canvas.MouseLeftButtonUp+=(_,_)=>{drag=null;canvas.ReleaseMouseCapture();};canvas.LostMouseCapture+=(_,_)=>drag=null;
    }
    internal void Update(AircraftRow[] rows,AircraftRow? selection){points=rows;selected=selection;if(!fitted&&points.Length>0){Fit();fitted=true;}else Draw();}
    private void Zoom(int delta,Point anchor){int next=Math.Clamp(zoom+delta,2,17);double old=256*Math.Pow(2,zoom),size=256*Math.Pow(2,next);cx=Wrap(cx+(anchor.X-canvas.ActualWidth/2)*(1/old-1/size));cy=Math.Clamp(cy+(anchor.Y-canvas.ActualHeight/2)*(1/old-1/size),0,1);zoom=next;follow.IsChecked=false;Draw();}
    private void Fit()
    {
        if(points.Length==0){Draw();return;}var p=points.Select(r=>Project(r.Latitude!.Value,r.Longitude!.Value)).ToArray();double origin=p[0].X;
        var xs=p.Select(v=>origin+Wrap(v.X-origin+.5)-.5).ToArray();cx=Wrap((xs.Min()+xs.Max())/2);cy=(p.Min(v=>v.Y)+p.Max(v=>v.Y))/2;
        double width=Math.Max(100,canvas.ActualWidth-80),height=Math.Max(100,canvas.ActualHeight-80);
        zoom=Math.Clamp((int)Math.Floor(Math.Log2(Math.Min(width/(256*Math.Max(.0005,xs.Max()-xs.Min())),height/(256*Math.Max(.0005,p.Max(v=>v.Y)-p.Min(v=>v.Y)))))),2,12);follow.IsChecked=false;Draw();
    }
    private void Draw()
    {
        if(disposed||canvas.ActualWidth<1||canvas.ActualHeight<1)return;
        if(follow.IsChecked==true&&selected!=null&&points.Contains(selected)){var c=Project(selected.Latitude!.Value,selected.Longitude!.Value);cx=c.X;cy=c.Y;}
        canvas.Children.Clear();double w=canvas.ActualWidth,h=canvas.ActualHeight,size=256*Math.Pow(2,zoom),left=cx*size-w/2,top=cy*size-h/2;int count=1<<zoom,missing=0;
        if(online.IsChecked==true)
        {
            for(int y=(int)Math.Floor(top/256);y<=(int)Math.Floor((top+h)/256);y++)for(int x=(int)Math.Floor(left/256);x<=(int)Math.Floor((left+w)/256);x++)
            {
                if(y<0||y>=count)continue;int tx=((x%count)+count)%count;string key=$"{zoom}/{tx}/{y}";
                if(tiles.TryGetValue(key,out var image)){var tile=new Image{Source=image,Width=256,Height=256};Canvas.SetLeft(tile,x*256-left);Canvas.SetTop(tile,y*256-top);canvas.Children.Add(tile);}
                else{missing++;if(pending.Count<24&&!pending.Contains(key)&&(!retry.TryGetValue(key,out var at)||DateTime.UtcNow>=at)){pending.Add(key);_ = LoadTile(key);}}
            }
        }
        else for(int n=1;n<4;n++){canvas.Children.Add(new Line{X1=w*n/4,X2=w*n/4,Y2=h,Stroke=Brushes.DarkSlateGray});canvas.Children.Add(new Line{Y1=h*n/4,Y2=h*n/4,X2=w,Stroke=Brushes.DarkSlateGray});}
        if(online.IsChecked!=true){for(int n=0;n<4;n++){double x=w*n/4,y=h*n/4;var lon=new TextBlock{Text=$"{(Wrap((left+x)/size)*360-180):0.00}°",Foreground=Brushes.LightSteelBlue,FontSize=10};Canvas.SetLeft(lon,x+2);Canvas.SetTop(lon,h-16);canvas.Children.Add(lon);var lat=new TextBlock{Text=$"{Math.Atan(Math.Sinh(Math.PI*(1-2*(top+y)/size)))*180/Math.PI:0.00}°",Foreground=Brushes.LightSteelBlue,FontSize=10};Canvas.SetTop(lat,y+2);canvas.Children.Add(lat);}}
        foreach(var row in points){var p=Project(row.Latitude!.Value,row.Longitude!.Value);double x=w/2+(Wrap(p.X-cx+.5)-.5)*size,y=h/2+(p.Y-cy)*size;if(x<0||x>w||y<0||y>h)continue;
            var dot=new Ellipse{Width=9,Height=9,Fill=row.Replay?Brushes.Orange:Brushes.Cyan,Stroke=Brushes.Black,StrokeThickness=1,ToolTip=$"{row.Identity}\n{row.Position}\n{row.Source}"};Canvas.SetLeft(dot,x-4);Canvas.SetTop(dot,y-4);canvas.Children.Add(dot);
            var label=new TextBlock{Text=string.IsNullOrEmpty(row.Callsign)?row.Identity:row.Callsign,Foreground=Brushes.White,Background=Brushes.Black,FontSize=11};Canvas.SetLeft(label,x+7);Canvas.SetTop(label,y-8);canvas.Children.Add(label);
        }
        info.Text=$"Zoom {zoom} · wheel: zoom · drag: pan · "+(online.IsChecked==true?(missing>0?"Map tiles loading/unavailable · ":"")+"© OpenStreetMap contributors · openstreetmap.org/copyright":"Offline coordinate view")+(points.Length==0?" · No fresh positions":"");
    }
    private async Task LoadTile(string key)
    {
        try
        {
            await network.WaitAsync(cancel.Token);byte[] data;
            try
            {
                if(disposed||online.IsChecked!=true)return;
                string path=System.IO.Path.Combine(cache,key.Replace('/','_')+".png");
                DateTime expires=File.Exists(path)?File.GetLastWriteTimeUtc(path).AddDays(7):DateTime.MinValue;
                if(File.Exists(path+".expires")&&DateTime.TryParse(File.ReadAllText(path+".expires"),null,System.Globalization.DateTimeStyles.RoundtripKind,out var parsed))expires=parsed;
                if(File.Exists(path)&&DateTime.UtcNow<expires)data=await File.ReadAllBytesAsync(path,cancel.Token);
                else
                {
                    using var response=await http.GetAsync("https://tile.openstreetmap.org/"+key+".png",HttpCompletionOption.ResponseHeadersRead,cancel.Token);response.EnsureSuccessStatusCode();
                    if(response.Content.Headers.ContentLength>1024*1024)throw new IOException("Map tile exceeds limit");
                    using var stream=await response.Content.ReadAsStreamAsync(cancel.Token);using var buffer=new MemoryStream();byte[] chunk=new byte[8192];int n;
                    while((n=await stream.ReadAsync(chunk,cancel.Token))>0){if(buffer.Length+n>1024*1024)throw new IOException("Map tile exceeds limit");buffer.Write(chunk,0,n);}data=buffer.ToArray();
                    Directory.CreateDirectory(cache);await File.WriteAllBytesAsync(path,data,cancel.Token);
                    expires=response.Headers.CacheControl?.MaxAge is {} age?DateTime.UtcNow+age:response.Content.Headers.Expires?.UtcDateTime??DateTime.UtcNow.AddDays(7);
                    await File.WriteAllTextAsync(path+".expires",expires.ToString("O"),cancel.Token);
                    // Bounded cache; only already-viewed tiles are stored, never prefetched.
                    var files=new DirectoryInfo(cache).GetFiles("*.png");if(files.Length>2048)foreach(var old in files.OrderBy(f=>f.LastWriteTimeUtc).Take(files.Length-2048))try{old.Delete();File.Delete(old.FullName+".expires");}catch(IOException){}
                }
                using var bytes=new MemoryStream(data);var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=bytes;bitmap.EndInit();bitmap.Freeze();
                if(tiles.Count>256)tiles.Clear();tiles[key]=bitmap;
            }
            finally{network.Release();}
        }
        catch(OperationCanceledException){}catch(Exception){if(retry.Count>=256)retry.Clear();retry[key]=DateTime.UtcNow.AddMinutes(5);}
        finally{pending.Remove(key);if(!disposed)Draw();}
    }
    public void Dispose(){disposed=true;cancel.Cancel();http.Dispose();}
}
