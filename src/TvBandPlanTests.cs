using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
internal static class TvBandPlanTests
{
    internal static void Run(Action<bool,string> check)
    {
        var channels=TvBandPlan.Channels;
        check(channels.Length==35&&channels.Select(c=>c.Number).SequenceEqual(Enumerable.Range(2,35))&&channels.All(c=>c.High-c.Low==6000000),"TV overlay contains physical RF channels 2–36 with 6 MHz widths");
        check(channels.First().Low==54000000&&channels.Last().High==608000000&&channels.Single(c=>c.Number==5).Low==76000000&&channels.Single(c=>c.Number==7).Low==174000000&&channels.Single(c=>c.Number==14).Low==470000000,"TV allocation endpoints include the VHF band gaps and current UHF limit");
        check(!TvBandPlan.Visible(72000000,76000000).Any()&&!TvBandPlan.Visible(88000000,174000000).Any()&&!TvBandPlan.Visible(216000000,470000000).Any()&&!TvBandPlan.Visible(608000000,614000000).Any(),"TV overlay excludes allocation gaps and reserved RF 37");
        var rf15=TvBandPlan.Visible(476000000,482000000).Single();
        check(rf15.Number==15&&rf15.Center==479000000&&Math.Abs(rf15.Pilot-476309440.55944056)<.01,"RF 15 bounds, center and nominal ATSC 1.0 pilot agree with channel geometry");
        check(TvBandPlan.Visible(482000000,482000001).Single().Number==16&&!rf15.Details(false).Contains("nominal pilot")&&rf15.Details(true).Contains("reference only"),"TV boundaries are half-open and pilot references are optional, not detection");
        string path=Path.Combine(Path.GetTempPath(),"mrs-tv-overlay-"+Guid.NewGuid().ToString("N"),"display.json");
        try
        {
            var panel=new DisplayPanel();panel.Initialize(path);
            var body=(StackPanel)((ScrollViewer)panel.Content).Content;
            var toggles=body.Children.OfType<System.Windows.Controls.HeaderedContentControl>().Select(g=>(StackPanel)g.Content).SelectMany(g=>g.Children.OfType<CheckBox>());
            panel.SetReferencePlan("reference/tv",true);
            check(!toggles.Any(c=>c.Content?.ToString()?.Contains("pilot references")==true),"TV pilot control is removed");
            panel.Save();DisplaySettings? loaded=null;
            var restored=new DisplayPanel();restored.Changed+=s=>loaded=s;restored.Initialize(path);
            check(loaded is {ShowTvPlan:true,ShowTvPilots:false,ShowBandPlan:false,ShowArrlPlan:false,ShowNifogPlan:false}&&!loaded.ColorsFrom(DisplaySettings.Theme("Light")).ShowTvPilots,"TV channel overlay persists without retired pilot references");
        }
        finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path)!,true);}
        var view=new SpectrumView();view.Measure(new Size(1000,500));view.Arrange(new Rect(0,0,1000,500));
        view.SetRf(479000000,19200000);view.Configure(new DisplaySettings{SnapMode=0,ShowTvPlan=true,ShowTvPilots=true});
        view.SetPointer(new Point(516,25));
        check(view.ToolTip?.ToString()?.Contains("TV RF 15")==true&&!view.ToolTip.ToString()!.Contains("476.309441")&&view.ToolTip.ToString()!.Contains("73.603"),"TV hover identifies RF 15 and provenance without retired pilot reference");
        long tuned=0;view.TuneRequested+=hz=>tuned=hz;
        view.ClickAt(new Point(516,25));check(tuned==479000000,"TV reference-bar clicks retain existing spectrum frequency mapping");
        Capture(view,"ui-tv-band-plan.png");
        view.Configure(view.Settings with{BandPlanAtBottom=true});view.SetPointer(new Point(516,220));
        check(view.ToolTip?.ToString()?.Contains("TV RF 15")==true,"TV hover follows the bottom reference-bar placement");
        view.Configure(view.Settings.ColorsFrom(DisplaySettings.Theme("Light")));Capture(view,"ui-tv-band-plan-light-bottom.png");
        foreach(int height in new[]{140,239,240,289,290,333,334,400,500})foreach(bool bottom in new[]{false,true})
        {
            var compact=new SpectrumView();compact.SetRf(479000000,19200000);
            compact.Configure(new DisplaySettings{SnapMode=0,ShowTvPlan=true,ShowTvPilots=true,ShowBandPlan=true,ShowArrlPlan=true,ShowNifogPlan=true,BandPlanAtBottom=bottom});
            compact.Measure(new Size(600,height));compact.Arrange(new Rect(0,0,600,height));compact.UpdateLayout();
            new RenderTargetBitmap(600,height,96,96,PixelFormats.Pbgra32).Render(compact);
        }
        check(true,"All four reference layers render safely at compact heights and either placement");
        view.Configure(new DisplaySettings{SnapMode=0,ShowTvPilots=true});view.SetPointer(new Point(516,25));check(view.ToolTip==null,"Pilot option alone does not enable a hidden TV layer");
        view.Configure(new DisplaySettings{SnapMode=0,ShowTvPlan=true,ShowTvPilots=true});view.SetBaseband(192000,"MPX");view.SetPointer(new Point(516,25));check(view.ToolTip==null,"TV RF channel references remain absent from MPX audio views");
    }
    private static void Capture(SpectrumView view,string file)
    {
        view.UpdateLayout();var bitmap=new RenderTargetBitmap(1000,500,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,file));png.Save(stream);
    }
}