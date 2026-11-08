using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
namespace MRS;
internal sealed class FoxGps : IDisposable
{
 private readonly object sync=new();private readonly FoxNmea parser=new();private CancellationTokenSource? cancel;private Task? worker;private string state="GPS disconnected";private long bytes;internal FoxFix? Fix{get{lock(sync)return parser.Fix;}}internal string Status{get{lock(sync)return state+$" · bytes {bytes} · valid NMEA {parser.ValidLines} · checksum errors {parser.BadChecksum}"+(parser.Fix==null?" · "+parser.Status:"");}}
 internal static string[] Ports(){try{using var key=Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");return key?.GetValueNames().Select(n=>key.GetValue(n)?.ToString()??"").Where(p=>System.Text.RegularExpressions.Regex.IsMatch(p,@"^COM[0-9]+$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)).Distinct().OrderBy(p=>int.Parse(p[3..])).ToArray()??[];}catch{return [];}}
 internal async Task Connect(string port,int baud){if(!System.Text.RegularExpressions.Regex.IsMatch(port,@"^COM[0-9]{1,4}$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)||!new[]{4800,9600,19200,38400,57600,115200}.Contains(baud))throw new IOException("Choose a COM port and supported baud rate.");await Disconnect();var c=new CancellationTokenSource();cancel=c;var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);lock(sync){parser.Clear("Waiting for GPS fix");state=port+" opening";bytes=0;}worker=Task.Run(()=>Read(port.ToUpperInvariant(),baud,c.Token,ready));await ready.Task;}
 internal async Task Disconnect(){var old=cancel;cancel=null;old?.Cancel();if(worker!=null){try{await worker.WaitAsync(TimeSpan.FromSeconds(2));}catch{}worker=null;}old?.Dispose();lock(sync){parser.Clear();state="GPS disconnected";}}
 private void Read(string port,int baud,CancellationToken token,TaskCompletionSource ready)
 {
  try{using var handle=CreateFile(@"\\.\"+port,0xC0000000,0,IntPtr.Zero,3,0,IntPtr.Zero);if(handle.IsInvalid)throw new Win32Exception(Marshal.GetLastWin32Error(),"Cannot open "+port+". Close other GPS apps using this port.");var dcb=new Dcb{Length=(uint)Marshal.SizeOf<Dcb>()};Check(GetCommState(handle,ref dcb),"Read GPS port settings");dcb.Baud=(uint)baud;dcb.Flags=0x1011;dcb.ByteSize=8;dcb.Parity=0;dcb.StopBits=0;Check(SetCommState(handle,ref dcb),"Configure GPS 8N1");var timeout=new Timeouts{Interval=uint.MaxValue,ReadConstant=200,WriteConstant=200};Check(SetCommTimeouts(handle,ref timeout),"Set GPS read timeout");PurgeComm(handle,8);lock(sync)state=port+$" connected at {baud}";ready.TrySetResult();var line=new StringBuilder();byte[] buffer=new byte[256];while(!token.IsCancellationRequested){Check(ReadFile(handle,buffer,(uint)buffer.Length,out uint count,IntPtr.Zero),"Read GPS");lock(sync){bytes+=count;for(int i=0;i<count;i++){byte b=buffer[i];if(b==10){parser.Feed(line.ToString(),DateTimeOffset.UtcNow,port+" NMEA");line.Clear();}else if(b!=13){if(line.Length>=512)line.Clear();if(b is >=32 and <=126)line.Append((char)b);else line.Clear();}}}}}
  catch(Exception ex){lock(sync){parser.Clear("No valid GPS fix");state="GPS error: "+ex.Message;}ready.TrySetException(ex);}
 }
 private static void Check(bool result,string action){if(!result)throw new Win32Exception(Marshal.GetLastWin32Error(),action);}
 public void Dispose(){cancel?.Cancel();lock(sync){parser.Clear();state="GPS disconnected";}}
 [StructLayout(LayoutKind.Sequential)]private struct Dcb{internal uint Length,Baud,Flags;internal ushort Reserved,XonLim,XoffLim;internal byte ByteSize,Parity,StopBits;internal sbyte Xon,Xoff,Error,Eof,Event;internal ushort Reserved1;}
 [StructLayout(LayoutKind.Sequential)]private struct Timeouts{internal uint Interval,ReadMultiplier,ReadConstant,WriteMultiplier,WriteConstant;}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern SafeFileHandle CreateFile(string name,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
 [DllImport("kernel32.dll",SetLastError=true)]private static extern bool GetCommState(SafeFileHandle h,ref Dcb d);
 [DllImport("kernel32.dll",SetLastError=true)]private static extern bool SetCommState(SafeFileHandle h,ref Dcb d);
 [DllImport("kernel32.dll",SetLastError=true)]private static extern bool SetCommTimeouts(SafeFileHandle h,ref Timeouts t);
 [DllImport("kernel32.dll",SetLastError=true)]private static extern bool PurgeComm(SafeFileHandle h,uint flags);
 [DllImport("kernel32.dll",SetLastError=true)]private static extern bool ReadFile(SafeFileHandle h,byte[] buffer,uint count,out uint read,IntPtr overlapped);
}
