using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderUpdate042Smoke()
 {
  void Shot(string name){UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var f=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(f);}
  async Task Wait(Func<bool> condition){for(int i=0;i<300&&!condition();i++)await Task.Delay(20);if(!condition())throw new IOException("042 simulated receiver did not reach target; "+RtlStatus.Text);}
  Stop();if(!VolumeSlider.IsMoveToPointEnabled||VolumeSlider.SmallChange!=.01)throw new IOException("Volume click/wheel configuration failed");if(refreshPresetBars.Count!=2)throw new IOException("Missing Amateur tune bars");SelectWorkspace(PacketsTab);Shot("ui-042-aprs-presets.png");SelectWorkspace(weakTab!);Shot("ui-042-weak-presets.png");
  if(setupReport.Visibility!=Visibility.Collapsed||ReceiverIdentityStatus.Visibility!=Visibility.Collapsed||SamplingStatus.Visibility!=Visibility.Collapsed)throw new IOException("Sidebar text was not collapsed");
  Aviation.Observe(AviationData.Parse(AviationTests.PositionJson,"adsb",AviationTests.Context)!);Aviation.Refresh();var pages=(TabControl)AviationTab.Content;pages.SelectedIndex=1;SelectWorkspace((TabItem)pages.Items[1]);Shot("ui-042-aviation-messages.png");
  try{
   rfMode=true;engine.StartExternal(2);receiver.Start(0,129350000,-1000,false,fixture:true,fixtureSeconds:20,sampleRate:240000,mode:"AM",bandwidth:15000);await Wait(()=>receiver.AppliedChannel?.Frequency==129350000);var worker=receiver.WorkerId;
   Aviation.StartForTest("acars",129350000);TuneSaved(new FrequencyItem{Name="Synthetic ACARS",FrequencyHz=131550000,Mode="AM",BandwidthHz=15000});await Wait(()=>receiver.AppliedChannel?.Frequency==131550000&&Aviation.DecoderTargetForTest==131550000);if(!Aviation.DecoderEnabledForTest||receiver.WorkerId!=worker)throw new IOException("Saved-frequency tuning stopped decoder/receiver");
   StartFolderScan(new FrequencyItem{IsFolder=true,Name="Synthetic ACARS folder"},[new(){Name="One",FrequencyHz=129350000,Mode="AM",BandwidthHz=15000},new(){Name="Two",FrequencyHz=131550000,Mode="AM",BandwidthHz=15000}]);await Wait(()=>FolderRunning&&receiver.AppliedChannel?.Frequency==129350000&&Aviation.DecoderTargetForTest==129350000);StopFolderScan("Synthetic check complete");if(!Aviation.DecoderEnabledForTest)throw new IOException("Folder scan disabled Aviation");
   Aviation.Stop("Explicit stop test");await LiveTune(130450000);await Wait(()=>receiver.AppliedChannel?.Frequency==130450000);if(Aviation.DecoderEnabledForTest)throw new IOException("Retune overrode explicit decoder Stop");
  }finally{StopFolderScan("Synthetic check finished");Stop();}
  pages.SelectedIndex=0;SelectWorkspace((TabItem)pages.Items[0]);Shot("ui-042-aviation.png");
  var tx=new TransmitTestWindow(new(0,"Synthetic","MRS","Test","TX-SMOKE","HackRF"),"synthetic"){Owner=this};if(!tx.SafeInitialState)throw new IOException("TX opens armed");tx.Show();tx.UpdateLayout();tx.Close();
  if(!HelpTopics.Search(HelpTopics.Load(Path.Combine(AppContext.BaseDirectory,"Help")),"Discovery").Any())throw new IOException("Discovery help not searchable");
 }
}
