using System.Globalization;
namespace MRS;

internal sealed record P25BandPlan(int Identifier,int Opcode,long BaseHz,long SpacingHz,long? OffsetHz,int? BandwidthHz,int SlotsPerCarrier,int? ChannelType)
{
    internal bool Usable=>BaseHz>0 && SpacingHz>0 && SlotsPerCarrier>0;
}
internal sealed record P25Channel(int ChannelId,int Identifier,int ChannelNumber,long? DownlinkHz,long? UplinkHz,int? Slot,int? SlotsPerCarrier,string Resolution,int? UplinkChannelId=null,string? UplinkResolution=null);

// Wire fields are read MSB-first. Tables belong only to this receiver's current
// source/tuning epoch and NAC; they are never inferred from the tuned frequency.
internal sealed class P25ChannelPlans
{
    private readonly Dictionary<int,P25BandPlan> tables=new();
    private int? nac,system,wacn,site;
    internal P25Identity Identity=>new(nac??0,wacn,system,site is int value?value>>8:null,site is int value2?value2&255:null);
    internal void Clear(){tables.Clear();nac=system=wacn=site=null;}
    private static long Bits(byte[] raw,int start,int count)
    {
        long value=0;for(int bit=start;bit<start+count;bit++)value=(value<<1)+(long)((raw[bit/8]>>(7-bit%8))&1);
        return value;
    }
    private static string MHz(long hz)=>(hz/1000000m).ToString("F6",CultureInfo.InvariantCulture)+" MHz";
    internal static P25BandPlan ReadBand(byte[] raw)
    {
        int opcode=raw[0]&63,identifier=(int)Bits(raw,16,4);
        long spacing=Bits(raw,38,10)*125,baseHz=Bits(raw,48,32)*5;
        bool standard=opcode==0x3D;
        int signBit=standard?29:24,magnitudeBits=standard?8:13;
        long magnitude=Bits(raw,signBit+1,magnitudeBits);
        long? offset=magnitude==0x80?null:magnitude*(standard?250000:spacing)*(Bits(raw,signBit,1)==0?-1:1);
        int type=(int)Bits(raw,20,4),slots=1;
        int? bandwidth=standard?(int)Bits(raw,20,9)*125:type switch{4=>6250,5=>12500,_=>null};
        if(opcode==0x33)
        {
            slots=type switch{0 or 1 or 2=>1,3 or 5=>2,4=>4,_=>0};
            bandwidth=type switch{0 or 1 or 3 or 5=>12500,2=>6250,4=>25000,_=>null};
        }
        return new(identifier,opcode,baseHz,spacing,offset,bandwidth,slots,opcode==0x33?type:null);
    }
    internal P25Message Apply(P25Message message)
    {
        byte[] raw=message.Raw;
        if(!message.PayloadValid || raw.Length!=12 || P25Framer.Crc(raw.AsSpan(0,10))!=((raw[10]<<8)|raw[11]))return message;
        if(nac!=message.Nac){Clear();nac=message.Nac;}
        if((raw[0]&64)!=0 || raw[1]!=0)return message;
        int opcode=raw[0]&63;
        // Do not reuse a table after a decoded system/site identity changes on
        // the same RF input. Normal repeated status messages retain the table.
        if(opcode is 0x3A or 0x3B)
        {
            int nextSystem=opcode==0x3A?(int)Bits(raw,28,12):(int)Bits(raw,44,12);
            int? nextWacn=opcode==0x3B?(int)Bits(raw,24,20):null;
            int? nextSite=opcode==0x3A?(int)Bits(raw,40,16):null;
            if((system.HasValue&&system!=nextSystem)||(nextWacn.HasValue&&wacn.HasValue&&wacn!=nextWacn)||(nextSite.HasValue&&site.HasValue&&site!=nextSite))
            {tables.Clear();system=wacn=site=null;}
            system=nextSystem;if(nextWacn.HasValue)wacn=nextWacn;if(nextSite.HasValue)site=nextSite;
        }
        if(opcode is 0x3D or 0x34 or 0x33)
        {
            var plan=ReadBand(raw);tables[plan.Identifier]=plan;
            string access=plan.SlotsPerCarrier==0?$"unsupported channel type {plan.ChannelType}":plan.SlotsPerCarrier>1?$"TDMA · {plan.SlotsPerCarrier} slots/carrier":"FDMA";
            string bandwidth=plan.BandwidthHz is int bw?$" · bandwidth {bw} Hz":" · bandwidth unknown";
            string offset=plan.OffsetHz is long off?$" · uplink offset {off} Hz":" · uplink unavailable";
            return message with{Kind="Identifier update",Text=$"NAC {message.Nac:X3} · Identifier {plan.Identifier:X1} · base {MHz(plan.BaseHz)} · spacing {plan.SpacingHz} Hz{offset} · {access}{bandwidth}"+(plan.Usable?"":" · table unusable; channels unresolved"),BandPlan=plan};
        }
        if(P25ControlMessages.Describe(message,this) is { } control)return control;
        int? channelId=opcode switch{0x00=>(int)Bits(raw,24,16),0x3A or 0x3B=>(int)Bits(raw,56,16),_=>null};
        if(channelId is not int id)return message;
        var channel=Resolve(id);
        return message with{Text=DescribeChannel(channel)+" · "+message.Text,Channel=channel};
    }
    internal static string DescribeChannel(P25Channel channel)
    {
        string down=channel.DownlinkHz is long hz?$"downlink {MHz(hz)}":$"downlink unresolved: {channel.Resolution}";
        string up=channel.UplinkHz is long uplink?$" · uplink {MHz(uplink)}":channel.UplinkChannelId.HasValue?$" · explicit uplink unresolved: {channel.UplinkResolution}":" · uplink unavailable";
        return down+up+$" · channel ID {channel.ChannelId:X4}"+(channel.UplinkChannelId is int id?$" / uplink ID {id:X4}":"")+
            (channel.Slot is int slot?$" · slot {slot} of {channel.SlotsPerCarrier} (0-based)":"");
    }
    internal P25Channel ResolveExplicit(int downlinkId,int uplinkId)
    {
        var down=Resolve(downlinkId);var up=Resolve(uplinkId);
        // Explicit uplink references use that table's base/carrier frequency,
        // never the implicit offset of either table.
        return down with{UplinkHz=up.DownlinkHz,UplinkChannelId=uplinkId,UplinkResolution=up.Resolution};
    }    internal P25Channel Resolve(int id)
    {
        int identifier=id>>12,number=id&4095;
        var unresolved=new P25Channel(id,identifier,number,null,null,null,null,$"awaiting identifier {identifier:X1}");
        if(id==0xFFFF)return unresolved with{Resolution="channel not specified"};
        if(!tables.TryGetValue(identifier,out var plan))return unresolved;
        if(!plan.Usable)return unresolved with{Resolution="unsupported or unusable identifier table"};
        long down=plan.BaseHz+plan.SpacingHz*(number/plan.SlotsPerCarrier);
        long? up=plan.OffsetHz is long offset && down+offset>0?down+offset:null;
        return unresolved with{DownlinkHz=down,UplinkHz=up,Slot=plan.SlotsPerCarrier>1?number%plan.SlotsPerCarrier:null,SlotsPerCarrier=plan.SlotsPerCarrier,Resolution="received identifier table"};
    }
}




