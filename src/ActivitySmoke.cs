using System.IO;
using System.Windows;
namespace MRS;
public partial class MainWindow
{
    private async Task RenderActivitySmoke(Action<string> capture)
    {
        async Task WaitFor(Func<bool> predicate)
        {for(int i=0;i<300&&!predicate();i++)await Task.Delay(20);if(!predicate())throw new InvalidOperationException("Activity UI state timed out: "+activityMessage);}
        Stop();ReceiverFamily.SelectedIndex=1;ReflectRate(2400000);FmMode.SelectedIndex=0;
        var device=new RtlNative.Device(0,"SYNTHETIC scanner HackRF","HackRF","Test","ACTIVITY-TEST","HackRF");
        RtlDevices.ItemsSource=new[]{device};RtlDevices.SelectedItem=device;
        activeRate=2400000;activeGain=new HackRfGain(16,16).Token;rfMode=true;engine.StartExternal(2);SpectrumTitle.Text="RF SPECTRUM / WATERFALL · SYNTHETIC ACTIVITY TEST";
        receiver.Start(0,162400000,activeGain,false,fixture:true,sampleRate:activeRate,fixtureSeconds:30,backend:"HackRF",captureOffset:new(true,250));
        await WaitFor(()=>receiver.AppliedFrequency==162650000&&Spectrum.CaptureCenter==162650000);
        timer.Stop();
        try
        {
            Spectrum.Configure(new DisplaySettings{RfFftSize=16384,ShowBandPlan=true});Spectrum.ResetView();
            int worker=receiver.WorkerId;long fixedCenter=receiver.AppliedFrequency;double fixedView=Spectrum.VisibleCenter;
            double time=DisplaySettings.Clock;int frame=0;
            void Feed(bool signals)
            {
                var bins=signals?ActivityTests.Frame(16384,2400000,fixedCenter,frame++,(162400000,6000,-48),(163000000,6000,-55)):
                    ActivityTests.Frame(16384,2400000,fixedCenter,frame++);
                time+=.15;ProcessActivity(bins,time);Spectrum.PushAt(bins,time);
            }
            for(int i=0;i<14;i++)Feed(false);
            StartActivity();if(!activity.Running||!Spectrum.InteractionLocked)throw new InvalidOperationException("Scanner failed to arm the locked viewport.");
            for(int i=0;i<12;i++)Feed(true);
            await WaitFor(()=>Math.Abs((receiver.AppliedChannel?.Frequency??0)-162400000)<1000);
            if(receiver.AppliedFrequency!=fixedCenter||receiver.WorkerId!=worker||Spectrum.VisibleCenter!=fixedView||!receiver.Offset.Enabled)
                throw new InvalidOperationException("Activity selection moved HackRF capture, changed its offset or restarted the worker.");
            FlushActivity();
            var library=new FrequencyLibrary(Path.Combine(dataFolder,"frequencies.json"));
            if(library.Items.Count(i=>i.Activity!=null)!=2)throw new InvalidOperationException("Scanner did not persist both qualified signals.");
            Spectrum.Configure(Spectrum.Settings with{Zoom=4,Pan=.8});
            if(Spectrum.VisibleCenter!=fixedView||Spectrum.Settings.Zoom!=1)throw new InvalidOperationException("Scanner viewport moved while locked.");
            SidebarTabs.SelectedItem=ActivityTab;WorkspaceTabs.SelectedIndex=0;RefreshActivity();UpdateLayout();capture("ui-activity-scanner.png");
            activity.Skip();for(int i=0;i<8;i++)Feed(true);
            await WaitFor(()=>Math.Abs((receiver.AppliedChannel?.Frequency??0)-163000000)<1000);
            if(receiver.AppliedFrequency!=fixedCenter||receiver.WorkerId!=worker)throw new InvalidOperationException("Scanner Skip recentered the capture.");
            var scanBody=(System.Windows.Controls.StackPanel)((System.Windows.Controls.ScrollViewer)ActivityControls.Content).Content;
            var discover=scanBody.Children.OfType<System.Windows.Controls.CheckBox>().Single(c=>c.Content?.ToString()=="Discover only (keep listening frequency)");
            discover.IsChecked=true;
            if(activity.Running)throw new InvalidOperationException("Changing discovery mode must stop scanning before rearming.");
            long heldFrequency=receiver.AppliedChannel!.Frequency;
            StartActivity();for(int i=0;i<18;i++)Feed(true);FlushActivity();
            if(!activity.Running||receiver.AppliedChannel!.Frequency!=heldFrequency||receiver.AppliedFrequency!=fixedCenter||activity.Candidates.Count==0)
                throw new InvalidOperationException("Discovery-only mode failed to detect with the listening and capture frequencies fixed.");
            discover.IsChecked=false;
            RfSquelchSlider.Value=-64;
            if(ActivityControls.Options.AutoSquelch||receiver.SquelchThreshold!=-64)throw new InvalidOperationException("Manual RF slider did not override automatic squelch.");
            await LiveTune(162400000);
            if(activity.Running||Spectrum.InteractionLocked)throw new InvalidOperationException("Manual tuning did not stop the scanner.");
            await WaitFor(()=>receiver.AppliedChannel?.Frequency==162400000);
            ResetActivity("Test complete.");
            if(activityCapture!=null)throw new InvalidOperationException("Noise estimator retained stale capture settings.");
        }
        finally{Stop();ActivityControls.DisableAuto();timer.Start();ReceiverFamily.SelectedIndex=0;}
    }
}
