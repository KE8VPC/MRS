using System.Globalization;

namespace MRS;

internal sealed record SubaudibleDetection(string Protocol,string Value,string Evidence);

// Receive-only analysis of continuous, normalized 48 kHz discriminator/audio samples.
internal sealed class SubaudibleDecoder
{
    internal static readonly double[] Tones=[62.5,67,69.3,71.9,74.4,77,79.7,82.5,85.4,88.5,91.5,94.8,97.4,100,103.5,107.2,110.9,114.8,118.8,123,127.3,131.8,136.5,141.3,146.2,151.4,156.7,159.8,162.2,165.5,167.9,171.3,173.8,177.3,179.9,183.5,186.2,189.9,192.8,196.6,199.5,203.5,206.5,210.7,218.1,225.7,229.1,233.6,241.8,250.3,254.1];
    internal static readonly string[] Codes="023 025 026 031 032 036 043 047 051 053 054 065 071 072 073 074 114 115 116 122 125 131 132 134 143 145 152 155 156 162 165 172 174 205 212 223 225 226 243 244 245 246 251 252 255 261 263 265 266 271 274 306 311 315 325 331 332 343 346 351 356 364 365 371 411 412 413 423 431 432 445 446 452 454 455 462 464 465 466 503 506 516 523 526 532 546 565 606 612 624 627 631 632 654 662 664 703 712 723 731 732 734 743 754".Split(' ');
    private const int Rate=2400,Window=2400,Hop=480;
    private static readonly Dictionary<uint,string> DcsWords=BuildWords();
    private static readonly double[] Hann=Enumerable.Range(0,Window).Select(n=>.5-.5*Math.Cos(2*Math.PI*n/(Window-1))).ToArray();
    private static readonly double Weight=Hann.Sum(),WeightSquared=Hann.Sum(x=>x*x);
    private readonly LowPass low1=new(48000,300,.5411961),low2=new(48000,300,1.306563);
    private readonly LowPass dcsFilter=new(Rate,150,.7071068);
    private readonly double[] ring=new double[Window],dcsRing=new double[Window],frame=new double[Window],weighted=new double[Window],dcsFrame=new double[Window];
    private int decimation,position,filled,hop,confirmed,misses;
    private double dc;
    private string? candidate;
    internal SubaudibleDetection? Current {get;private set;}
    internal event Action<SubaudibleDetection?,SubaudibleDetection?>? Changed;

