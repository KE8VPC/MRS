using System.IO;
using System.Text.Json;
namespace MRS;
internal sealed record PagingRule(string Protocol,long? FrequencyHz,string Match,string Value,string Label="",bool Ignore=false);
internal sealed class PagingRules
{
    internal List<PagingRule> Items {get;private set;}=[];
    private string? path;
    private bool unreadable;
    internal void Load(string file)
    {
        path=file;
        try {Items=File.Exists(file)?JsonSerializer.Deserialize<List<PagingRule>>(File.ReadAllText(file))??throw new IOException("Empty paging rules"):[];}
        catch {unreadable=true;throw;}
    }
    internal static bool IsPaging(PacketObservation p)=>p.Protocol is "FLEX" or "POCSAG";
    internal static string Payload(PacketObservation p)=>p.DisplaySummary??p.Information;
    internal static string? GroupKey(PacketObservation p)
    {
        if(!IsPaging(p) || !p.IsValidated)return null;
        if(p.Protocol=="FLEX" && !p.Kind.EndsWith(" · complete") && !p.Kind.EndsWith(" · reassembled"))return null;
        // Do not merge incomplete fragments with complete/reassembled pages or failed checks with passing checks.
        string kind=p.Kind.EndsWith(" · complete")||p.Kind.EndsWith(" · reassembled")?p.Kind.Split(" · ")[0]+" · complete":p.Kind;
        return JsonSerializer.Serialize(new object?[]{p.SourceKey,p.Source,p.Replay,p.FrequencyHz,p.Protocol,kind,p.IsValidated,Payload(p)});
    }
    private IEnumerable<PagingRule> Matching(PacketObservation p)=>Items.Where(r=>r.Protocol==p.Protocol && r.FrequencyHz==p.FrequencyHz && (r.Match=="Capcode"?r.Value==p.To:r.Match=="Message"&&r.Value==Payload(p)));
    internal bool Ignored(PacketObservation p)=>IsPaging(p)&&Matching(p).Any(r=>r.Ignore);
    internal PacketObservation Apply(PacketObservation p)
    {
        if(!IsPaging(p))return p;
        var labels=Matching(p).Select(r=>r.Label).Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct();
        return p with{Label=string.Join(" / ",labels)};
    }
    internal void Save(IEnumerable<PagingRule> rules)
    {
        if(unreadable)throw new IOException("Existing paging rules could not be read; repair or back up that file before saving.");
        var updated=rules.ToList();
        if(updated.Count>2000 || updated.Any(r=>r.Protocol is not ("FLEX" or "POCSAG") || r.Match is not ("Capcode" or "Message") || string.IsNullOrEmpty(r.Value) || r.Value.Length>32768 || r.Label.Length>160))throw new IOException("Invalid paging rule (limit 2,000 rules; labels up to 160 characters).");
        if(path!=null){string temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(updated,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);}
        Items=updated;
    }
}
