using System.Globalization;
using System.Text.RegularExpressions;
namespace MRS;
internal static class DiscoveryNames
{
 internal static string Frequency(long hz)=>(hz/1000000m).ToString("0.#######",CultureInfo.InvariantCulture);
 internal static bool Generated(string name)=>string.IsNullOrWhiteSpace(name)||Regex.IsMatch(name,@"^(Observed carrier|Mapped voice carrier|Unknown signal|Signal [\d.,]+ MHz|Channel \d+-\d+|DMR ·.*|P25 ·.*|NXDN ·.*)$",RegexOptions.IgnoreCase);
 internal static string Label(FrequencyItem item)
 {
  if(!Generated(item.Name))return item.Name;
  string text=item.Signaling+"; "+item.DmrIdentifiers;string Value(string pattern){var m=Regex.Match(text,pattern,RegexOptions.IgnoreCase);return m.Success?m.Groups[1].Value:"";}
  string site=Value(@"\bSITE\s+(\d+)"),net=Value(@"\b(?:NET(?:WORK)?|SYS(?:TEM)?)\s+([A-F\d]+)"),cc=Value(@"\bCC\s+(\d+)"),ran=Value(@"\bRAN\s+(\d+)");
  string protocol=item.Protocols is "" or "Unknown"?"Unknown signal":item.Protocols;
  var parts=new List<string>{protocol};if(net.Length>0)parts.Add((protocol=="NXDN"?"Sys ":"Net ")+net);if(site.Length>0)parts.Add("Site "+site);if(cc.Length>0)parts.Add("CC "+cc);if(ran.Length>0)parts.Add("RAN "+ran);
  if(item.ChannelIds.Length>0)parts.Add("Ch "+item.ChannelIds);
  if(parts.Count==1&&protocol!="Unknown signal")parts.Add("IDs unknown");
  return string.Join(" · ",parts)[..Math.Min(160,string.Join(" · ",parts).Length)];
 }
 internal static long Snap(long hz,int spacing,int tolerance)
 {
  if(spacing==0)return hz;
  if(spacing is not(500 or 1000 or 2500 or 5000 or 6250 or 12500 or 25000)||tolerance<0||tolerance>spacing/2)throw new InvalidOperationException("Choose a supported spacing and a tolerance no larger than half a channel.");
  long nearest=checked((long)Math.Round((decimal)hz/spacing,0,MidpointRounding.AwayFromZero)*spacing);
  return nearest>0&&Math.Abs(nearest-hz)<=tolerance?nearest:hz;
 }
}
