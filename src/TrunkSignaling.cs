namespace MRS;

// Wire-format fields only; unknown/vendor-reserved fields never become identities.
internal static class TrunkSignaling
{
    internal static int Bits(byte[] p,int start,int count){int v=0;for(int i=start;i<start+count;i++)v=(v<<1)|((p[i/8]>>(7-i%8))&1);return v;}
    internal static ushort Crc16(ReadOnlySpan<byte> p)
    {
        int crc=0;foreach(byte b in p){crc^=b<<8;for(int i=0;i<8;i++)crc=((crc<<1)^((crc&0x8000)!=0?0x1021:0))&65535;}return (ushort)(crc^65535);
    }
    internal static DmrObservation Enrich(DmrObservation o,byte[] p)
    {
        int op=p[0]&63,fid=p[1];
        if(o.DataType is 1 or 2 && o.LinkValid && (p[0]&0x80)==0 && fid==0x10 && op is 4 or 7)
            return o with{SystemType="Capacity Plus",Source=(uint)Bits(p,56,16),Destination=(uint)Bits(p,24,24),Group=op==4,RestChannel=Bits(p,52,4),Detail="Capacity Plus full link control"};
        if(o.DataType!=3)return o;
        bool valid=Crc16(p.AsSpan(0,10))==((p[10]<<8|p[11])^0xA5A5);
        o=o with{LinkValid=valid,Validation=valid?"Slot Golay + BPTC + CSBK CRC valid":"CSBK CRC failed; identifiers withheld",Detail=$"CSBK opcode {op:X2} · FID {fid:X2}"};
        if(!valid||(p[0]&0x40)!=0)return o;
        if(fid==0x10 && op==0x3E)
            return o with{SystemType="Capacity Plus",RestChannel=Bits(p,20,4),Detail=$"Capacity Plus channel status · fragment {Bits(p,16,2)} (0 continuation, 1 final, 2 first, 3 single)"};
        if(fid==0x10 && op==0x3B)
        {
            var neighbors=Enumerable.Range(4,6).Where(i=>(p[i]>>4)!=0).Select(i=>$"{p[i]>>4}/rest {p[i]&15}");
            return o with{SystemType="Capacity Plus",Detail="Adjacent sites (not current site): "+string.Join(", ",neighbors)};
        }
        if(fid is 0 or 0x10 && op==0x19)
        {
            int model=Bits(p,40,2),netBits=new[]{9,7,4,2}[model];
            return o with{SystemType="DMR Tier III",Network=Bits(p,42,netBits),Site=Bits(p,42+netBits,12-netBits),Detail=$"ALOHA · model {model} · raw system identity {Bits(p,40,14):X4} · NET/SITE are raw wire values"};
        }
        return o;
    }
}

internal static class LtrWords
{
    // CRC-7 wire contributions (area through free-channel), independently used as protocol constants.
    private static readonly int[] Weights=[0x38,0x1C,0x0E,0x46,0x23,0x51,0x68,0x75,0x7A,0x3D,0x1F,0x4F,0x26,0x52,0x29,0x15,0x0B,0x45,0x62,0x31,0x19,0x0D,0x07,0x43];
    internal static int Checksum(ulong word){int c=0;for(int i=0;i<24;i++)if((word&(1UL<<(30-i)))!=0)c^=Weights[i];return c;}
    internal static DmrObservation? Parse(ulong word)
    {
        if((word>>31)!=0x158 || Checksum(word)!=(int)(word&127))return null;
        int area=(int)(word>>30)&1,ch=(int)(word>>25)&31,home=(int)(word>>20)&31,group=(int)(word>>12)&255,free=(int)(word>>7)&31;
        if((ch is <1 or >20)&&ch!=31 || home is <1 or >20 || free>20&&free!=31)return null;
        string kind=ch==31?"Call end":group==255&&ch==free?"Idle":"Call / routing";
        return new(-1,-1,null,null,null,false,true,word.ToString("X10"),"LTR OSW sync + CRC-7 valid")
        {SystemType="LTR Standard",Area=area,Home=home,LtrGroup=group,Channel=ch,FreeChannel=free,Detail=kind};
    }
}

// 48 kHz flat discriminator -> 300 symbols/s. Sixteen timing hypotheses,
// integrate-and-dump and frame-local level estimation retain short idle bursts.
internal sealed class LtrDecoder
{
    private sealed class Phase{internal readonly double[] Values=new double[40];internal int At,Count;}
    private readonly Phase[] phases=Enumerable.Range(0,16).Select(_=>new Phase()).ToArray();
    private readonly double[] average=new double[160];private int at;private long time;private double sum;
    private readonly Dictionary<string,long> recent=[];
    internal event Action<DmrObservation>? Message;
    internal void Process(ReadOnlySpan<double> samples)
    {
        foreach(double sample in samples)
        {
            if(!double.IsFinite(sample))continue;
            sum+=sample-average[at];average[at]=sample;at=(at+1)%160;time++;
            if(time%10!=0)continue;
            var p=phases[(time/10)%16];p.Values[p.At]=sum/160;p.At=(p.At+1)%40;p.Count=Math.Min(40,p.Count+1);if(p.Count<40)continue;
            double lo=p.Values.Min(),hi=p.Values.Max(),mid=(lo+hi)/2;if(hi-lo<80)continue;
            ulong word=0;double error=0;foreach(double v in Enumerable.Range(0,40).Select(i=>p.Values[(p.At+i)%40])){word=(word<<1)|(v>mid?1UL:0);error+=Math.Pow(v>mid?v-hi:v-lo,2);}
            if(error/40>Math.Pow(hi-lo,2)*.06)continue;
            // Reversed discriminator polarity is accepted; only outbound CRC convention is implemented.
            var m=LtrWords.Parse(word)??LtrWords.Parse(word^0xFFFFFFFFFFUL);if(m==null)continue;
            if(!recent.TryGetValue(m.Raw,out long old)||time-old>320){recent[m.Raw]=time;Message?.Invoke(m);}
            if(recent.Count>128)foreach(var key in recent.Where(x=>time-x.Value>480000).Select(x=>x.Key).ToArray())recent.Remove(key);
        }
    }
}
