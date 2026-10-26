using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public sealed class P25ActivityPanel:DockPanel
{
    private sealed class Row
    {
        public string Key="";public string GroupKey="",RadioKey="";
        public string GroupLabel{get;set;}="";public string RadioLabel{get;set;}="";
        public string Channel{get;set;}="";public string MHz{get;set;}="";public string Talkgroup{get;set;}="";public string Radio{get;set;}="";
        public string State{get;set;}="";public string Source{get;set;}="";public string Identity{get;set;}="";public string LastSeen{get;set;}="";
        public DateTimeOffset Updated;
    }
    internal DataGrid MonitorGrid=>table;
    private readonly ObservableCollection<Row> rows=[];private readonly DataGrid table=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Background=new SolidColorBrush(Color.FromRgb(16,23,34)),Foreground=Brushes.Black};
    private string? labelPath;private Dictionary<string,string> labels=new();
    internal void Initialize(string path){labelPath=path;try{if(System.IO.File.Exists(path))labels=System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(System.IO.File.ReadAllText(path))??new();}catch(Exception ex){ToolTip="Could not load channel labels: "+ex.Message;labelPath=null;}}
    public P25ActivityPanel()
    {
        Background=new SolidColorBrush(Color.FromRgb(16,23,34));
        var note=new TextBlock{Text="Latest validated grants · stale after 5 s · grant activity does not mean audio is being followed",Foreground=Brushes.LightSteelBlue,TextWrapping=System.Windows.TextWrapping.Wrap,Margin=new(5)};SetDock(note,Dock.Top);Children.Add(note);
        foreach(var (name,binding) in new[]{("System / NAC","Identity"),("Channel","Channel"),("MHz","MHz"),("Talkgroup","Talkgroup"),("TG label","GroupLabel"),("Radio ID","Radio"),("Radio label","RadioLabel"),("State","State"),("Last seen UTC","LastSeen"),("Source","Source")})table.Columns.Add(new DataGridTextColumn{Header=name,Binding=new System.Windows.Data.Binding(binding),Width=DataGridLength.Auto,IsReadOnly=binding is not("GroupLabel" or "RadioLabel")});
        table.IsReadOnly=false;
        table.CellEditEnding+=(_,e)=>{if(e.EditAction!=DataGridEditAction.Commit||e.Row.Item is not Row row||e.EditingElement is not TextBox box)return;string key=e.Column.Header.ToString()=="TG label"?row.GroupKey:row.RadioKey;if(key.Length==0)return;labels[key]=box.Text;try{if(labelPath!=null)System.IO.File.WriteAllText(labelPath,System.Text.Json.JsonSerializer.Serialize(labels));}catch(Exception ex){System.Windows.MessageBox.Show("Label save failed: "+ex.Message,"M.R.S.");}};
        table.ItemsSource=rows;Children.Add(table);
    }
    internal void Observe(PacketObservation p)
    {
        if(!p.IsValidated||p.P25Assignments==null)return;
        foreach(var a in p.P25Assignments)
        {
            var c=a.Channel;string key=$"{p.SourceKey}|{p.Replay}|{p.FrequencyHz}|{p.P25Identity}|{c.ChannelId}|{c.Slot}";
            var row=rows.FirstOrDefault(r=>r.Key==key);
            if(row==null){if(rows.Count>=500)rows.RemoveAt(0);row=new Row{Key=key};rows.Add(row);}
            row.Channel=$"{c.Identifier:00}-{c.ChannelNumber:0000}"+(c.Slot is int slot?$" / slot {slot}":"");row.MHz=c.DownlinkHz is long hz?$"{hz/1e6:F6}":"Unknown";
            row.Talkgroup=a.TalkgroupId.ToString();row.Radio=a.SourceRadioId?.ToString()??"—";row.State=a.ServiceOptions is int so?( (so&64)!=0?"Grant · encrypted":"Grant · clear flag"):"Grant · encryption unknown";
            row.Identity=p.P25Identity is {} id?$"{(id.Wacn is int w?w.ToString("X5"):"?")}-{(id.SystemId is int sy?sy.ToString("X3"):"?")} / {id.Nac:X3}":p.Callsign;
            string scope=$"{p.P25Identity?.Wacn:X5}:{p.P25Identity?.SystemId:X3}:{p.P25Identity?.Nac:X3}";
            row.GroupKey=scope+":TG:"+a.TalkgroupId;row.RadioKey=a.SourceRadioId is int rid?scope+":RID:"+rid:"";
            row.GroupLabel=labels.GetValueOrDefault(row.GroupKey,"");row.RadioLabel=labels.GetValueOrDefault(row.RadioKey,"");
            row.Source=p.Input;row.Updated=DateTimeOffset.UtcNow;row.LastSeen=p.Time;
        }
        RefreshTable();
    }
    private void RefreshTable(){try{table.Items.Refresh();}catch(InvalidOperationException){/* Preserve an active label edit; refresh on next tick. */}}
    internal void Tick(){bool changed=false;foreach(var r in rows)if((DateTimeOffset.UtcNow-r.Updated).TotalSeconds>5&&!r.State.StartsWith("Stale")){r.State="Stale · "+r.State;changed=true;}if(changed)RefreshTable();}
    internal void ResetActivity(){foreach(var r in rows)if(!r.State.StartsWith("Stale"))r.State="Stale · "+r.State;RefreshTable();}
}
