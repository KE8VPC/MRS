using System.IO;
using System.Windows;
using Microsoft.Win32;
namespace MRS;

public partial class MainWindow
{
    private string? savedReceiverIdentity;
    private sealed record RecoveryPlan(string Identity, long Frequency, int Rate, int Gain, bool Wide, string Mode = "NFM", double Bandwidth = 0, bool Agc = true, int SamplingMode = 0, string Backend = "RTL", CaptureOffset? Offset = null);
    private RecoveryPlan? recoveryPlan;
    private int recoveryAttempts;
    private CancellationTokenSource? recoveryCancellation;
    private void CancelRecovery()
    {
        recoveryCancellation?.Cancel(); recoveryCancellation?.Dispose(); recoveryCancellation = null;
        recoveryPlan = null;
    }
    private void ReconnectChanged(object sender, RoutedEventArgs e)
    {
        if (ready && AutoReconnectCheck.IsChecked != true && recoveryCancellation != null)
        {
            if (!rfMode) { CancelRecovery(); RtlStatus.Text = "Automatic reconnect cancelled."; }
            else { recoveryCancellation.Cancel(); recoveryCancellation.Dispose(); recoveryCancellation = null; }
        }
    }
    private async void HandleReceiverEnd(string message)
    {
        var plan = recoveryPlan; int usedAttempts = recoveryAttempts;
        bool retry = !receiver.IsReplay && AutoReconnectCheck.IsChecked == true && plan != null;
        Stop(); RtlStatus.Text = message; Log(message);
        if (!retry || plan == null || closing) return;
        if (usedAttempts >= 3) { Log("Reconnect limit reached. Use Find and Start to begin a new session."); return; }
        recoveryCancellation = new CancellationTokenSource(); var token = recoveryCancellation.Token;
        try
        {
            var result = await ReceiverRecovery.Find(plan.Identity, usedAttempts, () => RtlReceiver.EnumerateBackend(plan.Backend), Task.Delay,
                status => { RtlStatus.Text = status; Log(status); }, token);
            token.ThrowIfCancellationRequested();
            if (closing) return;
            if (result.Device == null) { RtlStatus.Text = "Reconnect stopped after 3 attempts. Find and select your receiver manually."; Log(RtlStatus.Text); return; }
            var device = result.Device;
            if(!ClaimReceiver(device,plan.Identity)){CancelRecovery();return;}
            recoveryPlan = plan; recoveryAttempts = result.Attempts;
            activeDevice = device.Index; activeRate = plan.Rate; activeGain = plan.Gain; tunedHz = plan.Frequency; tunedMode = plan.Mode;
            RtlDevices.ItemsSource = new[] { device }; RtlDevices.SelectedIndex = 0;
            ReflectRate(plan.Rate); FmMode.SelectedIndex = Array.IndexOf(ReceiveDemodulator.Modes, plan.Mode);
            FrequencyBox.Text = (plan.Frequency / 1000000m).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
            foreach (System.Windows.Controls.ComboBoxItem item in RtlGain.Items) if (int.Parse(item.Tag.ToString()!) == plan.Gain) RtlGain.SelectedItem = item;
            updatingControls = true; SamplingModeBox.SelectedIndex = plan.SamplingMode; updatingControls = false; RefreshSamplingControls();
            rfMode = true; engine.StartExternal(2); Spectrum.SetRf(plan.Frequency, plan.Rate); SpectrumTitle.Text = "RF SPECTRUM / WATERFALL";
            receiver.Start(device.Index, plan.Frequency, plan.Gain, plan.Wide, sampleRate: plan.Rate, identity: plan.Identity, mode: plan.Mode, bandwidth: plan.Bandwidth, agc: plan.Agc, samplingMode: plan.SamplingMode, backend: plan.Backend, captureOffset: plan.Offset);
            Started(); RtlStatus.Text = "Same receiver reopened; waiting for I/Q. Recordings remain stopped."; Log(RtlStatus.Text);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Stop(); RtlStatus.Text = "Reconnect failed: " + ex.Message; Log(RtlStatus.Text); }
    }
    private void RecordIq(object sender, RoutedEventArgs e)
    {
        if(automaticIq.Armed){Log("Disarm automatic I/Q before starting a manual recording.");return;}
        if (!rfMode || receiver.IsReplay || receiver.AppliedFrequency == 0) { Log("Start live SDR reception before recording I/Q."); return; }
        var dialog = new SaveFileDialog { Filter = "M.R.S. I/Q recording|*.mrsiq", FileName = $"MRS-IQ-{workspaceSlot}-{DateTime.Now:yyyyMMdd-HHmmss}.mrsiq", Title = "Record captured RF I/Q" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            receiver.RecordIq(dialog.FileName, new IqMetadata(3, DateTimeOffset.UtcNow, tunedMode == "WFM", activeGain, recoveryPlan?.Identity, tunedMode));
            IqRecordLabel.Text = "I/Q recording started."; Log("I/Q recording: " + dialog.FileName);
        }
        catch (Exception ex) { Log("Cannot start I/Q recording: " + ex.Message); }
    }
    private void FinishIq(object sender, RoutedEventArgs e) => receiver.FinishIq();
    private void OpenIq(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "M.R.S. I/Q recording|*.mrsiq", Title = "Replay captured RF I/Q" };
        if (dialog.ShowDialog(this) == true) PlayIq(dialog.FileName);
    }
    private void PlayIq(string path, bool paused = false)
    {
        Stop(false);
        try
        {
            rfMode = true; engine.StartExternal(2);
            loadedIqPath = path; PlaybackPanel.Visibility = Visibility.Visible; SidebarTabs.SelectedItem = RecordingTab;
            var metadata = receiver.StartReplay(path, paused); tunedMode = metadata.Mode ?? (metadata.WideFm ? "WFM" : "NFM");
            SpectrumTitle.Text = "RF SPECTRUM / WATERFALL · I/Q REPLAY";
            SourceLabel.Text = "I/Q REPLAY · " + Path.GetFileName(path); Started();
            Log($"I/Q replay: {path}; captured {metadata.StartedUtc:u}. Follows recorded channel/mode/timing; no hardware opened.");
        }
        catch (Exception ex) { Stop(); Log("Cannot replay I/Q: " + ex.Message); }
    }
}


