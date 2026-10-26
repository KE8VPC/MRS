using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
namespace MRS;
internal sealed record HelpTopic(string Title,string Text,string Path);
internal static class HelpTopics
{
    internal static List<HelpTopic> Load(string root){var list=new List<HelpTopic>();if(Directory.Exists(root))foreach(string file in Directory.EnumerateFiles(root,"*.md").Take(200)){if(new FileInfo(file).Length>256000)continue;string text=File.ReadAllText(file);list.Add(new(text.Split('\n').FirstOrDefault(l=>l.StartsWith("# "))?.TrimStart('#',' ').Trim()??System.IO.Path.GetFileNameWithoutExtension(file),text,file));}return list.OrderBy(t=>t.Title).ToList();}
    internal static IEnumerable<HelpTopic> Search(IEnumerable<HelpTopic> topics,string query){var terms=query.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);return topics.Where(t=>terms.All(w=>(t.Title+"\n"+t.Text).Contains(w,StringComparison.OrdinalIgnoreCase))).OrderByDescending(t=>terms.Count(w=>t.Title.Contains(w,StringComparison.OrdinalIgnoreCase))).ThenBy(t=>t.Title);}
}
internal sealed class HelpWindow:Window
{
    private readonly TextBox search=new(){Padding=new Thickness(6),ToolTip="Search keywords or function names; all words must match"};private readonly ListBox list=new(){DisplayMemberPath="Title",MinWidth=210};private readonly FlowDocumentScrollViewer document=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto};private readonly List<HelpTopic> topics;
    internal HelpWindow(string root,string context)
    {
        Background=new SolidColorBrush(Color.FromRgb(16,23,34));Foreground=Brushes.Gainsboro;Title="M.R.S. Help · offline";Width=1000;Height=720;MinWidth=720;MinHeight=450;topics=HelpTopics.Load(root);var titleFactory=new FrameworkElementFactory(typeof(TextBlock));titleFactory.SetBinding(TextBlock.TextProperty,new Binding("Title"));titleFactory.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);titleFactory.SetValue(TextBlock.MaxWidthProperty,230.0);list.DisplayMemberPath="";list.ItemTemplate=new DataTemplate{VisualTree=titleFactory};list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);var grid=new Grid{Margin=new Thickness(12)};grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=new GridLength(260)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(8)});grid.ColumnDefinitions.Add(new());Content=grid;Grid.SetColumnSpan(search,3);grid.Children.Add(search);Grid.SetRow(list,1);grid.Children.Add(list);Grid.SetRow(document,1);Grid.SetColumn(document,2);grid.Children.Add(document);search.TextChanged+=(_,_)=>{list.ItemsSource=HelpTopics.Search(topics,search.Text).ToList();list.SelectedIndex=0;if(list.Items.Count==0)Render("# No matching topics\nTry a shorter keyword, or clear the search to browse all topics.");};list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is HelpTopic t)Render(t.Text);};list.ItemsSource=topics;list.SelectedItem=topics.FirstOrDefault(t=>t.Text.Contains("Context: "+context,StringComparison.OrdinalIgnoreCase))??topics.FirstOrDefault();if(topics.Count==0)Render("# Help files missing\nKeep the Help folder beside MRS.exe. Re-extract the complete release ZIP.");Loaded+=(_,_)=>search.Focus();
    }
    internal void SearchFor(string query)=>search.Text=query;
    internal string SelectedTitle=>(list.SelectedItem as HelpTopic)?.Title??"";
    private void Render(string text){var d=new FlowDocument{Foreground=Brushes.Gainsboro,Background=new SolidColorBrush(Color.FromRgb(16,23,34)),FontFamily=new FontFamily("Segoe UI"),FontSize=15,PagePadding=new Thickness(18)};foreach(string line in text.Replace("\r","").Split('\n')){if(line.StartsWith("Context:")||line.StartsWith("Keywords:"))continue;var p=new Paragraph(new Run(line.TrimStart('#',' '))){Margin=new Thickness(0,0,0,8)};if(line.StartsWith('#')){p.FontWeight=FontWeights.SemiBold;p.FontSize=line.StartsWith("# ")?23:18;p.Foreground=Brushes.Aquamarine;}d.Blocks.Add(p);}document.Document=d;}
}
public partial class MainWindow
{
    private void OpenHelp(){string context=BroadcastTab.IsSelected?(FmTab.IsSelected?"FM":AmTab.IsSelected?"AM":"Broadcast"):ProfessionalTab.IsSelected?(DmrTab.IsSelected?"NXDN":P25SystemsTab.IsSelected?"P25":"Paging"):WorkspaceTabs.SelectedItem is TabItem t?t.Header?.ToString()??"Overview":"Overview";if(SidebarTabs.IsKeyboardFocusWithin&&SidebarTabs.SelectedItem is TabItem sidebar)context=sidebar.Header?.ToString()??context;new HelpWindow(Path.Combine(AppContext.BaseDirectory,"Help"),context){Owner=this}.Show();}
    private void InitializeHelp(){PreviewKeyDown+=(_,e)=>{if(e.Key==Key.F1){e.Handled=true;OpenHelp();}};}
}
