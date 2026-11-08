using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
    private readonly ComboBox setupProfile=new(){ItemsSource=DecoderSetup.Profiles,DisplayMemberPath="Name",SelectedIndex=0};
    private readonly TextBlock setupReport=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,6,0,0)};
    private readonly TextBlock setupResult=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Aquamarine};
    private long? requestedSetupTarget;
    private void InitializeDecoderSetup()
    {
        DecoderSetupHost.Children.Add(setupProfile);
        var apply=new Button{Content="Apply recommended settings",ToolTip="Stops reception and finalizes recordings before applying the profile"};DecoderSetupHost.Children.Add(apply);
        setupReport.Visibility=Visibility.Collapsed;DecoderSetupHost.Children.Add(setupReport);DecoderSetupHost.Children.Add(setupResult);
        setupProfile.SelectionChanged+=(_,_)=>{requestedSetupTarget=null;setupResult.Text="";RefreshDecoderSetup();};
        apply.Click+=(_,_)=>ApplyDecoderRecommendation();RefreshDecoderSetup();
    }
    private void OpenDecoderSetup(string key,long target)
    {
        setupProfile.SelectedItem=DecoderSetup.Profiles.Single(p=>p.Key==key);requestedSetupTarget=target;
        SidebarTabs.SelectedIndex=0;DecoderSetupExpander.IsExpanded=true;DecoderSetupExpander.BringIntoView();RefreshDecoderSetup();
    }
    private long SetupTarget(DecoderProfile p)=>p.FixedHz??requestedSetupTarget??(FrequencyLibrary.TryFrequency(FrequencyBox.Text,out long hz)?hz:0);
    private void RefreshDecoderSetup()
    {
        if(setupProfile.SelectedItem is not DecoderProfile p||!ready)return;
        try
        {
            long target=SetupTarget(p);var recommendation=DecoderSetup.Recommend(p,SelectedBackend);
            bool applied=rfMode&&receiver.AppliedFrequency>0;
            int rate=applied?receiver.AppliedRate:SelectedSampleRate,gain=applied?receiver.AppliedGain:SelectedGain;
            var channel=applied?receiver.AppliedChannel!:SelectedChannel(target);
            long center=applied?receiver.AppliedFrequency:target+(SelectedHackRf?ReadCaptureOffset().Hz:0);
            double width=applied?receiver.AppliedBandwidth:SelectedHackRf?HackRfProtocol.CaptureBandwidth(rate,gain):rate;
            var issues=DecoderSetup.Check(p,target,new(center,rate,width,IqSampleFormat.Unsigned8),applied?receiver.Backend:SelectedBackend,gain,channel,applied?receiver.SamplingMode:SelectedSamplingMode);
            if(target<=0)issues=[..issues,"Enter the desired station frequency first."];
            if(p.Key=="atsc"&&!SelectedHackRf)issues=[..issues,"The available RTL-SDR sample rates cannot capture a 6 MHz TV channel. Select HackRF for ATSC."];
            setupReport.Text=(p.Key=="paging"?"POCSAG 512 may benefit from a 6 kHz filter; wider paging signals may need a wider filter.\n":"")+(applied?(receiver.IsReplay?"Current replay (settings cannot repair an existing recording)":"Current receiver"):"Selected settings (receiver stopped)")+"\n"+(issues.Length==0?"Capture/settings checks passed. Signal quality and decoder support still determine results.":string.Join("\n",issues.Select(i=>"• "+i)))+$"\nRecommendation: {recommendation.Rate/1e6:0.###} MS/s · {recommendation.Mode} / {recommendation.Filter/1000:0.###} kHz · offset {(SelectedHackRf?recommendation.OffsetKhz:0):+0;-0;0} kHz"+(recommendation.DisableHf?" · HF hardware filter off":"")+".\nApplying stops reception, finalizes recordings and disables automatic modulation changes. It preserves gain and does not start a decoder. Start / Apply the receiver afterward.";
        }
        catch(Exception ex){setupReport.Text="Setup check: "+ex.Message;}
    }
    private void ApplyDecoderRecommendation()
    {
        if(setupProfile.SelectedItem is not DecoderProfile p)return;
        try
        {
            if(receiver.IsReplay){setupResult.Text="Stop replay first. Receiver settings cannot widen a recorded capture.";return;}
            if(p.Key=="atsc"&&!SelectedHackRf){setupResult.Text="Select HackRF before applying the ATSC profile.";return;}
            long target=SetupTarget(p);if(target<=0||!(p.Key=="cw"?SelectedCanTune(target):(SelectedHackRf?ReceiverCapabilities.HackRf:ReceiverCapabilities.Rtl).CanTune(target))){setupResult.Text="Enter a frequency supported by the selected receiver.";return;}
            var recommendation=DecoderSetup.Recommend(p,SelectedBackend);Stop();updatingControls=true;
            try
            {
                if(p.Key!="cw")SamplingModeBox.SelectedIndex=0;AutoBandMode.IsChecked=false;ReflectRate(recommendation.Rate);
                if(SelectedHackRf){if(recommendation.DisableHf)HackRfHfFilter.IsChecked=false;CaptureOffsetEnabled.IsChecked=recommendation.OffsetKhz!=0;CaptureOffsetKhz.Text=recommendation.OffsetKhz.ToString(CultureInfo.InvariantCulture);}
                FmMode.SelectedIndex=Array.IndexOf(ReceiveDemodulator.Modes,recommendation.Mode);PopulateBandwidth(recommendation.Filter);FrequencyBox.Text=(target/1e6).ToString("0.000000",CultureInfo.InvariantCulture);
            }
            finally{updatingControls=false;}
            RefreshSamplingControls();setupResult.Text="Recommended settings applied. Click Start / Apply receiver, then start the decoder.";RefreshDecoderSetup();
        }
        catch(Exception ex){setupResult.Text="Could not apply setup: "+ex.Message;}
    }
}
