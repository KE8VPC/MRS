using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
internal sealed class TransmitTestWindow:Window
{
    private readonly RtlNative.Device device;private readonly string identity;
    private readonly TextBox frequency=new(){Text=""};
    private readonly ComboBox gain=new(){ItemsSource=new[]{0,4,8,12,16,20},SelectedIndex=0};
    private readonly ComboBox duration=new(){ItemsSource=new[]{1,2,3},SelectedIndex=0};
    private readonly CheckBox armed=new(){Content="Enable one test burst on my authorized frequency"};
    private readonly Button send=new(){Content="Transmit test tone"},stop=new(){Content="STOP TX",IsEnabled=false};
    private readonly TextBlock status=new(){Text="Not transmitting",TextWrapping=TextWrapping.Wrap};
    private Process? worker;
    private readonly TextBox licensedCall=new();private readonly ComboBox licenseClass=new(){ItemsSource=new[]{"Select license class","Technician","General","Advanced","Amateur Extra"},SelectedIndex=0};
    private readonly CheckBox licenseValid=new(){Content="I am the authorized control operator with a current U.S. license"},localRules=new(){Content="My location, frequency, emission and station operation comply with applicable rules"},equipment=new(){Content="I checked actual output, filtering, power limits and antenna/load; gain is not power"};
    private TxDeclaration Declaration()=>new("United States",licensedCall.Text.Trim().ToUpperInvariant(),licenseClass.SelectedItem?.ToString()??"",licenseValid.IsChecked==true,localRules.IsChecked==true,equipment.IsChecked==true);
    internal event Action<bool>? Transmitting;
    private readonly bool aprs;
    private readonly TextBox call=new(),path=new(),message=new(){MaxLength=80},latitude=new(),longitude=new();
    private readonly ComboBox kind=new(){ItemsSource=new[]{"Status","Position"},SelectedIndex=0};
    private readonly TextBox preview=new(){IsReadOnly=true,TextWrapping=TextWrapping.Wrap,MinHeight=55};
    private readonly TextBox history=new(){IsReadOnly=true,TextWrapping=TextWrapping.Wrap,Height=70,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    private readonly StackPanel packetFields=new();
    private AprsTransmit ReadPacket()
    {
        bool position=kind.SelectedIndex==1;double lat=0,lon=0;
        if(position&&(!double.TryParse(latitude.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out lat)||!double.TryParse(longitude.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out lon)))throw new ArgumentException("Enter decimal latitude and longitude (west/south negative).");
        var packet=new AprsTransmit(call.Text,path.Text,message.Text,position,lat,lon);packet.Validate();return packet;
    }
    private void PreviewPacket(){armed.IsChecked=false;try{preview.Text=ReadPacket().Preview;}catch(Exception ex){preview.Text=ex.Message;}}

    internal TransmitTestWindow(RtlNative.Device device,string identity,bool aprs=false)
    {
        this.device=device;this.identity=identity;this.aprs=aprs;Title="HackRF manual transmit test";Width=520;Height=600;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        if(aprs){Title="Manual APRS transmission";Width=600;Height=720;send.Content="Send APRS packet once";}
        var root=new DockPanel{Margin=new Thickness(20)};Content=root;
        var footer=new StackPanel();DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var panel=new StackPanel();root.Children.Add(new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        panel.Children.Add(new TextBlock{Text=aprs?"Manual APRS · AX.25 / 1200-baud AFSK":"Short FM test · 1 kHz tone · ±2.5 kHz deviation",FontWeight=FontWeights.Bold});
        panel.Children.Add(new TextBlock{Text="Reception on this HackRF is stopped. RF amplifier stays off. Use a dummy load or a suitable antenna and an authorized clear frequency. Do not cable the transmitter directly into another receiver. No automatic repeat or restart.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,12)});
        foreach(var pair in new (string,Control)[]{("Frequency MHz (144.1–148 or 430–450; emission must fit)",frequency),("TX gain dB — start at 0; not calibrated power",gain),("Duration seconds",duration)}){if(aprs&&pair.Item2==duration)continue;panel.Children.Add(new TextBlock{Text=pair.Item1});panel.Children.Add(pair.Item2);}
        if(aprs)
        {
            panel.Children.Add(new TextBlock{Text="Listen for a clear channel before opening this window. No automatic busy-channel check, beaconing, retries or acknowledgements. Blank path sends directly. Position uses the house symbol.",TextWrapping=TextWrapping.Wrap});
            panel.Children.Add(packetFields);
            foreach(var item in new (string,Control)[]{("Your callsign / SSID",call),("Path (optional, up to two comma-separated addresses)",path),("Packet type",kind),("Status text / position comment",message),("Latitude, decimal degrees (position only)",latitude),("Longitude, decimal degrees (position only)",longitude),("Packet preview",preview)}){packetFields.Children.Add(new TextBlock{Text=item.Item1});packetFields.Children.Add(item.Item2);}
            foreach(var field in new[]{call,path,message,latitude,longitude})field.TextChanged+=(_,_)=>PreviewPacket();kind.SelectionChanged+=(_,_)=>PreviewPacket();PreviewPacket();
        }
        panel.Children.Add(new TextBlock{Text="TX authorization · U.S. amateur only · operator declaration (not independently verified)",TextWrapping=TextWrapping.Wrap,FontWeight=FontWeights.Bold});
        panel.Children.Add(new TextBlock{Text="Callsign (without SSID)"});panel.Children.Add(licensedCall);panel.Children.Add(licenseClass);foreach(var c in new[]{licenseValid,localRules,equipment})panel.Children.Add(c);
        foreach(var field in new[]{frequency,licensedCall})field.TextChanged+=(_,_)=>armed.IsChecked=false;gain.SelectionChanged+=(_,_)=>armed.IsChecked=false;duration.SelectionChanged+=(_,_)=>armed.IsChecked=false;licenseClass.SelectionChanged+=(_,_)=>armed.IsChecked=false;foreach(var c in new[]{licenseValid,localRules,equipment})c.Unchecked+=(_,_)=>armed.IsChecked=false;
        footer.Children.Add(armed);footer.Children.Add(send);footer.Children.Add(stop);footer.Children.Add(status);if(aprs)footer.Children.Add(history);
        send.Click+=Send;stop.Click+=(_,_)=>StopTx();Closing+=(_,_)=>StopTx();
    }
    internal void SyntheticPreview(){call.Text="N0CALL-7";message.Text="Synthetic preview only";PreviewPacket();if(armed.IsChecked==true||!preview.Text.Contains("N0CALL-7>APZMRS:>Synthetic preview only"))throw new InvalidOperationException("APRS preview failed");}
    internal bool SafeInitialState => worker==null && armed.IsChecked!=true && frequency.Text.Length==0 && (int)gain.SelectedItem==0 && (int)duration.SelectedItem==1 && !stop.IsEnabled;
    private async void Send(object sender,RoutedEventArgs e)
    {
        if(worker!=null)return;
        try
        {
            if(armed.IsChecked!=true)throw new InvalidOperationException("Enable one burst first.");
            if(!decimal.TryParse(frequency.Text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var mhz))throw new ArgumentException("Enter frequency in MHz.");
            var plan=new TxTestPlan(checked((long)(mhz*1000000)),(int)gain.SelectedItem,aprs?3:(int)duration.SelectedItem);plan.Validate();
            AprsTransmit? packet=aprs?ReadPacket():null;if(packet!=null)preview.Text=packet.Preview;
            var declaration=Declaration();string permit=TransmitAuthorization.Issue(declaration,plan,identity);
            if(packet!=null&&!packet.Source.Split('-')[0].Equals(declaration.Callsign,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("APRS source must match the declared control-operator callsign.");
            if(!ReceiverOwnership.Acquire(this,device.Index,identity,"HackRF transmit test",out string message,"HackRF"))throw new InvalidOperationException(message);
            foreach(var c in new Control[]{licensedCall,licenseClass,licenseValid,localRules,equipment})c.IsEnabled=false;armed.IsChecked=false;packetFields.IsEnabled=false;send.IsEnabled=false;frequency.IsEnabled=gain.IsEnabled=duration.IsEnabled=armed.IsEnabled=false;stop.IsEnabled=true;
            var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var arg in new[]{aprs?"--hackrf-aprs-test":"--hackrf-tx-test",device.Index.ToString(),plan.Frequency.ToString(CultureInfo.InvariantCulture),plan.Gain.ToString(),plan.Seconds.ToString(),Convert.ToBase64String(Encoding.UTF8.GetBytes(identity))})start.ArgumentList.Add(arg);
            if(packet!=null)start.ArgumentList.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(packet))));
            start.Environment[TransmitAuthorization.Variable]=permit;status.Text="Preparing transmission…";status.Foreground=Brushes.DarkOrange;
            using var process=Process.Start(start)??throw new InvalidOperationException("Cannot start TX worker");worker=process;
            var errors=process.StandardError.ReadToEndAsync();
            var reader=Task.Run(async()=>{while(await process.StandardOutput.ReadLineAsync() is {} line){if(line=="TX_STARTED")await Dispatcher.InvokeAsync(()=>{if(process.HasExited)return;Transmitting?.Invoke(true);status.Text=aprs?$"TRANSMITTING APRS · {mhz} MHz":$"TRANSMITTING · {mhz} MHz · {plan.Seconds} second test";status.Foreground=Brushes.Red;if(packet!=null)history.AppendText($"{DateTime.Now:HH:mm:ss} TX started: {packet.Preview}\n");});else if(line=="TX_STOPPED")await Dispatcher.InvokeAsync(()=>Transmitting?.Invoke(false));}});
            try{await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(aprs?15:8));}
            catch(TimeoutException){StopTx();throw new InvalidOperationException("TX watchdog stopped the worker.");}
            await reader;string diagnostic=await errors;
            status.Text=process.ExitCode==0?"TX worker stopped. Confirm reception on your other radio; this is not RF verification.":"TX stopped / failed: "+diagnostic;
            status.Foreground=Brushes.Black;if(packet!=null)history.AppendText($"{DateTime.Now:HH:mm:ss} {status.Text}\n");
        }
        catch(Exception ex){status.Text=ex.Message;status.Foreground=Brushes.DarkRed;}
        finally{foreach(var c in new Control[]{licensedCall,licenseClass,licenseValid,localRules,equipment})c.IsEnabled=true;Transmitting?.Invoke(false);worker=null;ReceiverOwnership.Release(this);packetFields.IsEnabled=true;send.IsEnabled=true;stop.IsEnabled=false;frequency.IsEnabled=gain.IsEnabled=duration.IsEnabled=armed.IsEnabled=true;armed.IsChecked=false;}
    }
    private void StopTx()
    {
        try{if(worker is {HasExited:false} p){p.StandardInput.WriteLine("STOP");p.StandardInput.Flush();if(!p.WaitForExit(400))p.Kill(true);}}catch{}finally{Transmitting?.Invoke(false);};
    }
}
public partial class MainWindow
{
    private void OpenAprsTransmit(object sender,RoutedEventArgs e)=>OpenTransmitWindow(true);
    private void OpenTransmitTest(object sender,RoutedEventArgs e)=>OpenTransmitWindow(false);
    private void OpenTransmitWindow(bool aprs)
    {
        if(!SelectedHackRf||RtlDevices.SelectedItem is not RtlNative.Device device){RtlStatus.Text="Select and Find the HackRF first.";return;}
        string? identity=ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(),device);
        if(identity==null){RtlStatus.Text="TX test requires a uniquely identified HackRF. Find devices again.";return;}
        Stop();var dialog=new TransmitTestWindow(device,identity,aprs){Owner=this};dialog.Transmitting+=active=>{txIndicatorActive=active;RefreshRxTxIndicator();};try{dialog.ShowDialog();}finally{txIndicatorActive=false;RefreshRxTxIndicator();}
        Log("Transmit test window closed. Receiver remains stopped; Start / Apply to resume.");
    }
}
