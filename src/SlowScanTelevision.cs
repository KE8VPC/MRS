namespace MRS;

internal sealed record SstvMode(string Name, int Vis, int Width, double ScanMilliseconds)
{
    internal const int Height = 256;
    internal static readonly SstvMode[] Supported =
    [
        new("Martin M1", 44, 320, 146.432),
        new("Martin M2", 40, 160, 73.216)
    ];
    public override string ToString() => Name;
}

// Receives 48 kHz demodulated PCM. Image timing is measured from each
// horizontal sync, so sample-clock error cannot accumulate across rows.
internal sealed class SlowScanTelevision
{
    private const int Rate = 48000, ToneBlock = 240;
    private static readonly int[] HeaderTones = [1100, 1200, 1300, 1900];
    private readonly double[] coefficients;
    private readonly SstvMode? manual;
    private readonly double clockScale;
    private readonly int toneOffset;
    private readonly double[] first = new double[4], second = new double[4];
    private int blockCount, headerStage, leaderBlocks, bitNumber, vis, ones, headerUncertain;
    private double energy, nextBit, previous, baseline, crossing = -1, level;
    private double syncStart = -1, rowStart = -1;
    private long count;
    private bool rowActive, receiving, syncLost;
    private double[] totals = [], weights = [];
    private int rows;
    internal string Status { get; private set; } = "Waiting for a VIS header";
    internal SstvMode? Mode { get; private set; }
    internal bool HadSyncLoss { get; private set; }
    internal event Action<SstvMode>? ImageStarted;
    internal event Action<int, byte[]>? Line;
    internal event Action? SyncLost;

    internal SlowScanTelevision(SstvMode? manual = null, int clockPpm = 0, int toneOffset = 0)
    {
        if (clockPpm is < -10000 or > 10000 || toneOffset is < -300 or > 300)
            throw new ArgumentOutOfRangeException(nameof(clockPpm));
        this.manual = manual;
        this.toneOffset = toneOffset;
        coefficients = HeaderTones.Select(frequency =>
            2 * Math.Cos(2 * Math.PI * (frequency + toneOffset) / Rate)).ToArray();
        clockScale = 1 + clockPpm / 1e6;
        if (manual != null)
        {
            Mode = manual;
            Status = "Waiting for " + manual.Name + " line sync";
        }
    }

    internal void Feed(double value)
    {
        count++;
        baseline += .001 * (value - baseline);
        double sample = value - baseline;
        DetectHeader(sample);
        if (previous <= 0 && sample > 0)
        {
            double time = count - 1 - previous / (sample - previous);
            double period = time - crossing;
            if (crossing >= 0 && period is >= 14 and <= 55)
            {
                double frequency = Rate / period - toneOffset;
                level = Math.Clamp((frequency - 1500) / 800, 0, 1);
                FindLineSync(frequency, (time + crossing) / 2);
            }
            crossing = time;
        }
        previous = sample;
        if (rowActive) CollectPixel();
        if (receiving && !syncLost && rowStart >= 0 && count - rowStart > RowSamples() * 1.6)
        {
            rowActive = false;
            syncLost = true;
            HadSyncLoss = true;
            Status = $"Sync lost · {Mode!.Name} · {rows}/{SstvMode.Height} rows";
            SyncLost?.Invoke();
        }
    }

    private void FindLineSync(double frequency, double time)
    {
        if (frequency is >= 1080 and <= 1320)
        {
            if (syncStart < 0) syncStart = time;
            return;
        }
        if (syncStart < 0) return;
        // A crossing straddling sync and porch can briefly fall between the
        // two tone ranges. Wait for the first full porch period.
        if (frequency > 1320 && frequency < 1400) return;
        double duration = time - syncStart;
        syncStart = -1;
        // The VIS stop and first horizontal sync are contiguous 1200 Hz.
        double maximum = receiving && rows == 0 && rowStart < 0 ? Rate * .040 : Rate * .008;
        if (Mode == null || duration < Rate * .0028 || duration > maximum ||
            frequency is < 1400 or > 2450) return;
        if (receiving && rowStart >= 0 && time - rowStart < RowSamples() * .85) return;
        if (!receiving)
        {
            if (manual == null) return;
            BeginImage(manual);
        }
        // The first complete porch period is observed around half a cycle
        // after its leading edge. Compensate before mapping sample times.
        rowStart = time - Rate / 3000.0;
        rowActive = true;
        syncLost = false;
        Array.Clear(totals);
        Array.Clear(weights);
    }

    private double RowSamples() =>
        Rate / 1000.0 * (4.862 + .572 * 4 + 3 * Mode!.ScanMilliseconds) * clockScale;

    private void BeginImage(SstvMode mode)
    {
        Mode = mode;
        rows = 0;
        receiving = true;
        HadSyncLoss = false;
        syncLost = false;
        rowActive = false;
        rowStart = -1;
        syncStart = -1;
        totals = new double[mode.Width * 3];
        weights = new double[totals.Length];
        Status = mode.Name + " · waiting for line sync";
        ImageStarted?.Invoke(mode);
    }

