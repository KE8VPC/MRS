using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
internal static class NifogTests
{
    internal static void Run(Action<bool,string> check)
    {
        var channels=NifogPlan.Channels;
        check(channels.Length==246&&NifogPlan.Markers.Length==236,"NIFOG reviewed tables contain 246 channel/plan rows and 236 distinct RF centers");
        check(channels.All(c=>c.MobileRxHz>0&&(c.MobileTxHz==null||c.MobileTxHz>0)&&c.PrintedPage is >=28 and <=64)&&NifogPlan.Markers.Select(c=>c.Frequency).SequenceEqual(NifogPlan.Markers.Select(c=>c.Frequency).Distinct().Order()),"NIFOG frequencies are positive, sorted and grouped with source-page provenance");
        var call=channels.Single(c=>c.Name=="VCALL10");
        check(call.MobileRxHz==155752500&&call.MobileTxHz==155752500&&call.RxCode=="156.7","NIFOG VCALL10 matches the published simplex frequency and CTCSS");
        var tac=channels.Single(c=>c.Name=="VTAC33");
        check(tac.MobileRxHz==159472500&&tac.MobileTxHz==151137500&&tac.RxCode=="156.7"&&tac.TxCode=="136.5","NIFOG VTAC33 preserves the asymmetric repeater pair and tones");
        var seven=channels.Single(c=>c.Name=="7CALL50");var eight=channels.Single(c=>c.Name=="8CALL90");
        check(seven.MobileRxHz==769243750&&seven.MobileTxHz==799243750&&seven.RxCode=="$F7E"&&seven.TxCode=="$293"&&seven.DetailsAt(seven.MobileRxHz).Contains("accepts any"),"NIFOG 700 MHz exact 6.25 kHz precision and wildcard RX NAC remain distinct from TX NAC");
        check(eight.MobileRxHz==851012500&&eight.MobileTxHz==806012500&&channels.Single(c=>c.Name=="8CALL90D").MobileTxHz==851012500,"NIFOG 800 MHz negative repeater split is not confused with direct operation");
        check(channels.Single(c=>c.Name=="LE 2").RxCode=="$68F"&&channels.Single(c=>c.Name=="NC 1").MobileTxHz==164712500&&channels.Single(c=>c.Name=="NC 2").MobileTxHz==419237500,"NIFOG federal VHF/UHF channel pairs and law-enforcement NAC match the tables");
        check(channels.Count(c=>c.Category=="UHF medical")==40&&channels.Single(c=>c.Name=="MED-82").MobileTxHz==468187500,"NIFOG medical channels include narrowband interstitial and direct entries");
        check(channels.Single(c=>c.Name=="7AG88D").MobileRxHz==774618750&&channels.Single(c=>c.Name=="7-US-03").MobileTxHz==804993750&&channels.Single(c=>c.Name=="DEPLOY E").MobileRxHz==774518750,"NIFOG air-ground, low-power and deployable control references preserve exact frequencies");
        check(channels.Where(c=>c.Category=="Weather").All(c=>c.MobileTxHz==null)&&channels.Count(c=>c.Category=="Weather")==7,"NIFOG weather references never manufacture transmit channels");
        check(channels.Single(c=>c.Name=="LFIRE2").LabelAt(39480000).Contains("proposed")&&channels.Single(c=>c.Name=="VTAC17").Note.Contains("Inland-area"),"NIFOG proposed and geographically limited entries keep source qualifications");
        var aliases=NifogPlan.Markers.Single(c=>c.Frequency==151137500);
        check(aliases.Source.Contains("VTAC11")&&aliases.Source.Contains("VTAC33")&&aliases.Source.Contains("Mobile TX")&&aliases.Source.Contains("VTAC36"),"NIFOG shared-frequency hover preserves simplex, repeater input and output aliases");
        check(!NifogPlan.Visible(151137501,151137502).Any()&&NifogPlan.Visible(151137500,151137500).Single().Frequency==151137500,"NIFOG markers use channel centers rather than invented allocation ranges");
        var restored=JsonSerializer.Deserialize<DisplaySettings>(JsonSerializer.Serialize(new DisplaySettings{ShowNifogPlan=true}));
        check(restored is {ShowNifogPlan:true,ShowBandPlan:false,ShowArrlPlan:false}&&restored.ColorsFrom(DisplaySettings.Theme("Light")).ShowNifogPlan,"NIFOG visibility persists independently of other layers and theme");
        string settingsPath=Path.Combine(AppContext.BaseDirectory,"nifog-test-profile",Guid.NewGuid().ToString("N"),"display.json");
        var panel=new DisplayPanel();panel.Initialize(settingsPath);DisplaySettings? changed=null;panel.Changed+=s=>changed=s;
        var body=(StackPanel)((ScrollViewer)panel.Content).Content;
        panel.SetReferencePlan("reference/nifog",true);panel.Save();
        check(changed is {ShowNifogPlan:true,ShowBandPlan:false,ShowArrlPlan:false},"NIFOG manager selection publishes its own display setting without enabling other layers");
        var second=new DisplayPanel();DisplaySettings? loaded=null;second.Changed+=s=>loaded=s;second.Initialize(settingsPath);
        check(loaded?.ShowNifogPlan==true,"NIFOG manager selection survives actual display-profile save and reload");
        var view=new SpectrumView();view.Measure(new Size(1000,500));view.Arrange(new Rect(0,0,1000,500));
        view.SetRf(155752500,960000);view.Configure(new DisplaySettings{ShowNifogPlan=true});double center=view.VisibleCenter;
        view.SetPointer(new Point(516,45));
        check(view.ToolTip?.ToString()?.Contains("VCALL10")==true&&view.ToolTip.ToString()!.Contains("p. 29")&&view.VisibleCenter==center,"NIFOG independent bar hover reports channel provenance without tuning");
        Capture(view,"ui-nifog-vhf.png");
        view.SetRf(159472500,960000);view.SetPointer(new Point(516,45));
        if(view.ToolTip is not ReferenceToolTip tooltip)throw new InvalidOperationException("NIFOG reference tooltip is missing.");
        var detail=(ScrollViewer)tooltip.Content;detail.Background=tooltip.Background;detail.ApplyTemplate();detail.Measure(new Size(640,380));detail.Arrange(new Rect(0,0,640,380));detail.UpdateLayout();
        check(detail.ScrollableHeight>0&&detail.ExtentWidth<=640,$"NIFOG shared-frequency details wrap and retain overflow in a bounded scroll view (overflow {detail.ScrollableHeight:0}, width {detail.ExtentWidth:0})");
        var detailImage=new RenderTargetBitmap(640,380,96,96,PixelFormats.Pbgra32);detailImage.Render(detail);
        var detailPng=new PngBitmapEncoder();detailPng.Frames.Add(BitmapFrame.Create(detailImage));using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-nifog-details.png")))detailPng.Save(file);
        tooltip.Scroll(-120);detail.UpdateLayout();check(detail.VerticalOffset>0,"NIFOG overflow details can scroll while the pointer stays over the marker");
        view.SetRf(799243750,960000);view.SetPointer(new Point(516,45));
        check(view.ToolTip?.ToString()?.Contains("repeater input")==true&&view.ToolTip.ToString()!.Contains("7CALL50"),"NIFOG marker appears at the mobile-transmit side of the pair");
        view.SetRf(769243750,960000);view.Configure(view.Settings with{BandPlanAtBottom=true});view.SetPointer(new Point(516,220));
        check(view.ToolTip?.ToString()?.Contains("7CALL50D")==true,"NIFOG bottom bar retains direct-channel aliases");
        Capture(view,"ui-nifog-700-bottom.png");
        view.Configure(view.Settings.ColorsFrom(DisplaySettings.Theme("Light")));view.SetRf(851512500,2400000);Capture(view,"ui-nifog-800-light.png");
        foreach(int height in new[]{140,180,239,240,289,290,400,500})foreach(bool bottom in new[]{false,true})
        {
            var compact=new SpectrumView();compact.SetRf(774000000,2400000);compact.Configure(new DisplaySettings{ShowNifogPlan=true,ShowArrlPlan=true,ShowBandPlan=true,BandPlanAtBottom=bottom});
            compact.Measure(new Size(600,height));compact.Arrange(new Rect(0,0,600,height));compact.UpdateLayout();
            new RenderTargetBitmap(600,height,96,96,PixelFormats.Pbgra32).Render(compact);
        }
        check(true,"All three reference layers render safely at compact sizes and both placements");
        view.Configure(new DisplaySettings());view.SetPointer(new Point(516,45));check(view.ToolTip==null,"Disabling NIFOG removes its reference tooltip");
        view.Configure(new DisplaySettings{ShowNifogPlan=true});view.SetAudio();view.SetPointer(new Point(516,45));check(view.ToolTip==null,"NIFOG RF channels stay absent from audio and MPX views");
    }
    private static void Capture(SpectrumView view,string file)
    {
        view.UpdateLayout();var bitmap=new RenderTargetBitmap(1000,500,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,file));png.Save(stream);
    }
}
