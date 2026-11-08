using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderUpdate041Smoke()
 {
  Stop();
  void Shot(string name){UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(file);}
  foreach(var tab in new[]{signalingTab,weakTab,meshTab})if(tab?.Parent is not TabControl)throw new InvalidOperationException("Missing radio data workspace");
  sameDecode.IsChecked=dtmfDecode.IsChecked=mdcDecode.IsChecked=fleetDecode.IsChecked=true;
  if(!receiver.CaptureData)throw new InvalidOperationException("NFM signaling has no discriminator feed");
  var c=new PacketContext("041-ui","Generated UI preview",true,162550000,1);
  signalingPanel.Add(RadioDataParse.Signaling("{\"demod_name\":\"EAS\",\"header_begin\":\"ZCZC\",\"last_message\":\"-WXR-RWT-039011+0015-3021200-K1ABC   -\"}",c)!);
  signalingPanel.Add(RadioDataParse.Json("{\"protocol\":\"FleetSync\",\"from_fleet\":100,\"from_unit\":1290,\"to_fleet\":100,\"to_unit\":2109,\"cmd\":0,\"subcmd\":0,\"fsync2\":0,\"payload\":\"\"}",c)!);
  SelectWorkspace(signalingTab!);await Task.Delay(150);Shot("ui-041-signaling.png");
  sameDecode.IsChecked=dtmfDecode.IsChecked=mdcDecode.IsChecked=fleetDecode.IsChecked=false;
  weakPanel.Add(RadioDataParse.Json("{\"protocol\":\"FT8\",\"text\":\"CQ K1ABC FN42\",\"audio_hz\":1500,\"dt\":1.36,\"sync_score\":33}",c)!);SelectWorkspace(weakTab!);await Task.Delay(100);Shot("ui-041-weak.png");
  meshPanel.Add(MeshPacket.Parse("{\"status\":2,\"has_crc\":true,\"crc_ok\":true,\"sync\":43,\"snr_db\":10,\"hex\":\"FFFFFFFF785634122A00000003090000AABB\"}",c,new("Meshtastic",906875000))!);SelectWorkspace(meshTab!);await Task.Delay(100);Shot("ui-041-mesh.png");
  meshMode.SelectedItem="MeshCore";if(meshFrequency.Text.Length!=0||(int)meshBw.SelectedItem!=62500)throw new InvalidOperationException("MeshCore retained incompatible profile");
  weakPanel.ClearRows();weakMode.SelectedItem="FT4";weakData.Configure("FT4");PlayFile(Path.Combine(AppContext.BaseDirectory,"Docs","Samples","synthetic-ft4.wav"),true);SelectWorkspace(weakTab!);var until=DateTime.UtcNow.AddSeconds(20);while(DateTime.UtcNow<until&&!weakPanel.MonitorGrid.Items.OfType<PacketObservation>().Any(p=>p.Protocol=="FT4"&&p.Information.Contains("CQ K1ABC FN42")))await Task.Delay(150);if(!weakPanel.MonitorGrid.Items.OfType<PacketObservation>().Any(p=>p.Protocol=="FT4"&&p.Replay))throw new InvalidOperationException("WAV EOF discarded final weak-signal window: "+weakData.Status);Shot("ui-041-ft4-wav.png");
  weakTimingExpander.IsExpanded=true;useNistTime.IsChecked=true;if(!WeakReplay||NistWeakOffset!=0)throw new InvalidOperationException("Replay inherited live NIST clock correction");TickWeakTiming();Shot("ui-041-timing.png");
  weakMode.SelectedItem="FT8";useNistTime.IsChecked=false;manualWeakOffset=rxWeakOffset=0;rxTiming.Reset();foreach(string call in new[]{"K1ABC","W2XYZ","N3TEST"})foreach(long slot in new long[]{10,11})rxTiming.Observe(RadioDataParse.Json(System.Text.Json.JsonSerializer.Serialize(new{protocol="FT8",text="CQ "+call+" FN42",audio_hz=1500,dt=.8,dt_error=.3,mrs_slot=slot,mrs_correction=.2,sync_score=33}),c)!);ApplyWeakEstimate(false);if(Math.Abs(rxWeakOffset+.1)>.00001)throw new InvalidOperationException("RX estimate application has wrong sign");lastRxApply=0;rxWeakOffset=0;ApplyWeakEstimate(true);if(Math.Abs(rxWeakOffset+.1)>.00001)throw new InvalidOperationException("Automatic RX correction was not bounded/applied");ApplyWeakEstimate(true);if(Math.Abs(rxWeakOffset+.1)>.00001)throw new InvalidOperationException("Automatic RX correction ignored cooldown");TickWeakTiming();Shot("ui-041-timing-consensus.png");
  var topics=HelpTopics.Load(Path.Combine(AppContext.BaseDirectory,"Help"));if(!HelpTopics.Search(topics,"Meshtastic").Any()||!HelpTopics.Search(topics,"WSPR").Any())throw new InvalidOperationException("New decoder help not searchable");
 }
}
