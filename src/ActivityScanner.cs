namespace MRS;

internal sealed record ActivityOptions(bool AutoSquelch=false, bool ShowLines=true, bool SaveDiscoveries=true,
    double MarginDb=8, double MinimumSeconds=.3, double QuietSeconds=1.5, double GridHz=0)
{
    internal ActivityOptions Normalize()=>this with {MarginDb=Finite(MarginDb,3,30,8),MinimumSeconds=Finite(MinimumSeconds,.15,3,.3),QuietSeconds=Finite(QuietSeconds,.3,10,1.5),GridHz=new double[]{0,1000,2500,5000,6250,12500,25000,50000,100000,200000}.Contains(GridHz)?GridHz:0};
    private static double Finite(double value,double min,double max,double fallback)=>double.IsFinite(value)?Math.Clamp(value,min,max):fallback;
}
internal sealed record ActivityDiscovery(Guid TrackId,long Frequency,string Mode,double FilterHz,double OccupiedHz,
    DateTimeOffset FirstSeen,DateTimeOffset LastSeen,int Hits,double AboveFloorDb);
internal sealed record ActivityCandidate(long Frequency,double OccupiedHz,double AboveFloorDb,double PowerDb);

// UI-thread consumer of bounded, latest-only FFT snapshots. Never runs inside USB capture.
internal sealed class ActivityScanner
{
    private sealed class Track
    {
        internal Guid Id=Guid.NewGuid();internal long Frequency;internal double First,Last,Occupied,Strength;
        internal DateTimeOffset FirstUtc,LastUtc;internal bool Qualified,Dirty;internal int Hits;
    }
    private readonly List<Track> tracks=[];
    private readonly List<long> ignored=[];
    private double[] power=[],floors=[];
    private long center;private int rate;private double usable,width,binHz,lastTime=-1,startedAt,scopeLow,scopeHigh;
    private string mode="NFM";private Track? current;
    private int frames;
    internal bool Running {get;private set;}
    internal bool Held {get;set;}
    internal bool Ready {get;private set;}
    internal long? Current=>current?.Frequency;
    internal int IgnoredCount=>ignored.Count;
    internal IReadOnlyList<ActivityCandidate> Candidates {get;private set;}=[];
    internal double FloorBinDb {get;private set;}=-120;
    internal double TriggerBinDb {get;private set;}=-120;
    internal double ChannelFloorDb {get;private set;}=-120;
    internal double ChannelThresholdDb {get;private set;}=-120;
    internal double LastFrame=>lastTime;
    internal double Low=>center-usable/2;
    internal double High=>center+usable/2;
    private double Tolerance=>Math.Max(binHz*2,Math.Min(width*.3,5000));
    internal void Reset(long captureCenter,int sampleRate,double usableBandwidth,int bins,ChannelSettings channel)
    {
        Stop();center=captureCenter;rate=sampleRate;usable=usableBandwidth;width=channel.Width;mode=channel.Mode;
        binHz=rate/(double)bins;power=new double[bins];floors=[];lastTime=-1;Ready=false;frames=0;tracks.Clear();Candidates=[];
    }
    internal void Start(double low,double high)
    {
        if(power.Length==0)throw new InvalidOperationException("Wait for a live RF spectrum first.");
        if(width/binHz<4)throw new InvalidOperationException("Increase RF spectrum detail or lower sample rate: the receive filter needs at least four FFT bins for scanning.");
        scopeLow=Math.Max(Low,low);scopeHigh=Math.Min(High,high);
        if(scopeHigh-scopeLow<width)throw new InvalidOperationException("Zoom out: the scan span must fit the receive filter.");
        Running=true;Held=false;current=null;ignored.Clear();tracks.Clear();
    }
    internal void Stop(){Running=false;Held=false;current=null;ignored.Clear();}
    internal void Skip(){if(current!=null&&ignored.Count<512)ignored.Add(current.Frequency);current=null;Held=false;}
    internal void ClearIgnored()=>ignored.Clear();
    internal ActivityDiscovery[] Pending()=>tracks.Where(t=>t.Dirty).Select(t=>new ActivityDiscovery(t.Id,t.Frequency,mode,width,t.Occupied,t.FirstUtc,t.LastUtc,t.Hits,t.Strength)).ToArray();
    internal void Saved(IEnumerable<ActivityDiscovery> saved)
    {
        foreach(var item in saved)if(tracks.Find(t=>t.Id==item.TrackId) is {} t){t.Hits=0;t.Dirty=false;}
    }
    private static double Db(double value)=>10*Math.Log10(Math.Max(1e-12,value));
    private double Hz(int bin)=>center-rate/2.0+bin*binHz;
    private int Index(double hz)=>Math.Clamp((int)Math.Round((hz-center+rate/2.0)/binHz),0,power.Length-1);
    private bool Valid(int bin)=>Hz(bin)>=Low+binHz*3&&Hz(bin)<=High-binHz*3&&Math.Abs(Hz(bin)-center)>Math.Max(2000,binHz*3);
    private double FloorAt(int bin)=>floors[Math.Min(floors.Length-1,bin*floors.Length/power.Length)];
    internal long? Process(float[] spectrum,double time,DateTimeOffset utc,ChannelSettings listening,ActivityOptions options,double manualThreshold)
    {
        if(spectrum.Length!=power.Length||spectrum.Any(v=>!float.IsFinite(v))||!double.IsFinite(time)||time<=lastTime)return null;
        bool first=lastTime<0;double dt=first?.1:Math.Min(.3,time-lastTime);lastTime=time;
        if(first)startedAt=time;
        double blend=1-Math.Exp(-dt/.18);
        for(int i=0;i<power.Length;i++){double p=Math.Pow(10,spectrum[i]/10);power[i]=first?p:power[i]+blend*(p-power[i]);}
        int tiles=Math.Clamp((int)(rate/Math.Max(width*8,rate/32.0)),1,32);
        if(floors.Length!=tiles)floors=Enumerable.Repeat(1e-12,tiles).ToArray();
        for(int tile=0;tile<tiles;tile++)
        {
            int a=tile*power.Length/tiles,b=(tile+1)*power.Length/tiles;
            var noise=Enumerable.Range(a,b-a).Where(Valid).Select(i=>(double)spectrum[i]).Order().ToArray();
            if(noise.Length<8)continue;
            // A noise periodogram is exponential: correct its 20th percentile to mean bin power.
            double estimate=Math.Pow(10,noise[(int)((noise.Length-1)*.2)]/10)/(-Math.Log(.8));
            if(first)floors[tile]=estimate;
            else
            {
                double occupancy=Enumerable.Range(a,b-a).Count(i=>Valid(i)&&power[i]>floors[tile]*Math.Pow(10,options.MarginDb/10))/(double)noise.Length;
                if(estimate<floors[tile]||occupancy<.6)
                    floors[tile]+=(1-Math.Exp(-dt/(estimate>floors[tile]?8:1)))*(estimate-floors[tile]);
            }
        }
        Ready=++frames>=8&&time-startedAt>=1.2;
        var edges=listening.Edges;
        double noiseSum=0;int n=0;
        for(int i=Index(listening.Frequency+edges.Low);i<=Index(listening.Frequency+edges.High);i++){noiseSum+=FloorAt(i);n++;}
        // Hann coherent normalization has 1.5-bin ENBW. Channel gate uses integrated RF power.
        ChannelFloorDb=Db(noiseSum/1.5);
        ChannelThresholdDb=Math.Clamp(options.AutoSquelch?ChannelFloorDb+options.MarginDb:manualThreshold,-119,0);
        FloorBinDb=Db(noiseSum/Math.Max(1,n));
        TriggerBinDb=options.AutoSquelch?FloorBinDb+options.MarginDb:manualThreshold-10*Math.Log10(Math.Max(1,n)/1.5);
        if(!Running||!Ready){Candidates=[];return null;}
        var found=new List<ActivityCandidate>();
        int begin=-1,last=-1;
        void Finish()
        {
            if(begin<0)return;
            int count=last-begin+1;double occupied=count*binHz;
            if(count>=2&&occupied<=width*1.8)
            {
                double middle=(Hz(begin)+Hz(last))/2;
                double dial=mode=="USB"?middle-(width/2+300):mode=="LSB"?middle+(width/2+300):middle;
                long frequency=(long)Math.Round(options.GridHz>0?Math.Round(dial/options.GridHz)*options.GridHz:dial);
                var candidate=listening with{Frequency=frequency};var e=candidate.Edges;
                if(frequency+e.Low>=scopeLow&&frequency+e.High<=scopeHigh&&candidate.Fits(center,usable)&&
                    !(frequency+e.Low<center+Math.Max(2000,binHz*3)&&frequency+e.High>center-Math.Max(2000,binHz*3)))
                {
                    double signal=0,noise=0,instant=0;
                    for(int i=Index(frequency+e.Low);i<=Index(frequency+e.High);i++){signal+=power[i];instant+=Math.Pow(10,spectrum[i]/10);noise+=FloorAt(i);}
                    double strength=Db(signal/Math.Max(1e-12,noise)),level=Db(signal/1.5);
                    double hysteresis=current!=null&&Math.Abs(current.Frequency-frequency)<=Tolerance?3:0;
                    if(options.AutoSquelch?Math.Min(strength,Db(instant/Math.Max(1e-12,noise)))>=options.MarginDb-hysteresis:Math.Min(level,Db(instant/1.5))>=manualThreshold-hysteresis)
                        found.Add(new(frequency,occupied,strength,level));
                }
            }
            begin=last=-1;
        }
        for(int i=0;i<power.Length;i++)
        {
            bool hit=Valid(i)&&Hz(i)>=scopeLow&&Hz(i)<=scopeHigh&&power[i]>FloorAt(i)*Math.Pow(10,(options.AutoSquelch?Math.Max(3,options.MarginDb-3):3)/10);
            if(hit){if(begin<0)begin=i;last=i;}
            else if(begin>=0&&(i-last>2||!Valid(i)))Finish();
        }
        Finish();
        // Collapse fragmented modulation into one channel, strongest first; bounded work per frame.
        var distinct=new List<ActivityCandidate>();
        foreach(var candidate in found.OrderByDescending(c=>c.AboveFloorDb))
            if(distinct.Count<128&&!distinct.Any(c=>Math.Abs(c.Frequency-candidate.Frequency)<width*.65))distinct.Add(candidate);
        Candidates=distinct;
        foreach(var candidate in distinct)
        {
            var track=tracks.Where(t=>Math.Abs(t.Frequency-candidate.Frequency)<=Tolerance).MinBy(t=>Math.Abs(t.Frequency-candidate.Frequency));
            if(track==null)
            {
                if(tracks.Count>=512)continue;
                track=new(){Frequency=candidate.Frequency,First=time,Last=time,FirstUtc=utc};tracks.Add(track);
            }
            if(time-track.Last>.4){track.First=time;track.Qualified=false;}
            track.Last=time;track.LastUtc=utc;track.Occupied=candidate.OccupiedHz;track.Strength=candidate.AboveFloorDb;
            if(time-track.First>=options.MinimumSeconds)
            {if(!track.Qualified){track.Hits++;track.Qualified=true;}track.Dirty=true;}
        }
        if(current!=null)
        {
            if(Held||time-current.Last<options.QuietSeconds)return null;
            current=null;
        }
        if(Held)return null;
        current=tracks.Where(t=>t.Qualified&&time-t.Last<.25&&!ignored.Any(f=>Math.Abs(f-t.Frequency)<=Tolerance)).MaxBy(t=>t.Strength);
        tracks.RemoveAll(t=>t!=current&&!t.Dirty&&time-t.Last>60);
        return current?.Frequency;
    }
}
