// =====================================================================================
//  Spectrum-layout check  —  added by Claude on 2026-11-16.  Run:  MRS.exe --spectrum-layout-smoke
//  layout2: docked under the fixed main tab row; no right docking; Aviation measured with the map off (the user's setup).
//  For 1040×740 and 1366×768 and each variant (tab only, compact 180 px = default, compact 220 px, full 300 px)
//  it opens the table/map/image-heavy workspaces, saves a screenshot, and measures what is still visible:
//    tables  – rows that fit in the visible part of each DataGrid (compared with the tab-only layout)
//    maps    – visible Aviation / Fox Hunt map area
//    WEFAX   – visible image viewport
//    spectrum– height of the docked spectrum plot
//    tab row – position, height and overflow of the main tab row; it must not move between layouts or workspaces
//  Output: <exe folder>\layout-check\  (PNG files, layout-check.md, layout-check.json, index.html side-by-side page)
// =====================================================================================
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MRS;

internal sealed record LayoutMeasure(string Kind, string Name, double Width, double Height, int Rows);
internal sealed record LayoutResult(string Size, string Workspace, string Variant, string Image, double DockHeight, double SpectrumHeight, LayoutMeasure[] Measures, bool Usable, string Note, double TabRowX, double TabRowY, double TabRowHeight, double TabOverflow);

public partial class MainWindow
{
    private const double TableMinimumRows = 4, MapMinimumHeight = 220, MapMinimumWidth = 300, ImageMinimumHeight = 200, SpectrumMinimumHeight = 140; // SpectrumView draws nothing below 140 px

