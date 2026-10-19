using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace MRS;
public partial class PacketPanel
{
    private readonly PagingRules pagingRules=new();
    internal void SetPagingRules(IEnumerable<PagingRule> rules)=>SaveRules(rules);
    internal void IncludeIgnored(bool value)=>ShowIgnored.IsChecked=value;
    internal bool PagingGrouped {get=>GroupPaging.IsChecked==true;set=>GroupPaging.IsChecked=value;}
    private bool SummaryView=>Grouped||PagingGrouped||DmrGrouped;
    internal bool DmrGrouped {get=>GroupDmr.IsChecked==true;set=>GroupDmr.IsChecked=value;}
    internal void InitializePagingRules(string path)
    {
        try{pagingRules.Load(path);}catch(Exception ex){MessageBox.Show("Paging labels/ignore rules could not be loaded: "+ex.Message,"M.R.S.");}
    }
    private string? SummaryKey(PacketObservation p)=>(p.Protocol is "DMR" or "LTR")?(DmrGrouped?DataSummaryRow.Key(p):null):PagingRules.IsPaging(p)?PagingGrouped?PagingRules.GroupKey(p):null:Grouped?DataSummaryRow.Key(p):null;
    private void PagingViewChanged(object sender,RoutedEventArgs e){if(PacketGrid?.ItemsSource!=null)RebuildSummaries();}
    private void RebuildSummaries()
    {
        summaryRows.Clear();summaryIndex.Clear();
        foreach(var row in rows)AddSummary(pagingRules.Apply(row));
        SetView();CollectionViewSource.GetDefaultView(rows).Refresh();
    }
    private void PagingActions(object sender,RoutedEventArgs e)
    {
        var grouped=PacketGrid.SelectedItem as DataSummaryRow;
        var p=grouped?.Latest??PacketGrid.SelectedItem as PacketObservation;
        var menu=new ContextMenu();
        void Action(string title,Action action,bool enabled=true){var item=new MenuItem{Header=title,IsEnabled=enabled};item.Click+=(_,_)=>action();menu.Items.Add(item);}
        bool paging=p!=null&&PagingRules.IsPaging(p);
        Action("Label / rename selected capcode(s)…",()=>EditSelected(p!,grouped?.Recipients??[p!.To],false,false),paging);
        Action("Label this exact message…",()=>EditSelected(p!,[],true,false),paging);
        Action("Ignore selected capcode(s)",()=>EditSelected(p!,grouped?.Recipients??[p!.To],false,true),paging);
        Action("Ignore this exact message",()=>EditSelected(p!,[],true,true),paging);
        menu.Items.Add(new Separator());Action("Manage labels and ignored items…",ManagePagingRules);
        menu.PlacementTarget=(FrameworkElement)sender;menu.IsOpen=true;
    }
    private void EditSelected(PacketObservation p,string[] recipients,bool message,bool ignore)
    {
        string? label=ignore?null:AskLabel();if(!ignore&&label==null)return;
        var updated=pagingRules.Items.ToList();
        foreach(var value in message?new[]{PagingRules.Payload(p)}:recipients)
        {
            if(string.IsNullOrEmpty(value))continue;
            string kind=message?"Message":"Capcode";
            var old=updated.FirstOrDefault(r=>r.Protocol==p.Protocol&&r.FrequencyHz==p.FrequencyHz&&r.Match==kind&&r.Value==value);
            if(old!=null)updated.Remove(old);
            updated.Add(new PagingRule(p.Protocol,p.FrequencyHz,kind,value,label??old?.Label??"",ignore||old?.Ignore==true));
        }
        SaveRules(updated);
    }
    private string? AskLabel()
    {
        var window=new Window{Title="Paging label — your annotation",Owner=Window.GetWindow(this),Width=440,Height=180,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};
        var panel=new StackPanel{Margin=new Thickness(14)};var input=new TextBox{Foreground=System.Windows.Media.Brushes.Black,Background=System.Windows.Media.Brushes.White,MaxLength=160,Margin=new Thickness(0,8,0,8)};
        panel.Children.Add(new TextBlock{Text="Label / source group name (does not establish transmitter identity):",TextWrapping=TextWrapping.Wrap});panel.Children.Add(input);
        var save=new Button{Content="Save label",IsDefault=true};save.Click+=(_,_)=>window.DialogResult=true;panel.Children.Add(save);window.Content=panel;window.Loaded+=(_,_)=>input.Focus();
        return window.ShowDialog()==true?input.Text.Trim():null;
    }
    private bool SaveRules(IEnumerable<PagingRule> rules)
    {
        try{pagingRules.Save(rules);for(int i=0;i<rows.Count;i++)rows[i]=pagingRules.Apply(rows[i]);RebuildSummaries();return true;}
        catch(Exception ex){MessageBox.Show("Could not save paging rules: "+ex.Message,"M.R.S.");return false;}
    }
    private sealed class RuleEditor(PagingRule rule)
    {
        public string Protocol=>rule.Protocol;
        public string Frequency=>rule.FrequencyHz is long hz?$"{hz/1e6:F6}":"Audio / no RF";
        public string Match=>rule.Match;
        public string Value=>rule.Value;
        public string Label {get;set;}=rule.Label;
        public bool Ignore {get;set;}=rule.Ignore;
        internal PagingRule Build()=>rule with{Label=Label??"",Ignore=Ignore};
    }
    private void ManagePagingRules()
    {
        var window=new Window{Title="Paging labels and ignore rules",Owner=Window.GetWindow(this),Width=900,Height=460,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var list=new ObservableCollection<RuleEditor>(pagingRules.Items.Select(r=>new RuleEditor(r)));
        var panel=new DockPanel{Margin=new Thickness(12)};var buttons=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Bottom);
        var grid=new DataGrid{Foreground=System.Windows.Media.Brushes.Black,Background=System.Windows.Media.Brushes.White,ItemsSource=list,AutoGenerateColumns=false,CanUserAddRows=false,SelectionMode=DataGridSelectionMode.Extended};
        foreach(string field in new[]{"Protocol","Frequency","Match","Value","Label"})grid.Columns.Add(new DataGridTextColumn{Header=field,Binding=new Binding(field),IsReadOnly=field!="Label",Width=field is "Value" or "Label"?new DataGridLength(1,DataGridLengthUnitType.Star):DataGridLength.Auto});
        grid.Columns.Add(new DataGridCheckBoxColumn{Header="Ignore",Binding=new Binding("Ignore")});
        var remove=new Button{Content="Remove selected"};remove.Click+=(_,_)=>{foreach(var item in grid.SelectedItems.Cast<RuleEditor>().ToArray())list.Remove(item);};
        var save=new Button{Content="Save"};save.Click+=(_,_)=>{grid.CommitEdit(DataGridEditingUnit.Cell,true);grid.CommitEdit(DataGridEditingUnit.Row,true);if(SaveRules(list.Select(r=>r.Build())))window.Close();};
        buttons.Children.Add(remove);buttons.Children.Add(save);panel.Children.Add(buttons);panel.Children.Add(grid);window.Content=panel;window.ShowDialog();
    }
}
