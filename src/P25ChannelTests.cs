using System.Text.Json;
namespace MRS;
internal static class P25ChannelTests
{
    private static byte[] Change(byte[] input,int start,int count,long value)
    {
        byte[] raw=(byte[])input.Clone();
        for(int n=0;n<count;n++){int bit=start+n,mask=1<<(7-bit%8);raw[bit/8]=(byte)((raw[bit/8]&~mask)|(((value>>(count-1-n))&1)!=0?mask:0));}
        ushort crc=P25Framer.Crc(raw.AsSpan(0,10));raw[10]=(byte)(crc>>8);raw[11]=(byte)crc;return raw;
    }
    private static P25Message Message(byte[] raw,int nac=0x293)
    {
        var description=P25Framer.Describe(raw);return new(nac,7,0,raw,description.Kind,description.Text,true);
    }
    internal static void Run(Action<bool,string> check)
    {
        var plans=new P25ChannelPlans();
        check(plans.Apply(Message(P25ChannelFixtures.Grant)).Channel is {DownlinkHz:null} && plans.Resolve(0x1002).Resolution.Contains("awaiting"),"P25 does not guess frequencies without a received identifier table");
        var fdma=plans.Apply(Message(P25ChannelFixtures.Fdma));
        check(fdma.Kind=="Identifier update" && fdma.BandPlan is {Identifier:1,BaseHz:851000000,SpacingHz:12500,OffsetHz:-45000000,BandwidthHz:12500,SlotsPerCarrier:1},"Independent FDMA identifier fields decode with correct units and offset sign");
        var grant=plans.Apply(Message(P25ChannelFixtures.Grant));
        check(grant.Channel is {DownlinkHz:851025000,UplinkHz:806025000,Slot:null} && grant.Text.Contains("851.025000 MHz"),"FDMA grant resolves downlink/uplink while retaining original channel ID");
        check(plans.Apply(Message(P25ChannelFixtures.Site)).Channel is {DownlinkHz:851012500},"Site status uses its control-channel ID rather than grant field offsets");
        var net=Convert.FromHexString("3B0001ABCDE1231001058D58");
        check(plans.Apply(Message(net)).Channel is {DownlinkHz:851012500},"Network status resolves without clearing the same-system identifier table");
        var vu=plans.Apply(Message(P25ChannelFixtures.Vuhf));
        check(vu.BandPlan is {Identifier:2,BaseHz:150000000,SpacingHz:6250,OffsetHz:5000000,BandwidthHz:12500} && plans.Resolve(0x2008) is {DownlinkHz:150050000,UplinkHz:155050000},"Independent VHF/UHF identifier uses spacing-scaled positive offset");
        var tdma=plans.Apply(Message(P25ChannelFixtures.Tdma));
        check(tdma.BandPlan is {SlotsPerCarrier:2,ChannelType:3,OffsetHz:30000000} && plans.Apply(Message(P25ChannelFixtures.TdmaGrant)).Channel is {DownlinkHz:762025000,UplinkHz:792025000,Slot:1,SlotsPerCarrier:2},"Independent TDMA grant divides channel number into carrier and zero-based slot");
        foreach(var tuple in new[]{(0,1),(1,1),(2,1),(3,2),(4,4),(5,2)})
        {
            plans.Apply(Message(Change(P25ChannelFixtures.Tdma,20,4,tuple.Item1)));
            check(plans.Resolve(0x3007) is var resolved && resolved.DownlinkHz==762000000+12500*(7/tuple.Item2)&&resolved.Slot==(tuple.Item2>1?7%tuple.Item2:null),$"P25 channel type {tuple.Item1} uses {tuple.Item2} slot(s) per carrier");
        }
        plans.Apply(Message(Change(P25ChannelFixtures.Tdma,20,4,6)));
        check(plans.Resolve(0x3005).DownlinkHz==null,"Reserved TDMA channel type invalidates old table rather than assuming two slots");
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,30,8,128)));
        check(plans.Resolve(0x1002) is {DownlinkHz:851025000,UplinkHz:null},"No-uplink sentinel is not interpreted as a real transmit frequency");
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,29,1,1)));
        check(plans.Resolve(0x1002).UplinkHz==896025000,"FDMA positive offset direction is supported");
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,38,10,0)));
        check(plans.Resolve(0x1002).DownlinkHz==null,"Zero spacing invalidates a table instead of inventing a channel frequency");
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,48,32,0)));
        check(plans.Resolve(0x1002).DownlinkHz==null,"Zero base frequency is unresolved");
        plans.Apply(Message(P25ChannelFixtures.Fdma));
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,48,32,852000000/5)));
        check(plans.Resolve(0x1002).DownlinkHz==852025000,"A changed identifier replaces the current table");
        byte[] corrupt=(byte[])P25ChannelFixtures.Fdma.Clone();corrupt[7]^=1;
        plans.Apply(Message(corrupt));plans.Apply(Message(P25ChannelFixtures.Fdma) with{PayloadValid=false});
        plans.Apply(Message(Change(P25ChannelFixtures.Fdma,1,1,1)));plans.Apply(Message(Change(P25ChannelFixtures.Fdma,8,8,0x90)));
        check(plans.Resolve(0x1002).DownlinkHz==852025000,"Bad CRC, header-only, protected and manufacturer messages cannot replace a frequency table");
        plans.Apply(Message(P25ChannelFixtures.Grant,0x345));check(plans.Resolve(0x1002).DownlinkHz==null,"A new NAC cannot reuse the previous NAC's tables");
        plans.Apply(Message(P25ChannelFixtures.Fdma));plans.Apply(Message(net));
        plans.Apply(Message(Change(net,44,12,0x124)));check(plans.Resolve(0x1002).DownlinkHz==null,"A changed system identity clears learned tables on the same NAC");
        plans.Clear();plans.Apply(Message(P25ChannelFixtures.Fdma));plans.Apply(Message(P25ChannelFixtures.Site));
        plans.Apply(Message(Change(P25ChannelFixtures.Site,48,8,4)));check(plans.Resolve(0x1002).DownlinkHz==null,"A changed site identity clears learned tables");
        plans.Apply(Message(P25ChannelFixtures.Fdma));plans.Clear();check(plans.Resolve(0x1002).DownlinkHz==null,"Explicit source/gap reset clears frequency tables");
        check(plans.Resolve(0xFFFF).DownlinkHz==null,"Unspecified channel ID remains unresolved");

        var pipeline=new P25Pipeline{Enabled=true};var context=new PacketContext("channels","Synthetic P25",true,774000000);
        pipeline.Feed(P25Tests.Signal(symbols:P25ChannelFixtures.Symbols),context);
        var observations=pipeline.Drain();
        check(observations.Length==8 && observations.Count(r=>r.P25BandPlan!=null)==3 && observations.Last().P25Channel is {DownlinkHz:762025000,Slot:1},"Independent channel-table waveform passes production C4FM and pipeline enrichment");
        check(observations.All(r=>r.FrequencyHz==774000000),"Resolved traffic frequencies do not overwrite monitored input frequency");
        var panel=new PacketPanel();foreach(var row in observations)panel.Add(row);
        var band=observations.First(r=>r.P25BandPlan?.Identifier==1);
        panel.Add(band with{Information="Updated band",P25BandPlan=band.P25BandPlan! with{BaseHz=852000000},RawHex="1234"});
        using(var json=JsonDocument.Parse(panel.ExportJson()))
        {
            var bands=json.RootElement.EnumerateArray().Where(r=>r.GetProperty("Kind").GetString()=="Identifier update").ToArray();
            check(bands.Length==3&&bands.Single(r=>r.GetProperty("P25BandPlan").GetProperty("Identifier").GetInt32()==1).GetProperty("ReceptionCount").GetInt64()==2,"Identifier summaries update per table ID; multiple band IDs remain separate in export");
            check(json.RootElement.EnumerateArray().Any(r=>r.GetProperty("P25Channel").ValueKind==JsonValueKind.Object && r.GetProperty("P25Channel").GetProperty("DownlinkHz").GetInt64()==762025000),"JSON export retains structured traffic frequency and slot metadata");
        }
        panel.HiddenP25Types=["Identifier update"];check(panel.VisibleCount==4,"Identifier updates can be hidden without hiding grants or status");
        pipeline.Feed(P25Tests.Signal(),context with{Key="different-source"});
        check(pipeline.Drain().Where(r=>r.P25Channel!=null).All(r=>r.P25Channel!.DownlinkHz==null),"Pipeline source change cannot reuse old identifier tables");
        pipeline.Clear();pipeline.Feed(P25Tests.Signal(symbols:P25ChannelFixtures.Symbols),context);pipeline.Drain();pipeline.Reset();pipeline.Feed(P25Tests.Signal(),context);
        check(pipeline.Drain().Where(r=>r.P25Channel!=null).All(r=>r.P25Channel!.DownlinkHz==null),"Pipeline replay-gap reset clears identifier tables");
    }
}
