using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record WeakTxPlan(string Mode,long DialHz,int AudioHz,string Message,int Gain,DateTimeOffset SlotUtc,double ClockOffset=0)
{
 internal double Period=>Mode=="FT4"?7.5:Mode=="WSPR"?120:15;
 internal double Symbol=>Mode=="FT4"?.048:Mode=="WSPR"?8192.0/12000:.16;
 internal int Symbols=>Mode=="FT4"?105:Mode=="WSPR"?162:79;
 internal double Lead=>Mode=="WSPR"?1:Mode=="FT4"?.5:.5;
 internal double Duration=>Lead+Symbol*Symbols;
 internal void Validate(){if(Mode is not ("FT8" or "FT4" or "WSPR")||Gain<0||Gain>20||AudioHz<300||AudioHz>2500||Message.Length>35||Message.Any(c=>c<32||c>126)||!double.IsFinite(ClockOffset)||Math.Abs(ClockOffset)>5)throw new IOException("Unsupported digital TX parameters.");if(Math.Abs(SlotUtc.ToUnixTimeMilliseconds()/1000.0/Period-Math.Round(SlotUtc.ToUnixTimeMilliseconds()/1000.0/Period))>.0001)throw new IOException("TX must start on a complete protocol slot.");if(Mode!="WSPR"){var words=Message.Split(' ');if(words.Length!=3||!(words[0]=="CQ"||WeakDecode.IsCall(words[0]))||!WeakDecode.IsCall(words[1])||!(Regex.IsMatch(words[2],@"^[A-R]{2}[0-9]{2}$")||words[2] is "RRR" or "RR73" or "73"||Regex.IsMatch(words[2],@"^R?[+-][0-9]{2}$"))||words[0]=="CQ"&&!Regex.IsMatch(words[2],@"^[A-R]{2}[0-9]{2}$"))throw new IOException("Use a resolved standard callsign exchange or CQ with a four-character grid. Free text / hashed calls are not enabled for TX.");}if(Mode=="WSPR"){var w=Message.Split(' ');if(w.Length!=3||!Regex.IsMatch(w[0],@"^[A-Z0-9]{1,2}[0-9][A-Z]{1,3}$")||!Regex.IsMatch(w[1],@"^[A-R]{2}[0-9]{2}$")||!int.TryParse(w[2],out int power)||power<0||power>37||!new[]{0,3,7}.Contains(power%10))throw new IOException("WSPR: standard callsign, four-character grid and measured output power 0–37 dBm (0/3/7 steps). Gain is not output power.");}}
 internal static DateTimeOffset NextSlot(DateTimeOffset now,string mode,bool odd,double lead=4){double period=mode=="FT4"?7.5:mode=="WSPR"?120:15;long index=(long)Math.Ceiling((now.ToUnixTimeMilliseconds()/1000.0+lead)/period);if(mode!="WSPR"&&(index&1)!=(odd?1:0))index++;return DateTimeOffset.UnixEpoch.AddSeconds(index*period);}
}
internal sealed record WeakTxPermit(TxDeclaration Declaration,WeakTxPlan Plan,string Identity,DateTimeOffset Expires);
internal static class WeakTxAuthorization
{
 internal const string Variable="MRS_WEAK_TX_PERMIT";
 internal static void Validate(TxDeclaration d,WeakTxPlan p,string identity){p.Validate();if(d.Country!="United States"||!new[]{"Technician","General","Advanced","Amateur Extra"}.Contains(d.LicenseClass)||!Regex.IsMatch(d.Callsign,@"^[AKNW][A-Z]?[0-9][A-Z]{1,3}$")||!d.CurrentLicense||!d.LocalRulesChecked||!d.EquipmentChecked||string.IsNullOrWhiteSpace(identity))throw new IOException("TX locked: declare a current U.S. license, local authorization and equipment compliance.");
  // Conservative contiguous data segments in US Region 2. Special allocations
  // (60 m, LF/MF, 219 MHz, microwave) deliberately need separate profiles.
  var ranges=new List<(long Low,long High)>{(50100000,54000000),(144100000,148000000),(430000000,450000000)};
  if(d.LicenseClass!="Technician")ranges.AddRange(new (long,long)[]{(1800000,2000000),(3525000,3600000),(7025000,7125000),(10100000,10150000),(14025000,14150000),(18068000,18110000),(21025000,21200000),(24890000,24930000),(28000000,28300000)});else ranges.Add((28100000,28300000));
  double low=p.DialHz+p.AudioHz-100,high=p.DialHz+p.AudioHz+(p.Mode=="FT4"?4: p.Mode=="WSPR"?4:8)/p.Symbol+100;
  if(!ranges.Any(r=>low>=r.Low&&high<=r.High)||p.Mode=="WSPR"&&p.DialHz>=30000000)throw new IOException("TX locked: emission does not fit a supported U.S. data segment for this license. WSPR preview supports HF only.");
  var words=p.Message.Split(' ');if(p.Mode=="WSPR"?words[0]!=d.Callsign:words.Length!=3||words[1]!=d.Callsign||!(words[0]=="CQ"||WeakDecode.IsCall(words[0])))throw new IOException("TX message must identify the declared station as sender.");
 }
 internal static string Issue(TxDeclaration d,WeakTxPlan p,string identity){Validate(d,p,identity);return JsonSerializer.Serialize(new WeakTxPermit(d,p,identity,DateTimeOffset.UtcNow.AddSeconds(20)));}
 internal static WeakTxPermit Read(string? raw,WeakTxPlan p,string identity){var permit=JsonSerializer.Deserialize<WeakTxPermit>(raw??"")??throw new IOException("Digital TX must be armed in M.R.S.");Validate(permit.Declaration,p,identity);if(permit.Plan!=p||permit.Identity!=identity||p.SlotUtc<DateTimeOffset.UtcNow.AddSeconds(p.ClockOffset-.1)||p.SlotUtc>DateTimeOffset.UtcNow.AddSeconds(p.ClockOffset+15)||permit.Expires<DateTimeOffset.UtcNow||permit.Expires>DateTimeOffset.UtcNow.AddSeconds(21))throw new IOException("Digital TX session expired or changed.");return permit;}
}
internal static class WeakWaveform
{
 internal static byte[] Encode(WeakTxPlan p){p.Validate();string helper=Path.Combine(AppContext.BaseDirectory,"Decoders","RadioData","mrs-weak.exe");var start=new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(string arg in new[]{"--tones",p.Mode,p.Message})start.ArgumentList.Add(arg);using var process=Process.Start(start)??throw new IOException("Cannot start digital encoder");var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(10000)){process.Kill(true);throw new IOException("Encoder timeout");}string result=output.GetAwaiter().GetResult().Trim();if(process.ExitCode!=0)throw new IOException("Message is not supported by the standard-message encoder.");var tones=result.Split(',').Select(byte.Parse).ToArray();if(tones.Length!=p.Symbols||tones.Any(t=>t>=(p.Mode=="FT8"?8:4)))throw new IOException("Invalid encoder tones");return tones;}
 internal static double[] Phase(WeakTxPlan p,byte[] tones){int size=(int)Math.Round(p.Symbol*12000),count=size*tones.Length;var increments=new double[count+size*2];double spacing=1/p.Symbol;for(int i=0;i<increments.Length;i++)increments[i]=p.AudioHz;
  if(p.Mode=="WSPR"){for(int k=0;k<count;k++)increments[k+size]=p.AudioHz+tones[k/size]*spacing;}
  else{double bt=p.Mode=="FT4"?1:2;var pulse=new double[size*3];for(int i=0;i<pulse.Length;i++){double t=(double)i/size-1.5;pulse[i]=.5*(Erf(5.336446*bt*(t+.5))-Erf(5.336446*bt*(t-.5)));}for(int s=-1;s<=tones.Length;s++){int tone=tones[Math.Clamp(s,0,tones.Length-1)];for(int i=0;i<pulse.Length;i++){int at=s*size+i-size/2;if(at>=0&&at<increments.Length)increments[at]+=spacing*tone*pulse[i];}}}
  var phase=new double[count+1];for(int i=0;i<count;i++)phase[i+1]=phase[i]+2*Math.PI*increments[i+size]/12000;return phase;
 }
 private static double Erf(double x){double sign=Math.Sign(x);x=Math.Abs(x);double t=1/(1+.3275911*x);return sign*(1-(((((1.061405429*t-1.453152027)*t)+1.421413741)*t-.284496736)*t+.254829592)*t*Math.Exp(-x*x));}
 internal static float[] Audio(WeakTxPlan p,byte[] tones){var phase=Phase(p,tones);int lead=(int)(p.Lead*12000);var audio=new float[lead+phase.Length-1];for(int i=0;i<phase.Length-1;i++)audio[i+lead]=(float)(.7*Math.Sin(phase[i])*Envelope(i,phase.Length-1,12000));return audio;}
 internal static double Envelope(double index,int count,int rate){double ramp=rate*.005;return .5*(1-Math.Cos(Math.PI*Math.Clamp(Math.Min(index,count-1-index)/ramp,0,1)));}
}
internal sealed class WeakQso
{
 internal string Call {get;}internal string Grid {get;}internal string Peer {get;private set;}="";internal string Extra {get;private set;}="";internal string Stage {get;private set;}="CQ";internal bool Complete=>Stage=="Complete";internal int Attempts {get;private set;}
 private DateTimeOffset since=DateTimeOffset.MinValue;private string report="-10";
 internal WeakQso(string call,string grid){Call=call;Grid=grid;}
 internal void Cq(){Peer="";Extra=Grid;Stage="CQ";Attempts=0;since=DateTimeOffset.UtcNow;}
 internal void Answer(string peer,int snr,DateTimeOffset now){if(!WeakDecode.IsCall(peer)||peer==Call)throw new IOException("Choose another resolved callsign.");Peer=peer;report=Math.Clamp(snr,-50,49).ToString("+00;-00;+00",CultureInfo.InvariantCulture);Extra=Grid;Stage="Grid";Attempts=0;since=now;}
 internal string Message=>Stage=="CQ"?$"CQ {Call} {Grid}":$"{Peer} {Call} {Extra}";
 internal bool Observe(PacketObservation p,DateTimeOffset now){var d=p.Weak;if(p.Replay||!p.IsValidated||p.Protocol is not ("FT8" or "FT4")||d.Utc<since||now-d.Utc>TimeSpan.FromSeconds(35)||d.Utc>now.AddSeconds(2)||!d.SenderResolved||d.Sender!=Peer||d.Destination!=Call||Complete)return false;string last=d.Message.Split(' ',StringSplitOptions.RemoveEmptyEntries).Last();string next="";if(Stage=="Grid"&&(WeakDecode.IsGrid(last)||Regex.IsMatch(last,@"^[+-][0-9]{2}$")))next=WeakDecode.IsGrid(last)?report:"R"+report;else if(Stage=="Report"&&Regex.IsMatch(last,@"^R[+-][0-9]{2}$"))next="RR73";else if(Stage=="Rreport"&&last is "RRR" or "RR73")next="73";else if(Stage=="RR73"&&last=="73"){Stage="Complete";return true;}if(next.Length==0)return false;Extra=next;Stage=next=="RR73"?"RR73":next=="73"?"73":next.StartsWith('R')?"Rreport":"Report";Attempts=0;return true;}
 internal void Manual(string step,int snr){if(Peer.Length==0)throw new IOException("Select Answer peer first.");report=Math.Clamp(snr,-50,49).ToString("+00;-00;+00",CultureInfo.InvariantCulture);Extra=step switch{"Grid"=>Grid,"Report"=>report,"R-report"=>"R"+report,"RR73"=>"RR73","73"=>"73",_=>throw new IOException("Choose an exchange step")};Stage=step=="R-report"?"Rreport":step;Attempts=0;}
 internal void Sent(){Attempts++;if(Stage=="73")Stage="Complete";}
}

