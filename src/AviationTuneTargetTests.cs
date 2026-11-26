using System.Windows;
using System.Windows.Controls;
namespace MRS;

internal static class AviationTuneTargetTests
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var item in Children(child)) yield return item;
    }
    internal static void Run(Action<bool, string> check)
    {
        var panel = new AviationPanel();
        try
        {
            var controls = Children(panel).ToArray();
            var tune = controls.OfType<Button>().Single(b => Equals(b.Content, "Tune receiver"));
            var mode = controls.OfType<ComboBox>().First();
            var target = controls.OfType<TextBox>().Single(t => t.Width == 115);
            long requested = 0; panel.Tune = hz => requested = hz;
            mode.SelectedIndex = 1;
            panel.Boundary(); panel.Boundary();
            check(requested == 0 && target.Text == "136.975000", "Aviation capture boundaries preserve VDL2 preset without tuning");
            target.Text = "136.650000"; panel.Boundary();
            tune.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(requested == 136650000 && target.Text == "136.650000", "Aviation explicit Tune uses preserved editable target");
            mode.SelectedIndex = 2; panel.Boundary();
            tune.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(requested == 1090000000 && target.Text == "1090.000000", "Aviation mode change restores ADS-B target independently of receiver");
            target.Text = "1090.100000"; panel.Boundary();
            tune.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(requested == 1090100000 && target.Text == "1090.100000", "Aviation Tune preserves a custom ADS-B receive target");
        }
        finally { panel.Release(); }
    }
}
