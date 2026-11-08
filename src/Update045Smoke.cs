using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderUpdate045Smoke()
 {
  await RenderUpdate044Smoke();if(foxTab==null||foxSession.Running||foxCaptureEnabled)throw new IOException("Fox Hunt must be present and stopped at startup");var now=DateTimeOffset.UtcNow;var route=new List<FoxPoint>();for(int i=0;i<80;i++){var fix=new FoxFix(40+i*.0002,-100+Math.Sin(i/15.0)*.002,now.AddSeconds(i),"Synthetic NMEA",250,25,45,9,.8,now.AddSeconds(i));route.Add(new(now.AddSeconds(i),fix,FoxHuntTests.Scope,-90+i*.6,-85+i*.6,20,i>25,-100,i<45?1:2));}foxSession.Points.AddRange(route);foreach(var point in route.AsEnumerable().Reverse().Take(10))foxRows.Add(point);SelectWorkspace(foxTab);foxMap.Update(route,route[^1].Fix);UpdateLayout();foxMap.Fit();foxMessage="Synthetic route smoke — no live GPS or RF device opened";foxStatus.Text=foxMessage;await Task.Delay(100);var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-045-fox.png")))encoder.Save(file);if(foxRows.Count!=10||foxSession.Running)throw new IOException("Fox route UI data/state mismatch");
  // A native synthetic receiver worker causes the same channel-change callback
  // as a real retune, without opening any USB receiver.
  SelectWorkspace(PacketsTab);UpdateLayout();await Task.Delay(100);var target=FindChildren(this).OfType<TextBox>().First(t=>t.Name=="AprsPresetTarget");target.Text="144.390000";rfMode=true;receiver.Start(0,146520000,200,false,fixture:true,sampleRate:240000,fixtureSeconds:2,mode:"NFM",bandwidth:12500);for(int n=0;n<100&&receiver.AppliedChannel==null;n++)await Task.Delay(20);await Task.Delay(100);if(receiver.AppliedChannel==null)throw new IOException("Synthetic channel change did not arrive");if(target.Text!="144.390000")throw new IOException("APRS tune target overwritten by receiver change");target.Text="145.010000";weakMode.SelectedItem="FT4";await Task.Delay(100);if(target.Text!="145.010000")throw new IOException("Weak-mode selection reset unrelated APRS target");Stop();
 }
 private static IEnumerable<DependencyObject> FindChildren(DependencyObject parent){yield return parent;for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)foreach(var item in FindChildren(VisualTreeHelper.GetChild(parent,i)))yield return item;}
}
