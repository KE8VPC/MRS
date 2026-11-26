namespace MRS;

internal sealed class RttyReceiver
{
    private const int Rate = 48000, Window = 288;
    private const string Letters = "\0E\nA SIU\rDRJNFCKTZLWHYPQOBG\0MXV\0";
    // US ITA2 figures. FIGS and LTRS are handled before table lookup.
    private const string Figures = "\03\n- \a87\r$4',!:(5\")2#6019?&\0./;\0";
    private readonly double samplesPerBit;
    private readonly bool reverse;
    private readonly double markCos, markSin, spaceCos, spaceSin;
    private readonly double[] mi = new double[Window], mq = new double[Window],
        si = new double[Window], sq = new double[Window];
    private double markX = 1, markY, spaceX = 1, spaceY;
    private double markI, markQ, spaceI, spaceQ, nextDecision, start;
    private long sample;
    private int windowIndex, clock, state, value, bit, markRun, uncertain;
    private bool figures;
    internal int Characters { get; private set; }
    internal int Rejected { get; private set; }
    internal event Action<char>? Character;

    internal RttyReceiver(double baud = 45.45, int markHz = 2125, int shiftHz = 170, bool reverse = false)
    {
        if (baud is not (45.45 or 50 or 75) || markHz is < 300 or > 3500 ||
            shiftHz is not (170 or 425 or 850) || markHz + shiftHz > 4500)
            throw new ArgumentOutOfRangeException(nameof(baud));
        samplesPerBit = Rate / baud;
        this.reverse = reverse;
        double markAngle = 2 * Math.PI * markHz / Rate;
        double spaceAngle = 2 * Math.PI * (markHz + shiftHz) / Rate;
        markCos = Math.Cos(markAngle); markSin = Math.Sin(markAngle);
        spaceCos = Math.Cos(spaceAngle); spaceSin = Math.Sin(spaceAngle);
    }

    internal void Feed(double input)
    {
        sample++;
        markI += input * markX - mi[windowIndex]; mi[windowIndex] = input * markX;
        markQ += input * markY - mq[windowIndex]; mq[windowIndex] = input * markY;
        spaceI += input * spaceX - si[windowIndex]; si[windowIndex] = input * spaceX;
        spaceQ += input * spaceY - sq[windowIndex]; sq[windowIndex] = input * spaceY;
        windowIndex = (windowIndex + 1) % Window;
        double x = markX * markCos - markY * markSin;
        markY = markY * markCos + markX * markSin; markX = x;
        x = spaceX * spaceCos - spaceY * spaceSin;
        spaceY = spaceY * spaceCos + spaceX * spaceSin; spaceX = x;
        if (++clock < 48) return;
        clock = 0;
        double markPower = markI * markI + markQ * markQ;
        double spacePower = spaceI * spaceI + spaceQ * spaceQ;
        bool valid = Math.Max(markPower, spacePower) > .00001 * Window * Window &&
            Math.Abs(markPower - spacePower) > .24 * (markPower + spacePower);
        bool mark = (markPower > spacePower) ^ reverse;
        if (state == 0)
        {
            if (!valid)
            {
                uncertain += 48;
                if (uncertain > Window) markRun = 0;
                return;
            }
            uncertain = 0;
            if (mark) markRun += 48;
            else
            {
                if (valid && markRun >= samplesPerBit * .6)
                {
                    start = sample;
                    nextDecision = start + samplesPerBit * .5;
                    value = bit = 0;
                    state = 1;
                }
                markRun = 0;
            }
            return;
        }
        if (sample < nextDecision) return;
        if (!valid || state == 1 && mark || state == 3 && !mark)
        {
            Rejected++;
            state = markRun = 0;
            return;
        }
        if (state == 1)
        {
            state = 2;
            nextDecision = start + samplesPerBit * 1.5;
        }
        else if (state == 2)
        {
            if (mark) value |= 1 << bit;
            bit++;
            nextDecision += samplesPerBit;
            if (bit == 5) state = 3;
        }
        else
        {
            state = 0;
            markRun = (int)(samplesPerBit * .5);
            if (value == 27) figures = true;
            else if (value == 31) figures = false;
            else
            {
                char character = (figures ? Figures : Letters)[value];
                if (character is not ('\0' or '\a'))
                {
                    Characters++;
                    Character?.Invoke(character);
                }
            }
        }
    }
}
