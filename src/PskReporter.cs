using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace MRS;
internal sealed record PskSettings(string Callsign="",string Grid="",string Rig="",string Antenna="");
internal sealed record PskSpot(string Callsign,string Grid,string Mode,long Frequency,DateTimeOffset Utc,int? Snr,string Key);
internal interface IPskTransport:IDisposable{Task Send(byte[] bytes,CancellationToken token);}
internal sealed class PskUdpTransport:IPskTransport
{
 private UdpClient? client;
 public async Task Send(byte[] bytes,CancellationToken token){var addresses=await Dns.GetHostAddressesAsync("report.pskreporter.info",token);var address=addresses.FirstOrDefault(a=>a.AddressFamily==AddressFamily.InterNetwork)??throw new IOException("No IPv4 reporting endpoint");token.ThrowIfCancellationRequested();client??=new UdpClient(AddressFamily.InterNetwork);await client.SendAsync(bytes.AsMemory(),new IPEndPoint(address,4739),token);}
 public void Dispose()=>client?.Dispose();
}
internal static class PskWire
{
 internal const ushort Receiver=0x9992,Sender=0x9993,SenderSnr=0x9994;
 private static void U16(Stream s,int v){Span<byte>b=stackalloc byte[2];BinaryPrimitives.WriteUInt16BigEndian(b,checked((ushort)v));s.Write(b);}
 private static void U32(Stream s,uint v){Span<byte>b=stackalloc byte[4];BinaryPrimitives.WriteUInt32BigEndian(b,v);s.Write(b);}
 private static void String(Stream s,string value){byte[] b=Encoding.UTF8.GetBytes(value);if(b.Length>254)throw new IOException("Reporting field exceeds 254 bytes");s.WriteByte((byte)b.Length);s.Write(b);}
 private static void Set(Stream s,int id,Action<MemoryStream> fill){using var part=new MemoryStream();fill(part);while((part.Length+4)%4!=0)part.WriteByte(0);U16(s,id);U16(s,(int)part.Length+4);part.Position=0;part.CopyTo(s);}
 private static void Field(Stream s,int id,int len,bool enterprise=true){U16(s,id|(enterprise?0x8000:0));U16(s,len);if(enterprise)U32(s,30351);}
 private static void Template(Stream s,bool snr){Set(s,2,t=>{U16(t,snr?SenderSnr:Sender);U16(t,snr?7:6);Field(t,1,65535);Field(t,5,5);Field(t,10,65535);Field(t,11,1);Field(t,3,65535);Field(t,150,4,false);if(snr)Field(t,6,1);});}
 internal static byte[] Packet(PskSettings settings,IReadOnlyList<PskSpot> spots,uint sequence,uint domain,DateTimeOffset now,bool templates,string software)
 {
  using var body=new MemoryStream();if(templates){Set(body,3,t=>{U16(t,Receiver);U16(t,5);U16(t,1);foreach(int field in new[]{2,4,8,9,13})Field(t,field,65535);});Template(body,false);Template(body,true);}
  Set(body,Receiver,t=>{String(t,settings.Callsign);String(t,settings.Grid);String(t,software);String(t,settings.Antenna);String(t,settings.Rig);});
  foreach(bool hasSnr in new[]{false,true}){var group=spots.Where(p=>p.Snr.HasValue==hasSnr).ToArray();if(group.Length==0)continue;Set(body,hasSnr?SenderSnr:Sender,t=>{foreach(var p in group){String(t,p.Callsign);for(int shift=32;shift>=0;shift-=8)t.WriteByte((byte)(p.Frequency>>shift));String(t,p.Mode);t.WriteByte(1);String(t,p.Grid);U32(t,checked((uint)p.Utc.ToUnixTimeSeconds()));if(hasSnr)t.WriteByte(unchecked((byte)(sbyte)Math.Clamp(p.Snr!.Value,-127,127)));}});}
  using var packet=new MemoryStream();U16(packet,10);U16(packet,checked((int)body.Length+16));U32(packet,checked((uint)now.ToUnixTimeSeconds()));U32(packet,sequence);U32(packet,domain);body.Position=0;body.CopyTo(packet);return packet.ToArray();
 }
}
internal sealed class PskReporter:IDisposable
{
 private readonly Func<DateTimeOffset> utcNow;private readonly IPskTransport transport;private readonly bool allowNetwork;private readonly Dictionary<string,PskSpot> pending=[];private readonly Dictionary<string,DateTimeOffset> lastSent=[];private CancellationTokenSource cancel=new();private bool enabled,busy;private int generation,packets;private uint sequence;private readonly uint domain=(uint)Random.Shared.NextInt64(1,uint.MaxValue);private long due;private DateTimeOffset templateAt;internal PskSettings Settings{get;private set;}=new();internal string Status{get;private set;}="Reporting off";internal int Queued=>pending.Count;internal int Sent{get;private set;}internal bool Enabled=>enabled;
 internal PskReporter(IPskTransport? transport=null,bool allowNetwork=true,Func<DateTimeOffset>? utcNow=null){this.utcNow=utcNow??(()=>DateTimeOffset.UtcNow);this.transport=transport??new PskUdpTransport();this.allowNetwork=allowNetwork||transport!=null;}
 internal void Enable(PskSettings settings){if(!allowNetwork)throw new IOException("Live reporting disabled in diagnostic workspaces");if(!WeakDecode.IsCall(settings.Callsign)||!WeakDecode.IsGrid(settings.Grid))throw new IOException("Enter your receiver callsign and 4/6-character grid.");foreach(string v in new[]{settings.Callsign,settings.Grid,settings.Rig,settings.Antenna})if(Encoding.UTF8.GetByteCount(v)>254)throw new IOException("Reporting field is too long");if(Settings.Callsign!=settings.Callsign.ToUpperInvariant()||Settings.Grid!=settings.Grid.ToUpperInvariant())lastSent.Clear();Disable();Settings=settings with{Callsign=settings.Callsign.ToUpperInvariant(),Grid=settings.Grid.ToUpperInvariant()};enabled=true;due=Stopwatch.GetTimestamp()+(long)((300+Random.Shared.Next(0,20))*Stopwatch.Frequency);Status="Enabled · waiting for valid live receptions";}
 internal void Disable(){enabled=false;generation++;cancel.Cancel();cancel.Dispose();cancel=new();pending.Clear();packets=0;sequence=0;templateAt=default;Status="Reporting off";}
 internal bool Queue(PacketObservation p,bool ignored,bool excludeIgnored,DateTimeOffset now)
 {
  if(!enabled||p.Replay||!p.IsValidated||p.Protocol is not ("FT8" or "FT4")||p.FrequencyHz is not long dial||dial<=0||ignored&&excludeIgnored)return false;var d=WeakDecode.From(p);if(!d.SenderResolved||d.AudioHz is not double audio||audio<100||audio>4000||d.Slot==null||d.Utc<now.AddMinutes(-10)||d.Utc>now.AddMinutes(2))return false;
  long hz=dial+(long)Math.Round(audio);if(hz<=0||hz>6000000000)return false;string key=d.Sender+"|"+p.Protocol+"|"+Band(dial);if(lastSent.TryGetValue(key,out var last)&&now-last<TimeSpan.FromMinutes(5))return false;if(pending.Count>=1000&&!pending.ContainsKey(key)){Status="Queue full · newer stations omitted";return false;}pending[key]=new(d.Sender,d.Grid.Length>0?d.Grid:pending.GetValueOrDefault(key)?.Grid??"",p.Protocol,hz,d.Utc,d.Snr is double snr?(int)Math.Round(snr):null,key);return true;
 }
 private static long Band(long hz)=>hz switch{<2000000=>1,<4000000=>3,<6000000=>5,<8000000=>7,<11000000=>10,<15000000=>14,<19000000=>18,<22000000=>21,<26000000=>24,<30000000=>28,<54000000=>50,<71000000=>70,<148000000=>144,<225000000=>222,<450000000=>432,<1300000000=>1296,_=>hz/1000000};
 internal void Tick(){if(enabled&&!busy&&pending.Count>0&&(Stopwatch.GetTimestamp()>=due||pending.Count>=24))_=Flush(utcNow());}
 internal async Task Flush(DateTimeOffset now)
 {
  if(!enabled||busy||pending.Count==0)return;busy=true;int run=generation;var batch=new List<PskSpot>();byte[] bytes=[];bool templates=packets<3||now-templateAt>=TimeSpan.FromHours(1);string software="M.R.S. "+System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(PskReporter).Assembly)?.InformationalVersion.Split('+')[0];
  try{foreach(var spot in pending.Values.Where(s=>s.Utc>=now.AddMinutes(-10)).ToArray()){var next=batch.Append(spot).ToArray();var candidate=PskWire.Packet(Settings,next,sequence,domain,now,templates,software);if(candidate.Length>1200&&batch.Count>0)break;batch.Add(spot);bytes=candidate;}
  if(batch.Count==0){pending.Clear();Status="Stale queued reports discarded";return;}await transport.Send(bytes,cancel.Token);if(run!=generation)return;foreach(var spot in batch){if(pending.GetValueOrDefault(spot.Key)==spot)pending.Remove(spot.Key);lastSent[spot.Key]=now;}foreach(var key in lastSent.Where(x=>now-x.Value>TimeSpan.FromHours(1)).Select(x=>x.Key).ToArray())lastSent.Remove(key);sequence+=(uint)batch.Count;packets++;if(templates)templateAt=now;Sent+=batch.Count;Status=$"UDP sent {Sent} reports · delivery unconfirmed";}
  catch(OperationCanceledException){}catch(Exception e){if(run==generation)Status="Report failed; retained for retry: "+e.Message;}
  finally{busy=false;if(run==generation)due=Stopwatch.GetTimestamp()+(long)((300+Random.Shared.Next(0,20))*Stopwatch.Frequency);}
 }
 public void Dispose(){Disable();cancel.Dispose();transport.Dispose();}
}
