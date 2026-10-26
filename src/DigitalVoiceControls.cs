using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
    private readonly DigitalVoicePipeline digitalVoice=new();
    private readonly ComboBox digitalMode=new(){Width=190,ItemsSource=new[]{"Digital voice off","DMR / MOTOTRBO","NXDN48 / NEXEDGE 6.25k","NXDN96 / NEXEDGE 12.5k"},SelectedIndex=0};
    private readonly TextBlock digitalStatus=new(){Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap};
    private readonly TextBox digitalDiagnostics=new(){IsReadOnly=true,Height=150,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=new SolidColorBrush(Color.FromRgb(16,23,34)),Foreground=Brushes.LightSteelBlue};
    private double digitalGainDb=6;
    private void InitializeDigitalVoice()
    {
        var row=new WrapPanel();var slots=new ComboBox{Width=110,ItemsSource=new[]{"Both slots","Slot 1","Slot 2"},SelectedIndex=0};var inverse=new CheckBox{Content="Invert polarity",Foreground=Brushes.White};var cw=new CheckBox{Content="CW station ID",Foreground=Brushes.White};
        var gain=new Slider{Minimum=0,Maximum=24,Value=6,Width=100,SmallChange=1,TickFrequency=1,IsSnapToTickEnabled=true};SliderWheel.Enable(gain,1);var gainLabel=new TextBlock{Text="Voice +6 dB",Foreground=Brushes.LightSteelBlue};
        gain.ValueChanged+=(_,_)=>{digitalGainDb=gain.Value;gainLabel.Text=$"Voice +{gain.Value:0} dB";};
        foreach(var control in new FrameworkElement[]{digitalMode,slots,inverse,cw,gainLabel,gain}){control.Margin=new Thickness(0,3,10,3);row.Children.Add(control);}DigitalVoiceControls.Children.Add(row);DigitalVoiceControls.Children.Add(digitalStatus);
        DigitalVoiceControls.Children.Add(new TextBlock{Text="NFM: DMR / NXDN96 typically 12.5–16 kHz filter; NXDN48 6–8 kHz. Current channel only. Encrypted voice is muted by the backend. CW candidates require verification.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});
        var diagnosticsPanel=new DockPanel();var clearDiagnostics=new Button{Content="Clear diagnostics view"};DockPanel.SetDock(clearDiagnostics,Dock.Top);diagnosticsPanel.Children.Add(clearDiagnostics);diagnosticsPanel.Children.Add(digitalDiagnostics);clearDiagnostics.Click+=(_,_)=>{digitalVoice.ClearDiagnostics();digitalDiagnostics.Clear();};DigitalVoiceControls.Children.Add(new Expander{Header="Decoder diagnostics / received identifiers (unverified)",Foreground=Brushes.LightSteelBlue,Content=diagnosticsPanel});
        void Configure(){if(digitalMode.SelectedIndex>0&&trunkRadio.Running)StopTrunk(false);if(digitalMode.SelectedIndex>0)P25Packets.VoiceToggle.IsChecked=false;digitalVoice.Configure(digitalMode.SelectedIndex,slots.SelectedIndex==0?3:slots.SelectedIndex,inverse.IsChecked==true);slots.IsEnabled=digitalMode.SelectedIndex==1;UpdateDataCapture();engine.RetuneBoundary();}
        digitalMode.SelectionChanged+=(_,_)=>Configure();slots.SelectionChanged+=(_,_)=>Configure();inverse.Checked+=(_,_)=>Configure();inverse.Unchecked+=(_,_)=>Configure();
        P25Packets.VoiceToggle.Checked+=(_,_)=>digitalMode.SelectedIndex=0;
        cw.Checked+=(_,_)=>Packets.CwToggle.IsChecked=true;cw.Unchecked+=(_,_)=>Packets.CwToggle.IsChecked=false;
        Packets.CwToggle.Checked+=(_,_)=>cw.IsChecked=true;Packets.CwToggle.Unchecked+=(_,_)=>cw.IsChecked=false;
        InitializeNxdnRows();
        digitalVoice.Audio+=pcm=>engine.FeedStereo(VoiceAudioGain.Apply(pcm,digitalGainDb),true);
        void Reset(){digitalVoice.Reset();engine.RetuneBoundary();}
        receiver.ChannelChanged+=Reset;receiver.ReplayGap+=Reset;receiver.Ended+=_=>Reset();
    }
    private void RefreshDigitalVoice(){digitalStatus.Text=digitalVoice.Status;var text=digitalVoice.Diagnostics;if(digitalDiagnostics.Text!=text){digitalDiagnostics.Text=text;digitalDiagnostics.ScrollToEnd();}}
}
