using System.IO;
using NAudio.Wave;

namespace MRS;
public static class SelfTests
{
    public static void WriteTone(string path, int seconds)
    {
        using var writer = new WaveFileWriter(path, AudioEngine.Format);
        for (int i = 0; i < AudioEngine.Rate * seconds; i++) writer.WriteSample((float)(.5 * Math.Sin(2 * Math.PI * 1500 * i / AudioEngine.Rate)));
    }
    public static string Run()
    {
        var lines = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL: " + name); lines.Add("PASS: " + name); }
        Update039Tests.Run(Check);Update038Tests.Run(Check);Update037Tests.Run(Check);Update036Tests.Run(Check);SensorTests.Run(Check);
        ReceiverRefinementTests.Run(Check);
        PagingTests.Run(Check);
        PagingManagementTests.Run(Check);
        DataInspectionTests.Run(Check);
        AviationUpdateTests.Run(Check); TriggeredRecordingTests.Run(Check); FileBandPlanTests.Run(Check); AviationTests.Run(Check); DmrTests.Run(Check); TrunkTests.Run(Check);
        ReceiverFeatureTests.Run(Check);
        TvTests.Run(Check);
        RfSpectrumTests.Run(Check);
        ActivityTests.Run(Check);
        Check(!RfSquelch.RestoreEnabled(null, -120) && RfSquelch.RestoreEnabled(null, -60) && !RfSquelch.RestoreEnabled(false, -60) && RfSquelch.RestoreEnabled(true, -120), "Squelch checkbox migration preserves legacy OFF and explicit enabled state");
        Check(new RfSquelch().Update(-65, -70, .05) && !new RfSquelch().Update(-65, -60, .05), "Left lower threshold opens weaker signals; right higher threshold is tighter");
        var clickView = new SpectrumView(); clickView.Measure(new System.Windows.Size(872, 470)); clickView.Arrange(new System.Windows.Rect(0, 0, 872, 470));
        long clicked = 0; clickView.TuneRequested += hz => clicked = hz;
        clickView.SetRf(146520000, 960000); clickView.Configure(new DisplaySettings { Zoom = 4, Pan = .75 });
        clickView.ClickAt(new System.Windows.Point(252, 80));
        Check(clicked == 146640000, "Click-to-tune uses zoom and pan frequency mapping at selected sample rate");
        clicked = 0; clickView.ClickAt(new System.Windows.Point(252, 300));
        Check(clicked == 146640000, "Waterfall click-to-tune matches spectrum frequency");
        clicked = 0; clickView.ClickAt(new System.Windows.Point(10, 80)); clickView.ClickAt(new System.Windows.Point(252, 80), true);
        Check(clicked == 0, "Axis and Ctrl-click do not trigger tuning");
        clickView.SetAudio(); clickView.ClickAt(new System.Windows.Point(252, 80));
        Check(clicked == 0, "Audio-only spectrum cannot issue radio tuning commands");
        var bandwidthPanel = new DisplayPanel(); DisplaySettings? bandwidthValue = null; bandwidthPanel.Changed += s => bandwidthValue = s;
        bandwidthPanel.SetCaptureSpan(960000); bandwidthPanel.SelectViewSpan(240000);
        Check(bandwidthValue?.Zoom == 4, "Bandwidth selector converts desired span to zoom");
        bandwidthPanel.SelectViewSpan(2000000); Check(bandwidthValue?.Zoom == 1, "Bandwidth selector clamps to captured coverage");
        using (var packetStream = new MemoryStream())
        {
            ReceiverStream.Write(packetStream, new byte[4096], 4096, 146520000, 960000);
            byte[] header = packetStream.ToArray()[..16]; var metadata = ReceiverStream.Header(header);
            Check(metadata.Frequency == 146520000 && metadata.Rate == 960000 && metadata.Count == 4096, "I/Q packets bind frequency and sample rate to their payload");
            header[0] = 0; bool rejected = false; try { ReceiverStream.Header(header); } catch (IOException) { rejected = true; }
            Check(rejected, "Invalid I/Q frame headers are rejected");
        }
        foreach (int rate in new[] { 960000, 1920000, 2400000, 3200000 })
        foreach (bool wideMode in new[] { false, true })
        {
            byte[] highRateIq = MakeFm(wideMode, rate / 5, rate);
            var highDsp = new FmDemodulator(wideMode, rate); byte[] pcm = highDsp.Process(highRateIq);
            Check(pcm.Length == 19200, $"{rate} S/s {(wideMode ? "WFM" : "NFM")} produces exactly 48 kHz audio");
            float[] audio = Enumerable.Range(0, 2048).Select(n => BitConverter.ToInt16(pcm, 4096 + n * 2) / 32768f).ToArray();
            float[] rateSpectrum = SignalMath.Spectrum(audio);
            Check(Math.Abs(Array.IndexOf(rateSpectrum, rateSpectrum.Max()) * 48000.0 / 2048 - 1000) < 24 && SignalMath.Level(audio) > -40, $"{rate} S/s {(wideMode ? "WFM" : "NFM")} recovers the modulation tone");
            var splitDsp = new FmDemodulator(wideMode, rate);
            byte[] splitAudio = splitDsp.Process(highRateIq[..32766]).Concat(splitDsp.Process(highRateIq[32766..])).ToArray();
            Check(pcm.SequenceEqual(splitAudio), $"{rate} S/s {(wideMode ? "WFM" : "NFM")} preserves decimation state across uneven blocks");
        }
        foreach (int rate in new[] { 960000, 1920000, 2400000, 3200000 })
        {
            byte[] outside = new byte[rate / 5];
            for (int n = 0; n < outside.Length / 2; n++)
            { double phase = 2 * Math.PI * 241000 * n / rate; outside[n * 2] = (byte)(127.5 + 90 * Math.Cos(phase)); outside[n * 2 + 1] = (byte)(127.5 + 90 * Math.Sin(phase)); }
            var antiAlias = new FmDemodulator(false, rate); antiAlias.Process(outside); antiAlias.Process(outside);
            Check(antiAlias.ChannelDb < -45, $"{rate} S/s anti-alias filtering rejects an out-of-band carrier that would fold into the tuned channel");
        }
        string tuningRecording = Path.Combine(Path.GetTempPath(), "mrs-live-tune-" + Guid.NewGuid() + ".wav");
        try
        {
            using var liveReceiver = new RtlReceiver(); using var liveAudio = new AudioEngine(); using var done = new ManualResetEventSlim();
            int appliedCount = 0, beforeBytes = 0, afterBytes = 0, firstWorker = 0, lastWorker = 0, frameCount = 0;
            liveAudio.StartExternal(); liveAudio.Record(tuningRecording);
            liveReceiver.Applied += (_, _) => { appliedCount++; liveAudio.RetuneBoundary(); lastWorker = liveReceiver.WorkerId; if (firstWorker == 0) firstWorker = lastWorker; };
            liveReceiver.Audio += pcm =>
            {
                liveAudio.FeedExternal(pcm, liveReceiver.SquelchOpen);
                if (liveReceiver.AppliedFrequency == 146520000) beforeBytes += pcm.Length; else afterBytes += pcm.Length;
                if (++frameCount == 3) liveReceiver.Retune(146550000).GetAwaiter().GetResult();
            };
            liveReceiver.Ended += _ => done.Set();
            liveReceiver.Start(0, 146520000, -1000, false, fixture: true, sampleRate: 960000);
            Check(done.Wait(12000), "Live-tuning fixture completes without hanging");
            liveReceiver.Stop(); liveAudio.FinishRecording();
            Check(appliedCount == 2 && beforeBytes > 0 && afterBytes > 0 && firstWorker == lastWorker, "Retune command acknowledges new frequency and resumes audio in the same worker");
            using var wav = new WaveFileReader(tuningRecording);
            Check(wav.Length == beforeBytes + afterBytes && liveAudio.Active, "Live retune retains one open WAV recording across both frequencies");
        }
        finally { File.Delete(tuningRecording); }
        var rfGate = new RfSquelch();
        Check(!rfGate.Update(-80, -60, .05), "RF squelch rejects a channel below threshold");
        Check(rfGate.Update(-59, -60, .05), "RF squelch opens at channel-power threshold");
        Check(rfGate.Update(-62, -60, 1), "RF hysteresis holds gate within 3 dB closing margin");
        Check(rfGate.Update(-80, -60, .15) && !rfGate.Update(-80, -60, .06), "RF gate closes after 200 ms below closing threshold");
        rfGate.Update(-50, -60, .05); rfGate.Update(-80, -60, .15); rfGate.Update(-50, -60, .05);
        Check(rfGate.Update(-80, -60, .15), "Recovered RF signal resets hang timer");
        Check(rfGate.Update(-120, -120, .05), "Minimum RF threshold explicitly disables squelch");
        Check(!rfGate.Update(double.NaN, -60, .05), "Invalid RF measurement closes gate");
        float[] quietSpeech = Enumerable.Repeat(.0001f, 2048).ToArray();
        Check(SignalMath.ListeningPcm(quietSpeech, -80, -60, 1, true).Any(b => b != 0), "Open RF gate passes audio below the old audio threshold");
        Check(SignalMath.ListeningPcm(Enumerable.Repeat(.5f, 2048).ToArray(), -6, -60, 1, false).All(b => b == 0), "Closed RF gate mutes loud demodulated noise");
        double CarrierPower(double hz, double amplitude)
        {
            byte[] samples = new byte[32768];
            for (int n = 0; n < samples.Length / 2; n++)
            {
                double phase = 2 * Math.PI * hz * n / RtlNative.SampleRate;
                samples[n * 2] = (byte)Math.Round(127.5 + amplitude * Math.Cos(phase));
                samples[n * 2 + 1] = (byte)Math.Round(127.5 + amplitude * Math.Sin(phase));
            }
            var demod = new FmDemodulator(false); demod.Process(samples); demod.Process(samples);
            return demod.ChannelDb;
        }
        double centeredPower = CarrierPower(0, 80), outsidePower = CarrierPower(60000, 80);
        Check(centeredPower > -10 && outsidePower < centeredPower - 30, "RF squelch power measures the filtered channel rather than the full captured band");
        Check(new RfSquelch().Update(centeredPower, -60, .05), "Unmodulated centered carrier keeps RF gate open without speech");
        Check(Math.Abs(CarrierPower(1000, 40) - CarrierPower(1000, 80) + 6.02) < .3, "RF channel power tracks a 6 dB carrier amplitude change");
        string rfRecording = Path.Combine(Path.GetTempPath(), "mrs-rf-gate-" + Guid.NewGuid() + ".wav");
        try
        {
            using var gateEngine = new AudioEngine();
            int frames = 0; gateEngine.Frame += (_, _) => frames++;
            gateEngine.StartExternal(); gateEngine.Record(rfRecording);
            byte[] input = Enumerable.Repeat((byte)42, 8192).ToArray();
            gateEngine.FeedExternal(input, false); gateEngine.FinishRecording();
            using var gateRecordingReader = new WaveFileReader(rfRecording);
            byte[] retained = new byte[input.Length]; int count = gateRecordingReader.Read(retained, 0, retained.Length);
            Check(frames > 0 && count == input.Length && retained.SequenceEqual(input), "Closed RF squelch preserves analysis frames and original WAV samples");
        }
        finally { File.Delete(rfRecording); }
        var fullView = new FrequencyViewport(146400000, 240000, 1, .5);
        var zoomed = fullView.ZoomAt(8, .25);
        Check(Math.Abs(zoomed.Frequency(.25) - fullView.Frequency(.25)) < .001, "Pointer-anchored zoom preserves RF frequency");
        Check(Math.Abs(zoomed.Fraction(zoomed.Frequency(.63)) - .63) < 1e-10, "RF marker coordinate round-trip remains aligned after zoom");
        var atEdge = zoomed.ZoomAt(1, .9);
        Check(atEdge.Start == fullView.CaptureStart && atEdge.Span == fullView.CaptureSpan, "Zoom-out clamps to the captured span");
        var right = new FrequencyViewport(146400000, 240000, 32, 1);
        Check(Math.Abs(right.Frequency(1) - 146640000) < .001, "Pan cannot expose uncaptured RF frequencies");
        var normalized = new DisplaySettings { Zoom = double.NaN, WaterLow = 10, WaterHigh = -130, Background = "invalid", RefreshFps = 999 }.Normalize();
        Check(normalized.Zoom == 1 && normalized.WaterHigh >= normalized.WaterLow + 5 && normalized.RefreshFps == 30 && normalized.Background == "#090F19", "Invalid display settings normalize to safe bounded values");
        var timeline = new WaterfallHistory();
        float[] originalFrame = Enumerable.Repeat(-80f, 1024).ToArray();
        timeline.Add(originalFrame, 10); originalFrame[0] = 10;
        timeline.Add(Enumerable.Repeat(-40f, 1024).ToArray(), 10.1);
        var savedRows = timeline.Snapshot();
        Check(savedRows[0].Value(0) == -80, "Waterfall history owns its data independently of incoming buffers");
        Check(WaterfallHistory.RowAt(savedRows, 10.15) == 1 && WaterfallHistory.RowAt(savedRows, 9) == -1 && WaterfallHistory.RowAt(savedRows, 11) == -1, "Time mapping selects timestamped rows and leaves missing intervals blank");
        timeline.Add(Enumerable.Repeat(-30f, 1024).ToArray(), 150);
        Check(timeline.Snapshot().Length == 1, "Waterfall history discards data outside its retention limit");
        var view = new SpectrumView();
        var displayInput = Enumerable.Repeat(-70f, 1024).ToArray();
        view.PushAt(displayInput, DisplaySettings.Clock); view.Configure(new DisplaySettings { GainDb = 30, Zoom = 8, Pan = .7, PeakHold = true });
        Check(displayInput.All(v => v == -70), "Display gain, zoom and peak hold do not mutate source spectrum samples");
        var grayLow = DisplayPalette.Sample("Grayscale", 0); var grayHigh = DisplayPalette.Sample("Grayscale", 1);
        Check(grayLow.R == 0 && grayHigh.R == 255 && grayHigh.G == 255 && grayHigh.B == 255, "Grayscale palette covers black to white");
        Check(DisplaySettings.Palettes.All(p => DisplayPalette.Sample(p, 0) != DisplayPalette.Sample(p, 1)), "Every waterfall palette distinguishes weak and strong values");
        var themed = (new DisplaySettings { GainDb = 12, Zoom = 4 }).ColorsFrom(DisplaySettings.Theme("Light"));
        Check(themed.GainDb == 12 && themed.Zoom == 4 && themed.Background == "#F4F7FA", "Theme selection preserves display scale and zoom");
        string displayFolder = Path.Combine(Path.GetTempPath(), "mrs-display-test-" + Guid.NewGuid());
        Directory.CreateDirectory(displayFolder);
        try
        {
            string displayFile = Path.Combine(displayFolder, "display.json");
            var panel = new DisplayPanel(); panel.Initialize(displayFile); panel.AcceptViewChange(themed); panel.SavePreset("Day");
            panel.AcceptViewChange(new DisplaySettings { HistorySeconds = 60, RfFftSize = 65536 }); panel.Save();
            var restoredPanel = new DisplayPanel(); DisplaySettings? restoredDisplay = null;
            restoredPanel.Changed += value => restoredDisplay = value;
            restoredPanel.Initialize(displayFile);
            Check(restoredDisplay?.HistorySeconds == 60 && restoredDisplay.RfFftSize == 65536, "Display preferences and RF detail restore after reload");
            restoredPanel.LoadPreset("Day"); restoredPanel.Save();
            Check(restoredDisplay == themed.Normalize(), "Named display preset restores colors, gain and zoom together");
        }
        finally { foreach (string file in Directory.GetFiles(displayFolder)) File.Delete(file); Directory.Delete(displayFolder); }
        string libraryFolder = Path.Combine(Path.GetTempPath(), "mrs-library-test-" + Guid.NewGuid());
        Directory.CreateDirectory(libraryFolder);
        string libraryPath = Path.Combine(libraryFolder, "frequencies.json");
        try
        {
            var library = new FrequencyLibrary(libraryPath);
            Check(library.Items.Count == 0, "New frequency libraries contain no starter folders or signals");
            var folder = new FrequencyItem { Name = "Test folder", IsFolder = true };
            library.Upsert(folder);
            var child = new FrequencyItem { Name = "Nested", IsFolder = true, ParentId = folder.Id };
            library.Upsert(child);
            Check(FrequencyLibrary.TryFrequency("774.30625", out long exact) && exact == 774306250, "Fractional MHz converts exactly to integer Hz");
            Check(!FrequencyLibrary.TryFrequency("774.3062501", out _) && !FrequencyLibrary.TryFrequency("NaN", out _), "Invalid/sub-Hz frequency input rejected");
            var entry = new FrequencyItem { Name = "Control", ParentId = child.Id, FrequencyHz = exact, Mode = "NFM", Protocols = "P25", Signaling = "NAC 0x348", Notes = "Site notes" };
            library.Upsert(entry);
            var restored = new FrequencyLibrary(libraryPath);
            Check(restored.Items.Single(n => n.Id == entry.Id) == entry, "Nested entry metadata survives save/reload");
            library.Upsert(entry with { Id = Guid.NewGuid(), Name = "Another site" });
            Check(library.Items.Count(n => n.FrequencyHz == exact) == 2, "Duplicate frequencies retain distinct entries");
            bool blocked = false;
            try { library.Move(folder.Id, child.Id); } catch (IOException) { blocked = true; }
            Check(blocked && library.Items.Single(n => n.Id == folder.Id).ParentId == null, "Cyclic folder move rejected without changing state");
            library.Upsert(folder with { Visible = false });
            Check(!library.VisibleEntries().Any(n => n.FrequencyHz == exact), "Hidden parent folder hides descendant RF markers");
            library.Delete(folder.Id);
            Check(!library.Items.Any(n => n.Id == entry.Id), "Folder delete includes descendant entries");
            library.Undo();
            Check(library.Items.Any(n => n.Id == entry.Id) && new FrequencyLibrary(libraryPath).Items.Any(n => n.Id == entry.Id), "Undo restores subtree in memory and on disk");
            Check(File.Exists(libraryPath + ".bak"), "Library saves retain previous-file backup");
            var stale = new FrequencyLibrary(libraryPath);
            library.Upsert(entry with { Notes = "Latest notes" });
            blocked = false;
            try { stale.Upsert(entry with { Notes = "Stale notes" }); } catch (IOException) { blocked = true; }
            Check(blocked && new FrequencyLibrary(libraryPath).Items.Single(n => n.Id == entry.Id).Notes == "Latest notes", "Stale window cannot overwrite newer edits");
            library.Upsert(new FrequencyItem { Name = "HF record", FrequencyHz = 7200000, Mode = "LSB" });
            Check(new FrequencyLibrary(libraryPath).Items.Any(n => n.FrequencyHz == 7200000), "Library stores frequencies outside current receiver coverage");
        }
        finally { foreach (string file in Directory.GetFiles(libraryFolder)) File.Delete(file); Directory.Delete(libraryFolder); }
        float[] tone = Enumerable.Range(0, SignalMath.FftSize).Select(i => (float)(.5 * Math.Sin(2 * Math.PI * 1500 * i / AudioEngine.Rate))).ToArray();
        var fft = SignalMath.Spectrum(tone);
        int peak = Array.IndexOf(fft, fft.Max());
        Check(Math.Abs(peak * AudioEngine.Rate / (double)SignalMath.FftSize - 1500) < 24, "FFT locates 1500 Hz tone");
        Check(Math.Abs(SignalMath.Level(tone) - -9.0309) < .02, "RMS level calibration");
        Check(SignalMath.Spectrum(new float[2048]).All(float.IsFinite), "Silence produces finite spectrum");
        Check(SignalMath.ListeningPcm(tone, -9, -5, 1).All(b => b == 0), "Squelch closes below threshold");
        Check(SignalMath.ListeningPcm(tone, -9, -60, 0).All(b => b == 0), "Zero volume silences monitoring");
        var original = (float[])tone.Clone();
        var attenuated = SignalMath.ListeningPcm(tone, -9, -60, .5f);
        Check(Math.Abs(BitConverter.ToInt16(attenuated, 16) / 32768.0 - tone[8] * .5) < .0001, "Monitoring volume scales audio");
        Check(tone.SequenceEqual(original), "Listening gate preserves analysis/recording samples");
        foreach (bool wide in new[] { false, true })
        {
            byte[] iq = MakeFm(wide, 120000);
            var dsp = new FmDemodulator(wide);
            var pcm = dsp.Process(iq);
            Check(pcm.Length == iq.Length / 5, $"{(wide ? "WFM" : "NFM")} output rate is 48 kHz");
            var decoded = new float[2048];
            for (int n = 0; n < decoded.Length; n++) decoded[n] = BitConverter.ToInt16(pcm, 4096 + n * 2) / 32768f;
            var spectrum = SignalMath.Spectrum(decoded);
            int strongest = Array.IndexOf(spectrum, spectrum.Max());
            Check(Math.Abs(strongest * 48000.0 / 2048 - 1000) < 24 && SignalMath.Level(decoded) > -40, $"{(wide ? "WFM" : "NFM")} recovers 1 kHz modulation");
            var split = new FmDemodulator(wide);
            var a = split.Process(iq[..32768]); var b = split.Process(iq[32768..]);
            Check(a.Concat(b).SequenceEqual(pcm), $"{(wide ? "WFM" : "NFM")} filter/decimator state survives USB block boundaries");
        }
        byte[] carrier = new byte[4096];
        for (int n = 0; n < 2048; n++)
        {
            double phase = 2 * Math.PI * 30000 * n / 240000;
            carrier[n * 2] = (byte)(127.5 + 90 * Math.Cos(phase)); carrier[n * 2 + 1] = (byte)(127.5 + 90 * Math.Sin(phase));
        }
        var rf = FmDemodulator.RfSpectrum(carrier);
        Check(Math.Abs((Array.IndexOf(rf, rf.Max()) / 1024.0 - .5) * 240000 - 30000) < 235, "RF FFT places positive-frequency carrier correctly");
        using (var receiver = new RtlReceiver())
        using (var finishedReceiver = new ManualResetEventSlim())
        {
            int frames = 0, audioBytes = 0;
            receiver.Spectrum += _ => Interlocked.Increment(ref frames);
            receiver.Audio += data => Interlocked.Add(ref audioBytes, data.Length);
            receiver.Ended += _ => finishedReceiver.Set();
            receiver.Start(0, 146520000, -1000, false, fixture: true);
            Check(finishedReceiver.Wait(8000), "Isolated I/Q worker reaches EOF without hanging");
            receiver.Stop();
            Check(frames >= 10 && audioBytes > 48000, "Worker pipe drives RF spectrum and demodulated audio");
            receiver.Start(0, 146520000, -1000, false, fixture: true);
            Thread.Sleep(100);
            var stopClock = System.Diagnostics.Stopwatch.StartNew(); receiver.Stop();
            Check(stopClock.Elapsed < TimeSpan.FromSeconds(2), "Stop tears down the owned receiver worker promptly");
        }
        string path = Path.Combine(Path.GetTempPath(), "mrs-test-" + Guid.NewGuid() + ".wav");
        string recorded = path + ".recorded.wav";
        try
        {
            WriteTone(path, 2);
            using (var reader = new WaveFileReader(path))
                Check(reader.WaveFormat.SampleRate == 48000 && reader.WaveFormat.Channels == 1 && Math.Abs(reader.TotalTime.TotalSeconds - 2) < .01, "WAV format and duration");
            using var engine = new AudioEngine();
            using var finished = new ManualResetEventSlim();
            int frames = 0;
            engine.Frame += (_, _) => Interlocked.Increment(ref frames);
            engine.Ended += _ => finished.Set();
            engine.StartFile(path);
            engine.Record(recorded);
            Check(finished.Wait(6000), "WAV playback reaches end");
            engine.Stop();
            Check(frames > 35, "Playback emits analysis frames");
            using (var reader = new WaveFileReader(recorded)) Check(reader.Length > 48000, "Recording finalized with audio data");
            engine.StartFile(path); Thread.Sleep(80); engine.Stop();
            Check(!engine.Active, "Stop cancels playback");
            Check(!engine.Monitor, "Monitoring defaults off");
        }
        finally { File.Delete(path); if (File.Exists(recorded)) File.Delete(recorded); }
        return string.Join(Environment.NewLine, lines) + Environment.NewLine + "Local synthetic tests only; no live RF, audio hardware or PTT tested.";
    }
    internal static byte[] MakeFm(bool wide, int samples, int sampleRate = 240000)
    {
        var iq = new byte[samples * 2]; double phase = 0;
        for (int n = 0; n < samples; n++)
        {
            phase += 2 * Math.PI * (wide ? 40000 : 2500) * Math.Sin(2 * Math.PI * 1000 * n / sampleRate) / sampleRate;
            iq[n * 2] = (byte)Math.Clamp(127.5 + 90 * Math.Cos(phase), 0, 255);
            iq[n * 2 + 1] = (byte)Math.Clamp(127.5 + 90 * Math.Sin(phase), 0, 255);
        }
        return iq;
    }
}






