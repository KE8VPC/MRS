namespace MRS;
internal static class TuneSnap
{
    internal static long Apply(double hz,int mode,long step,IEnumerable<long> channels,double tolerance)
    {
        long raw=(long)Math.Clamp(Math.Round(hz),0,long.MaxValue);if(mode==0)return raw;step=Math.Clamp(step,1,100000000);
        if(mode==2){var nearest=channels.Where(c=>c>0&&Math.Abs(c-hz)<=tolerance).OrderBy(c=>Math.Abs(c-hz)).ThenBy(c=>c).Cast<long?>().FirstOrDefault();if(nearest is {} center)return center;}
        return (long)(Math.Round((decimal)raw/step,0,MidpointRounding.AwayFromZero)*step);
    }
}
public partial class MainWindow
{
    private void InitializeTuneSnap()
    {
        var combo=new System.Windows.Controls.ComboBox{Width=145,ItemsSource=new[]{"Snap off","Snap: tuning step","Snap: bandplan"},SelectedIndex=Spectrum.Settings.SnapMode,ToolTip="Spectrum and waterfall clicks: nearest visible channel within 8 pixels, otherwise selected tuning step. Entry boxes retain full precision."};bool sync=false;
        combo.SelectionChanged+=(_,_)=>{if(!sync)Spectrum.Configure(Spectrum.Settings with{SnapMode=combo.SelectedIndex});};
        NavigationStepSize.SelectionChanged+=(_,_)=>{if(sync)return;if(NavigationStepSize.SelectedItem is System.Windows.Controls.ComboBoxItem item&&long.TryParse(item.Tag?.ToString(),out long value))Spectrum.Configure(Spectrum.Settings with{SnapStepHz=value});};
        Spectrum.SettingsChanged+=s=>{sync=true;combo.SelectedIndex=s.SnapMode;foreach(System.Windows.Controls.ComboBoxItem item in NavigationStepSize.Items)if(long.TryParse(item.Tag?.ToString(),out long value)&&value==s.SnapStepHz)NavigationStepSize.SelectedItem=item;sync=false;};RfNavigationControls.Children.Add(combo);
    }
}
