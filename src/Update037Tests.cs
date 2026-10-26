using System.IO;
using System.Windows;
using NAudio.Wave;
namespace MRS;
internal static class Update037Tests
{
    internal static byte[] Tone(int channels,double amplitude=.4){var b=new byte[1920*channels];for(int n=0;n<960;n++)for(int c=0;c<channels;c++){short v=(short)(Math.Sin(2*Math.PI*(c==0?1000:1500)*n/48000)*32767*amplitude);System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan((n*channels+c)*2,2),v);}return b;}
    internal static void Run(Action<bool,string> check)
    {
        check(TuneSnap.Apply(461637480,1,6250,[],0)==461637500,"Step snapping retains exact 6.25 kHz channel centers");
        check(TuneSnap.Apply(461637480,0,12500,[],0)==461637480,"Snap off preserves exact 1 Hz click resolution");
        check(TuneSnap.Apply(88114000,2,200000,[88100000],20000)==88100000,"Bandplan snapping honors FM channel offsets");
        check(TuneSnap.Apply(88175000,2,12500,[88100000],1000)==88175000,"A distant bandplan channel cannot capture a click");
        var view=new SpectrumView();view.Measure(new Size(872,470));view.Arrange(new Rect(0,0,872,470));view.SetRf(461637500,48000);view.Configure(new DisplaySettings{SnapMode=1,SnapStepHz=6250});var tuned=new List<long>();view.TuneRequested+=tuned.Add;view.ClickAt(new Point(451,90));view.ClickAt(new Point(451,350));check(tuned.Count==2&&tuned[0]==tuned[1]&&tuned[0]%6250==0,"Spectrum and waterfall clicks share snap selection");
        view.ClickAt(new Point(451,90),true);check(tuned.Count==2,"Ctrl marker selection bypasses click tuning");
        var stereo=Tone(2);check(VoxMp3Recorder.ConvertPcm(stereo,2,false).Length==stereo.Length/2&&VoxMp3Recorder.ConvertPcm(stereo,2,true).SequenceEqual(stereo),"VOX conversion supports mono mixdown and stereo preservation");
        using(var engine=new AudioEngine()){byte[]? recorded=null;engine.Volume=0;engine.Monitor=false;engine.RecordingAudio+=(p,c)=>recorded=p;engine.StartExternal(2);engine.FeedStereo(stereo,true);check(recorded!=null&&VoxMp3Recorder.Level(recorded)>-20,"Recording tap is independent of muted speakers and volume");engine.FeedStereo(stereo,false);check(recorded!.All(b=>b==0),"Recording tap respects the RF audio gate");}
        Task.Run(async()=>{
            string root=Path.Combine(AppContext.BaseDirectory,"vox-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            using(var r=new VoxMp3Recorder()){
                r.Arm(root,"TEST",new(-30,.2,.2,2,64));var silent=new byte[1920];var tone=Tone(1);
                for(int i=0;i<10;i++)r.Feed(silent,silent.Length,1,"first",146520000);
                for(int i=0;i<15;i++)r.Feed(tone,tone.Length,1,"first",146520000);
                for(int i=0;i<15;i++)r.Feed(silent,silent.Length,1,"first",146520000);
                await r.FinishAsync().WaitAsync(TimeSpan.FromSeconds(30));check(r.Clips==1&&!r.Armed&&!r.Status.Contains("failed"),"VOX threshold/pre-roll/tail produce one finalized MP3");
                string file=Directory.GetFiles(root,"*.mp3").Single();using(var reader=new MediaFoundationReader(file)){check(reader.TotalTime.TotalSeconds>=.6&&reader.TotalTime.TotalSeconds<=1.0,"Encoded MP3 duration includes pre-trigger and quiet tail");var decoded=new byte[reader.WaveFormat.AverageBytesPerSecond];int n=reader.Read(decoded,0,decoded.Length);check(n>0&&decoded.Take(n).Any(b=>b!=0),"MP3 plays through an independent Windows decoder");}
                check(!Directory.GetFiles(root,"*.wav").Any(),"Successful MP3 encoding removes only its temporary WAV");
            }
            string split=Path.Combine(root,"stereo");using(var r=new VoxMp3Recorder()){r.Arm(split,"TEST",new(-30,0,.2,1,64,true));for(int i=0;i<70;i++){r.Feed(stereo,stereo.Length,2,"stereo",102100000);if(i%10==0)await r.DrainAsync();}r.Boundary();await r.FinishAsync().WaitAsync(TimeSpan.FromSeconds(30));check(r.Clips>=2,"Continuous VOX segments at the maximum clip duration");foreach(string f in Directory.GetFiles(split,"*.mp3")){using var reader=new MediaFoundationReader(f);check(reader.WaveFormat.Channels==2&&reader.TotalTime.TotalSeconds<=1.2,"Stereo MP3 remains stereo and bounded in length");}}
            string boundaries=Path.Combine(root,"boundary");using(var r=new VoxMp3Recorder()){r.Arm(boundaries,"TEST",new(-30,0,.2,10,64));for(int i=0;i<10;i++)r.Feed(Tone(1),1920,1,"channel-A",1000000);r.Boundary();for(int i=0;i<10;i++)r.Feed(Tone(1),1920,1,"channel-B",2000000);await r.FinishAsync().WaitAsync(TimeSpan.FromSeconds(30));check(r.Clips==2,"Source boundaries keep automatic audio clips separate");}
        }).GetAwaiter().GetResult();
    }
}
public partial class MainWindow
{
    private async Task RenderUpdate037Smoke(Action<string> capture)
    {
        Stop();automaticFormatBox.SelectedIndex=1;LoadDemo(this,new RoutedEventArgs());await Task.Delay(100);automaticIqArm.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));if(!automaticAudio.Armed)throw new InvalidOperationException("MP3 VOX did not arm");await Task.Delay(900);automaticAudio.Disarm();await automaticAudio.FinishAsync().WaitAsync(TimeSpan.FromSeconds(30));RefreshTriggeredRecording();if(automaticAudio.Clips==0)throw new InvalidOperationException("UI audio did not reach MP3 recording");SidebarTabs.SelectedItem=RecordingTab;SelectWorkspace(SpectrumTab);automaticIqStatus.BringIntoView();UpdateLayout();capture("ui-mp3-vox.png");Stop();automaticFormatBox.SelectedIndex=0;
        Spectrum.SetRf(461637500,48000);Spectrum.Configure(Spectrum.Settings with{SnapMode=1,SnapStepHz=6250});if(((System.Windows.Controls.ComboBoxItem)NavigationStepSize.SelectedItem).Tag.ToString()!="6250")throw new InvalidOperationException("Snap step is not reflected by navigation");SelectWorkspace(SpectrumTab);UpdateLayout();capture("ui-tune-snap.png");Spectrum.Configure(Spectrum.Settings with{SnapMode=0,SnapStepHz=12500});SelectWorkspace(AviationTab);UpdateLayout();capture("ui-aviation-centered-frequency.png");
    }
}
