using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderFoxToolsSmoke()
 {
  if(foxTab==null||!WorkspaceTabs.Items.Contains(foxTab)||((TabControl)AmateurTab.Content).Items.Contains(foxTab))throw new IOException("Fox Hunt is not a main workspace");
  var now=DateTimeOffset.UtcNow;foxSession.Points.AddRange(FoxAnalysisTests.Route(now));foreach(var point in foxSession.Points.AsEnumerable().Reverse().Take(12))foxRows.Add(point);
  var syntheticMap=Path.Combine(dataFolder,"synthetic-road-detail.geojson");File.WriteAllText(syntheticMap,FoxAnalysisTests.SyntheticRoads);await foxMap.LoadDetail(syntheticMap);
  SelectWorkspace(foxTab);UpdateLayout();foxMap.Update(foxSession.Points,null);foxMap.Fit();await CalculateFoxEstimate();
  if(!foxEstimateStatus.Text.StartsWith("Rough estimate:"))throw new IOException("Synthetic rough area not shown: "+foxEstimateStatus.Text);
  foxStatus.Text="Synthetic route and road detail · no live GPS / RF devices opened";
  foxTrend.Foreground=Brushes.LightGreen;foxTrend.Text="Signal trend: ↑ Stronger · +3.2 dB across recent windows · strength change, not direction or distance";
  UpdateLayout();await Task.Delay(100);Save("ui-fox-tools.png");
  if(foxEstimateStatus.TransformToAncestor(this).Transform(new Point(0,0)).Y>=foxMap.TransformToAncestor(this).Transform(new Point(0,0)).Y)throw new IOException("Estimate status is hidden below the map");
  if(foxMap.ActualHeight<130)throw new IOException("Fox map is hidden by setup controls");
  ResetFoxEstimate();if(!foxEstimateStatus.Text.Contains("not calculated"))throw new IOException("Rough area did not clear");
  // Show the local public optional map pack if provided to this diagnostic run.
  string publicMap=Path.Combine(AppContext.BaseDirectory,"optional-roads.geojson");
  if(File.Exists(publicMap)){await foxMap.LoadDetail(publicMap);foxSession.Points.Clear();foxRows.Clear();var fix=new FoxFix(40.56,-84.2,now,"Synthetic map center only");foxMap.Update([],fix);foxStatus.Text="Public offline road-detail rendering check · synthetic map center only";foxMap.Fit();UpdateLayout();await Task.Delay(100);Save("ui-fox-offline-roads.png");}
  var aircraftMapSettings=Path.Combine(dataFolder,"aviation-local-map-smoke.json");
  using(var firstMap=new AircraftMap()){await firstMap.ConfigureOfflineDetail(aircraftMapSettings);await firstMap.LoadOfflineDetail(File.Exists(publicMap)?publicMap:syntheticMap);if(firstMap.LocalDetail==null||firstMap.NetworkRequests!=0||firstMap.OnlineEnabled)throw new IOException("Aviation local detail load enabled network or failed");}
  using(var restoredMap=new AircraftMap()){
   await restoredMap.ConfigureOfflineDetail(aircraftMapSettings);if(restoredMap.LocalDetail==null||restoredMap.OnlineEnabled)throw new IOException("Aviation offline map choice did not restore");
   var mapWindow=new Window{Content=restoredMap,Width=1000,Height=580,ShowInTaskbar=false,Left=-16000};mapWindow.Show();try{var plane=new AircraftRow{Identity="ICAO:SYNTHETIC",Callsign="MRS TEST",Latitude=File.Exists(publicMap)?40.56:40,Longitude=File.Exists(publicMap)?-84.2:-100,PositionSeen=DateTimeOffset.UtcNow,LastSeen=DateTimeOffset.UtcNow};restoredMap.Update([plane],plane);restoredMap.FitForTest();mapWindow.UpdateLayout();await Task.Delay(100);var image=new RenderTargetBitmap((int)mapWindow.ActualWidth,(int)mapWindow.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(mapWindow);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-aviation-local-detail.png")))png.Save(file);if(restoredMap.NetworkRequests!=0)throw new IOException("Offline Aviation rendering requested network tiles");restoredMap.RemoveOfflineDetail();if(restoredMap.LocalDetail!=null)throw new IOException("Aviation detail was not removable");}finally{mapWindow.Close();}
  }
  void Save(string name){var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(output);}
 }
}
