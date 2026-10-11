using System.IO;
using System.Diagnostics;
using System.Text.Json;
namespace MRS;
internal static class MilestoneTests
{
    internal static void Run(Action<bool, string> check)
    {
        static bool Wait(Func<bool> condition, int milliseconds = 5000)
        { var clock = Stopwatch.StartNew(); while (!condition() && clock.ElapsedMilliseconds < milliseconds) Thread.Sleep(10); return condition(); }
        var commands = new ReceiverStream.Commands(false);
        commands.Accept("RATE 960000"); commands.Accept("GAIN 200"); commands.Accept("TUNE 146530000");
        check(commands.TakeRate() == 960000 && commands.TakeGain() == 200 && commands.Take() == 146530000, "Worker commands retain independent rate, gain and tuning requests");
        commands.Accept("RATE 123"); commands.Accept("GAIN -5");
        check(commands.TakeRate() == 0 && commands.TakeGain() == int.MinValue, "Invalid control requests never reach the native receiver");
        var view = new SpectrumView(); view.SetRf(6000000000, 20000000, false, 18000000);
        check(view.FullSpan == 18000000 && view.VisibleCenter == 6000000000, "Usable RF display span is independent of sample rate");
        var channel = new ChannelSettings(146530000, "USB", 2400, false);
        check(channel.Fits(146520000, 240000) && !(channel with { Frequency = 146640000 }).Fits(146520000, 240000), "Complete sideband filter must fit inside capture boundaries");
        // AM signal well away from the capture center, mixed before decimation.
        const int rate = 960000; var signal = new byte[rate / 2];
        for (int n = 0; n < signal.Length / 2; n++)
        {
            double amplitude = .45 * (1 + .4 * Math.Cos(2 * Math.PI * 1200 * n / rate));
            double phase = 2 * Math.PI * 400000 * n / rate;
            signal[2 * n] = (byte)Math.Clamp(Math.Round(127.5 + 128 * amplitude * Math.Cos(phase)), 0, 255);
            signal[2 * n + 1] = (byte)Math.Clamp(Math.Round(127.5 + 128 * amplitude * Math.Sin(phase)), 0, 255);
        }
        var dsp = new ReceiveDemodulator("AM", rate, 400000, 6000, false); byte[] audio = dsp.Process(signal);
        double tone = 0, other = 0;
        for (int n = 4000; n < audio.Length / 2; n++) { double sample = BitConverter.ToInt16(audio, n * 2); tone += sample * Math.Cos(2 * Math.PI * 1200 * n / 48000); other += sample * Math.Cos(2 * Math.PI * 3500 * n / 48000); }
        check(audio.Length == 24000 && Math.Abs(tone) > 20 * Math.Abs(other), "Offset channel mixes before decimation and recovers audio away from capture center");
        var split = new ReceiveDemodulator("AM", rate, 400000, 6000, false); var chunks = new List<byte>();
        for (int i = 0; i < signal.Length; i += 8190) chunks.AddRange(split.Process(signal.AsSpan(i, Math.Min(8190, signal.Length - i)).ToArray()));
        check(chunks.SequenceEqual(audio), "Channel oscillator retains phase across input blocks");
        string folder = Path.Combine(Path.GetTempPath(), "mrs-milestone-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "controls.mrsiq");
            using (var receiver = new RtlReceiver())
            {
                receiver.Start(0, 146520000, -1000, false, fixture: true, fixtureSeconds: 10);
                check(Wait(() => receiver.AppliedFrequency != 0), "Milestone fixture starts with acknowledged capture state"); int worker = receiver.WorkerId;
                receiver.RecordIq(path, new IqMetadata(3, DateTimeOffset.UtcNow, false, -1000, null)); Thread.Sleep(120);
                receiver.SetChannel(channel);
                check(Wait(() => receiver.AppliedChannel == channel) && receiver.AppliedFrequency == 146520000, "Software channel tuning leaves capture center unchanged");
                receiver.SetGain(200).GetAwaiter().GetResult();
                check(Wait(() => receiver.AppliedGain == 200), "Gain selection is acknowledged without reopening receiver");
                receiver.SetRate(960000).GetAwaiter().GetResult();
                check(Wait(() => receiver.AppliedRate == 960000) && receiver.AppliedChannel == channel, "Live rate change rebuilds DSP while preserving an in-range channel");
                Thread.Sleep(150);
                check(receiver.WorkerId == worker && receiver.IsRecording, "Live gain, rate and channel changes preserve worker and I/Q recording"); receiver.Stop();
            }
            using (var reader = new IqReader(path))
            {
                var frames = new List<IqFrame>(); IqFrame? frame; while ((frame = reader.Read()) != null) frames.Add(frame);
                check(reader.Metadata.Version == 3 && frames.Any(f => f.Rate == 240000) && frames.Any(f => f.Rate == 960000), "Version 3 records applied rate transitions");
                check(frames.Any(f => f.Gain == 200 && f.RequestedGain == 200 && f.Channel == channel) && frames.All(f => f.Frequency == 146520000), "Version 3 retains gain and channel settings independently of capture center");
            }
            // Deterministic long gaps make pause and seeking observable without large files.
            string timed = Path.Combine(folder, "timed.mrsiq");
            using (var stream = File.Create(timed)) using (var writer = new BinaryWriter(stream))
            {
                var json = JsonSerializer.SerializeToUtf8Bytes(new IqMetadata(1, DateTimeOffset.UtcNow, false, -1000, null));
                writer.Write(IqRecording.Magic); writer.Write(json.Length); writer.Write(json);
                var block = SelfTests.MakeFm(false, 16384);
                for (int n = 0; n < 4; n++) { writer.Write(n * TimeSpan.TicksPerSecond); ReceiverStream.Write(stream, block, block.Length, (uint)(146520000 + n * 10000), 240000); }
                writer.Write(-1L);
            }
            using (var reader = new IqReader(timed))
            {
                var index = IqReplayIndex.Build(reader, CancellationToken.None);
                check(index.Entries.Count == 4 && index.DurationTicks > 3 * TimeSpan.TicksPerSecond, "Replay index derives duration and seek offsets from complete frames");
                reader.SeekFrame(index.OffsetFor(2 * TimeSpan.TicksPerSecond)); check(reader.Read()!.Frequency == 146540000, "Indexed seek lands on a valid metadata boundary");
            }
            using (var replay = new RtlReceiver())
            {
                int audioBlocks = 0; replay.Audio += _ => Interlocked.Increment(ref audioBlocks);
                replay.StartReplay(timed, true); check(Wait(() => replay.ReplayReady), "Paused replay indexes without opening hardware"); Thread.Sleep(100);
                check(audioBlocks == 0 && replay.ReplayPosition == TimeSpan.Zero && replay.WorkerId == 0, "Paused playback does not emit audio or advance its timeline");
                replay.SeekReplay(TimeSpan.FromSeconds(2)); check(Wait(() => replay.ReplayPosition.TotalSeconds == 2), "Seeking updates paused position without resuming audio");
                check(Wait(() => replay.AppliedFrequency == 146540000) && audioBlocks == 0, "Paused seek restores frequency metadata without emitting audio");
                replay.ReplayPaused = false; check(Wait(() => replay.AppliedFrequency == 146540000 && audioBlocks > 0), "Resume after seek restores destination frequency and processing");
                replay.ReplayPaused = true; Thread.Sleep(60); var position = replay.ReplayPosition; int blocks = audioBlocks; Thread.Sleep(150);
                check(replay.ReplayPosition == position && audioBlocks == blocks, "Pause freezes audio and elapsed time during a recorded gap");
                var stop = Stopwatch.StartNew(); replay.Stop(); check(stop.ElapsedMilliseconds < 1000, "Stop cancels paused replay promptly");
            }
        }
        finally { foreach (var file in Directory.EnumerateFiles(folder)) File.Delete(file); Directory.Delete(folder); }
    }
}
