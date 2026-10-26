using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;

internal sealed class P25DiscoveryEditor : Window
{
    private readonly TextBox name,notes;
    internal (string Name,string Notes)? Result {get;private set;}
    internal P25DiscoveryEditor(P25Discovery discovery)
    {
        Title="Name and notes · P25 discovery";Width=560;Height=440;MinWidth=420;MinHeight=360;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var grid=new Grid{Margin=new Thickness(16)};Content=grid;
        grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var identity=new TextBlock{Text=discovery.Label+"\nNames and notes apply to this discovery entry. Decoded identifiers stay unchanged.\n\nName (optional)",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,0,0,10)};grid.Children.Add(identity);
        name=new TextBox{Text=discovery.Name??"",MaxLength=200,Padding=new Thickness(6),ToolTip="Your name for this discovery. Leave blank to show the decoded label."};Grid.SetRow(name,1);grid.Children.Add(name);
        var label=new TextBlock{Text="Notes",Margin=new Thickness(0,10,0,4)};Grid.SetRow(label,2);grid.Children.Add(label);
        notes=new TextBox{Text=discovery.Notes??"",MaxLength=8000,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new Thickness(6)};Grid.SetRow(notes,3);grid.Children.Add(notes);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};Grid.SetRow(buttons,4);grid.Children.Add(buttons);
        var save=new Button{Content="Save"};save.Click+=(_,_)=>{Result=(name.Text.Trim(),notes.Text);DialogResult=true;};buttons.Children.Add(save);
        buttons.Children.Add(new Button{Content="Cancel",IsCancel=true});
        Loaded+=(_,_)=>{name.Focus();name.SelectAll();};
    }
}

internal static class P25FrequencyDraft
{
    internal static bool CanCreate(P25Discovery discovery)=>!discovery.Observation.Replay && discovery.Category!="Band plan" && discovery.FrequencyHz is >0 and <=1000000000000;
    internal static FrequencyItem Create(P25Discovery discovery)
    {
        if(!CanCreate(discovery))throw new InvalidOperationException("Select a resolved live channel. Band bases, replay entries and unresolved channels cannot be saved as received stations.");
        var channel=SiteChannelData.FromP25(discovery)??throw new InvalidOperationException("No resolved live channel.");
        return channel.Draft(discovery.Name,discovery.Notes);
    }
}
