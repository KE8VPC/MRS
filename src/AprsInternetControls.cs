using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
 private readonly AprsInternet aprsInternet=new();private readonly TextBlock aprsNetStatus=new(){Foreground=Brushes.LightSteelBlue};private readonly CheckBox aprsNetEnable=new(){Content="Enable RF → APRS-IS reporting",Foreground=Brushes.Gainsboro};
 private void InitializeAprsInternet()
 {
  var call=new TextBox{Width=110};var pass=new PasswordBox{Width=100};var host=new TextBox{Width=190,Text="rotate.aprs2.net"};var port=new TextBox{Width=65,Text="14580"};string path=Path.Combine(dataFolder,"aprs-internet.json");try{if(File.Exists(path)){var s=JsonSerializer.Deserialize<AprsInternetSettings>(File.ReadAllText(path));if(s!=null){call.Text=s.Callsign;host.Text=s.Host;port.Text=s.Port.ToString();}}}catch(Exception ex){Log("APRS-IS settings: "+ex.Message);}
  var body=new StackPanel();var row=new WrapPanel();body.Children.Add(row);foreach(var pair in new (string,FrameworkElement)[]{("iGate callsign / SSID",call),("Passcode",pass),("Server",host),("Port",port)}){var box=new StackPanel{Margin=new(4)};box.Children.Add(new TextBlock{Text=pair.Item1,Foreground=Brushes.Gainsboro});box.Children.Add(pair.Item2);row.Children.Add(box);}body.Children.Add(aprsNetEnable);body.Children.Add(aprsNetStatus);body.Children.Add(new TextBlock{Text="Shares valid live APRS receptions with APRS-IS and downstream sites such as aprs.fi. Receive-only iGate: qAO; no Internet-to-RF forwarding. Passcode stays in memory. Reporting starts disabled each launch.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});
  var hostPanel=new DockPanel();var section=new Expander{Header="APRS-IS / aprs.fi reporting",Content=body,Foreground=Brushes.Gainsboro};DockPanel.SetDock(section,Dock.Top);hostPanel.Children.Add(section);var tab=(TabItem)Packets.Parent;var original=(UIElement)tab.Content;tab.Content=null;hostPanel.Children.Add(original);tab.Content=hostPanel;
  aprsNetEnable.Checked+=(_,_)=>{try{if(!rfMode||receiver.IsReplay||receiver.AppliedChannel?.Mode!="NFM")throw new IOException("Start live NFM SDR APRS reception first.");var s=new AprsInternetSettings(call.Text.Trim().ToUpperInvariant(),pass.Password.Trim(),host.Text.Trim(),int.Parse(port.Text));s.Validate();aprsInternet.Enable(s,!diagnosticWorkspace);File.WriteAllText(path,JsonSerializer.Serialize(s with{Passcode=""}));foreach(var field in new FrameworkElement[]{call,pass,host,port})field.IsEnabled=false;}catch(Exception ex){aprsNetEnable.IsChecked=false;aprsNetStatus.Text=ex.Message;}};
  aprsNetEnable.Unchecked+=(_,_)=>{aprsInternet.Disable();foreach(var field in new FrameworkElement[]{call,pass,host,port})field.IsEnabled=true;};Closed+=(_,_)=>aprsInternet.Dispose();
 }
 private void QueueAprsInternet(PacketObservation p){if(!rfMode||receiver.IsReplay||receiver.AppliedChannel?.Mode!="NFM"||p.SourceKey!=$"rf:{Interlocked.Read(ref session)}:{receiver.AppliedChannel.Frequency}")return;aprsInternet.Queue(p,DateTimeOffset.UtcNow);}
 private void TickAprsInternet(){if(aprsNetEnable.IsChecked==true&&(!rfMode||receiver.IsReplay)){aprsNetEnable.IsChecked=false;aprsNetStatus.Text="Source stopped or changed · enable reporting again";return;}aprsNetStatus.Text=aprsInternet.Status+$" · sent {aprsInternet.Sent} (server visibility unconfirmed)";}
}
