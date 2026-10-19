using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
namespace MRS;
internal sealed class DataInspectorWindow : Window
{
    internal DataInspection Snapshot {get;}
    private readonly TextBox bitView;
    internal void ShowComparison()=>((TabControl)((DockPanel)Content).Children.OfType<TabControl>().Single()).SelectedIndex=4;
    internal DataInspectorWindow(PacketObservation observation,DataInspection? reference=null,Action<DataInspection?>? setReference=null)
    {
        Snapshot=new(observation);Title=$"M.R.S. Data inspector — {observation.Protocol}";Width=1000;Height=700;MinWidth=650;MinHeight=450;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(14)};
        var heading=new TextBlock{Text=$"Frozen observation · {observation.ObservedUtc:O}\n{Snapshot.Provenance}",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,8,0,0)};DockPanel.SetDock(actions,Dock.Bottom);
        var export=new Button{Content="Export snapshot JSON"};export.Click+=(_,_)=>Export();actions.Children.Add(export);
        var close=new Button{Content="Close"};close.Click+=(_,_)=>Close();actions.Children.Add(close);root.Children.Add(actions);
        var pin=new Button{Content="Pin as reference",IsEnabled=setReference!=null};actions.Children.Add(pin);
        var forget=new Button{Content="Clear reference",IsEnabled=reference!=null};actions.Children.Add(forget);
        var tabs=new TabControl();root.Children.Add(tabs);
        var fields=new DataGrid{ItemsSource=Snapshot.Fields,AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White,SelectionMode=DataGridSelectionMode.Single};
        foreach(var (name,width) in new[]{("Name",220d),("Value",240d),("Range",120d),("Origin",260d)})
        {
            var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));style.Setters.Add(new Setter(TextBlock.PaddingProperty,new Thickness(3)));
            fields.Columns.Add(new DataGridTextColumn{Header=name=="Range"?"Bits (MSB first)":name,Binding=new Binding(name),Width=width,ElementStyle=style});
        }
        var decoded=new Grid();decoded.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});decoded.RowDefinitions.Add(new(){Height=GridLength.Auto});decoded.Children.Add(fields);
        bitView=Text(Snapshot.Bits.Length>0?Snapshot.Bits:"No received/codeword bits available.");bitView.Height=100;bitView.Margin=new Thickness(0,8,0,0);Grid.SetRow(bitView,1);decoded.Children.Add(bitView);
        fields.SelectionChanged+=(_,_)=>{if(fields.SelectedItem is InspectionField {BitStart:int start,BitLength:int length}){bitView.Focus();bitView.Select(start,length);}else bitView.Select(0,0);};
        tabs.Items.Add(new TabItem{Header="Decoded fields / bits",Content=decoded});
        tabs.Items.Add(new TabItem{Header="Bytes",Content=Text(Snapshot.ByteDump())});
        tabs.Items.Add(new TabItem{Header="Message",Content=Text(observation.Description+"\n\n"+observation.Information)});
        tabs.Items.Add(new TabItem{Header="Snapshot JSON",Content=Text(Snapshot.Json)});
        var comparisonTab=new TabItem{Header="Compare"};tabs.Items.Add(comparisonTab);
        void RefreshComparison()
        {
            if(reference==null){comparisonTab.Content=Text("No reference pinned. Pin this snapshot, then inspect another Data row in this receiver. The reference stays in memory until cleared or the receiver window closes.");return;}
            var comparison=new DataComparison(reference,Snapshot);
            var panel=new DockPanel();
            var info=new TextBlock{Text=comparison.Summary,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)};DockPanel.SetDock(info,Dock.Top);panel.Children.Add(info);
            var onlyChanges=new CheckBox{Content="Only changed fields",IsChecked=true,Margin=new Thickness(8)};DockPanel.SetDock(onlyChanges,Dock.Top);panel.Children.Add(onlyChanges);
            var bits=Text(comparison.BitReport());bits.Height=80;DockPanel.SetDock(bits,Dock.Bottom);panel.Children.Add(bits);
            var grid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White};
            foreach(var name in new[]{"Name","Reference","Current","Change"})
            {
                var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));
                grid.Columns.Add(new DataGridTextColumn{Header=name,Binding=new Binding(name),Width=name=="Change"?100:name=="Name"?200:270,MinWidth=80,ElementStyle=style});
            }
            void Filter()=>grid.ItemsSource=comparison.Fields.Where(f=>onlyChanges.IsChecked!=true||f.Change!="Same").ToArray();
            onlyChanges.Checked+=(_,_)=>Filter();onlyChanges.Unchecked+=(_,_)=>Filter();Filter();panel.Children.Add(grid);comparisonTab.Content=panel;
        }
        pin.Click+=(_,_)=>{reference=Snapshot;setReference?.Invoke(Snapshot);forget.IsEnabled=true;pin.Content="Reference pinned";RefreshComparison();};
        forget.Click+=(_,_)=>{reference=null;setReference?.Invoke(null);forget.IsEnabled=false;pin.Content="Pin as reference";RefreshComparison();};
        RefreshComparison();
        Content=root;
    }
    private static TextBox Text(string value)=>new(){Text=value,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,Foreground=Brushes.Black,Background=Brushes.White,FontFamily=new FontFamily("Consolas"),Padding=new Thickness(8)};
    private void Export()
    {
        var dialog=new SaveFileDialog{Filter="Data snapshot JSON|*.json",FileName=$"MRS-inspection-{DateTime.Now:yyyyMMdd-HHmmss}.json"};
        if(dialog.ShowDialog(this)!=true)return;
        try{File.WriteAllText(dialog.FileName,Snapshot.Json);}catch(Exception ex){MessageBox.Show(this,"Export failed: "+ex.Message,"M.R.S.");}
    }
}
