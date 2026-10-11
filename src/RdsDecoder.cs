namespace MRS;

internal sealed record RdsState(int? Pi, int Pty, string Ps, string Text, long Groups, double AgeSeconds);

internal sealed class RdsGroups
{
    private readonly uint[] blocks = new uint[4];
    private int bits;
    internal event Action<ushort[]>? Group;
    internal static uint Remainder(uint value)
    {
        for (int n = 25; n >= 10; n--) if ((value & (1u << n)) != 0) value ^= 0x5B9u << (n - 10);
        return value & 1023;
    }
    internal void Push(bool bit)
    {
        for (int n = 0; n < 3; n++) blocks[n] = ((blocks[n] << 1) | (blocks[n + 1] >> 25)) & 0x3FFFFFF;
        blocks[3] = ((blocks[3] << 1) | (bit ? 1u : 0u)) & 0x3FFFFFF;
        bits = Math.Min(104, bits + 1);
        if (bits < 104 || Remainder(blocks[0]) != 0xFC || Remainder(blocks[1]) != 0x198 || Remainder(blocks[3]) != 0x1B4) return;
        bool versionB = (blocks[1] & (0x800u << 10)) != 0;
        if (Remainder(blocks[2]) != (versionB ? 0x350u : 0x168u)) return;
        var words = blocks.Select(v => (ushort)(v >> 10)).ToArray();
        if (!versionB || words[0] == words[2]) Group?.Invoke(words);
    }
}

internal sealed class RdsText
{
    private int? pi;
    private int pty, psMask, rtMask, textFlag = -1, textWidth;
    private readonly char[] ps = new char[8], rt = new char[64];
    private string psCandidate = "", textCandidate = "", station = "", text = "";
    internal long Groups { get; private set; }
    internal RdsState Snapshot(double age) => new(pi, pty, station, text, Groups, age);
    private static char Character(int value) => value == 13 ? '\r' : value is >= 32 and <= 126 ? (char)value : '�';
    internal void Accept(ushort[] words)
    {
        if (pi != words[0]) { pi = words[0]; psMask = rtMask = 0; psCandidate = textCandidate = station = text = ""; textFlag = -1; }
        Groups++; pty = (words[1] >> 5) & 31;
        int type = words[1] >> 12; bool versionB = (words[1] & 0x800) != 0;
        if (type == 0)
        {
            int index = words[1] & 3; ps[index * 2] = Character(words[3] >> 8); ps[index * 2 + 1] = Character(words[3] & 255); psMask |= 1 << index;
            if (psMask == 15) { string value = new string(ps).TrimEnd(); if (value == psCandidate) station = value; psCandidate = value; psMask = 0; }
        }
        if (type != 2) return;
        int flag = (words[1] >> 4) & 1, width = versionB ? 2 : 4;
        if (flag != textFlag || textWidth != width) { textFlag = flag; textWidth = width; rtMask = 0; text = textCandidate = ""; Array.Fill(rt, '\0'); }
        int segment = words[1] & 15, offset = segment * width;
        if (!versionB) { rt[offset++] = Character(words[2] >> 8); rt[offset++] = Character(words[2] & 255); }
        rt[offset++] = Character(words[3] >> 8); rt[offset] = Character(words[3] & 255); rtMask |= 1 << segment;
        int length = Array.IndexOf(rt, '\r', 0, width * 16); if (length < 0) length = width * 16;
        int required = Math.Min(16, length / width + 1), mask = (1 << required) - 1;
        if ((rtMask & mask) == mask)
        {
            string value = new string(rt, 0, length).TrimEnd(); if (value == textCandidate) text = value; textCandidate = value; rtMask = 0;
        }
    }
}

// Independent 57 kHz carrier loop; timing hypotheses acquire without a stereo pilot.
// Strict CRC/offset checking only: no speculative error correction.
internal sealed class RdsDecoder
{
    private sealed class Lane
    {
        internal double Phase, Integral;
        internal bool Previous;
        internal readonly RdsGroups Frames = new();
    }
    private readonly Lane[] lanes = new Lane[16];
    private readonly RdsText messages = new();
    private double phase, correction, i1, q1, i2, q2;
    private long samples, lastGroup = -1;
    private int selected = -1;
    internal RdsState State => messages.Snapshot(lastGroup < 0 ? double.PositiveInfinity : (samples - lastGroup) / 240000.0);
    internal RdsDecoder()
    {
        for (int n = 0; n < lanes.Length; n++)
        {
            int index = n; lanes[n] = new Lane { Phase = n / 16.0 };
            lanes[n].Frames.Group += group =>
            {
                if (selected != -1 && selected != index && samples - lastGroup < 480000) return;
                selected = index; lastGroup = samples; messages.Accept(group);
            };
        }
    }
    internal void Process(ReadOnlySpan<double> input)
    {
        foreach (double value in input)
        {
            samples++;
            i1 += .06 * (value * Math.Cos(phase) - i1); q1 += .06 * (-value * Math.Sin(phase) - q1);
            i2 += .06 * (i1 - i2); q2 += .06 * (q1 - q2);
            double error = Math.Clamp(Math.Sign(i2) * q2 * 30, -.5, .5);
            correction = Math.Clamp(correction + .0000002 * error, -.001, .001);
            phase = (phase + 2 * Math.PI * 57000 / 240000 + correction + .0005 * error) % (2 * Math.PI);
            double step = (1187.5 / 240000) * (1 + correction / (2 * Math.PI * 57000 / 240000));
            foreach (var lane in lanes)
            {
                lane.Integral += lane.Phase < .5 ? i2 : -i2;
                lane.Phase += step;
                if (lane.Phase < 1) continue;
                lane.Phase -= 1; bool symbol = lane.Integral >= 0; lane.Integral = 0;
                lane.Frames.Push(symbol ^ lane.Previous); lane.Previous = symbol;
            }
        }
    }
}
