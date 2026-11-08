using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;

internal sealed record NistReading(DateTimeOffset ReceivedUtc,double OffsetSeconds,double DelaySeconds,double UncertaintySeconds,long Stamp)
{
 internal double Age=>Stopwatch.GetElapsedTime(Stamp).TotalSeconds;
 internal double CurrentOffset=>(ReceivedUtc.AddSeconds(OffsetSeconds+Age)-DateTimeOffset.UtcNow).TotalSeconds;
 internal bool Fresh=>Age<3600;
}
internal static class NistTime
{
 private static readonly object gate=new();private static Task<NistReading>? pending;private static long attempted;
 internal static NistReading? Reading {get;private set;}
 internal static string Status {get;private set;}="Not checked";
 internal static Task<NistReading> CheckAsync()
 {
  lock(gate){if(pending is {IsCompleted:false})return pending;if(attempted!=0&&Stopwatch.GetElapsedTime(attempted).TotalSeconds<64)throw new IOException("NIST request cooldown: wait at least 64 seconds between checks.");attempted=Stopwatch.GetTimestamp();Status="Checking time.nist.gov…";return pending=QueryAsync();}
 }
 private static async Task<NistReading> QueryAsync()
 {
  try{using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(6));var addresses=await Dns.GetHostAddressesAsync("time.nist.gov",timeout.Token);var address=addresses.FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork)??throw new IOException("No NIST IPv4 address returned");using var udp=new UdpClient(AddressFamily.InterNetwork);udp.Connect(address,123);byte[] request=new byte[48];request[0]=0x23;var t1=DateTimeOffset.UtcNow;WriteTimestamp(request.AsSpan(40),t1);long stamp=Stopwatch.GetTimestamp();await udp.SendAsync(request,timeout.Token);var response=await udp.ReceiveAsync(timeout.Token);var t4=t1+Stopwatch.GetElapsedTime(stamp);var answer=Parse(response.Buffer,request,t1,t4);Reading=answer;Status="NIST reference available";return answer;}
  catch(Exception e){Status="NIST unavailable: "+(e is OperationCanceledException?"UDP/123 timeout":e.Message);throw new IOException(Status,e);}
 }
 internal static void WriteTimestamp(Span<byte> b,DateTimeOffset time){double seconds=(time-DateTimeOffset.UnixEpoch).TotalSeconds+2208988800.0;ulong whole=(ulong)Math.Floor(seconds);BinaryPrimitives.WriteUInt32BigEndian(b,(uint)(whole&uint.MaxValue));BinaryPrimitives.WriteUInt32BigEndian(b[4..],(uint)((seconds-Math.Floor(seconds))*4294967296.0));}
 private static DateTimeOffset Timestamp(ReadOnlySpan<byte> b,DateTimeOffset near){uint seconds=BinaryPrimitives.ReadUInt32BigEndian(b),frac=BinaryPrimitives.ReadUInt32BigEndian(b[4..]);double expected=(near-DateTimeOffset.UnixEpoch).TotalSeconds+2208988800.0;double era=Math.Round((expected-seconds)/4294967296.0);return DateTimeOffset.UnixEpoch.AddSeconds(seconds+era*4294967296.0+frac/4294967296.0-2208988800.0);}
 internal static NistReading Parse(byte[] bytes,byte[] request,DateTimeOffset sent,DateTimeOffset received)
 {
  if(bytes.Length<48||request.Length<48||(bytes[0]&7)!=4||((bytes[0]>>3)&7) is not (3 or 4)||(bytes[0]>>6)==3||bytes[1] is 0 or >15)throw new IOException("Invalid, unsynchronized or rate-limited NTP response");
  if(!bytes.AsSpan(24,8).SequenceEqual(request.AsSpan(40,8))||bytes.AsSpan(32,8).IndexOfAnyExcept((byte)0)<0||bytes.AsSpan(40,8).IndexOfAnyExcept((byte)0)<0)throw new IOException("NTP response does not match this request");
  var t2=Timestamp(bytes.AsSpan(32,8),sent);var t3=Timestamp(bytes.AsSpan(40,8),sent);double delay=(received-sent).TotalSeconds-(t3-t2).TotalSeconds,offset=((t2-sent).TotalSeconds+(t3-received).TotalSeconds)/2;
  double dispersion=BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8,4))/65536.0,rootDelay=Math.Abs(BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4,4))/65536.0);
  if(t3<t2||delay<-.005||delay>1||dispersion>1||rootDelay>1||!double.IsFinite(offset)||Math.Abs(offset)>86400)throw new IOException("NTP sample delay/dispersion or clock difference is too large");
  return new(received,offset,Math.Max(0,delay),Math.Max(0,delay)/2+dispersion+rootDelay/2,Stopwatch.GetTimestamp());
 }
}

