using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace MRS;
internal sealed class NxdnRow
{
    internal bool Stored;internal NxdnReception Last=null!;internal DateTimeOffset First;
    public string MHz=>PlanCsv.MHz(Last.Context.FrequencyHz);public int System=>Last.Identity.System;public int Site=>Last.Identity.Site;public int RAN=>Last.Identity.Ran;public string Location=>Last.Identity.Location;
    public int Count{get;set;}public string LastSeen=>Last.Seen.ToString("HH:mm:ss 'UTC'");public string Source=>Last.Context.Replay?"Replay · "+Last.Context.Key:"Live · "+Last.Context.Key;public string Status=>Count>=3?"Confirmed control data":"Pending repeat confirmation";
}
public partial class MainWindow
{
    private readonly ObservableCollection<NxdnRow> nxdnRows=[];
    private readonly DataGrid nxdnGrid=new(){IsReadOnly=true,AutoGenerateColumns=false,MinHeight=100,MaxHeight=220,Background=Brushes.White,Foreground=Brushes.Black};
    private readonly CheckBox nxdnAuto=new(){Content="Auto-save confirmed NXDN (live only)",Foreground=Brushes.White};
    private void InitializeNxdnRows()
    {
        foreach(string name in new[]{"MHz","System","Site","RAN","Location","Count","LastSeen","Status","Source"})nxdnGrid.Columns.Add(new DataGridTextColumn{Header=name,Binding=new Binding(name)});
        nxdnGrid.ItemsSource=nxdnRows;var panel=new StackPanel();var actions=new WrapPanel();panel.Children.Add(actions);actions.Children.Add(nxdnAuto);var save=new Button{Content="Save / update selected"};actions.Children.Add(save);save.Click+=(_,_)=>{if(nxdnGrid.SelectedItem is NxdnRow r&&r.Count>=3&&!r.Last.Context.Replay)SaveNxdnRow(r);else Log("Select a confirmed live NXDN row; replay identities are not saved automatically.");};var clear=new Button{Content="Clear NXDN rows"};actions.Children.Add(clear);clear.Click+=(_,_)=>nxdnRows.Clear();panel.Children.Add(nxdnGrid);DigitalVoiceControls.Children.Add(new Expander{Header="NXDN control-channel identities",IsExpanded=true,Foreground=Brushes.LightSteelBlue,Content=panel});
        digitalVoice.Control+=r=>Dispatcher.BeginInvoke(()=>ObserveNxdn(r));
    }
    private void ObserveNxdn(NxdnReception reception)
    {
        if(closing)return;var r=nxdnRows.FirstOrDefault(r=>r.Last.Epoch==reception.Epoch&&r.Last.Context.Key==reception.Context.Key&&r.Last.Context.FrequencyHz==reception.Context.FrequencyHz&&r.Last.Identity==reception.Identity);
        if(r==null){if(nxdnRows.Count>=200)nxdnRows.RemoveAt(0);r=new(){Last=reception,First=reception.Seen};nxdnRows.Add(r);}
        if((reception.Seen-r.Last.Seen).TotalSeconds>10){r.Count=0;r.First=reception.Seen;}r.Last=reception;r.Count=Math.Min(1000000,r.Count+1);nxdnGrid.Items.Refresh();
        if(!r.Stored&&nxdnAuto.IsChecked==true&&r.Count>=3&&!reception.Context.Replay)SaveNxdnRow(r);
    }
    private void SaveNxdnRow(NxdnRow row){try{row.Stored=FrequencyManager.SaveNxdn(row.Last,workspaceSlot);}catch(Exception ex){Log("NXDN frequency save: "+ex.Message);}}
}
