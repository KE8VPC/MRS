using System.Windows;
namespace MRS;

internal static class BandModeDefaults
{
    internal static ChannelSettings Apply(ChannelSettings channel)
    {
        long hz=channel.Frequency;
        string? mode=hz>=88000000&&hz<108000000?"WFM":
            hz>=535000&&hz<1705000||hz>=118000000&&hz<137000000?"AM":null;
        if(mode==null && UsBandPlan.Channels.Any(c=>Math.Abs(c.Frequency-hz)<=2500 &&
            (c.Name.StartsWith("NOAA")||c.Name.StartsWith("FRS/")||c.Name.StartsWith("GMRS")||c.Name.StartsWith("MURS")||
            c.Source==UsBandPlan.MarineSource&&!c.Name.Contains("DSC")&&!c.Name.Contains("AIS"))))mode="NFM";
        return mode==null||mode==channel.Mode?channel:channel with { Mode=mode, Bandwidth=ChannelSettings.DefaultWidth(mode) };
    }
}

public partial class MainWindow
{
    private bool reflectingReceiverAuto;
    private void ApplyBandMode(long hz)
    {
        if(AutoBandMode.IsChecked!=true)return;
        var channel=BandModeDefaults.Apply(SelectedChannel(hz));
        bool prior=updatingControls; updatingControls=true;
        try { FmMode.SelectedIndex=Array.IndexOf(ReceiveDemodulator.Modes,channel.Mode); PopulateBandwidth(channel.Width); }
        finally { updatingControls=prior; }
    }
    private void InitializeReceiverRefinements()
    {
        void Pitch() { receiver.CwPitchHz=CwPitch.Value; CwPitchLabel.Text=$"CW pitch: {CwPitch.Value:0} Hz"; }
        CwPitch.ValueChanged+=(_,_)=>Pitch(); SliderWheel.Enable(CwPitch,10,false); Pitch();
        void Reflect(ActivityOptions options)
        {
            reflectingReceiverAuto=true;
            try { ReceiverAutoSquelch.IsChecked=options.AutoSquelch; ReceiverAutoMargin.Value=options.MarginDb; ReceiverAutoLines.IsChecked=options.ShowLines; ReceiverAutoMarginLabel.Text=$"Above noise floor: {options.MarginDb:0.#} dB"; }
            finally { reflectingReceiverAuto=false; }
        }
        void Update()
        {
            if(reflectingReceiverAuto)return;
            ActivityControls.SetReceiverAuto(ReceiverAutoSquelch.IsChecked==true,ReceiverAutoMargin.Value,ReceiverAutoLines.IsChecked==true);
            if(ReceiverAutoSquelch.IsChecked==true)RfSquelchEnabled.IsChecked=true;
        }
        Reflect(ActivityControls.Options);
        ActivityControls.Changed+=Reflect;
        ReceiverAutoSquelch.Checked+=(_,_)=>Update(); ReceiverAutoSquelch.Unchecked+=(_,_)=>Update();
        ReceiverAutoMargin.ValueChanged+=(_,_)=>Update(); SliderWheel.Enable(ReceiverAutoMargin,.5);
        ReceiverAutoLines.Checked+=(_,_)=>Update(); ReceiverAutoLines.Unchecked+=(_,_)=>Update();
        ReceiverRecalibrate.Click+=(_,_)=>ResetActivity("Recalibrating receiver noise floor.");
    }
}
