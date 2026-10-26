using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;

// A snapshot of library order; editing the library never mutates an active scan.
internal static class FolderScanPlan
{
    internal static FrequencyItem[] Create(IEnumerable<FrequencyItem> items, Guid folder, bool recursive, bool excludeHidden)
    {
        var all=items.ToArray();
        if(!all.Any(i=>i.Id==folder&&i.IsFolder))throw new InvalidOperationException("Select a saved folder.");
        var result=new List<FrequencyItem>();var visited=new HashSet<Guid>();
        void Walk(Guid id,bool hidden)
        {
            if(!visited.Add(id))return;
            foreach(var item in all.Where(i=>i.ParentId==id))
            {
                bool excluded=hidden||!item.Visible;
                if(excludeHidden&&excluded)continue;
                if(item.IsFolder){if(recursive)Walk(item.Id,excluded);}
                else result.Add(item);
            }
        }
        bool parentHidden=false;var parent=all.First(i=>i.Id==folder);var seen=new HashSet<Guid>();
        while(seen.Add(parent.Id)){parentHidden|=!parent.Visible;if(parent.ParentId is not {} p||all.FirstOrDefault(i=>i.Id==p) is not {} next)break;parent=next;}
        Walk(folder,parentHidden);return result.ToArray();
    }
}
internal sealed class FolderScanTiming
{
    private double started,lastActive; private bool active;
    internal void Reset(double now){started=lastActive=now;active=false;}
    internal bool Advance(double now,bool signal,bool hold,double dwell,double quiet,double maximum)
    {
        if(signal){lastActive=now;active=true;}
        if(hold)return false;
        return now-started>=maximum || (now-started>=dwell && (!active || !signal&&now-lastActive>=quiet));
    }
}
public sealed class FolderScanPanel : StackPanel
{
    internal readonly CheckBox Recursive=new(){Content="Include subfolders",Foreground=Brushes.White,IsChecked=true};
    internal readonly CheckBox Hidden=new(){Content="Skip hidden entries",Foreground=Brushes.White,IsChecked=false};
    internal readonly CheckBox Hold=new(){Content="Hold current channel",Foreground=Brushes.White};
    private readonly TextBox dwell=new(){Text="1.5",Width=45},quiet=new(){Text="1.5",Width=45},maximum=new(){Text="15",Width=45};
    private readonly TextBlock status=new(){Text="Right-click a folder → Scan folder. Start a live receiver first.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue};
    internal event Action? StopRequested,SkipRequested;
    internal (double Dwell,double Quiet,double Maximum) Settings()
    {
        double Read(TextBox box,double min,double max){if(!double.TryParse(box.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n)||n<min||n>max)throw new InvalidOperationException($"Scan seconds must be between {min} and {max}.");return n;}
        var d=Read(dwell,.5,30);var q=Read(quiet,.1,30);var m=Read(maximum,1,300);
        if(m<d)throw new InvalidOperationException("Maximum hold must be at least the dwell time.");return(d,q,m);
    }
    public FolderScanPanel()
    {
        Margin=new Thickness(0,6,0,6);
        Children.Add(Recursive);Children.Add(Hidden);
        var row=new WrapPanel();Children.Add(row);
        foreach(var pair in new[]{("Dwell s",dwell),("Quiet s",quiet),("Max hold s",maximum)}){var group=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,3,3)};group.Children.Add(new TextBlock{Text=pair.Item1,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(3)});group.Children.Add(pair.Item2);row.Children.Add(group);}
        var buttons=new WrapPanel();Children.Add(buttons);
        foreach(var label in new[]{"Stop scan","Skip"}){var b=new Button{Content=label,Margin=new Thickness(2),Padding=new Thickness(5)};b.Click+=(_,_)=>{if(label=="Skip")SkipRequested?.Invoke();else StopRequested?.Invoke();};buttons.Children.Add(b);}
        buttons.Children.Add(Hold);Children.Add(status);
    }
    internal void Display(string message,bool running)
    {
        status.Text=message;Recursive.IsEnabled=Hidden.IsEnabled=dwell.IsEnabled=quiet.IsEnabled=maximum.IsEnabled=!running;
        Hold.IsEnabled=running;if(!running)Hold.IsChecked=false;
    }
}
