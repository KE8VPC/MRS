using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace MRS;

public partial class MainWindow
{
 private readonly LcnSurvey lcnSurvey=new();private readonly LcnSurveyRadio lcnSurveyRadio=new();private string surveyFile="";private bool surveyReadOnly,surveyTuneBusy;private string surveyLoadError="";private int surveyTuneGeneration=-1;private DateTimeOffset surveyUiAt,surveySaveAt;
 private readonly DataGrid surveyGrid=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Background=Brushes.White,Foreground=Brushes.Black,SelectionMode=DataGridSelectionMode.Single};
 private readonly TextBlock surveySummary=new(){Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap},surveyStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};
 private readonly TextBox surveyControl=new(){Width=130},surveyDetails=new(){IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Height=115,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=Brushes.Transparent,Foreground=Brushes.LightSteelBlue};
 private readonly ComboBox surveyRequired=new(){ItemsSource=Enumerable.Range(1,20),SelectedItem=3,Width=55},surveyDwell=new(){ItemsSource=new[]{3,4,6,8,10,15},SelectedItem=4,Width=55},surveyMode=new(){ItemsSource=new[]{"Discover","Follow and learn"},SelectedIndex=0,Width=160};
 private readonly ComboBox surveyEndLimit=new(){ItemsSource=new[]{5,10,20,30,60},SelectedItem=20,Width=55};private readonly CheckBox surveyWaitEnd=new(){Content="Wait for call end",IsChecked=true,Foreground=Brushes.White};
 private readonly CheckBox surveyAutoSave=new(){Content="Update saved labels / status",Foreground=Brushes.White,IsChecked=true};private readonly CheckBox surveyTentative=new(){Content="Try tentative",Foreground=Brushes.White};private TabItem? surveyTab;
 private void InitializeLcnSurvey()
 {
  var body=new DockPanel{Margin=new(8)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);body.Children.Add(top);
  var settings=new WrapPanel();top.Children.Add(settings);void Text(string text)=>settings.Children.Add(new TextBlock{Text=text,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new(4)});
  Text("Control/rest MHz");settings.Children.Add(surveyControl);Text("Calls to pass");settings.Children.Add(surveyRequired);Text("Probe seconds");settings.Children.Add(surveyDwell);settings.Children.Add(surveyMode);settings.Children.Add(surveyTentative);settings.Children.Add(surveyAutoSave);settings.Children.Add(surveyWaitEnd);Text("End limit s");settings.Children.Add(surveyEndLimit);
  var actions=new WrapPanel();top.Children.Add(actions);void Button(string name,Action action){var b=new Button{Content=name,Foreground=Brushes.Black,Padding=new(8,5,8,5),Margin=new(0,4,6,4)};actions.Children.Add(b);b.Click+=(_,_)=>{try{action();}catch(Exception ex){surveyStatus.Text=ex.Message;}};}
  Button("Start / resume discovery",StartLcnSurvey);Button("Stop / return to control",()=>StopLcnSurvey("Stopped; progress retained",true));
  Button("Save map to frequencies",()=>{if(surveyReadOnly)throw new IOException(surveyLoadError);FrequencyManager.ApplyLcnSurvey(lcnSurvey.Data,lcnSurvey.Rows(),lcnSurvey.Summary);surveyStatus.Text="Updated existing site entries and mapping status; no new frequencies created.";});
  Button("Use passed map for following",UsePassedSurveyMap);
  Button("Reject selected candidate",()=>{if(surveyGrid.SelectedItem is SurveyRow r&&r.Hz>0){lcnSurvey.Reject(r.Lcn,r.Hz);SaveLcnSurvey();RefreshSurvey();}});
  Button("Export evidence",ExportLcnSurvey);
  Button("Candidate frequencies",EditSurveyCandidates);
  top.Children.Add(surveySummary);top.Children.Add(surveyStatus);top.Children.Add(new TextBlock{Text="Select a DMR site folder in Frequencies > Discover LCN map (single receiver). One candidate is tested per assignment. Silence is inconclusive. Separate calls require an observed end/idle or changed call identity; elapsed time alone never creates a new call. Progress is saved per site and receiver. Total inventory stays unknown.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new(0,5,0,7)});
  var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);bottom.Children.Add(new TextBlock{Text="Selected mapping evidence / recent search events",Foreground=Brushes.White});bottom.Children.Add(surveyDetails);body.Children.Add(bottom);
  foreach(var (title,path) in new[]{("Carrier LCN","Lcn"),("MHz","MHz"),("Separate calls","Attempts"),("State","Status"),("Evidence","Evidence")})surveyGrid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(path),MinWidth=90,Width=path=="Evidence"?new DataGridLength(1,DataGridLengthUnitType.Star):DataGridLength.Auto});body.Children.Add(surveyGrid);surveyGrid.SelectionChanged+=(_,_)=>SurveyDetails();
  surveyTab=new(){Header="LCN discovery",Content=body};((TabControl)ProfessionalTab.Content).Items.Add(surveyTab);
  FrequencyManager.SingleReceiverResearchRequested+=(scope,entries)=>SelectLcnSurvey(scope,entries);
  receiver.RawIq+=lcnSurveyRadio.Feed;receiver.Ended+=_=>Dispatcher.BeginInvoke(()=>StopLcnSurvey("Receiver ended; progress retained"));receiver.ReplayGap+=()=>Dispatcher.BeginInvoke(()=>StopLcnSurvey("Source discontinuity; progress retained"));
  receiver.ChannelChanged+=()=>Dispatcher.BeginInvoke(()=>{if(lcnSurvey.Running&&!surveyTuneBusy&&lcnSurvey.AppliedHz>0&&receiver.AppliedChannel?.Frequency!=lcnSurvey.AppliedHz)StopLcnSurvey("Receiver tuned elsewhere; progress retained");});
  try{string last=Path.Combine(dataFolder,"lcn-discovery","last.txt");if(File.Exists(last)){string name=File.ReadAllText(last);if(name.Length==69&&name.EndsWith(".json")&&name[..64].All(Uri.IsHexDigit)){surveyFile=Path.Combine(dataFolder,"lcn-discovery",name);lcnSurvey.Load(surveyFile);ReflectSurvey();}}}catch(Exception ex){surveyReadOnly=true;surveyLoadError="Saved research left unchanged: "+ex.Message;surveyStatus.Text=surveyLoadError;}
  RefreshSurvey();
 }
 private string SurveyPath(string scope)=>Path.Combine(dataFolder,"lcn-discovery",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope))).ToLowerInvariant()+".json");
 private void SelectLcnSurvey(string scope,FrequencyItem[] entries)
 {
  if(lcnSurvey.Running||surveyTuneBusy)throw new IOException("Stop discovery and wait for return tuning before selecting another site.");SaveLcnSurvey();surveyReadOnly=false;surveyFile=SurveyPath(scope);
  try{if(File.Exists(surveyFile)){lcnSurvey.Load(surveyFile);var current=entries.Select(e=>e.FrequencyHz).Distinct().Order().ToArray();if(!current.Contains(lcnSurvey.Data.ControlHz))throw new IOException("Saved control frequency is no longer in this folder; restore it before resuming.");lcnSurvey.Data.Frequencies=current.Where(h=>!lcnSurvey.Data.Excluded.Contains(h)||h==lcnSurvey.Data.ControlHz).ToArray();lcnSurvey.MarkDirty();}
   else {long control=entries.FirstOrDefault(e=>e.ChannelRoles.Contains("Control")||e.ChannelRoles.StartsWith("Rest"))?.FrequencyHz??entries.FirstOrDefault(e=>e.FrequencyHz==tunedHz)?.FrequencyHz??entries[0].FrequencyHz;lcnSurvey.Configure(scope,control,entries.Select(e=>e.FrequencyHz));}
   Directory.CreateDirectory(Path.GetDirectoryName(surveyFile)!);File.WriteAllText(Path.Combine(Path.GetDirectoryName(surveyFile)!,"last.txt"),Path.GetFileName(surveyFile));ReflectSurvey();SaveLcnSurvey();
  }catch(Exception ex){surveyReadOnly=true;surveyLoadError="Research file preserved: "+ex.Message;surveyStatus.Text=surveyLoadError;}
  RefreshSurvey();SelectWorkspace(surveyTab!);
 }
 private void ReflectSurvey(){surveyControl.Text=PlanCsv.MHz(lcnSurvey.Data.ControlHz);surveyRequired.SelectedItem=lcnSurvey.Data.Required;surveyDwell.SelectedItem=lcnSurvey.Data.DwellSeconds;surveyMode.SelectedIndex=lcnSurvey.Data.Follow?1:0;surveyTentative.IsChecked=lcnSurvey.Data.TryTentative;surveyAutoSave.IsChecked=lcnSurvey.Data.SaveToLibrary;surveyWaitEnd.IsChecked=lcnSurvey.Data.WaitForEnd;surveyEndLimit.SelectedItem=lcnSurvey.Data.FollowSeconds;}
 private void StartLcnSurvey()
 {
  if(surveyReadOnly)throw new IOException("Research could not be loaded; original file is protected.");if(surveyTuneBusy)throw new IOException("Wait for the current return tune.");if(lcnSurvey.Running)throw new IOException("Discovery is already running; stop before changing settings.");
  if(!rfMode||receiver.IsReplay||receiver.SamplingMode!=0||receiver.AppliedChannel==null)throw new IOException("Start a live quadrature SDR before discovery.");
  if(fleetProfile.Role=="Trunk voice"||fleetOwner!=null||dmrFleetOwner!=null||fleetVoiceBusy||dmrFleetVoiceBusy)throw new IOException("Use an Independent receiver; this receiver is assigned to a voice pool.");
  if(trunkRadio.Running||dmrTrunkRadio.Running)throw new IOException("Stop trunk following on this receiver before discovery.");
  long control=CommercialParseMHz(surveyControl.Text);if(!lcnSurvey.Data.Frequencies.Contains(control)||lcnSurvey.Data.Frequencies.Any(h=>!receiver.CanTune(h)))throw new IOException("Control must be a saved site candidate, and all candidates must fit this receiver's frequency range.");
  lcnSurvey.Data.ControlHz=control;lcnSurvey.Data.Required=(int)(surveyRequired.SelectedItem??3);lcnSurvey.Data.DwellSeconds=(int)(surveyDwell.SelectedItem??4);lcnSurvey.Data.Follow=surveyMode.SelectedIndex==1;lcnSurvey.Data.TryTentative=surveyTentative.IsChecked==true;lcnSurvey.Data.SaveToLibrary=surveyAutoSave.IsChecked==true;lcnSurvey.Data.WaitForEnd=surveyWaitEnd.IsChecked==true;lcnSurvey.Data.FollowSeconds=(int)(surveyEndLimit.SelectedItem??20);
  StopFolderScan("Single-receiver LCN discovery started");StopActivity("Single-receiver LCN discovery started");Aviation.Stop("LCN discovery started");Television.StopLive("LCN discovery started");HdRecordings.StopLive("LCN discovery started");P25Packets.VoiceToggle.IsChecked=false;digitalMode.SelectedIndex=0;
  lcnSurvey.Start(DateTimeOffset.UtcNow);SaveLcnSurvey();RefreshSurvey();
 }
 private void SaveLcnSurvey(){if(surveyReadOnly||surveyFile.Length==0)return;bool changed=lcnSurvey.Dirty;lcnSurvey.Save(surveyFile);if(changed&&lcnSurvey.Data.SaveToLibrary&&lcnSurvey.Data.Observed.Count>0)FrequencyManager.ApplyLcnSurvey(lcnSurvey.Data,lcnSurvey.Rows(),lcnSurvey.Summary,false);}
 private void StopLcnSurvey(string reason,bool returnControl=false)
 {
  bool was=lcnSurvey.Running;lcnSurvey.Stop(reason);lcnSurveyRadio.Tune(0);try{SaveLcnSurvey();}catch(Exception ex){surveyStatus.Text="Progress save failed: "+ex.Message;}if(was&&returnControl&&rfMode&&!receiver.IsReplay&&!closing)_=ReturnSurveyControl(lcnSurvey.Generation);
 }
 private async Task ReturnSurveyControl(int generation){while(surveyTuneBusy&&lcnSurvey.Generation==generation)await Task.Delay(25);if(lcnSurvey.Generation!=generation||closing||!rfMode||receiver.IsReplay)return;surveyTuneBusy=true;try{await ApplySurveyChannel(lcnSurvey.Data.ControlHz);surveyStatus.Text="Stopped on control/rest; progress retained";}catch(Exception ex){surveyStatus.Text="Stopped; return failed: "+ex.Message;}finally{surveyTuneBusy=false;}}
 private async Task ApplySurveyChannel(long hz)
 {
  var channel=new ChannelSettings(hz,"NFM",12500);if(channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))receiver.SetChannel(channel);else await receiver.Retune(hz,channel);
  for(int i=0;i<250&&!closing&&rfMode&&(receiver.AppliedChannel?.Frequency!=hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth));i++)await Task.Delay(20);
  if(closing||!rfMode||receiver.AppliedChannel?.Frequency!=hz||!channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))throw new IOException("Receiver did not acknowledge tuning within five seconds.");engine.RetuneBoundary();
 }
 private async Task TuneSurvey()
 {
  surveyTuneBusy=true;int generation=lcnSurvey.Generation;surveyTuneGeneration=generation;lcnSurveyRadio.Tune(0);long hz=lcnSurvey.DesiredHz;
  try{await ApplySurveyChannel(hz);await Task.Delay(250);if(!lcnSurvey.Running||lcnSurvey.Generation!=generation)return;lcnSurvey.Applied(hz,DateTimeOffset.UtcNow);lcnSurveyRadio.Tune(hz);}
  catch(Exception ex){if(lcnSurvey.Generation==generation)StopLcnSurvey("Discovery tuning failed: "+ex.Message);}
  finally{surveyTuneBusy=false;}
 }
 private void TickLcnSurvey()
 {
  var now=DateTimeOffset.UtcNow;
  if(lcnSurvey.Running){if(!rfMode||receiver.IsReplay){StopLcnSurvey("Receiver unavailable; resume when ready");return;}if(lcnSurveyRadio.Error.Length>0){StopLcnSurvey(lcnSurveyRadio.Error);return;}
   foreach(var r in lcnSurveyRadio.Drain())lcnSurvey.Observe(r);lcnSurvey.Tick(now);
   if(lcnSurvey.Phase=="Following")foreach(var pcm in lcnSurveyRadio.DrainAudio())engine.FeedStereo(VoiceAudioGain.Apply(pcm,digitalGainDb),true);else lcnSurveyRadio.DrainAudio();
   // Enable voice decoding only after matching identity; changing target epoch discards pre-confirmation audio.
   if(lcnSurvey.Phase=="Following"&&surveyAudioGeneration!=lcnSurvey.Generation){surveyAudioGeneration=lcnSurvey.Generation;lcnSurveyRadio.Tune(lcnSurvey.AppliedHz,lcnSurvey.PendingSlot);}
   if(lcnSurvey.Running&&lcnSurvey.AppliedHz==0&&!surveyTuneBusy&&surveyTuneGeneration!=lcnSurvey.Generation)_=TuneSurvey();
  }
  if(!lcnSurvey.Running)lcnSurveyRadio.Tune(0);
  if(now>=surveySaveAt){surveySaveAt=now.AddSeconds(1);try{SaveLcnSurvey();}catch(Exception ex){StopLcnSurvey("Progress save failed: "+ex.Message);}}
  if(now>=surveyUiAt){surveyUiAt=now.AddMilliseconds(500);RefreshSurvey();}
 }
 private int surveyAudioGeneration=-1;
 private void RefreshSurvey(){surveySummary.Text=lcnSurvey.Data.Scope+" · "+lcnSurvey.Summary;surveyStatus.Text=surveyReadOnly?surveyLoadError:lcnSurvey.Phase+" · "+lcnSurvey.Message;var selected=surveyGrid.SelectedItem as SurveyRow;surveyGrid.ItemsSource=lcnSurvey.Rows();surveyGrid.SelectedItem=surveyGrid.Items.OfType<SurveyRow>().FirstOrDefault(r=>r.Lcn==selected?.Lcn&&r.Hz==selected.Hz);SurveyDetails();}
 private void SurveyDetails(){if(surveyGrid.SelectedItem is SurveyRow r)surveyDetails.Text=string.Join("\n",lcnSurvey.Data.Evidence.Where(e=>e.Lcn==r.Lcn&&e.Hz==r.Hz).OrderByDescending(e=>e.Seen).Take(30).Select(e=>$"{e.Seen:u} · {e.Result} · slot {e.Slot} · TG {e.Group} · radio {e.Radio} · CC {e.ColorCode} · call {e.Call}"));else surveyDetails.Text=string.Join("\n",lcnSurvey.Events.Reverse().Take(12));}
 private void UsePassedSurveyMap(){if(surveyReadOnly)throw new IOException(surveyLoadError);if(lcnSurvey.Running||dmrTrunkRadio.Running)throw new IOException("Stop discovery and following before changing the map.");string type=lcnSurvey.Data.Protocol;if(type is not("Capacity Plus" or "Connect Plus" or "DMR Tier III"))throw new IOException("A supported system type must first be decoded.");var rows=lcnSurvey.Rows().Where(r=>r.Status=="Passed").ToArray();if(rows.Length==0)throw new IOException("No passed mappings yet.");dmrTrunkMaps.Clear();foreach(var r in rows){var e=lcnSurvey.Data.Evidence.Last(e=>e.Lcn==r.Lcn&&e.Hz==r.Hz&&e.Result=="Match");dmrTrunkMaps.Add(new(){Lcn=r.Lcn,FrequencyMHz=r.MHz,Confirmed=true,ColorCode=e.ColorCode,Evidence=$"{r.Calls} separate calls; single-receiver discovery; "+lcnSurvey.Data.Scope});}dmrTrunkMode.SelectedItem=type;dmrTrunkControl.Text=PlanCsv.MHz(lcnSurvey.Data.ControlHz);dmrTrunkName.Text=lcnSurvey.Data.Scope;dmrTrunkNetwork.Text=SurveyId("Network");dmrTrunkSite.Text=SurveyId("Site");dmrTrunkId=Guid.NewGuid().ToString("N");dmrTrunkAllow.Text=dmrTrunkIgnore.Text=dmrTrunkHold.Text="";lock(dmrTrunkRadio.Sync)dmrTrunkRadio.Model.Labels=[];SaveDmrTrunkProfile();SelectWorkspace(dmrTrunkFollowingTab!);}
 private string SurveyId(string field){var m=System.Text.RegularExpressions.Regex.Match(lcnSurvey.Data.Scope,@"\b"+field+@" (\d+)");return m.Success?m.Groups[1].Value:"";}
 private void EditSurveyCandidates()
 {
  if(surveyReadOnly)throw new IOException(surveyLoadError);if(lcnSurvey.Running||surveyTuneBusy)throw new IOException("Stop discovery before changing candidate frequencies.");
  var all=FrequencyManager.SurveyCandidates(lcnSurvey.Data.Scope);if(all.Length==0)throw new IOException("Select a saved site folder first.");
  var body=new DockPanel{Margin=new(10)};var list=new StackPanel();var checks=new List<(long Hz,CheckBox Box)>();foreach(var item in all.GroupBy(i=>i.FrequencyHz).Select(g=>g.First()).OrderBy(i=>i.FrequencyHz)){var box=new CheckBox{Content=$"{PlanCsv.MHz(item.FrequencyHz)} MHz · {item.Name}",IsChecked=lcnSurvey.Data.Frequencies.Contains(item.FrequencyHz),Foreground=Brushes.White,Margin=new(4)};checks.Add((item.FrequencyHz,box));list.Children.Add(box);}
  var save=new Button{Content="Save candidate selection",Foreground=Brushes.Black,Margin=new(0,5,0,0)};DockPanel.SetDock(save,Dock.Bottom);body.Children.Add(save);body.Children.Add(new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});var window=new Window{Owner=this,Title="Site candidate frequencies",Content=body,Background=new SolidColorBrush(Color.FromRgb(15,23,34)),Width=700,Height=450};
  save.Click+=(_,_)=>{var selected=checks.Where(c=>c.Box.IsChecked==true).Select(c=>c.Hz).ToArray();if(selected.Length==0||!selected.Contains(lcnSurvey.Data.ControlHz)){MessageBox.Show(window,"Keep the selected control/rest frequency included.");return;}lcnSurvey.Data.Frequencies=selected;lcnSurvey.Data.Excluded=checks.Where(c=>c.Box.IsChecked!=true).Select(c=>c.Hz).ToArray();lcnSurvey.MarkDirty();SaveLcnSurvey();window.DialogResult=true;};window.ShowDialog();RefreshSurvey();
 }
 private void ExportLcnSurvey(){var dialog=new Microsoft.Win32.SaveFileDialog{Filter="Research JSON|*.json|Evidence CSV|*.csv",FileName="dmr-lcn-evidence.json"};if(dialog.ShowDialog(this)!=true)return;if(dialog.FilterIndex==1)File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(lcnSurvey.Data,new JsonSerializerOptions{WriteIndented=true}));else PlanCsv.Write(dialog.FileName,lcnSurvey.Data.Evidence.Select(e=>new Dictionary<string,string>{{"scope",lcnSurvey.Data.Scope},{"lcn",e.Lcn.ToString()},{"frequency_mhz",PlanCsv.MHz(e.Hz)},{"slot",e.Slot.ToString()},{"talkgroup",e.Group.ToString()},{"radio",e.Radio?.ToString()??""},{"color_code",e.ColorCode.ToString()},{"call",e.Call},{"seen_utc",e.Seen.ToString("O")},{"result",e.Result}}),["scope","lcn","frequency_mhz","slot","talkgroup","radio","color_code","call","seen_utc","result"]);}
}

