using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderUpdate043Smoke()
 {
  Stop();void Shot(string name){UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(file);}
  SelectWorkspace(weakTab!);weakTimingExpander.IsExpanded=false;weakMode.SelectedItem="FT8";weakPanel.WeakSettings.ClearCycles=0;weakPanel.WeakSettings.Rules.Clear();weakPanel.WeakSettings.Rules.Add(new(){Callsign="W2XYZ",Color="Gold"});DateTimeOffset now=DateTimeOffset.UtcNow;
  foreach(var text in new[]{"CQ K1ABC FN42","K1ABC W2XYZ -09","CQ DX N3TEST FM19","W2XYZ K1ABC R-12"})weakPanel.Add(Update043Tests.Row(text,utc:now));weakPanel.MonitorGrid.SelectedItem=null;TickWeakTiming();foreach(var order in monitorOrders)order.Refresh();await Task.Delay(150);Shot("ui-043-weak.png");
  var cqRow=weakPanel.MonitorGrid.Items.OfType<PacketObservation>().First(p=>p.Weak.Sender=="K1ABC"&&p.Weak.Cq);var cqVisual=(DataGridRow)weakPanel.MonitorGrid.ItemContainerGenerator.ContainerFromItem(cqRow);if(cqVisual.Background is not SolidColorBrush cqBrush||cqBrush.Color!=Color.FromRgb(184,237,190))throw new IOException("CQ row color not applied");
  var watchRow=weakPanel.MonitorGrid.Items.OfType<PacketObservation>().First(p=>p.Weak.Sender=="W2XYZ");var watchVisual=(DataGridRow)weakPanel.MonitorGrid.ItemContainerGenerator.ContainerFromItem(watchRow);if(watchVisual.Background is not SolidColorBrush watchBrush||watchBrush.Color!=Colors.Gold)throw new IOException("Watched row color not applied");
  string[] headers=weakPanel.MonitorGrid.Columns.Select(c=>c.Header?.ToString()??"").ToArray();if(headers.Any(h=>h is "Label" or "Type" or "Validation")||!headers.Contains("dB")||!headers.Contains("DT"))throw new IOException("Weak table still contains diagnostic columns");if(weakTimingExpander.IsExpanded||weakStatus.IsVisible)throw new IOException("Verbose timing is not collapsed");if(pskReporter.Enabled||pskEnabled.IsChecked==true)throw new IOException("Reporting must be off by default");
  int tracked=weakPanel.WeakTrackedCount;var sample=Update043Tests.Row("CQ W2XYZ FN42",utc:now);weakPanel.SetWeakCycleMode(1);weakPanel.BeginWeakCycle(sample.SourceKey!,sample.Weak.Slot!.Value,"FT8",sample.Weak.Epoch);weakPanel.Add(sample);weakPanel.BeginWeakCycle(sample.SourceKey!,sample.Weak.Slot!.Value+1,"FT8",sample.Weak.Epoch);if(weakPanel.Count!=0||weakPanel.WeakTrackedCount<tracked)throw new IOException("Cycle clearing erased tracking or retained old messages");weakPanel.WeakSettings.ClearCycles=0;
  weakPanel.WeakSettings.Rules.Add(new(){Callsign="N3TEST",Ignore=true});weakPanel.Add(Update043Tests.Row("CQ N3TEST FN42",utc:now));weakPanel.RefreshWeakForTest();if(weakPanel.VisibleCount!=0||weakPanel.Count!=1)throw new IOException("Ignore must hide without deleting decoded evidence");weakPanel.WeakSettings.Rules.RemoveAt(weakPanel.WeakSettings.Rules.Count-1);weakPanel.RefreshWeakForTest();
  weakPanel.ClearRows();weakPanel.SetWeakCycleMode(2);long even=sample.Weak.Slot!.Value/2*2;weakPanel.BeginWeakCycle(sample.SourceKey!,even,"FT8",sample.Weak.Epoch);weakPanel.Add(sample);weakPanel.BeginWeakCycle(sample.SourceKey!,even+1,"FT8",sample.Weak.Epoch);if(weakPanel.Count!=1)throw new IOException("Two-cycle mode cleared too early");weakPanel.BeginWeakCycle(sample.SourceKey!,even+2,"FT8",sample.Weak.Epoch);if(weakPanel.Count!=0)throw new IOException("Two-cycle mode did not clear at the next batch");weakPanel.SetWeakCycleMode(0);
  using var transport=new Update043Tests.CaptureTransport();using var report=new PskReporter(transport);report.Enable(new("W2XYZ","FN42ab","Synthetic SDR","Synthetic antenna"));report.Queue(Update043Tests.Row(utc:now),false,true,now);await report.Flush(now);if(transport.Packets.Count!=1||!report.Status.Contains("unconfirmed"))throw new IOException("Mock reporting workflow failed");
  foreach(var text in new[]{"CQ K1ABC FN42","K1ABC W2XYZ -09","CQ N3TEST FM19"})weakPanel.Add(Update043Tests.Row(text,utc:now));weakPanel.MonitorGrid.SelectedItem=null;weakTimingExpander.IsExpanded=true;TickWeakTiming();Shot("ui-043-timing-details.png");weakTimingExpander.IsExpanded=false;
  var presets=DiscoveryReferences.Read(Path.Combine(AppContext.BaseDirectory,"bandplans","reference","amateur-presets","channels.csv"));if(!presets.Any(p=>p.Hz==144174000&&p.Protocol=="FT8")||!presets.Any(p=>p.Hz==432174000&&p.Protocol=="FT8"))throw new IOException("Missing VHF/UHF presets");
  if(!HelpTopics.Search(HelpTopics.Load(Path.Combine(AppContext.BaseDirectory,"Help")),"PSK Reporter").Any())throw new IOException("Reporter help not searchable");
 }
}
