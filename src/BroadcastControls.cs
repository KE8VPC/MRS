using System.Windows;
using System.Windows.Controls;
namespace MRS;
public partial class MainWindow
{
    private float[]? latestBroadcastAudio;
    private byte[]? latestStereoWaveform;
    private BroadcastState? shownBroadcast;
    private static readonly string[] RbdsTypes = ["None", "News", "Information", "Sports", "Talk", "Rock", "Classic rock", "Adult hits", "Soft rock", "Top 40", "Country", "Oldies", "Soft", "Nostalgia", "Jazz", "Classical", "Rhythm & blues", "Soft R&B", "Foreign language", "Religious music", "Religious talk", "Personality", "Public", "College", "Spanish talk", "Spanish music", "Hip hop", "Unassigned", "Unassigned", "Weather", "Emergency test", "Emergency"];
    private static readonly string[] RdsTypes = ["None", "News", "Current affairs", "Information", "Sport", "Education", "Drama", "Culture", "Science", "Varied", "Pop music", "Rock music", "Easy listening", "Light classical", "Serious classical", "Other music", "Weather", "Finance", "Children", "Social affairs", "Religion", "Phone-in", "Travel", "Leisure", "Jazz", "Country", "National music", "Oldies", "Folk music", "Documentary", "Alarm test", "Alarm"];
    private void BroadcastMonoChanged(object sender, RoutedEventArgs e) { if (receiver != null) receiver.ForceMono = ForceMonoBox.IsChecked == true; }
    private void BroadcastViewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BroadcastPlot == null || Spectrum == null) return;
        bool alternate = BroadcastPanel.Visibility == Visibility.Visible && BroadcastView.SelectedIndex != 0;
        Spectrum.Visibility = alternate ? Visibility.Collapsed : Visibility.Visible;
        BroadcastPlot.Visibility = alternate && BroadcastView.SelectedIndex != 3 ? Visibility.Visible : Visibility.Collapsed;
        BroadcastWaveform.Visibility = alternate && BroadcastView.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        RfNavigationControls.Visibility = RfViewControls.Visibility = alternate ? Visibility.Collapsed : Visibility.Visible;
        SpectrumTitle.Text = (alternate ? BroadcastView.SelectedIndex == 1 ? "MPX SPECTRUM / WATERFALL" : BroadcastView.SelectedIndex == 3 ? "STEREO AUDIO WAVEFORM" : "AUDIO SPECTRUM / WATERFALL (L+R)" : rfMode ? "RF SPECTRUM / WATERFALL" : "AUDIO SPECTRUM / WATERFALL") + (receiver.IsReplay ? " · I/Q REPLAY" : "");
        BroadcastPlot.SetBaseband(BroadcastView.SelectedIndex == 1 ? 240000 : 48000, BroadcastView.SelectedIndex == 1 ? "MPX" : "Audio");
        BroadcastPlot.Configure(Spectrum.Settings with { Zoom = 1, Pan = .5 });
        shownBroadcast = null;
    }
    private void RefreshBroadcast()
    {
        bool visible = rfMode && receiver.AppliedChannel?.Mode == "WFM" && FmTab.IsSelected && BroadcastTab.IsSelected;
        if ((BroadcastPanel.Visibility == Visibility.Visible) != visible)
        {
            BroadcastPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            BroadcastViewChanged(this, null!);
        }
        if (!visible) { shownBroadcast = null; BroadcastStatus.Text = "Waiting for composite signal"; BroadcastData.Text = "RDS: searching · HD: recording preview in HD Radio tab"; return; }
        var state = receiver.Broadcast;
        if (state == null) { BroadcastStatus.Text = "Waiting for composite signal"; BroadcastData.Text = "RDS: searching · HD: recording preview in HD Radio tab"; BroadcastPlot.Clear(); BroadcastWaveform.SetAudio(null); return; }
        var rds = receiver.Rds;
        BroadcastData.Text = rds?.Pi is not { } pi || rds.AgeSeconds > 10 ? "RDS: searching · HD: recording preview in HD Radio tab" : $"RDS PI {pi:X4} · PTY {rds.Pty}: {(RdsRegion.SelectedIndex == 0 ? RbdsTypes : RdsTypes)[rds.Pty]} · {(!string.IsNullOrEmpty(rds.Ps) ? rds.Ps : "station name pending")} · {rds.Groups} valid groups\n{rds.Text}";
        BroadcastStatus.Text = $"Pilot {(state.Pilot ? "detected" : "searching")} · lock {(state.Locked ? "yes" : "no")} · {(state.Stereo ? "STEREO" : ForceMonoBox.IsChecked == true ? "FORCED MONO" : "MONO")} · L {state.LeftDb:F1} / R {state.RightDb:F1} dBFS{(state.Clipped ? " · CLIPPING" : "")}";
        LeftMeter.Value = Math.Clamp(state.LeftDb, -90, 0); RightMeter.Value = Math.Clamp(state.RightDb,-90,0);
        if (!ReferenceEquals(shownBroadcast,state))
        {
            shownBroadcast = state;
            if (BroadcastView.SelectedIndex == 1 && state.CompositeSpectrum.Length != 0) BroadcastPlot.PushAt(state.CompositeSpectrum, DisplaySettings.Clock);
            if (BroadcastView.SelectedIndex == 2) { float[]? audio; lock(frameSync) audio = latestBroadcastAudio; if (audio != null) BroadcastPlot.PushAt(audio, DisplaySettings.Clock); }
            if (BroadcastView.SelectedIndex == 3) { byte[]? audio; lock(frameSync) audio = latestStereoWaveform; BroadcastWaveform.SetAudio(audio); }
        }
        BroadcastPlot.Tick();
    }
    private async Task RenderBroadcastSmoke(Action<string> capture)
    {
        Stop();
        string path = System.IO.Path.Combine(dataFolder, "broadcast-smoke.mrsiq");
        var iq = RdsTests.Modulate(RdsTests.Signal(7, stereo:true),240000);
        // Explicit elapsed times, independent of fixture generation/write speed.
        WriteBroadcastFixture(path, iq);
        PlayIq(path); SidebarTabs.SelectedIndex = 0;SelectWorkspace(FmTab);
        for (int n = 0; n < 600 && (receiver.Rds?.Ps != "MRS TEST" || receiver.Rds?.Text != "HELLO"); n++) await Task.Delay(20);
        if (receiver.Rds?.Ps != "MRS TEST" || receiver.Broadcast?.Stereo != true) throw new InvalidOperationException("Broadcast replay failed stereo/RDS acquisition.");
        receiver.ReplayPaused = true; await Task.Delay(100); RefreshBroadcast(); UpdateLayout(); capture("ui-broadcast-rf.png");
        RefreshPackets();
        if(!AnalysisPackets.ExportJson().Contains("Station metadata"))throw new InvalidOperationException("Data view did not collect decoded RDS metadata.");
        SelectWorkspace(ObservationsTab);await Task.Delay(100);UpdateLayout();capture("ui-data-rds.png");SelectWorkspace(FmTab);
        BroadcastView.SelectedIndex = 1; RefreshBroadcast(); UpdateLayout(); capture("ui-broadcast-mpx.png");
        if (Spectrum.Visibility != Visibility.Collapsed || BroadcastPlot.FullSpan != 120000) throw new InvalidOperationException("MPX view did not expose the 240 kS/s composite span.");
        BroadcastView.SelectedIndex = 2; RefreshBroadcast(); UpdateLayout(); capture("ui-broadcast-audio.png");
        BroadcastView.SelectedIndex = 3; RefreshBroadcast(); UpdateLayout(); capture("ui-broadcast-waveform.png");
        ForceMonoBox.IsChecked = true; receiver.ReplayPaused = false;
        for (int n=0; n<100 && receiver.Broadcast?.Stereo == true; n++) await Task.Delay(20);
        if (receiver.Broadcast?.Stereo == true) throw new InvalidOperationException("Force mono did not affect replay output.");
        receiver.ReplayPaused = true; receiver.SeekReplay(TimeSpan.Zero);
        await Task.Delay(150); RefreshBroadcast();
        if (receiver.Rds != null || !BroadcastData.Text.Contains("searching")) throw new InvalidOperationException("Seek retained stale RDS station metadata.");
        Stop(); RefreshBroadcast(); ForceMonoBox.IsChecked = false; BroadcastView.SelectedIndex = 0;
        if (BroadcastPanel.Visibility != Visibility.Collapsed || Spectrum.Visibility != Visibility.Visible) throw new InvalidOperationException("Stopping did not restore RF/audio view layout.");
    }
    private static void WriteBroadcastFixture(string path, byte[] iq)
    {
        using var file = System.IO.File.Create(path); using var writer = new System.IO.BinaryWriter(file);
        byte[] json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new IqMetadata(3,DateTimeOffset.UtcNow,true,-1000,null,"WFM"));
        writer.Write(0x31514953524DL); writer.Write(json.Length); writer.Write(json);
        for (int n=0; n<iq.Length; n+=16384)
        {
            int count=Math.Min(16384,iq.Length-n);
            writer.Write((long)(n / 480000.0 * TimeSpan.TicksPerSecond)); writer.Write(count); writer.Write(102100000L); writer.Write(240000); writer.Write(240000.0); writer.Write(0);
            writer.Write(102100000L); writer.Write(1); writer.Write(190000.0); writer.Write((byte)1); writer.Write(-1000); writer.Write(-1000); writer.Write(iq,n,count);
        }
        writer.Write(-1L);
    }
}
