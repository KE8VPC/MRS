using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
namespace MRS;

internal sealed record IqMetadata(int Version, DateTimeOffset StartedUtc, bool WideFm, int RequestedGain, string? ReceiverIdentity, string? Mode = null, int SamplingMode = 0, string Backend = "RTL");
internal sealed record IqFrame(long ElapsedTicks, long Frequency, int Rate, byte[] Data, double? UsableBandwidthHz = null, IqSampleFormat Format = IqSampleFormat.Unsigned8, ChannelSettings? Channel = null, int? Gain = null, int? RequestedGain = null)
{
    internal CaptureDescriptor Capture => new(Frequency, Rate, UsableBandwidthHz ?? Rate, Format);
}
internal sealed class IqRecording : IDisposable
{
    internal const long Magic = 0x31514953524D; // MRSIQ1; little endian
    private readonly Channel<IqFrame> queue = Channel.CreateBounded<IqFrame>(new BoundedChannelOptions(64) { SingleReader = true, SingleWriter = true });
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Task writerTask;
    private long bytes;
    private readonly int version;
    private readonly string defaultMode;
    internal long Bytes => Interlocked.Read(ref bytes);
    internal volatile string? Error;
    internal IqRecording(string path, IqMetadata metadata)
    {
        if (metadata.Version is not (1 or 2 or 3)) throw new IOException("Unsupported I/Q recording version.");
        if (!SamplingModes.Valid(metadata.SamplingMode)) throw new IOException("Unsupported sampling mode.");
        ValidateBackend(metadata);
        version = metadata.Version; defaultMode = metadata.Mode ?? (metadata.WideFm ? "WFM" : "NFM");
        if (!ReceiveDemodulator.Modes.Contains(defaultMode)) throw new IOException("Unsupported recording mode.");
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 65536);
        try
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(metadata);
            using var header = new BinaryWriter(stream, Encoding.UTF8, true);
            header.Write(Magic); header.Write(json.Length); header.Write(json); header.Flush();
        }
        catch { stream.Dispose(); throw; }
        writerTask = Task.Run(async () =>
        {
            using (stream)
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                try
                {
                    await foreach (var frame in queue.Reader.ReadAllAsync())
                    {
                        writer.Write(frame.ElapsedTicks);
                        if (version == 1) ReceiverStream.Write(stream, frame.Data, frame.Data.Length, checked((uint)frame.Frequency), frame.Rate);
                        else
                        {
                            writer.Write(frame.Data.Length); writer.Write(frame.Frequency); writer.Write(frame.Rate);
                            writer.Write(frame.Capture.UsableBandwidthHz); writer.Write((int)frame.Format);
                            if (version == 3) {
                                var channel = frame.Channel ?? new ChannelSettings(frame.Frequency, "NFM");
                                writer.Write(channel.Frequency); writer.Write(Array.IndexOf(ReceiveDemodulator.Modes, channel.Mode)); writer.Write(channel.Width); writer.Write(channel.Agc); writer.Write(frame.Gain ?? -1000); writer.Write(frame.RequestedGain ?? -1000);
                            }
                            writer.Write(frame.Data);
                        }
                        Interlocked.Add(ref bytes, frame.Data.Length);
                    }
                    if (Error == null) writer.Write(-1L); // explicit clean completion marker
                    writer.Flush(); stream.Flush(true);
                }
                catch (Exception ex) { Error = "I/Q recording stopped: " + ex.Message; queue.Writer.TryComplete(); }
            }
        });
    }
    internal static void ValidateBackend(IqMetadata metadata)
    {
        if (metadata.Backend is not ("RTL" or "HackRF")) throw new IOException("Unsupported recording receiver backend.");
        if (metadata.Backend == "HackRF")
        {
            if (metadata.SamplingMode != 0 || metadata.Version != 3) throw new IOException("HackRF recording requires version 3 quadrature metadata.");
            HackRfGain.Decode(metadata.RequestedGain);
        }
    }
    internal bool Write(byte[] data, long frequency, int rate) => Write(data, new CaptureDescriptor(frequency, rate, rate, IqSampleFormat.Unsigned8));
    internal bool Write(byte[] data, CaptureDescriptor capture, ChannelSettings? channel = null, int? gain = null, int? requestedGain = null)
    {
        if (Error != null) return false;
        capture.Validate(data.Length);
        if (version == 3) channel ??= new ChannelSettings(capture.CenterHz, defaultMode);
        if (channel != null) { channel.Validate(); if (!channel.Fits(capture.CenterHz, capture.UsableBandwidthHz)) throw new IOException("Channel exceeds recorded capture coverage."); }
        if (version == 1 && (capture.CenterHz > uint.MaxValue || !ReceiverStream.ValidFrequency((uint)capture.CenterHz) || !ReceiverStream.Rates.Contains(capture.SampleRate) || capture.Format != IqSampleFormat.Unsigned8 || capture.UsableBandwidthHz != capture.SampleRate || data.Length < 4096))
            throw new IOException("Capture requires I/Q recording version 2.");
        if (clock.Elapsed > TimeSpan.FromDays(7)) { Error = "I/Q recording reached the seven-day file duration limit."; queue.Writer.TryComplete(); return false; }
        if (queue.Writer.TryWrite(new IqFrame(clock.Elapsed.Ticks, capture.CenterHz, capture.SampleRate, (byte[])data.Clone(), capture.UsableBandwidthHz, capture.Format, channel, gain, requestedGain))) return true;
        Error = "I/Q recording stopped because storage could not keep up. Complete frames remain readable; the file is marked incomplete.";
        queue.Writer.TryComplete(); return false;
    }
    public void Dispose() { queue.Writer.TryComplete(); writerTask.GetAwaiter().GetResult(); }
}

