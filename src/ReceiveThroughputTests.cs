namespace MRS;

internal static class ReceiveThroughputTests
{
    internal static void Run(Action<bool, string> check)
    {
        // Independent scalar FIR oracle checks wraparound, padding and impulse response.
        foreach (double cutoff in new[] { 3000.0, 8000, 95000 })
        {
            const int taps = 129;
            var h = new double[taps]; var history = new double[taps]; int position = 0;
            double sum = 0, error = 0;
            for (int n = 0; n < taps; n++)
            {
                int m = n - (taps - 1) / 2; double f = cutoff / 240000;
                h[n] = (m == 0 ? 2 * f : Math.Sin(2 * Math.PI * f * m) / (Math.PI * m)) * (.54 - .46 * Math.Cos(2 * Math.PI * n / (taps - 1))); sum += h[n];
            }
            for (int n = 0; n < taps; n++) h[n] /= sum;
            var filter = new FmDemodulator.Fir(taps, cutoff); var random = new Random(17);
            for (int sample = 0; sample < 10000; sample++)
            {
                double value = sample < 256 ? (sample == 0 ? 1 : 0) : random.NextDouble() * 2 - 1;
                history[position] = value; double expected = 0; int index = position;
                for (int n = 0; n < taps; n++) { expected += h[n] * history[index]; if (--index < 0) index = taps - 1; }
                position = (position + 1) % taps;
                error = Math.Max(error, Math.Abs(filter.Push(value) - expected));
            }
            check(error < 1e-12, $"Receive SIMD FIR matches scalar impulse/noise reference at {cutoff} Hz");
        }
        // At unity decimation compare directly to the mathematical oscillator, across re-anchors.
        foreach (double offset in new[] { -25000.0, 0, 37123.5 })
        {
            var input = new byte[200000]; new Random(18).NextBytes(input);
            var actual = new IqDecimator(240000, offset).Process(input); double error = 0;
            for (int n = 0; n < actual.Length; n++)
            {
                double angle = -2 * Math.PI * offset * n / 240000, i = (input[n * 2] - 127.5) / 128, q = (input[n * 2 + 1] - 127.5) / 128;
                error = Math.Max(error, Math.Max(Math.Abs(actual[n].I - (i * Math.Cos(angle) - q * Math.Sin(angle))), Math.Abs(actual[n].Q - (i * Math.Sin(angle) + q * Math.Cos(angle)))));
            }
            check(error < 1e-9, $"Receive recurrent mixer matches analytic phase through re-anchors at {offset} Hz");
        }
        foreach (int rate in new[] { 2400000, 3200000, 9600000, 20000000 })
        {
            var input = new byte[262144]; new Random(19).NextBytes(input);
            var expected = new IqDecimator(rate, -250000).Process(input);
            var split = new IqDecimator(rate, -250000); var actual = new List<(double I, double Q)>();
            for (int start = 0; start < input.Length; start += 8192) actual.AddRange(split.Process(input[start..Math.Min(input.Length, start + 8192)]));
            check(expected.SequenceEqual(actual), $"Receive decimator preserves every sample and phase across block boundaries at {rate}");
        }
    }
}
