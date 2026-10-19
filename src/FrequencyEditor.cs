using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MRS;
internal sealed class FrequencyEditor : Window
{
    internal FrequencyItem? Result { get; private set; }
    private sealed record Destination(Guid? Id, string Name) { public override string ToString() => Name; }
    internal FrequencyEditor(FrequencyItem item, IReadOnlyList<FrequencyItem> library)
    {
        Title = item.IsFolder ? "Frequency folder" : "Save signal";
        Width = 540; Height = item.IsFolder ? 380 : 760; MinHeight = 320; MaxHeight = SystemParameters.WorkArea.Height - 40;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(16, 23, 34)); Foreground = Brushes.White;
        var layout = new DockPanel { Margin = new Thickness(20) }; Content = layout;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
        var save = new Button { Content = "Save", IsDefault = true }; var cancel = new Button { Content = "Cancel", IsCancel = true };
        actions.Children.Add(save); actions.Children.Add(cancel);
        var body = new StackPanel(); layout.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        TextBox Field(string label, string value, bool multiline = false)
        {
            body.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 8, 0, 4) });
            var box = new TextBox { Text = value, Padding = new Thickness(6), AcceptsReturn = multiline, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap, MinHeight = multiline ? 70 : 30, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            body.Children.Add(box); return box;
        }
        var name = Field("Name", item.Name);
        body.Children.Add(new TextBlock { Text = "Folder (move here)", Margin = new Thickness(0, 8, 0, 4) });
        var destination = new ComboBox(); destination.Items.Add(new Destination(null, "Frequency list /"));
        void AddFolders(Guid? parent, string prefix)
        {
            foreach (var folder in library.Where(n => n.IsFolder && n.ParentId == parent && n.Id != item.Id))
            {
                destination.Items.Add(new Destination(folder.Id, prefix + folder.Name)); AddFolders(folder.Id, prefix + folder.Name + " / ");
            }
        }
        AddFolders(null, ""); destination.SelectedItem = destination.Items.Cast<Destination>().FirstOrDefault(d => d.Id == item.ParentId) ?? destination.Items[0]; body.Children.Add(destination);
        var frequency = item.IsFolder ? null : Field("Frequency (MHz, up to six decimal places)", item.FrequencyHz > 0 ? (item.FrequencyHz / 1000000m).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) : "");
        ComboBox? mode = null;
        if (!item.IsFolder)
        {
            body.Children.Add(new TextBlock { Text = "Receiver mode (NFM / WFM / AM / USB / LSB / CW supported)", Margin = new Thickness(0, 8, 0, 4) });
            mode = new ComboBox { IsEditable = true, ItemsSource = new[] { "NFM", "WFM", "AM", "USB", "LSB", "CW", "Unknown" }, Text = item.Mode }; body.Children.Add(mode);
        }
        var bandwidth = item.IsFolder ? null : Field("Receive filter width (Hz; 0 = mode default)", item.BandwidthHz.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var modulation = item.IsFolder ? null : Field("Modulation details (optional)", item.Modulation);
        var protocols = item.IsFolder ? null : Field("Protocols / services (e.g. P25, RDS / HD)", item.Protocols);
        var signaling = item.IsFolder ? null : Field("Signaling / system / site (e.g. NAC 0x348)", item.Signaling);
        var tags = item.IsFolder ? null : Field("Tags", item.Tags);
        var notes = item.IsFolder ? null : Field("Notes", item.Notes, true);
        var color = Field("Marker color (#RRGGBB)", item.Color);
        var visible = new CheckBox { Content = item.IsFolder ? "Show this folder’s RF markers" : "Show RF marker", IsChecked = item.Visible, Foreground = Brushes.White, Margin = new Thickness(0, 12, 0, 0) }; body.Children.Add(visible);
        var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) }; body.Children.Add(error);
        void ShowError(string message) { error.Text = message; error.BringIntoView(); }
        Loaded += (_, _) => { name.Focus(); name.SelectAll(); };
        save.Click += (_, _) =>
        {
            long hz = 0;
            if (!item.IsFolder && !FrequencyLibrary.TryFrequency(frequency!.Text.Trim(), out hz)) { ShowError("Enter a positive MHz frequency with no more than six decimal places (maximum 1 THz)."); return; }
            double width = 0;
            if (!item.IsFolder && (!double.TryParse(bandwidth!.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out width) || !double.IsFinite(width) || width < 0)) { ShowError("Enter a nonnegative filter width in Hz."); return; }
            var result = item with { Name = name.Text.Trim(), ParentId = ((Destination)destination.SelectedItem).Id, FrequencyHz = hz,
                Mode = mode?.Text.Trim().ToUpperInvariant() ?? item.Mode, BandwidthHz = width, Modulation = modulation?.Text.Trim() ?? "", Protocols = protocols?.Text.Trim() ?? "",
                Signaling = signaling?.Text.Trim() ?? "", Tags = tags?.Text.Trim() ?? "", Notes = notes?.Text ?? "", Color = color.Text.Trim(), Visible = visible.IsChecked == true };
            try { FrequencyLibrary.Validate(library.Where(n => n.Id != result.Id).Append(result).ToList()); }
            catch (Exception ex) { ShowError(ex.Message); return; }
            Result = result; DialogResult = true;
        };
    }
}