internal sealed class IqReader : IDisposable
{
    private readonly FileStream stream;
    private readonly BinaryReader reader;
    private long lastTicks;
    internal long Position => stream.Position;
    internal long DataStart { get; private set; }
    internal void SeekFrame(long offset) { stream.Position = offset; lastTicks = 0; }
    internal IqMetadata Metadata { get; }
    internal IqReader(string path)
    {
        stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        reader = new BinaryReader(stream);
        try
        {
            if (reader.ReadInt64() != IqRecording.Magic) throw new IOException("Not an M.R.S. I/Q recording.");
            int length = reader.ReadInt32();
            if (length < 2 || length > 8192) throw new IOException("Invalid I/Q metadata length.");
            byte[] json = reader.ReadBytes(length); if (json.Length != length) throw new EndOfStreamException("Incomplete I/Q header.");
            Metadata = JsonSerializer.Deserialize<IqMetadata>(json) ?? throw new IOException("Missing I/Q metadata.");
            IqRecording.ValidateBackend(Metadata);
            if (!SamplingModes.Valid(Metadata.SamplingMode)) throw new IOException("Unsupported recorded sampling mode.");
            if (Metadata.Mode != null && !ReceiveDemodulator.Modes.Contains(Metadata.Mode)) throw new IOException("Unsupported recorded receive mode.");
            if (Metadata.Version is not (1 or 2 or 3)) throw new IOException("Unsupported I/Q recording version.");
            DataStart = stream.Position;
        }
        catch { reader.Dispose(); throw; }
    }
    internal IqFrame? Read()
    {
        try
        {
            long ticks = reader.ReadInt64();
            if (ticks == -1)
            { if (stream.Position != stream.Length) throw new IOException("Unexpected data after I/Q completion marker."); return null; }
            if (ticks < lastTicks || ticks > TimeSpan.FromDays(7).Ticks) throw new IOException("Invalid I/Q frame timestamp.");
            int count; CaptureDescriptor capture;
            if (Metadata.Version == 1)
            {
                byte[] header = reader.ReadBytes(16); if (header.Length != 16) throw new EndOfStreamException();
                var info = ReceiverStream.Header(header); count = info.Count;
                capture = new(info.Frequency, info.Rate, info.Rate, IqSampleFormat.Unsigned8);
            }
            else
            {
                count = reader.ReadInt32(); capture = new(reader.ReadInt64(), reader.ReadInt32(), reader.ReadDouble(), (IqSampleFormat)reader.ReadInt32());
            }
            capture.Validate(count);
            ChannelSettings? channel = null; int? gain = null, requestedGain = null;
            if (Metadata.Version == 3)
            {
                long channelHz = reader.ReadInt64(); int modeIndex = reader.ReadInt32(); double bandwidth = reader.ReadDouble(); byte agc = reader.ReadByte();
                if (modeIndex < 0 || modeIndex >= ReceiveDemodulator.Modes.Length || agc > 1) throw new IOException("Invalid recorded channel settings.");
                channel = new(channelHz, ReceiveDemodulator.Modes[modeIndex], bandwidth, agc == 1); channel.Validate();
                if (!channel.Fits(capture.CenterHz, capture.UsableBandwidthHz)) throw new IOException("Recorded channel exceeds capture coverage.");
                gain = reader.ReadInt32(); requestedGain = reader.ReadInt32();
                if (Metadata.Backend == "HackRF") { HackRfGain.Decode(gain.Value); HackRfGain.Decode(requestedGain.Value); }
            }
            byte[] data = reader.ReadBytes(count); if (data.Length != count) throw new EndOfStreamException();
            lastTicks = ticks; return new IqFrame(ticks, capture.CenterHz, capture.SampleRate, data, capture.UsableBandwidthHz, capture.Format, channel, gain, requestedGain);
        }
        catch (EndOfStreamException) { throw new IOException("I/Q file is incomplete or truncated. Replay recovered all complete frames preceding the missing tail."); }
    }
    public void Dispose() => reader.Dispose();
}
