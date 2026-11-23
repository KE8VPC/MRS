namespace MRS;
internal static class FoxOverload
{
    internal static bool Mark(double? peak,long? clipped)=>clipped>0||peak>=-3;
    internal static bool State(ReceiverInputLevel level,bool previous)=>Mark(level.PeakDb,level.RailComponents)||previous&&level.PeakDb>-6;
    internal static string? FlatHint(IReadOnlyList<FoxPoint> points)
    {
        if(points.Count<5)return null;
        var last=points[^1];
        var recent=points.Where(p=>p.Scope==last.Scope&&p.Utc>=last.Utc.AddSeconds(-60)).ToArray();
        if(recent.Length<5||recent.Any(p=>p.InputPeakDbfs is null or < -6)||recent.Max(p=>p.MeanDb)-recent.Min(p=>p.MeanDb)>2||!recent.Any(p=>p.Overloaded))return null;
        double north=(recent.Max(p=>p.Fix.Latitude)-recent.Min(p=>p.Fix.Latitude))*111195;
        double east=(recent.Max(p=>p.Fix.Longitude)-recent.Min(p=>p.Fix.Longitude))*111195*Math.Cos(last.Fix.Latitude*Math.PI/180);
        return Math.Sqrt(north*north+east*east)>=500?"Readings flat while moving, with high input peaks: try lower gain. This is a suggestion, not a cause diagnosis.":null;
    }
}
