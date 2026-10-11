using System.IO;
namespace MRS;

// Transport/storage metadata is independent of a particular receiver or demodulator.
internal enum IqSampleFormat { Unsigned8 = 0, Signed8 = 1, Signed16LittleEndian = 2, Signed24LittleEndian = 3, Signed32LittleEndian = 4 }
internal sealed record CaptureDescriptor(long CenterHz, int SampleRate, double UsableBandwidthHz, IqSampleFormat Format)
{
    internal int BytesPerComplexSample => Format switch
    {
        IqSampleFormat.Unsigned8 or IqSampleFormat.Signed8 => 2,
        IqSampleFormat.Signed16LittleEndian => 4,
        IqSampleFormat.Signed24LittleEndian => 6,
        IqSampleFormat.Signed32LittleEndian => 8,
        _ => throw new IOException("Unknown I/Q sample format.")
    };
    internal long BytesPerSecond => (long)SampleRate * BytesPerComplexSample;
    internal void Validate(int byteCount)
    {
        if (CenterHz < 0 || SampleRate <= 0 || !double.IsFinite(UsableBandwidthHz) || UsableBandwidthHz <= 0 || UsableBandwidthHz > SampleRate || byteCount <= 0 || byteCount > 32768 || byteCount % BytesPerComplexSample != 0)
            throw new IOException("Invalid capture metadata or sample alignment.");
    }
    internal void RequireCurrentDsp()
    {
        if (Format != IqSampleFormat.Unsigned8 || !ReceiverCapabilities.SupportsDspRate(SampleRate))
            throw new IOException("This recording's I/Q format or sample rate is not yet supported by the FM playback processor.");
    }
}
