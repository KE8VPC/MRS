using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
internal sealed record FrequencyCalibration(bool Enabled=false,double Ppm=0)
{
 internal double Active=>Enabled?Ppm:0;
 internal static long HardwareHz(long nominal,double ppm){if(!double.IsFinite(ppm)||Math.Abs(ppm)>200)throw new IOException("Correction must be between -200 and +200 PPM.");return checked((long)Math.Round(nominal*(1+ppm/1e6),MidpointRounding.AwayFromZero));}
}
public partial class MainWindow
{
 private readonly CheckBox ppmEnabled=new(){Content="Enable RX frequency correction",Foreground=Brushes.Gainsboro};private readonly TextBox ppmValue=new(){Text="0",Width=90};private readonly TextBlock ppmStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};private Dictionary<string,FrequencyCalibration> ppmSaved=[];private string ppmFile="";
 private string? PpmKey=>RtlDevices.SelectedItem is RtlNative.Device d?ReceiverIdentity.UniqueKey(RtlDevices.Items.Cast<RtlNative.Device>(),d):null;
 private double SelectedPpm{get{if(!double.TryParse(ppmValue.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||!double.IsFinite(value)||Math.Abs(value)>200)throw new IOException("Enter signed PPM from -200 to +200, using a decimal point.");return ppmEnabled.IsChecked==true?value:0;}}
 private void InitializeCalibration(){ppmFile=Path.Combine(dataFolder,"frequency-calibration.json");try{if(File.Exists(ppmFile))ppmSaved=JsonSerializer.Deserialize<Dictionary<string,FrequencyCalibration>>(File.ReadAllText(ppmFile))??[];}catch(Exception ex){Log("PPM settings could not be loaded: "+ex.Message);ppmFile="";}
  var content=new StackPanel();var row=new WrapPanel();row.Children.Add(ppmEnabled);row.Children.Add(ppmValue);row.Children.Add(new TextBlock{Text=" PPM",Foreground=Brushes.Gainsboro});content.Children.Add(row);var actions=new WrapPanel();content.Children.Add(actions);
  void Button(string text,Action action){var b=new Button{Content=text,Padding=new(8,4,8,4)};actions.Children.Add(b);b.Click+=(_,_)=>{try{action();}catch(Exception ex){ppmStatus.Text=ex.Message;}};}
  Button("Save correction",()=>{if(rfMode)throw new IOException("Stop the receiver before changing calibration; recordings and following must finish first.");_=SelectedPpm;if(PpmKey is not {} key)throw new IOException("This device lacks a unique serial. Correction can be used this session; it cannot be safely saved by identity.");if(ppmFile.Length==0)throw new IOException("Existing calibration file could not be read; original left unchanged.");ppmSaved[key]=new(ppmEnabled.IsChecked==true,double.Parse(ppmValue.Text,CultureInfo.InvariantCulture));string temp=ppmFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(ppmSaved,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,ppmFile,true);ppmStatus.Text="Saved for this device. Start / Apply to use it. RX only; TX settings are unchanged.";});
  Button("Reset to zero",()=>{if(rfMode)throw new IOException("Stop the receiver first.");ppmEnabled.IsChecked=false;ppmValue.Text="0";ppmStatus.Text="Reset in editor. Save correction to retain zero for this device.";});Button("Help",()=>new HelpWindow(Path.Combine(AppContext.BaseDirectory,"Help"),"PPM"){Owner=this}.Show());content.Children.Add(ppmStatus);var startRow=(FrameworkElement)StartRtlButton.Parent;var panel=(Panel)startRow.Parent;panel.Children.Insert(panel.Children.IndexOf(startRow),new Expander{Header="Frequency calibration / PPM",Content=content,Foreground=Brushes.Gainsboro});
  RtlDevices.SelectionChanged+=(_,_)=>RestorePpm();ppmValue.TextChanged+=(_,_)=>{if(ready)ppmStatus.Text="Edited — Stop, Save correction, then Start / Apply. Positive PPM increases the hardware tune command.";};ppmEnabled.Checked+=(_,_)=>{};RestorePpm();
 }
 private void RestorePpm(){var c=PpmKey is {} key&&ppmSaved.TryGetValue(key,out var saved)?saved:new FrequencyCalibration();ppmEnabled.IsChecked=c.Enabled;ppmValue.Text=c.Ppm.ToString("0.######",CultureInfo.InvariantCulture);ppmStatus.Text=PpmKey==null?"No unique device identity — session correction only.":"Device correction loaded. Stop before editing; Start / Apply applies it.";}
 private void TickCalibration(){ppmValue.IsEnabled=ppmEnabled.IsEnabled=!rfMode&&!receiver.IsReplay;if(rfMode&&!receiver.IsReplay)ppmStatus.Text=$"Applied RX correction {receiver.AppliedPpm:+0.######;-0.######;0} PPM · nominal capture {receiver.AppliedFrequency/1e6:F6} MHz · hardware command {FrequencyCalibration.HardwareHz(receiver.AppliedFrequency,receiver.AppliedPpm)/1e6:F6} MHz. Sample clock is not resampled.";else if(receiver.IsReplay)ppmStatus.Text="Recorded I/Q — live calibration is not reapplied.";}
}
