using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record TxDeclaration(string Country,string Callsign,string LicenseClass,bool CurrentLicense,bool LocalRulesChecked,bool EquipmentChecked);
internal sealed record TxPermit(TxDeclaration Declaration,long Frequency,int Gain,int Seconds,string Identity,DateTimeOffset Expires);
internal static class TransmitAuthorization
{
 internal const string Variable="MRS_SESSION_TX_PERMIT";
 internal static void Validate(TxDeclaration? d,TxTestPlan plan,string identity)
 {
  plan.Validate();if(d==null||d.Country!="United States")throw new IOException("TX locked: only the reviewed United States amateur profile is supported.");
  if(!new[]{"Technician","General","Advanced","Amateur Extra"}.Contains(d.LicenseClass)||!Regex.IsMatch(d.Callsign.ToUpperInvariant(),@"^[AKNW][A-Z]?[0-9][A-Z]{1,3}$"))throw new IOException("TX locked: enter a U.S. callsign and supported license class.");
  if(!d.CurrentLicense||!d.LocalRulesChecked||!d.EquipmentChecked)throw new IOException("TX locked: confirm current authority, local restrictions and equipment/power compliance.");
  // This narrow profile supports NFM test/APRS only, with a conservative 25 kHz occupied envelope.
  const long half=12500;bool fits=plan.Frequency-half>=144100000&&plan.Frequency+half<=148000000 || plan.Frequency-half>=430000000&&plan.Frequency+half<=450000000;
  if(!fits)throw new IOException("TX locked: complete 25 kHz emission must fit 144.1–148 or 430–450 MHz. Other bands/profiles are unsupported.");
  if(string.IsNullOrWhiteSpace(identity))throw new IOException("TX locked: a stable device identity is required.");
 }
 internal static string Issue(TxDeclaration d,TxTestPlan p,string identity){Validate(d,p,identity);return JsonSerializer.Serialize(new TxPermit(d,p.Frequency,p.Gain,p.Seconds,identity,DateTimeOffset.UtcNow.AddSeconds(15)));}
 internal static TxPermit Read(string? text,TxTestPlan plan,string? identity=null)
 {
  if(string.IsNullOrWhiteSpace(text))throw new IOException("TX locked: start an explicitly armed session in the transmit window.");var p=JsonSerializer.Deserialize<TxPermit>(text)??throw new IOException("Invalid TX permit");
  Validate(p.Declaration,plan,p.Identity);if(p.Expires<DateTimeOffset.UtcNow||p.Expires>DateTimeOffset.UtcNow.AddSeconds(16)||p.Frequency!=plan.Frequency||p.Gain!=plan.Gain||p.Seconds!=plan.Seconds||identity!=null&&identity!=p.Identity)throw new IOException("TX locked: expired or mismatched session authorization. Arm again.");return p;
 }
}
