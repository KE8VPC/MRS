namespace MRS;

internal sealed record DecoderProfile(string Key,string Name,string Mode,double Filter,int MinimumRate,double Span,bool Centered=false,long? FixedHz=null);
internal sealed record DecoderRecommendation(int Rate,bool DisableHf,int OffsetKhz,string Mode,double Filter);
internal static class DecoderSetup
{
    internal static readonly DecoderProfile[] Profiles=[
        new("adsb","ADS-B / Mode S","AM",15000,2000000,2000000,FixedHz:1090000000),
        new("uat","UAT","AM",15000,2083334,1600000,FixedHz:978000000),
        new("acars","ACARS","AM",15000,24000,15000),
        new("vdl2","VDL2","NFM",25000,50000,25000),
        new("atsc","HDTV / ATSC 1.0","WFM",200000,8000000,6000000),
        new("hd","HD Radio","WFM",200000,960000,600000,true),
        new("dmr","DMR / MOTOTRBO","NFM",16000,48000,16000),
        new("nxdn48","NXDN48 / NEXEDGE","NFM",6000,48000,6000),
        new("nxdn96","NXDN96 / NEXEDGE","NFM",12500,48000,12500),
        new("p25","P25 Phase I C4FM","NFM",12500,48000,12500),
        new("paging","POCSAG / FLEX","NFM",12500,48000,12500),
        new("cw","CW carrier / Morse","CW",500,48000,500)
    ];
    internal static DecoderRecommendation Recommend(DecoderProfile p,string backend)
    {
        int rate=backend=="HackRF"?9600000:p.MinimumRate>240000?2400000:240000;
        return new(rate,p.Span>=1600000,p.Centered?0:backend=="HackRF"?250:0,p.Mode,p.Filter);
    }
    internal static string[] Check(DecoderProfile p,long target,CaptureDescriptor capture,string backend,int gain,ChannelSettings channel,int sampling=0)
    {
        var issues=new List<string>();
        if(p.FixedHz is {} fixedHz&&target!=fixedHz)issues.Add($"{p.Name} target must be {fixedHz/1e6:0.######} MHz.");
        if(sampling!=0&&p.Key!="cw")issues.Add("Use quadrature / tuner sampling for this decoder profile.");
        if(capture.SampleRate<p.MinimumRate)issues.Add($"Sample rate {capture.SampleRate/1e6:0.###} MS/s is below {p.MinimumRate/1e6:0.###} MS/s required by {p.Name}.");
        if(backend=="HackRF"&&HackRfGain.Decode(gain).HfFilter&&Math.Abs(target-(double)capture.CenterHz)+p.Span/2>875000)issues.Add($"HF hardware filter (1.75 MHz) blocks {p.Name} coverage; disable it or use the recommended capture settings.");
        if(Math.Abs(target-(double)capture.CenterHz)+p.Span/2>Math.Min(capture.SampleRate,capture.UsableBandwidthHz)/2)issues.Add($"Target {target/1e6:0.######} MHz does not fit: capture center {capture.CenterHz/1e6:0.######} MHz, usable width {capture.UsableBandwidthHz/1e6:0.###} MHz; decoder needs {p.Span/1e6:0.###} MHz around its target.");
        if(p.Centered&&(capture.CenterHz!=target||channel.Frequency!=target))issues.Add("HD Radio needs capture and listening centers at the station frequency; disable capture offset.");
        bool raw=p.Key is "adsb" or "uat" or "acars" or "vdl2" or "atsc";
        if(!raw&&(channel.Mode!=p.Mode||p.Key!="cw"&&(channel.Width<(p.Key=="paging"?6000:p.Filter*.6)||channel.Width>p.Filter*2)))issues.Add($"Select {p.Mode} with a listening filter near {p.Filter/1000:0.###} kHz for {p.Name}.");
        return issues.ToArray();
    }
}
