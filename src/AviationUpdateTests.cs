namespace MRS;
internal static class AviationUpdateTests
{
    internal static void Run(Action<bool,string> check)
    {
        var data=OfflineBasemap.Shared.Value;
        check(data.Error==""&&data.Shapes.Count>2500&&data.Places.Count==7342,"Offline basemap loads bundled vector geometry and city labels without tile downloads");
        check(data.Places.Any(p=>p.Name=="Lima"&&Math.Abs(p.Lat-40.74287)<.001&&Math.Abs(p.Lon+84.10527)<.001),"Offline basemap contains Lima Ohio regional context");
        check(data.Shapes.Any(s=>s.Kind==1&&s.Name=="Ohio")&&data.Shapes.Any(s=>s.Kind==2),"Offline basemap includes state boundaries and lake geometry");
        var message=AviationData.Parse(AviationTests.PositionJson,"adsb",AviationTests.Context)!;var now=DateTimeOffset.UtcNow.AddSeconds(-90);
        var table=new AircraftTable();
        AviationMessage At(int seconds,double lat)=>message with{Latitude=lat,Packet=message.Packet with{ObservedUtc=now.AddSeconds(seconds),PositionSeconds=seconds}};
        table.Observe(At(0,40));table.Observe(At(1,40.01));var row=table.Rows.Single();
        check(row.Trail.Count==2&&AircraftMap.JoinTrail(row.Trail[0],row.Trail[1]),"Sequential aircraft positions form a trail");
        table.Observe(At(2,40.01));check(row.Trail.Count==2,"Repeated identical positions do not consume trail storage");
        table.Observe(At(3,40.02) with{Packet=At(3,40.02).Packet with{Validated=false}});check(row.Trail.Count==2,"Failed-validation messages do not add trail positions");
        var stationary=new AircraftTable();stationary.Observe(At(0,40));stationary.Observe(At(25,40));stationary.Observe(At(50,40));stationary.Observe(At(51,40.1));
        check(stationary.Rows.Single().Trail.Count==2&&AircraftMap.JoinTrail(stationary.Rows.Single().Trail[0],stationary.Rows.Single().Trail[1]),"Repeated stationary reports refresh the trail anchor without a false reporting gap");
        table.Observe(At(40,40.1));check(row.Trail.Count==3&&!AircraftMap.JoinTrail(row.Trail[1],row.Trail[2]),"Missing position interval over 30 seconds breaks the trail");
        table.Observe(At(41,40.2) with{Packet=At(41,40.2).Packet with{PositionSeconds=0}});check(!AircraftMap.JoinTrail(row.Trail[^2],row.Trail[^1]),"Replay rewind starts a new trail segment");
        table.Observe(At(42,40.3) with{Packet=At(42,40.3).Packet with{Replay=false}});check(table.Rows.Length==2&&table.Rows.Single(r=>!r.Replay).Trail.Count==1,"Live and replay trails remain separate");
        for(int i=43;i<443;i++)table.Observe(At(i,40+i*.0001));check(row.Trail.Count==300,"Aircraft trails are bounded to 300 positions per source identity");
        table.Observe(At(1200,41));check(row.Trail.Count==1,"Trail storage expires points older than ten minutes");
        check(!AircraftMap.JoinTrail(new(now,0,179,0,null),new(now.AddSeconds(1),0,-179,0,null)),"Dateline crossings do not draw a line across the map");
        using(var map=new AircraftMap())
        {
            map.Measure(new System.Windows.Size(800,400));map.Arrange(new System.Windows.Rect(0,0,800,400));map.UpdateLayout();
            var crowded=Enumerable.Range(0,150).Select(n=>new AircraftRow{Identity="TEST"+n,Latitude=40.5,Longitude=-84.2,PositionSeen=DateTimeOffset.UtcNow,LastSeen=DateTimeOffset.UtcNow}).ToArray();
            foreach(var aircraft in crowded)for(int i=0;i<300;i++)aircraft.Trail.Add(new(DateTimeOffset.UtcNow.AddSeconds(-300+i),40.5+i*.00002,-84.2,0,null));
            map.Update(crowded,crowded[0]);map.SetTrailsForTest(true);map.FitForTest();
            check(map.TrailsLimited&&map.TrailSegments==30000&&map.NetworkRequests==0,"Crowded offline trail rendering is bounded and does not request tiles");
        }
        var adsb=DecoderSetup.Profiles.Single(p=>p.Key=="adsb");int hf=new HackRfGain(24,24,false,true).Token;
        var issues=DecoderSetup.Check(adsb,1090000000,new(1090250000,9600000,1750000,IqSampleFormat.Unsigned8),"HackRF",hf,new(1090000000,"AM",15000));
        check(issues.Any(i=>i.Contains("HF hardware filter"))&&issues.Any(i=>i.Contains("usable width")),"Decoder setup identifies the exact HF filter and usable-capture conflict");
        var advice=DecoderSetup.Recommend(adsb,"HackRF");
        check(advice.Rate==9600000&&advice.DisableHf&&advice.OffsetKhz==250,"ADS-B profile restores the user's successful HackRF capture baseline");
        check(DecoderSetup.Check(adsb,1090000000,new(1090250000,9600000,9000000,IqSampleFormat.Unsigned8),"HackRF",new HackRfGain().Token,new(1090000000,"NFM",6000)).Length==0,"Raw I/Q ADS-B setup does not incorrectly reject the listening audio filter");
        var hd=DecoderSetup.Profiles.Single(p=>p.Key=="hd");
        check(DecoderSetup.Recommend(hd,"HackRF").OffsetKhz==0&&DecoderSetup.Check(hd,100000000,new(100250000,9600000,9000000,IqSampleFormat.Unsigned8),"HackRF",new HackRfGain().Token,new(100000000,"WFM",200000)).Any(i=>i.Contains("disable capture offset")),"HD Radio setup requires centered capture");
        var paging=DecoderSetup.Profiles.Single(p=>p.Key=="paging");check(DecoderSetup.Check(paging,460000000,new(460000000,240000,240000,IqSampleFormat.Unsigned8),"RTL",0,new(460000000,"NFM",6000)).Length==0,"Paging setup accepts the user's successful 6 kHz POCSAG filter");
        var cw=DecoderSetup.Profiles.Single(p=>p.Key=="cw");check(DecoderSetup.Check(cw,7100000,new(7100000,240000,240000,IqSampleFormat.Unsigned8),"RTL",0,new(7100000,"CW",500),1).Length==0,"CW setup preserves supported direct-sampling HF reception");
        var dmr=DecoderSetup.Profiles.Single(p=>p.Key=="dmr");check(DecoderSetup.Check(dmr,451600000,new(451600000,240000,240000,IqSampleFormat.Unsigned8),"RTL",0,new(451600000,"AM",15000)).Any(i=>i.Contains("Select NFM")),"DMR setup checks discriminator listening mode");
        check(DecoderSetup.Check(adsb,1090000000,new(1090000000,2400000,2400000,IqSampleFormat.Unsigned8),"RTL",0,new(1090000000,"AM",15000),1).Any(i=>i.Contains("quadrature")),"Decoder setup detects incompatible direct-sampling configuration");
    }
}
