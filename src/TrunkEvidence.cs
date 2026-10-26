namespace MRS;

// Confirmation windows match each protocol's repeat cadence; keys isolate sources and frequencies.
internal sealed class TrunkEvidence
{
    private readonly Dictionary<string,(DateTimeOffset First,DateTimeOffset Expires,int Count,string Label)> pending=[];
    internal int Candidates=>pending.Count;
    internal void Clear()=>pending.Clear();
    internal string Progress(DateTimeOffset now)
    {
        var active=pending.Values.Where(x=>x.Expires>=now&&x.Count<3).OrderByDescending(x=>x.First).ToArray();
        return active.Length==0?"no pending confirmations":$"{active[0].Label}: {active[0].Count}/3 confirmations"+(active.Length>1?$" · {active.Length} pending identities":"");
    }
    internal int Observe(DmrReception r)
    {
        var f=r.Fields;
        if(f.Protocol=="DMR"&&(f.SlotCorrections>1||!f.LinkValid&&f.DataType!=9))return 0;
        if(f.Protocol=="DMR"&&!f.LinkValid&&f.SlotCorrections!=0)return 0;
        if(f.Protocol=="LTR"&&!f.LinkValid)return 0;
        var window=TimeSpan.FromSeconds(f.Protocol=="LTR"?60:10);
        string key=System.Text.Json.JsonSerializer.Serialize(new object?[]{r.Context.Key,r.Context.Source,r.Context.Replay,r.Context.FrequencyHz,f.Protocol,f.Identity,f.SystemType,f.Network,f.Site});
        foreach(var stale in pending.Where(x=>r.Seen>x.Value.Expires).Select(x=>x.Key).ToArray())pending.Remove(stale);
        if(pending.TryGetValue(key,out var old)&&r.Seen>=old.First)
        {
            int count=Math.Min(old.Count+1,1000000);
            pending[key]=(old.First,f.Protocol=="LTR"&&count>=3?r.Seen+window:old.Expires,count,old.Label);return count;
        }
        if(pending.Count>=512)pending.Remove(pending.MinBy(x=>x.Value.First).Key);
        string label=f.Protocol=="LTR"?$"LTR {f.Identity}":$"DMR {f.Identity}";
        pending[key]=(r.Seen,r.Seen+window,1,label);return 1;
    }
}

internal static class DiscoveryVisibility
{
 internal static bool Show(int mode,int confirmations)=>mode>=2||confirmations>=(mode==1?1:3);
 internal static bool Unverified(DmrObservation f)=>f.SlotCorrections>1||(!f.LinkValid&&(f.DataType!=9||f.SlotCorrections!=0));
}