// Bounded streaming I/Q source, shared by the native callback and offline tests.
internal sealed class WeakIqSource
{
 internal const int Rate=2000000;private static readonly double[] Sine=Enumerable.Range(0,65536).Select(i=>Math.Sin(i*2*Math.PI/65536)).ToArray();private readonly double[] phase;private readonly double lead;internal long Position{get;private set;}internal long Samples{get;}
 internal WeakIqSource(WeakTxPlan p,byte[] tones,long startSample=0){phase=WeakWaveform.Phase(p,tones);lead=p.Lead;Samples=(long)Math.Ceiling(p.Duration*Rate);Position=startSample;}
 internal int Read(byte[] data){Array.Clear(data);int count=(int)Math.Min(data.Length/2,Math.Max(0,Samples-Position));for(int i=0;i<count;i++,Position++){double at=(double)Position*12000/Rate-lead*12000;if(at<0||at>=phase.Length-1)continue;int n=(int)at;double angle=phase[n]+(phase[n+1]-phase[n])*(at-n);int index=(int)((long)(angle*(65536/(2*Math.PI)))&65535);double amp=32*WeakWaveform.Envelope(at,phase.Length-1,12000);data[2*i]=unchecked((byte)(sbyte)Math.Round(amp*Sine[(index+16384)&65535]));data[2*i+1]=unchecked((byte)(sbyte)Math.Round(amp*Sine[index]));}return count*2;}
}
