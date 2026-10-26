using System.IO;
using System.Runtime.InteropServices;
namespace MRS;
internal sealed class ImbeVocoder:IDisposable
{
    private static readonly Lazy<IntPtr> Library=new(()=>NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory,"Decoders","Voice","mrs_voice.dll")));
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Create();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Free(IntPtr p);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Decode(IntPtr p,byte[] symbols,[Out] float[] audio);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Metadata(int duid,byte[] symbols,int length,[Out] byte[] data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Encode(int duid,byte[] data,[Out] byte[] symbols);
    private static T Get<T>(string name) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(Library.Value,name));
    private static readonly object FecGate=new();
    private IntPtr handle=Get<Create>("mrs_create")();
    internal static byte[]? ReadMetadata(int duid,byte[] symbols){if(duid is not(0 or 5 or 10)||symbols.Length!=(duid==0?324:120))return null;var result=new byte[15];int n;lock(FecGate)n=Get<Metadata>("mrs_metadata")(duid,symbols,symbols.Length,result);return n>0?result[..n]:null;}
    internal static byte[] EncodeMetadata(int duid,byte[] data){if(duid is not(0 or 5 or 10)||data.Length!=(duid==0?15:duid==10?12:9))throw new ArgumentException();var output=new byte[duid==0?324:120];lock(FecGate)Get<Encode>("mrs_encode_metadata")(duid,data,output);return output;}
    internal float[] DecodeFrame(byte[] symbols,out int errors){if(handle==IntPtr.Zero||symbols.Length!=72)throw new ArgumentException();var audio=new float[160];errors=Get<Decode>("mrs_imbe")(handle,symbols,audio);return audio;}
    public void Dispose(){if(handle!=IntPtr.Zero){Get<Free>("mrs_free")(handle);handle=IntPtr.Zero;}}
}
internal sealed record P25VoiceUnit(int Nac,int Duid,byte[] Payload);
internal sealed class P25VoiceFramer
{
    private int physical=-1,nidCount,nac,duid,needed;private ulong nid;private readonly List<byte> payload=[];
    internal event Action<P25VoiceUnit>? Unit;
    internal void Begin(){physical=24;nidCount=0;nid=0;payload.Clear();needed=0;}
    internal void Push(int dibit)
    {
        if(physical<0)return;bool status=physical%36==35;physical++;if(status)return;
        if(nidCount<32){nid=(nid<<2)|(uint)dibit;if(++nidCount<32)return;
            if(!P25Framer.DecodeNid(nid,out nac,out duid,out var errors)||errors>2){physical=-1;return;}
            needed=duid==0?324:duid is 5 or 10?784:0;
            if(duid is 0 or 3 or 15)Unit?.Invoke(new(nac,duid,[]));
            if(needed==0)physical=-1;return;}
        payload.Add((byte)dibit);if(payload.Count==needed){Unit?.Invoke(new(nac,duid,payload.ToArray()));physical=-1;}
    }
}
// Per-channel voice state. Unknown/failed encryption metadata never authorizes audio.
internal sealed class P25VoiceDecoder:IDisposable
{
    private ImbeVocoder? vocoder;private int nac=-1,algorithm=-1,key;private long samples,authorizedAt=-480000;private P25C4fm modem=new();
    internal int? ExpectedNac,ExpectedGroup;private bool groupChecked;
    internal event Action<byte[]>? Audio;
    internal string Status {get;private set;}="Waiting for P25 Phase I C4FM voice";
    internal int Frames {get;private set;}
    internal P25VoiceDecoder(){modem.VoiceUnit+=Accept;}
    internal void Feed(double[] values){samples+=values.Length;modem.Process(values);}
    internal void Accept(P25VoiceUnit unit)
    {
        if(ExpectedNac is int expected&&unit.Nac!=expected){algorithm=-1;groupChecked=false;Status="NAC mismatch; muted";return;}
        if(unit.Nac!=nac){groupChecked=false;nac=unit.Nac;algorithm=-1;vocoder?.Dispose();vocoder=null;}
        if(unit.Payload.Length==0){groupChecked=false;algorithm=-1;vocoder?.Dispose();vocoder=null;Status=unit.Duid==0?"Voice header; checking encryption":"Call ended";return;}
        if(unit.Duid==0){ApplyMetadata(ImbeVocoder.ReadMetadata(0,unit.Payload),true);return;}
        int[] starts=[0,72,164,256,348,440,532,624,712];
        var metadata=new byte[120];for(int i=0;i<6;i++)Buffer.BlockCopy(unit.Payload,144+i*92,metadata,i*20,20);
        var fields=ImbeVocoder.ReadMetadata(unit.Duid,metadata);
        if(unit.Duid==10){bool previouslyClear=algorithm==0x80&&samples-authorizedAt<=48000;if(!ApplyMetadata(fields,false)||!previouslyClear)return;}
        else if(fields==null){algorithm=-1;Status="Link-control FEC failed; voice muted";return;}
        // Service-options encryption bit on standard group/individual voice link control.
        else if(fields[1]==0 && (fields[0]&63) is 0 or 3 && (fields[2]&64)!=0){algorithm=-1;Status="Encrypted service option; voice muted";return;}
        if(unit.Duid==5&&ExpectedGroup!=null)groupChecked=fields!=null&&fields[1]==0&&(fields[0]&63)==0&&((fields[4]<<8)|fields[5])==ExpectedGroup;
        if(ExpectedGroup!=null&&!groupChecked){Status="Talkgroup identity unverified/mismatch; muted";return;}
        if(algorithm!=0x80||samples-authorizedAt>48000){Status=algorithm>=0&&algorithm!=0x80?$"Encrypted · ALG {algorithm:X2} · key ID {key:X4} · muted":"Encryption state unknown/stale; voice muted";return;}
        vocoder??=new();var pcm=new byte[9*160*6*4];int at=0,bad=0;
        foreach(int start in starts){var audio=vocoder.DecodeFrame(unit.Payload[start..(start+72)],out int errors);if(errors<0||errors>6){Array.Clear(audio);bad++;}
            foreach(float value in audio){short sample=(short)Math.Clamp(float.IsFinite(value)?value:0,-28000,28000);for(int k=0;k<6;k++){pcm[at++]=(byte)sample;pcm[at++]=(byte)(sample>>8);pcm[at++]=(byte)sample;pcm[at++]=(byte)(sample>>8);}}}
        Frames+=9;Status=$"Clear P25 I · NAC {nac:X3} · {Frames} voice frames · {bad}/9 muted for errors";Audio?.Invoke(pcm);
    }
    private bool ApplyMetadata(byte[]? data,bool header)
    {
        if(data==null){algorithm=-1;Status="Encryption metadata FEC failed; muted";return false;}
        if(header)groupChecked=ExpectedGroup==null||((data[13]<<8)|data[14])==ExpectedGroup;
        int offset=header?10:9;algorithm=data[offset];key=(data[offset+1]<<8)|data[offset+2];authorizedAt=samples;
        Status=algorithm==0x80?$"Clear P25 I · NAC {nac:X3}":$"Encrypted/unsupported · ALG {algorithm:X2} · key ID {key:X4} · muted";
        return algorithm==0x80;
    }
    public void Dispose()=>vocoder?.Dispose();
}
