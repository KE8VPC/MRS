using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace MRS;
internal static class BandPlanFiles
{
    internal static readonly string[] Names=["channels.csv","bands.csv","spacing.csv"];
    internal static string CustomPath(string root,string name){if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"^[a-zA-Z0-9][a-zA-Z0-9 _-]{0,63}$")||name.EndsWith(' ')||name.EndsWith('.'))throw new IOException("Use 1–64 letters, numbers, spaces, hyphens or underscores.");string p=Path.GetFullPath(Path.Combine(root,"custom",name));if(Directory.Exists(p))throw new IOException("A custom plan with this name already exists.");return p;}
    internal static void Validate(string folder){var plans=new CsvBandPlans(folder);plans.Reload();if(plans.Error.Length>0)throw new IOException(plans.Error);if(plans.Markers.Count==0&&plans.Plans.Count==0)throw new IOException("No valid plan rows.");}
    internal static string? Hash(string path)=>File.Exists(path)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))):null;
}
internal sealed class BandPlanEditor:Window
{
    private readonly string destination;private readonly Action saved;private readonly Dictionary<string,DataTable> tables=[];private readonly Dictionary<string,string?> hashes=[];private readonly TextBlock status=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};private readonly List<DataGrid> grids=[];
    internal BandPlanEditor(string target,Dictionary<string,List<Dictionary<string,string>>> data,Action onSaved)
    {
        Background=new SolidColorBrush(Color.FromRgb(16,23,34));Foreground=Brushes.Gainsboro;destination=target;saved=onSaved;Title="Bandplan editor / import preview · "+Path.GetFileName(target);Width=1000;Height=650;MinWidth=740;MinHeight=420;var host=new DockPanel{Margin=new Thickness(12)};Content=host;var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);host.Children.Add(top);top.Children.Add(new TextBlock{Text="Edit cells, add rows at the bottom, select rows and Delete to remove. Frequencies are MHz. Save validates every row before applying. Extra imported columns are preserved.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue});top.Children.Add(status);var bottom=new WrapPanel();DockPanel.SetDock(bottom,Dock.Bottom);host.Children.Add(bottom);var save=new Button{Content="Validate and save"};bottom.Children.Add(save);var cancel=new Button{Content="Cancel"};bottom.Children.Add(cancel);cancel.Click+=(_,_)=>Close();save.Click+=(_,_)=>Save();var tabs=new TabControl();host.Children.Add(tabs);
        foreach(string name in BandPlanFiles.Names){hashes[name]=BandPlanFiles.Hash(Path.Combine(target,name));var rows=data.GetValueOrDefault(name)??[];string[] fields=name=="channels.csv"?["downlink_mhz","uplink_mhz","label","lcn","role","color","source"]:name=="bands.csv"?["start_mhz","end_mhz","label","color","source"]:["base_mhz","spacing_khz","offset_mhz","identifier","slots_per_carrier","first_channel","last_channel","label","source"];var table=new DataTable();foreach(string field in fields.Concat(rows.SelectMany(r=>r.Keys)).Distinct(StringComparer.OrdinalIgnoreCase))table.Columns.Add(field,typeof(string));foreach(var row in rows){var r=table.NewRow();foreach(var pair in row)r[pair.Key]=pair.Value;table.Rows.Add(r);}tables[name]=table;var grid=new DataGrid{ItemsSource=table.DefaultView,AutoGenerateColumns=true,CanUserAddRows=true,CanUserDeleteRows=true,SelectionUnit=DataGridSelectionUnit.FullRow,SelectionMode=DataGridSelectionMode.Extended,Foreground=Brushes.Black,Background=Brushes.White,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto};grids.Add(grid);tabs.Items.Add(new TabItem{Header=name,Content=grid});}
    }
    internal void Save()
    {
        string stage=Path.Combine(Path.GetTempPath(),"mrs-bandplan-"+Guid.NewGuid().ToString("N"));var original=new Dictionary<string,byte[]?>();try{
            foreach(var g in grids){g.CommitEdit(DataGridEditingUnit.Cell,true);g.CommitEdit(DataGridEditingUnit.Row,true);}Directory.CreateDirectory(stage);
            foreach(var pair in tables){var rows=pair.Value.Rows.Cast<DataRow>().Where(r=>r.RowState!=DataRowState.Deleted).Select(r=>pair.Value.Columns.Cast<DataColumn>().ToDictionary(c=>c.ColumnName,c=>r[c]?.ToString()??"")).Where(r=>r.Values.Any(v=>!string.IsNullOrWhiteSpace(v))).ToArray();if(rows.Length>0)PlanCsv.Write(Path.Combine(stage,pair.Key),rows,[]);}
            BandPlanFiles.Validate(stage);
            foreach(string name in BandPlanFiles.Names){string file=Path.Combine(destination,name);if(BandPlanFiles.Hash(file)!=hashes[name])throw new IOException("Plan changed outside this editor. Close and reopen to avoid overwriting newer edits.");original[name]=File.Exists(file)?File.ReadAllBytes(file):null;}
            Directory.CreateDirectory(destination);
            try{foreach(string name in BandPlanFiles.Names){string from=Path.Combine(stage,name),to=Path.Combine(destination,name);if(File.Exists(from))PlanCsv.Write(to,PlanCsv.Read(from),[]);else if(File.Exists(to))File.Delete(to);}}
            catch{foreach(var pair in original){string file=Path.Combine(destination,pair.Key);if(pair.Value==null){if(File.Exists(file))File.Delete(file);}else File.WriteAllBytes(file,pair.Value);}throw;}
            saved();Close();
        }catch(Exception ex){status.Text="Not saved: "+ex.Message;}finally{foreach(string name in BandPlanFiles.Names){string p=Path.Combine(stage,name);if(File.Exists(p))File.Delete(p);}if(Directory.Exists(stage))Directory.Delete(stage);}
    }
}
public partial class MainWindow
{
    private TabItem? bandManagerTab;
    private void ShowBandManager(){if(bandManagerTab!=null)SelectWorkspace(bandManagerTab);}
    private void AddBandManagerActions(Panel actions)
    {
        var selected=new ComboBox{Width=210,Margin=new Thickness(0,0,8,6),ToolTip="Plan to edit, duplicate, export or delete"};actions.Children.Add(selected);
        void Refresh(){selected.ItemsSource=filePlans?.Plans.ToArray();if(selected.Items.Count>0)selected.SelectedIndex=0;}
        selected.DropDownOpened+=(_,_)=>{string? old=selected.SelectedItem as string;Refresh();if(old!=null)selected.SelectedItem=old;};
        string Plan()=>selected.SelectedItem as string??throw new IOException("Select a plan first.");
        Dictionary<string,List<Dictionary<string,string>>> Load(string plan){var d=new Dictionary<string,List<Dictionary<string,string>>>();foreach(string n in BandPlanFiles.Names){string path=Path.Combine(filePlans!.Root,plan,n);if(File.Exists(path))d[n]=PlanCsv.Read(path);}return d;}
        string? NewName(){var w=new Window{Title="Custom plan name",Width=420,Height=155,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};var p=new StackPanel{Margin=new Thickness(12)};var name=new TextBox{Text="My plan",Margin=new Thickness(0,0,0,10)};p.Children.Add(name);var ok=new Button{Content="Continue",IsDefault=true};p.Children.Add(ok);ok.Click+=(_,_)=>w.DialogResult=true;w.Content=p;return w.ShowDialog()==true?name.Text.Trim():null;}
        void Edit(string path,Dictionary<string,List<Dictionary<string,string>>> rows)=>new BandPlanEditor(path,rows,()=>{ReloadFilePlans();Refresh();}){Owner=this}.ShowDialog();
        void Add(string name,Action fn){var b=new Button{Content=name,Margin=new Thickness(0,0,8,6)};actions.Children.Add(b);b.Click+=(_,_)=>{try{if(filePlans==null)return;RefreshIfEmpty();fn();}catch(Exception ex){bandStatus.Text=ex.Message;}};}
        void RefreshIfEmpty(){if(selected.ItemsSource==null)Refresh();}
        Add("New…",()=>{if(NewName() is {} name)Edit(BandPlanFiles.CustomPath(filePlans!.Root,name),new());});
        Add("Edit…",()=>{string p=Plan();if(!p.StartsWith("custom/",StringComparison.OrdinalIgnoreCase))throw new IOException("Supplied and collected plans are protected here. Duplicate to make an editable custom copy.");Edit(Path.Combine(filePlans!.Root,p),Load(p));});
        Add("Duplicate…",()=>{string p=Plan();if(NewName() is {} name)Edit(BandPlanFiles.CustomPath(filePlans!.Root,name),Load(p));});
        Add("Import CSV…",()=>{var picker=new Microsoft.Win32.OpenFileDialog{Filter="Bandplan CSV|*.csv",Multiselect=true,Title="Choose channels.csv, bands.csv and/or spacing.csv"};if(picker.ShowDialog(this)!=true)return;var rows=new Dictionary<string,List<Dictionary<string,string>>>();foreach(string file in picker.FileNames){string n=Path.GetFileName(file).ToLowerInvariant();if(!BandPlanFiles.Names.Contains(n)||rows.ContainsKey(n))throw new IOException("Choose at most one each of channels.csv, bands.csv and spacing.csv.");rows[n]=PlanCsv.Read(file);}if(NewName() is {} name)Edit(BandPlanFiles.CustomPath(filePlans!.Root,name),rows);});
        Add("Export…",()=>{var rows=Load(Plan());var picker=new Microsoft.Win32.OpenFolderDialog{Title="Choose export folder"};if(picker.ShowDialog(this)!=true)return;foreach(string name in rows.Keys)if(File.Exists(Path.Combine(picker.FolderName,name)))throw new IOException("Export folder already contains "+name+". Choose an empty folder.");foreach(var pair in rows)PlanCsv.Write(Path.Combine(picker.FolderName,pair.Key),pair.Value,[]);bandStatus.Text="Exported CSV files to "+picker.FolderName;});
        Add("Delete custom…",()=>{string p=Plan();if(!p.StartsWith("custom/",StringComparison.OrdinalIgnoreCase))throw new IOException("Only custom plans can be deleted here.");string path=Path.GetFullPath(Path.Combine(filePlans!.Root,p));if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked folders cannot be deleted here.");if(MessageBox.Show(this,"Delete custom plan "+p+"?","Delete bandplan",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;foreach(string n in BandPlanFiles.Names){string f=Path.Combine(path,n);if(File.Exists(f))File.Delete(f);}if(!Directory.EnumerateFileSystemEntries(path).Any())Directory.Delete(path);ReloadFilePlans();Refresh();});
    }
}
