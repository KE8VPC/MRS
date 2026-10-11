namespace MRS;
public partial class MainWindow
{
    private Dictionary<string, int> savedSampling = new();
    private int SelectedSamplingMode => SelectedHackRf ? 0 : Math.Clamp(SamplingModeBox.SelectedIndex, 0, 2);
    private void RefreshSamplingControls()
    {
        HackRfControls.Visibility = SelectedHackRf ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        RtlSamplingPanel.Visibility = SelectedHackRf ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        SetupRtlButton.Visibility = SelectedHackRf ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        RtlGain.Visibility = SelectedHackRf ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        SamplingModeBox.IsEnabled = !SelectedHackRf && !receiver.IsReplay;
        FrequencyRangeLabel.Text = "Frequency (MHz) · " + (SelectedHackRf ? "1–6000 MHz" : SelectedSamplingMode == 0 ? "25–1750 MHz" : "1 Hz–28.8 MHz");
        SamplingStatus.Text = SelectedSamplingMode == 0 ? "Tuner input. Gain controls apply." : "Tuner bypassed; tuner gain unavailable. Branch wiring and antenna determine reception. Near-DC response and image rejection are unverified.";
        if (SelectedHackRf) SamplingStatus.Text = "HackRF quadrature I/Q · no RTL direct-sampling branch";
        RtlGain.IsEnabled = !SelectedHackRf && SelectedSamplingMode == 0 && !receiver.IsReplay;
        ReceiverIdentityStatus.Text=RtlDevices.SelectedItem is RtlNative.Device device ? $"{device.Manufacturer} {device.Product} · serial {device.Serial}\n"+(ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(),device)!=null?"Unique identity: selection and sampling preference can be restored.":"Manual selection: serial is missing, generic or duplicated.") : "Select a receiver";
    }
    private void SamplingSelectionChanged()
    {
        if (!ready || updatingControls) return;
        if (rfMode) Stop();
        RefreshSamplingControls();
        if (RtlDevices.SelectedItem is RtlNative.Device device)
        {
            string? key = ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(), device);
            if (key != null) savedSampling[key] = SelectedSamplingMode;
        }
        Log("Selected " + SamplingModes.Name(SelectedSamplingMode) + ". Enter a frequency in range and Start. Sampling changes finalize recordings.");
    }
    private void RestoreSamplingForSelection()
    {
        if (!ready || updatingControls || receiver.IsReplay) return;
        if (rfMode) Stop();
        string? key = RtlDevices.SelectedItem is RtlNative.Device device ? ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(), device) : null;
        updatingControls = true;
        try { SamplingModeBox.SelectedIndex = key != null && savedSampling.TryGetValue(key, out int mode) && SamplingModes.Valid(mode) ? mode : 0; }
        finally { updatingControls = false; }
        RefreshSamplingControls();
    }
    private async Task RenderSamplingSmoke(Action<string> capture)
    {
        var a = new RtlNative.Device(0, "SYNTHETIC direct input", "MRS", "Test", "DS-A17");
        var b = a with { Index = 1, Serial = "DS-B18" };
        RtlDevices.ItemsSource = new[] { a, b }; RtlDevices.SelectedItem = a;
        SamplingModeBox.SelectedIndex = 1;
        RtlDevices.SelectedItem = b;
        if (SelectedSamplingMode != 0) throw new InvalidOperationException("New receiver inherited another device's sampling branch.");
        SamplingModeBox.SelectedIndex = 2; RtlDevices.SelectedItem = a;
        if (SelectedSamplingMode != 1) throw new InvalidOperationException("I branch preference was not restored by receiver identity.");
        RtlDevices.SelectedItem = b;
        if (SelectedSamplingMode != 2 || RtlGain.IsEnabled || !FrequencyRangeLabel.Text.Contains("1 Hz")) throw new InvalidOperationException("Direct sampling controls were not restored.");
        FmMode.SelectedIndex = 2; FrequencyBox.Text = "7.000000";
        rfMode = true; tunedMode = "AM"; activeRate = 240000; activeGain = -1000; engine.StartExternal(2);
        SpectrumTitle.Text = "RF SPECTRUM / WATERFALL · SYNTHETIC DIRECT-SAMPLING TEST";
        receiver.Start(1, 7000000, -1000, false, fixture: true, fixtureSeconds: 5, mode: "AM", samplingMode: 2);
        for (int i = 0; i < 150 && tunedHz != 7000000; i++) await Task.Delay(20);
        if (tunedHz != 7000000 || receiver.SamplingMode != 2) throw new InvalidOperationException("Direct sampling UI did not apply 7 MHz.");
        SidebarTabs.SelectedIndex = 0; ReceiverScroll.ScrollToTop(); Spectrum.ResetView(); Spectrum.SetPointer(null);
        await Task.Delay(100); UpdateLayout(); capture("ui-direct-sampling.png");
        await LiveTune(1);
        for (int i = 0; i < 150 && tunedHz != 1; i++) await Task.Delay(20);
        if (tunedHz != 1 || Spectrum.VisibleCenter - Spectrum.VisibleSpan / 2 < 0) throw new InvalidOperationException("Near-zero tuning or RF clipping failed.");
        Spectrum.ResetView(); await Task.Delay(100); UpdateLayout(); capture("ui-near-zero.png");
        string path = System.IO.Path.Combine(dataFolder, "direct-branch-smoke.mrsiq");
        receiver.RecordIq(path, new IqMetadata(3, DateTimeOffset.UtcNow, false, -1000, null, "AM")); await Task.Delay(100);
        RtlDevices.SelectedItem = a;
        if (rfMode || receiver.IsRecording || receiver.WorkerId != 0 || SelectedSamplingMode != 1) throw new InvalidOperationException("Receiver change did not stop capture, finalize I/Q and restore the saved branch.");
        using (var reader = new IqReader(path))
        {
            if (reader.Metadata.SamplingMode != 2) throw new InvalidOperationException("Recorded branch did not match the actual receiver.");
            while (reader.Read() != null) { }
        }
        PlayIq(path, true);
        for (int i = 0; i < 150 && !receiver.ReplayReady; i++) await Task.Delay(20);
        receiver.SeekReplay(TimeSpan.Zero); await Task.Delay(100); RefreshPlayback();
        if (SelectedSamplingMode != 2 || SamplingModeBox.IsEnabled || RtlGain.IsEnabled) throw new InvalidOperationException("Replay did not restore and lock the recorded branch.");
        Stop(); SamplingModeBox.SelectedIndex = 0;
        if (!RtlGain.IsEnabled) throw new InvalidOperationException("Returning to quadrature did not enable tuner gain.");
    }
}

