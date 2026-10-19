using System.Numerics;
namespace MRS;

internal sealed class DmrShortControl
{
    internal static readonly int[] Order=[0,4,8,12,14,18,22,1,2,3,5,6,7,9,10,11,13,15,16,17,19,20,21,23];
    internal static readonly int[] Parity=[27,31,23,7,14,28,17,11,22,5,10,20,1,2,4,8,16];
    private readonly List<bool> pending=[];private long last;private int color=-1;
    internal DmrObservation? Feed(bool[] cach,DmrObservation basis,long time)
    {
        if(time-last>2000||basis.ColorCode!=color)pending.Clear();last=time;color=basis.ColorCode;
        int word=DmrBits.Number(Order.Take(7).Select(i=>cach[i])),value=-1,best=2;
        for(int n=0;n<16;n++){int coded=n<<3;int[] checks=[5,7,6,3];for(int j=0;j<4;j++)if((n&(8>>j))!=0)coded^=checks[j];int distance=BitOperations.PopCount((uint)(coded^word));if(distance<best){best=distance;value=n;}else if(distance==best)value=-1;}
        if(value<0){pending.Clear();return null;}int flag=value&3;
        if(flag==1)pending.Clear();
        else if(flag==0 || flag==3&&pending.Count is not (17 or 34) || flag==2&&pending.Count!=51){pending.Clear();return null;}
        pending.AddRange(Order.Skip(7).Select(i=>cach[i]));if(flag!=2)return null;
        var payload=Decode(pending.ToArray());pending.Clear();return payload==null?null:Parse(payload,basis);
    }
    internal static byte[]? Decode(bool[] raw)
    {
        if(raw.Length!=68)return null;
        bool[] d=Enumerable.Range(0,68).Select(i=>raw[i==67?67:i*4%67]).ToArray();
        for(int row=0;row<3;row++)
        {
            int syndrome=0;for(int i=0;i<17;i++)if(d[row*17+i])syndrome^=Parity[i];
            if(syndrome!=0){int bit=Array.IndexOf(Parity,syndrome);if(bit<0)return null;d[row*17+bit]=!d[row*17+bit];}
        }
        for(int i=0;i<17;i++)if(d[i]^d[17+i]^d[34+i]^d[51+i])return null;
        var bits=Enumerable.Range(0,3).SelectMany(r=>d.Skip(r*17).Take(12)).ToArray();
        int crc=0;foreach(bool bit in bits){int feedback=((crc>>7)&1)^(bit?1:0);crc=((crc<<1)&255)^(feedback!=0?7:0);}if(crc!=0)return null;
        return DmrBits.Bytes(bits.Concat(new bool[4]).ToArray());
    }
    internal static DmrObservation? Parse(byte[] p,DmrObservation basis)
    {
        int op=TrunkSignaling.Bits(p,0,4);
        var o=basis with{DataType=-2,Source=null,Destination=null,Group=false,LinkValid=true,Network=null,Site=null,RestChannel=null,Raw=Convert.ToHexString(p),Validation="CACH Hamming + short LC product code + CRC-8 valid",Detail="Short link control"};
        if(op is 9 or 10)return o with{SystemType="Connect Plus",Network=TrunkSignaling.Bits(p,4,12),Site=TrunkSignaling.Bits(p,16,8),Detail=op==10?"Control-channel identity":"Traffic-channel identity"};
        if(op==15&&TrunkSignaling.Bits(p,4,8)==0x10)return o with{SystemType="Motorola rest signaling",Site=TrunkSignaling.Bits(p,20,5),RestChannel=TrunkSignaling.Bits(p,15,5),Detail="Cap+ compatible short LC; subtype needs CSBK / call confirmation"};
        if(op is 2 or 3){int model=TrunkSignaling.Bits(p,4,2),n=new[]{9,7,4,2}[model];return o with{SystemType="DMR Tier III",Network=TrunkSignaling.Bits(p,6,n),Site=TrunkSignaling.Bits(p,6+n,12-n),Detail=$"Short LC model {model}; raw NET/SITE values"};}
        return null;
    }
}
