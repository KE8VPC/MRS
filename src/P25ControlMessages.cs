namespace MRS;

internal sealed record P25GrantAssignment(int TalkgroupId,int? SourceRadioId,int? ServiceOptions,P25Channel Channel);
internal sealed record P25ControlChannel(P25Channel Channel,int ServiceClass);
internal sealed record P25SiteAnnouncement(int? SystemId,int RfssId,int SiteId,int? LocationRegistrationArea,int? Flags,P25ControlChannel[] Channels);

internal static class P25ControlMessages
{
    // Called only after the pipeline has verified CRC, standard MFID and the
    // unprotected flag. One received TSBK stays one observation even with two grants.
    internal static P25Message? Describe(P25Message message,P25ChannelPlans plans)
    {
        var b=message.Raw;int op=b[0]&63;
        int U16(int n)=>(b[n]<<8)|b[n+1];
        string Prefix()=>$"NAC {message.Nac:X3}";
        string GrantText(P25GrantAssignment grant)=>$"Talkgroup {grant.TalkgroupId} · {P25ChannelPlans.DescribeChannel(grant.Channel)}"+
            (grant.SourceRadioId is int source?$" · Source {source}":"")+(grant.ServiceOptions is int options?$" · service options {options:X2}":"");
        if(op is 0x00 or 0x02 or 0x03)
        {
            var grants=new List<P25GrantAssignment>();
            if(op==0x00)grants.Add(new(U16(5),(b[7]<<16)|(b[8]<<8)|b[9],b[2],plans.Resolve(U16(3))));
            else if(op==0x03)grants.Add(new(U16(8),null,b[2],plans.ResolveExplicit(U16(4),U16(6))));
            else
            {
                grants.Add(new(U16(4),null,null,plans.Resolve(U16(2))));
                // Suppress unused/identically repeated B entries, but preserve
                // distinct TDMA slots even when they map to the same carrier.
                if(U16(8)!=0 && (U16(8)!=U16(4)||U16(6)!=U16(2)))grants.Add(new(U16(8),null,null,plans.Resolve(U16(6))));
            }
            return message with{Kind=op switch{0x02=>"Group voice grant update",0x03=>"Explicit group voice grant",_=>"Group voice grant"},
                Text=string.Join(" | ",grants.Select(GrantText))+" · "+Prefix(),Channel=grants[0].Channel,Assignments=grants.ToArray()};
        }
        if(op==0x39)
        {
            var channels=new List<P25ControlChannel>{new(plans.Resolve(U16(4)),b[6])};
            if(b[9]!=0 && U16(7)!=U16(4))channels.Add(new(plans.Resolve(U16(7)),b[9]));
            var site=new P25SiteAnnouncement(null,b[2],b[3],null,null,channels.ToArray());
            return message with{Kind="Secondary control channels",Text=$"RFSS {site.RfssId} · Site {site.SiteId} · "+
                string.Join(" | ",channels.Select(c=>$"{P25ChannelPlans.DescribeChannel(c.Channel)} · service class {c.ServiceClass:X2}"))+" · "+Prefix(),
                Channel=channels[0].Channel,SiteAnnouncement=site};
        }
        if(op==0x3C)
        {
            var channel=plans.Resolve(U16(7));
            var site=new P25SiteAnnouncement(((b[3]&15)<<8)|b[4],b[5],b[6],b[2],b[3]>>4,[new(channel,b[9])]);
            return message with{Kind="Adjacent site status",Text=$"System {site.SystemId:X3} · RFSS {site.RfssId} · Site {site.SiteId} · "+
                P25ChannelPlans.DescribeChannel(channel)+$" · LRA {site.LocationRegistrationArea:X2} · flags {site.Flags:X1} · service class {b[9]:X2} · "+Prefix(),
                Channel=channel,SiteAnnouncement=site};
        }
        return null;
    }
}
