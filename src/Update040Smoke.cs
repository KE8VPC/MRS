using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderUpdate040Smoke(Action<string> capture)
 {
  Stop();
  void Shot(Window w,string name){w.UpdateLayout();var bitmap=new RenderTargetBitmap((int)w.ActualWidth,(int)w.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(w);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(AppContext.BaseDirectory,name));png.Save(file);}
  static IEnumerable<T> Find<T>(DependencyObject d) where T:DependencyObject {if(d is T t)yield return t;for(int i=0;i<VisualTreeHelper.GetChildrenCount(d);i++)foreach(var c in Find<T>(VisualTreeHelper.GetChild(d,i)))yield return c;}
  void Press(Window w,string name)=>Find<Button>(w).First(b=>b.Content?.ToString()==name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  var profile=new ReceiverProfile("F","Synthetic voice F");SaveFleet(profile);var voice=OpenFleetReceiver(profile);voice.ShowInTaskbar=false;voice.Left=-16000;
  var manager=CreateFleetManager();manager.ShowInTaskbar=false;manager.Left=-16000;manager.Show();await Task.Delay(100);
  var role=Find<ComboBox>(manager).Single(c=>c.Name=="ReceiverRoleEdit");role.SelectedItem="Trunk control";Find<TextBox>(manager).Single(c=>c.Name=="ReceiverNameEdit").Text="Test control saved";
  var status=Find<TextBlock>(manager).Single(c=>c.Name=="AssignmentSaveStatus");if(!status.Text.StartsWith("Unsaved"))throw new InvalidOperationException("Receiver edits lack unsaved feedback");
  var box=Find<CheckBox>(manager).First(c=>c.Content?.ToString()?.Contains("[F]")==true);box.IsChecked=true;Press(manager,"Save assignments");
  if(!status.Text.StartsWith("Saved assignments")||ReadFleet()["F"].ControlId!=workspaceSlot||voice.fleetProfile.ControlId!=workspaceSlot)throw new InvalidOperationException("Manager did not save/apply selected voice routing");
  Press(manager,"Reload saved");if(Find<CheckBox>(manager).First(c=>c.Content?.ToString()?.Contains("[F]")==true).IsChecked!=true)throw new InvalidOperationException("Manager reload lost voice assignment");Shot(manager,"ui-receiver-direct-assignments.png");manager.Close();
  manager=CreateFleetManager();manager.ShowInTaskbar=false;manager.Left=-16000;manager.Show();await Task.Delay(50);if(Find<CheckBox>(manager).First(c=>c.Content?.ToString()?.Contains("[F]")==true).IsChecked!=true)throw new InvalidOperationException("Reopened manager lost routing");manager.Close();voice.Close();
  var restarted=new MainWindow(true,"A",dataFolder){ShowInTaskbar=false,Left=-16000};if(restarted.fleetProfile.Name!="Test control saved"||restarted.ReadFleet()["F"].ControlId!=workspaceSlot)throw new InvalidOperationException("Restarted workspace lost saved receiver assignments");restarted.Close();
  var device=new RtlNative.Device(0,"Synthetic calibration","MRS","Fixture","PPM-040");RtlDevices.ItemsSource=new[]{device};RtlDevices.SelectedItem=device;ppmEnabled.IsChecked=true;ppmValue.Text="1.25";SidebarTabs.SelectedIndex=0;for(DependencyObject? ancestor=ppmValue;ancestor!=null;ancestor=LogicalTreeHelper.GetParent(ancestor))if(ancestor is Expander expander)expander.IsExpanded=true;UpdateLayout();Press(this,"Save correction");if(!File.Exists(ppmFile))throw new InvalidOperationException("Calibration Save did not create per-device settings");ppmValue.Text="0";RestorePpm();if(SelectedPpm!=1.25)throw new InvalidOperationException("Per-device PPM did not restore");receiver.Start(0,146520000,0,false,fixture:true,fixtureSeconds:3,ppm:SelectedPpm);rfMode=true;for(int i=0;i<100&&receiver.AppliedFrequency==0;i++)await Task.Delay(20);TickCalibration();if(!ppmStatus.Text.Contains("1.25 PPM"))throw new InvalidOperationException("Applied calibration missing from receiver status");SidebarTabs.SelectedIndex=0;ReceiverScroll.ScrollToBottom();UpdateLayout();capture("ui-ppm-calibration.png");Stop();
  var packet=PagingJson.Parse(System.Text.Json.JsonSerializer.Serialize(new{baud=1600,level=4,msg_type="alphanumeric",capcode=1234567,fragment="reassembled",message=@"\nMRS SYNTHETIC TEST\r\nLine two\tvalue\n",k_ok=true}),new("040","Synthetic paging",true,931000000))!;PagingPackets.Add(packet);SelectWorkspace(PagingTab);UpdateLayout();capture("ui-flex-readable.png");
  var about=new AboutWindow{Owner=this,ShowInTaskbar=false,Left=-16000};about.Show();await Task.Delay(100);Shot(about,"ui-about-components.png");about.Close();var help=new HelpWindow(Path.Combine(AppContext.BaseDirectory,"Help"),"PPM"){Owner=this,ShowInTaskbar=false,Left=-16000};help.Show();help.SearchFor("PPM");await Task.Delay(50);Shot(help,"ui-help-ppm.png");help.Close();
 }
}
