using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record WeakDecode(string Message,string Sender,string Destination,string Grid,bool Cq,bool SenderResolved,double? Snr,double? Dt,double? AudioHz,long? Slot,long Epoch,DateTimeOffset Utc)
{
 private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PacketObservation,WeakDecode> cache=new();
 internal static bool IsGrid(string value)=>!string.IsNullOrEmpty(value)&&value!="RR73"&&Regex.IsMatch(value,@"^[A-R]{2}[0-9]{2}(?:[A-X]{2})?$",RegexOptions.IgnoreCase);
 internal static bool IsCall(string value)=>!string.IsNullOrEmpty(value)&&!IsGrid(value)&&Regex.IsMatch(value,@"^(?:[A-Z0-9]{1,4}/)?[A-Z0-9]{1,4}[0-9][A-Z]{1,4}(?:/[A-Z0-9]{1,4})?$",RegexOptions.IgnoreCase);
 internal static string Normalize(string value,bool baseCall=false){value=(value??"").Trim().ToUpperInvariant();if(!baseCall)return value;return value.Split('/').FirstOrDefault(IsCall)??value;}
 internal static WeakDecode From(PacketObservation p)=>cache.GetValue(p,Parse);
 private static WeakDecode Parse(PacketObservation p)
 {
  string message=p.Information,from="",to="",grid="";bool cq=false;double? snr=null,dt=null,audio=null;long? slot=null;long epoch=0;
  try{using var doc=JsonDocument.Parse(p.DecoderJson??"{}");var j=doc.RootElement;string S(string k)=>j.TryGetProperty(k,out var v)?v.ToString():"";double? D(string k)=>j.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number&&v.TryGetDouble(out double n)&&double.IsFinite(n)?n:null;
   message=S("text");if(message.Length==0)message=p.Information.Split(" · ")[0];snr=D("snr_db");dt=D(p.Protocol=="WSPR"?"dt":"dt_error");audio=D("audio_hz");if(j.TryGetProperty("mrs_slot",out var sl)&&sl.TryGetInt64(out long v))slot=v;if(j.TryGetProperty("mrs_epoch",out var ep)&&ep.TryGetInt64(out long ev))epoch=ev;
   if(p.Protocol=="WSPR"){from=S("call").ToUpperInvariant();grid=S("grid").ToUpperInvariant();if(!IsGrid(grid))grid="";}
  }catch(JsonException){}
  if(p.Protocol is "FT8" or "FT4"){
   var words=message.Trim().ToUpperInvariant().Split(' ',StringSplitOptions.RemoveEmptyEntries);cq=words.Length>0&&words[0]=="CQ";
   bool Address(string s)=>IsCall(s)||s.StartsWith('<');
   if(cq){int at=Array.FindIndex(words,1,Math.Max(0,words.Length-1),IsCall);if(at>=1){from=words[at];if(at+1<words.Length&&IsGrid(words[at+1]))grid=words[at+1];}}
   else if(words.Length>=2&&Address(words[0])&&Address(words[1])){to=words[0];from=words[1];if(words.Length>2&&IsGrid(words[2]))grid=words[2];else if(words.Length>3&&words[2]=="R"&&IsGrid(words[3]))grid=words[3];}
  }
  double period=p.Protocol=="FT4"?7.5:p.Protocol=="WSPR"?120:15;DateTimeOffset utc=p.ObservedUtc;if(slot is long index){double seconds=index*period;if(seconds>=946684800&&seconds<4102444800)utc=DateTimeOffset.UnixEpoch.AddSeconds(seconds);}
  return new(message,from,to,grid,cq,IsCall(from),snr,dt,audio,slot,epoch,utc);
 }
}
internal sealed class WeakRule:System.ComponentModel.INotifyPropertyChanged
{
 public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;private string color="#FFE29A",callsign="";
 public string Callsign{get=>callsign;set{callsign=value;PropertyChanged?.Invoke(this,new(nameof(Callsign)));}}public string Match{get;set;}="Sender";public bool BaseCall{get;set;}public bool Watch{get;set;}=true;public bool Ignore{get;set;}public string Color{get=>color;set{color=value;PropertyChanged?.Invoke(this,new(nameof(Color)));}}
 internal bool Matches(WeakDecode d){string call=WeakDecode.Normalize(Callsign,BaseCall);return (Match is "Sender" or "Either"&&WeakDecode.Normalize(d.Sender,BaseCall)==call)||(Match is "Destination" or "Either"&&WeakDecode.Normalize(d.Destination,BaseCall)==call);}
}
internal sealed class WeakPreferences
{
 public bool LocalTime{get;set;}public int ClearCycles{get;set;}public bool HighlightCq{get;set;}=true;public string CqColor{get;set;}="#B8EDBE";public bool ShowIgnored{get;set;}public bool ExcludeIgnoredReports{get;set;}=true;public List<WeakRule> Rules{get;set;}=[];
 internal bool Ignored(WeakDecode d)=>Rules.Any(r=>r.Ignore&&r.Matches(d));
 internal string? Highlight(WeakDecode d)=>Rules.FirstOrDefault(r=>r.Watch&&!r.Ignore&&r.Matches(d))?.Color??(HighlightCq&&d.Cq?CqColor:null);
}
internal sealed class WeakStation
{
 public string Callsign{get;set;}="";public string Source{get;set;}="";public string Mode{get;set;}="";public long? DialHz{get;set;}public string Grid{get;set;}="";public DateTimeOffset FirstSeen{get;set;}public DateTimeOffset LastSeen{get;set;}public int Count{get;set;}public string Message{get;set;}="";public double? AudioHz{get;set;}public bool Replay{get;set;}
}
internal sealed class WeakTracking
{
 internal readonly System.Collections.ObjectModel.ObservableCollection<WeakStation> Stations=[];
 internal void Observe(PacketObservation p){var d=WeakDecode.From(p);if(!d.SenderResolved)return;var row=Stations.FirstOrDefault(s=>s.Callsign==d.Sender&&s.Source==p.Source&&s.Mode==p.Protocol&&s.DialHz==p.FrequencyHz&&s.Replay==p.Replay);if(row==null){if(Stations.Count>=2000)Stations.Remove(Stations.OrderBy(s=>s.LastSeen).First());row=new(){Callsign=d.Sender,Source=p.Source,Mode=p.Protocol,DialHz=p.FrequencyHz,FirstSeen=d.Utc,Replay=p.Replay};Stations.Add(row);}row.LastSeen=d.Utc;row.Count++;row.Message=d.Message;row.AudioHz=d.AudioHz;if(d.Grid.Length>0)row.Grid=d.Grid;}
}
