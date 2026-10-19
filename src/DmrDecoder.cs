using System.Numerics;



namespace MRS;



internal sealed record DmrObservation(int ColorCode,int DataType,int? Slot,uint? Source,uint? Destination,bool Group,bool LinkValid,string Raw,string Validation)

{

    public string SystemType {get;init;}="DMR (type unknown)";

    public int? Network {get;init;} public int? Site {get;init;} public int? RestChannel {get;init;}

    public int? Area {get;init;} public int? Home {get;init;} public int? LtrGroup {get;init;} public int? Channel {get;init;} public int? FreeChannel {get;init;}

    public string Detail {get;init;}="";
    public int SlotCorrections {get;init;}

    public string Protocol=>Area.HasValue?"LTR":"DMR";

    public string Identity=>Area.HasValue?$"{Area}-{Home:00}-{LtrGroup:000}":$"CC {ColorCode}";

    public string TrunkSummary=>$"{SystemType}"+(Network is {} n?$" · NET {n}":"")+(Site is {} s?$" · site {s}":"")+(RestChannel is {} r?$" · rest LSN {r}":"")+(Area.HasValue?$" · {Identity} · channel {Channel} · free {FreeChannel}":"")+($" · {Detail}");

}



// ETSI TS 102 361-1 v2.6.1: burst layout, B.1.1, B.3.1/4/5/6; no vocoder.



internal static class DmrBits



{



    internal static readonly uint[] GolayRows=[0x80000|0x3DA,0x40000|0xD99,0x20000|0x6CD,0x10000|0x367,0x8000|0xDC6,0x4000|0xA97,0x2000|0x93E,0x1000|0x8EB];



    internal static uint Golay(int value){uint word=0;for(int i=0;i<8;i++)if((value&(128>>i))!=0)word^=GolayRows[i];return word;}



    internal static int DecodeGolay(uint word){int best=4,value=-1;for(int i=0;i<256;i++){int d=BitOperations.PopCount(word^Golay(i));if(d<best){best=d;value=i;}else if(d==best)value=-1;}return value;}



    internal static readonly int[] RowParity=[9,13,15,14,7,10,5,11,12,6,3],ColParity=[15,14,7,10,5,11,12,6,3];



    internal static int Syndrome(bool[] bits,int[] parity){int syndrome=0;for(int i=0;i<parity.Length;i++)if(bits[i])syndrome^=parity[i];for(int i=0;i<4;i++)if(bits[parity.Length+i])syndrome^=8>>i;return syndrome;}



    private static bool Correct(bool[] bits,int[] parity){int s=Syndrome(bits,parity);if(s==0)return false;for(int i=0;i<bits.Length;i++)if((i<parity.Length?parity[i]:8>>(i-parity.Length))==s){bits[i]=!bits[i];return true;}return false;}



    internal static byte[]? Bptc(bool[] raw)



    {



        if(raw.Length!=196)return null;var data=new bool[196];for(int i=0;i<196;i++)data[i]=raw[i*181%196];



        for(int pass=0;pass<5;pass++){bool changed=false;for(int c=0;c<15;c++){var col=Enumerable.Range(0,13).Select(r=>data[1+r*15+c]).ToArray();changed|=Correct(col,ColParity);for(int r=0;r<13;r++)data[1+r*15+c]=col[r];}for(int r=0;r<9;r++){var row=data.Skip(1+r*15).Take(15).ToArray();changed|=Correct(row,RowParity);Array.Copy(row,0,data,1+r*15,15);}if(!changed)break;}



        for(int c=0;c<15;c++)if(Syndrome(Enumerable.Range(0,13).Select(r=>data[1+r*15+c]).ToArray(),ColParity)!=0)return null;



        for(int r=0;r<9;r++)if(Syndrome(data.Skip(1+r*15).Take(15).ToArray(),RowParity)!=0)return null;



        var information=new List<bool>();information.AddRange(data.Skip(4).Take(8));for(int r=1;r<9;r++)information.AddRange(data.Skip(1+r*15).Take(11));return Bytes(information.ToArray());



    }



    internal static byte[] Bytes(bool[] bits){var result=new byte[bits.Length/8];for(int i=0;i<result.Length*8;i++)if(bits[i])result[i/8]|=(byte)(128>>(i%8));return result;}



    internal static int Number(IEnumerable<bool> bits){int n=0;foreach(bool b in bits)n=(n<<1)|(b?1:0);return n;}



    private static int Multiply(int a,int b){int n=0;while(b!=0){if((b&1)!=0)n^=a;b>>=1;a<<=1;if((a&256)!=0)a^=0x11D;}return n;}



    internal static readonly int[,] RsMatrix={{0x1c,0xbc,0xfd},{0x89,0x31,8},{0xad,0x41,0x36},{0x7d,0x71,0x16},{0xf3,0xa6,0x3a},{8,0x83,0x7b},{0x3f,0x6f,2},{0x6c,0x0d,0xa7},{0x0e,0x38,0x40}};



    internal static byte[] RsParity(byte[] message){var p=new byte[3];for(int i=0;i<9;i++)for(int j=0;j<3;j++)p[j]^=(byte)Multiply(message[i],RsMatrix[i,j]);return p;}



    internal static DmrObservation? Parse(bool[] burst,bool[]? cach)



