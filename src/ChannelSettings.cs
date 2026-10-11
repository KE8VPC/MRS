using System.IO;
namespace MRS;
internal sealed record ChannelSettings(long Frequency, string Mode, double Bandwidth = 0, bool Agc = true)
{
    internal double Width => Bandwidth == 0 ? DefaultWidth(Mode) : Bandwidth;
    internal static double DefaultWidth(string mode) => mode switch { "AM" => 10000, "USB" or "LSB" => 2700, "WFM" => 190000, _ => 16000 };
    internal static double[] Widths(string mode) => mode switch { "AM" => [4000, 6000, 8000, 10000, 15000], "USB" or "LSB" => [1800, 2400, 2700, 3000, 4000], "WFM" => [150000, 170000, 190000], _ => [8000, 12500, 16000, 20000, 25000] };
    internal (double Low, double High) Edges => Mode switch { "USB" => (300, 300 + Width), "LSB" => (-300 - Width, -300), _ => (-Width / 2, Width / 2) };
    internal void Validate()
    {
        if (Frequency < 0 || !ReceiveDemodulator.Modes.Contains(Mode) || !double.IsFinite(Width) || !Widths(Mode).Contains(Width)) throw new IOException("Unsupported channel mode or filter bandwidth.");
    }
    internal bool Fits(long center, double usableBandwidth)
    {
        var edges = Edges; double offset = Frequency - (double)center;
        return offset + edges.Low >= -usableBandwidth / 2 && offset + edges.High <= usableBandwidth / 2;
    }
}