    // Systematic Golay(23,12), data in bits 0..11; transmitted least significant bit first.
    // Independent CML CMX138A Table 5 vectors are verified in SubaudibleTests.
    internal static uint DcsWord(string octal)
    {
        uint data=(uint)(0x800|Convert.ToInt32(octal,8)),remainder=data<<11;
        for(int bit=22;bit>=11;bit--)if((remainder&(1u<<bit))!=0)remainder^=0xC75u<<(bit-11);
        return (remainder<<12)|data;
    }
    private static Dictionary<uint,string> BuildWords()
    {
        var words=new Dictionary<uint,SortedSet<string>>();
        foreach(string code in Codes)for(int inverted=0;inverted<2;inverted++)
        {
            uint word=DcsWord(code)^(inverted==0?0:0x7FFFFFu);
            for(int rotation=0;rotation<23;rotation++)
            {
                if(!words.TryGetValue(word,out var labels))words[word]=labels=new(StringComparer.Ordinal);
                labels.Add(code+(inverted==0?"N":"I"));
                word=(word>>1)|((word&1)<<22);
            }
        }
        // A cyclic stream can identify two equivalent code/polarity names. Preserve both.
        return words.ToDictionary(pair=>pair.Key,pair=>string.Join(" / ",pair.Value));
    }
    internal void Process(ReadOnlySpan<double> samples)
    {
        foreach(double raw in samples)
        {
            double x=double.IsFinite(raw)?Math.Clamp(raw,-10,10):0;
            x=low2.Process(low1.Process(x));
            if(++decimation<20)continue;
            decimation=0;dc+=.002*(x-dc);x-=dc;
            ring[position]=x;dcsRing[position]=dcsFilter.Process(x);
            position=(position+1)%Window;filled=Math.Min(Window,filled+1);
            if(++hop<Hop)continue;
            hop=0;if(filled<Window)continue;
            for(int n=0;n<Window;n++){frame[n]=ring[(position+n)%Window];dcsFrame[n]=dcsRing[(position+n)%Window];}
            // Prefer a verified repeating code over one of its low-frequency harmonics.
            Update(DetectDcs()??DetectTone());
        }
    }
    private void Update(SubaudibleDetection? detection)
    {
        string? identity=detection==null?null:detection.Protocol+":"+detection.Value;
        string? active=Current==null?null:Current.Protocol+":"+Current.Value;
        if(identity!=null && identity==candidate)confirmed=Math.Min(2,confirmed+1);else{candidate=identity;confirmed=identity==null?0:1;}
        if(identity==active && identity!=null){Current=detection;misses=0;return;}
        if(detection!=null && confirmed>=2){var previous=Current;Current=detection;misses=0;Changed?.Invoke(previous,Current);return;}
        if(Current!=null && ++misses>=3){var previous=Current;Current=null;misses=0;Changed?.Invoke(previous,null);}
    }
    private SubaudibleDetection? DetectTone()
    {
        double mean=frame.Average(),energy=0,recent=0;
        for(int n=0;n<Window;n++)
        {
            double x=frame[n]-mean;weighted[n]=x*Hann[n];energy+=weighted[n]*weighted[n];
            if(n>=Window-Hop)recent+=x*x;
        }
        if(energy/WeightSquared<1e-6 || recent/Hop<1e-6)return null;
        double best=0,nominal=0;
        foreach(double tone in Tones){double power=Power(tone);if(power>best){best=power;nominal=tone;}}
        double measured=nominal;
        // Refine the frequency so an off-list tone cannot simply become the nearest label.
        for(int step=-10;step<=10;step++)
        {
            double frequency=nominal+step*.15,power=Power(frequency);
            if(power>best){best=power;measured=frequency;}
        }
        double purity=2*best/(Weight*Weight)/(energy/WeightSquared);
        if(purity<.82 || Math.Abs(measured-nominal)>Math.Max(.35,nominal*.005))return null;
        return new("CTCSS",nominal.ToString("F1",CultureInfo.InvariantCulture)+" Hz",
            FormattableString.Invariant($"Measured {measured:F2} Hz; coherent tone fraction {Math.Clamp(purity,0,1):P0}; two consecutive analysis windows. PL / TPL / QT aliases."));
    }
    private double Power(double frequency)
    {
        double coefficient=2*Math.Cos(2*Math.PI*frequency/Rate),a=0,b=0;
        foreach(double x in weighted){double next=x+coefficient*a-b;b=a;a=next;}
        return Math.Max(0,a*a+b*b-coefficient*a*b);
    }
    private SubaudibleDetection? DetectDcs()
    {
        double mean=dcsFrame.Average(),energy=dcsFrame.Sum(x=>(x-mean)*(x-mean))/Window;
        if(energy<1e-6 || dcsFrame.Skip(Window-Hop).Sum(x=>(x-mean)*(x-mean))/Hop<1e-6)return null;
        // Short overlapping windows and multiple phases/rates avoid a drifting fixed clock.
        // Acquire only after THREE identical, error-free 23-bit words; no guessed correction.
        foreach(double baud in new[]{134.4,133.8,135.0})for(int phase=0;phase<12;phase++)
        {
            double step=Rate/baud;uint word=0;int bits=0;
            var matches=new Dictionary<uint,(int Bit,int Repeats)>();
            for(double at=step*phase/12;at<Window-1;at+=step)
            {
                int i=(int)at;double x=dcsFrame[i]+(at-i)*(dcsFrame[i+1]-dcsFrame[i])-mean;
                word=(word>>1)|(x>=0?1u<<22:0);bits++;
                if(bits<23 || !DcsWords.TryGetValue(word,out string? label))continue;
                int count=matches.TryGetValue(word,out var previous)&&bits-previous.Bit==23?previous.Repeats+1:1;
                matches[word]=(bits,count);
                if(count>=3)return new("DCS",label,"Three repeated, error-free Golay(23,12) words at nominal 134.4 bit/s; two consecutive analysis windows. N = normal, I = inverted; paired names describe the same cyclic waveform. DPL / DQT / DTCS aliases.");
            }
        }
        return null;
    }
    private sealed class LowPass
    {
        private readonly double b0,b1,b2,a1,a2;private double z1,z2;
        internal LowPass(double rate,double cutoff,double q)
        {
            double w=2*Math.PI*cutoff/rate,c=Math.Cos(w),alpha=Math.Sin(w)/(2*q),a0=1+alpha;
            b0=(1-c)/(2*a0);b1=(1-c)/a0;b2=b0;a1=-2*c/a0;a2=(1-alpha)/a0;
        }
        internal double Process(double x){double y=b0*x+z1;z1=b1*x-a1*y+z2;z2=b2*x-a2*y;return y;}
    }
}
