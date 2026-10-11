namespace MRS;
internal static class RdsTests
{
    internal static List<bool> Bits(ushort[] words)
    {
        var result = new List<bool>(); uint[] offsets = [0xFC,0x198,(words[1] & 0x800) != 0 ? 0x350u : 0x168u,0x1B4];
        for (int b = 0; b < 4; b++)
        {
            // Independent bit-at-a-time encoder for x^10+x^8+x^7+x^5+x^4+x^3+1.
            uint remainder = 0;
            for (int n = 15; n >= -10; n--) { remainder = (remainder << 1) | (n >= 0 ? (uint)((words[b] >> n) & 1) : 0); if ((remainder & 1024) != 0) remainder ^= 0x5B9; }
            uint block = ((uint)words[b] << 10) | (remainder ^ offsets[b]);
            for (int n = 25; n >= 0; n--) result.Add((block & (1u << n)) != 0);
        }
        return result;
    }
    internal static List<bool> StationBits()
    {
        var result = new List<bool>(); string ps = "MRS TEST";
        for (int round = 0; round < 24; round++)
        {
            for (int segment = 0; segment < 4; segment++) result.AddRange(Bits([0x1234,(ushort)((10 << 5) | segment),0,(ushort)((ps[segment*2] << 8) | ps[segment*2+1])]));
            result.AddRange(Bits([0x1234,0x2140,0x4845,0x4C4C]));
            result.AddRange(Bits([0x1234,0x2141,0x4F0D,0x2020]));
        }
        return result;
    }
    internal static double[] Signal(double seconds, double offset = 0, bool stereo = false)
    {
        var bits = StationBits(); bool sign = false; var symbols = new double[bits.Count];
        for (int n = 0; n < bits.Count; n++) { sign ^= bits[n]; symbols[n] = sign ? 1 : -1; }
        var output = stereo ? BroadcastTests.Composite(seconds) : new double[(int)(seconds * 240000)];
        double filtered1 = 0, filtered2 = 0;
        for (int n = 0; n < output.Length; n++)
        {
            double t = n / 240000.0, clock = t * (1187.5 * (1 + offset / 57000)) + .317;
            int index = (int)clock % symbols.Length;
            double symbol = symbols[index] * (clock - Math.Floor(clock) < .5 ? 1 : -1);
            filtered1 += .09 * (symbol - filtered1); filtered2 += .09 * (filtered1 - filtered2);
            output[n] += .045 * filtered2 * Math.Cos(2 * Math.PI * (57000 + offset) * t + .4);
        }
        return output;
    }
    internal static void Run(Action<bool,string> check)
    {
        var framing = new RdsGroups(); int accepted = 0; framing.Group += _ => accepted++;
        var valid = Bits([0x1234,0x0140,0,0x4D52]); foreach (var bit in valid) framing.Push(bit);
        check(accepted == 1, "RDS checks all four CRC/offset blocks before accepting a group");
        valid[60] = !valid[60]; foreach (var bit in valid) framing.Push(bit);
        check(accepted == 1, "RDS rejects a corrupted group without displaying its fields");
        var parser = new RdsText(); var groups = new RdsGroups(); groups.Group += parser.Accept;
        foreach (var bit in StationBits()) groups.Push(bit);
        var state = parser.Snapshot(0);
        check(state.Pi == 0x1234 && state.Ps == "MRS TEST" && state.Text == "HELLO" && state.Pty == 10, $"RDS confirms repeated PS and RadioText with PI and PTY ({state.Pi:X4}, {state.Ps}, {state.Text}, {state.Pty}, {state.Groups})");
        parser.Accept([0x5678,0,0,0]); state = parser.Snapshot(0);
        check(state.Ps == "" && state.Text == "" && state.Pi == 0x5678, "RDS station change discards previous station text");
        foreach (double offset in new[] { -3.0, 0, 3.0 })
        {
            var decoder = new RdsDecoder(); var signal = Signal(5, offset);
            for (int n = 0; n < signal.Length; n += 8191) decoder.Process(signal.AsSpan(n,Math.Min(8191,signal.Length-n)));
            state = decoder.State;
            check(state.Pi == 0x1234 && state.Ps == "MRS TEST" && state.Text == "HELLO", $"RDS recovers independent-carrier MPX at {offset:+0;-0;0} Hz offset (groups {state.Groups}, PS {state.Ps}, RT {state.Text})");
        }
        var silent = new RdsDecoder(); silent.Process(new double[240000]);
        check(silent.State.Pi == null && silent.State.Groups == 0, "RDS silence does not invent a station");
        var noise = new Random(29017); var noiseOnly = new double[240000];
        for(int n=0;n<noiseOnly.Length;n++) noiseOnly[n] = (noise.NextDouble()-.5)*.3;
        silent.Process(noiseOnly);
        check(silent.State.Pi == null && silent.State.Groups == 0, "RDS rejects deterministic broadband noise without false station metadata");
        var noisySignal = Signal(5,stereo:true);
        for(int n=0;n<noisySignal.Length;n++) noisySignal[n] += (noise.NextDouble()-.5)*.02;
        var noisyDecoder = new RdsDecoder(); noisyDecoder.Process(noisySignal);
        check(noisyDecoder.State.Ps == "MRS TEST" && noisyDecoder.State.Text == "HELLO", "RDS decodes stereo composite with added broadband noise");
        noisyDecoder.Process(new double[240000 * 11]);
        check(noisyDecoder.State.AgeSeconds > 10, "RDS loss ages previous metadata beyond the display freshness limit");
        var versionB = new RdsGroups(); var textB = new RdsText(); versionB.Group += textB.Accept;
        for(int round=0;round<2;round++)
            for(int segment=0;segment<3;segment++)
                foreach(bool bit in Bits([0x4321,(ushort)(0x2800|segment),0x4321,new ushort[]{0x4845,0x4C4C,0x4F0D}[segment]])) versionB.Push(bit);
        check(textB.Snapshot(0).Text == "HELLO", "RDS 2B RadioText uses C-prime and repeated PI correctly");
        textB.Accept([0x4321,0x2810,0x4321,0x4E45]);
        check(textB.Snapshot(0).Text == "", "RDS RadioText A/B change clears the previous message before completion");
        var mixed = Signal(5, stereo:true);
        foreach (int rate in ReceiverStream.Rates)
        {
            var dsp = new ReceiveDemodulator("WFM", rate); var audio = new List<byte>();
            var iq = Modulate(mixed, rate);
            for (int n = 0; n < iq.Length; n += 16384) { dsp.Process(iq.AsSpan(n,Math.Min(16384,iq.Length-n)).ToArray()); audio.AddRange(dsp.StereoAudio); }
            check(dsp.Rds?.Ps == "MRS TEST" && dsp.Rds.Text == "HELLO" && dsp.Broadcast?.Stereo == true, $"WFM I/Q at {rate} S/s decodes stereo and RDS together");
            var pcm = audio.ToArray();
            check(BroadcastTests.Tone(pcm,0,1000) > BroadcastTests.Tone(pcm,1,1000) * 15 && BroadcastTests.Tone(pcm,1,2300) > BroadcastTests.Tone(pcm,0,2300) * 15, $"WFM I/Q at {rate} S/s retains over 23 dB stereo separation");
        }
    }
    internal static byte[] Modulate(double[] composite, int rate)
    {
        var iq = new byte[(int)((long)composite.Length * rate / 240000) * 2]; double phase = 0;
        for (int n = 0; n < iq.Length / 2; n++)
        {
            long position = (long)n * 240000; int index = (int)(position / rate); double fraction = (position % rate) / (double)rate;
            double value = composite[index] * (1 - fraction) + composite[Math.Min(index + 1, composite.Length - 1)] * fraction;
            phase = (phase + 2 * Math.PI * 75000 * value / rate) % (2 * Math.PI);
            iq[n*2] = (byte)Math.Clamp(Math.Round(127.5 + 85 * Math.Cos(phase)),0,255);
            iq[n*2+1] = (byte)Math.Clamp(Math.Round(127.5 + 85 * Math.Sin(phase)),0,255);
        }
        return iq;
    }
}


