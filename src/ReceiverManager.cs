using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
 internal static Dictionary<string,ReceiverProfile> AssignReceiver(Dictionary<string,ReceiverProfile> existing,ReceiverProfile profile,string? controlId,IEnumerable<string> voices)
 {
  var all=new Dictionary<string,ReceiverProfile>(existing);
  if(existing.TryGetValue(profile.Id,out var previous)&&previous.Role=="Trunk control"&&profile.Role!="Trunk control")foreach(var v in all.Values.Where(v=>v.Id!=profile.Id&&(v.ControlId==profile.Id||previous.Group.Length>0&&v.Role=="Trunk voice"&&v.Group==previous.Group)).ToArray())all[v.Id]=v with{Role="Independent",Group="",ControlId=null};
  all[profile.Id]=profile;
  if(string.IsNullOrWhiteSpace(profile.Name)||profile.Name.Length>80||profile.Priority is <0 or >999)throw new IOException("Enter a name (1–80 characters) and priority from 0 to 999.");
  if(profile.Role=="Trunk voice"){
   if(controlId==profile.Id||controlId==null||!all.TryGetValue(controlId,out var control)||control.Role!="Trunk control")throw new IOException("Select a saved control receiver, or save that receiver as Trunk control first.");
   all[profile.Id]=profile with{ControlId=control.Id,Group=control.Group};
  }else if(profile.Role=="Trunk control"){
   var group="receiver:"+profile.Id;all[profile.Id]=profile with{Group=group,ControlId=null};var selected=voices.ToHashSet();
   foreach(var p in all.Values.ToArray()){
    if(p.Id==profile.Id)continue;
    bool linked=p.ControlId==profile.Id||(p.Role=="Trunk voice"&&p.Group.Length>0&&p.Group==profile.Group);
    if(selected.Contains(p.Id)&&p.Role=="Trunk control")throw new IOException("A control receiver cannot also be a voice receiver. Change its role first.");
    if(selected.Contains(p.Id))all[p.Id]=p with{Role="Trunk voice",Group=group,ControlId=profile.Id};
    else if(linked)all[p.Id]=p with{Role="Independent",Group="",ControlId=null};
   }
  }else all[profile.Id]=profile with{Role="Independent",Group="",ControlId=null};
  return all;
 }
 private void WriteFleet(Dictionary<string,ReceiverProfile> all){Directory.CreateDirectory(Path.GetDirectoryName(fleetFile)!);string temp=fleetFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(all,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,fleetFile,true);}
 private Dictionary<string,ReceiverProfile> CurrentFleet(){var all=ReadFleet();foreach(var w in FleetWindows())all.TryAdd(w.workspaceSlot,w.fleetProfile);return all;}
 private void ApplyFleet(Dictionary<string,ReceiverProfile> all){WriteFleet(all);foreach(var w in FleetWindows().ToArray())if(all.TryGetValue(w.workspaceSlot,out var p)&&w.fleetProfile!=p){w.StopTrunk(false);w.ReleaseFleetAssignments();w.fleetProfile=p;w.fleetPaused=false;w.ReflectFleet();}}
 private string FleetState(ReceiverProfile p){var w=FleetWindows().FirstOrDefault(w=>w.workspaceSlot==p.Id);if(w==null)return "Closed — Open receiver to select/start its SDR";if(!w.rfMode||w.receiver.AppliedFrequency==0)return "Stopped — "+(w.RtlDevices.SelectedItem is RtlNative.Device selected?selected.DisplayName+" — ":"")+"select a device and Start / Apply in its window";if(w.receiver.IsReplay)return "I/Q replay — unavailable for live following";return $"{w.receiver.ReceiverName} · {(w.RtlDevices.SelectedItem as RtlNative.Device)?.DisplayName} · {w.receiver.AppliedChannel?.Frequency/1e6:F6} MHz · "+(w.fleetOwner!=null?"Following for "+w.fleetOwner.fleetProfile.Name:w.fleetPaused?"Voice pool paused — Resume required":p.Role=="Trunk voice"?"Ready for voice assignments":p.Role=="Trunk control"?(w.trunkRadio.Running?"Control following active":"Start following in Trunk following"):"Independent");}
 private Window CreateFleetManager()
 {
  var win=new Window{Owner=this,Title="Receivers · devices and trunk assignments",Width=1020,Height=760,MinWidth=760,MinHeight=580};var root=new DockPanel{Margin=new(12)};win.Content=root;
  var heading=new TextBlock{Text="1. Add/open receivers and start their SDRs.  2. Select a control receiver.  3. Check its voice receivers and Save.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Aquamarine,Margin=new(0,0,0,8)};DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
  var list=new ListBox{DisplayMemberPath="Display",Height=130};DockPanel.SetDock(list,Dock.Top);root.Children.Add(list);
  var actions=new WrapPanel{Margin=new(0,8,0,0)};DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);
  var form=new StackPanel();root.Children.Add(new ScrollViewer{Content=form,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  var state=new TextBlock{Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,8)};form.Children.Add(state);
  var name=new TextBox{Name="ReceiverNameEdit"};var role=new ComboBox{Name="ReceiverRoleEdit",ItemsSource=new[]{"Independent","Trunk control","Trunk voice"}};var control=new ComboBox{Name="ReceiverControlEdit",DisplayMemberPath="Name"};var priority=new TextBox{Text="0"};var voiceList=new StackPanel();
  StackPanel Field(string text,UIElement c){var panel=new StackPanel();panel.Children.Add(new TextBlock{Text=text,Foreground=Brushes.Gainsboro,Margin=new(0,6,0,3)});panel.Children.Add(c);form.Children.Add(panel);return panel;}
  Field("Receiver name",name);Field("Use this receiver as",role);var controlField=Field("Control receiver",control);var priorityField=Field("Voice priority · 0 first, then higher numbers",priority);var voiceField=Field("Voice receivers assigned to this control · check then Save",voiceList);
  var note=new TextBlock{Name="AssignmentSaveStatus",Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,5)};form.Children.Add(note);var overview=new TextBlock{Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};form.Children.Add(overview);
  bool loading=false,dirty=false;string? selected=null;var checks=new Dictionary<string,CheckBox>();
  void Changed(){if(loading)return;dirty=true;note.Text="Unsaved changes — click Save assignments. Running receivers keep their current assignment until saved.";}
  void Refresh(string? id=null){loading=true;var all=CurrentFleet();list.ItemsSource=all.Values.OrderBy(p=>p.Id).ToArray();list.SelectedItem=list.Items.OfType<ReceiverProfile>().FirstOrDefault(p=>p.Id==(id??selected??workspaceSlot));loading=false;Load();}
  void Load(){if(list.SelectedItem is not ReceiverProfile p)return;loading=true;selected=p.Id;name.Text=p.Name;role.SelectedItem=p.Role;priority.Text=p.Priority.ToString();var all=CurrentFleet();control.ItemsSource=all.Values.Where(c=>c.Id!=p.Id&&c.Role=="Trunk control").OrderBy(c=>c.Name).ToArray();control.SelectedItem=control.Items.OfType<ReceiverProfile>().FirstOrDefault(c=>c.Id==p.ControlId||p.ControlId==null&&p.Group.Length>0&&c.Group==p.Group);voiceList.Children.Clear();checks.Clear();foreach(var v in all.Values.Where(v=>v.Id!=p.Id).OrderBy(v=>v.Id)){var check=new CheckBox{IsEnabled=v.Role!="Trunk control",ToolTip=v.Role=="Trunk control"?"Change this control to Independent before using it as a voice receiver.":null,Content=$"{v.Name} [{v.Id}] · {FleetState(v)}",IsChecked=v.Role=="Trunk voice"&&(v.ControlId==p.Id||v.ControlId==null&&p.Group.Length>0&&v.Group==p.Group),Foreground=Brushes.Gainsboro,Margin=new(0,3,0,3)};checks[v.Id]=check;voiceList.Children.Add(check);check.Checked+=(_,_)=>Changed();check.Unchecked+=(_,_)=>Changed();}Enable();state.Text=FleetState(p);dirty=false;loading=false;}
  void Enable(){bool voice=role.SelectedItem?.ToString()=="Trunk voice",master=role.SelectedItem?.ToString()=="Trunk control";controlField.Visibility=priorityField.Visibility=voice?Visibility.Visible:Visibility.Collapsed;voiceField.Visibility=master?Visibility.Visible:Visibility.Collapsed;}
  list.SelectionChanged+=(_,_)=>{if(loading)return;if(dirty&&MessageBox.Show(win,"Discard unsaved assignment edits and select another receiver?","Unsaved changes",MessageBoxButton.YesNo)!=MessageBoxResult.Yes){loading=true;list.SelectedItem=list.Items.OfType<ReceiverProfile>().FirstOrDefault(p=>p.Id==selected);loading=false;return;}Load();note.Text="Assignment loaded. Open receiver to select a physical SDR; saving roles does not start it.";};
  name.TextChanged+=(_,_)=>Changed();priority.TextChanged+=(_,_)=>Changed();control.SelectionChanged+=(_,_)=>Changed();role.SelectionChanged+=(_,_)=>{Enable();Changed();};
  void Button(string label,Action action){var b=new Button{Content=label,Padding=new(9,6,9,6),Margin=new(0,0,6,0)};actions.Children.Add(b);b.Click+=(_,_)=>{try{action();}catch(Exception ex){note.Foreground=Brushes.Salmon;note.Text="Not saved: "+ex.Message;}};}
  Button("Add receiver",()=>{if(dirty)throw new IOException("Save or reload your current edits first.");var all=CurrentFleet();string id=Enumerable.Range('A',26).Select(n=>((char)n).ToString()).FirstOrDefault(n=>!all.ContainsKey(n))??throw new IOException("Maximum 26 receivers.");var p=new ReceiverProfile(id,"Receiver "+id);all[id]=p;WriteFleet(all);OpenFleetReceiver(p);Refresh(id);note.Text="Receiver added. Its window is open: select family, Find, select device and Start / Apply.";});
  Button("Open receiver",()=>{if(list.SelectedItem is ReceiverProfile p)OpenFleetReceiver(p);});
  Button("Save assignments",()=>{if(list.SelectedItem is not ReceiverProfile p)throw new IOException("Select a receiver first.");if(!int.TryParse(priority.Text,out int rank))throw new IOException("Priority must be a whole number from 0 to 999.");var all=AssignReceiver(CurrentFleet(),p with{Name=name.Text.Trim(),Role=role.SelectedItem?.ToString()??"Independent",Priority=rank},(control.SelectedItem as ReceiverProfile)?.Id,checks.Where(c=>c.Value.IsChecked==true).Select(c=>c.Key));ApplyFleet(all);Refresh(p.Id);note.Foreground=Brushes.Aquamarine;note.Text="Saved assignments. They will be restored next launch. Start each SDR, then Start following on the control receiver.";});
  Button("Reload saved",()=>{dirty=false;Refresh(selected);note.Text="Reloaded saved assignments.";});
  Button("Remove receiver",()=>{if(list.SelectedItem is not ReceiverProfile p)return;if(MessageBox.Show(win,$"Remove {p.Name}? Its window/source closes. Saved settings, lists and recordings are kept.","Remove receiver",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;var all=CurrentFleet();all.Remove(p.Id);foreach(var v in all.Values.Where(v=>v.ControlId==p.Id||p.Role=="Trunk control"&&p.Group.Length>0&&v.Group==p.Group).ToArray())all[v.Id]=v with{Role="Independent",Group="",ControlId=null};ApplyFleet(all);FleetWindows().FirstOrDefault(w=>w.workspaceSlot==p.Id)?.Close();dirty=false;Refresh();note.Text="Receiver removed; saved files retained. Dependent voice receivers released.";});
  Button("Resume voice pool",()=>{if(list.SelectedItem is ReceiverProfile p&&FleetWindows().FirstOrDefault(w=>w.workspaceSlot==p.Id) is {} w){w.fleetPaused=false;note.Text="Voice pool resumed for "+p.Name+". Its SDR must be running.";}});
  Button("Help",()=>new HelpWindow(Path.Combine(AppContext.BaseDirectory,"Help"),"Receiver fleet"){Owner=win}.Show());Button("Close",()=>win.Close());
  var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};timer.Tick+=(_,_)=>{try{if(list.SelectedItem is ReceiverProfile p)state.Text=FleetState(p);overview.Text=string.Join("\n",CurrentFleet().Values.OrderBy(p=>p.Id).Select(p=>$"{p.Name} [{p.Id}] · {p.Role} · {FleetState(p)}"));}catch(Exception ex){overview.Text="Receiver status unavailable: "+ex.Message;timer.Stop();}};win.Loaded+=(_,_)=>timer.Start();win.Closed+=(_,_)=>timer.Stop();win.Closing+=(_,e)=>{if(dirty&&MessageBox.Show(win,"Close without saving assignment changes?","Unsaved changes",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)e.Cancel=true;};Refresh(workspaceSlot);return win;
 }
}
