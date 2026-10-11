using System.IO;
using NAudio.Wave;
namespace MRS;
internal static class BroadcastTests
{
    internal static double[] Composite(double seconds, double pilotOffset = 0, bool pilot = true)
    {
        var values = new double[(int)(240000 * seconds)];
        for (int n = 0; n < values.Length; n++)
        {
            double t = n / 240000.0, left = .3 * Math.Sin(2 * Math.PI * 1000 * t), right = .3 * Math.Sin(2 * Math.PI * 2300 * t);
            double phase = 2 * Math.PI * (19000 + pilotOffset) * t + .7;
            values[n] = (left + right) / 2 + (left - right) / 2 * Math.Sin(2 * phase) + (pilot ? .09 * Math.Sin(phase) : 0);
        }
        return values;
    }
    internal static double Tone(byte[] audio, int channel, double hz)
    {
        int start = audio.Length / 8, count = audio.Length / 4 - start;
        double x = 0, y = 0;
        for (int n = start; n < audio.Length / 4; n++)
        {
            double v = BitConverter.ToInt16(audio, n * 4 + channel * 2) / 32768.0;
            x += v * Math.Cos(2 * Math.PI * hz * n / 48000); y += v * Math.Sin(2 * Math.PI * hz * n / 48000);
        }
        return Math.Sqrt(x * x + y * y) / Math.Max(1, count);
    }
    internal static void Run(Action<bool,string> check)
    {
        foreach (double offset in new[] { -2.0, 0, 2.0 })
        {
            var input = Composite(1, offset); var dsp = new BroadcastStereo(); var pcm = dsp.Process(input);
            check(pcm.Length == 48000 * 4 && dsp.State.Locked && dsp.State.Stereo, $"WFM locks stereo pilot at {offset:+0;-0;0} Hz offset and produces 48 kHz stereo");
            double left = Tone(pcm, 0, 1000), wrongRight = Tone(pcm, 1, 1000), right = Tone(pcm, 1, 2300), wrongLeft = Tone(pcm, 0, 2300);
            check(left > .04 && right > .03 && left > wrongRight * 20 && right > wrongLeft * 20, $"WFM separates left/right tones by over 26 dB at {offset:+0;-0;0} Hz pilot offset ({left:F3}/{wrongRight:F4}, {right:F3}/{wrongLeft:F4})");
            var split = new BroadcastStereo(); var pieces = new List<byte>();
            for (int n = 0; n < input.Length; n += 8191) pieces.AddRange(split.Process(input.AsSpan(n, Math.Min(8191, input.Length - n))));
            check(pcm.SequenceEqual(pieces), "WFM stereo preserves PLL/filter state across uneven blocks");
        }
        var mono = new BroadcastStereo { ForceMono = true }; var monoPcm = mono.Process(Composite(1));
        check(mono.State.Locked && !mono.State.Stereo && Enumerable.Range(0, monoPcm.Length / 4).All(n => BitConverter.ToInt16(monoPcm, n * 4) == BitConverter.ToInt16(monoPcm, n * 4 + 2)), "Force mono keeps pilot lock but duplicates mono to both channels");
        mono.ForceMono = false; mono.Process(Composite(1, pilot:false));
        check(!mono.State.Pilot && !mono.State.Locked && !mono.State.Stereo, "Pilot loss releases stereo lock and returns to mono");
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
        try
        {
            using var engine = new AudioEngine(); engine.StartExternal(2); engine.Record(path); engine.FeedStereo(monoPcm, false); engine.FinishRecording();
            using var reader = new WaveFileReader(path); byte[] read = new byte[monoPcm.Length]; int count = reader.Read(read,0,read.Length);
            check(reader.WaveFormat.Channels == 2 && reader.WaveFormat.SampleRate == 48000 && count == monoPcm.Length && read.SequenceEqual(monoPcm), "Stereo WAV retains both channels and remains ungated by RF squelch");
        }
        finally { File.Delete(path); }
    }
}
