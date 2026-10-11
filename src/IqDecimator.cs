using System.Numerics;
namespace MRS;

// Anti-alias FIR evaluated only on output samples. Retains state across USB blocks.
internal sealed class IqDecimator
{
    private readonly int factor;
    private readonly int inputRate;
    private readonly double[][] kernels;
    private readonly double[] coefficients, iHistory, qHistory;
    private readonly int historyLength;
    private int position, phase;
    private readonly double shiftStep;
    private double shiftPhase;
    internal IqDecimator(int rate, double offsetHz = 0)
    {
        shiftStep = -2 * Math.PI * offsetHz / rate;
        if (!ReceiverCapabilities.SupportsDspRate(rate)) throw new ArgumentOutOfRangeException(nameof(rate));
        inputRate = rate;
        factor = (int)Math.Ceiling(rate / 240000.0);
        int taps = factor == 1 ? 1 : factor * 32 + 1;
        historyLength = taps;
        int padded = ((taps + Vector<double>.Count - 1) / Vector<double>.Count) * Vector<double>.Count;
        coefficients = new double[taps]; iHistory = new double[padded * 2]; qHistory = new double[padded * 2];
        kernels = new double[rate % 240000 != 0 ? 3 : 1][];
        if (factor == 1) { coefficients[0] = 1; kernels[0]=coefficients; return; }
        for(int fractional=0;fractional<kernels.Length;fractional++)
        {
        var kernel=new double[padded];
        double sum = 0, cutoff = 105000.0 / rate;
        for (int n = 0; n < taps; n++)
        {
            double m = n - taps / 2 - fractional / 3.0;
            kernel[n] = (m == 0 ? 2 * cutoff : Math.Sin(2 * Math.PI * cutoff * m) / (Math.PI * m)) * (.42 - .5 * Math.Cos(2 * Math.PI * n / (taps - 1)) + .08 * Math.Cos(4 * Math.PI * n / (taps - 1)));
            sum += kernel[n];
        }
        for (int n = 0; n < taps; n++) kernel[n] /= sum;
        kernels[fractional]=kernel;
        }
    }
    internal (double I, double Q)[] Process(byte[] bytes)
    {
        var output = new List<(double, double)>(bytes.Length / (factor * 2));
        for (int n = 0; n + 1 < bytes.Length; n += 2)
        {
            double i = (bytes[n] - 127.5) / 128, q = (bytes[n + 1] - 127.5) / 128;
            if (shiftStep != 0)
            {
                double cos = Math.Cos(shiftPhase), sin = Math.Sin(shiftPhase);
                (i, q) = (i * cos - q * sin, i * sin + q * cos);
                shiftPhase = Math.IEEERemainder(shiftPhase + shiftStep, 2 * Math.PI);
            }
            if (factor == 1) { output.Add((i, q)); continue; }
            if (--position < 0) position = historyLength - 1;
            iHistory[position] = iHistory[position + historyLength] = i; qHistory[position] = qHistory[position + historyLength] = q;
            phase += 240000;
            if (phase >= inputRate)
            {
                phase -= inputRate; double outI = 0, outQ = 0;
                // 3.2 and 20 MHz need three fractional-delay phases at 240 kHz output.
                var kernel=kernels[kernels.Length==1 ? 0 : phase/80000];
                var vi = Vector<double>.Zero; var vq = Vector<double>.Zero;
                for (int k = 0; k < kernel.Length; k += Vector<double>.Count)
                {
                    var h = new Vector<double>(kernel, k);
                    vi += h * new Vector<double>(iHistory, position + k); vq += h * new Vector<double>(qHistory, position + k);
                }
                outI = Vector.Sum(vi); outQ = Vector.Sum(vq);
                output.Add((outI, outQ));
            }
        }
        return output.ToArray();
    }
}
