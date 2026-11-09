using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public sealed partial class FrequencyPanel
{
 private bool syncingPolicy;
 private readonly ComboBox discoveryPolicy=new(){ItemsSource=new[]{"Auto-save: new + updates","Research: update existing only"},SelectedIndex=0,Width=215,Margin=new(0,0,4,5),Foreground=Brushes.Black,ToolTip="Research mode holds new/ambiguous site discoveries for review instead of adding frequency markers."};
 internal string EntryDescription(Guid id)
 {
  var all=RequireLibrary().Items;var item=all.FirstOrDefault(i=>i.Id==id);if(item==null)return "Entry no longer saved";var names=new List<string>();var parent=item.ParentId;for(int n=0;parent is {} p&&n<64;n++){var f=all.FirstOrDefault(i=>i.Id==p);if(f==null)break;names.Insert(0,f.Name);parent=f.ParentId;}
  return $"{item}\nFolder: {string.Join(" / ",names)}\n{item.Signaling}\nChannel IDs: {item.ChannelIds}\nEvidence: {item.EvidenceStatus}\nScope: {item.SiteScope}\nID: {item.Id}";
 }
 internal void ShowOverlappingEntries(IEnumerable<Guid> ids)
 {
  var chosen=ids.ToHashSet();var panel=new StackPanel{Margin=new(12)};var win=ToolWindow("Saved entries at this frequency",panel,920,610);panel.Children.Add(new TextBlock{Text="Entries can share an exact frequency or simply overlap at this zoom. Choose the saved entry you want to edit; full folder and identity details appear below.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});
  var list=new ListBox{Height=210,Foreground=Brushes.Black,Background=Brushes.White};panel.Children.Add(list);var details=new TextBox{Height=230,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};panel.Children.Add(details);
  void Reload(){list.ItemsSource=RequireLibrary().Items.Where(i=>chosen.Contains(i.Id)&&!i.IsFolder).ToArray();if(list.Items.Count>0)list.SelectedIndex=0;}
  list.SelectionChanged+=(_,_)=>details.Text=list.SelectedItem is FrequencyItem item?EntryDescription(item.Id):"";var row=new WrapPanel();panel.Children.Add(row);
  ToolButton(row,"Edit selected",()=>{if(list.SelectedItem is FrequencyItem item){SelectEntry(item.Id);EditSelected();Reload();}});ToolButton(row,"Show in frequency list",()=>{if(list.SelectedItem is FrequencyItem item){SelectEntry(item.Id);win.Close();}});ToolButton(row,"Tune selected",()=>{if(list.SelectedItem is FrequencyItem item)TuneRequested?.Invoke(item);});ToolButton(row,"Hide marker",()=>{if(list.SelectedItem is FrequencyItem item){RequireLibrary().SetVisibility([item.Id],false);Rebuild();Reload();Notify("Marker hidden; saved entry retained.");}});Reload();win.ShowDialog();
 }
 internal Window CreatePendingDiscoveryWindow()
 {
  var panel=new StackPanel{Margin=new(12)};var win=ToolWindow("Review held discoveries",panel,980,690);var list=new ListBox{Height=230,DisplayMemberPath="Display",Foreground=Brushes.Black,Background=Brushes.White};panel.Children.Add(new TextBlock{Text="New discoveries in research mode and ambiguous/conflicting matches are held here. No extra marker is created until you choose how to save one.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap});panel.Children.Add(list);
  var details=new TextBox{Height=130,IsReadOnly=true,TextWrapping=TextWrapping.Wrap};panel.Children.Add(details);panel.Children.Add(new TextBlock{Text="Existing saved entry to enrich",Foreground=Brushes.Gainsboro,Margin=new(0,8,0,4)});var existing=new ComboBox{Foreground=Brushes.Black};panel.Children.Add(existing);
  void Reload(){list.ItemsSource=RequireLibrary().PendingDiscoveries.ToArray();existing.ItemsSource=RequireLibrary().Items.Where(i=>!i.IsFolder).ToArray();if(list.Items.Count>0)list.SelectedIndex=0;}
  list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is PendingDiscovery p){details.Text=$"{p.Data.Protocol} · {DiscoveryNames.Frequency(p.Data.Hz)} MHz\n{p.Data.Scope}\n{p.Data.Identifiers}\n{p.Reason}";existing.SelectedItem=existing.Items.OfType<FrequencyItem>().Where(i=>FrequencyLibrary.CompatibleIdentity(i,p.Data)).MinBy(i=>Math.Abs(i.FrequencyHz-p.Data.Hz));}};
  var row=new WrapPanel();panel.Children.Add(row);ToolButton(row,"Update chosen entry",()=>{if(list.SelectedItem is PendingDiscovery p&&existing.SelectedItem is FrequencyItem item){RequireLibrary().ResolveDiscovery(p.Key,item.Id,false);Rebuild();Reload();Notify("Discovery merged into selected compatible entry.");}});ToolButton(row,"Keep as separate channel",()=>{if(list.SelectedItem is PendingDiscovery p){RequireLibrary().ResolveDiscovery(p.Key,null,true);Rebuild();Reload();Notify("Saved as a deliberately separate channel.");}});ToolButton(row,"Reload",Reload);Reload();return win;
 }
}
