using System.Windows;
using System.Windows.Controls;
namespace MRS;
public partial class MainWindow
{
    private void RenderFilePlanSmoke(Action<string> capture)
    {
        Stop();collectP25.IsChecked=true;
        DisplayControls.SetReferencePlan("reference/us",true);if(!Spectrum.Settings.ShowBandPlan)throw new InvalidOperationException("Reference selection did not reach spectrum");DisplayControls.SetReferencePlan("reference/us",false);

        var id=new P25Identity(0x345,0xBEE00,0x348,1,22);var band=new P25BandPlan(0,0,851006250,6250,-45000000,12500,1,null);var channel=new P25Channel(29,0,29,851187500,806187500,null,1,"Synthetic resolved");
        siteCollector!.Observe(FileBandPlanTests.Observation(id,band));siteCollector.Observe(FileBandPlanTests.Observation(id,channel:channel));siteCollector.Observe(FileBandPlanTests.Observation(id,channel:channel with{ChannelId=149,ChannelNumber=149,DownlinkHz=851937500,UplinkHz=806937500}));
        siteCollector.Observe(FileBandPlanTests.Observation(id) with{P25SiteAnnouncement=new(0x348,1,23,null,null,[new(channel,0)])});siteCollector.Flush(true);ReloadFilePlans();
        if(filePlans!.Error!=""||!filePlans.Plans.Contains("p25/BEE00-348/site-001-022"))throw new InvalidOperationException("Site CSV did not load into UI: "+filePlans.Error);
        SelectWorkspace(AnalysisTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"Bandplans")));UpdateLayout();capture("ui-csv-bandplans.png");
        WorkspaceTabs.SelectedIndex=0;Spectrum.SetRf(851500000,2400000);Spectrum.Configure(Spectrum.Settings with{Zoom=1,Pan=.5,ShowBandPlan=false,ShowArrlPlan=false,ShowNifogPlan=false,ShowTvPlan=false,ShowChannelMarkers=true,HidePlans=false,ExpandPlans=false});UpdateLayout();capture("ui-p25-site-overlay.png");
        var matching=filePlans.Visible(851187499,851187501,Spectrum.Settings).Where(m=>!m.Grid).ToArray();if(matching.Length!=2)throw new InvalidOperationException("Overlapping sites lost their membership");
        filePlans.Hidden.Add("p25/BEE00-348/site-001-023");Spectrum.InvalidateVisual();if(filePlans.Visible(851187499,851187501,Spectrum.Settings).Count()!=1)throw new InvalidOperationException("Site hide failed");filePlans.Hidden.Clear();
        // Simulated lifecycle state only: no hardware and no transmit worker is started.
        txIndicatorActive=true;RefreshRxTxIndicator();if(RxTxLabel.Text!="TX")throw new InvalidOperationException("TX indicator priority failed");UpdateLayout();capture("ui-indicator-tx-simulated.png");
        txIndicatorActive=false;RefreshRxTxIndicator();if(RxTxLabel.Text!="Idle")throw new InvalidOperationException("TX completion did not clear indicator");
        var railroad=bandList.Children.OfType<System.Windows.Controls.CheckBox>().Single(c=>Equals(c.Content,"reference/railroad"));railroad.IsChecked=false;ReloadFilePlans();
        if(bandList.Children.OfType<System.Windows.Controls.CheckBox>().Single(c=>Equals(c.Content,"reference/railroad")).IsChecked==true||filePlans.Visible(160200000,161600000,Spectrum.Settings).Any(m=>m.Plan=="reference/railroad"))throw new InvalidOperationException("Railroad hide did not survive CSV reload");
        bandList.Children.OfType<System.Windows.Controls.CheckBox>().Single(c=>Equals(c.Content,"reference/railroad")).IsChecked=true;
        collectP25.IsChecked=false;Spectrum.InvalidateVisual();
    }
}
