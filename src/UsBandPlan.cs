namespace MRS;
internal sealed record PlanBand(long Low, long High, string Name, string Source);
internal sealed record PlanChannel(long Frequency, string Name, string Source);
internal static class UsBandPlan
{
    internal const string AmateurSource = "47 CFR 97.301 / 97.303; Region 2; 2024 edition";
    internal const string PersonalSource = "47 CFR Part 95; 2025 edition";
    internal const string MarineSource = "USCG U.S. VHF channel table; reviewed 2026-09-29; https://www.navcen.uscg.gov/us-vhf-channel-information";
    internal const string AviationSource = "FAA AIM navigation aids; reviewed 2026-09-29; https://www.faa.gov/air_traffic/publications/aim_html/chap1_section_1.html";
    internal const string AmSource = "47 CFR 73.14; 2024 annual edition; 540–1700 kHz carrier centers; https://www.govinfo.gov/content/pkg/CFR-2024-title47-vol4/pdf/CFR-2024-title47-vol4.pdf";
    internal static readonly PlanBand[] Bands =
    [
        new(135700,137800,"Amateur 2200 m",AmateurSource), new(472000,479000,"Amateur 630 m",AmateurSource),
        new(1800000,2000000,"Amateur 160 m",AmateurSource),new(3500000,4000000,"Amateur 80 / 75 m",AmateurSource),
        new(7000000,7300000,"Amateur 40 m",AmateurSource),new(10100000,10150000,"Amateur 30 m",AmateurSource),
        new(14000000,14350000,"Amateur 20 m",AmateurSource),new(18068000,18168000,"Amateur 17 m",AmateurSource),
        new(21000000,21450000,"Amateur 15 m",AmateurSource),new(24890000,24990000,"Amateur 12 m",AmateurSource),
        new(28000000,29700000,"Amateur 10 m",AmateurSource),new(50000000,54000000,"Amateur 6 m",AmateurSource),
        new(144000000,148000000,"Amateur 2 m",AmateurSource),new(219000000,220000000,"Amateur 1.25 m · limited data",AmateurSource),
        new(222000000,225000000,"Amateur 1.25 m",AmateurSource),new(420000000,450000000,"Amateur 70 cm · shared",AmateurSource),
        new(902000000,928000000,"Amateur 33 cm · shared",AmateurSource),new(1240000000,1300000000,"Amateur 23 cm · shared",AmateurSource),
        new(88000000,108000000,"FM broadcast","47 CFR 73.201; FCC DA 99-500"),
        new(535000,1705000,"AM broadcast",AmSource),
        new(108000000,118000000,"Aviation navigation · VOR / ILS",AviationSource+"; operating-area outline, not exclusive allocation; VOR centers through 117.95 MHz"),
        new(118000000,137000000,"Aviation VHF communications","FAA AC 90-50D; 118–136.975 MHz channel centers; operating-area outline; https://www.faa.gov/documentlibrary/media/advisory_circular/ac90-50d.pdf"),
        new(156050000,157425001,"Marine VHF · channel area",MarineSource+"; envelope of listed ship channels, includes gaps; not exclusive allocation"),
        new(161600000,162025001,"Marine shore / AIS · selected channels",MarineSource+"; envelope of selected shore/AIS centers, includes gaps and other uses")
    ];
    internal static readonly PlanChannel[] Channels = BuildChannels();
    private static PlanChannel[] BuildChannels()
    {
        var list=new List<PlanChannel>();
        for(int khz=540;khz<=1700;khz+=10)list.Add(new(khz*1000L,$"AM {khz} kHz",AmSource));
        const string emergency="FAA AIM 6-3; reviewed 2026-09-29; https://www.faa.gov/air_traffic/publications/atpubs/aim_html/chap6_section_3.html";
        list.Add(new(121500000,"Aviation emergency / guard",emergency));
        list.Add(new(243000000,"Aviation UHF emergency / guard",emergency));
        foreach(var (hz,name) in new (long,string)[]{
            (156300000,"06 · intership safety"),(156450000,"09 · calling"),
            (156650000,"13 · bridge-to-bridge"),(156800000,"16 · distress / safety / calling"),
            (156525000,"70 · DSC data, no voice"),(157100000,"1022 / 22A · USCG liaison / safety"),
            (157000000,"20 · ship TX / shore RX; also 1020 simplex"),(161600000,"20 · shore TX / ship RX"),
            (157200000,"24 · ship TX / shore RX"),(161800000,"24 · shore TX / ship RX"),
            (161975000,"AIS 1 · data"),(162025000,"AIS 2 · data")})
            list.Add(new(hz,"Marine "+name,MarineSource));
        int[] cb=[26965,26975,26985,27005,27015,27025,27035,27055,27065,27075,27085,27105,27115,27125,27135,27155,27165,27175,27185,27205,27215,27225,27255,27235,27245,27265,27275,27285,27295,27305,27315,27325,27335,27345,27355,27365,27375,27385,27395,27405];
        for(int i=0;i<cb.Length;i++)list.Add(new(cb[i]*1000L,$"CB {i+1}","47 CFR 95.963; 2025 edition"));
        for(int i=0;i<7;i++)
        {
            list.Add(new(462562500+i*25000L,$"FRS/GMRS {i+1}",PersonalSource));
            list.Add(new(467562500+i*25000L,$"FRS/GMRS {i+8}",PersonalSource));
            list.Add(new(162400000+i*25000L,$"NOAA {(162.4+i*.025):F3}","NOAA NWR; reviewed 2026-09-29"));
        }
        for(int i=0;i<8;i++)
        {
            list.Add(new(462550000+i*25000L,$"FRS/GMRS {i+15}",PersonalSource));
            list.Add(new(467550000+i*25000L,$"GMRS input {(467.55+i*.025):F3}","47 CFR 95.1763; 2025 edition"));
        }
        foreach(long hz in new long[]{151820000,151880000,151940000,154570000,154600000})list.Add(new(hz,$"MURS {hz/1000000.0:F3}","47 CFR 95.2763; 2025 edition"));
        return list.OrderBy(x=>x.Frequency).ToArray();
    }
    internal static IEnumerable<PlanBand> VisibleBands(double start,double end) => Bands.Where(x=>x.Low<end && x.High>start);
    internal static IEnumerable<PlanChannel> VisibleChannels(double start,double end) => Channels.Where(x=>x.Frequency>=start && x.Frequency<=end);
}
