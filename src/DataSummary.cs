using System.ComponentModel;
namespace MRS;

// Mutable display rows keep their position and selection when status repeats.
internal sealed class DataSummaryRow : INotifyPropertyChanged
{
    internal PacketObservation Latest {get;private set;}
    internal DateTimeOffset FirstSeen {get;}
    public long Repeats {get;private set;}=1;
    public long Changes {get;private set;}
    public string Protocol=>Latest.Protocol;
    public string Time=>Latest.Time;
    public string From=>Latest.From;
    public string To=>Latest.To;
    public string Type=>Latest.Type;
    public string Description=>Latest.Description;
    public string Status=>Latest.Status;
    public event PropertyChangedEventHandler? PropertyChanged;
    internal DataSummaryRow(PacketObservation observation){Latest=observation;FirstSeen=observation.ObservedUtc;}
    internal void Update(PacketObservation observation)
    {
        if(Latest.Information!=observation.Information || Latest.RawHex!=observation.RawHex)Changes++;
        Latest=observation;Repeats++;
        PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(null));
    }
    internal static string? Key(PacketObservation observation)
    {
        if(observation.Protocol!="P25 Phase I" || !observation.IsValidated)return null;
        // Serialize a tuple to avoid delimiter collisions in externally supplied fields.
        bool band=observation.Kind=="Identifier update" && observation.P25BandPlan!=null;
        var site=observation.P25SiteAnnouncement;
        bool discovery=site!=null && observation.Kind is "Adjacent site status" or "Secondary control channels";
        object? identity=band?observation.P25BandPlan!.Identifier:discovery?new object?[]{site!.SystemId,site.RfssId,site.SiteId,
            observation.Kind=="Secondary control channels"?site.Channels.Select(c=>c.Channel.ChannelId).ToArray():null}:null;
        bool status=band || discovery || observation.Kind is "Site status" or "Network status";
        // Headers can contain corrected bit errors; unknown payloads change without changing their message identity.
        bool category=observation.Kind is "NID header" or "TSBK" or "Manufacturer TSBK" or "Protected TSBK";
        return System.Text.Json.JsonSerializer.Serialize(new object?[]{observation.SourceKey,observation.Source,observation.Replay,observation.FrequencyHz,
            observation.Callsign,observation.Kind,identity,status?null:observation.Information,status||category?null:observation.RawHex});
    }
}



