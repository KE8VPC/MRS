namespace MRS;
internal static class SamplingModes
{
    internal const int Quadrature = 0, DirectI = 1, DirectQ = 2;
    internal static bool Valid(int mode) => mode is >= 0 and <= 2;
    internal static string Name(int mode) => mode switch { 0 => "Quadrature / tuner", 1 => "Direct sampling — I branch", 2 => "Direct sampling — Q branch", _ => "Unknown" };
    internal static bool CanTune(long hz, int mode) => Valid(mode) && (mode == 0 ? ReceiverCapabilities.Rtl.CanTune(hz) : hz is >= 1 and <= 28800000);
    internal static string Range(int mode) => mode == 0 ? "25–1750 MHz" : "1 Hz–28.8 MHz (selectable; reception unverified)";
}