    internal async Task RenderSpectrumLayoutSmoke(string output)
    {
        Directory.CreateDirectory(output);
        foreach (var old in Directory.GetFiles(output)) File.Delete(old);
        Spectrum.SetRf(851500000, 2400000);
        var rf = FmDemodulator.RfSpectrum(SelfTests.MakeFm(false, 2048));
        double now = DisplaySettings.Clock;
        for (int n = 0; n <= 600; n++) Spectrum.PushAt(rf, now - 30 + n * .05);

        TabItem? Top(string header) => WorkspaceTabs.Items.OfType<TabItem>().FirstOrDefault(t => Equals(t.Header, header));
        static TabItem? Sub(TabItem? parent, string header) => (parent?.Content as TabControl)?.Items.OfType<TabItem>().FirstOrDefault(t => Equals(t.Header, header));
        void AviationMap(bool on)
        {
            UpdateLayout();
            static IEnumerable<CheckBox> Logical(DependencyObject node)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
                { if (child is CheckBox c) yield return c; foreach (var deeper in Logical(child)) yield return deeper; }
            }
            var box = Descendants<CheckBox>(Aviation).Concat(Logical(Aviation)).FirstOrDefault(c => Equals(c.Content, "Show aircraft map")) ?? throw new InvalidOperationException("Show aircraft map checkbox not found");
            box.IsChecked = on;
        }
        var workspaces = new (string Name, Func<TabItem?> Tab, Action? Prepare)[]
        {
            ("P25 Systems", () => P25SystemsTab, null),
            ("P25 Trunk following", () => Sub(P25Workspace, "Trunk following"), null),
            ("P25 Talkgroups", () => Sub(P25Workspace, "Talkgroups"), null),
            ("Commercial DMR-LTR-NXDN", () => DmrTab, null),
            ("Commercial POCSAG-FLEX", () => PagingTab, null),
            ("Amateur APRS", () => PacketsTab, null),
            ("Amateur SSTV", () => Sub(AmateurTab, "SSTV"), null),
            ("Amateur RTTY", () => Sub(AmateurTab, "RTTY"), null),
            ("Analysis Sensors", () => SensorsTab, null),
            ("Analysis Messages-CW", () => ObservationsTab, null),
            ("Aviation (map off)", () => Sub(AviationTab, "Aircraft / map"), () => AviationMap(false)),
            ("Aviation (map on)", () => Sub(AviationTab, "Aircraft / map"), () => AviationMap(true)),
            ("Fox Hunt", () => Top("Fox Hunt"), null),
            ("Weather WEFAX", () => Sub(Top("Weather"), "WEFAX"), null),
        };
        var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var variants = new (string Name, SpectrumLayout Layout)[]
        {
            ("Tab only", new SpectrumLayout { Placement = SpectrumPlacements.Tab, Overrides = empty }),
            ("Compact 180 (default)", new SpectrumLayout { Placement = SpectrumPlacements.DockTop, Compact = true, CompactHeight = 180, HeightCapPercent = 80, Overrides = empty }),
            ("Compact 220", new SpectrumLayout { Placement = SpectrumPlacements.DockTop, Compact = true, CompactHeight = 220, HeightCapPercent = 80, Overrides = empty }),
            ("Full 300", new SpectrumLayout { Placement = SpectrumPlacements.DockTop, Compact = false, FullHeight = 300, HeightCapPercent = 80, Overrides = empty }),
        };
        var results = new List<LayoutResult>();
        foreach (var (w, h) in new[] { (1040, 740), (1366, 768) })
        {
            Width = w; Height = h; SidebarColumn.Width = new GridLength(330);
            string size = $"{w}x{h}";
            foreach (var (name, find, prepare) in workspaces)
            {
                var tab = find() ?? throw new InvalidOperationException("Layout check could not find workspace " + name);
                Dictionary<string, int> baselineRows = new();
                foreach (var (variant, layout) in variants)
                {
                    ApplySpectrumLayoutForTest(layout);
                    SelectWorkspace(tab);
                    prepare?.Invoke();
                    await Settle();
                    string file = $"{size}-{Slug(name)}-{Slug(variant)}.png";
                    CaptureWindow(this, Path.Combine(output, file));
                    var measures = MeasureWorkspace();
                    if (variant == "Tab only") foreach (var m in measures.Where(m => m.Kind == "table")) baselineRows[m.Name] = m.Rows;
                    double dock = CurrentSpectrumPlacement == SpectrumPlacements.DockTop ? dockTopRow.ActualHeight : 0;
                    double plot = dock > 0 && Spectrum.IsVisible ? Spectrum.ActualHeight : 0;
                    var (usable, note) = Judge(measures, baselineRows, dock > 0, plot);
                    var origin = MainTabRowOrigin;
                    results.Add(new LayoutResult(size, name, variant, file, Math.Round(dock), Math.Round(plot), measures, usable, note, Math.Round(origin.X, 1), Math.Round(origin.Y, 1), Math.Round(MainTabRowHeight, 1), Math.Round(MainTabOverflow, 1)));
                }
            }
        }
        // The main tab row must stay in exactly the same place for every layout and workspace at a given size.
        foreach (var size in results.GroupBy(r => r.Size))
        {
            var first = size.First();
            var moved = size.Where(r => Math.Abs(r.TabRowX - first.TabRowX) > .5 || Math.Abs(r.TabRowY - first.TabRowY) > .5 || Math.Abs(r.TabRowHeight - first.TabRowHeight) > .5).ToArray();
            if (moved.Length > 0) throw new InvalidOperationException($"Main tab row moved at {size.Key}: {moved[0].Workspace} / {moved[0].Variant} at ({moved[0].TabRowX},{moved[0].TabRowY}) h {moved[0].TabRowHeight} vs ({first.TabRowX},{first.TabRowY}) h {first.TabRowHeight}");
            if (size.Max(r => r.TabOverflow) > 0.5) throw new InvalidOperationException($"Main tabs do not fit in one row at {size.Key} ({size.Max(r => r.TabOverflow):0} px over); scrolling would move them. {MainTabDiagnostics}");
        }
        // Reference shots of the separate window and the preview defaults (not part of the pass/fail matrix).
        Width = 1366; Height = 768;
        SelectWorkspace(P25SystemsTab);
        ApplySpectrumLayoutForTest(new SpectrumLayout { Placement = SpectrumPlacements.Window, Overrides = empty });
        await Settle();
        if (spectrumWindow == null || !ReferenceEquals(SharedSpectrum.Parent, spectrumWindowHost)) throw new InvalidOperationException("Separate spectrum window did not receive the spectrum.");
        spectrumWindow.UpdateLayout(); CaptureWindow(spectrumWindow, Path.Combine(output, "reference-separate-spectrum-window.png"));
        CaptureWindow(this, Path.Combine(output, "reference-1366x768-p25-systems-while-window-open.png"));
        spectrumWindow.Close(); await Settle();
        if (spectrumLayout.Placement != SpectrumPlacements.Tab || spectrumWindow != null)
            throw new InvalidOperationException("Closing the spectrum window did not return the spectrum.");
        SelectWorkspace(SpectrumTab); await Settle();
        if (!ReferenceEquals(SpectrumTab.Content, SharedSpectrum)) throw new InvalidOperationException("Spectrum tab did not get the spectrum back after the window closed.");
        // Preview defaults: docked under the tabs, compact, no preset exceptions.
        if (SpectrumLayout.Defaults().Overrides.Count != 0) throw new InvalidOperationException("Preview defaults must not contain preset exceptions.");
        ApplySpectrumLayoutForTest(SpectrumLayout.Defaults()); Width = 1040; Height = 740;
        foreach (var tab in new TabItem?[] { P25SystemsTab, AviationTab, Top("Fox Hunt"), Top("Weather"), AnalysisTab, HdRadioTab })
        {
            SelectWorkspace(tab!); await Settle();
            if (CurrentSpectrumPlacement != SpectrumPlacements.DockTop) throw new InvalidOperationException($"Preview defaults did not dock the spectrum on {tab!.Header}.");
        }
        SelectWorkspace(P25SystemsTab); await Settle();
        CaptureWindow(this, Path.Combine(output, "reference-1040x740-p25-systems-preview-defaults.png"));
        // Choosing a layout for all workspaces clears exceptions, so the result always matches the last choice (review note 1).
        SetWorkspaceOverride("P25", SpectrumPlacements.Tab); await Settle();
        if (CurrentSpectrumPlacement != SpectrumPlacements.Tab) throw new InvalidOperationException("Workspace exception was not applied.");
        SetSpectrumPlacement(SpectrumPlacements.DockTop); await Settle();
        if (CurrentSpectrumPlacement != SpectrumPlacements.DockTop || spectrumLayout.Overrides.Count != 0) throw new InvalidOperationException("Choosing a layout for all workspaces did not clear the exception.");
        // A layout1 settings file that says DockRight must load as docked under the tabs.
        var legacy = (JsonSerializer.Deserialize<SpectrumLayout>("{\"Placement\":\"DockRight\",\"Overrides\":{\"Amateur\":\"DockRight\"}}") ?? throw new InvalidOperationException()).Normalize();
        if (legacy.Placement != SpectrumPlacements.DockTop || legacy.Overrides.Count != 0) throw new InvalidOperationException("layout1 DockRight settings were not migrated.");
        WriteLayoutReport(output, results, variants.Select(v => v.Name).ToArray());
        ApplySpectrumLayoutForTest(SpectrumLayout.Defaults() with { Placement = SpectrumPlacements.Tab });
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject node) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T match) yield return match;
            foreach (var deeper in Descendants<T>(child)) yield return deeper;
        }
    }

    private async Task Settle()
    {
        UpdateLayout(); await Task.Delay(80);
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        UpdateLayout();
    }

    private static string Slug(string text) => new string(text.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Replace("--", "-").Trim('-');

    private static void CaptureWindow(Window window, string path)
    {
        var bitmap = new RenderTargetBitmap(Math.Max(1, (int)window.ActualWidth), Math.Max(1, (int)window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); png.Save(file);
    }

    /// <summary>Visible size of an element inside the workspace tabs, after clipping by every scrolling ancestor.</summary>
    private Rect VisibleInWorkspace(FrameworkElement element)
    {
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return Rect.Empty;
        Rect bounds = element.TransformToAncestor(WorkspaceTabs).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bounds.Intersect(new Rect(0, 0, WorkspaceTabs.ActualWidth, WorkspaceTabs.ActualHeight));
        for (DependencyObject? node = VisualTreeHelper.GetParent(element); node != null && !ReferenceEquals(node, WorkspaceTabs); node = VisualTreeHelper.GetParent(node))
        {
            if (bounds.IsEmpty) break;
            if (node is ScrollContentPresenter presenter && presenter.ActualHeight > 0)
                bounds.Intersect(presenter.TransformToAncestor(WorkspaceTabs).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight)));
        }
        return bounds;
    }

    private LayoutMeasure[] MeasureWorkspace()
    {
        var found = new List<LayoutMeasure>();
        void Walk(DependencyObject node)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                if (child is DataGrid grid && grid.IsVisible)
                {
                    var r = VisibleInWorkspace(grid);
                    double header = FindChild<DataGridColumnHeadersPresenter>(grid)?.ActualHeight ?? 28;
                    double row = FindChild<DataGridRow>(grid)?.ActualHeight is > 0 and var realized ? realized : (double.IsFinite(grid.RowHeight) ? grid.RowHeight : 24);
                    int rows = r.IsEmpty ? 0 : (int)Math.Max(0, Math.Floor((r.Height - header - 2) / row));
                    string name = grid.Columns.FirstOrDefault()?.Header?.ToString() ?? "table";
                    if (grid.Columns.Count > 1) name += " / " + grid.Columns[1].Header;
                    found.Add(new LayoutMeasure("table", name, Math.Round(r.IsEmpty ? 0 : r.Width), Math.Round(r.IsEmpty ? 0 : r.Height), rows));
                    continue;
                }
                if (child is AircraftMap or FoxMap && child is FrameworkElement map && map.IsVisible)
                {
                    var r = VisibleInWorkspace(map);
                    found.Add(new LayoutMeasure("map", child is FoxMap ? "Fox Hunt map" : "Aircraft map", Math.Round(r.IsEmpty ? 0 : r.Width), Math.Round(r.IsEmpty ? 0 : r.Height), 0));
                    continue;
                }
                if (child is ScrollViewer viewer && (ReferenceEquals(viewer.Content, faxImage) || ReferenceEquals(viewer.Content, sstvImage)) && viewer.IsVisible)
                {
                    var r = VisibleInWorkspace(viewer);
                    found.Add(new LayoutMeasure("image", ReferenceEquals(viewer.Content, sstvImage) ? "SSTV image" : "WEFAX image", Math.Round(r.IsEmpty ? 0 : r.Width), Math.Round(r.IsEmpty ? 0 : r.Height), 0));
                    continue;
                }
                if (ReferenceEquals(child, rttyTranscript))
                {
                    var r = VisibleInWorkspace(rttyTranscript);
                    found.Add(new LayoutMeasure("text", "RTTY transcript", Math.Round(r.IsEmpty ? 0 : r.Width),
                        Math.Round(r.IsEmpty ? 0 : r.Height), 0));
                    continue;
                }
                Walk(child);
            }
        }
        Walk(WorkspaceTabs);
        return found.ToArray();
    }

    private static T? FindChild<T>(DependencyObject node) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T match) return match;
            if (FindChild<T>(child) is { } deeper) return deeper;
        }
        return null;
    }

    private static (bool Usable, string Note) Judge(LayoutMeasure[] measures, Dictionary<string, int> baselineRows, bool docked, double plot)
    {
        var problems = new List<string>();
        foreach (var m in measures)
        {
            if (m.Kind == "table")
            {
                int baseline = baselineRows.GetValueOrDefault(m.Name, m.Rows);
                int needed = (int)Math.Min(TableMinimumRows, baseline);
                if (m.Rows < needed) problems.Add($"table '{m.Name}' shows {m.Rows} rows (needs {needed}; tab-only {baseline})");
            }
            else if (m.Kind == "map" && (m.Height < MapMinimumHeight || m.Width < MapMinimumWidth)) problems.Add($"{m.Name} {m.Width}×{m.Height} px (needs ≥ {MapMinimumWidth}×{MapMinimumHeight})");
            else if (m.Kind == "text" && m.Height < 120) problems.Add($"{m.Name} viewport {m.Height} px tall (needs ≥ 120)");
            else if (m.Kind == "image" && m.Height < ImageMinimumHeight) problems.Add($"{m.Name} viewport {m.Height} px tall (needs ≥ {ImageMinimumHeight})");
        }
        if (docked && plot < SpectrumMinimumHeight) problems.Add($"docked spectrum only {plot:0} px tall (needs ≥ {SpectrumMinimumHeight})");
        if (measures.Length == 0) problems.Add("no table, map or image found to measure");
        return (problems.Count == 0, problems.Count == 0 ? "usable" : string.Join("; ", problems));
    }

    private static void WriteLayoutReport(string output, List<LayoutResult> results, string[] variants)
    {
        File.WriteAllText(Path.Combine(output, "layout-check.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        var md = new StringBuilder();
        md.AppendLine("# Spectrum layout check · layout2").AppendLine();
        md.AppendLine($"Generated {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz} by `MRS.exe --spectrum-layout-smoke` (synthetic, offscreen render at 96 DPI).").AppendLine();
        md.AppendLine($"Usable means: every table shows at least {TableMinimumRows} rows (or as many as the tab-only layout shows, if fewer); maps at least {MapMinimumWidth}×{MapMinimumHeight} px visible; WEFAX image viewport at least {ImageMinimumHeight} px tall; docked spectrum plot at least {SpectrumMinimumHeight} px tall.").AppendLine();
        md.AppendLine("| Size | Workspace | " + string.Join(" | ", variants) + " |");
        md.AppendLine("|---|---|" + string.Concat(variants.Select(_ => "---|")));
        foreach (var group in results.GroupBy(r => (r.Size, r.Workspace)))
            md.AppendLine($"| {group.Key.Size} | {group.Key.Workspace} | " + string.Join(" | ", variants.Select(v => group.FirstOrDefault(r => r.Variant == v) is { } r ? (r.Usable ? "✅ usable" : "⚠️ short") : "–")) + " |");
        md.AppendLine().AppendLine("## Main tab row").AppendLine();
        foreach (var size in results.GroupBy(r => r.Size))
        {
            var f = size.First();
            double overflow = size.Max(r => r.TabOverflow);
            md.AppendLine($"- **{size.Key}**: same position ({f.TabRowX}, {f.TabRowY}) and height {f.TabRowHeight} px in all {size.Count()} screenshots; single row; " +
                          (overflow > 0.5 ? $"**{overflow:0} px wider than the space available — small scroll buttons appear at the end of the row**" : "all tabs fit (no scroll buttons)."));
        }
        md.AppendLine().AppendLine("## Docked under the tabs: what fits").AppendLine();
        md.AppendLine("Default is compact 180 px (the smallest dock where the spectrum actually draws: SpectrumView needs 140 px). A workspace that is short at the default can be given an exception (Only this workspace → Spectrum tab only) by the user; layout2 sets no exceptions itself.").AppendLine();
        foreach (var workspace in results.GroupBy(r => r.Workspace))
        {
            bool Passes(string variant) => workspace.Where(r => r.Variant == variant).All(r => r.Usable);
            string outcome = Passes("Full 300") ? "usable docked at every size tested, even full 300 px"
                : Passes("Compact 220") ? "usable docked compact (180 or 220 px)"
                : Passes("Compact 180 (default)") ? "usable docked at the default compact 180 px"
                : workspace.Where(r => r.Variant == "Compact 180 (default)").Any(r => r.Usable) ? "usable at the default compact 180 px at " + string.Join(" and ", workspace.Where(r => r.Variant == "Compact 180 (default)" && r.Usable).Select(r => r.Size)) + " only"
                : "short when docked at both sizes";
            md.AppendLine($"- **{workspace.Key}**: {outcome}" + (Passes("Tab only") ? "" : " (already short with the spectrum on its own tab)"));
        }
        md.AppendLine().AppendLine("## Details").AppendLine();
        foreach (var r in results)
        {
            md.AppendLine($"- **{r.Size} · {r.Workspace} · {r.Variant}**: {(r.Usable ? "usable" : "SHORT")}. Dock {r.DockHeight} px, tab row at ({r.TabRowX}, {r.TabRowY}), spectrum plot {r.SpectrumHeight} px. " +
                          string.Join("; ", r.Measures.Select(m => m.Kind == "table" ? $"{m.Name}: {m.Rows} rows ({m.Height} px)" : $"{m.Name}: {m.Width}×{m.Height} px")) +
                          (r.Usable ? "" : $". Problem: {r.Note}") + $". [{r.Image}]({r.Image})");
        }
        File.WriteAllText(Path.Combine(output, "layout-check.md"), md.ToString());

        static string H(string s) => System.Net.WebUtility.HtmlEncode(s);
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>M.R.S. spectrum layout check</title><style>")
            .Append("body{background:#101722;color:#E4EDF8;font-family:Segoe UI,Arial,sans-serif;margin:20px}h1{font-size:22px}h2{font-size:17px;margin-top:28px;border-bottom:1px solid #35445B;padding-bottom:4px}")
            .Append("table{border-collapse:collapse;width:100%}td,th{vertical-align:top;padding:6px;border:1px solid #26364A}th{background:#1A2636;text-align:left}")
            .Append("img{width:100%;max-width:520px;border:1px solid #35445B;display:block}.ok{color:#66DEBA;font-weight:600}.short{color:#FF8A8A;font-weight:600}.m{font-size:12px;color:#9EB1C9}")
            .Append("</style></head><body><h1>M.R.S. spectrum layout check · layout2 (docked under fixed tabs)</h1>")
            .Append($"<p class='m'>Generated {H(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz"))} from synthetic offscreen renders. Click a picture to open it full size. ")
            .Append($"Usable = tables ≥ {TableMinimumRows} rows (or the tab-only count if fewer), maps ≥ {MapMinimumWidth}×{MapMinimumHeight} px, WEFAX viewport ≥ {ImageMinimumHeight} px, docked spectrum plot ≥ {SpectrumMinimumHeight} px.</p>");
        foreach (var workspace in results.GroupBy(r => r.Workspace))
        {
            html.Append($"<h2>{H(workspace.Key)}</h2><table><tr><th>Size</th>");
            foreach (var v in variants) html.Append($"<th>{H(v)}</th>");
            html.Append("</tr>");
            foreach (var size in workspace.GroupBy(r => r.Size))
            {
                html.Append($"<tr><th>{H(size.Key)}</th>");
                foreach (var v in variants)
                {
                    var r = size.FirstOrDefault(x => x.Variant == v);
                    if (r == null) { html.Append("<td>–</td>"); continue; }
                    html.Append($"<td><a href='{H(r.Image)}'><img src='{H(r.Image)}' alt='{H(r.Workspace + " " + r.Variant)}'></a>")
                        .Append($"<div class='{(r.Usable ? "ok" : "short")}'>{(r.Usable ? "Usable" : "Short")}</div><div class='m'>")
                        .Append(H(string.Join(" · ", r.Measures.Select(m => m.Kind == "table" ? $"{m.Name}: {m.Rows} rows" : $"{m.Name}: {m.Width}×{m.Height}"))))
                        .Append(r.DockHeight > 0 ? H($" · dock {r.DockHeight} px, plot {r.SpectrumHeight} px") : "")
                        .Append(r.Usable ? "" : "<br>" + H(r.Note)).Append("</div></td>");
                }
                html.Append("</tr>");
            }
            html.Append("</table>");
        }
        html.Append("<h2>Reference</h2><table><tr><th>Separate window</th><th>Preview defaults · P25 Systems · 1040×740</th></tr><tr>")
            .Append("<td><a href='reference-separate-spectrum-window.png'><img src='reference-separate-spectrum-window.png'></a></td>")
            .Append("<td><a href='reference-1040x740-p25-systems-preview-defaults.png'><img src='reference-1040x740-p25-systems-preview-defaults.png'></a></td></tr></table></body></html>");
        File.WriteAllText(Path.Combine(output, "index.html"), html.ToString());
    }
}
