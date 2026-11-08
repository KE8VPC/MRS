using System.IO;
using System.Text.Json;
namespace MRS;
internal static class Update040Tests
{
 internal static void Run(Action<bool,string> check){
  check(FrequencyCalibration.HardwareHz(100000000,1.25)==100000125&&FrequencyCalibration.HardwareHz(100000000,-1.25)==99999875,"Signed fractional PPM command correction in both directions");
  check(FrequencyCalibration.HardwareHz(1090000000,0)==1090000000&&new FrequencyCalibration(false,12).Active==0,"Disabled calibration leaves nominal tune unchanged");
  foreach(double value in new[]{double.NaN,double.PositiveInfinity,201d,-201d}){bool rejected=false;try{FrequencyCalibration.HardwareHz(100000000,value);}catch(IOException){rejected=true;}check(rejected,"Invalid calibration rejected: "+value);}
  var source=new PacketContext("synthetic-flex","Synthetic paging",true,931000000);
  string json=JsonSerializer.Serialize(new{baud=1600,level=4,msg_type="alphanumeric",capcode=1234567,fragment="complete",message=@"\nMRS TEST\r\nLine two\tvalue\n",k_ok=true});var packet=PagingJson.Parse(json,source)!;
  check(packet.ReadableInformation.Contains("\nMRS TEST\nLine two\tvalue\n")&&packet.Description=="MRS TEST Line two value","FLEX control escapes render readable details and compact table text");
  check(packet.DecoderJson==json&&packet.Information.Contains(@"\r\n")&&packet.IsValidated,"Readable paging formatting preserves raw payload and checksum evidence");
  check(PagingText.Expand(@"C:\folder\unit \x41") == @"C:\folder\unit \x41","Paging display does not blanket-unescape arbitrary backslash sequences");
  check(PagingText.Readable(packet with{Protocol="POCSAG"},@"\nTEST")==@"\nTEST"&&PagingText.Readable(packet with{Kind="numeric"},@"\nTEST")==@"\nTEST","FLEX normalization does not change POCSAG or numeric payloads");
  check(PagingText.Expand("A\r\nB\rC\nD")=="A\nB\nC\nD","Actual CR/LF and escaped controls share stable readable line endings");
  var all=new Dictionary<string,ReceiverProfile>{{"A",new("A","Control")},{"B",new("B","Voice")},{"C",new("C","Voice two")},{"D",new("D","Independent")}};
  all=MainWindow.AssignReceiver(all,all["A"] with{Role="Trunk control"},null,["B","C"]);
  check(all["A"].Group=="receiver:A"&&all["B"].ControlId=="A"&&all["C"].Group==all["A"].Group&&all["D"].Role=="Independent","Direct control/voice selection creates isolated routing without typed group names");
  all=MainWindow.AssignReceiver(all,all["A"],null,["C"]);check(all["B"].Role=="Independent"&&all["B"].ControlId==null&&all["C"].ControlId=="A","Unchecking a voice receiver releases its saved assignment");
  bool invalid=false;try{MainWindow.AssignReceiver(all,all["B"] with{Role="Trunk voice"},"D",[]);}catch(IOException){invalid=true;}check(invalid,"Voice assignment rejects an independent control target with actionable validation");
  string folder=Path.Combine(AppContext.BaseDirectory,"tests-040",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"fleet.json");File.WriteAllText(path,JsonSerializer.Serialize(all));var restored=JsonSerializer.Deserialize<Dictionary<string,ReceiverProfile>>(File.ReadAllText(path))!;check(restored["C"].ControlId=="A"&&restored["A"].Role=="Trunk control","Direct receiver assignments round-trip across disk/restart");
  var old=JsonSerializer.Deserialize<ReceiverProfile>("{\"Id\":\"A\",\"Name\":\"Old\",\"Role\":\"Trunk control\",\"Group\":\"Old group\",\"Priority\":0}")!;check(old.ControlId==null&&old.Group=="Old group","Legacy receiver definitions remain compatible");
  var metadata=new IqMetadata(3,DateTimeOffset.UtcNow,false,0,null,FrequencyCorrectionPpm:1.25);string recording=Path.Combine(folder,"calibrated.mrsiq");using(var w=new IqRecording(recording,metadata)){w.Write(new byte[16384],new CaptureDescriptor(100000000,240000,240000,IqSampleFormat.Unsigned8),new(100000000,"NFM",12500));}using(var reader=new IqReader(recording))check(reader.Metadata.FrequencyCorrectionPpm==1.25&&reader.Read()?.Frequency==100000000,"I/Q records PPM provenance while keeping nominal RF coordinates");
  check(AboutWindow.Dependencies.Any(d=>d.Feature.Contains("FLEX"))&&AboutWindow.Dependencies.Any(d=>d.Component=="rtl_433")&&AboutWindow.Dependencies.All(d=>d.Source.StartsWith("https://")),"About dependency catalog identifies helpers and HTTPS source links");
 }
}
