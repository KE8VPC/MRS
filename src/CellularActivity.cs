namespace MRS;

internal sealed record CellularBand(string Name, int LowMHz, int HighMHz)
{
    public override string ToString()=>$"{Name} · {LowMHz}–{HighMHz} MHz";
}
internal sealed record CellularEnergy(double LowHz,double HighHz,float Peak);
internal sealed record CellularSnapshot(double LowHz,double HighHz,float Floor,CellularEnergy[] Activity);

internal static class CellularActivity
{
    // Selected US mobile allocations. Edges are references, never an RF classification.
    internal static readonly CellularBand[] Bands = [
        new("600 downlink",617,652),new("600 uplink",663,698),
        new("Lower 700 downlink",716,746),new("Lower 700 uplink",698,716),
        new("Upper 700 downlink",746,757),new("Upper 700 uplink",776,787),
        new("Cellular 850 uplink",824,849),new("Cellular 850 downlink",869,894),
        new("Broadband PCS uplink",1850,1915),new("Broadband PCS downlink",1930,1995),
        new("AWS-1 uplink",1710,1755),new("AWS-1 downlink",2110,2155),
        new("AWS-3 uplink",1755,1780),new("AWS-3 downlink",2155,2180),
        new("2.5 GHz shared direction",2496,2690),new("3.45 GHz shared direction",3450,3550),
        new("CBRS shared direction",3550,3700),new("3.7 GHz shared direction",3700,3980)
    ];

    internal static CellularSnapshot? Measure(float[] bins,long center,int rate,double usable,CellularBand band,int margin)
    {
        if(bins.Length==0||rate<=0||usable<=0)return null;
        double step=(double)rate/bins.Length;
        double origin=center-rate/2.0;
        double low=Math.Max(center-Math.Min(usable,rate)/2,band.LowMHz*1e6);
        double high=Math.Min(center+Math.Min(usable,rate)/2,band.HighMHz*1e6);
        int first=Math.Clamp((int)Math.Ceiling((low-origin)/step),0,bins.Length);
        int end=Math.Clamp((int)Math.Ceiling((high-origin)/step),0,bins.Length);
        if(end-first<4)return null;
        int dc=bins.Length/2;
        var floorBins=new List<float>(1024);
        for(int n=first;n<end;n+=Math.Max(1,(end-first)/1024))
            if(Math.Abs(n-dc)>2&&float.IsFinite(bins[n]))floorBins.Add(bins[n]);
        if(floorBins.Count<4)return null;
        floorBins.Sort();
        float floor=floorBins[floorBins.Count/5],above=floor+margin;
        var activity=new List<CellularEnergy>();
        int start=-1;
        float peak=float.NegativeInfinity;
        for(int n=first;n<=end;n++)
        {
            if(n<end&&Math.Abs(n-dc)>2&&float.IsFinite(bins[n])&&bins[n]>=above)
            {
                if(start<0){start=n;peak=bins[n];}
                else peak=Math.Max(peak,bins[n]);
                continue;
            }
            if(start>=0)
            {
                activity.Add(new(Math.Max(low,origin+(start-.5)*step),Math.Min(high,origin+(n-.5)*step),peak));
                start=-1;
            }
        }
        return new(low,high,floor,activity.OrderByDescending(v=>v.Peak).Take(16).OrderBy(v=>v.LowHz).ToArray());
    }
}
