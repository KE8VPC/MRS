using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
namespace MRS;
public sealed class AviationPanel : UserControl
{
    private readonly AviationPipeline pipeline=new();private readonly AircraftTable table=new();
    private readonly System.Collections.ObjectModel.ObservableCollection<AircraftRow> displayed=[];
    private readonly ComboBox mode=new(){ItemsSource=new[]{"ACARS","VDL2","ADS-B / Mode S","UAT"},SelectedIndex=0,Width=160};
    private readonly TextBox frequency=new(){Text="131.550000",Width=115,TextAlignment=TextAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center};
    private readonly TextBlock status=new(){Text="Aviation off",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Aquamarine,Margin=new Thickness(0,8,0,8)};
    private readonly CheckBox showMap=new(){Content="Show aircraft map",Foreground=Brushes.White};
    private readonly DataGrid aircraft=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White};
    private readonly AircraftMap map=new();private long lastMapUpdate;
    private readonly Grid mapHost=new(){Visibility=Visibility.Collapsed};
    internal Action<long>? Tune;internal Action<string,long>? SetupRequested;internal event Action<PacketObservation>? Observation;
    private string Mode=>new[]{"acars","vdl2","adsb","uat"}[mode.SelectedIndex];
    internal IEnumerable<DataGrid> MonitorTables=>new[]{aircraft};
    public AviationPanel()
    {
        aircraft.ItemsSource=displayed;
        aircraft.SelectionChanged+=(_,_)=>DrawMap();map.Selected+=row=>{aircraft.SelectedItem=row;aircraft.ScrollIntoView(row);DrawMap();};
        var root=new Grid{Margin=new Thickness(10)};root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        root.RowDefinitions[1].MinHeight=100;root.RowDefinitions[3].Height=new GridLength(0);
        var header=new StackPanel();root.Children.Add(header);
        header.Children.Add(new TextBlock{Text="AVIATION · RECEIVE ONLY",Foreground=Brushes.White,FontSize=18,Margin=new Thickness(0,0,0,8)});
        var controls=new WrapPanel();header.Children.Add(controls);controls.Children.Add(mode);controls.Children.Add(new TextBlock{Text=" MHz ",Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center});controls.Children.Add(frequency);
        void Button(string text,Action action){var b=new Button{Content=text};b.Click+=(_,_)=>action();controls.Children.Add(b);}
        Button("Tune receiver",()=>{if(Target() is {} hz)Tune?.Invoke(hz);});
        Button("Check / setup receiver",()=>{if(Target() is {} hz)SetupRequested?.Invoke(Mode,hz);});
        Button("Start aviation",()=>{if(Target() is {} hz){pipeline.Start(Mode,hz);status.Text="Waiting for current SDR or I/Q replay. Reception mode/filter do not gate aviation I/Q decoding.";}});
        Button("Stop",()=>Stop("Aviation stopped"));Button("Clear aircraft",()=>{table.Clear();Refresh();});Button("Export aircraft",Export);
        controls.Children.Add(showMap);mode.SelectionChanged+=(_,_)=>{Stop("Mode changed; start aviation again");frequency.Text=mode.SelectedIndex switch{0=>"131.550000",1=>"136.975000",2=>"1090.000000",_=>"978.000000"};};
        header.Children.Add(status);header.Children.Add(new TextBlock{Text="One selected aviation channel per receiver. Start the SDR or I/Q replay first. ADS-B: ≥2 MS/s; UAT: ≥2.084 MS/s (2.4 recommended). VHF defaults are editable. Full messages are in Data. No MLAT or online feeding.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,0,0,8)});
        foreach(var (title,path,width) in new[]{("Identity","Identity",190),("Callsign","Callsign",90),("Protocol","Protocol",110),("Position","Position",165),("Pos age","PositionAge",65),("Altitude ft","AltitudeDisplay",80),("Speed kt","SpeedDisplay",75),("Count","Count",55),("Age","Age",50),("Source","Source",180)})aircraft.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(path),Width=width});
        Grid.SetRow(aircraft,1);root.Children.Add(aircraft);
        var note=new TextBlock{Text="Unknown positions stay blank. Altitude/speed expire after 60 s; map positions after 60 s. Rows expire after 15 min. Live/replay and address classes remain separate. Table times are local processing times.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,6)};Grid.SetRow(note,2);root.Children.Add(note);
        mapHost.Children.Add(map);Grid.SetRow(mapHost,4);root.Children.Add(mapHost);map.SizeChanged+=(_,_)=>DrawMap();
        var splitter=new GridSplitter{Height=6,HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch,ResizeDirection=GridResizeDirection.Rows,ResizeBehavior=GridResizeBehavior.PreviousAndNext,Background=Brushes.SlateGray,Visibility=Visibility.Collapsed,ToolTip="Drag to resize aircraft table and map"};
        // Keep the explanatory note above the split region; table and map share available height.
        root.Children.Remove(note);header.Children.Add(note);root.RowDefinitions[2].Height=new GridLength(0);
        Grid.SetRow(splitter,2);root.Children.Add(splitter);Grid.SetRow(mapHost,3);
        showMap.Checked+=(_,_)=>{root.RowDefinitions[2].Height=new GridLength(6);root.RowDefinitions[3].Height=new GridLength(1.5,GridUnitType.Star);root.RowDefinitions[3].MinHeight=100;mapHost.Visibility=Visibility.Visible;splitter.Visibility=Visibility.Visible;DrawMap();};
        showMap.Unchecked+=(_,_)=>{mapHost.Visibility=Visibility.Collapsed;splitter.Visibility=Visibility.Collapsed;root.RowDefinitions[2].Height=new GridLength(0);root.RowDefinitions[3].MinHeight=0;root.RowDefinitions[3].Height=new GridLength(0);};
        Content=root;
    }
    private long? Target()
    {
        if(double.TryParse(frequency.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double mhz)&&double.IsFinite(mhz)&&mhz>0&&mhz<=6000)return (long)Math.Round(mhz*1e6);
        status.Text="Enter a valid frequency in MHz.";return null;
    }
    internal void Feed(IqFrame frame,PacketContext context)=>pipeline.Feed(frame,context);
    internal void Stop(string reason)=>pipeline.Stop(reason);
    internal void Release(){pipeline.Dispose();map.Dispose();}
    internal void Finish()=>_ = pipeline.FinishAsync();
    internal void Observe(AviationMessage message){table.Observe(message);Observation?.Invoke(message.Packet);}
    internal void Refresh()
    {
        foreach(var m in pipeline.Drain())Observe(m);table.Expire();var current=table.Rows;
        foreach(var old in displayed.Where(r=>!current.Contains(r)).ToArray())displayed.Remove(old);
        foreach(var row in current)if(!displayed.Contains(row))displayed.Add(row);
        aircraft.Items.Refresh();status.Text=pipeline.Status;if(showMap.IsChecked==true&&IsVisible&&Environment.TickCount64-lastMapUpdate>=500)DrawMap();
    }
    private void Export()
    {
        var dialog=new SaveFileDialog{Filter="Aircraft snapshot JSON|*.json",FileName=$"MRS-aircraft-{DateTime.Now:yyyyMMdd-HHmmss}.json"};if(dialog.ShowDialog()!=true)return;
        try{File.WriteAllText(dialog.FileName,JsonSerializer.Serialize(table.Rows,new JsonSerializerOptions{WriteIndented=true}));}catch(Exception ex){MessageBox.Show("Aircraft export failed: "+ex.Message);}
    }
    private void DrawMap()
    {
        lastMapUpdate=Environment.TickCount64;
        var points=table.Rows;
        map.Update(points,aircraft.SelectedItem as AircraftRow);
    }
    internal void ShowMapForTest()=>showMap.IsChecked=true;
    internal void ShowTrailsForTest(){aircraft.SelectedItem=displayed.FirstOrDefault();map.SetTrailsForTest(true);DrawMap();}
    internal int MapTrailSegments=>map.TrailSegments;
    internal bool MapOnline=>map.OnlineEnabled;
    internal int MapNetworkRequests=>map.NetworkRequests;
    internal void SetAdsbForTest()=>mode.SelectedIndex=2;
    internal void ClearForTest(){table.Clear();Refresh();}
    internal void FitMapForTest()=>map.FitForTest();
}

