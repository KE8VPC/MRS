using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace MRS;
public partial class PacketPanel
{
 internal void Verify044Dialogs()
 {
  void Check(Window w){w.UpdateLayout();foreach(var box in Walk(w).OfType<CheckBox>().Where(b=>b.Content is string s&&(s.Contains("Highlight CQ")||s.Contains("Watched calls")||s.Contains("Exclude ignored"))))if(box.Foreground is not SolidColorBrush ink||ink.Color==Colors.Black)throw new IOException("Unreadable checkbox: "+box.Content);}
  ShowWeakTracking();var tracking=Application.Current.Windows.OfType<Window>().Single(w=>w.Title.StartsWith("Heard transmitting"));Check(tracking);tracking.Close();Exception? error=null;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>{var w=Application.Current.Windows.OfType<Window>().FirstOrDefault(w=>w.Title=="Callsign watch, ignore and colors");if(w==null)return;timer.Stop();try{Check(w);}catch(Exception ex){error=ex;}w.Close();};timer.Start();EditWeakRules();timer.Stop();if(error!=null)throw error;
 }
 private static IEnumerable<DependencyObject> Walk(DependencyObject parent){yield return parent;for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)foreach(var child in Walk(VisualTreeHelper.GetChild(parent,i)))yield return child;}
}
public partial class MainWindow
{
 internal async Task RenderUpdate044Smoke()
 {
  await RenderUpdate043Smoke();weakPanel.Verify044Dialogs();if(aprsInternet.Enabled||aprsNetEnable.IsChecked==true)throw new IOException("APRS reporting must start off");if(js8Tab==null||hfReferenceTab==null)throw new IOException("Missing new workspaces");SelectWorkspace(PacketsTab);UpdateLayout();await Task.Delay(100);Shot("ui-044-aprs.png");SelectWorkspace(hfReferenceTab);UpdateLayout();await Task.Delay(100);Shot("ui-044-hf.png");SelectWorkspace(js8Tab);UpdateLayout();await Task.Delay(100);Shot("ui-044-js8.png");
  var tx=new WeakTransmitWindow("FT8",14074000,"synthetic",(_,_,_)=>throw new IOException("No RF in UI smoke"),()=>false,null){Owner=this};tx.Show();tx.UpdateLayout();if(tx.Background is not SolidColorBrush surface||surface.Color.R>40)throw new IOException("TX window must use dark background with its light checkbox labels");var bmp=new RenderTargetBitmap((int)tx.ActualWidth,(int)tx.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(tx);Save(bmp,"ui-044-tx.png");tx.Close();
  void Shot(string name){var bmp=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(this);Save(bmp,name);}void Save(RenderTargetBitmap bmp,string name){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(file);}
 }
}
