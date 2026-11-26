namespace MRS;

internal static class AdditionalReceiveTests
{
    internal static void Run(Action<bool,string> check)
    {
        SstvTests.Run(check);
        RttyTests.Run(check);
        var justBeforeMidnight = new DateTimeOffset(2026,11,16,23,59,10,TimeSpan.Zero);
        check(WeatherFaxStation.NextTune(justBeforeMidnight,TimeSpan.Zero)==justBeforeMidnight.AddSeconds(20)
            && WeatherFaxStation.NextTune(justBeforeMidnight.AddSeconds(30),TimeSpan.Zero)==justBeforeMidnight.AddDays(1).AddSeconds(20),
            "WEFAX schedules one future tune across the UTC midnight lead window");
        var honolulu=WeatherFaxStation.All.Single(s=>s.Call=="KVM70");
        check(honolulu.AvailableAt(16133100,TimeSpan.FromHours(1))&&!honolulu.AvailableAt(16133100,TimeSpan.FromHours(10)),
            "WEFAX channel windows handle overnight broadcasts");
        var alarm=new WeatherAlarmTone();
        double[] attention=Enumerable.Range(0,48000).Select(i=>Math.Sin(i*2*Math.PI*1050/48000)).ToArray();
        double[] adjacent=Enumerable.Range(0,48000).Select(i=>Math.Sin(i*2*Math.PI*1080/48000)).ToArray();
        check(!alarm.Feed(adjacent)&&!alarm.Feed(attention)&&alarm.Feed(attention)&&!alarm.Feed(attention),
            "NOAA tone detector rejects adjacent tones and reports a sustained tone once");

        const ulong payload = 0x1439d730340c;
        const uint parity = 0x38b7;
        var rail = new RailDecoder();
        var frames = new List<RailFrame>();
        rail.Frame += frames.Add;
        var sample = ModulateRail(payload,parity);
        for(int n=0;n<sample.Length;n+=311)
            rail.Feed(sample.AsSpan(n,Math.Min(311,sample.Length-n)));
        check(frames.Count==1&&frames[0].Field(7,17)==24680&&frames[0].Field(24,7)==87,
            "Rail FFSK validates an EOT frame across uneven audio blocks");
        frames.Clear();
        rail.Feed(ModulateRail(payload,parity^1));
        check(frames.Count==0,"Rail decoder rejects a changed check bit");
        check(new RailFrame(payload,parity).Summary(true).Contains("meaning unresolved"),
            "HOT display retains frame type without applying EOT field meanings");

        var cw = new ConversationCw(700,20,20);
        var decoded = new System.Text.StringBuilder();
        cw.Character += c=>decoded.Append(c);
        int index=0;
        void Key(bool on,int dots)
        {
            for(int n=0;n<2880*dots;n++,index++)cw.Feed(on ? .2*Math.Sin(index*2*Math.PI*700/48000):0);
        }
        foreach(string word in new[]{"CQ","TEST","K1ABC"})
        {
            foreach(char letter in word)
            {
                foreach(char element in MorseDecoder.Alphabet[letter]){Key(true,element=='.'?1:3);Key(false,1);}
                Key(false,2);
            }
            Key(false,4);
        }
        check(decoded.ToString().Trim()=="CQ TEST K1ABC","Amateur CW produces a continuous transcript with word spaces");

        var fax = new WeatherFax(576,120,0,0);
        var lines = new List<byte[]>();
        fax.Line += lines.Add;
        double phase=0;
        for(int n=0;n<48000;n++)
        {
            int hz=((n/8000)%3) switch { 0=>1500,1=>1900,_=>2300 };
            phase+=2*Math.PI*hz/48000;
            fax.Feed(.4*Math.Sin(phase));
        }
        check(lines.Count==2&&lines.All(r=>r.Length==1809)&&lines[0][250]<10
            &&lines[0][850] is >110 and <145&&lines[0][1520]>245,
            "WEFAX draws black, midpoint and white at the selected IOC and line timing");

        var spectrum=Enumerable.Repeat(-90f,4096).ToArray();
        spectrum[2048]=-20;
        Array.Fill(spectrum,-46f,2600,100);
        var band=CellularActivity.Bands[0];
        var activity=CellularActivity.Measure(spectrum,620000000,9600000,9000000,band,8);
        check(activity is {LowHz:617000000, HighHz:624500000, Floor:-90}
            &&activity.Activity.Length==1&&activity.Activity[0].Peak==-46,
            "Cellular survey clips measured coverage to capture and band and rejects capture-center DC");
        check(CellularActivity.Measure(spectrum,145000000,9600000,9000000,band,8)==null,
            "Cellular survey reports no coverage outside the selected band");
    }

    private static double[] ModulateRail(ulong data,uint check)
    {
        var bits=new List<int>();
        bits.AddRange("101010101010101010101011100010010".Select(c=>c-'0'));
        for(int n=0;n<45;n++)bits.Add((int)(data>>n&1));
        for(int n=17;n>=0;n--)bits.Add((int)(check>>n&1));
        var output=new List<double>(bits.Count*40);
        double phase=.6;
        foreach(int b in bits)
        {
            double increment=2*Math.PI*(b==1?1200:1800)/48000;
            for(int n=0;n<40;n++){output.Add(Math.Sin(phase));phase+=increment;}
        }
        output.AddRange(new double[80]);
        return output.ToArray();
    }
}
