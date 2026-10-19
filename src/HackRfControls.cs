using System.Windows;
using System.Windows.Controls;
namespace MRS;
public partial class MainWindow
{
    private Dictionary<string, CaptureOffset> savedCaptureOffsets = new();
    private CaptureOffset ReadCaptureOffset()
    {
        if (!SelectedHackRf) return new(false);
        if (!int.TryParse(CaptureOffsetKhz.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int khz) || khz < -5000 || khz > 5000)
            throw new System.IO.IOException("Enter an offset from -5000 to +5000 kHz.");
        return new(CaptureOffsetEnabled.IsChecked == true, khz);
    }
    private void RestoreCaptureOffset()
    {
        string? key = RtlDevices.SelectedItem is RtlNative.Device device ? ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(), device) : null;
        var offset = key != null && savedCaptureOffsets.TryGetValue(key, out var saved) ? saved : new CaptureOffset();
        CaptureOffsetEnabled.IsChecked = offset.Enabled; CaptureOffsetKhz.Text = offset.Khz.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    private bool SelectedHackRf => ReceiverFamily.SelectedIndex == 1;
    private string SelectedBackend => SelectedHackRf ? "HackRF" : "RTL";
    private IReadOnlyList<int> SelectedRates => SelectedHackRf ? ReceiverCapabilities.HackRf.SampleRates : ReceiverCapabilities.Rtl.SampleRates;
    private bool SelectedCanTune(long hz) => SelectedHackRf ? ReceiverCapabilities.HackRf.CanTune(hz) : SamplingModes.CanTune(hz, SelectedSamplingMode);
    private int SelectedGain => SelectedHackRf ? new HackRfGain(Math.Max(0, HackRfLna.SelectedIndex) * 8, Math.Max(0, HackRfVga.SelectedIndex) * 2, HackRfAmplifier.IsChecked == true, HackRfHfFilter.IsChecked == true).Token : SelectedSamplingMode != 0 ? -1000 : int.Parse(((ComboBoxItem)RtlGain.SelectedItem).Tag.ToString()!);
    private void InitializeHackRfControls()
    {
        foreach (int gain in Enumerable.Range(0, 6).Select(n => n * 8)) HackRfLna.Items.Add($"{gain} dB");
        foreach (int gain in Enumerable.Range(0, 32).Select(n => n * 2)) HackRfVga.Items.Add($"{gain} dB");
        HackRfLna.SelectedIndex = 2; HackRfVga.SelectedIndex = 8;
    }
    private void PopulateReceiverRates(int preferred = 0)
    {
        bool previous = updatingControls; updatingControls = true;
        try
        {
            SampleRateBox.Items.Clear();
            foreach (int rate in SelectedRates)
                SampleRateBox.Items.Add(new ComboBoxItem { Tag = rate, Content = SelectedHackRf ? $"{rate / 1000000.0:0.0} MS/s" + (rate == 2400000 ? " · reduced-rate experiment" : rate >= 19200000 ? " · high CPU / USB load" : " · recommended") : rate == 3200000 ? "3.2 MS/s · 3.2 MHz span (experimental)" : $"{rate / 1000.0:0.###} kS/s · {rate / 1000.0:0.###} kHz span" });
            ReflectRate(preferred == 0 ? (SelectedHackRf ? 9600000 : 240000) : preferred);
        }
        finally { updatingControls = previous; }
    }
    private void ReflectRate(int rate)
    {
        SampleRateBox.SelectedItem = SampleRateBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == rate) ?? SampleRateBox.Items.Cast<ComboBoxItem>().First();
    }
    private void ReflectHackRfGain(int token)
    {
        var gain = HackRfGain.Decode(token); HackRfLna.SelectedIndex = gain.Lna / 8; HackRfVga.SelectedIndex = gain.Vga / 2; HackRfAmplifier.IsChecked = gain.Amplifier; HackRfHfFilter.IsChecked = gain.HfFilter;
    }
    private void ReceiverFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updatingControls) return;
        Stop();
        updatingControls = true;
        try { RtlDevices.ItemsSource = null; SamplingModeBox.SelectedIndex = 0; PopulateReceiverRates(); }
        finally { updatingControls = false; }
        RefreshSamplingControls();
        RtlStatus.Text = SelectedHackRf ? "Select HackRF mode on the PortaPack, connect USB, then click Find." : "Click Find to select your RTL receiver.";
    }
    private async void HackRfGainChanged(object sender, RoutedEventArgs e)
    {
        if (SelectedHackRf) await ApplyLiveControl(false);
    }
    private async Task RenderHackRfSmoke(Action<string> capture)
    {
        async Task WaitUi(Func<bool> condition)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && clock.ElapsedMilliseconds < 6000) await Task.Delay(25);
            if (!condition()) throw new InvalidOperationException("HackRF UI did not acknowledge the requested state: " + RtlStatus.Text);
        }
        Stop(); ReceiverFamily.SelectedIndex = 1;
        if (SelectedSampleRate != 9600000 || HackRfControls.Visibility != Visibility.Visible || SamplingModeBox.IsEnabled || RtlGain.Visibility != Visibility.Collapsed)
            throw new InvalidOperationException("HackRF family did not expose the correct capabilities and controls.");
        var device = new RtlNative.Device(0,"SYNTHETIC HackRF / PortaPack", "HackRF", "HackRF One", "MRS-HACKRF-TEST", "HackRF");
        RtlDevices.ItemsSource = new[]{device}; RtlDevices.SelectedItem = device;
        ReflectRate(2400000); FmMode.SelectedIndex = 0; HackRfLna.SelectedIndex = 2; HackRfVga.SelectedIndex = 8;
        activeGain = SelectedGain; activeRate = SelectedSampleRate; activeDevice = 0;
        rfMode = true; engine.StartExternal(2); SpectrumTitle.Text = "RF SPECTRUM / WATERFALL";
        receiver.Start(0,5800000000,activeGain,false,fixture:true,sampleRate:activeRate,fixtureSeconds:20,backend:"HackRF");
        await WaitUi(()=>receiver.AppliedFrequency==5800000000 && SourceLabel.Text.Contains("HackRF"));
        await LiveTune(6000000000); await WaitUi(()=>receiver.AppliedFrequency==6000000000 && FrequencyBox.Text=="6000.000000");
        HackRfLna.SelectedIndex=3; HackRfVga.SelectedIndex=10;
        await WaitUi(()=>receiver.AppliedGain==new HackRfGain(24,20).Token);
        SampleRateBox.SelectedIndex=1;await WaitUi(()=>receiver.AppliedRate==9600000 && activeRate==9600000);
        Spectrum.ResetView(); Spectrum.SetPointer(null); await Task.Delay(500);
        if (Spectrum.FullSpan != 9600000) throw new InvalidOperationException("HackRF full FFT span was cropped to the analog filter.");
        SidebarTabs.SelectedIndex=0;WorkspaceTabs.SelectedIndex=0;ReceiverScroll.ScrollToTop();RefreshPlayback();UpdateLayout();capture("ui-hackrf.png");
        ReceiverScroll.ScrollToVerticalOffset(430);UpdateLayout();capture("ui-hackrf-controls.png");
        Stop();
        SpectrumTitle.Text="RF SPECTRUM / WATERFALL";
        receiver.Start(0,162400000,activeGain,false,fixture:true,sampleRate:2400000,fixtureSeconds:20,backend:"HackRF",captureOffset:new());
        rfMode=true;engine.StartExternal(2);
        await WaitUi(()=>receiver.AppliedFrequency==162650000 && FrequencyBox.Text=="162.400000");
        FrequencyBox.Text="162.425000"; StartRtl(this,new RoutedEventArgs());
        await WaitUi(()=>receiver.AppliedFrequency==162675000 && FrequencyBox.Text=="162.425000");
        await TuneText("162.400000");
        await WaitUi(()=>receiver.AppliedFrequency==162650000 && FrequencyBox.Text=="162.400000");
        RefreshPlayback();
        if(!CaptureCenterLabel.Text.Contains("162.650000") || CaptureOffsetKhz.IsEnabled) throw new InvalidOperationException("Offset readout or live edit guard failed.");
        await Task.Delay(400); ReceiverScroll.ScrollToTop();UpdateLayout();capture("ui-hackrf-offset.png");
        Stop();
        updatingControls=true;HackRfHfFilter.IsChecked=true;ReflectRate(9600000);FmMode.SelectedIndex=3;PopulateBandwidth(2400);FrequencyBox.Text="7.100000";updatingControls=false;
        receiver.Start(0,7100000,SelectedGain,false,fixture:true,sampleRate:9600000,fixtureSeconds:15,mode:"USB",bandwidth:2400,backend:"HackRF",captureOffset:new(true,250));
        rfMode=true;engine.StartExternal(2);await WaitUi(()=>receiver.AppliedFrequency==7350000&&receiver.InputLevel!=null);await Task.Delay(250);RefreshPlayback();
        if(receiver.AppliedBandwidth!=1750000||HackRfHfFilter.IsEnabled||!HackRfInputStatus.Text.Contains("1.75"))throw new InvalidOperationException("HF capture coverage, level readout or stopped-only control failed");
        ReceiverScroll.ScrollToTop();UpdateLayout();capture("ui-hf-reception.png");
        Stop();HackRfHfFilter.IsChecked=false;
        Stop(); ReceiverFamily.SelectedIndex = 0;
        RtlDevices.ItemsSource = new[] { new RtlNative.Device(0,"SYNTHETIC RTL","MRS","Test","RTL-REPLAY-SWITCH") }; RtlDevices.SelectedIndex = 0;
        string replay = System.IO.Path.Combine(dataFolder,"synthetic-hackrf-replay.mrsiq");
        int replayGain = new HackRfGain().Token;
        using (var recording = new IqRecording(replay,new IqMetadata(3,DateTimeOffset.UtcNow,false,replayGain,null,"NFM",0,"HackRF")))
            recording.Write(SelfTests.MakeFm(false,16384,2400000),new CaptureDescriptor(5800000000,2400000,1750000,IqSampleFormat.Unsigned8),new ChannelSettings(5800000000,"NFM"),replayGain,replayGain);
        PlayIq(replay); await WaitUi(()=>SelectedHackRf && receiver.ReplayFinished);
        if (RtlDevices.Items.Count != 0) throw new InvalidOperationException("HackRF replay retained an RTL device under the HackRF selector.");
        StartRtl(this,new RoutedEventArgs());
        if (!RtlStatus.Text.Contains("Click Find") || receiver.WorkerId != 0) throw new InvalidOperationException("Replay attempted to open a stale hardware selection.");
        Stop();ReceiverFamily.SelectedIndex=0;RefreshPlayback();
        if (SelectedSampleRate!=240000 || HackRfControls.Visibility!=Visibility.Collapsed || !SamplingModeBox.IsEnabled || !RtlGain.IsEnabled)
            throw new InvalidOperationException("Switching back to RTL did not restore its controls.");
    }
}
