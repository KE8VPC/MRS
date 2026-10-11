using System.IO;
namespace MRS;
internal static class ReceiverFeatureTests
{
    internal static void Run(Action<bool, string> check)
    {
        HackRfTests.Run(check);
        SubaudibleTests.Run(check);
        DataSummaryTests.Run(check);
        ReceiveModeTests.Run(check);
        MilestoneTests.Run(check);
        SamplingTests.Run(check);
        BroadcastTests.Run(check);
        RdsTests.Run(check);
        HdTests.Run(check);
        PacketTests.Run(check);
        P25Tests.Run(check);
        P25ChannelTests.Run(check);
        P25ControlTests.Run(check);
        P25CatalogTests.Run(check);
        SharedFrequencyTests.Run(check);
        BandPlanTests.Run(check);
        var ownerA=new object();var ownerB=new object();
        try
        {
            check(ReceiverOwnership.Acquire(ownerA,0,"A","A",out _),"First workspace claims a receiver");
            check(!ReceiverOwnership.Acquire(ownerB,1,"A","B",out _),"Identity prevents duplicate receiver use after index changes");
            check(ReceiverOwnership.Acquire(ownerB,0,"B","B",out _),"Distinct identities remain distinct after enumeration changes");
            ReceiverOwnership.Release(ownerB);
            check(!ReceiverOwnership.Acquire(ownerB,0,null,"B",out _),"Unknown identity uses the index to prevent conflicting opens");
            ReceiverOwnership.Release(ownerA);
            check(ReceiverOwnership.Acquire(ownerB,0,null,"B",out _),"Stopping the owner releases the receiver for another workspace");
        }
        finally {ReceiverOwnership.Release(ownerA);ReceiverOwnership.Release(ownerB);}
        var wide = new FrequencyViewport(6000000000, 64000000, 4, .5);
        check(wide.Span == 16000000 && wide.CenterOn(6032000000).Pan == .5, "Wideband viewport retains GHz frequencies and independent visible span");
        check(wide.Drag(100).Start == wide.CaptureStart && wide.Drag(-100).Start + wide.Span == wide.CaptureStart + wide.CaptureSpan, "Dragging clamps both edges to captured coverage");
        var navigation = new SpectrumView(); navigation.Measure(new System.Windows.Size(872, 470)); navigation.Arrange(new System.Windows.Rect(0, 0, 872, 470));
        navigation.SetRf(146520000, 1920000); navigation.Configure(new DisplaySettings { Zoom = 4 });
        long navigationTune = 0; navigation.TuneRequested += hz => navigationTune = hz;
        navigation.BeginDrag(new(452, 80)); navigation.EndDrag(new(552, 80));
        check(navigationTune == 0 && navigation.Settings.Pan < .5, "Spectrum drag pans without issuing a tuning command");
        navigation.BeginDrag(new(452, 80)); navigation.EndDrag(new(453, 80));
        check(navigationTune != 0, "Small pointer movement preserves click-to-tune");
        navigation.CenterView(); check(navigation.Settings.Pan == .5 && navigation.Settings.Zoom == 4, "Center view preserves selected zoom");
        navigation.ResetView(); check(navigation.VisibleSpan == 1920000, "Full span restores all captured coverage");
        var a = new RtlNative.Device(0, "A", "Maker", "RTL", "MRS-A17");
        var b = new RtlNative.Device(1, "B", "Maker", "RTL", "MRS-B18");
        string key = ReceiverIdentity.Key(a)!;
        check(ReceiverIdentity.Match([b with { Index = 0 }, a with { Index = 1 }], key)?.Serial == a.Serial, "Receiver identity follows serial across device index changes");
        check(ReceiverIdentity.UniqueKey([a, a with { Index = 1 }], a) == null, "Duplicate receiver identities require manual selection");
        check(ReceiverIdentity.Key(a with { Serial = "00000001" }) == null && ReceiverIdentity.Key(a with { Serial = "" }) == null, "Blank and common factory serials cannot authorize reconnect");
        check(ReceiverIdentity.Match([b], key) == null && ReceiverIdentity.Match([a with { Product = "Other" }], key) == null, "Missing or mismatched receiver identity does not select a substitute");
        var replayView = new SpectrumView(); replayView.Measure(new System.Windows.Size(872, 470)); replayView.Arrange(new System.Windows.Rect(0, 0, 872, 470));
        long clicked = 0; replayView.TuneRequested += hz => clicked = hz; replayView.SetRf(146520000, 240000, false);
        replayView.ClickAt(new System.Windows.Point(252, 80)); check(clicked == 0, "Replay spectrum suppresses click-to-tune commands");
        int calls = 0; var waits = new List<int>();
        var recovery = ReceiverRecovery.Find(key, 0, () => Task.FromResult(++calls < 3 ? new List<RtlNative.Device>() : [a with { Index = 9 }]),
            (ms, _) => { waits.Add(ms); return Task.CompletedTask; }, _ => { }, CancellationToken.None).GetAwaiter().GetResult();
        check(recovery.Device?.Index == 9 && recovery.Attempts == 3 && waits.SequenceEqual(new[] { 2000, 5000, 10000 }), "Reconnect retries use bounded backoff and reacquire the matching receiver");
        calls = 0;
        var absent = ReceiverRecovery.Find(key, 2, () => { calls++; return Task.FromResult(new List<RtlNative.Device> { b }); }, (_, _) => Task.CompletedTask, _ => { }, CancellationToken.None).GetAwaiter().GetResult();
        check(absent.Device == null && absent.Attempts == 3 && calls == 1, "Reconnect respects remaining session budget and refuses another receiver");
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel(); bool stopped = false;
            try { ReceiverRecovery.Find(key, 0, () => throw new Exception("Unexpected discovery"), (_, token) => Task.Delay(1, token), _ => { }, cancelled.Token).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { stopped = true; }
            check(stopped, "Stop cancellation prevents reconnect discovery");
        }
        string folder = Path.Combine(Path.GetTempPath(), "mrs-iq-tests-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            string wideFile = Path.Combine(folder, "wide-v2.mrsiq");
            var wideCapture = new CaptureDescriptor(6000000000, 20000000, 18000000, IqSampleFormat.Signed16LittleEndian);
            var wideData = new byte[8192]; wideData[17] = 123;
            using (var writer = new IqRecording(wideFile, new IqMetadata(2, DateTimeOffset.UtcNow, false, 0, null)))
                check(writer.Write(wideData, wideCapture), "Version 2 accepts wideband signed I/Q and 64-bit frequencies");
            using (var reader = new IqReader(wideFile))
            {
                var frame = reader.Read()!;
                check(frame.Capture == wideCapture && frame.Data.SequenceEqual(wideData) && reader.Read() == null, "Version 2 preserves separate usable bandwidth, rate, format and sample bytes");
            }
            check(wideCapture.BytesPerSecond == 80000000, "Recording throughput includes both I/Q components and container width");
            bool unsupportedDsp = false; try { wideCapture.RequireCurrentDsp(); } catch (IOException) { unsupportedDsp = true; }
            check(unsupportedDsp, "Unsupported wideband playback is rejected explicitly before demodulation");
            foreach (var invalidCapture in new[] { wideCapture with { CenterHz = -1 }, wideCapture with { UsableBandwidthHz = double.NaN }, wideCapture with { UsableBandwidthHz = 21000000 }, wideCapture with { Format = (IqSampleFormat)99 } })
            {
                bool invalidMetadata = false; try { invalidCapture.Validate(8192); } catch (IOException) { invalidMetadata = true; }
                check(invalidMetadata, "Invalid version 2 capture metadata is rejected: " + invalidCapture);
            }
            bool alignmentRejected = false; try { wideCapture.Validate(8191); } catch (IOException) { alignmentRejected = true; }
            check(alignmentRejected, "Partial complex samples cannot enter recordings");
            string v2Replay = Path.Combine(folder, "v2-playback.mrsiq");
            using (var writer = new IqRecording(v2Replay, new IqMetadata(2, DateTimeOffset.UtcNow, false, 0, null)))
                writer.Write(SelfTests.MakeFm(false, 16384, 3200000), 6000000000, 3200000);
            using (var replay = new RtlReceiver())
            using (var done = new ManualResetEventSlim())
            {
                long frequency = 0; string end = ""; int samples = 0;
                replay.Applied += (hz, _) => frequency = hz; replay.Audio += data => samples += data.Length;
                replay.Ended += message => { end = message; done.Set(); };
                replay.StartReplay(v2Replay);
                check(done.Wait(5000) && frequency == 6000000000 && samples > 0 && end == "I/Q replay finished.", "Version 2 replay at 3.2 MS/s retains frequency above 32-bit range");
            }
            string capture = Path.Combine(folder, "capture.mrsiq");
            foreach (string recordedMode in new[] { "AM", "USB", "LSB" })
            {
                string modeFile = Path.Combine(folder, recordedMode + ".mrsiq");
                byte[] samples = SelfTests.MakeFm(false, 16384);
                using (var writer = new IqRecording(modeFile, new IqMetadata(2, DateTimeOffset.UtcNow, false, 0, null, recordedMode))) writer.Write(samples, 146520000, 240000);
                using var playback = new RtlReceiver(); using var completed = new ManualResetEventSlim();
                var received = new List<byte>(); string ended = "";
                playback.Audio += bytes => received.AddRange(bytes); playback.Ended += message => { ended = message; completed.Set(); };
                var readMetadata = playback.StartReplay(modeFile);
                check(completed.Wait(5000) && ended == "I/Q replay finished." && readMetadata.Mode == recordedMode && received.SequenceEqual(new ReceiveDemodulator(recordedMode, 240000).Process(samples)), recordedMode + " recording metadata restores the correct playback demodulator");
            }
            byte[] original = SelfTests.MakeFm(false, 16384); byte[] expected = (byte[])original.Clone();
            var metadata = new IqMetadata(1, DateTimeOffset.UtcNow, false, -1000, key);
            using (var writer = new IqRecording(capture, metadata))
            {
                check(writer.Write(original, 146520000, 240000), "I/Q writer accepts full captured samples"); original[0] ^= 255;
                Thread.Sleep(35); check(writer.Write(expected, 146550000, 960000), "I/Q writer accepts per-frame retune and sample-rate metadata");
            }
            using (var reader = new IqReader(capture))
            {
                var first = reader.Read()!; var second = reader.Read()!;
                check(first.Data.SequenceEqual(expected), "I/Q recording owns samples independently of producer buffers");
                check(reader.Metadata == metadata && first.Frequency == 146520000 && second.Frequency == 146550000 && second.Rate == 960000, "I/Q metadata and frequency transitions round-trip exactly");
                check(second.ElapsedTicks > first.ElapsedTicks && reader.Read() == null, "I/Q timing and clean completion marker survive reload");
            }
            string truncated = Path.Combine(folder, "truncated.mrsiq");
            byte[] file = File.ReadAllBytes(capture); File.WriteAllBytes(truncated, file[..^8]);
            using (var reader = new IqReader(truncated))
            {
                check(reader.Read() != null && reader.Read() != null, "Incomplete I/Q files retain preceding complete frames");
                bool incomplete = false; try { reader.Read(); } catch (IOException ex) { incomplete = ex.Message.Contains("incomplete"); }
                check(incomplete, "Missing completion marker is reported as incomplete rather than successful EOF");
            }
            string invalid = Path.Combine(folder, "invalid.mrsiq"); File.WriteAllBytes(invalid, new byte[12]);
            bool rejected = false; try { using var bad = new IqReader(invalid); } catch (IOException) { rejected = true; }
            check(rejected, "Invalid I/Q file signature is rejected before replay");
            // Build a deterministic gap and retune with the documented on-disk framing.
            string gapFile = Path.Combine(folder, "gap.mrsiq");
            using (var stream = File.Create(gapFile))
            using (var writer = new BinaryWriter(stream))
            {
                byte[] json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata);
                writer.Write(IqRecording.Magic); writer.Write(json.Length); writer.Write(json);
                writer.Write(0L); ReceiverStream.Write(stream, expected, expected.Length, 146520000, 240000);
                writer.Write(TimeSpan.FromMilliseconds(400).Ticks); ReceiverStream.Write(stream, expected, expected.Length, 146550000, 240000);
                writer.Write(-1L);
            }
            using (var replay = new RtlReceiver())
            using (var ended = new ManualResetEventSlim())
            {
                var frequencies = new List<long>(); int gaps = 0, audio = 0; string result = "";
                replay.Applied += (frequency, _) => frequencies.Add(frequency); replay.ReplayGap += () => gaps++;
                replay.Audio += bytes => audio += bytes.Length; replay.Ended += message => { result = message; ended.Set(); };
                var clock = System.Diagnostics.Stopwatch.StartNew(); replay.StartReplay(gapFile);
                bool refused = false; try { replay.Retune(146560000).GetAwaiter().GetResult(); } catch (IOException) { refused = true; }
                check(refused && replay.WorkerId == 0, "I/Q replay opens no SDR worker and refuses hardware tuning");
                check(ended.Wait(4000), "I/Q replay reaches completion without hanging");
                check(frequencies.SequenceEqual(new long[] { 146520000, 146550000 }) && audio > 0 && result == "I/Q replay finished.", "I/Q replay applies recorded retunes and recovers demodulated audio");
                check(gaps == 1 && clock.ElapsedMilliseconds >= 350, "I/Q replay preserves a recorded timing gap");
                replay.Stop();
                replay.StartReplay(gapFile); Thread.Sleep(50); clock.Restart(); replay.Stop();
                check(clock.ElapsedMilliseconds < 1000 && !replay.IsReplay, "Stop promptly cancels I/Q replay during a recorded gap");
            }
            string liveFile = Path.Combine(folder, "live.mrsiq");
            using (var receiver = new RtlReceiver())
            using (var ended = new ManualResetEventSlim())
            {
                int frames = 0; receiver.Applied += (_, _) => { if (!receiver.IsRecording) receiver.RecordIq(liveFile, metadata); };
                receiver.Audio += _ => { if (++frames == 3) receiver.Retune(146550000).GetAwaiter().GetResult(); };
                receiver.Ended += _ => ended.Set(); receiver.Start(0, 146520000, -1000, false, fixture: true);
                check(ended.Wait(8000), "Live I/Q recording fixture reaches EOF"); receiver.Stop();
            }
            using (var reader = new IqReader(liveFile))
            {
                var frequencies = new HashSet<long>(); int frames = 0; IqFrame? frame;
                while ((frame = reader.Read()) != null) { frequencies.Add(frame.Frequency); frames++; }
                check(frames >= 10 && frequencies.SetEquals(new long[] { 146520000, 146550000 }), "Worker I/Q recording preserves both retuned spans and finalizes on source loss");
            }
        }
        finally { foreach (string path in Directory.EnumerateFiles(folder)) File.Delete(path); Directory.Delete(folder); }
    }
}





