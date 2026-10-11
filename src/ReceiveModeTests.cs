namespace MRS;
internal static class ReceiveModeTests
{
    internal static void Run(Action<bool, string> check)
    {
        static byte[] Signal(string mode, int rate, int sign = 1)
        {
            var data = new byte[rate / 2];
            for (int n = 0; n < data.Length / 2; n++)
            {
                double phase = 2 * Math.PI * 1200 * n / rate;
                double i = mode == "AM" ? .45 * (1 + .5 * Math.Cos(phase)) : .45 * Math.Cos(phase);
                double q = mode == "AM" ? 0 : sign * .45 * Math.Sin(phase);
                data[n * 2] = (byte)Math.Clamp(Math.Round(i * 128 + 127.5), 0, 255);
                data[n * 2 + 1] = (byte)Math.Clamp(Math.Round(q * 128 + 127.5), 0, 255);
            }
            return data;
        }
        static double Tone(byte[] audio, double hz)
        {
            double real = 0, imaginary = 0; int start = 4000, count = audio.Length / 2 - start;
            for (int n = start; n < audio.Length / 2; n++)
            {
                double value = BitConverter.ToInt16(audio, n * 2) / 32768.0;
                real += value * Math.Cos(2 * Math.PI * hz * n / 48000); imaginary += value * Math.Sin(2 * Math.PI * hz * n / 48000);
            }
            return Math.Sqrt(real * real + imaginary * imaginary) / count;
        }
        foreach (string mode in new[] { "AM", "USB", "LSB" })
        foreach (int rate in ReceiverStream.Rates)
        {
            byte[] data = Signal(mode, rate, mode == "LSB" ? -1 : 1);
            var dsp = new ReceiveDemodulator(mode, rate); byte[] audio = dsp.Process(data);
            check(audio.Length == 24000 && Tone(audio, 1200) > .03 && Tone(audio, 1200) > 20 * Tone(audio, 3000), $"{mode} recovers 1200 Hz audio at 48 kHz from {rate} S/s");
            var split = new ReceiveDemodulator(mode, rate); var result = new List<byte>();
            for (int offset = 0; offset < data.Length; offset += 8190) result.AddRange(split.Process(data.AsSpan(offset, Math.Min(8190, data.Length - offset)).ToArray()));
            check(result.SequenceEqual(audio), $"{mode} preserves filter and AGC state across uneven input blocks at {rate} S/s");
            if (mode != "AM")
            {
                var opposite = new ReceiveDemodulator(mode, rate); opposite.Process(Signal(mode, rate, mode == "LSB" ? 1 : -1));
                // Discard startup before comparing filtered RF power (independent of audio AGC).
                dsp.Process(data); opposite.Process(Signal(mode, rate, mode == "LSB" ? 1 : -1));
                check(dsp.ChannelDb - opposite.ChannelDb > 40, $"{mode} rejects the opposite 1200 Hz sideband by over 40 dB at {rate} S/s");
            }
        }
    }
}