public sealed partial class FrequencyPanel
{
 internal event Action<string,FrequencyItem[]>? SingleReceiverResearchRequested;
 internal FrequencyItem[] SurveyCandidates(string scope)=>RequireLibrary().Items.Where(i=>!i.IsFolder&&i.SiteScope==scope).ToArray();
 internal void ApplyLcnSurvey(SurveyDocument document,SurveyRow[] rows,string summary,bool notify=true)
 {
  if(!notify&&openEditors>0)return;var library=RequireLibrary();var items=library.Items.ToArray();var updates=new List<FrequencyItem>();foreach(var item in items.Where(i=>!i.IsFolder&&i.SiteScope==document.Scope)){
   var matching=rows.Where(r=>r.Hz==item.FrequencyHz&&r.Calls>0&&!r.Status.StartsWith("Rejected")).ToArray();
   var existing=item.ChannelIds.Split(", ",StringSplitOptions.RemoveEmptyEntries);bool own=item.Tags.Split(';').Any(t=>t.Trim()=="LCN survey");bool takeOwnership=own||matching.Length>0&&!existing.Any(id=>id.StartsWith("LCN ")&&!id.StartsWith("LCN ?"));if(!own&&matching.Length>0&&existing.Any(id=>id.StartsWith("LCN ")&&!id.StartsWith("LCN ?")&&!matching.Any(r=>id=="LCN "+r.Lcn)))throw new IOException("Existing confirmed LCN conflicts with research; review the saved channel before applying.");var ids=existing.Where(id=>own?!id.StartsWith("LCN "):matching.Length==0||!id.StartsWith("LCN ?")).ToList();ids.AddRange(matching.Where(r=>own||!existing.Contains("LCN "+r.Lcn)).Select(r=>"LCN "+(r.Status=="Passed"?"":"?")+r.Lcn));
   string role=document.Roles.GetValueOrDefault(item.FrequencyHz,item.ChannelRoles);string evidence=matching.Length>0?string.Join("; ",matching.Select(r=>$"{r.Status}: {r.Calls}/{r.Required} separate calls")):own?"Survey candidate rejected / no current match":item.EvidenceStatus;
   var updated=item with{ChannelIds=string.Join(", ",ids.Distinct()),ChannelRoles=role,EvidenceStatus=evidence,Tags=item.Tags+(takeOwnership&&!own?"; LCN survey":"")};if(DiscoveryNames.Generated(item.Name))updated=updated with{Name=DiscoveryNames.Label(updated)};updates.Add(updated);
  }
  var parents=items.Where(i=>!i.IsFolder&&i.SiteScope==document.Scope).Select(i=>i.ParentId).Distinct().ToHashSet();foreach(var folder in items.Where(i=>i.IsFolder&&parents.Contains(i.Id)))updates.Add(folder with{MappingStatus=summary});if(library.UpdateSurveyEntries(updates))Rebuild();if(notify)Notify("Existing site channels updated; custom names and notes retained.");
 }
}
