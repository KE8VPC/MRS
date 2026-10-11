namespace MRS;

internal sealed class ReceiveDemodulator
{
    internal static readonly string[] Modes = ["NFM", "WFM", "AM", "USB", "LSB"];
    private readonly FmDemodulator? fm;
    private readonly IqDecimator? frontEnd;
    private readonly string mode;
    private readonly bool agc;
    private readonly double[] real = new double[1025], imaginary = new double[1025], iHistory = new double[1025], qHistory = new double[1025];
    private int position, phase;
    private double previous, dc, envelope = .1;
    internal double ChannelDb { get; private set; } = -120;
    internal BroadcastState? Broadcast => fm?.Broadcast;
    internal RdsState? Rds => fm?.Rds;
    internal byte[] StereoAudio => fm?.StereoAudio ?? [];
    internal bool CaptureData { set { if(fm!=null)fm.CaptureData=value; } }
    internal double[] DataSamples=>fm?.DataSamples ?? [];
    internal bool ForceMono { set { if (fm != null) fm.ForceMono = value; } }
    internal ReceiveDemodulator(string mode, int rate, double offsetHz = 0, double bandwidth = 0, bool agc = true)
    {
        if (!Modes.Contains(mode)) throw new ArgumentException("Unsupported receive mode.", nameof(mode));
        this.mode = mode;
        this.agc = agc;
        var channel = new ChannelSettings(0, mode, bandwidth, agc); channel.Validate();
        if (mode is "NFM" or "WFM") { fm = new(mode == "WFM", rate, offsetHz, channel.Width); return; }
        frontEnd = new(rate, offsetHz);
        // Complex bandpass selects positive (USB) or negative (LSB) frequencies.
        var (low, high) = channel.Edges;
        double cutoff = (high - low) / 2 / 240000, center = (high + low) / 2 / 240000, sum = 0;
        for (int n = 0; n < real.Length; n++)
        {
            int m = n - real.Length / 2;
            double h = (m == 0 ? 2 * cutoff : Math.Sin(2 * Math.PI * cutoff * m) / (Math.PI * m)) * (.42 - .5 * Math.Cos(2 * Math.PI * n / (real.Length - 1)) + .08 * Math.Cos(4 * Math.PI * n / (real.Length - 1)));
            sum += h; real[n] = h * Math.Cos(2 * Math.PI * center * m); imaginary[n] = h * Math.Sin(2 * Math.PI * center * m);
        }
        for (int n = 0; n < real.Length; n++) { real[n] /= sum; imaginary[n] /= sum; }
    }
    internal byte[] Process(byte[] iq)
    {
        if (fm != null) { var result = fm.Process(iq); ChannelDb = fm.ChannelDb; return result; }
        var output = new List<byte>(); double power = 0; int count = 0;
        foreach (var input in frontEnd!.Process(iq))
        {
            iHistory[position] = input.I; qHistory[position] = input.Q;
            if (++phase == 5)
            {
                phase = 0; double i = 0, q = 0; int index = position;
                for (int n = 0; n < real.Length; n++)
                {
                    i += real[n] * iHistory[index] - imaginary[n] * qHistory[index];
                    q += real[n] * qHistory[index] + imaginary[n] * iHistory[index];
                    if (--index < 0) index = real.Length - 1;
                }
                double p = i * i + q * q; power += p; count++;
                double detected = mode == "AM" ? Math.Sqrt(p) : i;
                dc = detected - previous + .995 * dc; previous = detected;
                // Bounded audio AGC: fast attack and slower release, never RF squelch.
                double level = Math.Abs(dc); envelope += (level > envelope ? .02 : .00005) * (level - envelope);
                double gain = agc ? Math.Min(20, .18 / Math.Max(.009, envelope)) : 1;
                short sample = (short)Math.Clamp(dc * gain * 30000, short.MinValue, short.MaxValue);
                output.Add((byte)sample); output.Add((byte)(sample >> 8));
            }
            if (++position == real.Length) position = 0;
        }
        ChannelDb = 10 * Math.Log10(Math.Max(1e-12, power / Math.Max(1, count)));
        return output.ToArray();
    }
}
