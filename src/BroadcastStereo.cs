namespace MRS;

internal sealed record BroadcastState(bool Pilot, bool Locked, bool Stereo, double PilotPercent,
    double LeftDb, double RightDb, bool Clipped, float[] CompositeSpectrum);

// Receives the discriminator output at 240 kS/s, before mono audio filtering.
internal sealed class BroadcastStereo
{
    private readonly FmDemodulator.Fir sumFilter = new(129, 15000), differenceFilter = new(129, 15000);
    private double phase, frequency, pilotI, pilotQ, errorPower = 1, blend, leftDc, rightDc, previousLeft, previousRight, leftDe, rightDe;
    private int decimation, stable, fftCount;
    private readonly float[] fft = new float[SignalMath.FftSize];
    private float[] spectrum = Enumerable.Repeat(-120f, 1024).ToArray();
    internal bool ForceMono { get; set; }
    internal BroadcastState State { get; private set; } = new(false, false, false, 0, -120, -120, false, []);
    internal double PilotPhase => phase;
    internal byte[] Process(ReadOnlySpan<double> composite)
    {
        var result = new List<byte>(composite.Length * 4 / 5 + 4);
        double lp = 0, rp = 0; int count = 0; bool clipped = false;
        foreach (double value in composite)
        {
            double sin = Math.Sin(phase), cos = Math.Cos(phase);
            pilotI += .002 * (value * sin - pilotI);
            pilotQ += .002 * (value * cos - pilotQ);
            double error = Math.Atan2(pilotQ, pilotI);
            double amplitude = 2 * Math.Sqrt(pilotI * pilotI + pilotQ * pilotQ);
            errorPower += .0002 * (error * error - errorPower);
            bool pilot = amplitude > .025 && amplitude < .2;
            if (pilot && errorPower < .025) stable = Math.Min(48000, stable + 1); else stable = 0;
            bool locked = stable >= 24000;
            double sum = sumFilter.Push(value);
            double difference = differenceFilter.Push(2 * value * Math.Sin(2 * phase));
            frequency = Math.Clamp(frequency + .0000003 * error, -2 * Math.PI * 30 / 240000, 2 * Math.PI * 30 / 240000);
            phase = (phase + 2 * Math.PI * 19000 / 240000 + frequency + .0003 * error) % (2 * Math.PI);
            blend += .0001 * ((locked && !ForceMono ? 1 : 0) - blend);
            fft[fftCount++] = (float)value;
            if (fftCount == fft.Length) { spectrum = SignalMath.Spectrum(fft); fftCount = 0; }
            if (++decimation != 5) continue;
            decimation = 0;
            double left = sum + difference * blend, right = sum - difference * blend;
            leftDc = left - previousLeft + .995 * leftDc; previousLeft = left;
            rightDc = right - previousRight + .995 * rightDc; previousRight = right;
            const double alpha = .24253487160303355; // 75 us de-emphasis at 48 kS/s.
            leftDe += alpha * (leftDc - leftDe); rightDe += alpha * (rightDc - rightDe);
            lp += leftDe * leftDe; rp += rightDe * rightDe; count++;
            for (int channel = 0; channel < 2; channel++)
            {
                double sample = channel == 0 ? leftDe : rightDe;
                clipped |= Math.Abs(sample * 24000) > short.MaxValue;
                short pcm = (short)Math.Clamp(sample * 24000, short.MinValue, short.MaxValue);
                result.Add((byte)pcm); result.Add((byte)(pcm >> 8));
            }
        }
        double magnitude = 2 * Math.Sqrt(pilotI * pilotI + pilotQ * pilotQ);
        bool detected = magnitude > .025 && magnitude < .2, isLocked = stable >= 24000;
        State = new(detected, isLocked, isLocked && !ForceMono && blend > .95, magnitude * 100,
            10 * Math.Log10(Math.Max(1e-12, lp / Math.Max(1, count) * Math.Pow(24000.0 / 32768, 2))),
            10 * Math.Log10(Math.Max(1e-12, rp / Math.Max(1, count) * Math.Pow(24000.0 / 32768, 2))), clipped, spectrum);
        return result.ToArray();
    }
}
