namespace MRS;

internal sealed record RailFrame(ulong Data, uint Check)
{
    internal int Field(int offset, int width) => (int)(Data >> offset & ((1UL << width) - 1));
    internal static uint Remainder(ulong codeword)
    {
        const ulong polynomial = 0b1111001101000001111;
        for (int bit = 62; bit >= 18; bit--)
            if ((codeword & (1UL << bit)) != 0)
                codeword ^= polynomial << (bit - 18);
        return (uint)codeword;
    }
    internal bool Valid => Remainder((Data << 18) | (Check ^ 0b101011011101110000)) == 0;
    internal string Raw => FormattableString.Invariant($"{Data:X12}/{Check:X5}");
    internal string Summary(bool hot) => hot
        ? $"Head-end frame · type bits {Convert.ToString(Field(4, 3), 2).PadLeft(3, '0')} (meaning unresolved)"
        : $"Unit {Field(7, 17)} · {Field(24, 7)} psig · battery charge field {Field(31, 7) * 100 / 127}% · motion {Field(42, 1)} · marker {Field(44, 1)}";
}

internal sealed class RailDecoder
{
    private const int Symbol = 10; // 12 kHz discriminator / 1200 baud.
    private const uint Sync = 0b10101011100010010;
    private readonly double[] window = new double[Symbol];
    private static readonly (double[] Cos, double[] Sin, double Cc, double Ss, double Cs)[] Tones =
        new[] { 1200,1800 }.Select(Tone).ToArray();
    private readonly ulong[] payload = new ulong[Symbol];
    private readonly uint[] history = new uint[Symbol], checks = new uint[Symbol];
    private readonly int[] count = new int[Symbol];
    private int decimate, position, filled, phase;
    private double sum;
    private long samples, lastFrame = -1000;
    internal event Action<RailFrame>? Frame;

    private static (double[],double[],double,double,double) Tone(int hz)
    {
        var c=Enumerable.Range(0,Symbol).Select(n=>Math.Cos(n*2*Math.PI*hz/12000)).ToArray();
        var s=Enumerable.Range(0,Symbol).Select(n=>Math.Sin(n*2*Math.PI*hz/12000)).ToArray();
        double cm=c.Average(), sm=s.Average();
        for(int i=0;i<Symbol;i++){c[i]-=cm;s[i]-=sm;}
        return (c,s,c.Sum(v=>v*v),s.Sum(v=>v*v),c.Zip(s).Sum(v=>v.First*v.Second));
    }
    internal void Feed(ReadOnlySpan<double> pcm)
    {
        foreach (double sample in pcm)
        {
            sum += sample;
            if (++decimate != 4) continue;
            window[position] = sum / 4;
            position = (position + 1) % Symbol;
            decimate = 0;
            sum = 0;
            if (filled < Symbol && ++filled < Symbol) continue;
            samples++;
            phase = (phase+1)%Symbol;
            int bit = Power(0) > Power(1) ? 1 : 0;
            history[phase] = ((history[phase] << 1) | (uint)bit) & 0x1ffff;
            if (count[phase] > 0)
            {
                int offset = count[phase]++ - 1;
                if (offset < 45) payload[phase] |= (ulong)bit << offset;
                else checks[phase] = (checks[phase]<<1) | (uint)bit;
                if (offset == 62)
                {
                    count[phase] = 0;
                    var frame = new RailFrame(payload[phase], checks[phase]);
                    if (frame.Valid && samples-lastFrame >= Symbol)
                    {
                        lastFrame = samples;
                        Frame?.Invoke(frame);
                    }
                }
            }
            if (history[phase] == Sync)
            {
                count[phase]=1;
                payload[phase]=0;
                checks[phase]=0;
            }
        }
    }
    private double Power(int index)
    {
        var tone = Tones[index];
        double c=0,s=0;
        for (int n=0;n<Symbol;n++)
        {
            double x=window[(position+n)%Symbol];
            c+=x*tone.Cos[n]; s+=x*tone.Sin[n];
        }
        return (tone.Ss*c*c-2*tone.Cs*c*s+tone.Cc*s*s)/(tone.Cc*tone.Ss-tone.Cs*tone.Cs);
    }
}
