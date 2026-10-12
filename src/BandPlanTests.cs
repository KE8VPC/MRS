using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
internal static class BandPlanTests
{
    internal static void Run(Action<bool,string> check)
    {
        NifogTests.Run(check);
        TvBandPlanTests.Run(check);
        check(UsBandPlan.VisibleBands(144000000,148000000).Single().Name=="Amateur 2 m", "Band reference identifies the full Region 2 two-meter band");
        check(!UsBandPlan.VisibleBands(148000000,149000000).Any(), "Band reference excludes the upper boundary from adjacent coverage");
        check(UsBandPlan.Channels.Count(x=>x.Name.StartsWith("CB "))==40 && UsBandPlan.Channels.Single(x=>x.Name=="CB 23").Frequency==27255000 && UsBandPlan.Channels.Single(x=>x.Name=="CB 24").Frequency==27235000, "CB numbering preserves irregular channel 23/24 order");
        check(!UsBandPlan.Channels.Any(x=>x.Frequency==26995000),"CB reference does not invent a channel in the radio-control gap");
        check(UsBandPlan.Channels.Count(x=>x.Name.StartsWith("FRS/GMRS"))==22 && UsBandPlan.Channels.Count(x=>x.Name.StartsWith("GMRS input"))==8,"Reference distinguishes 22 shared channels from eight GMRS repeater inputs");
        check(UsBandPlan.Channels.Count(x=>x.Name.StartsWith("NOAA"))==7 && UsBandPlan.Channels.Count(x=>x.Name.StartsWith("MURS"))==5,"Weather and MURS channel counts match source tables");
        check(UsBandPlan.Channels.Count(x=>x.Name.StartsWith("AM "))==117 && UsBandPlan.VisibleBands(1000000,1000001).Any(x=>x.Name=="AM broadcast"),"AM reference includes 117 carrier centers and broadcast outline");
        check(UsBandPlan.VisibleBands(117999999,118000000).Single().Name.Contains("navigation") && UsBandPlan.VisibleBands(118000000,118000001).Single().Name.Contains("communications"),"Aviation navigation and communication references meet without overlap");
        check(UsBandPlan.Channels.Any(x=>x.Frequency==156525000 && x.Name.Contains("no voice")) && UsBandPlan.Channels.Any(x=>x.Frequency==162025000 && x.Name.Contains("AIS 2")),"Marine references distinguish DSC and AIS data");
        check(UsBandPlan.Channels.Any(x=>x.Frequency==157000000 && x.Name.Contains("ship TX")) && UsBandPlan.Channels.Any(x=>x.Frequency==161600000 && x.Name.Contains("shore TX")),"Marine duplex channels retain ship and shore direction");
        var saved=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(new DisplaySettings {ShowBandPlan=true}));
        check(saved?.ShowBandPlan==true && saved.ColorsFrom(DisplaySettings.Theme("Light")).ShowBandPlan,"Band reference preference persists and survives theme changes");
        var view=new SpectrumView(); view.Measure(new Size(1000,500));view.Arrange(new Rect(0,0,1000,500));
        view.Configure(new DisplaySettings {ShowBandPlan=true});view.SetRf(146000000,6000000);
        double before=view.VisibleCenter; view.SetPointer(new Point(500,25));
        check(view.ToolTip?.ToString()?.Contains("97.301")==true && view.VisibleCenter==before,"Band hover shows source without tuning");
        Capture(view,"ui-band-plan-amateur.png");
        view.SetRf(1000000,1400000);Capture(view,"ui-band-plan-am-broadcast.png");
        view.SetRf(121500000,2400000);Capture(view,"ui-band-plan-aviation.png");
        view.SetRf(156800000,1000000);Capture(view,"ui-band-plan-marine.png");
        view.SetRf(27205000,960000); view.Configure(view.Settings with {Zoom=4,Pan=.5});
        Capture(view,"ui-band-plan-cb.png");
        view.Configure(view.Settings.ColorsFrom(DisplaySettings.Theme("Light")));Capture(view,"ui-band-plan-light.png");
        check(ArrlPlan.Entries.Select(x=>x.Band).Distinct().Count()==28 && ArrlPlan.Entries.All(x=>x.Low>0 && x.High>=x.Low),"ARRL covers 28 band groups with valid endpoints");
        check(ArrlPlan.Visible(135700,137800).Any(x=>x.Band=="2200 m") && !ArrlPlan.Visible(137800,140000).Any(),"ARRL LF boundaries use Hz precision and exclude adjacent spectrum");
        check(ArrlPlan.Entries.Any(x=>x.Low==7290000 && x.IsPoint) && ArrlPlan.Entries.Any(x=>x.Low==14230000 && x.IsPoint),"HF calling markers retain exact frequencies");
        check(ArrlPlan.Entries.Count(x=>x.Band=="60 m" && x.IsPoint)==4 && ArrlPlan.Entries.Any(x=>x.Low==5351500 && x.High==5366500),"60 m distinguishes four dial markers from the RF segment");
        check(!ArrlPlan.Visible(2310000000L,2390000000L).Any(),"13 cm does not bridge the allocation gap");
        check(!ArrlPlan.Visible(3450000000L,3500000000L).Any(),"9 cm omits discontinued upper coverage");
        check(ArrlPlan.Visible(443000000,443000001).Count(x=>!x.IsPoint)==2,"70 cm preserves overlapping ATV and repeater recommendations");
        check(ArrlPlan.Visible(241000000000L,250000000000L).Any(x=>x.Band=="1 mm"),"Microwave outlines retain 64-bit frequency precision");
        var options=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(new DisplaySettings {ShowArrlPlan=true,ShowChannelMarkers=false}));
        check(options?.ShowArrlPlan==true && !options.ShowChannelMarkers && !options.ShowBandPlan && options.ColorsFrom(DisplaySettings.Theme("Light")).ShowArrlPlan,"ARRL and channel settings persist independently through theme changes");
        view.Configure(new DisplaySettings {ShowArrlPlan=true});view.SetRf(14175000,400000);
        Capture(view,"ui-arrl-hf.png");
        view.SetRf(443000000,8000000);view.SetPointer(new Point(516,25));
        check(view.ToolTip?.ToString()?.Contains("ATV")==true && view.ToolTip.ToString()!.Contains("Repeater") && !view.ToolTip.ToString()!.Contains("97.301"),"ARRL-only hover includes overlapping recommendations without FCC layer");
        Capture(view,"ui-arrl-overlap.png");
        view.Configure(view.Settings.ColorsFrom(DisplaySettings.Theme("Light")));view.SetRf(10368000000L,100000000);
        Capture(view,"ui-arrl-microwave-light.png");
        view.Configure(new DisplaySettings {ShowArrlPlan=true,ShowBandPlan=true,BandPlanAtBottom=true});
        view.SetRf(146000000,6000000);view.SetPointer(new Point(516,200));
        check(view.ToolTip?.ToString()?.Contains("ARRL")==true,"Bottom overlay hover uses relocated bar bounds");
        long tuned=0;view.TuneRequested += hz=>tuned=hz;
        view.ClickAt(new Point(516,50));check(tuned==146000000,"Bottom placement preserves spectrum click frequency");
        tuned=0;view.ClickAt(new Point(516,300));check(tuned==146000000,"Bottom placement preserves waterfall click frequency");
        tuned=0;view.ClickAt(new Point(516,115));check(tuned==0,"Frequency-axis gap does not accidentally tune");
        var bottomSaved=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(view.Settings));
        check(bottomSaved?.BandPlanAtBottom==true && bottomSaved.ColorsFrom(DisplaySettings.Theme("Light")).BandPlanAtBottom,"Overlay placement persists independently of theme");
        Capture(view,"ui-band-plan-bottom.png");
        view.SetPointer(new Point(516,50));check(view.ToolTip==null,"Spectrum hover clears bottom reference tooltip");
        foreach(var frequency in new long[]{774000000,90700000})
        foreach(var height in new[]{140,150,180,220,240,300})
        foreach(var bottom in new[]{false,true})
        {
            var compact=new SpectrumView();
            compact.SetRf(frequency,2400000);compact.Configure(new DisplaySettings {ShowBandPlan=true,ShowArrlPlan=true,BandPlanAtBottom=bottom});
            compact.Measure(new Size(600,height));compact.Arrange(new Rect(0,0,600,height));compact.UpdateLayout();
            compact.SetPointer(new Point(300,80));
            var render=new RenderTargetBitmap(600,height,96,96,PixelFormats.Pbgra32);render.Render(compact);
        }
        check(true,"Both reported RF frequencies render with combined overlays at compact heights and either placement");
        view.SetAudio();view.SetPointer(new Point(500,25));
        check(view.ToolTip==null,"RF band reference stays absent from audio-frequency views");
    }
    private static void Capture(SpectrumView view,string name)
    {
        view.UpdateLayout();
        var bitmap=new RenderTargetBitmap(1000,500,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(file);
    }
}

