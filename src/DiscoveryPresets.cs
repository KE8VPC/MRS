using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public sealed class ListeningPreset
{
 public string Name {get;set;}=""; public string Protocol {get;set;}="APRS"; public string FrequencyMHz {get;set;}="144.390000"; public string Mode {get;set;}="NFM"; public double BandwidthHz {get;set;}=16000; public string Notes {get;set;}="";
 public long Hz {get{if(!FrequencyLibrary.TryFrequency(FrequencyMHz,out long hz)||hz<=0)throw new IOException("Invalid MHz frequency for "+Name);return hz;}}
 public void Validate(){if(string.IsNullOrWhiteSpace(Name)||string.IsNullOrWhiteSpace(Protocol))throw new IOException("Name and protocol are required.");new ChannelSettings(Hz,Mode,BandwidthHz).Validate();}
 public override string ToString()=>Name+" · "+FrequencyMHz+" MHz";
}
internal static class DiscoveryReferences
{
 internal static ListeningPreset[] Read(string path)
 {
  return PlanCsv.Read(path).Select(row=>{string protocol=PlanCsv.Get(row,"protocol"),mode=PlanCsv.Get(row,"mode");if(!ReceiveDemodulator.Modes.Contains(mode))mode=protocol=="ACARS"?"AM":"NFM";
   return new ListeningPreset{Name=PlanCsv.Get(row,"label"),Protocol=protocol.Length==0?"Reference":protocol,FrequencyMHz=PlanCsv.MHz(PlanCsv.Hz(row,"downlink")!.Value),Mode=mode,BandwidthHz=double.TryParse(PlanCsv.Get(row,"bandwidth_hz"),NumberStyles.Float,CultureInfo.InvariantCulture,out double w)?w:protocol=="ACARS"?15000:protocol=="VDL2"?25000:ChannelSettings.DefaultWidth(mode),Notes=string.Join(" | ",new[]{PlanCsv.Get(row,"reference_status"),PlanCsv.Get(row,"region"),PlanCsv.Get(row,"notes"),PlanCsv.Get(row,"source")}.Where(v=>v.Length>0))};}).ToArray();
 }
 internal static void Seed(string root)
 {
  string source=Path.Combine(AppContext.BaseDirectory,"bandplans","reference");if(!Directory.Exists(source))return;
  foreach(string folder in Directory.GetDirectories(source).Where(d=>Path.GetFileName(d).StartsWith("aviation-")||Path.GetFileName(d) is "amateur-presets" or "time-stations" or "ism" or "amateur-beacons" or "hf-hfgcs"))
   foreach(string file in Directory.GetFiles(folder,"*.csv")){string to=Path.Combine(root,"reference",Path.GetFileName(folder),Path.GetFileName(file));if(Path.GetFullPath(to)==Path.GetFullPath(file)||File.Exists(to))continue;Directory.CreateDirectory(Path.GetDirectoryName(to)!);File.Copy(file,to,false);}
 }
}
public sealed partial class FrequencyLibrary
{
 internal int AddReferences(string folder,IEnumerable<ListeningPreset> presets)
 {
  var references=presets.ToArray();foreach(var entry in references)entry.Validate();var next=items.ToList();Guid? parent=null;
  foreach(string segment in folder.Split('/')){var found=next.FirstOrDefault(x=>x.IsFolder&&x.ParentId==parent&&x.Name==segment);if(found==null){found=new FrequencyItem{IsFolder=true,ParentId=parent,Name=segment};next.Add(found);}parent=found.Id;}
  int added=0;foreach(var entry in references){if(next.Any(x=>!x.IsFolder&&x.FrequencyHz==entry.Hz&&x.Mode==entry.Mode))continue;next.Add(new FrequencyItem{ParentId=parent,Name=entry.Name,FrequencyHz=entry.Hz,Mode=entry.Mode,BandwidthHz=entry.BandwidthHz,Protocols=entry.Protocol,Notes=entry.Notes});added++;}if(added>0)Commit(next);return added;
 }
}
public partial class MainWindow
{
 private readonly List<ListeningPreset> localPresets=[];private readonly List<Action> refreshPresetBars=[];
 private string PresetPath=>Path.Combine(dataFolder,"listening-presets.json");
 private void InitializeDiscoveryPresets()
 {
  try{if(File.Exists(PresetPath))localPresets.AddRange(JsonSerializer.Deserialize<List<ListeningPreset>>(File.ReadAllText(PresetPath))??[]);foreach(var p in localPresets)p.Validate();}catch(Exception ex){Log("Presets: "+ex.Message);localPresets.Clear();}
  AddPresetBar(PacketsTab,()=>"APRS");if(weakTab!=null){var refreshWeak=AddPresetBar(weakTab,()=>weakMode.SelectedItem?.ToString()??"FT8");weakMode.SelectionChanged+=(_,_)=>refreshWeak();}
 }
 private Action AddPresetBar(TabItem tab,Func<string> protocol)
 {
  var host=new DockPanel();var row=new WrapPanel{Margin=new(8,4,8,4)};DockPanel.SetDock(row,Dock.Top);host.Children.Add(row);var original=(UIElement)tab.Content;tab.Content=null;host.Children.Add(original);tab.Content=host;
  var choices=new ComboBox{Width=245,ToolTip="Receive presets; weak-signal entries are USB dial frequencies"};var hz=new TextBox{Name=ReferenceEquals(tab,PacketsTab)?"AprsPresetTarget":"WeakPresetTarget",Width=110,TextAlignment=TextAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center};var actual=new TextBlock{Foreground=Brushes.Aquamarine,VerticalAlignment=VerticalAlignment.Center,Margin=new(8,0,0,0)};
  row.Children.Add(choices);row.Children.Add(hz);void Button(string name,Action action){var b=new Button{Content=name};b.Click+=(_,_)=>action();row.Children.Add(b);}
  choices.SelectionChanged+=(_,_)=>{if(choices.SelectedItem is ListeningPreset p){hz.Text=p.FrequencyMHz;hz.ToolTip=p.Notes;}};
  Button("Tune",()=>{try{if(!FrequencyLibrary.TryFrequency(hz.Text,out long f)||f<=0)throw new IOException("Enter frequency in MHz.");var chosen=choices.SelectedItem as ListeningPreset;string mode=chosen?.Mode??(protocol()=="APRS"?"NFM":"USB");double width=chosen?.BandwidthHz??(mode=="USB"?4000:16000);if(!rfMode||receiver.IsReplay)throw new IOException("Start a live SDR first; replay/audio cannot retune hardware.");TuneSaved(new FrequencyItem{Name=protocol()+" receive preset",FrequencyHz=f,Mode=mode,BandwidthHz=width});}catch(Exception ex){actual.Text=ex.Message;}});
  Button("Edit presets",()=>EditListeningPresets());row.Children.Add(actual);
  void Refresh(){var values=new List<ListeningPreset>();string file=Path.Combine(AppContext.BaseDirectory,"bandplans","reference","amateur-presets","channels.csv");try{if(File.Exists(file))values.AddRange(DiscoveryReferences.Read(file));}catch(Exception ex){Log("Reference presets: "+ex.Message);}values.AddRange(localPresets);choices.ItemsSource=values.Where(p=>p.Protocol.Equals(protocol(),StringComparison.OrdinalIgnoreCase)||protocol()=="APRS"&&p.Protocol=="Local").ToArray();choices.SelectedIndex=choices.Items.Count>0?0:-1;}
  refreshPresetBars.Add(Refresh);Refresh();void Reflect()=>Dispatcher.BeginInvoke(()=>{if(receiver.AppliedChannel is {} ch){actual.Text=$"RX {ch.Mode} · {ch.Frequency/1e6:0.######} MHz";}});receiver.ChannelChanged+=Reflect;Reflect();return Refresh;
 }
 private void EditListeningPresets()
 {
  var entries=new System.Collections.ObjectModel.ObservableCollection<ListeningPreset>(localPresets.Select(p=>new ListeningPreset{Name=p.Name,Protocol=p.Protocol,FrequencyMHz=p.FrequencyMHz,Mode=p.Mode,BandwidthHz=p.BandwidthHz,Notes=p.Notes}));
  var grid=new DataGrid{ItemsSource=entries,AutoGenerateColumns=true,CanUserAddRows=true,CanUserDeleteRows=true,Foreground=Brushes.Black,Background=Brushes.White};var root=new DockPanel{Margin=new(10)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);top.Children.Add(new TextBlock{Text="Your receive presets. Protocol: APRS, Local, FT8, FT4 or WSPR. Mode: NFM or USB. USB frequencies are dial frequencies. Delete selects/removes a row; blank last row adds a preset.",TextWrapping=TextWrapping.Wrap});var save=new Button{Content="Save presets"};top.Children.Add(save);var status=new TextBlock{TextWrapping=TextWrapping.Wrap};top.Children.Add(status);root.Children.Add(grid);var w=new Window{Owner=this,Title="Edit local receive presets",Width=950,Height=500,Content=root};grid.AutoGeneratingColumn+=(_,e)=>{if(e.PropertyName=="Hz")e.Cancel=true;};save.Click+=(_,_)=>{try{if(!grid.CommitEdit(DataGridEditingUnit.Cell,true)||!grid.CommitEdit(DataGridEditingUnit.Row,true))throw new IOException("Finish editing the current row.");foreach(var p in entries)p.Validate();string tmp=PresetPath+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(entries,new JsonSerializerOptions{WriteIndented=true}));File.Move(tmp,PresetPath,true);localPresets.Clear();localPresets.AddRange(entries);foreach(var refresh in refreshPresetBars)refresh();w.Close();}catch(Exception ex){status.Text=ex.Message;}};w.ShowDialog();
 }
 private void ImportReferenceChannels()
 {
  try{
   if(filePlans==null)return;var picker=new Microsoft.Win32.OpenFileDialog{Title="Select reference channels.csv",InitialDirectory=filePlans.Root,Filter="Channel references|channels.csv"};if(picker.ShowDialog(this)!=true)return;
   var entries=DiscoveryReferences.Read(picker.FileName);if(entries.Length==0)throw new IOException("No channels found.");foreach(var p in entries)p.Validate();
   string dataset=Path.GetFileName(Path.GetDirectoryName(picker.FileName)!);string folder=dataset switch{"aviation-acars"=>"Aviation/ACARS","aviation-vdl2"=>"Aviation/VDL2","aviation-hf-voice"=>"Aviation/HF voice","aviation-hf-volmet"=>"Aviation/VOLMET","aviation-hf-hfdl"=>"Aviation/HFDL",_=>"References/"+dataset};
   var root=new DockPanel{Margin=new(10)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);top.Children.Add(new TextBlock{Text=$"Select channels to add to {folder}. Ctrl/Shift selects multiple. Candidate rows are initially unselected. Existing matches and labels are preserved.",TextWrapping=TextWrapping.Wrap});
   var add=new Button{Content="Add selected to frequencies"};top.Children.Add(add);var grid=new DataGrid{ItemsSource=entries,AutoGenerateColumns=true,IsReadOnly=true,SelectionMode=DataGridSelectionMode.Extended,SelectionUnit=DataGridSelectionUnit.FullRow,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White};root.Children.Add(grid);
   grid.AutoGeneratingColumn+=(_,e)=>{if(e.PropertyName=="Hz")e.Cancel=true;};foreach(var entry in entries.Where(p=>!p.Notes.Contains("unverified",StringComparison.OrdinalIgnoreCase)))grid.SelectedItems.Add(entry);
   var window=new Window{Owner=this,Title="Add reference channels",Width=1000,Height=500,Content=root};add.Click+=(_,_)=>{try{var selected=grid.SelectedItems.Cast<ListeningPreset>().ToArray();if(selected.Length==0){add.Content="Select at least one row";return;}int count=FrequencyManager.ImportReferences(folder,selected);window.Close();SidebarTabs.SelectedItem=FrequencyTab;Log($"Added {count} references to {folder}; existing matches preserved.");}catch(Exception ex){MessageBox.Show(window,ex.Message,"Reference import");}};window.ShowDialog();
  }catch(Exception ex){bandStatus.Text="Reference import: "+ex.Message;}
 }
}
