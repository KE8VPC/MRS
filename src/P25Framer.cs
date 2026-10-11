using System.Numerics;
namespace MRS;

internal sealed record P25Message(int Nac,int Duid,int NidErrors,byte[] Raw,string Kind,string Text,bool PayloadValid,P25BandPlan? BandPlan=null,P25Channel? Channel=null,P25GrantAssignment[]? Assignments=null,P25SiteAnnouncement? SiteAnnouncement=null);

// Independent hard-decision implementation of the P25 Phase I wire format.
// Protocol constants and fixture provenance are documented in P25.md.
internal sealed class P25Framer
{
    internal const ulong Sync=0x5575F5FF77FF;
    private static readonly int[] Duids=[0,3,5,7,10,12,15];
    private static readonly (ushort Data,ulong Code)[] Codebook=Enumerable.Range(0,4096).SelectMany(n=>Duids.Select(d=>((ushort)(n*16+d),EncodeNid((ushort)(n*16+d))))).ToArray();
    private ulong shift,nid;
    private int physical=-1,nidSymbols,blockSymbols,blocks,nac,duid,errors;
    private readonly byte[] block=new byte[98];
    internal event Action<P25Message>? Message;
    internal long BadNid, BadCrc;
    internal void Begin(){physical=24;nid=shift=0;nidSymbols=blockSymbols=blocks=0;}
    internal static ulong EncodeNid(ushort data)
    {
        const ulong polynomial=0xCD930BDD3B2B;
        ulong word=(ulong)data<<47,remainder=word;
        for(int bit=62;bit>=47;bit--)if((remainder&(1UL<<bit))!=0)remainder^=polynomial<<(bit-47);
        return ((word|remainder)<<1)|((data&15) is 5 or 10?1UL:0UL);
    }
    internal static bool DecodeNid(ulong raw,out int nac,out int duid,out int errors)
    {
        foreach(var entry in Codebook)
        {
            int distance=BitOperations.PopCount(raw^entry.Code);
            if(distance>4)continue;
            nac=entry.Data>>4;duid=entry.Data&15;errors=distance;return true;
        }
        nac=duid=errors=0;return false;
    }
    internal void Push(int dibit)
    {
        shift=((shift<<2)|(uint)dibit)&0xFFFFFFFFFFFF;
        if(physical<0)
        {
            if(BitOperations.PopCount(shift^Sync)<=2)Begin();
            return;
        }
        bool status=physical%36==35;physical++;if(status)return;
        if(nidSymbols<32)
        {
            nid=(nid<<2)|(uint)dibit;
            if(++nidSymbols!=32)return;
            if(!DecodeNid(nid,out nac,out duid,out errors)){BadNid++;physical=-1;return;}
            var raw=BitConverter.GetBytes(nid);Array.Reverse(raw);
            Message?.Invoke(new(nac,duid,errors,raw,"NID header",$"NAC {nac:X3} · DUID {duid:X1} · header verified; payload not decoded",false));
            if(duid!=7)physical=-1;
            return;
        }
        block[blockSymbols++]=(byte)dibit;
        if(blockSymbols!=98)return;
        blockSymbols=0;blocks++;
        var decoded=DecodeBlock(block);
        if(decoded==null){BadCrc++;physical=-1;return;}
        var (kind,text)=Describe(decoded);
        Message?.Invoke(new(nac,duid,errors,decoded,kind,$"NAC {nac:X3} · {text}",true));
        if((decoded[0]&128)!=0 || blocks==3)physical=-1;
    }
    internal static byte[]? DecodeBlock(byte[] transmitted)
    {
        if(transmitted.Length!=98)return null;
        var symbols=new byte[98];int offset=0;
        for(int column=0;column<4;column++)for(int pair=column;pair<49;pair+=4){symbols[pair*2]=transmitted[offset++];symbols[pair*2+1]=transmitted[offset++];}
        // Four-state rate-1/2 trellis outputs, indexed by previous/current dibit.
        int[] constellation=[2,12,1,15,14,0,13,3,9,7,10,4,5,11,6,8];
        int[] costs=[0,10000,10000,10000];var previous=new int[49,4];
        for(int step=0;step<49;step++)
        {
            int observed=(symbols[step*2]<<2)|symbols[step*2+1];int[] next=[10000,10000,10000,10000];
            for(int state=0;state<4;state++)for(int data=0;data<4;data++)
            {
                int metric=costs[state]+BitOperations.PopCount((uint)(observed^constellation[state*4+data]));
                if(metric<next[data]){next[data]=metric;previous[step,data]=state;}
            }
            costs=next;
        }
        if(costs[0]>12)return null;
        var decoded=new byte[12];int end=0;
        for(int step=48;step>=0;step--){if(step<48)decoded[step/4]|=(byte)(end<<(6-2*(step%4)));end=previous[step,end];}
        return Crc(decoded.AsSpan(0,10))==((decoded[10]<<8)|decoded[11])?decoded:null;
    }
    internal static ushort Crc(ReadOnlySpan<byte> bytes)
    {
        ushort crc=0;
        foreach(byte value in bytes){crc^=(ushort)(value<<8);for(int n=0;n<8;n++)crc=(ushort)((crc<<1)^((crc&0x8000)!=0?0x1021:0));}
        return (ushort)(crc^0xFFFF);
    }
    internal static (string Kind,string Text) Describe(byte[] b)
    {
        int opcode=b[0]&63,mfid=b[1];
        string prefix=$"Opcode {opcode:X2} · MFID {mfid:X2}";
        if((b[0]&64)!=0)return("Protected TSBK",prefix+" · protected payload; not interpreted");
        if(mfid!=0)return("Manufacturer TSBK",prefix+" · unimplemented manufacturer format");
        int U16(int start)=>(b[start]<<8)|b[start+1];
        return opcode switch
        {
            0x3B=>("Network status",$"WACN {((b[3]<<12)|(b[4]<<4)|(b[5]>>4)):X5} · System {(((b[5]&15)<<8)|b[6]):X3} · control channel ID {U16(7):X4}"),
            0x3A=>("Site status",$"System {(((b[3]&15)<<8)|b[4]):X3} · RFSS {b[5]} · Site {b[6]} · control channel ID {U16(7):X4}"),
            0x00=>("Group voice grant",$"Talkgroup {U16(5)} · Source {((b[7]<<16)|(b[8]<<8)|b[9])} · channel ID {U16(3):X4} · service options {b[2]:X2}"),
            _=>("TSBK",prefix+" · CRC verified; message fields not implemented")
        };
    }
}