    {



        if(burst.Length!=264)return null;int slotType=DecodeGolay((uint)Number(burst.Skip(98).Take(10).Concat(burst.Skip(156).Take(10))));if(slotType<0)return null;



        int? slot=null;if(cach is {Length:24}){int[] positions=[0,4,8,12,14,18,22];int word=Number(positions.Select(i=>cach[i]));int best=2,decoded=-1;for(int v=0;v<16;v++){int coded=v<<3;int[] p=[5,7,6,3];for(int i=0;i<4;i++)if((v&(8>>i))!=0)coded^=p[i];int d=BitOperations.PopCount((uint)(coded^word));if(d<best){best=d;decoded=v;}else if(d==best)decoded=-1;}if(decoded>=0)slot=(decoded&4)!=0?2:1;}



        int type=slotType&15,cc=slotType>>4;byte[]? lc=Bptc(burst.Take(98).Concat(burst.Skip(166).Take(98)).ToArray());bool valid=false;uint? source=null,dest=null;bool group=false;



        // Only implemented BPTC-protected burst types qualify; reserved or failed payloads stay unidentified.



        if(type is not (1 or 2 or 3 or 6 or 7 or 9)||lc==null)return null;



        if(type is 1 or 2 && lc!=null){int mask=type==1?0x96:0x99;var parity=RsParity(lc);valid=Enumerable.Range(0,3).All(i=>(lc[9+i]^mask)==parity[i]);if(valid&&lc[1]==0&&(lc[0]&0x80)==0&&(lc[0]&63) is 0 or 3){group=(lc[0]&63)==0;dest=(uint)(lc[3]<<16|lc[4]<<8|lc[5]);source=(uint)(lc[6]<<16|lc[7]<<8|lc[8]);}}



        return TrunkSignaling.Enrich(new(cc,type,slot,source,dest,group,valid,Convert.ToHexString(Bytes(burst)),valid?"Slot Golay + BPTC + LC RS valid":"Slot Golay + BPTC valid; payload identifiers unavailable"),lc!) with{SlotCorrections=BitOperations.PopCount((uint)Number(burst.Skip(98).Take(10).Concat(burst.Skip(156).Take(10)))^Golay(slotType))};



    }



}



internal sealed class DmrDecoder



{



    private sealed class Phase{internal readonly double[] Ring=new double[144];internal int At,Count,Remaining=-1;internal double Scale,Center;internal bool Base;}



    private readonly DmrShortControl shortControl=new();

    private readonly Phase[] phases=Enumerable.Range(0,10).Select(_=>new Phase()).ToArray();private readonly double[] average=new double[10];private int index;private double sum;private long time;private readonly Dictionary<string,long> recent=[];



    private static readonly (double[] Signs,double Mean,double Energy,bool Base)[] Sync=new[]{(0xDFF57D75DF5DUL,true),(0xD5D7F77FD757UL,false)}.Select(p=>{var a=Enumerable.Range(0,24).Select(n=>((p.Item1>>(46-2*n))&3)==1?1.0:-1.0).ToArray();double m=a.Average();return(a,m,a.Sum(x=>(x-m)*(x-m)),p.Item2);}).ToArray();



    internal event Action<DmrObservation>? Message;



    internal void Process(ReadOnlySpan<double> samples)



    {



        foreach(double v in samples){if(!double.IsFinite(v))continue;sum+=v-average[index];average[index]=v;index=(index+1)%10;time++;var p=phases[index];p.Ring[p.At]=sum/10;p.At=(p.At+1)%144;p.Count=Math.Min(144,p.Count+1);



            if(p.Remaining>0&&--p.Remaining==0){var symbols=Enumerable.Range(0,144).Select(n=>p.Ring[(p.At+n)%144]).ToArray();var bits=symbols.SelectMany(x=>{double level=(x-p.Center)/p.Scale;int d=level>=0?(level>2.0/3?1:0):(level< -2.0/3?3:2);return new[]{(d&2)!=0,(d&1)!=0};}).ToArray();if(p.Count==144&&DmrBits.Parse(bits.Skip(24).ToArray(),p.Base?bits.Take(24).ToArray():null) is {} result){string key=$"{result.ColorCode}:{result.Raw}";if(!recent.TryGetValue(key,out long at)||time-at>=960){recent[key]=time;Message?.Invoke(result);if(p.Base && shortControl.Feed(bits.Take(24).ToArray(),result,time) is {} control)Message?.Invoke(control);}foreach(var old in recent.Where(x=>time-x.Value>48000).Select(x=>x.Key).ToArray())recent.Remove(old);}p.Remaining=-1;}



            if(p.Remaining>0||p.Count<90)continue;



            foreach(var pattern in Sync){double[] signs=pattern.Signs;double mean=0;for(int n=0;n<24;n++)mean+=p.Ring[(p.At+120+n)%144];mean/=24;double sm=pattern.Mean,energy=pattern.Energy,scale=0;for(int n=0;n<24;n++)scale+=(p.Ring[(p.At+120+n)%144]-mean)*(signs[n]-sm);scale/=energy;double center=mean-scale*sm,residual=0;for(int n=0;n<24;n++){double error=p.Ring[(p.At+120+n)%144]-center-scale*signs[n];residual+=error*error;}if(Math.Abs(scale)>150&&Math.Abs(scale)<6000&&residual/24<scale*scale*.08){p.Scale=scale;p.Center=center;p.Base=pattern.Base;p.Remaining=54;break;}}



        }



    }



}







