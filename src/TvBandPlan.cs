namespace MRS;
internal sealed record TvPlanChannel(int Number,long Center)
{
    internal long Low=>Center-3000000;
    internal long High=>Center+3000000;
    internal double Pilot=>Low+309440.55944056;
    internal string Details(bool pilot)=>$"TV RF {Number}: {Low/1e6:0.###}–{High/1e6:0.###} MHz\nCenter {Center/1e6:0.######} MHz"+
        (pilot?$"\nATSC 1.0 nominal pilot {Pilot/1e6:0.######} MHz (reference only)":"")+
        (Number==17?"\nRF 17 is allocated for non-broadcast use in Hawaii.":"")+
        "\nPhysical RF channel, not virtual program number. Allocation does not identify an active station or ATSC version.\n"+TvBandPlan.Source;
}
internal static class TvBandPlan
{
    internal const string Source="47 CFR 73.603; reviewed 2026-10-12; https://www.ecfr.gov/current/title-47/chapter-I/subchapter-C/part-73/subpart-E/section-73.603; pilot: ATSC A/53 Part 2. Channel 37 is reserved for radio astronomy and is excluded.";
    internal static readonly TvPlanChannel[] Channels=Enumerable.Range(2,35).Select(n=>new TvPlanChannel(n,TvCapture.ChannelCenter(n))).ToArray();
    internal static IEnumerable<TvPlanChannel> Visible(double low,double high)=>Channels.Where(c=>c.Low<high && c.High>low);
}
