namespace MRS;

// Ten symbol-phase hypotheses at 48 kHz. Sync establishes discriminator scale/DC.
// No CQPSK/simulcast equalizer is provided in this preview.
internal sealed class P25C4fm
{
    private sealed class Phase
    {
        internal readonly double[] Window=new double[24];
        internal int Position,Count;
        internal readonly P25Framer Framer=new();
        internal double Center,Scale=1800;
        internal int SinceSync=9999;
    }
    private readonly Phase[] phases=Enumerable.Range(0,10).Select(_=>new Phase()).ToArray();
    private readonly double[] history=new double[10];
    private int index;
    private double sum;
    private long samples;
    private readonly Dictionary<string,long> recentlySeen=new();
    private static readonly double[] Signs=Enumerable.Range(0,24).Select(n=>((P25Framer.Sync>>(46-2*n))&3)==1?1.0:-1.0).ToArray();
    private static readonly double SignMean=Signs.Average();
    private static readonly double SignEnergy=Signs.Sum(s=>(s-SignMean)*(s-SignMean));
    internal event Action<P25Message>? Message;
    internal long BadNid=>phases.Sum(p=>p.Framer.BadNid);
    internal long BadCrc=>phases.Sum(p=>p.Framer.BadCrc);
    internal P25C4fm()
    {
        foreach(var p in phases)p.Framer.Message+=m=>
        {
            string key=$"{m.Nac}:{m.Duid}:"+(m.PayloadValid?Convert.ToHexString(m.Raw):"header");
            if(recentlySeen.TryGetValue(key,out long at) && samples-at<480)return;
            foreach(var stale in recentlySeen.Where(pair=>samples-pair.Value>=480).Select(pair=>pair.Key).ToArray())recentlySeen.Remove(stale);
            recentlySeen[key]=samples;Message?.Invoke(m);
        };
    }
    internal void Process(ReadOnlySpan<double> discriminator)
    {
        foreach(double value in discriminator)
        {
            if(!double.IsFinite(value))continue;
            sum+=value-history[index];history[index]=value;index=(index+1)%10;samples++;
            var phase=phases[index];double averaged=sum/10;
            phase.Window[phase.Position]=averaged;phase.Position=(phase.Position+1)%24;phase.Count=Math.Min(24,phase.Count+1);phase.SinceSync=Math.Min(9999,phase.SinceSync+1);
            if(phase.Count==24 && phase.SinceSync>=24)
            {
                double mean=0,correlation=0;
                for(int n=0;n<24;n++)mean+=phase.Window[n];mean/=24;
                // The sync word contains outer C4FM levels only. Center both sequences.
                for(int n=0;n<24;n++)correlation+=(phase.Window[(phase.Position+n)%24]-mean)*(Signs[n]-SignMean);
                double scale=correlation/SignEnergy,center=mean-scale*SignMean,residual=0;
                for(int n=0;n<24;n++){double error=phase.Window[(phase.Position+n)%24]-center-scale*Signs[n];residual+=error*error;}
                if(Math.Abs(scale)>150 && Math.Abs(scale)<6000 && residual/24<scale*scale*.10)
                {
                    phase.Scale=scale;phase.Center=center;phase.SinceSync=0;
                    phase.Framer.Begin();
                    continue;
                }
            }
            if(phase.SinceSync<360)phase.Framer.Push(Slice((averaged-phase.Center)/phase.Scale));
        }
    }
    private static int Slice(double level)=>level>=0?(level>2.0/3?1:0):(level< -2.0/3?3:2);
}
