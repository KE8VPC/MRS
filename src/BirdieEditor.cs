using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace MRS;
internal sealed class BirdieEditor:Window
{
    internal Birdie[]? Result {get;private set;}
    internal BirdieEditor(IEnumerable<Birdie> existing)
    {
        Title="Known birdies · user-defined references";Width=660;Height=530;MinWidth=520;MinHeight=430;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var body=new DockPanel{Margin=new Thickness(14)};Content=body;
        var fields=new StackPanel();DockPanel.SetDock(fields,Dock.Bottom);body.Children.Add(fields);
        var list=new ListBox();body.Children.Add(list);foreach(var b in existing)list.Items.Add(b);
        fields.Children.Add(new TextBlock{Text="Fixed: MHz. Capture-relative: offset in kHz (0 marks the center spike). Width is the full exclusion width in kHz. Selecting a row loads it for editing.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
        TextBox Input(string label,string value){fields.Children.Add(new TextBlock{Text=label});var box=new TextBox{Text=value};fields.Children.Add(box);return box;}
        var name=Input("Name / reason","Known birdie");
        var relative=new CheckBox{Content="Relative to hardware capture center (kHz)"};fields.Children.Add(relative);
        var position=Input("Fixed frequency (MHz) or relative offset (kHz)","0");var width=Input("Full width (kHz)","4");
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};fields.Children.Add(status);
        list.SelectionChanged+=(_,_)=>{if(list.SelectedItem is Birdie b){name.Text=b.Name;relative.IsChecked=b.Relative;position.Text=(b.PositionHz/(b.Relative?1000:1e6)).ToString("0.######",CultureInfo.InvariantCulture);width.Text=(b.WidthHz/1000).ToString("0.###",CultureInfo.InvariantCulture);}};
        var buttons=new WrapPanel{Margin=new Thickness(0,10,0,0)};fields.Children.Add(buttons);
        void Button(string label,Action action){var button=new Button{Content=label,Margin=new Thickness(0,0,6,0),Padding=new Thickness(8,5,8,5)};buttons.Children.Add(button);button.Click+=(_,_)=>action();}
        Birdie? Read()
        {
            if(!double.TryParse(position.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double p)||!double.TryParse(width.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double w))return null;
            var b=new Birdie(name.Text.Trim(),p*(relative.IsChecked==true?1000:1e6),w*1000,relative.IsChecked==true);return b.Valid?b:null;
        }
        Button("Add",()=>{if(Read() is {} b&&list.Items.Count<128)list.Items.Add(b);else status.Text="Use a name, valid frequency/offset and width 0.1–1000 kHz; maximum 128 entries.";});
        Button("Update selected",()=>{if(list.SelectedIndex is int i&&i>=0&&Read() is {} b)list.Items[i]=b;else status.Text="Select a row and enter valid values.";});
        Button("Remove selected",()=>{if(list.SelectedIndex>=0)list.Items.RemoveAt(list.SelectedIndex);});
        Button("Save",()=>{Result=list.Items.Cast<Birdie>().ToArray();DialogResult=true;});
        Button("Cancel",()=>DialogResult=false);
    }
}
