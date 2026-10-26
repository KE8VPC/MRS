using System.Windows;
namespace MRS;
public partial class MainWindow
{
    private void RenderAviationUpdateSmoke(Action<string> capture)
    {
        Stop();Aviation.SetAdsbForTest();ReceiverFamily.SelectedIndex=1;ReflectRate(2400000);HackRfHfFilter.IsChecked=true;
        CaptureOffsetEnabled.IsChecked=true;CaptureOffsetKhz.Text="750";HackRfLna.SelectedIndex=3;HackRfVga.SelectedIndex=12;
        FrequencyBox.Text="1090.000000";OpenDecoderSetup("adsb",1090000000);
        if(!setupReport.Text.Contains("HF hardware filter"))throw new InvalidOperationException("Setup checker did not explain the reported HF/ADS-B conflict");
        UpdateLayout();DecoderSetupExpander.BringIntoView();UpdateLayout();capture("ui-decoder-setup-conflict.png");
        ApplyDecoderRecommendation();
        if(SelectedSampleRate!=9600000||HackRfHfFilter.IsChecked==true||ReadCaptureOffset().Khz!=250||HackRfLna.SelectedIndex!=3||HackRfVga.SelectedIndex!=12||rfMode)throw new InvalidOperationException("Recommended ADS-B settings changed gain, started hardware or failed to repair capture coverage");
        if(setupReport.Text.Contains("•"))throw new InvalidOperationException("Applied ADS-B recommendation still fails checks: "+setupReport.Text);
        UpdateLayout();DecoderSetupExpander.BringIntoView();UpdateLayout();capture("ui-decoder-setup-applied.png");
        Aviation.ClearForTest();var message=AviationData.Parse(AviationTests.PositionJson,"adsb",AviationTests.Context)!;
        for(int plane=0;plane<3;plane++)for(int i=0;i<15;i++)
        {
            double lat=plane==0?40.4+i*.012:plane==1?40.82-i*.015:39.95+i*.014;
            double lon=plane==0?-84.8+i*.025:plane==1?-84-i*.022:-84.4-i*.012;
            Aviation.Observe(message with{Identity="ICAO:TEST"+plane,Callsign="MRS TEST "+(plane+1),Latitude=lat,Longitude=lon,Packet=message.Packet with{ObservedUtc=DateTimeOffset.UtcNow.AddSeconds(-28+i*2),PositionSeconds=i*2}});
        }
        Aviation.ShowMapForTest();Aviation.Refresh();Aviation.ShowTrailsForTest();WorkspaceTabs.SelectedItem=AviationTab;UpdateLayout();Aviation.FitMapForTest();UpdateLayout();
        if(Aviation.MapOnline||Aviation.MapNetworkRequests!=0||Aviation.MapTrailSegments<30)throw new InvalidOperationException("Offline map/trails check failed or attempted network access");
        capture("ui-aviation-offline-trails.png");
    }
}
