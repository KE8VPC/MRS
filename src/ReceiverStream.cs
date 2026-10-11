using System.IO;
namespace MRS;

// Legacy frames remain the version 1 recording format; live frames carry applied controls.
internal static class ReceiverStream
{
    internal const int Magic = 0x4D525306, LiveMagic = 0x4D525309;
    internal const int LiveHeaderSize = 28;
    internal static readonly int[] Rates = ReceiverCapabilities.Rtl.SampleRates.ToArray();
    internal static bool ValidFrequency(uint hz) => ReceiverCapabilities.Rtl.CanTune(hz);
    internal static void Write(Stream output, byte[] data, int count, uint frequency, int rate)
    {
        using var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, true);
        writer.Write(Magic); writer.Write(count); writer.Write(frequency); writer.Write(rate);
        writer.Write(data, 0, count); writer.Flush();
    }
    internal static void WriteLive(Stream output, byte[] data, int count, uint frequency, int rate, int requestedGain, int appliedGain, int samplingMode = 0)
    {
        using var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, true);
        writer.Write(LiveMagic); writer.Write(count); writer.Write(frequency); writer.Write(rate); writer.Write(requestedGain); writer.Write(appliedGain); writer.Write(samplingMode);
        writer.Write(data, 0, count); writer.Flush();
    }
    internal sealed record FrameHeader(int Count, long Frequency, int Rate, int RequestedGain = -1000, int AppliedGain = -1000, int SamplingMode = 0, int Bandwidth = 0)
    { internal int UsableBandwidth => Bandwidth == 0 ? Rate : Bandwidth; }
    internal static FrameHeader Header(byte[] header)
    {
        if (header.Length is not (16 or LiveHeaderSize)) throw new IOException("Invalid receiver header size.");
        using var reader = new BinaryReader(new MemoryStream(header));
        int magic = reader.ReadInt32(), count = reader.ReadInt32(); uint frequency = reader.ReadUInt32(); int rate = reader.ReadInt32();
        if (magic != (header.Length == 16 ? Magic : LiveMagic) || count < 4096 || count > 32768 || count % 2 != 0 || !Rates.Contains(rate)) throw new IOException("Invalid receiver frame metadata.");
        if (header.Length == 16) { if (!ValidFrequency(frequency)) throw new IOException("Invalid legacy frequency."); return new(count, frequency, rate); }
        int requested = reader.ReadInt32(), applied = reader.ReadInt32(), sampling = reader.ReadInt32();
        if (!SamplingModes.CanTune(frequency, sampling)) throw new IOException("Invalid sampling mode or frequency.");
        return new(count, frequency, rate, requested, applied, sampling);
    }
    internal sealed class Commands
    {
        private long pending;
        private readonly int samplingMode;
        private int rate, gain = int.MinValue;
        internal void Accept(string line)
        {
            if (line.StartsWith("TUNE ") && uint.TryParse(line.AsSpan(5), out uint hz) && SamplingModes.CanTune(hz, samplingMode)) Interlocked.Exchange(ref pending, hz);
            if (line.StartsWith("RATE ") && int.TryParse(line.AsSpan(5), out int r) && Rates.Contains(r)) Interlocked.Exchange(ref rate, r);
            if (line.StartsWith("GAIN ") && int.TryParse(line.AsSpan(5), out int g) && (g == -1000 || g is >= 0 and <= 600)) Interlocked.Exchange(ref gain, g);
        }
        internal Commands(bool readConsole = true, int samplingMode = 0)
        {
            this.samplingMode = samplingMode;
            if (!readConsole) return;
            _ = Task.Run(() => { try { string? line; while ((line = Console.ReadLine()) != null) Accept(line); } catch (IOException) { } });
        }
        internal uint Take() => (uint)Interlocked.Exchange(ref pending, 0);
        internal int TakeRate() => Interlocked.Exchange(ref rate, 0);
        internal int TakeGain() => Interlocked.Exchange(ref gain, int.MinValue);
    }
    internal static void Fixture(int rate, int seconds = 1, int gain = -1000, int samplingMode = 0, uint frequency = 146520000)
    {
        using var output = Console.OpenStandardOutput(); var commands = new Commands(samplingMode: samplingMode);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        byte[] block = SelfTests.MakeFm(false, 16384, rate);
        while (clock.Elapsed.TotalSeconds < seconds)
        {
            uint next = commands.Take(); if (next != 0) frequency = next;
            int nextRate = commands.TakeRate(); if (nextRate != 0) { rate = nextRate; block = SelfTests.MakeFm(false, 16384, rate); }
            int nextGain = commands.TakeGain(); if (nextGain != int.MinValue) gain = nextGain;
            WriteLive(output, block, block.Length, frequency, rate, gain, gain, samplingMode);
            Thread.Sleep(Math.Max(1, (int)(16384 * 1000.0 / rate)));
        }
    }
}
