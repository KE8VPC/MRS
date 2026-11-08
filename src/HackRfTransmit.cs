using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
namespace MRS;
internal sealed record TxTestPlan(long Frequency,int Gain=0,int Seconds=1)
{
    internal const int Rate=8000000;
    internal void Validate()
    {
        // First preview deliberately limits the test to the user's 2 m / 70 cm equipment.
        if(!(Frequency>=144000000&&Frequency<=148000000 || Frequency>=420000000&&Frequency<=450000000))throw new ArgumentException("This test supports 144–148 or 420–450 MHz only. Choose a frequency you are authorized to use.");
        if(Gain<0||Gain>20||Seconds<1||Seconds>3)throw new ArgumentException("Test gain must be 0–20 dB and duration 1–3 seconds.");
    }
    internal static byte[] Tone()
    {
        var data=new byte[16000];
        for(int i=0;i<8000;i++){double phase=-2.5*Math.Cos(2*Math.PI*i/8000);data[2*i]=unchecked((byte)(sbyte)Math.Round(32*Math.Cos(phase)));data[2*i+1]=unchecked((byte)(sbyte)Math.Round(32*Math.Sin(phase)));}
        return data;
    }
}
internal sealed partial class HackRfNative
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FlushCallback(IntPtr context,byte success);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int EnableFlush(IntPtr device,FlushCallback callback,IntPtr context);
    private void TransmitTest(IntPtr device,TxTestPlan plan,byte[]? packet=null)
    {
        TransmitAuthorization.Read(Environment.GetEnvironmentVariable(TransmitAuthorization.Variable),plan);plan.Validate();if(packet!=null&&(packet.Length==0||packet.Length%2!=0||packet.Length>TxTestPlan.Rate*plan.Seconds*2))throw new ArgumentException("Invalid packet waveform length");long total=packet?.Length??(long)TxTestPlan.Rate*plan.Seconds*2;var tone=TxTestPlan.Tone();int stopped=0;long sent=0;int position=0;string? error=null;
        _=Task.Run(()=>{try{while(Console.ReadLine() is {} line){if(line=="STOP")break;}}finally{Interlocked.Exchange(ref stopped,1);}});
        var clock=Stopwatch.StartNew();
        Callback callback=pointer=>
        {
            try
            {
                if(Volatile.Read(ref stopped)!=0||clock.Elapsed.TotalSeconds>plan.Seconds+1||sent>=total)return -1;
                var transfer=Marshal.PtrToStructure<Transfer>(pointer);
                if(transfer.BufferLength<=0||transfer.BufferLength>1048576||transfer.BufferLength%2!=0)throw new IOException("Invalid TX buffer length");
                int length=(int)Math.Min(transfer.BufferLength,total-sent);
                byte[] data=new byte[transfer.BufferLength];
                if(packet!=null)Buffer.BlockCopy(packet,(int)sent,data,0,length);else for(int i=0;i<length;i++){data[i]=tone[position++];if(position==tone.Length)position=0;}
                Marshal.Copy(data,0,transfer.Buffer,data.Length);Marshal.WriteInt32(pointer,20,length);sent+=length;return 0;
            }
            catch(Exception ex){error=ex.Message;Interlocked.Exchange(ref stopped,1);return -1;}
        };
        Check(Get<SetByte>("set_antenna_enable")(device,0),"Disable antenna power");
        Check(Get<SetByte>("set_amp_enable")(device,0),"Disable RF amplifier");
        Check(Get<SetRate>("set_sample_rate")(device,TxTestPlan.Rate),"Set TX sample rate");
        Check(Get<SetUInt>("set_baseband_filter_bandwidth")(device,1750000),"Set TX filter");
        Check(Get<SetFrequency>("set_freq")(device,(ulong)plan.Frequency),"Set TX frequency");
        Check(Get<SetUInt>("set_txvga_gain")(device,(uint)plan.Gain),"Set TX gain");
        int flushed=0;FlushCallback flushCallback=(_,success)=>Interlocked.Exchange(ref flushed,success!=0?1:-1);
        if(packet!=null)Check(Get<EnableFlush>("enable_tx_flush")(device,flushCallback,IntPtr.Zero),"Enable complete packet drain");
        bool started=false;
        try
        {
            TransmitAuthorization.Read(Environment.GetEnvironmentVariable(TransmitAuthorization.Variable),plan);clock.Restart();Check(Get<StartRx>("start_tx")(device,callback,IntPtr.Zero),"Start test transmission");started=true;
            Console.WriteLine("TX_STARTED");Console.Out.Flush();
            while(Volatile.Read(ref stopped)==0&&clock.Elapsed.TotalSeconds<plan.Seconds+1&&(packet!=null?Volatile.Read(ref flushed)==0:Get<DeviceAction>("is_streaming")(device)==1))Thread.Sleep(10);
        }
        finally{if(started)Check(Get<DeviceAction>("stop_tx")(device),"Stop test transmission");GC.KeepAlive(callback);GC.KeepAlive(flushCallback);}
        if(error!=null)throw new IOException(error);
        if(packet!=null&&Volatile.Read(ref stopped)==0&&Volatile.Read(ref flushed)!=1)throw new IOException("Packet drain was not acknowledged; delivery unconfirmed.");
        Console.WriteLine("TX_STOPPED");Console.Out.Flush();
    }
}
