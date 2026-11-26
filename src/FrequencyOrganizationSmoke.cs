using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal void RenderOrganizationSmoke()
 {
  string file=Path.Combine(dataFolder,"organization-smoke.json");var library=new FrequencyLibrary(file);foreach(var (variant,hz) in new[]{("Unidentified system",453000000L),("Capacity Plus",463562500L),("Connect Plus",461375000L)})library.SaveSiteChannels([new("DMR",["DMR",variant,"Unassigned at "+PlanCsv.MHz(hz),"Unresolved site"],$"DMR:{variant}:Unassigned at {PlanCsv.MHz(hz)}:Unresolved site",hz,"","Observed carrier",variant+"; CC 1",Evidence:"Provisional identity")]);library.Upsert(new(){Name="LTR · IDs unknown",Protocols="LTR",FrequencyHz=451449500,Signaling="Area 1; Home 7",Notes="Synthetic note"});FrequencyManager.Initialize(file);
  var wizard=(FrequencyOrganizationWindow)FrequencyManager.CreateOrganizationWindow();wizard.ShowInTaskbar=false;wizard.Left=-16000;wizard.Show();try{wizard.UpdateLayout();Capture("ui-051-organization-rules.png");wizard.GeneratePreview();wizard.UpdateLayout();Capture("ui-051-organization-preview.png");wizard.SmokeApply();var after=new FrequencyLibrary(file);if(after.Items.Count(i=>!i.IsFolder)!=4||after.Organization==null||after.Items.Any(i=>i.Name=="Unresolved site"))throw new IOException("Wizard apply lost channels, preferences or placeholder cleanup");if(after.Items.Single(i=>i.Protocols=="LTR").Notes!="Synthetic note")throw new IOException("Wizard changed notes");Capture("ui-051-organization-applied.png");File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"organization-ui.txt"),"PASS: real WPF organization window, rules/tree/move preview, explicit Apply, saved preferences, placeholder cleanup and stable channel/note preservation. Isolated synthetic library only; no operational library, device, RF or API request.");}finally{wizard.Close();}
  void Capture(string name){var bitmap=new RenderTargetBitmap((int)wizard.ActualWidth,(int)wizard.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(wizard);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(stream);}
 }
}
