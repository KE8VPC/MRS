using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.Wave;
using System.Globalization;
using System.Windows.Controls;

namespace MRS;
public partial class MainWindow : Window
{
    private readonly AudioEngine engine = new();
    private readonly RtlReceiver receiver = new();
    private volatile bool rfMode;
    private long tunedHz;
    private string tunedMode = "NFM";
    private uint activeDevice;
    private int activeRate = 240000, activeGain;
    private double audioLevel = -120;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS");
    private readonly object frameSync = new();
    private (float[] Values, double Level, double Time, long Center)? latest;
    private bool ready, closing;
    private long session;
    private record Device(int Id, string Name);
    private record Preferences(float Volume, float Squelch, string? InputName, string Frequency = "146.520000", int Mode = 0, int Gain = 0, float RfSquelch = -60, bool? RfSquelchOn = null, int SampleRate = 240000, string? RtlIdentity = null, bool AutoReconnect = false, string? OutputId = null, double Bandwidth = 0, bool AudioAgc = true, Dictionary<string, int>? SamplingByReceiver = null, bool ForceMono = false, int RdsRegion = 0, bool PacketDecoding = false, bool P25Decoding = false, bool SubaudibleDecoding = false, bool GroupP25Repeats = true, string[]? HiddenP25Types = null, string Backend = "RTL", int HackRfGainToken = 0x1001010, Dictionary<string, CaptureOffset>? CaptureOffsets = null, double SidebarWidth = 330, bool PagingDecoding = false, bool GroupPagingRepeats = true, bool AutoBand = false, double CwPitch = 700, double P25VoiceGainDb = 12);

