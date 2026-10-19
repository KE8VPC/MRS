namespace MRS;
internal sealed record ReceiverInputLevel(double RmsDb,double PeakDb,double RailPercent,long Tick)
{
    internal static ReceiverInputLevel Measure(ReadOnlySpan<byte> bytes,long tick)
    {
        if(bytes.Length==0)return new(-120,-120,0,tick);
        double sum=0,peak=0;int rails=0;
        foreach(byte b in bytes){double x=(b-128)/128.0;sum+=x*x;peak=Math.Max(peak,Math.Abs(x));if(b is 0 or 255)rails++;}
        return new(10*Math.Log10(Math.Max(1e-12,sum/bytes.Length)),20*Math.Log10(Math.Max(1e-6,peak)),100.0*rails/bytes.Length,tick);
    }
}
