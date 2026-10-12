using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NAudio.CoreAudioApi;
namespace MRS;
public partial class MainWindow
{
    private bool updatingControls, refreshingOutputs, seeking;
    private string? loadedIqPath, savedOutputId;
    private record OutputEndpoint(string Id, string Name);
    private double SelectedBandwidth => ChannelBandwidth.SelectedItem is ComboBoxItem item ? (double)item.Tag : ChannelSettings.DefaultWidth(ReceiveDemodulator.Modes[Math.Max(0, FmMode.SelectedIndex)]);
    private ChannelSettings SelectedChannel(long frequency) => new(frequency, ReceiveDemodulator.Modes[Math.Max(0, FmMode.SelectedIndex)], SelectedBandwidth, ChannelAgc.IsChecked == true);
    private void PopulateBandwidth(double selected = 0)
    {
        bool previous = updatingControls; updatingControls = true;
        try
        {
            string mode = ReceiveDemodulator.Modes[Math.Max(0, FmMode.SelectedIndex)];
            ChannelBandwidth.Items.Clear();
            foreach (double width in ChannelSettings.Widths(mode)) ChannelBandwidth.Items.Add(new ComboBoxItem { Content = $"{width / 1000:0.###} kHz", Tag = width });
            ChannelBandwidth.SelectedItem = ChannelBandwidth.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (double)i.Tag == selected) ?? ChannelBandwidth.Items.Cast<ComboBoxItem>().First(i => (double)i.Tag == ChannelSettings.DefaultWidth(mode));
            ChannelAgc.IsEnabled = mode is "AM" or "USB" or "LSB";
        }
        finally { updatingControls = previous; }
    }
    private void ReflectChannel()
    {
        if (receiver.AppliedChannel is not { } channel) return;
        tunedHz = channel.Frequency; tunedMode = channel.Mode;
        BroadcastPlot.Clear(); BroadcastWaveform.SetAudio(null); shownBroadcast = null;
        lock(frameSync) { latestBroadcastAudio = null; latestStereoWaveform = null; }
        updatingControls = true;
        try { FmMode.SelectedIndex = Array.IndexOf(ReceiveDemodulator.Modes, channel.Mode); PopulateBandwidth(channel.Width); ChannelAgc.IsChecked = channel.Agc; }
        finally { updatingControls = false; }
        FrequencyBox.Text = (channel.Frequency / 1000000m).ToString("F6", CultureInfo.InvariantCulture);
        Spectrum.SetChannel(channel); RefreshNavigation();
        if (recoveryPlan != null) recoveryPlan = recoveryPlan with { Frequency = channel.Frequency, Mode = channel.Mode, Wide = channel.Mode == "WFM", Bandwidth = channel.Width, Agc = channel.Agc };
    }
    private void ApplyChannelSelection()
    {
        if (!ready || updatingControls || !rfMode || receiver.AppliedFrequency == 0) return;
        if (receiver.IsReplay) { ReflectChannel(); return; }
        ResetActivity("Receive mode/filter changed; scanner stopped.");
        try { receiver.SetChannel(SelectedChannel(tunedHz)); }
        catch (Exception ex) { ReflectChannel(); Log("Channel change not applied: " + ex.Message); }
    }
    private async Task ApplyLiveControl(bool rate)
    {
        if (!ready || updatingControls || !rfMode || receiver.AppliedFrequency == 0) return;
        if (receiver.IsReplay) return;
        ResetActivity("Gain/rate changed; scanner stopped.");
        long controlSession = session;
        try
        {
            if (rate) await receiver.SetRate(SelectedSampleRate);
            else await receiver.SetGain(SelectedGain);
        }
        catch (Exception ex)
        {
            if (controlSession != session) return;
            updatingControls = true;
            try
            {
                ReflectRate(activeRate); if (receiver.IsHackRf) ReflectHackRfGain(activeGain);
                foreach (ComboBoxItem item in RtlGain.Items) if (int.Parse(item.Tag.ToString()!) == activeGain) RtlGain.SelectedItem = item;
            }
            finally { updatingControls = false; }
            Log("Receiver change failed: " + ex.Message);
        }
    }
    private async void ShiftCapture(object sender, RoutedEventArgs e)
    {
        StopActivity("Scanner stopped for capture shift.");
        if (!rfMode || receiver.IsReplay) { Log("Start a live receiver to shift capture coverage."); return; }
        if (receiver.IsHackRf && receiver.Offset.Enabled) { Log("Capture offset is enabled. Tune the listening frequency or pan the spectrum; disable offset while stopped to shift capture independently."); return; }
        int direction = int.Parse(((Button)sender).Tag.ToString()!, CultureInfo.InvariantCulture);
        long frequency = receiver.AppliedFrequency + direction * activeRate / 2;
        if (!receiver.CanTune(frequency)) { Log("Capture shift exceeds receiver tuning range."); return; }
        try { await receiver.Retune(frequency); } catch (Exception ex) { Log("Capture shift failed: " + ex.Message); }
    }
    private void RefreshOutputs()
    {
        refreshingOutputs = true;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            OutputDevices.Items.Clear();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                using (device) OutputDevices.Items.Add(new OutputEndpoint(device.ID, device.FriendlyName));
            RestoreOutput();
        }
        finally { refreshingOutputs = false; }
        if (ready) ListeningChanged(this, new RoutedEventArgs());
    }
    private void RestoreOutput()
    {
        OutputDevices.SelectedItem = OutputDevices.Items.Cast<OutputEndpoint>().SingleOrDefault(d => d.Id == savedOutputId);
    }
    private void RefreshPlayback()
    {
        PlaybackPanel.Visibility = loadedIqPath == null ? Visibility.Collapsed : Visibility.Visible;
        CaptureOffsetEnabled.IsEnabled = CaptureOffsetKhz.IsEnabled = !rfMode && !receiver.IsReplay;
        CaptureCenterLabel.Text = receiver.AppliedFrequency == 0 ? "Capture center: stopped" : $"Capture: {receiver.AppliedFrequency / 1000000.0:F6} MHz · offset {(receiver.AppliedFrequency - (receiver.AppliedChannel?.Frequency ?? receiver.AppliedFrequency)) / 1000.0:+0.###;-0.###;0} kHz";
        SampleRateBox.IsEnabled = FmMode.IsEnabled = ChannelBandwidth.IsEnabled = !receiver.IsReplay;
        SamplingModeBox.IsEnabled = !receiver.IsReplay && !SelectedHackRf; RtlGain.IsEnabled = !receiver.IsReplay && !SelectedHackRf && SelectedSamplingMode == 0; HackRfControls.IsEnabled = !receiver.IsReplay;
        ChannelAgc.IsEnabled = !receiver.IsReplay && (ReceiveDemodulator.Modes[Math.Max(0, FmMode.SelectedIndex)] is "AM" or "USB" or "LSB");
        if (loadedIqPath == null) return;
        bool active = receiver.IsReplay;
        ReplayPlayButton.IsEnabled = !active || receiver.ReplayPaused || receiver.ReplayFinished;
        ReplayPauseButton.IsEnabled = active && receiver.ReplayReady && !receiver.ReplayPaused && !receiver.ReplayFinished;
        ReplaySeek.IsEnabled = active && receiver.ReplayReady;
        if (active)
        {
            if (!seeking) { ReplaySeek.Maximum = Math.Max(.001, receiver.ReplayDuration.TotalSeconds); ReplaySeek.Value = receiver.ReplayPosition.TotalSeconds; }
            ReplayTime.Text = !receiver.ReplayReady ? "Indexing…" : $"{receiver.ReplayPosition:hh\\:mm\\:ss} / {receiver.ReplayDuration:hh\\:mm\\:ss} · {(receiver.ReplayFinished ? "Finished" : receiver.ReplayPaused ? "Paused" : "Playing")}";
        }
        else ReplayTime.Text = "Stopped · file remains loaded";
    }
    private void ReplayPlay(object sender, RoutedEventArgs e)
    {
        ChannelAgc.IsEnabled = !receiver.IsReplay && (ReceiveDemodulator.Modes[Math.Max(0, FmMode.SelectedIndex)] is "AM" or "USB" or "LSB");
        if (loadedIqPath == null) return;
        if (!receiver.IsReplay || receiver.ReplayFinished) PlayIq(loadedIqPath); else receiver.ReplayPaused = false;
    }
    private void ReplayPause(object sender, RoutedEventArgs e) { HdRecordings.StopLive("Replay paused; restart HD after resuming."); receiver.ReplayPaused = true; engine.RetuneBoundary(); }
    private void ReplayStop(object sender, RoutedEventArgs e) { Stop(false); RefreshPlayback(); }
    private void ReplaySeekBegin(object sender, MouseButtonEventArgs e) => seeking = true;
    private async void ReplaySeekEnd(object sender, MouseButtonEventArgs e) { if (!seeking) return; seeking = false; await ApplySeek(); }
    private void ReplaySeekKeyBegin(object sender, KeyEventArgs e) { if (e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown) seeking = true; }
    private async void ReplaySeekKey(object sender, KeyEventArgs e) { if (seeking) { seeking = false; await ApplySeek(); } }
    private async Task ApplySeek()
    {
        if (loadedIqPath == null || !receiver.ReplayReady) return;
        var position = TimeSpan.FromSeconds(ReplaySeek.Value);
        FinishRecording(this, new RoutedEventArgs()); engine.RetuneBoundary();
        if (receiver.ReplayFinished)
        {
            PlayIq(loadedIqPath, true); long seekingSession = session;
            while (receiver.IsReplay && !receiver.ReplayReady && session == seekingSession) await Task.Delay(20);
            if (session != seekingSession || !receiver.IsReplay) return;
        }
        try { receiver.SeekReplay(position); } catch (Exception ex) { Log("Seek failed: " + ex.Message); }
    }
}