    public MainWindow(bool diagnostic = false, string slot = "A", string? diagnosticRoot = null)
    {
        if(slot.Length!=1||slot[0]<'A'||slot[0]>'Z')throw new ArgumentException("Unknown receiver workspace.");
        workspaceSlot=slot;diagnosticWorkspace=diagnostic;
        if(slot=="B")dataFolder=Path.Combine(dataFolder,"ReceiverB");else if(slot!="A")dataFolder=Path.Combine(dataFolder,"Receivers",slot);
        InitializeComponent();
        InitializeWorkspaceLayout();
        InitializeBroadcastWorkspace();
        SliderWheel.Enable(RfSquelchSlider,.5);
        SliderWheel.Enable(SquelchSlider,.5);
        SliderWheel.Enable(VolumeSlider,.01);
        SliderWheel.Enable(ReplaySeek,1,changed:async ()=>await ApplySeek());
        Title=$"M.R.S. — Receiver {slot}";WorkspaceLabel.Text=$"Receiver {slot}";OtherReceiverButton.Content="Receivers...";
        InitializePackets();
        InitializeSensors();
        InitializeHackRfControls(); SampleRateBox.Items.Clear();
        foreach (int rate in ReceiverCapabilities.Rtl.SampleRates)
            SampleRateBox.Items.Add(new ComboBoxItem { Content = rate == 3200000 ? "3.2 MS/s · 3.2 MHz span (experimental)" : $"{rate / 1000.0:0.###} kS/s · {rate / 1000.0:0.###} kHz span", Tag = rate });
        SampleRateBox.SelectedIndex = 0;
        PopulateBandwidth();
        if (diagnostic) dataFolder = diagnosticRoot ?? Path.Combine(AppContext.BaseDirectory, "smoke-profile", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataFolder);Aviation.ConfigureOfflineDetail(dataFolder);
        InitializeFilePlans();InitializeTrunkFollowing();InitializeFleet();InitializeCalibration();InitializeAbout();InitializeSiteSaving();InitializeMonitoringViews();InitializeRadioData();InitializeAprsInternet();InitializeJs8();InitializeHfReferences();InitializeFoxHunt();InitializeDiscoveryPresets();Dmr.InitializeDiscovery(Path.Combine(dataFolder,"dmr-discovery-mode.json"));
        receiver.RawIq+=_=>{if(!receiver.IsReplay)Interlocked.Exchange(ref lastLiveFrame,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());};
        PagingPackets.InitializePagingRules(Path.Combine(dataFolder,"paging-rules.json"));
        P25Systems.Initialize(Path.Combine(dataFolder,"p25-discoveries.json"));
        P25Activity.Initialize(Path.Combine(dataFolder,"p25-activity-labels.json"));
        P25Systems.SaveFrequencyRequested+=draft=>{SidebarTabs.SelectedItem=FrequencyTab;FrequencyManager.ReviewDiscovery(draft);};
        DisplayControls.Changed += s => receiver.SpectrumSize = s.RfFftSize;
        DisplayControls.Changed += Spectrum.Configure; DisplayControls.Changed += value => BroadcastPlot.Configure(value with { Zoom = BroadcastPlot.Settings.Zoom, Pan = BroadcastPlot.Settings.Pan });
        DisplayControls.ClearPeakRequested += Spectrum.ResetPeak;
        Spectrum.SettingsChanged += DisplayControls.AcceptViewChange;
        Spectrum.CaptureSpanChanged += DisplayControls.SetCaptureSpan;
        Spectrum.TuneRequested += async hz => await LiveTune(hz);
        Spectrum.SettingsChanged += _ => RefreshNavigation();
        Spectrum.CaptureSpanChanged += _ => RefreshNavigation();
        FrequencyBox.KeyDown += async (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { e.Handled = true; await TuneText(FrequencyBox.Text); } };
        DisplayControls.Initialize(Path.Combine(dataFolder, "display.json"));
        FrequencyManager.SharingCatalog = DiscoveryCatalog;
        FrequencyManager.CurrentBandwidth = () => receiver.AppliedChannel?.Width ?? SelectedBandwidth;
        FrequencyManager.CurrentSettings = () => rfMode ? (tunedHz, tunedMode) : (0L, "NFM");
        FrequencyManager.Changed += () => Spectrum.SetMarkers(FrequencyManager.Markers);
        FrequencyManager.TuneRequested += TuneSaved;
        InitializeFolderScan();
        FrequencyManager.Initialize(Path.Combine(diagnosticWorkspace ? dataFolder : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS"), "frequencies.json"), diagnosticWorkspace ? null : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS", "ReceiverB", "frequencies.json"));
        Spectrum.SaveFrequencyRequested += hz => { SidebarTabs.SelectedItem = FrequencyTab; FrequencyManager.AddSignal(hz); };
        InitializeActivity();
        Spectrum.MarkerSelected += id => { SidebarTabs.SelectedItem = FrequencyTab; FrequencyManager.SelectEntry(id); };
        Spectrum.MarkerMoveRequested += (id,oldHz,newHz)=>{SidebarTabs.SelectedItem=FrequencyTab;FrequencyManager.CorrectMarker(id,oldHz,newHz);};
        RefreshInputs(this, new RoutedEventArgs());
        try
        {
            var path = Path.Combine(dataFolder, "settings.json");
            if (File.Exists(path))
            {
                var settings = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path));
                if (settings != null)
                {
                    AutoBandMode.IsChecked = settings.AutoBand; CwPitch.Value = double.IsFinite(settings.CwPitch) ? Math.Clamp(settings.CwPitch, 300, 1200) : 700;
                    savedSampling = settings.SamplingByReceiver?.Where(p => SamplingModes.Valid(p.Value)).ToDictionary(p => p.Key, p => p.Value) ?? new();
                    savedOutputId = settings.OutputId;
                    savedCaptureOffsets = settings.CaptureOffsets ?? new();
                    savedReceiverIdentity = settings.RtlIdentity; AutoReconnectCheck.IsChecked = settings.AutoReconnect;
                    SidebarColumn.Width = new GridLength(double.IsFinite(settings.SidebarWidth) ? Math.Clamp(settings.SidebarWidth,260,520) : 330);
                    VolumeSlider.Value = Math.Clamp(settings.Volume, 0, 1);
                    SquelchSlider.Value = Math.Clamp(settings.Squelch, -90, 0);
                    RfSquelchSlider.Value = float.IsFinite(settings.RfSquelch) ? Math.Clamp(settings.RfSquelch, -119, 0) : -60;
                    RfSquelchEnabled.IsChecked = RfSquelch.RestoreEnabled(settings.RfSquelchOn, settings.RfSquelch);
                    ReceiverFamily.SelectedIndex = settings.Backend == "HackRF" ? 1 : 0; PopulateReceiverRates(settings.SampleRate); try { ReflectHackRfGain((HackRfGain.Decode(settings.HackRfGainToken) with { Amplifier = false }).Token); } catch (ArgumentException) { ReflectHackRfGain(new HackRfGain().Token); }
                    FrequencyBox.Text = settings.Frequency ?? "146.520000";
                    FmMode.SelectedIndex = Math.Clamp(settings.Mode, 0, ReceiveDemodulator.Modes.Length-1);
                    voiceGainSlider.Value=Math.Clamp(double.IsFinite(settings.P25VoiceGainDb)?settings.P25VoiceGainDb:12,0,30); PopulateBandwidth(settings.Bandwidth); ChannelAgc.IsChecked = settings.AudioAgc; ForceMonoBox.IsChecked = settings.ForceMono; RdsRegion.SelectedIndex = Math.Clamp(settings.RdsRegion,0,1); Packets.DecoderEnabled=settings.PacketDecoding; P25Packets.P25Enabled=settings.P25Decoding; Packets.SubaudibleEnabled=settings.SubaudibleDecoding; PagingPackets.PagingEnabled=settings.PagingDecoding; PagingPackets.PagingGrouped=settings.GroupPagingRepeats; P25Packets.Grouped=settings.GroupP25Repeats; P25Packets.HiddenP25Types=settings.HiddenP25Types??[];
                    RtlGain.SelectedIndex = Math.Clamp(settings.Gain, 0, 5);
                    var matches = InputDevices.Items.Cast<Device>().Where(d => d.Name == settings.InputName).ToArray();
                    if (matches.Length == 1) InputDevices.SelectedItem = matches[0];
                }
            }
        }
        catch (Exception ex) { Log("Settings could not be restored: " + ex.Message); }
        engine.Frame += (values, level) => { lock (frameSync) { audioLevel = level; latestBroadcastAudio = values; if (!rfMode) latest = (values, level, DisplaySettings.Clock, 0); } };
        receiver.Spectrum += values => { lock (frameSync) latest = (values, audioLevel, DisplaySettings.Clock, receiver.AppliedFrequency); };
        receiver.StereoAudio += pcm => { lock(frameSync) latestStereoWaveform = pcm; if((fleetProfile?.Role!="Trunk voice"||fleetPaused)&&!trunkRadio.Running&&((!voiceIds.Voice&&!digitalVoice.Enabled)||receiver.AppliedChannel?.Mode!="NFM"))engine.FeedStereo(pcm, receiver.SquelchOpen); };
        receiver.ReplayGap += () =>
        {
            engine.RetuneBoundary(); long gapSession = Interlocked.Read(ref session);
            Dispatcher.BeginInvoke(new Action(() => { if (!closing && gapSession == session) { Spectrum.Clear(); lock (frameSync) latest = null; } }));
        };
        receiver.RecordingStatus += message => Dispatcher.BeginInvoke(new Action(() => { if (!closing) { IqRecordLabel.Text = message; Log(message); } }));
        receiver.Applied += (frequency, rate) =>
        {
            engine.RetuneBoundary();
            long eventSession = Interlocked.Read(ref session);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (closing || !rfMode || eventSession != session || receiver.AppliedFrequency != frequency) return;
                ResetActivity("Capture settings changed; scanner stopped and floor recalibrating.");
                activeRate = rate; activeGain = receiver.RequestedGain;
                if (receiver.IsReplay) { updatingControls = true; SamplingModeBox.SelectedIndex = receiver.SamplingMode; updatingControls = false; RefreshSamplingControls(); }
                updatingControls = true;
                try
                {
                    if (receiver.IsReplay && SelectedBackend != receiver.Backend) { ReceiverFamily.SelectedIndex = receiver.IsHackRf ? 1 : 0; RtlDevices.ItemsSource = null; PopulateReceiverRates(rate); RefreshSamplingControls(); } ReflectRate(rate); if (receiver.IsHackRf) ReflectHackRfGain(activeGain);
                    foreach (ComboBoxItem item in RtlGain.Items) if (int.Parse(item.Tag.ToString()!) == activeGain) RtlGain.SelectedItem = item;
                }
                finally { updatingControls = false; }
                if (recoveryPlan != null) recoveryPlan = recoveryPlan with { Frequency = receiver.AppliedChannel?.Frequency ?? frequency, Rate = rate, Gain = activeGain };
                bool firstView = !Spectrum.IsRf;
                Spectrum.SetRf(frequency, rate, !receiver.IsReplay, receiver.IsHackRf ? 0 : receiver.AppliedBandwidth);
                ReflectChannel();
                SourceLabel.Text = $"{(receiver.IsReplay ? "I/Q REPLAY" : receiver.ReceiverName)} · capture {frequency / 1000000.0:F6} MHz";
                Log($"Applied capture {frequency} Hz at {rate} S/s; gain {receiver.GainDescription}.");            }));
        };
        receiver.ChannelChanged += () =>
        {
            engine.RetuneBoundary(); long changeSession = Interlocked.Read(ref session);
            Dispatcher.BeginInvoke(new Action(() => { if (!closing && rfMode && changeSession == session) ReflectChannel(); }));
        };
        receiver.Status += message => PostReceiverMessage(message, false);
        receiver.Ended += message => PostReceiverMessage(message, true);
        engine.Ended += message =>
        {
            long eventSession = Interlocked.Read(ref session);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (closing || eventSession != session) return;
                Stop(); Log(message);
            }));
        };
        timer.Tick += (_, _) =>
        {
            TickFilePlans();RefreshRxTxIndicator();if(DecoderSetupExpander.IsExpanded)RefreshDecoderSetup();
            ClockLabel.Text = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd  HH:mm:ss 'UTC'");
            (float[] Values, double Level, double Time, long Center)? frame;
            lock (frameSync) { frame = latest; latest = null; }
            if (frame is { } item && (!rfMode || item.Center == receiver.AppliedFrequency)) { Spectrum.PushAt(item.Values, item.Time); ProcessActivity(item.Values,item.Time); LevelLabel.Text = $"{(rfMode ? "Audio" : "Input")}: {item.Level:F1} dBFS"; }
            RefreshActivity();
            RefreshFolderScan(frame is {} fresh && fresh.Center==receiver.AppliedFrequency ? fresh.Time : null);
            SquelchSlider.IsEnabled = !rfMode;
            RfSquelchStatus.Text = rfMode ? $"RF gate: {(!receiver.SquelchEnabled ? "OFF" : receiver.SquelchOpen ? "OPEN" : "CLOSED")} · channel {receiver.ChannelDb:F1} dB rel." : "RF gate: stopped";
            IqRecordButton.IsEnabled = rfMode && !receiver.IsReplay && receiver.AppliedFrequency != 0 && !receiver.IsRecording;
            FinishIqButton.IsEnabled = receiver.IsRecording;
            RefreshTriggeredRecording();
            if(automaticIq.Armed)IqRecordButton.IsEnabled=false;
            if (receiver.IsRecording) IqRecordLabel.Text = $"I/Q: {receiver.RecordedBytes / 1000000.0:0.0} MB · approx. {activeRate * 120.0 / 1000000:0} MB/min";
            RefreshPlayback();
            RefreshBroadcast();
            RefreshPackets();TickTrunk();TickFleet();TickCalibration();TickSiteSaving();TickMonitoringViews();TickRadioData();TickFoxHunt();
            Spectrum.Tick();
        };
        timer.Start();
        RtlGain.SelectionChanged += async (_, _) => await ApplyLiveControl(false);
        SampleRateBox.SelectionChanged += async (_, _) => await ApplyLiveControl(true);
        FmMode.SelectionChanged += (_, _) => { if (!ready || updatingControls) return; PopulateBandwidth(); ApplyChannelSelection(); };
        ChannelBandwidth.SelectionChanged += (_, _) => ApplyChannelSelection();
        ChannelAgc.Checked += (_, _) => ApplyChannelSelection(); ChannelAgc.Unchecked += (_, _) => ApplyChannelSelection();
        OutputDevices.SelectionChanged += (_, _) => { if (!ready || refreshingOutputs) return; savedOutputId = (OutputDevices.SelectedItem as OutputEndpoint)?.Id; ListeningChanged(this, new RoutedEventArgs()); };
        RestoreOutput();
        SamplingModeBox.SelectionChanged += (_, _) => SamplingSelectionChanged();
        RtlDevices.SelectionChanged += (_, _) => { RestoreSamplingForSelection(); RestoreCaptureOffset(); };
        RefreshSamplingControls();
        InitializeReceiverRefinements();
        InitializeTriggeredRecording();
        InitializeDecoderSetup();
        ready = true;
        RfSquelchChanged(this, new RoutedEventArgs());
        ListeningChanged(this, new RoutedEventArgs());
        Log("Ready. No reception started. Monitoring enabled when the selected output is available.");
        Closing += (_, _) =>
        {
            StopFolderScan("Folder scan stopped on close.");
            StopActivity("Scanner stopped on close.");
            closing = true;if(Application.Current.MainWindow==this)Application.Current.MainWindow=FleetWindows().FirstOrDefault(w=>w!=this)??this;try{FlushSiteSaving();}catch(Exception ex){Log("Final site save: "+ex.Message);}ReleaseFleetAssignments(); trunkRadio.Dispose();Sensors.Release(); automaticIq.Dispose(); automaticAudio.Dispose(); siteCollector?.Flush(true); P25Systems.Save(); HdRecordings.Release(); Television.Release(); FrequencyManager.Release(); CancelRecovery(); session++; timer.Stop(); DisplayControls.Save(); voiceIds.Dispose(); digitalVoice.Dispose(); pagingDecoder.Dispose(); Aviation.Release(); Dmr.Release(); receiver.Dispose(); engine.Dispose(); ReceiverOwnership.Release(this);
            try
            {
                ActivityControls.Save();
                var preferences = new Preferences((float)VolumeSlider.Value, (float)SquelchSlider.Value, (InputDevices.SelectedItem as Device)?.Name, FrequencyBox.Text, FmMode.SelectedIndex, RtlGain.SelectedIndex, manualActivityThreshold ?? (float)RfSquelchSlider.Value, RfSquelchEnabled.IsChecked == true, SelectedSampleRate, savedReceiverIdentity, AutoReconnectCheck.IsChecked == true, savedOutputId, SelectedBandwidth, ChannelAgc.IsChecked == true, savedSampling, ForceMonoBox.IsChecked == true, RdsRegion.SelectedIndex, Packets.DecoderEnabled, P25Packets.P25Enabled, Packets.SubaudibleEnabled, P25Packets.Grouped, P25Packets.HiddenP25Types, SelectedBackend, new HackRfGain(Math.Max(0,HackRfLna.SelectedIndex)*8,Math.Max(0,HackRfVga.SelectedIndex)*2,false,HackRfHfFilter.IsChecked==true).Token, savedCaptureOffsets, SidebarColumn.ActualWidth, PagingPackets.PagingEnabled, PagingPackets.PagingGrouped, AutoBandMode.IsChecked == true, CwPitch.Value, voiceGainDb);
                string target = Path.Combine(dataFolder, "settings.json");
                File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(target + ".tmp", target, true);
            }
            catch (Exception ex) { Log("Could not save settings: " + ex.Message); }
        };
    }
    private void SaveFrequency(object sender, RoutedEventArgs e) { SidebarTabs.SelectedItem = FrequencyTab; FrequencyManager.AddSignal(null); }
    private async void TuneSaved(FrequencyItem item)
    {
        if(trunkRadio.Running)StopTrunk(false);
        StopFolderScan("Folder scan stopped for receiver control.");
        StopActivity("Scanner stopped for saved-frequency tuning.");
        if (!ReceiveDemodulator.Modes.Contains(item.Mode)) { Log($"Cannot tune '{item.Name}': mode {item.Mode} is not implemented."); return; }
        if (receiver.IsReplay) { Log("Replay follows recorded channel settings; saved entry remains available."); return; }
        if (!SelectedCanTune(item.FrequencyHz)) { Log("Saved frequency is outside current receiver coverage."); return; }
        double width = item.BandwidthHz == 0 ? ChannelSettings.DefaultWidth(item.Mode) : item.BandwidthHz;
        if (!ChannelSettings.Widths(item.Mode).Contains(width)) { Log("Saved filter width is unsupported for this mode; entry retained."); return; }
        updatingControls = true;
        try { FmMode.SelectedIndex = Array.IndexOf(ReceiveDemodulator.Modes, item.Mode); PopulateBandwidth(width); FrequencyBox.Text = (item.FrequencyHz / 1000000m).ToString("F6", CultureInfo.InvariantCulture); }
        finally { updatingControls = false; }
        if (rfMode && receiver.AppliedChannel != null)
        {
            var channel = SelectedChannel(item.FrequencyHz);
            if (receiver.IsHackRf && receiver.Offset.Enabled)
            { try { await receiver.Retune(item.FrequencyHz, channel); } catch (Exception ex) { Log(ex.Message); } return; }
            if (channel.Fits(receiver.AppliedFrequency, receiver.AppliedBandwidth)) { receiver.SetChannel(channel); return; }
            try { await receiver.Retune(item.FrequencyHz, channel); } catch(Exception ex){ Log(ex.Message); } return;
        }
        if (RtlDevices.SelectedItem is not RtlNative.Device) { Log("Select an SDR receiver before tuning."); return; }
        StartRtl(FrequencyManager, new RoutedEventArgs());
        await Task.CompletedTask;
    }    private void PostReceiverMessage(string message, bool ended)
    {
        long eventSession = Interlocked.Read(ref session);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (closing || eventSession != session) return;
            if (ended && receiver.IsReplay) { engine.Stop(); RecordButton.IsEnabled = false; FinishRecordButton.IsEnabled = false; RecordLabel.Text = "Not recording"; RtlStatus.Text = message; Log(message); return; }
            if (ended) { HandleReceiverEnd(message); return; }
            RtlStatus.Text = message; Log(message);
        }));
    }
    private async void SetupRtl(object sender, RoutedEventArgs e)
    {
        if(Application.Current.Windows.OfType<MainWindow>().Any(w=>w!=this && w.rfMode && !w.receiver.IsReplay))
        {Log("Stop the other live receiver before updating the shared RTL library.");return;}
        if (rfMode) Stop(); else CancelRecovery();
        SetRtlSetupBusy(true);
        RtlStatus.Text = "Downloading verified RTL-SDR Blog v1.3.6 x64 library…";
        try { await RtlReceiver.InstallLibrary(); RtlStatus.Text = "Library ready. Click Find to discover receivers."; Log(RtlStatus.Text); }
        catch (Exception ex) { RtlStatus.Text = "Setup failed: " + ex.Message; Log(RtlStatus.Text); }
        finally { SetRtlSetupBusy(false); }
    }
    private void SetRtlSetupBusy(bool busy) { SetupRtlButton.IsEnabled = !busy; FindRtlButton.IsEnabled = !busy; StartRtlButton.IsEnabled = !busy; }
    private async void FindRtl(object sender, RoutedEventArgs e)
    {
        if (rfMode) Stop(); else CancelRecovery();
        SetRtlSetupBusy(true);
        try
        {
            var devices = await RtlReceiver.EnumerateBackend(SelectedBackend);
            RtlDevices.ItemsSource = devices;
            var remembered = ReceiverIdentity.Match(devices, savedReceiverIdentity);
            if (remembered != null) RtlDevices.SelectedItem = remembered;
            else if (devices.Count == 1) RtlDevices.SelectedIndex = 0;
            RtlStatus.Text = RtlDevices.Items.Count == 0 ? "No accessible receiver. Check USB and WinUSB; for PortaPack select HackRF mode. Close other SDR apps." : $"Found {RtlDevices.Items.Count} receiver(s). Select one and Start.";
            Log(RtlStatus.Text);
        }
        catch (Exception ex) { RtlStatus.Text = "Discovery failed: " + ex.Message; Log(RtlStatus.Text); }
        finally { SetRtlSetupBusy(false); }
    }
    private async void StartRtl(object sender, RoutedEventArgs e)
    {
        StopFolderScan("Folder scan stopped for receiver control.");
        StopActivity("Scanner stopped for receiver control.");
        if (RtlDevices.SelectedItem is not RtlNative.Device device || device.Backend != SelectedBackend) { RtlStatus.Text = "Click Find and select a receiver from the current receiver family first."; return; }
        if (!double.TryParse(FrequencyBox.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double mhz) || !double.IsFinite(mhz) || mhz < .000001 || mhz > 6000 || !SelectedCanTune((long)Math.Round(mhz * 1000000)))
        { RtlStatus.Text = "Enter a frequency in " + (SelectedHackRf ? "1–6000 MHz" : SamplingModes.Range(SelectedSamplingMode)) + ". Use a decimal point for MHz."; return; }
        if (!ReferenceEquals(sender, FrequencyManager)) ApplyBandMode((long)Math.Round(mhz * 1000000));
        CaptureOffset offset;
        try { _=SelectedPpm; offset = ReadCaptureOffset(); if (SelectedHackRf) offset.Center(SelectedChannel((long)Math.Round(mhz * 1000000)), SelectedSampleRate, SelectedGain); }
        catch (Exception ex) { RtlStatus.Text = ex.Message; Log(ex.Message); return; }
        int selectedGain = SelectedGain;
        if (!ReferenceEquals(sender, FrequencyManager) && rfMode && !receiver.IsReplay && device.Index == activeDevice && receiver.SamplingMode == SelectedSamplingMode && SelectedSampleRate == activeRate && selectedGain == activeGain && SelectedPpm == receiver.AppliedPpm && ReceiveDemodulator.Modes[FmMode.SelectedIndex] == tunedMode && receiver.AppliedChannel?.Width == SelectedBandwidth && receiver.AppliedChannel?.Agc == (ChannelAgc.IsChecked == true))
        { await LiveTune((long)Math.Round(mhz * 1000000)); return; }
        Stop();
        try
        {
            long frequency = (long)Math.Round(mhz * 1000000);
            tunedHz = frequency; tunedMode = ReceiveDemodulator.Modes[FmMode.SelectedIndex];
            int gain = SelectedGain;
            activeDevice = device.Index; activeRate = SelectedSampleRate; activeGain = gain;
            string? identity = ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(), device);
            if(!ClaimReceiver(device,identity))return;
            savedReceiverIdentity = identity; recoveryAttempts = 0;
            if (identity != null && SelectedHackRf) savedCaptureOffsets[identity] = offset;
            if (identity != null) savedSampling[identity] = SelectedSamplingMode;
            recoveryPlan = identity == null ? null : new RecoveryPlan(identity, frequency, activeRate, gain, FmMode.SelectedIndex == 1, tunedMode, SelectedBandwidth, ChannelAgc.IsChecked == true, SelectedSamplingMode, SelectedBackend, offset, SelectedPpm);
            if (identity == null) Log("Receiver serial is absent, generic or duplicated. Selection is manual; automatic reconnect is unavailable for this receiver.");
            rfMode = true; engine.StartExternal(2);
            Spectrum.SetRf(frequency, activeRate); SpectrumTitle.Text = "RF SPECTRUM / WATERFALL";
            receiver.Start(device.Index, frequency, gain, FmMode.SelectedIndex == 1, sampleRate: activeRate, identity: identity, mode: tunedMode, bandwidth: SelectedBandwidth, agc: ChannelAgc.IsChecked == true, samplingMode: SelectedSamplingMode, backend: SelectedBackend, captureOffset: offset, ppm: SelectedPpm);
            SourceLabel.Text = $"{(receiver.IsReplay ? "I/Q REPLAY" : receiver.ReceiverName)} · {frequency / 1000000.0:F6} MHz";
            RtlStatus.Text = SourceLabel.Text + " · waiting for USB data…";
            Started(); Log($"Starting {device.Name} at {frequency} Hz; {((ComboBoxItem)FmMode.SelectedItem).Content}. Gain, rate, mode and filter selections apply live; Start / Apply starts or reopens the selected source.");
        }
        catch (Exception ex) { Stop(); RtlStatus.Text = "Cannot start receiver: " + ex.Message; Log(RtlStatus.Text); }
    }
    private int SelectedSampleRate => int.Parse(((ComboBoxItem)SampleRateBox.SelectedItem).Tag.ToString()!, CultureInfo.InvariantCulture);
    private async Task LiveTune(long frequency)
    {
        if(trunkRadio.Running)StopTrunk(false);
        StopFolderScan("Folder scan stopped for receiver control.");
        StopActivity("Scanner stopped for manual tuning.");
        if (receiver.IsReplay) { Log("I/Q replay follows recorded frequencies; hardware tuning is disabled."); return; }
        if (!rfMode) { Log("Start an SDR receiver before click-to-tune."); return; }
        if (!receiver.CanTune(frequency)) { Log("Requested frequency is outside the active sampling mode range."); return; }
        long tuningSession = session;
        try
        {
            RtlStatus.Text = $"Tuning to {frequency / 1000000.0:F6} MHz…";
            var channel = (receiver.AppliedChannel ?? SelectedChannel(frequency)) with { Frequency = frequency };
            if (AutoBandMode.IsChecked == true) channel = BandModeDefaults.Apply(channel);
            if (receiver.IsHackRf && receiver.Offset.Enabled) await receiver.Retune(frequency, channel);
            else if (channel.Fits(receiver.AppliedFrequency, receiver.AppliedBandwidth)) receiver.SetChannel(channel);
            else await receiver.Retune(frequency, channel);
        }
        catch (Exception ex)
        {
            if (tuningSession != session) return;
            ReflectChannel(); RtlStatus.Text = ex.Message; Log("Live retune not applied: " + ex.Message);
        }
    }
    private void RefreshInputs(object sender, RoutedEventArgs e)
    {
        try
        {
            InputDevices.Items.Clear();
            RefreshOutputs();
            for (int i = 0; i < WaveIn.DeviceCount; i++) InputDevices.Items.Add(new Device(i, WaveIn.GetCapabilities(i).ProductName));

            if (InputDevices.Items.Count > 0) InputDevices.SelectedIndex = 0;
            LiveButton.IsEnabled = InputDevices.Items.Count > 0;
        }
        catch (Exception ex) { Log("Audio device enumeration failed: " + ex.Message); }
    }
    private void StartLive(object sender, RoutedEventArgs e)
    {
        if (InputDevices.SelectedItem is not Device device) return;
        Stop();
        try { packetAudioSource="Audio input · "+device.Name; packetAudioReplay=false; engine.StartLive(device.Id); SourceLabel.Text = "LIVE AUDIO · " + device.Name; Started(); Log("Input started: " + device.Name); }
        catch (Exception ex) { Stop(); Log("Cannot open input. Check Windows microphone permission and the selected device. " + ex.Message); }
    }
    private void OpenWave(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "WAV audio|*.wav", Title = "Open a WAV recording" };
        if (dialog.ShowDialog(this) == true) PlayFile(dialog.FileName, false);
    }
    private void LoadDemo(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = Path.Combine(dataFolder, "test-tone.wav");
            Stop();
            SelfTests.WriteTone(path, 12);
            PlayFile(path, true);
        }
        catch (Exception ex) { Log("Test tone error: " + ex.Message); }
    }
    private void PlayFile(string path, bool demo)
    {
        Stop();
        try { packetAudioSource=(demo?"Synthetic test · ":"WAV · ")+Path.GetFileName(path); packetAudioReplay=true; engine.StartFile(path); SourceLabel.Text = (demo ? "SYNTHETIC TEST · " : "WAV REPLAY · ") + Path.GetFileName(path); Started(); Log(SourceLabel.Text); }
        catch (Exception ex) { Stop(); Log("Cannot play WAV: " + ex.Message); }
    }
    private void Started() { RecordButton.IsEnabled = true; RefreshButton.IsEnabled = false; InputDevices.IsEnabled = false; }
    private void StopSource(object sender, RoutedEventArgs e) { Stop(!receiver.IsReplay); Log("Source stopped."); }
    private void Stop(bool clearReplay = true)
    {
        if(!weakTxHandoff)weakTransmitWindow?.Halt("Source stopped");
        StopTrunk(false);
        automaticIq.Disarm("Disarmed · receiver stopped");automaticAudio.Disarm("Disarmed - source stopped");
        Volatile.Write(ref triggerGate,null);
        voiceIds.Reset();
        StopFolderScan("Folder scan stopped for receiver control.");
        ResetActivity("Scanner stopped with source.");
        HdRecordings.StopLive("Receiver stopped.");
        Television.StopLive("Receiver stopped.");
        CancelRecovery(); session++; receiver.Stop(); ReceiverOwnership.Release(this); engine.Stop(); rfMode = false;
        pagingDecoder.Reset(); p25Decoder.Reset();
        subaudibleDecoder.Reset();
        if (clearReplay) { loadedIqPath = null; PlaybackPanel.Visibility = Visibility.Collapsed; }
        Spectrum.SetAudio(); SpectrumTitle.Text = "AUDIO SPECTRUM / WATERFALL";
        RefreshBroadcast();
        RtlStatus.Text = "Receiver stopped.";
        RecordButton.IsEnabled = false; FinishRecordButton.IsEnabled = false; RecordLabel.Text = "Not recording";
        RefreshButton.IsEnabled = true; InputDevices.IsEnabled = true;
        SourceLabel.Text = "Stopped · no input opened"; LevelLabel.Text = "Input: — dBFS";
        lock (frameSync) { latest = null; latestBroadcastAudio = null; latestStereoWaveform = null; }
        Spectrum.Clear();
    }
    private void RfSquelchChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        receiver.SquelchThreshold = (float)RfSquelchSlider.Value;
        receiver.SquelchEnabled = RfSquelchEnabled.IsChecked == true;
        RfSquelchSlider.IsEnabled = receiver.SquelchEnabled;
        RfSquelchLabel.Text = $"RF threshold: {receiver.SquelchThreshold:F0} dB relative";
    }
    private void ListeningChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        engine.Volume = (float)VolumeSlider.Value;
        engine.Squelch = (float)SquelchSlider.Value;
        SquelchLabel.Text = $"Audio input gate: {engine.Squelch:F0} dBFS";
        try
        {
            engine.SetEndpointMonitoring(!hdListening && !tvActive && MonitorCheck.IsChecked == true && OutputDevices.SelectedItem is OutputEndpoint, (OutputDevices.SelectedItem as OutputEndpoint)?.Id);
            OutputDevices.IsEnabled = true;
            OutputStatus.Text = engine.Monitor ? "Monitoring: " + ((OutputEndpoint)OutputDevices.SelectedItem).Name : MonitorCheck.IsChecked == true ? "Select an available output to enable monitoring." : "Monitoring off.";
            if(tvActive)OutputStatus.Text="Analog monitoring paused while TV decoding is active.";
            else if(hdListening)OutputStatus.Text="Analog monitoring paused while HD plays on the Windows default output.";
        }
        catch (Exception ex) { MonitorCheck.IsChecked = false; Log("Monitoring unavailable: " + ex.Message); }
    }
    private void StartRecording(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "WAV audio|*.wav", FileName = $"MRS-{workspaceSlot}-{DateTime.Now:yyyyMMdd-HHmmss}.wav", Title = "Record original input audio" };
        if (dialog.ShowDialog(this) != true) return;
        try { engine.Record(dialog.FileName); RecordLabel.Text = "Recording · " + Path.GetFileName(dialog.FileName); RecordButton.IsEnabled = false; FinishRecordButton.IsEnabled = true; Log("Recording started: " + dialog.FileName); }
        catch (Exception ex) { Log("Recording failed: " + ex.Message); }
    }
    private void FinishRecording(object sender, RoutedEventArgs e)
    {
        try { engine.FinishRecording(); RecordLabel.Text = "WAV saved"; RecordButton.IsEnabled = engine.Active; FinishRecordButton.IsEnabled = false; Log("Recording finalized."); }
        catch (Exception ex) { Log("Recording could not be finalized: " + ex.Message); }
    }
    private void Log(string message)
    {
        string line = $"{DateTimeOffset.UtcNow:HH:mm:ss}Z  {message}{Environment.NewLine}";
        LogBox.AppendText(line);
        if (LogBox.Text.Length > 30000) LogBox.Text = LogBox.Text[^20000..];
        LogBox.ScrollToEnd();
        try { File.AppendAllText(Path.Combine(dataFolder, $"session-{DateTime.UtcNow:yyyyMMdd}.log"), line); } catch { /* UI remains the fallback when storage is unavailable. */ }
    }
    internal async Task RenderSmokeTest(string path)
    {
        ReceiverAutoSquelch.IsChecked=true;
        ReceiverAutoMargin.Value=11;
        if(!ActivityControls.Options.AutoSquelch||ActivityControls.Options.MarginDb!=11||activity.Running)throw new InvalidOperationException("Receiver auto squelch failed without scanning.");
        ActivityControls.DisableAuto();
        if(ReceiverAutoSquelch.IsChecked==true)throw new InvalidOperationException("Scan/receiver auto controls lost synchronization.");
        FmMode.SelectedIndex=5;
        if(CwPitchPanel.Visibility!=Visibility.Visible||SelectedBandwidth!=500)throw new InvalidOperationException("CW controls not visible.");
        CwPitch.Value=800;
        if(receiver.CwPitchHz!=800)throw new InvalidOperationException("CW pitch not applied.");
        FmMode.SelectedIndex=0;

        LoadDemo(this, new RoutedEventArgs());
        await Task.Delay(1500);
        UpdateLayout();
        if (!engine.Active || !SourceLabel.Text.StartsWith("SYNTHETIC TEST")) throw new InvalidOperationException("UI did not start test-tone playback.");
        Spectrum.SetPointer(new Point(52 + (Spectrum.ActualWidth - 72) * 1500 / 24000.0, 85));
        await Task.Delay(100);
        var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        image.Render(this);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        using (var stream = File.Create(path)) encoder.Save(stream);
        Stop();
        Spectrum.SetRf(146520000); SpectrumTitle.Text = "RF SPECTRUM / WATERFALL · SYNTHETIC TEST";
        SelectWorkspace(HdRadioTab); UpdateLayout();
        var hdImage=new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);
        hdImage.Render(this);var hdEncoder=new System.Windows.Media.Imaging.PngBitmapEncoder();hdEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(hdImage));
        using(var hdFile=File.Create(Path.Combine(Path.GetDirectoryName(path)!,"ui-hd-smoke.png")))hdEncoder.Save(hdFile);
        SelectWorkspace(TvTab);UpdateLayout();
        var tvImage=new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);
        tvImage.Render(this);var tvEncoder=new System.Windows.Media.Imaging.PngBitmapEncoder();tvEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(tvImage));
        using(var tvFile=File.Create(Path.Combine(Path.GetDirectoryName(path)!,"ui-tv-smoke.png")))tvEncoder.Save(tvFile);
        WorkspaceTabs.SelectedIndex=0;
        RtlStatus.Text = "Synthetic I/Q display test · no radio connected";
        var demoFolder = new FrequencyItem { Name = "Synthetic test signals", IsFolder = true };
        var fixture = new FrequencyLibrary(Path.Combine(dataFolder, "frequencies.json"));
        fixture.Upsert(demoFolder);
        var demoEntry = new FrequencyItem { ParentId = demoFolder.Id, Name = "Demo FM signal", FrequencyHz = 146520000, Mode = "NFM", Modulation = "FM", Notes = "Synthetic marker test; not a verified station." };
        fixture.Upsert(demoEntry);
        FrequencyManager.Initialize(Path.Combine(diagnosticWorkspace ? dataFolder : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS"), "frequencies.json"), diagnosticWorkspace ? null : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS", "ReceiverB", "frequencies.json"));
        SidebarTabs.SelectedItem = FrequencyTab; FrequencyManager.SelectEntry(demoEntry.Id);
        StartFolderScan(demoFolder,[demoEntry]);
        if(FolderRunning)throw new InvalidOperationException("Folder scan started without live hardware.");
        FrequencyManager.ScanControls.Display("Railroad · 1/3 · Listening (synthetic UI preview)\nRoad channel · 160.230 MHz · NFM · 12.5 kHz",false);
        var testRf = FmDemodulator.RfSpectrum(SelfTests.MakeFm(false, 2048));
        double testNow = DisplaySettings.Clock;
        for (int n = 0; n <= 600; n++) Spectrum.PushAt(testRf, testNow - 30 + n * .05);
        Spectrum.SetPointer(new Point(52 + (Spectrum.ActualWidth - 72) * .5, 85));
        await Task.Delay(100);
        var rfImage = new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        rfImage.Render(this);
        var rfEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        rfEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rfImage));
        using (var stream = File.Create(Path.Combine(Path.GetDirectoryName(path)!, "ui-rf-smoke.png"))) rfEncoder.Save(stream);
        var editor = new FrequencyEditor(demoEntry, fixture.Items) { Owner = this, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -16000 };
        editor.Show();
        await Task.Delay(100); editor.UpdateLayout();
        var editorImage = new System.Windows.Media.Imaging.RenderTargetBitmap((int)editor.ActualWidth, (int)editor.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        editorImage.Render(editor);
        var editorEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        editorEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(editorImage));
        using (var stream = File.Create(Path.Combine(Path.GetDirectoryName(path)!, "ui-editor-smoke.png"))) editorEncoder.Save(stream);
        editor.Close();
        SidebarTabs.SelectedItem = DisplayTab;
        foreach (string themeName in new[] { "Dark", "Light", "High contrast", "Amber", "Hot signals" })
        {
            Spectrum.Configure(DisplaySettings.Theme(themeName) with { Zoom = 4, PeakHold = true, HistorySeconds = 30 });
            double now = DisplaySettings.Clock;
            Spectrum.PushAt(testRf, now);
            await Task.Delay(100); UpdateLayout();
            var themeImage = new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            themeImage.Render(this);
            var themeEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            themeEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(themeImage));
            using var stream = File.Create(Path.Combine(Path.GetDirectoryName(path)!, "ui-theme-" + themeName.Replace(' ', '-') + ".png")); themeEncoder.Save(stream);
        }
        Stop();
        SidebarTabs.SelectedIndex = 0; SampleRateBox.SelectedIndex = 1;
        RtlDevices.ItemsSource = new[] { new RtlNative.Device(0, "SYNTHETIC worker · no receiver opened") }; RtlDevices.SelectedIndex = 0;
        FmMode.SelectedIndex = 0; RtlGain.SelectedIndex = 0;
        activeDevice = 0; activeRate = 960000; activeGain = -1000; tunedMode = "NFM";
        tunedHz = 0; rfMode = true; engine.StartExternal(2);
        receiver.Start(0, 146520000, -1000, false, fixture: true, sampleRate: 960000, fixtureSeconds: 10);
        async Task WaitForTune(long hz)
        {
            for (int attempt = 0; attempt < 150 && tunedHz != hz; attempt++) await Task.Delay(20);
            if (tunedHz != hz || !rfMode) throw new InvalidOperationException("UI live tuning did not acknowledge " + hz);
        }
        await WaitForTune(146520000); int workerId = receiver.WorkerId;
        RfSquelchSlider.Value = -72; RfSquelchEnabled.IsChecked = false;
        if (receiver.SquelchEnabled || RfSquelchSlider.IsEnabled || receiver.SquelchThreshold != -72) throw new InvalidOperationException("Squelch checkbox did not preserve threshold while bypassing.");
        RfSquelchEnabled.IsChecked = true;
        if (!receiver.SquelchEnabled || !RfSquelchSlider.IsEnabled) throw new InvalidOperationException("Squelch did not re-enable.");
        engine.Record(Path.Combine(dataFolder, "live-retune-smoke.wav"));
        string iqSmokePath = Path.Combine(dataFolder, "record-replay-smoke.mrsiq");
        receiver.RecordIq(iqSmokePath, new IqMetadata(3, DateTimeOffset.UtcNow, false, -1000, null));
        FrequencyBox.Text = "146.550000"; StartRtl(this, new RoutedEventArgs()); await WaitForTune(146550000);
        Spectrum.ResetView(); UpdateLayout();
        Spectrum.ClickAt(new Point(52 + (Spectrum.ActualWidth - 72) * .25, 80)); await WaitForTune(146280000);
        if (receiver.WorkerId != workerId || !engine.Active || FrequencyBox.Text != "146.280000") throw new InvalidOperationException("UI tuning restarted receiver or failed to update controls.");
        async Task WaitUi(Func<bool> condition)
        {
            for (int i = 0; i < 200 && !condition(); i++) await Task.Delay(20);
            if (!condition()) throw new InvalidOperationException("Milestone control did not acknowledge its UI change.");
        }
        RtlGain.SelectedIndex = 2; await WaitUi(() => receiver.AppliedGain == 100);
        SampleRateBox.SelectedIndex = 2; await WaitUi(() => receiver.AppliedRate == 1920000 && activeRate == 1920000);
        FmMode.SelectedIndex = 3; await WaitUi(() => receiver.AppliedChannel?.Mode == "USB");
        // Receiver state is published before its queued UI reflection. Let that settle before the next synthetic edit.
        await Task.Delay(100);await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        ChannelBandwidth.SelectedIndex = 0; await WaitUi(() => receiver.AppliedChannel?.Width == 1800);
        ChannelAgc.IsChecked = false; await WaitUi(() => receiver.AppliedChannel?.Agc == false);
        if (receiver.WorkerId != workerId || !receiver.IsRecording) throw new InvalidOperationException("Live controls interrupted the worker or recording.");
        SpectrumTitle.Text = "RF SPECTRUM / WATERFALL · SYNTHETIC CHANNEL TEST";
        DisplayControls.SelectViewSpan(60000); Spectrum.CenterView(); await Task.Delay(100); UpdateLayout();
        void Capture(string name)
        {
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(this);
            var png = new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(Path.GetDirectoryName(path)!, name)); png.Save(output);
        }
        Capture("ui-live-tune.png"); SidebarTabs.SelectedItem = DisplayTab; UpdateLayout(); Capture("ui-bandwidth.png");
        await Task.Delay(650);
        Stop();
        PlayIq(iqSmokePath, true); await WaitUi(() => receiver.ReplayReady); SidebarTabs.SelectedItem = RecordingTab;
        ReplayPlay(this, new RoutedEventArgs()); await WaitUi(() => receiver.AppliedFrequency != 0);
        ReplayPause(this, new RoutedEventArgs()); await Task.Delay(100); RefreshPlayback(); UpdateLayout();
        if (!receiver.IsReplay || receiver.WorkerId != 0 || !rfMode || !SpectrumTitle.Text.Contains("I/Q REPLAY") || IqRecordButton.IsEnabled || PlaybackPanel.Visibility != Visibility.Visible)
            throw new InvalidOperationException("I/Q replay UI did not enter receiver-free playback mode.");
        ReplaySeek.Value = Math.Min(.2, receiver.ReplayDuration.TotalSeconds / 2); await ApplySeek();
        await Task.Delay(100); RefreshPlayback(); UpdateLayout(); Capture("ui-iq-replay.png");
        ReplayStop(this, new RoutedEventArgs());
        if (loadedIqPath == null || PlaybackPanel.Visibility != Visibility.Visible) throw new InvalidOperationException("Stop discarded the loaded I/Q file.");
        ReplayPlay(this, new RoutedEventArgs()); await WaitUi(() => receiver.ReplayReady);
        await WaitUi(() => receiver.ReplayFinished); await Task.Delay(50);
        if (loadedIqPath == null || PlaybackPanel.Visibility != Visibility.Visible || !ReplayPlayButton.IsEnabled) throw new InvalidOperationException("Playback controls disappeared at EOF.");
        Stop(); await RenderHackRfSmoke(Capture); await RenderSamplingSmoke(Capture); await RenderBroadcastSmoke(Capture); await RenderPacketSmoke(Capture); await RenderP25Smoke(Capture); await RenderSubaudibleSmoke(Capture); await RenderDualReceiverSmoke(Capture);
        await RenderSpectrumDetailSmoke(Capture);
        await RenderActivitySmoke(Capture);
        await RenderPagingSmoke(Capture);
        await RenderVoiceIdSmoke(Capture);
        await RenderDigitalVoiceSmoke(Capture);
        RenderFilePlanSmoke(Capture);
        SidebarTabs.SelectedItem=RecordingTab;WorkspaceTabs.SelectedIndex=0;RefreshTriggeredRecording();UpdateLayout();Capture("ui-triggered-recording.png");
        SidebarTabs.SelectedIndex=0;ReceiverAutoSquelch.IsChecked=true;ReceiverAutoMargin.Value=8;UpdateLayout();Capture("ui-receiver-auto-squelch.png");
        ReceiverAutoSquelch.IsChecked=false;
        Spectrum.SetRf(160900000,2400000);Spectrum.Configure(Spectrum.Settings with{ShowNifogPlan=false,ShowBandPlan=false,ShowArrlPlan=false,ShowTvPlan=false});Spectrum.InvalidateVisual();UpdateLayout();Capture("ui-railroad-plan.png");
        Aviation.Observe(AviationData.Parse(AviationTests.PositionJson,"adsb",AviationTests.Context)!);
        Aviation.Observe(AviationData.Parse(AviationTests.PositionJson.Replace("ABCDEF","123456").Replace("40.5","40.7").Replace("84.2","84.5"),"adsb",AviationTests.Context)!);
        Aviation.ShowMapForTest();Aviation.Refresh();WorkspaceTabs.SelectedItem=AviationTab;UpdateLayout();Capture("ui-aviation.png");
        RenderTrunkSmoke(Capture);RenderAviationUpdateSmoke(Capture);RenderUpdate036Smoke(Capture);await RenderUpdate037Smoke(Capture);await RenderUpdate039Smoke(Capture);await RenderUpdate040Smoke(Capture);
        SelectWorkspace(ObservationsTab);AnalysisPackets.ClearRows();AnalysisPackets.SetFilter("");
        var flood=new PacketObservation(DateTimeOffset.UtcNow,"Synthetic DMR",true,451600000,null,"Slot Golay + BPTC valid","","","","Slot identification","CC 0 slot 1 data type 3",null,"ABCD","DMR",true);
        for(int batch=0;batch<100;batch++){for(int n=0;n<100;n++)AnalysisPackets.Add(flood with{ObservedUtc=DateTimeOffset.UtcNow});if(batch%10==0){WorkspaceTabs.SelectedIndex=0;UpdateLayout();SelectWorkspace(ObservationsTab);}await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);}
        if(AnalysisPackets.Count!=500||AnalysisPackets.VisibleCount!=1)throw new InvalidOperationException("DMR Data flood grouping failed");await Task.Delay(250);UpdateLayout();Capture("ui-dmr-data-flood.png");
        await Dmr.Smoke();Dmr.DiscoverySmoke(Capture,()=>SelectWorkspace(DmrTab));SelectWorkspace(DmrTab);UpdateLayout();Capture("ui-dmr.png");
        Sensors.ShowSmoke(Capture,()=>{SelectWorkspace(SensorsTab);UpdateLayout();});
        WorkspaceTabs.SelectedItem=SessionLogTab;UpdateLayout();Capture("ui-session-log.png");WorkspaceTabs.SelectedIndex=0;
    }
}

















