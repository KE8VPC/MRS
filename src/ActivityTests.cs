using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
internal static class ActivityTests
{
    internal static float[] Frame(int bins,int rate,long center,int seed,params (long Hz,double Width,double Db)[] signals)
    {
        var random=new Random(seed);var values=new float[bins];
        for(int i=0;i<bins;i++)
        {
            double hz=center-rate/2.0+i*(double)rate/bins;
            double power=-Math.Log(Math.Max(1e-9,random.NextDouble()))*1e-9;
            foreach(var signal in signals)if(Math.Abs(hz-signal.Hz)<=signal.Width/2)power+=Math.Pow(10,signal.Db/10);
            values[i]=(float)(10*Math.Log10(Math.Max(1e-12,power)));
        }
        return values;
    }
    internal static void Run(Action<bool,string> check)
    {
        const long center=146500000,a=146400000,b=146650000;const int rate=960000,bins=4096;
        var channel=new ChannelSettings(a,"NFM",16000);var options=new ActivityOptions(AutoSquelch:true);
        var scanner=new ActivityScanner();scanner.Reset(center,rate,rate,bins,channel);scanner.Start(center-rate/2,center+rate/2);
        var epoch=new DateTimeOffset(2026,10,12,0,0,0,TimeSpan.Zero);double time=0;int seed=0;
        long? Feed(params (long Hz,double Width,double Db)[] signals)
        {time+=.15;return scanner.Process(Frame(bins,rate,center,seed++,signals),time,epoch.AddSeconds(time),channel,options,-60);}
        for(int i=0;i<25;i++)Feed();
        check(scanner.Ready&&scanner.Current==null&&scanner.Pending().Length==0,"RF scanner calibrates on noise without saving or tuning false channels");
        double expected=-90+10*Math.Log10(channel.Width/(rate/(double)bins)/1.5);
        check(Math.Abs(scanner.ChannelFloorDb-expected)<2,"Noise floor converts FFT-bin power to integrated channel power with Hann ENBW");
        Feed((a,5000,-45));Feed();for(int i=0;i<5;i++)Feed();
        check(scanner.Current==null,"Minimum duration rejects a brief RF impulse");
        for(int i=0;i<20;i++)Feed((center,3000,-10),(center+rate/2-1000,5000,-20));
        check(scanner.Current==null,"Scanner excludes capture-center artifact and filter-overrun edge signals");
        for(int i=0;i<12;i++)Feed((a,6000,-50),(b,6000,-58));
        check(Math.Abs((scanner.Current??0)-a)<500,"Scanner selects the strongest sustained channel using its occupied center");
        check(scanner.Pending().Length==2&&scanner.Pending().All(p=>p.Hits==1&&p.Mode=="NFM"&&p.OccupiedHz>4000),"All qualified channels accumulate discovery metadata while listening to one");
        var saved=scanner.Pending();scanner.Saved(saved);
        for(int i=0;i<5;i++)Feed((a,6000,-50),(b,6000,-58));
        check(scanner.Pending().All(p=>p.Hits==0),"Continuous activity updates last seen without counting each FFT as a new burst");
        scanner.Held=true;for(int i=0;i<20;i++)Feed((b,6000,-58));
        check(Math.Abs((scanner.Current??0)-a)<500,"Hold prevents hopping when the monitored signal becomes quiet");
        scanner.Held=false;Feed((b,6000,-58));
        check(Math.Abs((scanner.Current??0)-b)<500,"Releasing Hold selects an active signal after quiet delay");
        scanner.Skip();for(int i=0;i<8;i++)Feed((b,6000,-58));
        check(scanner.Current==null&&scanner.IgnoredCount==1,"Skip ignores a persistent channel for the rest of the scan session");
        scanner.ClearIgnored();Feed((b,6000,-58));check(scanner.Current!=null,"Clear ignored makes skipped activity eligible again");
        scanner.Stop();check(!scanner.Running&&!scanner.Held&&scanner.Current==null,"Scanner Stop clears selection and hold state");
        scanner.Start(center-200000,center-50000);for(int i=0;i<15;i++)Feed((b,6000,-40));
        check(scanner.Current==null,"Locked scan scope does not follow signals outside the chosen viewport");
        scanner.Reset(center,rate,rate,bins,channel);scanner.Start(center-rate/2,center+rate/2);
        options=options with{AutoSquelch=false};for(int i=0;i<25;i++)Feed((a,6000,-80));
        check(scanner.Current==null&&scanner.ChannelThresholdDb==-60,"Manual override uses the integrated RF threshold instead of the auto margin");
        check(new ActivityOptions(MarginDb:double.NaN,GridHz:77).Normalize() is {MarginDb:8,GridHz:0},"Invalid scan preferences restore bounded defaults");
        check(new ActivityOptions(ScanLowHz:a,ScanHighHz:b).ScanBounds(a-10000,b+10000,12500)==((double)a,(double)b),"Explicit scan limits preserve the requested captured subrange");
        bool badRange=false;try{new ActivityOptions(ScanLowHz:a-20000,ScanHighHz:b).ScanBounds(a-10000,b+10000,12500);}catch(InvalidOperationException){badRange=true;}
        check(badRange,"Scan limits outside capture are rejected rather than silently clipped or retuned");
        var artifact=new Birdie("Center spur",0,4000,true);
        check(artifact.Frequency(center)==center&&artifact.Frequency(center+100000)==center+100000,"Capture-relative birdie follows the actual capture center");
        check(new Birdie("Fixed",a,2000).Frequency(center+100000)==a,"Fixed birdie remains at its RF frequency after capture shifts");
        scanner.Reset(center,rate,rate,bins,channel);scanner.Start(center-rate/2,center+rate/2);
        options=new(AutoSquelch:true,Birdies:[new("Known test spur",a,3000)]);
        for(int i=0;i<28;i++)Feed((a,6000,-48),(b,6000,-55));
        check(scanner.Pending().Length==1&&Math.Abs((scanner.Current??0)-b)<500,"Known birdie is excluded from automatic tuning and saved discoveries while other channels remain eligible");
        scanner.Reset(center,rate,rate,bins,channel);scanner.Start(center-rate/2,center+rate/2);options=options with{ExcludeBirdies=false};
        for(int i=0;i<28;i++)Feed((a,6000,-48),(b,6000,-55));
        check(scanner.Pending().Length==2,"Disabling birdie exclusion restores both detected channels");
        var restoredBirdies=System.Text.Json.JsonSerializer.Deserialize<ActivityOptions>(System.Text.Json.JsonSerializer.Serialize(options))!.Normalize();
        check(restoredBirdies.Birdies is {Length:1}&&!restoredBirdies.ExcludeBirdies&&restoredBirdies.Birdies[0].PositionHz==a,"Birdie list and exclusion toggle survive settings serialization");
        scanner.Reset(center,rate,rate,bins,channel);scanner.Start(center-rate/2,center+rate/2);options=new(AutoSquelch:true,GridHz:6250);
        for(int i=0;i<28;i++)Feed((a+700,6000,-50));
        check(scanner.Current==a,"Configured 6.25 kHz channel grid rounds an estimated center to the channel frequency");
        foreach(double offset in new double[]{0,6250})
        {
            const long tvCenter=772300000,target=772081250;
            var narrow=new ChannelSettings(target,"NFM",12500);
            scanner.Reset(tvCenter,rate,rate,bins,narrow);scanner.Start(tvCenter-rate/2,tvCenter+rate/2);
            var gridOptions=new ActivityOptions(AutoSquelch:true,GridHz:12500,GridOffsetHz:offset);
            for(int i=0;i<50;i++)scanner.Process(Frame(bins,rate,tvCenter,i,(target+(i%2==0?120:-120),7000,-55)),i*.15,epoch.AddSeconds(i*.15),narrow,gridOptions,-60);
            check(scanner.Pending().Length==1&&Math.Abs((scanner.Current??0)-target)<500&&scanner.Pending().All(p=>Math.Abs(p.Frequency-target)<500),
                $"12.5 kHz grid with offset {offset} preserves one centered discovery at 772.08125 MHz despite estimate jitter");
            if(offset==6250)check(scanner.Current==target,"Explicit half-channel grid offset snaps to the actual channel center");
        }
        scanner.Reset(center,rate,rate,bins,channel);
        check(!scanner.Running&&!scanner.Ready&&scanner.Current==null&&scanner.Pending().Length==0,"Capture reset discards old candidates and requires fresh calibration and explicit rearming");
        scanner.Reset(center,20000000,15000000,4096,channel);bool rejected=false;
        try{scanner.Start(center-1000000,center+1000000);}catch(InvalidOperationException){rejected=true;}
        check(rejected&&!scanner.Running,"Insufficient FFT resolution refuses a narrow-channel scan instead of inventing detail");
        scanner.Reset(center,rate,rate,bins,channel);double lowFloor=0,highFloor=0;
        for(int i=0;i<30;i++)
        {
            var slope=Frame(bins,rate,center,i);for(int j=bins/2;j<bins;j++)slope[j]+=12;
            scanner.Process(slope,i*.15,epoch.AddSeconds(i*.15),i<28?channel:channel with{Frequency=b},options,-60);
            if(i==27)lowFloor=scanner.ChannelFloorDb;if(i==29)highFloor=scanner.ChannelFloorDb;
        }
        check(highFloor-lowFloor is >9 and <15,"Local floor estimate follows frequency-dependent noise instead of one global threshold");
        Persistence(check,saved);
        CheckIqPower(check);
        var view=new SpectrumView();view.Measure(new Size(1000,500));view.Arrange(new Rect(0,0,1000,500));view.SetRf(center,rate);
        view.SetBirdies([artifact,new Birdie("Fixed test",a,3000)]);
        var marker=new FrequencyItem{Name="Marker correction test",FrequencyHz=a};view.SetMarkers([marker]);
        Point MarkerPoint(long hz)=>new(52+(hz-(center-rate/2.0))/rate*928,80);
        int moved=0;long movedHz=0;view.MarkerMoveRequested+=(id,oldHz,newHz)=>{if(id==marker.Id&&oldHz==a){moved++;movedHz=newHz;}};
        check(view.BeginMarkerAdjustment(MarkerPoint(a)),"Explicit marker adjustment selects a single visible marker");
        view.PreviewMarkerAdjustment(MarkerPoint(a+3000));view.FinishMarkerAdjustment(false);
        check(moved==0,"Cancel leaves saved marker unchanged");
        view.BeginMarkerAdjustment(MarkerPoint(a));view.PreviewMarkerAdjustment(MarkerPoint(a+3000));view.FinishMarkerAdjustment(true);
        check(moved==1&&movedHz==a+3000,"Marker adjustment commits the preview frequency and original identity once");
        view.InteractionLocked=true;check(!view.BeginMarkerAdjustment(MarkerPoint(a)),"Running scanner lock prevents marker adjustment");view.InteractionLocked=false;
        view.SetMarkers([marker,marker with{Id=Guid.NewGuid()}]);check(!view.BeginMarkerAdjustment(MarkerPoint(a)),"Overlapping markers refuse ambiguous adjustment");view.SetMarkers([marker]);
        view.Configure(new DisplaySettings{AutoSpectrum=false,SpectrumTop=-30,SpectrumRange=80});view.Push(Frame(bins,rate,center,23,(a,6000,-50)));view.SetActivityLevels(-90,-82);
        view.InteractionLocked=true;view.Configure(view.Settings with{Zoom=4,Pan=.8});
        check(view.Settings.Zoom==1&&view.Settings.Pan==.5,"Locked scanner view rejects zoom and pan changes");
        view.InteractionLocked=false;view.Configure(view.Settings with{Zoom=2});check(view.Settings.Zoom==2,"Stopping scanner restores spectrum zoom control");
        view.ResetView();view.UpdateLayout();var bitmap=new RenderTargetBitmap(1000,500,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-activity-levels.png"));png.Save(output);
    }
    private static void Persistence(Action<bool,string> check,ActivityDiscovery[] discoveries)
    {
        string root=Path.Combine(Path.GetTempPath(),"mrs-activity-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"frequencies.json");var library=new FrequencyLibrary(path);
            check(library.Items.Count==0,"Discovery folder is not a starter-library preset");
            var user=new FrequencyItem{Name="My named station",FrequencyHz=discoveries[0].Frequency,Notes="Keep my notes",Protocols="P25",Modulation="C4FM",Color="#FFCC00"};library.Upsert(user);
            library.SaveDiscoveries(discoveries,"A");var reopened=new FrequencyLibrary(path);
            check(reopened.Items.Count(i=>!i.IsFolder)==2&&reopened.Items.Count(i=>i.ScannerFolder)==1,"Discoveries deduplicate against existing signals and create one managed folder");
            var kept=reopened.Items.Single(i=>i.Id==user.Id);
            check(kept.Name==user.Name&&kept.Notes==user.Notes&&kept.ParentId==user.ParentId&&kept.Protocols=="P25"&&kept.Activity?.Hits==1,"Discovery metadata preserves existing names, notes, protocols and folder placement");
            reopened.SaveDiscoveries(discoveries.Select(d=>d with{Hits=0,LastSeen=d.LastSeen.AddSeconds(10)}),"B");
            var updated=new FrequencyLibrary(path).Items.Single(i=>i.Id==user.Id);
            check(updated.Activity?.Hits==1&&updated.Activity.Receiver=="B"&&updated.Activity.LastSeen>kept.Activity!.LastSeen,"Persistent discovery refresh updates last seen without duplicate burst counts");
            var unknown=reopened.Items.Single(i=>!i.IsFolder&&i.Id!=user.Id);check(unknown.Modulation=="Unknown"&&unknown.Protocols=="Unknown","Energy discoveries do not invent modulation or protocol identification");
            check(reopened.Items.Single(i=>i.Id==unknown.ParentId) is {IsFolder:true,DetectionDate:not null},"New discoveries use a local-date subfolder");
            reopened.SaveDiscoveries(discoveries.Select(d=>d with{FirstSeen=d.FirstSeen.AddDays(1),LastSeen=d.LastSeen.AddDays(1)}),"A");
            check(reopened.Items.Count(i=>i.IsFolder&&i.DetectionDate!=null)==2&&reopened.Items.Count(i=>!i.IsFolder)==3,"Next-day discoveries get a new date folder while named user entries remain in place");
            var before=reopened.Items.ToArray();var dayFolder=reopened.Items.First(i=>i.IsFolder&&i.DetectionDate!=null);
            reopened.DeleteMany([dayFolder.Id,unknown.Id,user.Id]);
            check(!reopened.Items.Any(i=>i.Id==unknown.Id||i.Id==dayFolder.Id||i.Id==user.Id),"Batch deletion removes overlapping folder/child selections and entries in other folders atomically");
            reopened.Undo();check(reopened.Items.SequenceEqual(before),"One Undo restores the entire multi-delete batch");
            var panel=new ActivityPanel();panel.Initialize(Path.Combine(root,"activity.json"));panel.EnableAuto();panel.Save();
            var panel2=new ActivityPanel();panel2.Initialize(Path.Combine(root,"activity.json"));check(panel2.Options.AutoSquelch,"Automatic RF squelch preference persists without starting the scanner");
        }
        finally{Directory.Delete(root,true);}
    }
    private static void CheckIqPower(Action<bool,string> check)
    {
        foreach(int size in new[]{4096,32768})
        {
            const int rate=960000;var random=new Random(432);var analyzer=new RfSpectrumAnalyzer();var scanner=new ActivityScanner();
            var channel=new ChannelSettings(146400000,"NFM",16000);scanner.Reset(146500000,rate,rate,size,channel);
            var demod=new ReceiveDemodulator("NFM",rate,-100000,16000);double measured=0;
            for(int frame=0;frame<30;frame++)
            {
                var iq=new byte[Math.Max(size,32768)*2];
                for(int i=0;i<iq.Length;i+=2)
                {double r=5*Math.Sqrt(-2*Math.Log(Math.Max(1e-9,random.NextDouble()))),p=2*Math.PI*random.NextDouble();iq[i]=(byte)Math.Clamp(Math.Round(127.5+r*Math.Cos(p)),0,255);iq[i+1]=(byte)Math.Clamp(Math.Round(127.5+r*Math.Sin(p)),0,255);}
                analyzer.Append(iq,size);demod.Process(iq);if(frame>10)measured+=demod.ChannelDb/19;
                scanner.Process(analyzer.Snapshot()!,frame*.15,DateTimeOffset.UnixEpoch.AddSeconds(frame*.15),channel,new(AutoSquelch:true),-60);
            }
            check(Math.Abs(scanner.ChannelFloorDb-measured)<3,$"Real I/Q {size}-bin noise estimate matches demodulator channel power within 3 dB ({scanner.ChannelFloorDb:0.0} vs {measured:0.0})");
        }
    }
}
