using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
namespace MRS;
internal sealed partial class HackRfNative
{
 private void TransmitWeak(IntPtr device,WeakTxPlan plan,string identity)
 {
  WeakTxAuthorization.Read(Environment.GetEnvironmentVariable(WeakTxAuthorization.Variable),plan,identity);byte[] tones=WeakWaveform.Encode(plan);var iq=new WeakIqSource(plan,tones);const int rate=WeakIqSource.Rate;int stopped=0,flushed=0;string? error=null;long clockStamp=Stopwatch.GetTimestamp();DateTimeOffset clockUtc=DateTimeOffset.UtcNow.AddSeconds(plan.ClockOffset);
  _=Task.Run(()=>{try{while(Console.ReadLine() is {} s)if(s=="STOP")break;}finally{Interlocked.Exchange(ref stopped,1);}});
  Check(Get<SetByte>("set_antenna_enable")(device,0),"Disable antenna power");Check(Get<SetByte>("set_amp_enable")(device,0),"Disable RF amplifier");Check(Get<SetRate>("set_sample_rate")(device,rate),"Set digital TX rate");Check(Get<SetUInt>("set_baseband_filter_bandwidth")(device,1750000),"Set digital TX filter");Check(Get<SetFrequency>("set_freq")(device,(ulong)plan.DialHz),"Set digital USB dial");Check(Get<SetUInt>("set_txvga_gain")(device,(uint)plan.Gain),"Set digital TX gain");
  DateTimeOffset Utc()=>clockUtc+Stopwatch.GetElapsedTime(clockStamp);Console.WriteLine("TX_READY");Console.Out.Flush();
  while(Utc()<plan.SlotUtc){if(stopped!=0)throw new OperationCanceledException("TX cancelled before slot");if(Math.Abs((DateTimeOffset.UtcNow.AddSeconds(plan.ClockOffset)-Utc()).TotalSeconds)>.1)throw new IOException("Clock changed before TX slot");Thread.Sleep(1);}
  if((Utc()-plan.SlotUtc).TotalMilliseconds>80)throw new IOException("Missed digital slot; no transmission started");WeakTxAuthorization.Read(Environment.GetEnvironmentVariable(WeakTxAuthorization.Variable),plan,identity);if(Volatile.Read(ref stopped)!=0)throw new OperationCanceledException("TX cancelled before RF start");
  Callback callback=pointer=>{try{if(stopped!=0||iq.Position>=iq.Samples)return -1;var t=Marshal.PtrToStructure<Transfer>(pointer);if(t.BufferLength<=0||t.BufferLength>1048576||t.BufferLength%2!=0)throw new IOException("Invalid digital TX transfer");var data=new byte[t.BufferLength];int bytes=iq.Read(data);Marshal.Copy(data,0,t.Buffer,data.Length);Marshal.WriteInt32(pointer,20,bytes);return 0;}catch(Exception ex){error=ex.Message;Interlocked.Exchange(ref stopped,1);return -1;}};
  FlushCallback flush=(_,ok)=>Interlocked.Exchange(ref flushed,ok!=0?1:-1);Check(Get<EnableFlush>("enable_tx_flush")(device,flush,IntPtr.Zero),"Enable digital drain");bool started=false;var duration=Stopwatch.StartNew();try{Check(Get<StartRx>("start_tx")(device,callback,IntPtr.Zero),"Start digital TX");started=true;Console.WriteLine("TX_STARTED");Console.Out.Flush();while(stopped==0&&flushed==0&&duration.Elapsed.TotalSeconds<plan.Duration+3){if(Math.Abs((DateTimeOffset.UtcNow.AddSeconds(plan.ClockOffset)-Utc()).TotalSeconds)>.1){error="Clock changed during TX";break;}Thread.Sleep(5);}}finally{if(started)Check(Get<DeviceAction>("stop_tx")(device),"Stop digital TX");GC.KeepAlive(callback);GC.KeepAlive(flush);Console.WriteLine("TX_STOPPED");Console.Out.Flush();}if(error!=null)throw new IOException(error);if(stopped==0&&flushed!=1)throw new IOException("Digital TX did not drain completely; RF delivery unconfirmed");
 }
}
