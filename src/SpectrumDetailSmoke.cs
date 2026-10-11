using System.Windows;
namespace MRS;
public partial class MainWindow
{
    private async Task RenderSpectrumDetailSmoke(Action<string> capture)
    {
        Stop();WorkspaceTabs.SelectedIndex=0;SidebarTabs.SelectedIndex=2;
        SpectrumTitle.Text="SYNTHETIC RF DETAIL TEST";
        Spectrum.SetRf(774400000,9600000,false);
        Spectrum.Configure(new DisplaySettings{Zoom=32,Pan=.5,HistorySeconds=5,RfFftSize=65536,SpectrumTop=0,WaterLow=-100,WaterHigh=-15});
        var analyzer=new RfSpectrumAnalyzer();analyzer.Append(RfSpectrumTests.Tones(65536,(-160,.3),(-154,.2),(90,.4)),65536);
        var data=analyzer.Snapshot()!;double now=DisplaySettings.Clock;
        for(int n=0;n<100;n++)Spectrum.PushAt(data,now-5+n*.05);
        UpdateLayout();await Task.Delay(60);capture("ui-rf-high-detail.png");
        if(Spectrum.BinCount!=65536)throw new InvalidOperationException("High-detail spectrum did not retain its FFT bins.");
    }
}
