using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace MRS;
internal sealed record BroadcastChannel(long Hz,string Label,string Mode){public override string ToString()=>Label;}
internal static class BroadcastChannels
{
    internal static BroadcastChannel[] Am=>Enumerable.Range(0,117).Select(n=>new BroadcastChannel(540000+n*10000,$"{540+n*10} kHz","AM")).Prepend(new(530000,"530 kHz · travelers information","AM")).ToArray();
    internal static BroadcastChannel[] Fm=>Enumerable.Range(0,100).Select(n=>new BroadcastChannel(88100000+n*200000,$"{88.1+n*.2:0.0} MHz · ch {201+n}","WFM")).ToArray();
    internal static void Seed(string root){foreach(var (name,channels) in new[]{("am",Am),("fm",Fm)}){string path=Path.Combine(root,"reference","broadcast-"+name,"channels.csv");if(!File.Exists(path))PlanCsv.Write(path,channels.Select(c=>new Dictionary<string,string>{{"downlink_mhz",PlanCsv.MHz(c.Hz)},{"label",c.Label},{"source",name=="fm"?"47 CFR 73.201":"47 CFR 73.14; 530 kHz supplemental travelers-information reference"}}),["downlink_mhz","label","source"]);}}
}
public partial class MainWindow
{
    private void InitializeBroadcastWorkspace()
    {
        void Controls(Panel parent,BroadcastChannel[] channels){var row=new WrapPanel{Margin=new Thickness(6)};var list=new ComboBox{ItemsSource=channels,SelectedIndex=0,Width=240,IsTextSearchEnabled=true};var tune=new Button{Content="Tune selected"};tune.Click+=(_,_)=>{if(list.SelectedItem is BroadcastChannel c){if(!rfMode||receiver.IsReplay){FrequencyBox.Text=PlanCsv.MHz(c.Hz);FmMode.SelectedIndex=Array.IndexOf(ReceiveDemodulator.Modes,c.Mode);PopulateBandwidth(ChannelSettings.DefaultWidth(c.Mode));Log("Broadcast channel staged. Start / Apply the receiver to receive it.");}else TuneSaved(new FrequencyItem{FrequencyHz=c.Hz,Mode=c.Mode,BandwidthHz=ChannelSettings.DefaultWidth(c.Mode),Name=c.Label});}};row.Children.Add(list);row.Children.Add(tune);parent.Children.Add(row);}
        Controls(AmControls,BroadcastChannels.Am);Controls(FmControls,BroadcastChannels.Fm);
        void Move(){Grid? host=BroadcastTab.IsSelected?(FmTab.IsSelected?FmSpectrumHost:AmTab.IsSelected?AmSpectrumHost:null):null;if(host!=null){if(SpectrumTab.Content==SharedSpectrum)SpectrumTab.Content=null;else if(SharedSpectrum.Parent is Panel p)p.Children.Remove(SharedSpectrum);if(!host.Children.Contains(SharedSpectrum))host.Children.Add(SharedSpectrum);}else if(SpectrumTab.Content==null){if(SharedSpectrum.Parent is Panel p)p.Children.Remove(SharedSpectrum);SpectrumTab.Content=SharedSpectrum;}
            RefreshBroadcast();BroadcastViewChanged(this,null!);
        }
        WorkspaceTabs.SelectionChanged+=(_,e)=>{if(ReferenceEquals(e.Source,WorkspaceTabs))Move();};BroadcastTabs.SelectionChanged+=(_,e)=>{if(ReferenceEquals(e.Source,BroadcastTabs))Move();};
    }
}
