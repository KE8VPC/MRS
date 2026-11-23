using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
namespace MRS;
public partial class MainWindow
{
    private readonly TextBlock foxTrendIndicator = new(), foxGpsIndicator = new(), foxRecordingIndicator = new();
    private FrameworkElement CreateFoxIndicatorOptions()
    {
        var row = new WrapPanel();
        row.Children.Add(new TextBlock { Text = "Status size ", Foreground = Brushes.LightSteelBlue, VerticalAlignment = VerticalAlignment.Center });
        var size = new ComboBox { ItemsSource = new[] { "Compact", "Large", "Extra large" }, SelectedIndex = 1, Width = 120 };
        string path = Path.Combine(dataFolder, "fox-indicators.json");
        try { if (File.Exists(path)) size.SelectedIndex = Math.Clamp(JsonSerializer.Deserialize<int>(File.ReadAllText(path)), 0, 2); }
        catch (Exception ex) { Log("Fox indicator preferences: " + ex.Message); }
        void Apply() { foreach (var text in new[] { foxTrendIndicator, foxGpsIndicator, foxRecordingIndicator }) text.FontSize = new[] { 18.0, 22, 28 }[size.SelectedIndex]; }
        Apply(); size.SelectionChanged += (_, _) => { Apply(); try { File.WriteAllText(path, JsonSerializer.Serialize(size.SelectedIndex)); } catch (Exception ex) { Log("Fox indicator preferences: " + ex.Message); } };
        row.Children.Add(size); return row;
    }
    private FrameworkElement CreateFoxIndicators()
    {
        var row = new WrapPanel();
        foreach (var pair in new[] { ("SIGNAL TREND", foxTrendIndicator), ("POSITION", foxGpsIndicator), ("LOGGING", foxRecordingIndicator) })
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = pair.Item1, Foreground = Brushes.LightSteelBlue, FontSize = 11 });
            pair.Item2.FontWeight = FontWeights.Bold; pair.Item2.TextWrapping = TextWrapping.Wrap;
            stack.Children.Add(pair.Item2);
            row.Children.Add(new Border { Child = stack, Padding = new Thickness(8,4,8,4), Margin = new Thickness(0,2,8,2),
                MinWidth = 180, MaxWidth = 300, BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4) });
        }
        return row;
    }
    private static void SetFoxIndicator(TextBlock control, FoxIndicator value)
    {
        control.Text = value.Symbol + " " + value.Label; control.ToolTip = value.Detail;
        control.Foreground = value.Tone switch { "green" => Brushes.LightGreen, "red" => Brushes.Salmon, "amber" => Brushes.Gold, "grey" => Brushes.Silver, _ => Brushes.Gainsboro };
        System.Windows.Automation.AutomationProperties.SetName(control, value.Label + ". " + value.Detail);
    }
    private void RefreshFoxIndicators(DateTimeOffset now, FoxTrend trend)
    {
        SetFoxIndicator(foxTrendIndicator, FoxIndicators.Trend(trend, foxSession.Running));
        SetFoxIndicator(foxGpsIndicator, FoxIndicators.Gps(foxManual.IsChecked == true, foxGps.Connection, FoxPosition, now));
        SetFoxIndicator(foxRecordingIndicator, FoxIndicators.Recording(foxSession.Running, FoxScopeNow() != null, FoxPosition,
            foxSession.Points.LastOrDefault()?.Utc, (int)foxInterval.SelectedItem, now));
    }
}