    private void CollectPixel()
    {
        var mode = Mode!;
        double milliseconds = (count - rowStart) / (Rate / 1000.0 * clockScale) - .572;
        double channelDuration = mode.ScanMilliseconds + .572;
        int channel = (int)(milliseconds / channelDuration);
        double within = milliseconds - channel * channelDuration;
        if (milliseconds >= 0 && channel < 3 && within < mode.ScanMilliseconds &&
            crossing >= 0 && count - crossing < Rate * .003)
        {
            int x = Math.Clamp((int)(within * mode.Width / mode.ScanMilliseconds), 0, mode.Width - 1);
            // The wire order is green, blue, red. The image is RGB.
            int component = channel == 0 ? 1 : channel == 1 ? 2 : 0;
            int index = x * 3 + component;
            totals[index] += level;
            weights[index]++;
        }
        if (milliseconds < 3 * mode.ScanMilliseconds + 2 * .572) return;
        var pixels = new byte[totals.Length];
        if(weights.Count(weight=>weight==0)>mode.Width*.03&&!HadSyncLoss)
        {
            HadSyncLoss=true;
            SyncLost?.Invoke();
        }
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = weights[i] > 0 ? (byte)Math.Round(255 * totals[i] / weights[i]) : (byte)0;
        Line?.Invoke(rows++, pixels);
        rowActive = false;
        Status = $"{mode.Name} · {rows}/{SstvMode.Height} rows";
        if (rows == SstvMode.Height)
        {
            receiving = false;
            rowStart = -1;
            Status = mode.Name + (HadSyncLoss ? " · image ended with sync loss" : " · image complete");
        }
    }

    private void DetectHeader(double sample)
    {
        energy += sample * sample;
        for (int i = 0; i < first.Length; i++)
        {
            double next = sample + coefficients[i] * first[i] - second[i];
            second[i] = first[i];
            first[i] = next;
        }
        if (++blockCount < ToneBlock) return;
        int tone = 0;
        double strongest = 0;
        for (int i = 0; i < first.Length; i++)
        {
            double power = first[i] * first[i] + second[i] * second[i] -
                coefficients[i] * first[i] * second[i];
            if (power > strongest) { strongest = power; tone = HeaderTones[i]; }
        }
        if (energy < .0001 || strongest < ToneBlock * energy * .30) tone = 0;
        Array.Clear(first);
        Array.Clear(second);
        energy = 0;
        blockCount = 0;
        HeaderBlock(tone, count - ToneBlock / 2.0);
    }

    private void HeaderBlock(int tone, double time)
    {
        if(tone==0&&headerStage!=0)
        {
            if(++headerUncertain<=2)return;
            ResetHeader();return;
        }
        headerUncertain=0;
        switch (headerStage)
        {
            case 0:
                leaderBlocks = tone == 1900 ? leaderBlocks + 1 : 0;
                if (leaderBlocks >= 50) headerStage = 1;
                break;
            case 1:
                if (tone == 1200) { headerStage = 2; leaderBlocks = 0; }
                else if (tone != 1900) ResetHeader();
                break;
            case 2:
                if (tone == 1900)
                {
                    if (++leaderBlocks >= 50) headerStage = 3;
                }
                else if (tone != 1200 || leaderBlocks > 0) ResetHeader();
                break;
            case 3:
                if (tone == 1200)
                {
                    headerStage = 4;
                    nextBit = time + Rate * .040;
                    bitNumber = vis = ones = 0;
                }
                else if (tone != 1900) ResetHeader();
                break;
            case 4:
                if (time < nextBit) return;
                nextBit += Rate * .030;
                if (bitNumber < 8)
                {
                    if (tone is not (1100 or 1300)) { ResetHeader(); return; }
                    int bit = tone == 1100 ? 1 : 0;
                    ones += bit;
                    if (bitNumber < 7) vis |= bit << bitNumber;
                }
                else
                {
                    if (tone == 1200 && ones % 2 == 0)
                    {
                        var mode = SstvMode.Supported.FirstOrDefault(m => m.Vis == vis);
                        if (mode == null)
                        {
                            if(receiving){HadSyncLoss=true;SyncLost?.Invoke();}
                            receiving=rowActive=false;
                            Status = $"Unsupported SSTV VIS {vis} · raw audio can be recorded";
                        }
                        else if (manual == null || mode == manual) BeginImage(mode);
                        else Status = $"Received {mode.Name} header · selected {manual.Name}";
                    }
                    else Status = "VIS header rejected · waiting for a valid header";
                    ResetHeader();
                    return;
                }
                bitNumber++;
                break;
        }
    }

    private void ResetHeader() { headerStage = leaderBlocks = 0; }
}
