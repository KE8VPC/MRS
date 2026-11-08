using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
namespace MRS;
public partial class PacketPanel : UserControl
{
    internal void ShowUnverified(bool value)=>ShowInvalid.IsChecked=value;
    private readonly HashSet<string> hiddenP25Types=new(StringComparer.Ordinal);
    private static readonly string[] p25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Site status","Network status","Group voice grant","Group voice grant update","Explicit group voice grant","Secondary control channels","Adjacent site status","Identifier update"];
    internal string[] HiddenP25Types
    {
        get=>hiddenP25Types.OrderBy(x=>x).ToArray();
        set {hiddenP25Types.Clear();foreach(var kind in value??[])hiddenP25Types.Add(kind);RefreshP25Types();}
    }
    private void RefreshP25Types()
    {
        if(PacketGrid?.ItemsSource==null)return;
        P25FilterButton.Content=hiddenP25Types.Count==0?"P25 message types: all":$"P25 message types: {hiddenP25Types.Count} hidden";
        CollectionViewSource.GetDefaultView(rows).Refresh();
        CollectionViewSource.GetDefaultView(summaryRows).Refresh();
    }
    private void OpenP25Types(object sender,RoutedEventArgs e)
    {
        var menu=new ContextMenu();
        var all=new MenuItem{Header="Show all P25 types"};all.Click+=(_,_)=>HiddenP25Types=[];menu.Items.Add(all);
        var activity=new MenuItem{Header="Voice grants and site/network status"};
        activity.Click+=(_,_)=>HiddenP25Types=["NID header","TSBK","Manufacturer TSBK","Protected TSBK","Identifier update","Secondary control channels","Adjacent site status"];menu.Items.Add(activity);
        menu.Items.Add(new Separator());
        foreach(var kind in p25Types.Concat(rows.Where(r=>r.Protocol=="P25 Phase I").Select(r=>r.Kind)).Concat(hiddenP25Types).Distinct())
        {
            var item=new MenuItem{Header=kind,IsCheckable=true,IsChecked=!hiddenP25Types.Contains(kind),StaysOpenOnClick=true};
            item.Click+=(_,_)=>{if(item.IsChecked)hiddenP25Types.Remove(kind);else hiddenP25Types.Add(kind);RefreshP25Types();};menu.Items.Add(item);
        }
        menu.PlacementTarget=P25FilterButton;menu.IsOpen=true;
    }
    internal DataGrid MonitorGrid=>PacketGrid;
    private readonly ObservableCollection<PacketObservation> rows=new();
    private readonly ObservableCollection<DataSummaryRow> summaryRows=new();
    private readonly Dictionary<string,DataSummaryRow> summaryIndex=new();
    internal bool Grouped {get=>GroupRepeated.IsChecked==true;set=>GroupRepeated.IsChecked=value;}
    internal event Action<bool>? EnabledChanged;
    internal event Action<bool>? P25Changed;
    internal event Action<bool>? PagingChanged;
    internal bool PagingEnabled {get=>EnablePaging.IsChecked==true;set=>EnablePaging.IsChecked=value;}
    private void PagingEnableChanged(object sender,RoutedEventArgs e)=>PagingChanged?.Invoke(PagingEnabled);
    internal void SetPagingStatus(string text)=>PagingStatus.Text=text;
    internal event Action<bool>? SubaudibleChanged;
    internal bool SubaudibleEnabled {get=>EnableSubaudible.IsChecked==true;set=>EnableSubaudible.IsChecked=value;}
    private void SubaudibleEnableChanged(object sender,RoutedEventArgs e)=>SubaudibleChanged?.Invoke(SubaudibleEnabled);
    internal void SetSubaudibleStatus(string text)=>SubaudibleStatus.Text=text;
    internal bool P25Enabled {get=>EnableP25.IsChecked==true;set=>EnableP25.IsChecked=value;}
    private void P25EnableChanged(object sender,RoutedEventArgs e)=>P25Changed?.Invoke(P25Enabled);
    internal void SetP25Status(string text)=>P25Status.Text=text;
    internal event Action? ClearRequested;
    internal bool DecoderEnabled { get=>EnableDecoder.IsChecked==true;set=>EnableDecoder.IsChecked=value; }
    internal int Count=>rows.Count;
    internal int VisibleCount=>PacketGrid.Items.Cast<object>().Count();
    internal void SetFilter(string value)=>FilterText.Text=value;
    internal void SelectFirst()=>PacketGrid.SelectedIndex=rows.Count>0?0:-1;
    internal void IncludeFailed(bool value)=>ShowInvalid.IsChecked=value;
    public PacketPanel(){InitializeComponent();detailsTimer.Tick+=(_,_)=>{if(detailsDirty&&IsVisible){detailsDirty=false;double offset=Details.VerticalOffset;ShowDetails(PacketGrid.SelectedItem);Details.ScrollToVerticalOffset(offset);}};Loaded+=(_,_)=>detailsTimer.Start();Unloaded+=(_,_)=>detailsTimer.Stop();CollectionViewSource.GetDefaultView(rows).Filter=Matches;CollectionViewSource.GetDefaultView(summaryRows).Filter=Matches;SetView();}
    private void GroupChanged(object sender,RoutedEventArgs e){if(PacketGrid?.ItemsSource!=null)RebuildSummaries();}
    private void SetView(){PacketGrid.ItemsSource=SummaryView?summaryRows:rows;PacketGrid.Columns[2].Visibility=SummaryView?Visibility.Visible:Visibility.Collapsed;}
    private bool Matches(object item)
    {
        var packet=item is DataSummaryRow grouped?grouped.Latest:(PacketObservation)item;
        if(!MatchesWeak(packet))return false;
        if(ShowIgnored.IsChecked!=true && pagingRules.Ignored(packet))return false;
        if(packet.Protocol=="P25 Phase I" && hiddenP25Types.Contains(packet.Kind))return false;
        if(ShowInvalid.IsChecked!=true && !packet.IsValidated && packet.Protocol!="CW / Morse")return false;
        string filter=FilterText.Text.Trim();
        return filter.Length==0 || $"{(item is DataSummaryRow labels?labels.SourceLabel:packet.SourceLabel)} {(item is DataSummaryRow ds?string.Join(" ",ds.Recipients):packet.To)} {packet.Protocol} {packet.Callsign} {packet.Destination} {packet.Path} {packet.Source} {packet.Information} {packet.Description}".Contains(filter,StringComparison.OrdinalIgnoreCase);
    }
    private void FilterChanged(object sender,RoutedEventArgs e){if(PacketGrid?.ItemsSource!=null){CollectionViewSource.GetDefaultView(rows).Refresh();CollectionViewSource.GetDefaultView(summaryRows).Refresh();}}
    private void EnableChanged(object sender,RoutedEventArgs e)=>EnabledChanged?.Invoke(DecoderEnabled);
    private void Clear(object sender,RoutedEventArgs e){ClearRequested?.Invoke();ClearRows();}
    internal void ClearRows(){rows.Clear();summaryRows.Clear();summaryIndex.Clear();Details.Text="Data history cleared.";}
    internal void Add(PacketObservation row)
    {
        ObserveWeak(row);row=pagingRules.Apply(row);rows.Add(row);if(rows.Count>500)rows.RemoveAt(0);
        AddSummary(row);
        if(PacketGrid.SelectedItem==null && PacketGrid.Items.Count>0)PacketGrid.SelectedIndex=0;
    }
    private bool detailsDirty;
    private readonly System.Windows.Threading.DispatcherTimer detailsTimer=new(){Interval=TimeSpan.FromMilliseconds(200)};
    private void AddSummary(PacketObservation row)
    {
        if(ShowIgnored.IsChecked!=true && pagingRules.Ignored(row))return;
        string? key=SummaryKey(row);
        if(key!=null && summaryIndex.TryGetValue(key,out var existing))
        {
            bool matched=Matches(existing);existing.Update(row);
            if(matched!=Matches(existing))CollectionViewSource.GetDefaultView(summaryRows).Refresh();
            if(ReferenceEquals(PacketGrid.SelectedItem,existing))detailsDirty=true;
        }
        else
        {
            var added=new DataSummaryRow(row);summaryRows.Add(added);if(key!=null)summaryIndex[key]=added;
            if(summaryRows.Count>500){var oldest=summaryRows.FirstOrDefault(r=>r.Latest.Protocol!="P25 Phase I"||r.Latest.Kind is not ("Site status" or "Network status" or "Identifier update"))??summaryRows.OrderBy(r=>r.Latest.ObservedUtc).First();summaryRows.Remove(oldest);string? oldKey=SummaryKey(oldest.Latest);if(oldKey!=null)summaryIndex.Remove(oldKey);}
        }
        if(PacketGrid.SelectedItem==null && PacketGrid.Items.Count>0)PacketGrid.SelectedIndex=0;
    }
    internal void SetStatus(string input,(long Good,long Bad,long Dropped) counts)=>InputStatus.Text=$"{input} · AX.25 FCS valid {counts.Good} / failed candidates {counts.Bad}"+(counts.Dropped>0?$" · queue overflow {counts.Dropped}":"");
    private void SelectionChanged(object sender,SelectionChangedEventArgs e)
    {
        ShowDetails(PacketGrid.SelectedItem);
    }
    private void ShowDetails(object? selected)
    {
        var grouped=selected as DataSummaryRow;
        var p=grouped?.Latest??selected as PacketObservation;
        if(p==null){Details.Text="Select an observation to inspect its source, decoded fields and available raw bytes.";return;}
        string raw=string.Join(" ",Enumerable.Range(0,p.RawHex.Length/2).Select(n=>p.RawHex.Substring(n*2,2)));
        string address=p.Callsign.Length>0||p.Destination.Length>0?$"\n{p.Callsign}>{p.Destination}"+(p.Path.Length>0?$" via {p.Path}":""):"";
        string summary=p.Description!=p.Information?$"\n{p.Description}":"";
        Details.Text=$"Protocol: {p.Protocol}\nObserved UTC: {p.ObservedUtc:O}\nInput: {p.Input}"+(p.FrequencyHz is long hz?$" · {hz/1000000.0:F6} MHz":"")+(p.PositionSeconds is double seconds?$" · source position ≈{seconds:F3} s":"")+address+$"\n{p.Validation} · {p.Kind}\n{p.ReadableInformation}"+summary+$"\nRAW: {(raw.Length>0?raw:"Not supplied for this metadata observation.")}";
        if(grouped!=null && PagingRules.IsPaging(p))Details.Text="Capcodes seen: "+string.Join(", ",grouped.Recipients)+"\nLabel: "+grouped.SourceLabel+"\n"+Details.Text;
        if(p.DecoderJson!=null)Details.Text+="\nDecoder fields (worker timestamp is processing time):\n"+p.DecoderJson;
        if(grouped is {Repeats:>1})Details.Text=$"First seen: {grouped.FirstSeen:O}\nLast seen: {p.ObservedUtc:O} · Received {grouped.Repeats} · Content changes {grouped.Changes}\nSummary shows latest fields/raw bytes; uncheck grouping for individual history.\n"+Details.Text;
    }
    // Keep the existing raw-observation export contract; summary totals are additional fields.
    internal string ExportJson()=>JsonSerializer.Serialize(SummaryView?CollectionViewSource.GetDefaultView(summaryRows).Cast<DataSummaryRow>().Select(r=>r.Latest with{ReceptionCount=r.Repeats,FirstSeenUtc=r.FirstSeen,ContentChanges=r.Changes,PagingRecipients=PagingRules.IsPaging(r.Latest)?r.Recipients:null,Label=r.SourceLabel}).ToArray():CollectionViewSource.GetDefaultView(rows).Cast<PacketObservation>().ToArray(),new JsonSerializerOptions{WriteIndented=true});
    private DataInspectorWindow? inspector;
    private DataInspection? inspectionReference;
    private void InspectSelected(object sender,RoutedEventArgs e)
    {
        var group=PacketGrid.SelectedItem as DataSummaryRow;
        var selected=group?.Latest??PacketGrid.SelectedItem as PacketObservation;
        if(selected==null)return;
        if(group!=null)selected=selected with{ReceptionCount=group.Repeats,FirstSeenUtc=group.FirstSeen,ContentChanges=group.Changes,Label=group.SourceLabel,PagingRecipients=PagingRules.IsPaging(selected)?group.Recipients:null};
        inspector?.Close();inspector=new DataInspectorWindow(selected,inspectionReference,value=>inspectionReference=value){Owner=Window.GetWindow(this)};inspector.Show();
    }
    private void Export(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog{Filter="Data observations JSON|*.json",FileName=$"MRS-data-{DateTime.Now:yyyyMMdd-HHmmss}.json"};
        if(dialog.ShowDialog()!=true)return;
        try{File.WriteAllText(dialog.FileName,ExportJson());}catch(Exception ex){MessageBox.Show("Data export failed: "+ex.Message,"M.R.S.");}
    }
}





