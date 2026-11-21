using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MRS;

public partial class MainWindow
{
    internal async Task RenderReceiveModesSmoke()
    {
        rfMode=false;
        var amateur=(TabControl)AmateurTab.Content;
        var sstv=amateur.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"SSTV"));
        var rtty=amateur.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"RTTY"));
        IEnumerable<T> Controls<T>(DependencyObject parent) where T:DependencyObject
        {
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
            {
                var child=VisualTreeHelper.GetChild(parent,i);
                if(child is T control)yield return control;
                foreach(var descendant in Controls<T>(child))yield return descendant;
            }
        }
        async Task Capture(TabItem tab,string name)
        {
            SelectWorkspace(tab);UpdateLayout();await Task.Delay(100);
            var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);
            bitmap.Render(this);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(file);
        }
        SelectWorkspace(sstv);UpdateLayout();
        Controls<Button>((DependencyObject)sstv.Content).Single(b=>Equals(b.Content,"Start receiving"))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(sstvDecoder==null)throw new IOException("SSTV Start did not create a decoder.");
        var context=new PacketContext("receive-mode-smoke","Synthetic audio",true,null,0);
        FeedAdditionalAudio(new byte[1920],1,context,true);
        if(sstvAudioContext!=context)throw new IOException("SSTV did not retain its PCM source.");
        // Synthetic pixels are for UI sizing only. Separate audio tests verify
        // the real demodulator and independent WAV recordings.
        lock(receiveModeSync)
        {
            sstvInfo=new("Martin M1",DateTimeOffset.UtcNow,context.Source,null,true,0,320,64,false,0,0,0);
            for(int y=0;y<64;y++)
            {
                var pixels=new byte[320*3];
                for(int x=0;x<320;x++){pixels[x*3]=(byte)(x*255/319);pixels[x*3+1]=(byte)(y*4);pixels[x*3+2]=(byte)(255-x*255/319);}
                sstvLines.Add(pixels);
            }
            sstvRevision++;
        }
        TickSstv();await Capture(sstv,"ui-sstv.png");
        ResetAdditionalReceive(true);
        if(sstvDecoder!=null||sstvLines.Count!=64)throw new IOException("SSTV source boundary lost the partial image.");
        SelectWorkspace(rtty);UpdateLayout();
        Controls<Button>((DependencyObject)rtty.Content).Single(b=>Equals(b.Content,"Start decoding"))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(rttyReceiver==null)throw new IOException("RTTY Start did not create a decoder.");
        rttyPending.Append("RYRY TEST 123\r\nSynthetic UI sample");
        TickRtty();await Capture(rtty,"ui-rtty.png");
        ResetAdditionalReceive(true);
        if(rttyReceiver!=null||!rttyTranscript.Text.Contains("RYRY TEST 123"))
            throw new IOException("RTTY source boundary lost the transcript.");
        var weather=WorkspaceTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"Weather"));
        var weatherTabs=(TabControl)weather.Content;
        await Capture(weatherTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"WEFAX")),"ui-weather-wefax.png");
        var noaa=weatherTabs.Items.OfType<TabItem>().Single(t=>Equals(t.Header,"NOAA Weather Radio"));
        await Capture(noaa,"ui-weather-noaa.png");
        if(Controls<ComboBox>((DependencyObject)noaa.Content)
            .Single(c=>c.Items.Count==NoaaChannels.Length&&Equals(c.Items[0],PlanCsv.MHz(NoaaChannels[0])+" MHz"))
            .SelectedIndex!=0)
            throw new IOException("NOAA Weather Radio has no default channel.");
        if(amateur.Items.OfType<TabItem>().Any(t=>Equals(t.Header,"WEFAX")))
            throw new IOException("WEFAX remains in Amateur.");
    }
}
