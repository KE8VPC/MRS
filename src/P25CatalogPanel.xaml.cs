using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
namespace MRS;
public partial class P25CatalogPanel : UserControl
{
    private P25Catalog live=new(),replay=new();
    internal event Action<FrequencyItem>? SaveFrequencyRequested;
    private P25Catalog Current=>SourceChoice.SelectedIndex==1?replay:live;
    internal int LiveCount=>live.Rows.Count;
    internal int ReplayCount=>replay.Rows.Count;
    public P25CatalogPanel(){InitializeComponent();SetView();}
    internal void Initialize(string path){live=new P25Catalog(path);SetView();}
    internal void Observe(PacketObservation observation)
    {
        (observation.Replay?replay:live).Observe(observation);
        if(Search.Text.Length>0)CollectionViewSource.GetDefaultView(Current.Rows).Refresh();
        if(Entries.SelectedItem!=null){double scroll=Details.VerticalOffset;ShowDetails();Details.ScrollToVerticalOffset(scroll);}
    }
    internal void Tick(){live.Save();UpdateStatus();}
    internal void Save()=>live.Save(true);
    internal void AssertReadable(){if(Entries.Columns[0].ActualWidth<150||Entries.Columns[3].ActualWidth<90)throw new InvalidOperationException("Discovery columns collapsed.");}
    internal void ShowReplay(){SourceChoice.SelectedIndex=1;}
    internal void CheckActions(bool requestFrequency=false)
    {
        Entries.SelectedItem=Current.Rows.FirstOrDefault();
        if(Entries.SelectedItem!=null&&!EditNotesButton.IsEnabled)throw new InvalidOperationException("Discovery annotation action is disabled for a selection.");
        if(SourceChoice.SelectedIndex==1&&SaveFrequencyButton.IsEnabled)throw new InvalidOperationException("Replay discovery offered live frequency saving.");
        if(Entries.SelectedItem is P25DiscoveryRow row&&SaveFrequencyButton.IsEnabled!=P25FrequencyDraft.CanCreate(row.Value))throw new InvalidOperationException("Discovery frequency action has incorrect availability.");
        if(requestFrequency)SaveFrequencyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
    private void SetView()
    {
        if(Entries==null)return;Entries.ItemsSource=Current.Rows;CollectionViewSource.GetDefaultView(Current.Rows).Filter=Matches;UpdateStatus();
    }
    private bool Matches(object row)
    {
        var entry=(P25DiscoveryRow)row;
        return $"{entry.System} {entry.Site} {entry.Category} {entry.Label} {entry.Value.Label} {entry.Value.Notes} {entry.Frequency} {entry.Value.Observation.Source} {entry.Value.Observation.Information}".Contains(Search.Text.Trim(),StringComparison.OrdinalIgnoreCase);
    }
    private void UpdateStatus()=>Status.Text=$"{Current.Rows.Count} discoveries · {(SourceChoice.SelectedIndex==1?"Replay is not saved":"Live catalog saved per receiver")} · Last observed, not live activity. Provisional identities remain separate."+(Current.Error is string error?"\n"+error:"");
    private void SourceChanged(object sender,SelectionChangedEventArgs e)=>SetView();
    private void SearchChanged(object sender,TextChangedEventArgs e){if(Entries?.ItemsSource!=null)CollectionViewSource.GetDefaultView(Current.Rows).Refresh();}
    private void SelectedChanged(object sender,SelectionChangedEventArgs e)=>ShowDetails();
    private void EditNotes(object sender,RoutedEventArgs e)
    {
        if(Entries.SelectedItem is not P25DiscoveryRow row)return;
        var store=Current;var key=row.Value.Key;
        var editor=new P25DiscoveryEditor(row.Value){Owner=Window.GetWindow(this)};
        if(editor.ShowDialog()!=true||editor.Result is not { } result)return;
        store.Annotate(key,result.Name,result.Notes);store.Save(true);
        CollectionViewSource.GetDefaultView(store.Rows).Refresh();ShowDetails();UpdateStatus();
    }
    private void SaveFrequency(object sender,RoutedEventArgs e)
    {
        if(Entries.SelectedItem is P25DiscoveryRow row&&P25FrequencyDraft.CanCreate(row.Value))SaveFrequencyRequested?.Invoke(P25FrequencyDraft.Create(row.Value));
    }
    private void ShowDetails()
    {
        if(EditNotesButton!=null)EditNotesButton.IsEnabled=Entries.SelectedItem is P25DiscoveryRow;
        if(SaveFrequencyButton!=null)SaveFrequencyButton.IsEnabled=Entries.SelectedItem is P25DiscoveryRow selected&&P25FrequencyDraft.CanCreate(selected.Value);
        if(Entries.SelectedItem is not P25DiscoveryRow row){Details.Text="Select a discovery. These are last-observed announcements, not a live-call or site-reachability indicator.";return;}
        var value=row.Value;var observation=value.Observation;
        Details.Text=$"{row.System} · RFSS / Site: {row.Site}\n{row.Category}: {row.Label}\nDecoded label: {value.Label}\nNotes: {value.Notes}\nFrequency: {row.Frequency} MHz\nFirst seen: {value.FirstSeen:O}\nLast seen: {value.LastSeen:O} · Announcements: {value.Receptions}\nSource: {observation.Input} · monitored {observation.FrequencyHz/1000000m:F6} MHz\n{observation.Information}\nValidation: {observation.Validation}\nLatest raw: {observation.RawHex}";
    }
    private void ExportClick(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog{Filter="P25 discovery catalog JSON|*.json",FileName=$"MRS-p25-discoveries-{DateTime.Now:yyyyMMdd-HHmmss}.json"};
        if(dialog.ShowDialog()!=true)return;
        try{File.WriteAllText(dialog.FileName,Current.Export());}catch(Exception ex){MessageBox.Show("Discovery export failed: "+ex.Message,"M.R.S.");}
    }
}