internal sealed record RxTimingEstimate(double Correction,int Samples,int Stations,int Slots,double Spread,string Confidence);
internal sealed class RxTimingTracker
{
 private sealed record Point(string Station,long Slot,double Correction,long At);
 private readonly List<Point> points=[];private string? source;private string? mode;
 internal void Reset(){points.Clear();source=mode=null;}
 internal void Observe(PacketObservation row)
 {
  if(row.Protocol is not ("FT8" or "FT4")||!row.IsValidated||row.DecoderJson==null)return;
  if(source!=row.SourceKey||mode!=row.Protocol){Reset();source=row.SourceKey;mode=row.Protocol;}
  using var doc=JsonDocument.Parse(row.DecoderJson);var j=doc.RootElement;
  if(!j.TryGetProperty("dt_error",out var dt)||!j.TryGetProperty("mrs_slot",out var slot)||!j.TryGetProperty("mrs_correction",out var correction))return;
  double error=dt.GetDouble(),value=correction.GetDouble()-error;if(!double.IsFinite(value)||Math.Abs(error)>2||Math.Abs(value)>86410)return;
  string message=j.GetProperty("text").GetString()??"";
  var calls=Regex.Matches(message,@"(?<![A-Z0-9/<])[A-Z0-9]{1,4}[0-9][A-Z0-9]{1,6}(?:/[A-Z0-9]+)?(?![A-Z0-9/>])").Select(m=>m.Value).Where(x=>!Regex.IsMatch(x,@"^[A-R]{2}[0-9]{2}(?:[A-X]{2})?$"));
  string? station=calls.LastOrDefault();if(station==null)return;long number=slot.GetInt64();
  points.RemoveAll(p=>Stopwatch.GetElapsedTime(p.At).TotalMinutes>5);if(points.Any(p=>p.Station==station&&p.Slot==number))return;
  points.Add(new(station,number,value,Stopwatch.GetTimestamp()));if(points.Count>120)points.RemoveAt(0);
 }
 private static double Median(IEnumerable<double> values){var a=values.Order().ToArray();return a.Length%2==0?(a[a.Length/2-1]+a[a.Length/2])/2:a[a.Length/2];}
 internal RxTimingEstimate? Estimate()
 {
  points.RemoveAll(p=>Stopwatch.GetElapsedTime(p.At).TotalMinutes>5);if(points.Count==0)return null;
  // Weight stations equally so one frequent caller cannot dominate the reference.
  var stations=points.GroupBy(p=>p.Station).Select(g=>(Name:g.Key,Value:Median(g.Select(p=>p.Correction)))).ToArray();double center=Median(stations.Select(p=>p.Value));var accepted=stations.Where(p=>Math.Abs(p.Value-center)<=.15).ToArray();
  var names=accepted.Select(p=>p.Name).ToHashSet();var evidence=points.Where(p=>names.Contains(p.Station)&&Math.Abs(p.Correction-center)<=.15).ToArray();if(evidence.Length==0)return null;
  double result=Median(accepted.Select(p=>p.Value)),spread=evidence.Max(p=>p.Correction)-evidence.Min(p=>p.Correction);int slots=evidence.Select(p=>p.Slot).Distinct().Count();bool ready=accepted.Length>=3&&evidence.Length>=6&&slots>=2&&spread<=.20;
  return new(result,evidence.Length,accepted.Length,slots,spread,ready?"Ready — station consensus":"Collecting — need 3 stations / 6 decodes / 2 slots");
 }
}
