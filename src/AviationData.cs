using System.Text.Json;
namespace MRS;
internal sealed record AviationMessage(PacketObservation Packet,string? Identity,string Callsign,double? Latitude,double? Longitude,double? Altitude,double? Speed,string AddressType);
internal static class AviationData
{
    internal static string Str(JsonElement e,string key)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString()??"":"";
    internal static JsonElement Obj(JsonElement e,string key)=>e.ValueKind==JsonValueKind.Object&&e.TryGetProperty(key,out var v)?v:default;
    internal static double? Num(JsonElement e,string key)=>Obj(e,key) is var v&&v.ValueKind==JsonValueKind.Number&&v.TryGetDouble(out var n)&&double.IsFinite(n)?n:null;
    internal static bool Position(double? lat,double? lon)=>lat is >=-90 and <=90&&lon is >=-180 and <=180;
    internal static AviationMessage? Parse(string json,string mode,PacketContext context)
    {
        try
        {
            using var doc=JsonDocument.Parse(json);var root=doc.RootElement;var body=Obj(root,"body");var quality=Obj(root,"decode");
            if(body.ValueKind!=JsonValueKind.Object)return null;
            bool valid=Obj(quality,"crc_ok").ValueKind==JsonValueKind.True;
            string protocol=mode switch{"acars"=>"ACARS","vdl2"=>"VDL2","adsb"=>"ADS-B / Mode S","uat"=>"UAT",_=>"Unknown aviation"};
            string type=Str(body,"type"),id="",call="",addressType="",summary="";
            double? lat=null,lon=null,alt=null,speed=null;
            if(type=="acars") {id=Str(body,"tail");addressType="Reported registration";call=Str(body,"flight");summary=$"{Str(body,"label")} · {Str(body,"text")}";}
            else if(type=="mode_s")
            {
                id=Str(body,"icao");call=Str(body,"callsign");int df=(int)(Num(body,"df")??-1);
                addressType=df==18?"DF18 address (source-qualified)":"ICAO";
                // DF18 can contain non-ICAO/TIS-B addresses. Keep its control field in the identity.
                if(df==18&&Str(root,"raw") is {Length:>=2} raw&&byte.TryParse(raw[..2],System.Globalization.NumberStyles.HexNumber,null,out byte first))addressType+=$" CF{first&7}";
                lat=Num(body,"lat");lon=Num(body,"lon");alt=Num(body,"altitude_ft");speed=Num(body,"speed_kt");summary=$"DF{df} · {call} · {id}";
            }
            else if(type=="uat")
            {
                var d=Obj(body,"details");summary=Str(body,"kind");
                if(summary=="adsb")
                {
                    id=Str(d,"address");addressType=Str(d,"address_qualifier");call=Str(d,"callsign");
                    var pos=Obj(d,"position");lat=Num(pos,"lat");lon=Num(pos,"lon");alt=Num(d,"pressure_altitude")??Num(d,"geometric_altitude");speed=Num(d,"ground_speed");
                    if(addressType is "vehicle" or "fixed_beacon" or "reserved"){id="";lat=lon=null;}
                }
                summary+=" · "+d.GetRawText();
            }
            else if(type=="vdl2")
            {
                var d=Obj(body,"details");var src=Obj(d,"src");
                if(Str(src,"kind")=="aircraft"){id=Str(src,"addr");addressType="ICAO (VDL2 source)";}
                summary=Str(body,"kind")+" · "+d.GetRawText();
            }
            if(!Position(lat,lon))lat=lon=null;
            string check=!valid?"Decoder validation failed":mode=="uat"?"RS-FEC accepted (not CRC)":"Decoder CRC/FCS accepted";
            if(Num(quality,"fec_corrected") is double corrections)check+=$" · corrections {corrections}";
            var packet=new PacketObservation(DateTimeOffset.UtcNow,context.Source,context.Replay,context.FrequencyHz,context.PositionSeconds,check,id,"","",type,summary,null,Str(root,"raw"),protocol,valid,SourceKey:context.Key,DecoderJson:json);
            return new(packet,string.IsNullOrWhiteSpace(id)?null:addressType+":"+id,call,lat,lon,alt,speed,addressType);
        }
        catch(Exception ex) when(ex is JsonException or InvalidOperationException or FormatException){return null;}
    }
}
internal sealed record AircraftTrailPoint(DateTimeOffset Seen,double Latitude,double Longitude,int Segment,double? ReplaySeconds);
internal sealed class AircraftRow
{
    public string Identity {get;set;}="";public string Protocol {get;set;}="";public string Source {get;set;}="";public string SourceKey {get;set;}="";public string Callsign {get;set;}="";
    public DateTimeOffset LastSeen {get;set;} public DateTimeOffset? PositionSeen {get;set;} public DateTimeOffset? AltitudeSeen {get;set;} public DateTimeOffset? SpeedSeen {get;set;}
    public double? Latitude {get;set;} public double? Longitude {get;set;} public double? Altitude {get;set;} public double? Speed {get;set;}
    public int Count {get;set;} public bool Replay {get;set;}
    public List<AircraftTrailPoint> Trail {get;}=[];
    internal void AddPosition(AviationMessage m)
    {
        var now=m.Packet.ObservedUtc;double? seconds=m.Packet.PositionSeconds;var last=Trail.LastOrDefault();
        if(last!=null&&now<last.Seen)return;
        double gap=Replay&&seconds.HasValue&&last?.ReplaySeconds is {} previous?seconds.Value-previous:last==null?0:(now-last.Seen).TotalSeconds;
        int segment=(last?.Segment??0)+(last!=null&&(gap>30||gap<0)?1:0);
        if(last==null||last.Latitude!=m.Latitude||last.Longitude!=m.Longitude||segment!=last.Segment)Trail.Add(new(now,m.Latitude!.Value,m.Longitude!.Value,segment,seconds));
        else Trail[^1]=last with{Seen=now,ReplaySeconds=seconds};
        Trail.RemoveAll(p=>(now-p.Seen).TotalMinutes>10);
        if(Trail.Count>300)Trail.RemoveRange(0,Trail.Count-300);
    }
    public string Age=>$"{Math.Max(0,(DateTimeOffset.UtcNow-LastSeen).TotalSeconds):F0}s";
    public string PositionAge=>PositionSeen is {} t?$"{Math.Max(0,(DateTimeOffset.UtcNow-t).TotalSeconds):F0}s":"—";
    public string Position=>Latitude is {} lat&&Longitude is {} lon?$"{lat:F5}, {lon:F5}":"—";
    public string AltitudeDisplay=>AltitudeSeen is {} t&&(DateTimeOffset.UtcNow-t).TotalSeconds<=60?$"{Altitude:F0}":"—";
    public string SpeedDisplay=>SpeedSeen is {} t&&(DateTimeOffset.UtcNow-t).TotalSeconds<=60?$"{Speed:F0}":"—";
}
internal sealed class AircraftTable
{
    private readonly Dictionary<string,AircraftRow> rows=[];
    internal AircraftRow[] Rows=>rows.Values.OrderBy(r=>r.Identity,StringComparer.Ordinal).ThenBy(r=>r.SourceKey,StringComparer.Ordinal).ToArray();
    internal void Clear()=>rows.Clear();
    internal void Observe(AviationMessage m)
    {
        if(!m.Packet.IsValidated||m.Identity==null)return;
        string key=$"{m.Packet.SourceKey}|{m.Packet.Input}|{m.Packet.Replay}|{m.Packet.Protocol}|{m.Identity}";
        if(!rows.TryGetValue(key,out var r))
        {if(rows.Count>=2000)rows.Remove(rows.MinBy(p=>p.Value.LastSeen).Key);rows[key]=r=new(){Identity=m.Identity,Protocol=m.Packet.Protocol,Source=m.Packet.Input,SourceKey=m.Packet.SourceKey??"",Replay=m.Packet.Replay};}
        r.LastSeen=m.Packet.ObservedUtc;r.Count++;if(m.Callsign.Length>0)r.Callsign=m.Callsign;
        if(AviationData.Position(m.Latitude,m.Longitude)){r.AddPosition(m);r.Latitude=m.Latitude;r.Longitude=m.Longitude;r.PositionSeen=r.LastSeen;}
        if(m.Altitude!=null){r.Altitude=m.Altitude;r.AltitudeSeen=r.LastSeen;}if(m.Speed!=null){r.Speed=m.Speed;r.SpeedSeen=r.LastSeen;}
    }
    internal void Expire()=>rows.Where(p=>(DateTimeOffset.UtcNow-p.Value.LastSeen).TotalMinutes>15).Select(p=>p.Key).ToArray().ToList().ForEach(k=>rows.Remove(k));
}
