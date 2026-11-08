using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderDiscoverySharingSmoke()
 {
  string file=Path.Combine(dataFolder,"sharing-smoke-library.json");var library=new FrequencyLibrary(file);var folder=new FrequencyItem{Name="DMR",IsFolder=true};library.Upsert(folder);var channel=new FrequencyItem{Name="Observed carrier",FrequencyHz=461413035,Protocols="DMR",Signaling="Network 5; Site 11; CC 4",SiteScope="DMR:Tier III:Network 5:Site 11",ParentId=folder.Id};library.Upsert(channel);
  FrequencyManager.Initialize(file);FrequencyManager.SelectEntry(folder.Id);SidebarTabs.SelectedItem=FrequencyTab;
  foreach(var (window,name) in new[]{(FrequencyManager.CreateSharingWindow(),"ui-discovery-sharing.png"),(FrequencyManager.CreateCleanupWindow(),"ui-discovery-cleanup.png")}){
   window.ShowInTaskbar=false;window.Left=-16000;window.Show();window.UpdateLayout();await Task.Delay(100);var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(AppContext.BaseDirectory,name)))png.Save(stream);if(window.ActualWidth<600)throw new IOException("Library tools window failed to render");window.Close();
  }
  var exported=library.ExportDiscoveries(extra:[DiscoveryRecord.Create("talkgroup","DMR:Tier III:Network 5","91","Dispatch","DMR")]);string bundle=Path.Combine(dataFolder,"synthetic-discoveries.mrsdiscovery");DiscoveryExchange.Write(bundle,exported);var imported=new FrequencyLibrary(Path.Combine(dataFolder,"imported-sharing-smoke.json"));imported.ApplyReview(imported.PreviewImport(DiscoveryExchange.Read(bundle)));if(imported.Items.Count(i=>!i.IsFolder)!=1||!imported.Records.Any(r=>r.Kind=="talkgroup"))throw new IOException("Shared catalog import was lost");
  library.ApplyReview(library.PreviewCleanup(true,true,true,12500,600));FrequencyManager.Initialize(file);if(!FrequencyManager.Markers.Single().Name.Contains("Site 11")||FrequencyManager.Markers.Single().FrequencyHz!=461412500)throw new IOException("Cleanup did not refresh the saved list/markers");Spectrum.SetMarkers(FrequencyManager.Markers);SelectWorkspace(SpectrumTab);UpdateLayout();
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"discovery-sharing-ui.txt"),"PASS: import/export and cleanup windows render; approved synthetic bundle preserves channel/site/TG catalog; cleanup retains measured frequency and refreshes informative markers. No operational library, RF or reporting touched.");
 }
}
