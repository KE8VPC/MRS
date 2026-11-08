using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public sealed partial class FrequencyPanel
{
 internal Func<IEnumerable<DiscoveryRecord>>? SharingCatalog {get;set;}
 private Window ToolWindow(string title,StackPanel panel,int width=900,int height=680)=>new(){Owner=Window.GetWindow(this),Title=title,Width=width,Height=height,MinWidth=650,MinHeight=450,Background=new SolidColorBrush(Color.FromRgb(15,23,35)),Foreground=Brushes.Gainsboro,Content=panel};
 private static Button ToolButton(Panel panel,string title,Action action)
 {
  var button=new Button{Content=title,Foreground=Brushes.Black,Padding=new(9,6,9,6),Margin=new(0,4,7,4)};panel.Children.Add(button);button.Click+=(_,_)=>{try{action();}catch(Exception ex){MessageBox.Show(Window.GetWindow(button),ex.Message,"Saved discoveries");}};return button;
 }
 internal Window CreateSharingWindow()
 {
  var panel=new StackPanel{Margin=new(12)};var win=ToolWindow("Saved discoveries · import / export",panel);var scope=new ComboBox{ItemsSource=new[]{"Entire library","Selected entries / folders"},SelectedIndex=0};panel.Children.Add(new TextBlock{Text="Export scope",Foreground=Brushes.Gainsboro});panel.Children.Add(scope);
  var notes=new CheckBox{Content="Include my notes in shared exports",Foreground=Brushes.Gainsboro,Margin=new(0,8,0,5)};panel.Children.Add(notes);
  var info=new TextBlock{Text="Bundles retain exact frequencies, folders, systems/sites, identifiers, talkgroups, radio labels and channel maps. Share export omits notes by default and receiver names from activity records. Review your labels before public sharing. A library backup includes notes. Credentials, GPS tracks, receiver settings and recordings are never collected.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new(0,5,0,10)};panel.Children.Add(info);
  var row=new WrapPanel();panel.Children.Add(row);var state=new TextBlock{Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap,Margin=new(0,7,0,8)};panel.Children.Add(state);
  DiscoveryBundle Bundle(bool backup=false){if(scope.SelectedIndex==1&&selection.Count==0)throw new IOException("Select an entry or folder in the frequency list first.");return RequireLibrary().ExportDiscoveries(!backup&&scope.SelectedIndex==1?selection:null,SharingCatalog?.Invoke(),notes.IsChecked==true,backup);}
  void Export(bool csv,bool catalog,bool backup){var bundle=Bundle(backup);var picker=new Microsoft.Win32.SaveFileDialog{Filter=csv?"CSV table|*.csv":"M.R.S. discovery bundle|*.mrsdiscovery|JSON document|*.json",FileName=csv?catalog?"discovery-catalog.csv":"saved-channels.csv":backup?"mrs-library-backup.mrsdiscovery":"mrs-discoveries.mrsdiscovery"};if(picker.ShowDialog(win)!=true)return;DiscoveryExchange.Write(picker.FileName,bundle,csv,catalog);state.Text=$"Exported {bundle.Channels.Count(c=>!c.IsFolder)} channels and {bundle.Records.Count} catalog records. Selected exports include only scope-linked catalog records; use Entire library for all records.";}
  ToolButton(row,"Export share bundle",()=>Export(false,false,false));ToolButton(row,"Export channels CSV",()=>Export(true,false,false));ToolButton(row,"Export catalog CSV",()=>Export(true,true,false));ToolButton(row,"Library backup",()=>Export(false,false,true));
  ToolButton(row,"Import…",()=>{var picker=new Microsoft.Win32.OpenFileDialog{Filter="Discovery bundles / JSON / CSV|*.mrsdiscovery;*.zip;*.json;*.csv"};if(picker.ShowDialog(win)!=true)return;var review=RequireLibrary().PreviewImport(DiscoveryExchange.Read(picker.FileName));ShowReview(review,"Import preview");});
  var catalogButton=ToolButton(row,"View systems / talkgroups",()=>{var b=Bundle();var content=new StackPanel{Margin=new(12)};var w=ToolWindow("Shared discovery catalog",content);content.Children.Add(new TextBlock{Text="Saved/imported catalog plus current receiver discoveries. Import does not start decoders or configure trunk following.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});var grid=ReviewGrid(b.Records.Select(r=>new{r.Kind,r.Scope,r.Identifier,r.Label,r.Protocol,Details=string.Join("; ",r.Fields.Select(k=>k.Key+"="+k.Value))}));grid.Height=500;content.Children.Add(grid);w.ShowDialog();});
  panel.Children.Add(new TextBlock{Text="CSV can be edited in a spreadsheet. Keep ID, parent ID, scope and JSON metadata columns intact to preserve relationships. Channel CSV and catalog CSV can be imported separately. The bundle is a ZIP with a versioned JSON document and both CSV tables.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new(0,15,0,0)});return win;
 }
 private static DataGrid ReviewGrid(object rows)=>new(){ItemsSource=(System.Collections.IEnumerable)rows,AutoGenerateColumns=true,IsReadOnly=true,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new(0,8,0,6)};
 internal Window CreateCleanupWindow()
 {
  var panel=new StackPanel{Margin=new(12)};var win=ToolWindow("Clean up saved discoveries",panel);var scope=new ComboBox{ItemsSource=new[]{"Entire library","Selected entries / folders"},SelectedIndex=selection.Count>0?1:0};panel.Children.Add(new TextBlock{Text="Cleanup scope",Foreground=Brushes.Gainsboro});panel.Children.Add(scope);var rename=new CheckBox{Content="Replace generic automatic names with known identifiers",IsChecked=true,Foreground=Brushes.Gainsboro};var organize=new CheckBox{Content="Organize entries with a known site scope into system/site folders",IsChecked=true,Foreground=Brushes.Gainsboro};var merge=new CheckBox{Content="Merge exact, compatible frequency/scope duplicates",IsChecked=true,Foreground=Brushes.Gainsboro};foreach(var box in new[]{rename,organize,merge}){box.Margin=new(0,4,0,5);panel.Children.Add(box);}
  var spacing=new ComboBox{ItemsSource=new[]{"Keep exact frequencies","500 Hz","1 kHz","2.5 kHz","5 kHz","6.25 kHz","12.5 kHz","25 kHz"},SelectedIndex=0};panel.Children.Add(new TextBlock{Text="Optional channel grid — choose only when appropriate for these channels",Foreground=Brushes.Gainsboro,Margin=new(0,12,0,4)});panel.Children.Add(spacing);
  var tolerance=new TextBox{Text="500",Margin=new(0,4,0,5)};panel.Children.Add(new TextBlock{Text="Maximum correction (Hz); never more than half the selected spacing",Foreground=Brushes.Gainsboro});panel.Children.Add(tolerance);
  panel.Children.Add(new TextBlock{Text="No change is made until you approve the preview. Custom labels/notes and conflicting duplicates stay intact. Unknown entries and empty folders are retained. Snapping uses the selected zero-origin grid; it is not automatic protocol identification. Original frequency is retained when corrected. A timestamped library backup is created before applying; Undo restores this session's last batch.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,8)});
  ToolButton(panel,"Preview cleanup",()=>{int[] steps=[0,500,1000,2500,5000,6250,12500,25000];if(scope.SelectedIndex==1&&selection.Count==0)throw new IOException("Select entries or a folder first.");if(!int.TryParse(tolerance.Text,out int allowed))throw new IOException("Enter a whole-number tolerance in Hz.");ShowReview(RequireLibrary().PreviewCleanup(rename.IsChecked==true,organize.IsChecked==true,merge.IsChecked==true,steps[spacing.SelectedIndex],allowed,scope.SelectedIndex==1?selection:null),"Cleanup preview");});return win;
 }
 internal void ShowReview(LibraryReview plan,string title)
 {
  var panel=new StackPanel{Margin=new(12)};var win=ToolWindow(title,panel);panel.Children.Add(new TextBlock{Text=plan.Summary+"\nConflicts retain existing values; skipped records are listed below. This preview does not tune a receiver.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Aquamarine});var grid=ReviewGrid(plan.Changes);grid.Height=450;panel.Children.Add(grid);var row=new WrapPanel();panel.Children.Add(row);
  var apply=ToolButton(row,"Apply reviewed changes",()=>{string backup=RequireLibrary().ApplyReview(plan);Rebuild();Notify(plan.Summary+" · Backup: "+backup);win.DialogResult=true;});apply.IsEnabled=plan.Changes.Any(c=>c.Action is "Add" or "Update" or "Merge");ToolButton(row,"Export review CSV",()=>{var picker=new Microsoft.Win32.SaveFileDialog{Filter="CSV|*.csv",FileName="discovery-review.csv"};if(picker.ShowDialog(win)==true)PlanCsv.Write(picker.FileName,plan.Changes.Select(c=>new Dictionary<string,string>{{"action",c.Action},{"entry",c.Entry},{"detail",c.Detail}}),["action","entry","detail"]);});ToolButton(row,"Cancel",()=>win.Close());win.ShowDialog();
 }
}
public partial class MainWindow
{
 private IEnumerable<DiscoveryRecord> DiscoveryCatalog()
 {
  talkgroupGrid.CommitEdit(DataGridEditingUnit.Cell,true);talkgroupGrid.CommitEdit(DataGridEditingUnit.Row,true);
  string SharedScope(string source){var m=System.Text.RegularExpressions.Regex.Match(source,@"^(.*):NET (\d+)$");return m.Success?"DMR:"+m.Groups[1].Value+":Network "+m.Groups[2].Value:source;}
  foreach(var r in talkgroupRows)yield return DiscoveryRecord.Create("talkgroup",SharedScope(r.System),r.Talkgroup.ToString(System.Globalization.CultureInfo.InvariantCulture),r.Label,r.System.StartsWith("P25")?"P25":"DMR",new(){{"last_seen_utc",r.Seen.ToString("O")},{"observations",r.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)}});
  foreach(var saved in trunkSaved.Values){var p=saved.Profile;string scope="Profile:"+p.Id;yield return DiscoveryRecord.Create("system",scope,p.Id,p.Name,p.Protocol,new(){{"control_frequency_hz",p.ControlHz.ToString()},{"network_id",p.NetworkId?.ToString()??""},{"site_id",p.SiteId?.ToString()??""}});
   foreach(var c in p.Carriers)yield return DiscoveryRecord.Create("channel_map",scope,c.Lcn.ToString(),"",p.Protocol,new(){{"frequency_hz",c.Hz.ToString()},{"confirmed",c.Confirmed.ToString()},{"color_code",c.ColorCode?.ToString()??""},{"evidence",c.Evidence}});
   foreach(var label in saved.Labels??[])yield return DiscoveryRecord.Create("talkgroup",scope,label.Key.ToString(),label.Value,p.Protocol);
  }
 }
}
