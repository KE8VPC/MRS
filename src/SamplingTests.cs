using System.IO;
using System.Diagnostics;
namespace MRS;
internal static class SamplingTests
{
    internal static void Run(Action<bool, string> check)
    {
        static bool Wait(Func<bool> predicate) { var clock = Stopwatch.StartNew(); while (!predicate() && clock.ElapsedMilliseconds < 5000) Thread.Sleep(10); return predicate(); }
        check(!SamplingModes.CanTune(7000000, 0) && SamplingModes.CanTune(7000000, 1) && SamplingModes.CanTune(7000000, 2), "HF tuning limits depend on sampling path");
        check(SamplingModes.CanTune(1, 1) && SamplingModes.CanTune(1, 2) && !SamplingModes.CanTune(0, 2) && !SamplingModes.CanTune(28800001, 1) && !SamplingModes.CanTune(7000000, 3), "Direct sampling permits 1 Hz but rejects DC, out-of-range frequencies and invalid branch values");
        var view = new SpectrumView(); view.SetRf(1, 240000); view.ResetView();
        check(view.VisibleCenter - view.VisibleSpan / 2 == 0 && view.FullSpan == 120001, "Near-zero RF view clips negative frequencies without remapping sample coverage");
        view.SetRf(7000000, 960000); check(view.FullSpan == 960000 && view.CaptureCenter == 7000000, "HF view retains full span away from zero");
        foreach (int branch in new[] { 1, 2 })
        {
            var commands = new ReceiverStream.Commands(false, branch); commands.Accept("TUNE 1"); check(commands.Take() == 1, "Direct branch command accepts the lowest positive frequency: " + branch);
            commands.Accept("TUNE 0"); check(commands.Take() == 0, "Direct branch rejects zero-frequency commands: " + branch);
            using var stream = new MemoryStream(); var block = SelfTests.MakeFm(false, 16384);
            ReceiverStream.WriteLive(stream, block, block.Length, 7000000, 240000, -1000, -1000, branch);
            var header = ReceiverStream.Header(stream.ToArray()[..ReceiverStream.LiveHeaderSize]);
            check(header.SamplingMode == branch && header.Frequency == 7000000, "Live frame carries HF frequency and applied sampling branch: " + branch);
            byte[] bad = stream.ToArray()[..ReceiverStream.LiveHeaderSize]; BitConverter.GetBytes(0).CopyTo(bad, 24);
            bool rejected = false; try { ReceiverStream.Header(bad); } catch (IOException) { rejected = true; }
            check(rejected, "HF frame cannot masquerade as quadrature sampling: " + branch);
            string path = Path.Combine(Path.GetTempPath(), "mrs-direct-" + Guid.NewGuid() + ".mrsiq");
            try
            {
                using (var receiver = new RtlReceiver())
                {
                    receiver.Start(0, 7000000, 200, false, fixture: true, fixtureSeconds: 5, mode: "AM", samplingMode: branch);
                    check(Wait(() => receiver.AppliedFrequency == 7000000) && receiver.SamplingMode == branch && receiver.AppliedGain == -1000, "Direct sampling starts at 7 MHz without tuner gain: " + branch);
                    bool noGain = false; try { receiver.SetGain(100).GetAwaiter().GetResult(); } catch (IOException) { noGain = true; }
                    check(noGain, "Tuner gain command is rejected in direct mode: " + branch);
                    receiver.RecordIq(path, new IqMetadata(3, DateTimeOffset.UtcNow, false, -1000, null, "AM")); Thread.Sleep(90);
                    int worker = receiver.WorkerId;
                    Task.Run(() => receiver.Retune(1)).GetAwaiter().GetResult();
                    check(Wait(() => receiver.AppliedFrequency == 1) && receiver.WorkerId == worker, "Direct mode retunes to 1 Hz without reopening worker: " + branch);
                    Thread.Sleep(90); receiver.Stop();
                }
                using (var reader = new IqReader(path))
                {
                    var frequencies = new HashSet<long>(); IqFrame? frame; while ((frame = reader.Read()) != null) frequencies.Add(frame.Frequency);
                    check(reader.Metadata.SamplingMode == branch && frequencies.Contains(7000000) && frequencies.Contains(1), "Recording preserves branch and near-zero/HF capture metadata: " + branch);
                }
                using (var replay = new RtlReceiver())
                {
                    replay.StartReplay(path); check(Wait(() => replay.ReplayFinished) && replay.SamplingMode == branch && replay.AppliedFrequency == 1 && replay.WorkerId == 0, "Recorded direct sampling replays without receiver hardware: " + branch);
                }
            }
            finally { File.Delete(path); }
        }
    }
}
