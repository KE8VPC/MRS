using System.IO;
namespace MRS;
internal static class Update042Tests
{
 internal static void Run(Action<bool,string> check)
 {
  void Reject(Action action,string label){bool denied=false;try{action();}catch{denied=true;}check(denied,label);}
  var declaration=new TxDeclaration("United States","K1ABC","Technician",true,true,true);var plan=new TxTestPlan(144390000);
  Reject(()=>TransmitAuthorization.Read(null,plan),"TX denies missing authorization before hardware access");
  foreach(var bad in new[]{declaration with{Country="Unknown"},declaration with{Callsign=""},declaration with{LicenseClass="Novice"},declaration with{CurrentLicense=false},declaration with{LocalRulesChecked=false},declaration with{EquipmentChecked=false}})Reject(()=>TransmitAuthorization.Validate(bad,plan,"synthetic"),"TX denies missing/unsupported declaration: "+bad);
  foreach(long hz in new[]{144100000L,148000000,430000000,450000000,162400000,131550000,1090000000})Reject(()=>TransmitAuthorization.Validate(declaration,new(hz),"synthetic"),"TX rejects emission crossing supported edges or receive-only service "+hz);
  var permit=TransmitAuthorization.Issue(declaration,plan,"synthetic");check(TransmitAuthorization.Read(permit,plan,"synthetic").Frequency==plan.Frequency,"TX declaration bound to exact request and device");
  Reject(()=>TransmitAuthorization.Read(permit,plan with{Gain=4},"synthetic"),"TX rejects changed gain after arming");Reject(()=>TransmitAuthorization.Read(permit,plan,"other"),"TX rejects changed device");
  var expired=System.Text.Json.JsonSerializer.Serialize(new TxPermit(declaration,plan.Frequency,plan.Gain,plan.Seconds,"synthetic",DateTimeOffset.UtcNow.AddSeconds(-1)));Reject(()=>TransmitAuthorization.Read(expired,plan),"TX rejects expired authorization");
  using(var aviation=new AviationPipeline()){aviation.Start("acars",129350000);aviation.Follow(131550000);check(aviation.Enabled&&aviation.TargetHz==131550000,"ACARS decoder selection follows channel without disabling");aviation.Boundary();check(aviation.Enabled,"Aviation survives acquisition boundary");aviation.Stop();aviation.Follow(129350000);check(!aviation.Enabled,"Explicit Aviation Stop survives retune");}
  string dir=Path.Combine(Path.GetTempPath(),"MRS-042-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{
   var library=new FrequencyLibrary(Path.Combine(dir,"frequencies.json"));var entry=new ListeningPreset{Name="User receive reference",FrequencyMHz="144.390",Protocol="APRS"};check(library.AddReferences("Aviation/ACARS",[entry])==1,"Reference import adds one saved channel");check(library.AddReferences("Aviation/ACARS",[entry])==0,"Repeated reference import is deduplicated");check(library.AddReferences("Other",[new ListeningPreset{Name="Alternate protocol label",FrequencyMHz="144.390",Protocol="Local"}])==0,"Reference import preserves existing same-frequency/mode labels regardless of protocol label");check(new FrequencyLibrary(Path.Combine(dir,"frequencies.json")).Items.Any(i=>i.Name==entry.Name&&i.FrequencyHz==144390000),"Reference import persists on reload");
   Reject(()=>library.AddReferences("Bad",[entry,new(){FrequencyMHz="bad"}]),"Reference import validates entire batch before saving");check(library.Items.Count(i=>!i.IsFolder)==1,"Rejected reference batch preserves library");
  }finally{Directory.Delete(dir,true);}
  foreach(string folder in new[]{"aviation-acars","aviation-vdl2","aviation-hf-voice","aviation-hf-volmet","aviation-hf-hfdl","amateur-presets","time-stations"}){var entries=DiscoveryReferences.Read(Path.Combine(AppContext.BaseDirectory,"bandplans","reference",folder,"channels.csv"));foreach(var entry in entries)entry.Validate();check(entries.Length>0,"Reference CSV usable for receiver presets/import: "+folder);}
 }
}
