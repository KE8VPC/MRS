using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class PacketPanel
{
    internal void ConfigureWorkspace(string category)
    {
        PanelHeading.Visibility=Visibility.Collapsed;
        DecoderHelp.Text=category=="P25"?"Group repeated P25 messages into stable rows. Select an observation for fields and raw data. Export follows the filter.":category=="Paging"?"Group matching complete pages; label capcodes and manage ignore rules. Select a message for full payload and validation.":"Select an observation for full fields and raw data. Export follows the current filter. CW IDs are unverified candidates.";
        EnableP25.Visibility=VoiceCheck.Visibility=P25Status.Visibility=P25FilterButton.Visibility=GroupRepeated.Visibility=category=="P25"?Visibility.Visible:Visibility.Collapsed;
        EnablePaging.Visibility=PagingStatus.Visibility=GroupPaging.Visibility=PagingActionsButton.Visibility=ShowIgnored.Visibility=category=="Paging"?Visibility.Visible:Visibility.Collapsed;
        EnableDecoder.Visibility=CwCheck.Visibility=GroupDmr.Visibility=InputStatus.Visibility=category is "Data" or "Amateur"?Visibility.Visible:Visibility.Collapsed;
        EnableSubaudible.Visibility=SubaudibleStatus.Visibility=Visibility.Collapsed;
        VoiceIdStatus.Visibility=category=="Paging"?Visibility.Collapsed:Visibility.Visible;
        CwCheck.Visibility=Visibility.Collapsed;GroupDmr.Visibility=category=="Analysis"?Visibility.Visible:Visibility.Collapsed;
        if(category=="RadioData"){VoiceIdStatus.Visibility=Visibility.Collapsed;DecoderHelp.Text="Select an observation for decoded fields, source, confidence and raw data. Export follows the current filter.";}
        if(category=="Amateur")DecoderHelp.Text="AX.25 / APRS reception from SDR, WAV or radio audio. CW IDs and general observations are under Analysis.";
        if(category!="P25")Grouped=false;
        if(category!="Paging")PagingGrouped=false;
        DmrGrouped=category=="Analysis";
    }
    internal void MoveCwControls(Panel target){foreach(var control in new FrameworkElement[]{CwCheck,VoiceIdStatus}){if(control.Parent is Panel parent)parent.Children.Remove(control);control.Visibility=Visibility.Visible;control.Margin=new Thickness(0,3,12,3);target.Children.Add(control);}}
    internal void MoveP25Controls(Panel target)
    {
        foreach(var control in new FrameworkElement[]{EnableP25,VoiceCheck,VoiceIdStatus})
        {if(control.Parent is Panel parent)parent.Children.Remove(control);control.Margin=new Thickness(0,3,12,3);target.Children.Add(control);}
    }
}
public partial class MainWindow
{
    private void SelectWorkspace(TabItem item){item.IsSelected=true;DependencyObject? p=item;while(p!=null){if(p is TabItem t)t.IsSelected=true;p=LogicalTreeHelper.GetParent(p);}}
    private double voiceGainDb=12;
    private Slider voiceGainSlider=null!;
    private void InitializeWorkspaceLayout()
    {
        InitializeTuneSnap();Packets.ConfigureWorkspace("Amateur");AnalysisPackets.ConfigureWorkspace("Analysis");Packets.MoveCwControls(CwControls);PagingPackets.ConfigureWorkspace("Paging");P25Packets.ConfigureWorkspace("P25");P25Packets.MoveP25Controls(P25Controls);
        ToneToggle.Checked+=(_,_)=>Packets.SubaudibleEnabled=true;ToneToggle.Unchecked+=(_,_)=>Packets.SubaudibleEnabled=false;
        Packets.SubaudibleChanged+=v=>ToneToggle.IsChecked=v;
        var viewMode=new ComboBox{Width=125,SelectedIndex=Spectrum.Settings.ViewMode,ToolTip="Follow keeps tuning visible; Center lock centers tuning; Free view permits independent panning"};foreach(string mode in new[]{"Follow tuned","Center lock","Free view"})viewMode.Items.Add(mode);
        bool syncingView=false;viewMode.SelectionChanged+=(_,_)=>{if(!syncingView)Spectrum.Configure(Spectrum.Settings with{ViewMode=viewMode.SelectedIndex});};Spectrum.SettingsChanged+=s=>{syncingView=true;viewMode.SelectedIndex=s.ViewMode;syncingView=false;};RfNavigationControls.Children.Add(viewMode);
        var gain=voiceGainSlider=new Slider{Minimum=0,Maximum=30,Value=12,Width=110,TickFrequency=1,IsSnapToTickEnabled=true,VerticalAlignment=VerticalAlignment.Center,ToolTip="P25 voice gain only; output limited to prevent overflow"};
        var label=new TextBlock{Text="Voice +12 dB",Foreground=Brushes.LightSteelBlue,VerticalAlignment=VerticalAlignment.Center};
        gain.ValueChanged+=(_,_)=>{voiceGainDb=gain.Value;label.Text=$"Voice +{gain.Value:0} dB";};SliderWheel.Enable(gain,1);
        P25Controls.Children.Add(label);P25Controls.Children.Add(gain);
        var help=new Button{Content="?",ToolTip="Search M.R.S. Help (F1)"};
        help.Click+=(_,_)=>OpenHelp();InitializeHelp();
        // Context help is shared with F1.
        if(OtherReceiverButton.Parent is Panel navigation)navigation.Children.Add(help);
        receiver.ChannelChanged+=()=>Dispatcher.BeginInvoke(()=>P25Activity.ResetActivity());
        receiver.ReplayGap+=()=>Dispatcher.BeginInvoke(()=>P25Activity.ResetActivity());
        receiver.Ended+=_=>Dispatcher.BeginInvoke(()=>P25Activity.ResetActivity());
    }
}
internal static class VoiceAudioGain
{
    internal static byte[] Apply(byte[] pcm,double db)
    {
        double gain=Math.Pow(10,Math.Clamp(double.IsFinite(db)?db:0,0,30)/20);var output=new byte[pcm.Length];
        for(int i=0;i+1<pcm.Length;i+=2){double v=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i,2))*gain;
            // Smoothly compress peaks above 24k, with a ceiling below full scale.
            double a=Math.Abs(v);if(a>24000)a=24000+6000*(1-Math.Exp(-(a-24000)/6000));
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(i,2),(short)Math.CopySign(a,v));}
        return output;
    }
}
