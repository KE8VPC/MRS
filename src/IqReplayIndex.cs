using System.IO;
namespace MRS;
internal sealed class IqReplayIndex
{
    internal sealed record Entry(long Offset, long Ticks);
    internal List<Entry> Entries { get; } = [];
    internal long FirstTicks { get; private set; }
    internal long LastFrameTicks { get; private set; }
    internal long DurationTicks { get; private set; }
    internal string? TailError { get; private set; }
    internal static IqReplayIndex Build(IqReader reader, CancellationToken token)
    {
        var index = new IqReplayIndex(); long next = 0;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested(); long offset = reader.Position;
                var frame = reader.Read(); if (frame == null) break;
                if (index.Entries.Count == 0) { index.FirstTicks = frame.ElapsedTicks; next = frame.ElapsedTicks; }
                if (frame.ElapsedTicks >= next) { index.Entries.Add(new(offset, frame.ElapsedTicks)); next = frame.ElapsedTicks + TimeSpan.TicksPerSecond; }
                index.LastFrameTicks = frame.ElapsedTicks;
                index.DurationTicks = frame.ElapsedTicks - index.FirstTicks + (long)(TimeSpan.TicksPerSecond * (frame.Data.Length / (double)frame.Capture.BytesPerComplexSample / frame.Rate));
            }
        }
        catch (IOException ex) { index.TailError = ex.Message; }
        reader.SeekFrame(reader.DataStart); return index;
    }
    internal long OffsetFor(long ticks)
    {
        if (Entries.Count == 0) throw new IOException("Recording has no complete frames.");
        long target = FirstTicks + ticks; int lo = 0, hi = Entries.Count - 1;
        while (lo < hi) { int mid = (lo + hi + 1) / 2; if (Entries[mid].Ticks <= target) lo = mid; else hi = mid - 1; }
        return Entries[lo].Offset;
    }
}
