using NAudio.Dsp;

namespace MRS;
internal sealed class FmDemodulator
{
    private readonly Fir iFilter, qFilter, audioFilter;
    private readonly bool wide;
    private readonly IqDecimator frontEnd;
    private double previousI, previousQ, previousAudio, highPass, deEmphasis;
    private int decimation;
    private readonly BroadcastStereo? broadcast;
    private readonly RdsDecoder? rds;
    internal RdsState? Rds => rds?.State;
    internal byte[] StereoAudio { get; private set; } = [];
    internal bool CaptureData { get; set; }
    internal double[] DataSamples { get; private set; } = [];
    private double dataSum;
    internal BroadcastState? Broadcast => broadcast?.State;
    internal bool ForceMono { set { if (broadcast != null) broadcast.ForceMono = value; } }
    internal double ChannelDb { get; private set; } = -120;
    internal FmDemodulator(bool wide, int sampleRate = RtlNative.SampleRate, double offsetHz = 0, double bandwidth = 0)
    {
        this.wide = wide;
        if (wide) { broadcast = new(); rds = new(); }
        frontEnd = new IqDecimator(sampleRate, offsetHz);
        double cutoff = bandwidth == 0 ? (wide ? 95000 : 8000) : bandwidth / 2;
        iFilter = new Fir(129, cutoff);
        qFilter = new Fir(129, cutoff);
        audioFilter = new Fir(129, wide ? 15000 : 3000);
    }
    internal byte[] Process(byte[] iq)
    {
        var output = new List<byte>(iq.Length / 5);
        var data = CaptureData && !wide ? new List<double>() : null;
        double channelPower = 0;
        double scale = RtlNative.SampleRate / (2 * Math.PI * (wide ? 75000 : 5000));
        var baseband = frontEnd.Process(iq);
        var composite = wide ? new double[baseband.Length] : null;
        int compositeIndex = 0;
        foreach (var input in baseband)
        {
            double i = iFilter.Push(input.I);
            double q = qFilter.Push(input.Q);
            channelPower += i * i + q * q;
            double fm = Math.Atan2(q * previousI - i * previousQ, i * previousI + q * previousQ) * scale;
            previousI = i; previousQ = q;
            if (composite != null) composite[compositeIndex++] = fm;
            double filtered = audioFilter.Push(fm);
            dataSum += fm * 5000;
            if (++decimation != 5) continue;
            decimation = 0;
            data?.Add(dataSum / 5);dataSum=0;
            highPass = filtered - previousAudio + .995 * highPass;
            previousAudio = filtered;
            double audio = highPass;
            if (wide) { deEmphasis += (1 - Math.Exp(-1.0 / (48000 * 75e-6))) * (audio - deEmphasis); audio = deEmphasis; }
            short sample = (short)Math.Clamp(audio * 24000, short.MinValue, short.MaxValue);
            output.Add((byte)sample); output.Add((byte)(sample >> 8));
        }
        ChannelDb = 10 * Math.Log10(Math.Max(1e-12, channelPower / Math.Max(1, baseband.Length)));
        DataSamples=data?.ToArray() ?? [];
        if (broadcast != null) { StereoAudio = broadcast.Process(composite!); rds!.Process(composite!); }
        return output.ToArray();
    }
    internal static float[] RfSpectrum(byte[] iq)
    {
        if (iq.Length < 4096) return Enumerable.Repeat(-120f, 1024).ToArray();
        var values = new Complex[2048];
        for (int n = 0; n < 2048; n++)
        {
            float window = (float)(.5 - .5 * Math.Cos(2 * Math.PI * n / 2047));
            values[n].X = (iq[n * 2] - 127.5f) / 128 * window;
            values[n].Y = (iq[n * 2 + 1] - 127.5f) / 128 * window;
        }
        FastFourierTransform.FFT(true, 11, values);
        var result = new float[1024];
        for (int n = 0; n < 1024; n++)
        {
            double power = 0;
            for (int k = 0; k < 2; k++)
            {
                var bin = values[(n * 2 + k + 1024) % 2048];
                power = Math.Max(power, bin.X * bin.X + bin.Y * bin.Y);
            }
            result[n] = (float)Math.Max(-120, 10 * Math.Log10(Math.Max(1e-12, power * 4)));
        }
        return result;
    }
    internal sealed class Fir
    {
        private readonly double[] coefficients, history;
        private int position;
        internal Fir(int taps, double cutoff)
        {
            history = new double[taps]; coefficients = new double[taps];
            double f = cutoff / RtlNative.SampleRate, sum = 0;
            for (int n = 0; n < taps; n++)
            {
                int m = n - (taps - 1) / 2;
                coefficients[n] = (m == 0 ? 2 * f : Math.Sin(2 * Math.PI * f * m) / (Math.PI * m)) * (.54 - .46 * Math.Cos(2 * Math.PI * n / (taps - 1)));
                sum += coefficients[n];
            }
            for (int n = 0; n < taps; n++) coefficients[n] /= sum;
        }
        internal double Push(double value)
        {
            history[position] = value;
            double sum = 0; int index = position;
            for (int n = 0; n < coefficients.Length; n++) { sum += coefficients[n] * history[index]; if (--index < 0) index = history.Length - 1; }
            if (++position == history.Length) position = 0;
            return sum;
        }
    }
}
