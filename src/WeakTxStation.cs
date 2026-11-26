using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MRS;

internal sealed record WeakTxStation(string Callsign = "", string Grid = "", string LicenseClass = "General")
{
    internal static readonly string[] LicenseClasses = ["Technician", "General", "Advanced", "Amateur Extra"];
    internal bool HasIdentity => WeakTxAuthorization.IsStationCall(Callsign) && Regex.IsMatch(Grid, @"^[A-R]{2}[0-9]{2}$");

    internal WeakTxStation WithIdentity(string callsign, string grid) =>
        this with { Callsign = callsign.Trim().ToUpperInvariant(), Grid = grid.Trim().ToUpperInvariant() };

    internal static WeakTxStation Load(string path, string defaultCall = "", string defaultGrid = "")
    {
        if (!File.Exists(path))
            return new WeakTxStation().WithIdentity(defaultCall, WeakDecode.IsGrid(defaultGrid) ? defaultGrid[..4] : "");
        var station = JsonSerializer.Deserialize<WeakTxStation>(File.ReadAllText(path));
        if (station?.Callsign == null || station.Grid == null || !LicenseClasses.Contains(station.LicenseClass))
            throw new IOException("Invalid saved TX station settings.");
        return station.WithIdentity(station.Callsign, station.Grid);
    }

    internal void Save(string path)
    {
        if (!HasIdentity || !LicenseClasses.Contains(LicenseClass))
            throw new IOException("Enter a U.S. callsign and a four-character grid, then choose your license class.");
        SyncJson.Atomic(path, JsonSerializer.Serialize(this));
    }
}

public partial class MainWindow
{
    private string WeakTxStationPath => Path.Combine(dataFolder, "weak-tx-station.json");

    private WeakTxStation ReadWeakTxStation() => WeakTxStation.Load(WeakTxStationPath, pskCall.Text.Trim(), pskGrid.Text.Trim());

    private void OpenWeakTxStationSettings()
    {
        var station = ReadWeakTxStation();
        var callsign = new TextBox { Text = station.Callsign };
        var grid = new TextBox { Text = station.Grid };
        var license = new ComboBox { ItemsSource = WeakTxStation.LicenseClasses, SelectedItem = station.LicenseClass };
        var fields = new StackPanel { Margin = new Thickness(12) };
        var dialog = new Window
        {
            Owner = this, Title = "TX station settings", Content = fields, Width = 360,
            SizeToContent = SizeToContent.Height, Background = new SolidColorBrush(Color.FromRgb(16, 23, 34)),
            Foreground = Brushes.Gainsboro
        };
        foreach (var (label, input) in new (string, FrameworkElement)[] { ("Callsign", callsign), ("Grid (4 chars)", grid), ("License class", license) })
        {
            fields.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 5, 0, 3) });
            fields.Children.Add(input);
        }
        var save = new Button { Content = "Save station settings", Margin = new Thickness(0, 10, 0, 0) };
        fields.Children.Add(save);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Aquamarine };
        fields.Children.Add(status);
        save.Click += (_, _) =>
        {
            try
            {
                var updated = station.WithIdentity(callsign.Text, grid.Text) with { LicenseClass = (string)license.SelectedItem };
                updated.Save(WeakTxStationPath);
                dialog.DialogResult = true;
            }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        dialog.ShowDialog();
    }
}
