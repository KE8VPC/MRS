using System.Text.Json;
namespace MRS;
internal static class P25ControlTests
{
    private static P25Message Message(byte[] raw)
    {
        var info=P25Framer.Describe(raw);return new(0x293,7,0,raw,info.Kind,info.Text,true);
    }
    private static byte[] Change(byte[] input,int index,byte value)
    {
        var raw=(byte[])input.Clone();raw[index]=value;ushort crc=P25Framer.Crc(raw.AsSpan(0,10));raw[10]=(byte)(crc>>8);raw[11]=(byte)crc;return raw;
    }
    internal static void Run(Action<bool,string> check)
    {
        var plans=new P25ChannelPlans();
        foreach(var b in new[]{P25ChannelFixtures.Fdma,P25ChannelFixtures.Vuhf,P25ChannelFixtures.Tdma})plans.Apply(Message(b));
        var update=plans.Apply(Message(P25ControlFixtures.Update));
        check(update.Kind=="Group voice grant update" && update.Assignments is {Length:2} && update.Assignments[0] is {TalkgroupId:4660,SourceRadioId:null,ServiceOptions:null} && update.Assignments[1].TalkgroupId==4661,"Independent dual-grant update preserves both talkgroups without inventing radio ID/service options");
        check(update.Assignments![0].Channel.DownlinkHz==851025000 && update.Assignments[1].Channel is {DownlinkHz:762025000,Slot:1},"Dual grant update resolves each channel through its own identifier and slot mapping");
        var explicitGrant=plans.Apply(Message(P25ControlFixtures.Explicit));
        check(explicitGrant.Assignments is {Length:1} && explicitGrant.Assignments[0] is {TalkgroupId:9029,ServiceOptions:4,SourceRadioId:null} && explicitGrant.Channel is {DownlinkHz:851050000,UplinkHz:150050000,UplinkChannelId:0x2008},"Explicit grant uses separate uplink channel base without adding the implicit offset");
        var missing=new P25ChannelPlans();missing.Apply(Message(P25ChannelFixtures.Fdma));
        var unresolved=missing.Apply(Message(P25ControlFixtures.Explicit));
        check(unresolved.Channel is {DownlinkHz:851050000,UplinkHz:null} && unresolved.Text.Contains("explicit uplink unresolved"),"Missing explicit uplink table does not fall back to downlink offset");
        var onlyUplink=new P25ChannelPlans();onlyUplink.Apply(Message(P25ChannelFixtures.Vuhf));
        check(onlyUplink.Apply(Message(P25ControlFixtures.Explicit)).Channel is {DownlinkHz:null,UplinkHz:150050000},"Explicit uplink can resolve independently of a missing downlink table");
        var secondary=plans.Apply(Message(P25ControlFixtures.Secondary));
        check(secondary.Kind=="Secondary control channels" && secondary.SiteAnnouncement is {SystemId:null,RfssId:2,SiteId:3,Channels.Length:2} && secondary.SiteAnnouncement.Channels[0] is {ServiceClass:5,Channel.DownlinkHz:851075000} && secondary.SiteAnnouncement.Channels[1] is {ServiceClass:33,Channel.DownlinkHz:851100000},"Secondary broadcast preserves RFSS/site and two independently resolved service/channel entries");
        check(plans.Apply(Message(Change(P25ControlFixtures.Secondary,9,0))).SiteAnnouncement?.Channels.Length==1,"Unused secondary B entry does not invent an extra channel");
        var neighbor=plans.Apply(Message(P25ControlFixtures.Adjacent));
        check(neighbor.Kind=="Adjacent site status" && neighbor.SiteAnnouncement is {SystemId:0x123,RfssId:2,SiteId:4,LocationRegistrationArea:7,Flags:3} && neighbor.Channel?.DownlinkHz==851125000,"Adjacent broadcast decodes the advertised neighbor and its control channel");
        check(plans.Resolve(0x1002).DownlinkHz==851025000,"Neighbor identity does not replace the current system/site or clear frequency tables");
        plans.Apply(Message(P25ChannelFixtures.Site));plans.Apply(Message(Change(P25ControlFixtures.Adjacent,4,0x24)));
        check(plans.Resolve(0x1002).DownlinkHz==851025000,"Even a different advertised neighbor system does not clear the current tables");
        foreach(var raw in new[]{P25ControlFixtures.Update,P25ControlFixtures.Explicit,P25ControlFixtures.Secondary,P25ControlFixtures.Adjacent})
        {
            var manufacturer=plans.Apply(Message(Change(raw,1,0x90)));
            var protectedMessage=plans.Apply(Message(Change(raw,0,(byte)(raw[0]|64))));
            var damaged=(byte[])raw.Clone();damaged[11]^=1;var corrupt=plans.Apply(Message(damaged));
            check(manufacturer.Assignments==null&&manufacturer.SiteAnnouncement==null&&protectedMessage.Assignments==null&&protectedMessage.SiteAnnouncement==null&&corrupt.Assignments==null&&corrupt.SiteAnnouncement==null,$"Opcode {raw[0]&63:X2} requires CRC-valid unprotected standard format");
        }
        var duplicate=(byte[])P25ControlFixtures.Update.Clone();Array.Copy(duplicate,2,duplicate,6,4);duplicate=Change(duplicate,2,duplicate[2]);
        check(plans.Apply(Message(duplicate)).Assignments?.Length==1,"Identically repeated B grant does not create duplicate activity");
        duplicate=Change(duplicate,7,3);
        check(plans.Apply(Message(duplicate)).Assignments?.Length==2,"Same talkgroup on a different channel remains a separate announcement");
        var pipeline=new P25Pipeline{Enabled=true};var context=new PacketContext("control","Synthetic P25",true,774000000);
        pipeline.Feed(P25Tests.Signal(3,symbols:P25ControlFixtures.Symbols),context);var rows=pipeline.Drain();
        check(rows.Length==36 && rows.Count(r=>r.Kind=="Group voice grant update")==3 && rows.Count(r=>r.Kind=="Adjacent site status")==3,"Independent expanded-control waveform survives production C4FM/pipeline decoding");
        var panel=new PacketPanel();foreach(var row in rows)panel.Add(row);
        var adjacent=rows.First(r=>r.Kind=="Adjacent site status");
        int initial=panel.VisibleCount;
        panel.Add(adjacent with{Information="Neighbor channel changed",RawHex="1234",P25SiteAnnouncement=adjacent.P25SiteAnnouncement! with{Flags=7}});
        check(panel.VisibleCount==initial,"Repeated neighbor announcements update a stable per-neighbor row");
        panel.Add(adjacent with{P25SiteAnnouncement=adjacent.P25SiteAnnouncement! with{SiteId=5}});
        check(panel.VisibleCount==initial+1,"Different neighbor sites are never collapsed into one row");
        var alternate=rows.First(r=>r.Kind=="Secondary control channels");
        panel.Add(alternate with{Information="Service flags changed",RawHex="5678"});
        check(panel.VisibleCount==initial+1,"Secondary service changes update the same announced channel-pair row");
        panel.Add(alternate with{P25SiteAnnouncement=alternate.P25SiteAnnouncement! with{Channels=[new(plans.Resolve(0x1010),5)]}});
        check(panel.VisibleCount==initial+2,"Different secondary channel sets remain separately visible");
        panel.HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update","Secondary control channels","Adjacent site status"];
        check(panel.VisibleCount==4,"Activity filter retains all three grant formats and current site status");
        using(var json=JsonDocument.Parse(panel.ExportJson()))
            check(json.RootElement.EnumerateArray().Single(r=>r.GetProperty("Kind").GetString()=="Group voice grant update").GetProperty("P25Assignments").GetArrayLength()==2,"JSON export retains both structured grants in one received message");
        panel.Grouped=false;check(panel.VisibleCount==12,"The same message-type filters apply to expanded-control raw history");
    }
}
