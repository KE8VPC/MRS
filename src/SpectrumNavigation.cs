using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MRS;
public partial class MainWindow
{
    private void RefreshNavigation()
    {
        if (!NavigationFrequency.IsKeyboardFocused) NavigationFrequency.Text = Spectrum.IsRf ? (Spectrum.TunedCenter / 1000000).ToString("F6", CultureInfo.InvariantCulture) : "";
        NavigationReadout.Text = $"{(Spectrum.IsRf ? "RF" : "Audio")} · View {Spectrum.VisibleSpan / 1000:0.###} kHz / full {Spectrum.FullSpan / 1000:0.###} kHz · center {Spectrum.VisibleCenter / 1000000:0.000000} MHz";
        if (Spectrum.IsRf && receiver.IsHackRf && receiver.AppliedBandwidth > 0) NavigationReadout.Text += $" · analog filter {receiver.AppliedBandwidth / 1000000:0.##} MHz";
        if(Spectrum.InteractionLocked)NavigationReadout.Text+=" · SCAN LOCKED";
    }
    private async Task TuneText(string text)
    {
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mhz) || mhz < 0 || mhz > long.MaxValue / 1000000m || decimal.Truncate(mhz * 1000000m) != mhz * 1000000m)
        { Log("Enter a frequency in MHz with at most six decimal places."); return; }
        await LiveTune((long)(mhz * 1000000m));
    }
    private async void NavigationEnter(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter) { e.Handled = true; await TuneText(NavigationFrequency.Text); } }
    private async void NavigationStep(object sender, RoutedEventArgs e)
    {
        long step = long.Parse(((ComboBoxItem)NavigationStepSize.SelectedItem).Tag.ToString()!, CultureInfo.InvariantCulture);
        int direction = int.Parse(((Button)sender).Tag.ToString()!, CultureInfo.InvariantCulture);
        await LiveTune(tunedHz + direction * step);
    }
    private void NavigationFull(object sender, RoutedEventArgs e) => Spectrum.ResetView();
    private void NavigationCenter(object sender, RoutedEventArgs e) => Spectrum.CenterView();
    private void NavigationZoom(object sender, RoutedEventArgs e)
    {
        double factor = double.Parse(((Button)sender).Tag.ToString()!, CultureInfo.InvariantCulture);
        Spectrum.Configure(Spectrum.Settings with { Zoom = Spectrum.Settings.Zoom * factor });
    }
}
