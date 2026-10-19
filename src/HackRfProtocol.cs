using System.IO;
namespace MRS;

// Gain tokens are transport values, not tenths of dB. Backend metadata identifies their meaning.
internal sealed record HackRfGain(int Lna = 16, int Vga = 16, bool Amplifier = false, bool HfFilter = false)
{
    internal int Token => 0x1000000 | Lna | (Vga << 8) | (Amplifier ? 0x10000 : 0) | (HfFilter ? 0x20000 : 0);
    internal static HackRfGain Decode(int token)
    {
        var value = new HackRfGain(token & 255, (token >> 8) & 255, (token & 0x10000) != 0, (token & 0x20000) != 0);
        if (value.Token != token || value.Lna > 40 || value.Lna % 8 != 0 || value.Vga > 62 || value.Vga % 2 != 0)
            throw new ArgumentException("Invalid HackRF LNA/VGA gain settings.");
        return value;
    }
    public override string ToString() => $"LNA {Lna} dB / VGA {Vga} dB / RF amp {(Amplifier ? "on" : "off")}";
}

internal static class HackRfProtocol
{
    internal const int Magic = 0x4D525310, HeaderSize = 32;
    internal static int UsableBandwidth(int rate) => rate switch { 2400000 => 1750000, 9600000 => 9000000, 19200000 or 20000000 => 15000000, _ => throw new ArgumentOutOfRangeException(nameof(rate)) };
    internal static int CaptureBandwidth(int rate,int gain) => HackRfGain.Decode(gain).HfFilter ? Math.Min(1750000,UsableBandwidth(rate)) : UsableBandwidth(rate);
    internal static void Normalize(byte[] data) { for (int n = 0; n < data.Length; n++) data[n] ^= 0x80; }
    internal static void Write(Stream output, byte[] data, long frequency, int rate, int gain)
    {
        using var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, true);
        writer.Write(Magic); writer.Write(data.Length); writer.Write(frequency); writer.Write(rate);
        writer.Write(gain); writer.Write(gain); writer.Write(CaptureBandwidth(rate,gain)); writer.Write(data); writer.Flush();
    }
    internal static ReceiverStream.FrameHeader Header(byte[] bytes)
    {
        if (bytes.Length != HeaderSize) throw new IOException("Invalid HackRF header length.");
        using var r = new BinaryReader(new MemoryStream(bytes));
        int magic = r.ReadInt32(), count = r.ReadInt32(); long frequency = r.ReadInt64();
        int rate = r.ReadInt32(), requested = r.ReadInt32(), applied = r.ReadInt32(), bandwidth = r.ReadInt32();
        if (magic != Magic || count < 4096 || count > 32768 || count % 2 != 0 ||
            !ReceiverCapabilities.HackRf.CanTune(frequency) || !ReceiverCapabilities.HackRf.SampleRates.Contains(rate))
            throw new IOException("Invalid HackRF capture metadata.");
        if (bandwidth != CaptureBandwidth(rate,applied)) throw new IOException("Unexpected HackRF analog bandwidth.");
        HackRfGain.Decode(requested); HackRfGain.Decode(applied);
        return new(count, frequency, rate, requested, applied, 0, bandwidth);
    }
    internal sealed class Commands
    {
        private long frequency;
        private int rate, gain;
        internal volatile bool Stopped;
        internal Commands(bool readConsole = true)
        {
            if (readConsole) _ = Task.Run(() => { try { string? line; while ((line = Console.ReadLine()) != null) Accept(line); } catch (IOException) { } });
        }
        internal void Accept(string line)
        {
            if (line == "STOP") Stopped = true;
            if (line.StartsWith("TUNE ") && long.TryParse(line.AsSpan(5), out long hz) && ReceiverCapabilities.HackRf.CanTune(hz)) Interlocked.Exchange(ref frequency, hz);
            if (line.StartsWith("RATE ") && int.TryParse(line.AsSpan(5), out int r) && ReceiverCapabilities.HackRf.SampleRates.Contains(r)) Interlocked.Exchange(ref rate, r);
            if (line.StartsWith("GAIN ") && int.TryParse(line.AsSpan(5), out int g))
                try { HackRfGain.Decode(g); Interlocked.Exchange(ref gain, g); } catch (ArgumentException) { }
        }
        internal (long Frequency, int Rate, int Gain) Take() => (Interlocked.Exchange(ref frequency, 0), Interlocked.Exchange(ref rate, 0), Interlocked.Exchange(ref gain, 0));
    }
    internal static void Fixture(long frequency, int rate, int gain, int seconds)
    {
        using var output = Console.OpenStandardOutput(); var commands = new Commands();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        byte[] block = SelfTests.MakeFm(false, 16384, rate);
        while (clock.Elapsed.TotalSeconds < seconds && !commands.Stopped)
        {
            var next = commands.Take(); if (next.Frequency != 0) frequency = next.Frequency;
            if (next.Rate != 0) { rate = next.Rate; block = SelfTests.MakeFm(false, 16384, rate); }
            if (next.Gain != 0) gain = next.Gain;
            Write(output, block, frequency, rate, gain);
            Thread.Sleep(Math.Max(1, (int)(16384 * 1000.0 / rate)));
        }
    }
}
