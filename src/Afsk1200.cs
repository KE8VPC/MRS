namespace MRS;

internal sealed class HdlcReceiver
{
    private readonly List<bool> bits = new();
    private int shift, seen, ones;
    private bool collecting;
    internal event Action<byte[]>? Frame;
    internal void Push(bool bit)
    {
        shift=(shift>>1)|(bit?128:0); seen=Math.Min(8,seen+1);
        if(collecting)bits.Add(bit);
        if(seen==8 && shift==0x7e)
        {
            if(collecting && bits.Count>8) Decode(bits.Count-8);
            bits.Clear(); collecting=true; ones=0; return;
        }
        ones=bit?ones+1:0;
        if(ones>=7 || bits.Count>2048*10) { bits.Clear(); collecting=false; }
    }
    private void Decode(int length)
    {
        var bytes=new List<byte>(); int run=0,bitIndex=0,value=0;
        for(int n=0;n<length;n++)
        {
            bool bit=bits[n];
            if(run==5) { if(bit)return; run=0;continue; }
            run=bit?run+1:0;
            if(bit)value|=1<<bitIndex;
            if(++bitIndex==8){bytes.Add((byte)value);bitIndex=value=0;}
        }
        if(bitIndex==0 && run!=5 && bytes.Count is >=17 and <=2048)Frame?.Invoke(bytes.ToArray());
    }
}

// Streaming Bell 202 AFSK with two matched-filter windows and transition-driven timing.
internal sealed class Afsk1200
{
    private sealed class Detector
    {
        private readonly double[] mi,mq,si,sq;
        private int index;
        private double mI,mQ,sI,sQ,clock;
        private bool previousTone, previousSymbol, initialized;
        internal readonly HdlcReceiver Hdlc=new();
        internal Detector(int window){mi=new double[window];mq=new double[window];si=new double[window];sq=new double[window];}
        internal void Push(double value,double mc,double ms,double sc,double ss)
        {
            mI-=mi[index];mQ-=mq[index];sI-=si[index];sQ-=sq[index];
            mI+=(mi[index]=value*mc);mQ+=(mq[index]=value*ms);sI+=(si[index]=value*sc);sQ+=(sq[index]=value*ss);
            if(++index==mi.Length)index=0;
            bool tone=mI*mI+mQ*mQ>=sI*sI+sQ*sQ;
            if(!initialized){previousTone=previousSymbol=tone;initialized=true;}
            if(tone!=previousTone)clock+=clock<20?-clock*.25:(40-clock)*.25;
            previousTone=tone;
            double before=clock; clock++;
            if(before<20 && clock>=20){Hdlc.Push(tone==previousSymbol);previousSymbol=tone;}
            if(clock>=40)clock-=40;
        }
    }
    private readonly Detector[] detectors=[new(48),new(32)];
    private double phaseMark,phaseSpace,previous,dc;
    private long samples,lastSample=-1000;
    private byte[]? last;
    internal long Samples=>samples;
    internal event Action<byte[],long>? Frame;
    internal Afsk1200()
    {
        foreach(var detector in detectors)detector.Hdlc.Frame+=raw=>
        {
            // Merge only timing-hypothesis duplicates, not later over-air repeats.
            if(last!=null && samples-lastSample<160 && raw.AsSpan().SequenceEqual(last))return;
            last=raw;lastSample=samples;Frame?.Invoke(raw,samples);
        };
    }
    internal void Process(ReadOnlySpan<byte> pcm,int channels=1)
    {
        if(channels is not (1 or 2) || pcm.Length%(channels*2)!=0)throw new ArgumentException("Expected aligned 48 kHz PCM16 mono/stereo audio.");
        for(int n=0;n<pcm.Length;n+=channels*2)
        {
            double value=(short)(pcm[n]|pcm[n+1]<<8)/32768.0;
            if(channels==2)value=(value+(short)(pcm[n+2]|pcm[n+3]<<8)/32768.0)/2;
            dc=value-previous+.995*dc;previous=value;samples++;
            double mc=Math.Cos(phaseMark),ms=Math.Sin(phaseMark),sc=Math.Cos(phaseSpace),ss=Math.Sin(phaseSpace);
            phaseMark=(phaseMark+2*Math.PI*1200/48000)%(2*Math.PI);phaseSpace=(phaseSpace+2*Math.PI*2200/48000)%(2*Math.PI);
            foreach(var detector in detectors)detector.Push(dc,mc,ms,sc,ss);
        }
    }
}

