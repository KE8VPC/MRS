using NAudio.Dsp;

namespace MRS;
public static class SignalMath
{
    public const int FftSize = 2048;
    public static float[] Spectrum(float[] input)
    {
        var fft = new Complex[FftSize];
        for (int i = 0; i < Math.Min(input.Length, FftSize); i++)
            fft[i].X = input[i] * (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1)));
        FastFourierTransform.FFT(true, 11, fft);
        var result = new float[FftSize / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = (float)Math.Max(-120, 20 * Math.Log10(Math.Max(1e-12, 4 * Math.Sqrt(fft[i].X * fft[i].X + fft[i].Y * fft[i].Y))));
        return result;
    }
    public static double Level(float[] samples) => samples.Length == 0 ? -120 :
        Math.Max(-120, 20 * Math.Log10(Math.Max(1e-12, Math.Sqrt(samples.Sum(v => (double)v * v) / samples.Length))));
    public static byte[] ListeningPcm(float[] samples, double level, float threshold, float volume, bool? rfGateOpen = null)
    {
        var result = new byte[samples.Length * 2];
        float gain = (rfGateOpen ?? (level >= threshold)) ? Math.Clamp(volume, 0, 1) : 0;
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)Math.Clamp(samples[i] * gain * 32767, short.MinValue, short.MaxValue);
            result[i * 2] = (byte)value; result[i * 2 + 1] = (byte)(value >> 8);
        }
        return result;
    }
}
