using System.Text;
namespace MRS;

internal sealed record Ax25Packet(bool FcsValid, string Validation, string Source, string Destination,
    string Path, string FrameType, string Information, byte[] Raw, AprsData? Aprs);

internal static class Ax25Parser
{
    internal static ushort Fcs(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xffff;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int n = 0; n < 8; n++) crc = (ushort)((crc >> 1) ^ ((crc & 1) != 0 ? 0x8408 : 0));
        }
        return (ushort)~crc;
    }
    internal static string Printable(ReadOnlySpan<byte> data)
    {
        var result = new StringBuilder();
        foreach(byte b in data) { if(b is >=32 and <=126) result.Append((char)b); else result.Append($"\\x{b:X2}"); }
        return result.ToString();
    }
    internal static Ax25Packet Parse(byte[] raw)
    {
        var copy = (byte[])raw.Clone();
        Ax25Packet Invalid(string reason, bool fcs) => new(fcs, reason, "", "", "", "Unparsed", "", copy, null);
        if (raw.Length is < 17 or > 2048) return Invalid("Invalid frame length", false);
        if (Fcs(raw.AsSpan(0,raw.Length-2)) != (ushort)(raw[^2] | raw[^1]<<8)) return Invalid("FCS failed — fields not trusted", false);
        var addresses = new List<string>(); int offset=0; bool ended=false;
        while (!ended && addresses.Count < 10)
        {
            if (offset+7 > raw.Length-3) return Invalid("FCS valid; truncated address", true);
            var name = new StringBuilder(); bool padding=false;
            for(int n=0;n<6;n++)
            {
                byte b=raw[offset+n]; char c=(char)(b>>1);
                if ((b&1)!=0 || !(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or ' ') || (padding && c!=' ')) return Invalid("FCS valid; unsupported address encoding",true);
                if(c==' ') padding=true; else name.Append(c);
            }
            if(name.Length==0) return Invalid("FCS valid; empty callsign",true);
            byte ssid=raw[offset+6]; int id=(ssid>>1)&15;
            addresses.Add(name+(id==0?"":$"-{id}")+(addresses.Count>=2 && (ssid&128)!=0?"*":""));
            ended=(ssid&1)!=0; offset+=7;
        }
        if(!ended || addresses.Count<2) return Invalid("FCS valid; invalid address chain",true);
        byte control=raw[offset++]; string type; string information=""; AprsData? aprs=null;
        if((control&0xef)==3)
        {
            type="UI";
            if(offset>=raw.Length-2) return Invalid("FCS valid; UI frame missing PID",true);
            byte pid=raw[offset++]; var payload=raw.AsSpan(offset,raw.Length-2-offset);
            information=Printable(payload); type+=$" · PID {pid:X2}";
            if(pid==0xf0) aprs=AprsParser.Parse(payload);
        }
        else
        {
            type=(control&1)==0 ? "I (raw; link modulus unknown)" : (control&3)==1 ? "S (raw; link modulus unknown)" : $"U · {control:X2}";
            information=Printable(raw.AsSpan(offset,raw.Length-2-offset));
        }
        return new(true,"FCS valid",addresses[1],addresses[0],string.Join(",",addresses.Skip(2)),type,information,copy,aprs);
    }
}

internal sealed record AprsData(string Type, string Summary, double? Latitude=null, double? Longitude=null,
    string? Addressee=null, string? MessageId=null);

internal static class AprsParser
{
    internal static AprsData? Parse(ReadOnlySpan<byte> payload)
    {
        if(payload.Length==0) return null;
        string text=Ax25Parser.Printable(payload);
        char type=(char)payload[0];
        if(type=='>') return new("Status",text[1..]);
        if(type==':')
        {
            if(payload.Length<11 || payload[10]!=(byte)':') return new("Message","Malformed APRS message; raw payload retained");
            string to=Encoding.ASCII.GetString(payload.Slice(1,9)).TrimEnd();
            if(payload.Slice(1,9).ToArray().Any(b=>b<32 || b>126) || to.Length==0) return new("Message","Invalid APRS addressee; raw payload retained");
            string body=Ax25Parser.Printable(payload[11..]), id=""; int brace=body.LastIndexOf('{');
            if(brace>=0)
            {
                id=body[(brace+1)..];
                if(!MessageId(id))return new("Message","Unsupported or invalid message ID; raw payload retained",Addressee:to);
                body=body[..brace];
            }
            string kind="Message";
            if(id.Length==0 && body.Length>=4 && MessageId(body[3..]) && (body.StartsWith("ack",StringComparison.Ordinal)||body.StartsWith("rej",StringComparison.Ordinal)))
            {kind=body.StartsWith("ack",StringComparison.Ordinal)?"Acknowledgment":"Rejection";id=body[3..];}
            return new(kind,$"To {to}: {body}"+(id.Length>0?$" [ID {id}]":""),Addressee:to,MessageId:id.Length>0?id:null);
        }
        if(type is '!' or '=' or '/' or '@')
        {
            int start=type is '/' or '@'?8:1;
            if(start==8 && (payload.Length<8 || !Timestamp(payload.Slice(1,7))))return new("Position","Invalid APRS timestamp; raw payload retained");
            if(payload.Length<start+19) return new("Position","Short or unsupported position; raw payload retained");
            byte table=payload[start+8],symbol=payload[start+18];
            if(!(table is (byte)'/' or (byte)'\\' or >=(byte)'0' and <=(byte)'9' or >=(byte)'A' and <=(byte)'Z') || symbol is <33 or >126)return new("Position","Unsupported symbol encoding; raw payload retained");
            var lat=payload.Slice(start,8); var lon=payload.Slice(start+9,9);
            if(!Coordinate(lat,true,out double latitude) || !Coordinate(lon,false,out double longitude)) return new("Position","Compressed, ambiguous or invalid position; raw payload retained");
            string comment=Ax25Parser.Printable(payload[(start+19)..]);
            return new("Position",FormattableString.Invariant($"{latitude:F6}, {longitude:F6} · symbol {(char)payload[start+8]}{(char)payload[start+18]} · {comment}"),latitude,longitude);
        }
        return null;
    }
    private static bool MessageId(string value)=>value.Length is >=1 and <=5 && value.All(c=>c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    private static bool Timestamp(ReadOnlySpan<byte> value)
    {
        for(int n=0;n<6;n++)if(value[n]<'0'||value[n]>'9')return false;
        int a=(value[0]-'0')*10+value[1]-'0',b=(value[2]-'0')*10+value[3]-'0',c=(value[4]-'0')*10+value[5]-'0';
        return value[6]=='h'?a<24 && b<60 && c<60:(value[6]=='z'||value[6]=='/') && a is >=1 and <=31 && b<24 && c<60;
    }
    private static bool Coordinate(ReadOnlySpan<byte> value,bool latitude,out double result)
    {
        result=0; int degrees=latitude?2:3;
        if(value[degrees+2]!=(byte)'.') return false;
        int d=0,m=0;
        for(int n=0;n<value.Length-1;n++)
        {
            if(n==degrees+2)continue;
            if(value[n]<'0'||value[n]>'9')return false;
            if(n<degrees)d=d*10+value[n]-'0';else m=m*10+value[n]-'0';
        }
        int max=latitude?90:180;
        if(d>max || m>=6000 || (d==max && m!=0))return false;
        char direction=(char)value[^1];
        if(latitude ? direction is not ('N' or 'S') : direction is not ('E' or 'W'))return false;
        result=(d+m/6000.0)*(direction is 'S' or 'W'?-1:1); return true;
    }
}
