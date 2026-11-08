using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
namespace MRS;

internal sealed record AprsInternetSettings(string Callsign,string Passcode,string Host="rotate.aprs2.net",int Port=14580)
{
 internal void Validate(){if(!Regex.IsMatch(Callsign,@"^[A-Z0-9]{3,6}(-([1-9]|1[0-5]))?$")||!Callsign.Any(char.IsDigit)||!Callsign.Any(char.IsLetter))throw new IOException("Enter your iGate callsign and optional SSID (0–15).");if(!Regex.IsMatch(Passcode,@"^[0-9]{1,5}$"))throw new IOException("Enter your APRS-IS passcode; an unverified login cannot upload.");if(Host.Length>253||!Regex.IsMatch(Host,@"^[a-zA-Z0-9.-]+$")||Port<1||Port>65535)throw new IOException("Invalid APRS-IS server.");}
}
internal static class AprsInternetPacket
{
 internal static string? Prepare(PacketObservation p,string gate,DateTimeOffset now)
 {
  if(p.Protocol!="AX.25"||!p.IsValidated||p.Replay||p.FrequencyHz is not >0||p.SourceKey==null||p.ObservedUtc>now.AddSeconds(2)||now-p.ObservedUtc>TimeSpan.FromSeconds(30))return null;
  byte[] raw;try{raw=Convert.FromHexString(p.RawHex);}catch{return null;}int at=0;var addresses=new List<string>();bool end=false;
  while(!end&&at+7<=raw.Length&&addresses.Count<10){var a=raw.AsSpan(at,7);if(a[..6].ToArray().Any(b=>(b&1)!=0))return null;string call=new(a[..6].ToArray().Select(b=>(char)(b>>1)).ToArray());call=call.TrimEnd();if(!Regex.IsMatch(call,@"^[A-Z0-9]{1,6}$"))return null;int ssid=(a[6]>>1)&15;string name=call+(ssid>0?"-"+ssid:"");if(addresses.Count>=2&&(a[6]&128)!=0)name+="*";addresses.Add(name);end=(a[6]&1)!=0;at+=7;}
  if(!end||addresses.Count<2||at+2>=raw.Length||raw[at]!=3||raw[at+1]!=240)return null;
  // RawHex stores the decoder's complete frame including its FCS. Do not trust
  // display summaries to reconstruct the RF payload or its repeater path.
  if(raw.Length<at+5||Ax25Parser.Fcs(raw.AsSpan(0,raw.Length-2).ToArray())!=BitConverter.ToUInt16(raw,raw.Length-2))return null;
  string body=Encoding.Latin1.GetString(raw,at+2,raw.Length-at-4);
  string header=addresses[1]+">"+addresses[0]+(addresses.Count>2?","+string.Join(',',addresses.Skip(2)):"");
  for(int depth=0;depth<4;depth++){
   if(!HeaderOkay(header)||body.Length==0||body[0]=='?'||body.Any(c=>c=='\r'||c=='\n'||c=='\0'))return null;
   if(body[0]!='}') {if(!"!=/@>:;)_T`'$<#*%[]{".Contains(body[0]))return null;string line=header+",qAO,"+gate+":"+body;return Encoding.Latin1.GetByteCount(line)+2<=512?line:null;}
   int colon=body.IndexOf(':');if(colon<2)return null;header=body[1..colon];body=body[(colon+1)..];
  }return null;
 }
 private static bool HeaderOkay(string h){var split=h.Split('>');if(split.Length!=2||!Regex.IsMatch(split[0],@"^[A-Z0-9]{1,6}(-[0-9]{1,2})?$"))return false;var tokens=split[1].Split(',');if(tokens.Length>10)return false;foreach(string token in tokens){string t=token.TrimEnd('*');if(!Regex.IsMatch(t,@"^[A-Z0-9]{1,6}(-[0-9]{1,2})?$")||t is "TCPIP" or "TCPXX" or "NOGATE" or "RFONLY"||t.StartsWith("q",StringComparison.OrdinalIgnoreCase))return false;}return true;}
 internal static string DedupKey(string line){int colon=line.IndexOf(':');return line.Split('>')[0]+">"+line.Split('>')[1].Split(',')[0].Split(':')[0]+line[colon..];}
}
internal sealed class AprsInternet : IDisposable
{
 private readonly ConcurrentQueue<(DateTimeOffset Time,string Line)> queue=new();private readonly Dictionary<string,DateTimeOffset> seen=[];private readonly object sync=new();private CancellationTokenSource? cancel;private Task? task;private volatile bool verified;private long sent;private string status="Off";internal string Status=>status;internal bool Verified=>verified;internal long Sent=>Interlocked.Read(ref sent);internal bool Enabled=>cancel!=null;private AprsInternetSettings? settings;
 internal void Enable(AprsInternetSettings s,bool allowNetwork=true){s.Validate();Disable();if(task is {IsCompleted:false})throw new IOException("Previous connection is closing; enable again in a moment.");if(!allowNetwork)throw new IOException("Network reporting is disabled in diagnostic workspaces.");settings=s;cancel=new();task=Run(s,cancel.Token);}
 internal bool Queue(PacketObservation p,DateTimeOffset now){if(!verified||settings==null)return false;string? line=AprsInternetPacket.Prepare(p,settings.Callsign,now);if(line==null)return false;lock(sync){foreach(var oldKey in seen.Where(v=>now-v.Value>TimeSpan.FromSeconds(30)).Select(v=>v.Key).ToArray())seen.Remove(oldKey);string key=AprsInternetPacket.DedupKey(line);if(seen.ContainsKey(key)||seen.Count>=1024||queue.Count>=100)return false;seen[key]=now;queue.Enqueue((now,line));return true;}}
 private async Task Run(AprsInternetSettings s,CancellationToken token){int backoff=5;while(!token.IsCancellationRequested){try{status="Connecting";using var client=new TcpClient{NoDelay=true};await client.ConnectAsync(s.Host,s.Port,token);using var stream=client.GetStream();await Write(stream,$"user {s.Callsign} pass {s.Passcode} vers MRS {typeof(AprsInternet).Assembly.GetName().Version}",token);status="Waiting for verified login";var login=DateTimeOffset.UtcNow;var input=new List<byte>();byte[] bytes=new byte[2048];Task<int>? read=null;while(!token.IsCancellationRequested){read??=stream.ReadAsync(bytes,token).AsTask();if(await Task.WhenAny(read,Task.Delay(100,token))==read){int n=await read;read=null;if(n==0)throw new IOException("Server disconnected");for(int i=0;i<n;i++){byte b=bytes[i];if(b==10){string line=Encoding.ASCII.GetString(input.ToArray()).TrimEnd('\r');input.Clear();if(line.StartsWith("# logresp ",StringComparison.OrdinalIgnoreCase)){var match=Regex.Match(line,@"^# logresp ([^, ]+) verified(?:,| |$)",RegexOptions.IgnoreCase);if(!match.Success||!match.Groups[1].Value.Equals(s.Callsign,StringComparison.OrdinalIgnoreCase))throw new IOException("APRS-IS login was not verified");verified=true;backoff=5;status="Verified · receive-only iGate";}}else{if(input.Count>=512)throw new IOException("Server line exceeded limit");input.Add(b);}}}if(!verified&&DateTimeOffset.UtcNow-login>TimeSpan.FromSeconds(15))throw new IOException("Login verification timed out");while(verified&&queue.TryDequeue(out var item)){if(DateTimeOffset.UtcNow-item.Time>TimeSpan.FromSeconds(30))continue;await Write(stream,item.Line,token);Interlocked.Increment(ref sent);}}}catch(OperationCanceledException){break;}catch(Exception ex){if(token.IsCancellationRequested)break;verified=false;while(queue.TryDequeue(out _)){}status=ex.Message+$" · retry in {backoff}s";try{await Task.Delay(TimeSpan.FromSeconds(backoff),token);}catch(OperationCanceledException){break;}backoff=Math.Min(120,backoff*2);}}verified=false;}
 private static async Task Write(Stream s,string line,CancellationToken token){byte[] bytes=Encoding.Latin1.GetBytes(line+"\r\n");await s.WriteAsync(bytes,token);await s.FlushAsync(token);}
 internal void Disable(){cancel?.Cancel();cancel=null;verified=false;settings=null;while(queue.TryDequeue(out _)){}lock(sync)seen.Clear();status="Off";}
 public void Dispose()=>Disable();
}
