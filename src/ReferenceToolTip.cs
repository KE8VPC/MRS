using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;

// Shared-frequency aliases can exceed the screen. Keep all details scrollable.
internal sealed class ReferenceToolTip : ToolTip
{
    private readonly string text;
    internal ReferenceToolTip(string text)
    {
        this.text=text;
        Background=new SolidColorBrush(Color.FromRgb(16,23,34));
        Foreground=new SolidColorBrush(Color.FromRgb(228,237,248));
        BorderBrush=new SolidColorBrush(Color.FromRgb(113,205,245));
        Content=new ScrollViewer
        {
            MaxHeight=380,MaxWidth=640,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,
            Content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=600,FontSize=12,Foreground=Foreground,Margin=new Thickness(6)}
        };
    }
    public override string ToString()=>text;
    internal void Scroll(int delta)
    {
        var viewer=(ScrollViewer)Content;
        viewer.ScrollToVerticalOffset(Math.Clamp(viewer.VerticalOffset-delta/120.0*80,0,viewer.ScrollableHeight));
    }
}
