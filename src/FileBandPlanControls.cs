using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public partial class MainWindow
{
    private bool updatingBandList;private CsvBandPlans? filePlans;private P25BandCollector? siteCollector;private string bandSettingsPath="";
    private readonly StackPanel bandList=new();private readonly TextBlock bandStatus=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};
    private readonly CheckBox collectP25=new(){Content="Collect validated live P25 site bandplans",Foreground=Brushes.White};
    private sealed record BandPreferences(string Root,string[] Hidden,bool Uplinks,bool Grid,bool Collect);
    private void InitializeFilePlans()
    {
        bandSettingsPath=Path.Combine(dataFolder,"bandplans.json");BandPreferences? saved=null;try{if(File.Exists(bandSettingsPath))saved=JsonSerializer.Deserialize<BandPreferences>(File.ReadAllText(bandSettingsPath));}catch(Exception ex){Log("Bandplan settings: "+ex.Message);}
        string root=saved?.Root??Path.Combine(diagnosticWorkspace?dataFolder:AppContext.BaseDirectory,"bandplans");
        var host=new DockPanel{Margin=new Thickness(10)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);host.Children.Add(top);
        var up=new CheckBox{Content="Show uplinks",Foreground=Brushes.White,IsChecked=saved?.Uplinks==true};var grid=new CheckBox{Content="Show calculated grid (not observed)",Foreground=Brushes.White,IsChecked=saved?.Grid==true};collectP25.IsChecked=saved?.Collect==true;
        var actions=new WrapPanel();top.Children.Add(actions);AddBandManagerActions(actions);
        void Button(string title,Action action){var b=new Button{Content=title,Margin=new Thickness(0,0,8,6)};b.Click+=(_,_)=>{try{action();}catch(Exception ex){bandStatus.Text=ex.Message;}};actions.Children.Add(b);}
        Button("Reload CSV",()=>ReloadFilePlans());Button("Create missing reference CSVs",()=>{if(filePlans!=null){CsvBandPlans.Seed(filePlans.Root);ReloadFilePlans();}});Button("Open folder",()=>{if(filePlans!=null)Process.Start(new ProcessStartInfo(filePlans.Root){UseShellExecute=true});});
        Button("Choose folder…",()=>{var picker=new Microsoft.Win32.OpenFolderDialog{Title="Choose editable bandplans folder"};if(picker.ShowDialog(this)==true){siteCollector?.Flush(true);SetRoot(picker.FolderName,null);SaveBandSettings();}});

        foreach(var c in new[]{collectP25,up,grid}){c.Margin=new Thickness(0,3,0,3);top.Children.Add(c);}
        top.Children.Add(new TextBlock{Text="Reference selections are shared with Display. Select site/custom folders below. CSV edits take effect on Reload; collection updates every 5 seconds. Gray dashed grid markers are calculated, not observed.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,6,0,6)});top.Children.Add(bandStatus);
        host.Children.Add(new ScrollViewer{Content=bandList,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});bandManagerTab=new TabItem{Header="Bandplans",Content=host};AnalysisTabs.Items.Add(bandManagerTab);DisplayControls.ManagePlansRequested+=ShowBandManager;
        void Changed(){if(filePlans==null)return;filePlans.Uplinks=up.IsChecked==true;filePlans.Grid=grid.IsChecked==true;if(siteCollector!=null)siteCollector.Enabled=collectP25.IsChecked==true;SaveBandSettings();Spectrum.InvalidateVisual();}
        foreach(var c in new[]{collectP25,up,grid}){c.Checked+=(_,_)=>Changed();c.Unchecked+=(_,_)=>Changed();}
        void SetRoot(string path,BandPreferences? preferences){filePlans=new(path){Hidden=preferences?.Hidden.ToHashSet()??new(),Uplinks=up.IsChecked==true,Grid=grid.IsChecked==true};siteCollector=new(path){Enabled=collectP25.IsChecked==true};Spectrum.FilePlans=filePlans;ReloadFilePlans();}
        try{RailroadPlan.SeedExpanded(root);BroadcastChannels.Seed(root);}catch(Exception ex){Log("Expanded railroad references: "+ex.Message);}if(diagnosticWorkspace){CsvBandPlans.Seed(root);}SetRoot(root,saved);SaveBandSettings();
        DisplayControls.Changed+=settings=>{updatingBandList=true;try{foreach(var c in bandList.Children.OfType<CheckBox>())if(c.Content is string p&&LegacyReference(p))c.IsChecked=ReferenceShown(p,settings);}finally{updatingBandList=false;}};
        receiver.ChannelChanged+=()=>Dispatcher.BeginInvoke(()=>siteCollector?.Reset());receiver.ReplayGap+=()=>Dispatcher.BeginInvoke(()=>siteCollector?.Reset());receiver.Ended+=_=>Dispatcher.BeginInvoke(()=>siteCollector?.Reset());
    }
    private static bool LegacyReference(string plan)=>plan is "reference/us" or "reference/arrl" or "reference/nifog" or "reference/tv";
    private static bool ReferenceShown(string p,DisplaySettings s)=>p switch{"reference/us"=>s.ShowBandPlan,"reference/arrl"=>s.ShowArrlPlan,"reference/nifog"=>s.ShowNifogPlan,"reference/tv"=>s.ShowTvPlan,_=>true};
    private void SaveBandSettings(){if(filePlans==null)return;try{File.WriteAllText(bandSettingsPath,JsonSerializer.Serialize(new BandPreferences(filePlans.Root,filePlans.Hidden.ToArray(),filePlans.Uplinks,filePlans.Grid,collectP25.IsChecked==true)));}catch(Exception ex){bandStatus.Text="Could not save settings: "+ex.Message;}}
    private void ReloadFilePlans()
    {
        if(filePlans==null)return;filePlans.Reload();bandList.Children.Clear();foreach(string plan in filePlans.Plans){var c=new CheckBox{Content=plan,IsChecked=LegacyReference(plan)?ReferenceShown(plan,Spectrum.Settings):!filePlans.Hidden.Contains(plan),Foreground=Brushes.White,Margin=new Thickness(0,4,0,4)};void Changed(){if(updatingBandList)return;if(LegacyReference(plan)){filePlans.Hidden.Remove(plan);DisplayControls.SetReferencePlan(plan,c.IsChecked==true);}else if(c.IsChecked==true)filePlans.Hidden.Remove(plan);else filePlans.Hidden.Add(plan);SaveBandSettings();Spectrum.InvalidateVisual();}c.Checked+=(_,_)=>Changed();c.Unchecked+=(_,_)=>Changed();bandList.Children.Add(c);}Spectrum.InvalidateVisual();RefreshBandStatus();
    }
    private void RefreshBandStatus(){if(filePlans==null)return;bandStatus.Text=$"Folder: {filePlans.Root}\n{filePlans.Plans.Count} plans · {filePlans.Markers.Count(m=>!m.Uplink)} entries · unresolved observations: {siteCollector?.PendingCount??0}\n{filePlans.Error}\n{siteCollector?.Error}";}
    private void TickFilePlans(){if(siteCollector?.Flush()==true)ReloadFilePlans();RefreshBandStatus();}
    private static readonly Brush TxBrush=new SolidColorBrush(Color.FromRgb(255,85,93)),RxBrush=new SolidColorBrush(Color.FromRgb(67,219,130)),IdleBrush=new SolidColorBrush(Color.FromRgb(115,128,146));
    private bool txIndicatorActive;private long lastLiveFrame;
    private void RefreshRxTxIndicator(){string state=txIndicatorActive?"TX":(!receiver.IsReplay&&rfMode&&DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-Interlocked.Read(ref lastLiveFrame)<1500)?"RX":"Idle";RxTxLabel.Text=state;RxTxLight.Fill=state=="TX"?TxBrush:state=="RX"?RxBrush:IdleBrush;RxTxPanel.ToolTip=state=="TX"?"Transmitter reports active; not confirmation of radiated RF":state=="RX"?"Live receiver is delivering samples":"Receiver stopped, awaiting samples, or replaying";System.Windows.Automation.AutomationProperties.SetName(RxTxPanel,state);}
}
