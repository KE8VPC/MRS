using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MRS;

internal sealed record SstvImageInfo(string Mode, DateTimeOffset StartedUtc, string Source, long? FrequencyHz,
    bool Replay, double? ReplaySeconds, int Width, int Rows, bool Complete, int ClockPpm, int ToneOffsetHz, int ShiftPixels,
    bool HadSyncLoss=false);

public partial class MainWindow
{
    private SlowScanTelevision? sstvDecoder;
    private readonly Image sstvImage = new() { Stretch=Stretch.None, HorizontalAlignment=HorizontalAlignment.Left,
        VerticalAlignment=VerticalAlignment.Top };
    private readonly TextBlock sstvStatus = ModeStatus();
    private readonly List<byte[]> sstvLines = [];
    private SstvImageInfo? sstvInfo;
    private int sstvRevision, sstvDrawn = -1, sstvShift;

    private void InitializeSstv(TabControl amateur)
    {
        AddModePage(amateur, "SSTV", new ScrollViewer { Content=sstvImage,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Auto, VerticalScrollBarVisibility=ScrollBarVisibility.Auto },
            out var tools, out var header);
        var mode = new ComboBox { ItemsSource=new[] { "Auto (Martin M1 / M2)", "Martin M1", "Martin M2" },
            SelectedIndex=0, Width=180 };
        var ppm = new TextBox { Text="0", Width=65 };
        var offset = new TextBox { Text="0", Width=60 };
        var shift = new TextBox { Text="0", Width=65 };
        ModeFields(tools, ("Mode",mode), ("Slant ppm",ppm), ("Tone offset Hz",offset), ("Shift pixels",shift));
        header.Children.Add(sstvStatus);
        sstvStatus.Text="Stopped · use USB, LSB, AM, NFM, Windows audio input or WAV";
        ModeButton(tools, "Start receiving", () =>
        {
            if (rfMode && receiver.AppliedChannel?.Mode is not ("USB" or "LSB" or "AM" or "NFM"))
                throw new IOException("Choose USB, LSB, AM or NFM with enough bandwidth for 1100–2300 Hz tones.");
            int clock = ModeNumber(ppm, "Slant", -10000, 10000);
            int frequencyOffset = ModeNumber(offset, "Tone offset", -300, 300);
            var decoder = new SlowScanTelevision(mode.SelectedIndex == 0 ? null :
                SstvMode.Supported[mode.SelectedIndex-1], clock, frequencyOffset);
            decoder.ImageStarted += selected =>
            {
                sstvLines.Clear();
                sstvShift=0;
                var context=sstvAudioContext;
                sstvInfo=new(selected.Name, DateTimeOffset.UtcNow, context?.Source ?? "Unknown source",
                    context?.FrequencyHz, context?.Replay ?? false, context?.PositionSeconds,
                    selected.Width, 0, false, clock, frequencyOffset, 0);
                sstvRevision++;
            };
            decoder.Line += (_,pixels) =>
            {
                if(sstvLines.Count>=SstvMode.Height)return;
                sstvLines.Add(pixels);
                sstvInfo=sstvInfo! with { Rows=sstvLines.Count,
                    Complete=sstvLines.Count==SstvMode.Height&&!decoder.HadSyncLoss };
                sstvRevision++;
            };
            decoder.SyncLost+=()=>{if(sstvInfo!=null)sstvInfo=sstvInfo with{HadSyncLoss=true,Complete=false};};
            lock(receiveModeSync)
            {
                sstvDecoder=decoder;
                sstvAudioContext=null;
            }
            sstvStatus.Text=decoder.Status;
        });
        ModeButton(tools, "Stop", () =>
        {
            lock(receiveModeSync)sstvDecoder=null;
            sstvStatus.Text="Stopped · image retained";
        });
        ModeButton(tools, "Clear image", () =>
        {
            lock(receiveModeSync)
            {
                sstvDecoder=null;sstvInfo=null;sstvLines.Clear();sstvRevision++;
            }
            DrawSstv();
            sstvStatus.Text="Cleared · start receiving for another image";
        });
        ModeButton(tools, "Align", () =>
        {
            int pixels=ModeNumber(shift,"Shift",-319,319);
            lock(receiveModeSync)
            {
                sstvShift=pixels;
                if(sstvInfo!=null)sstvInfo=sstvInfo with{ShiftPixels=pixels};
                sstvRevision++;
            }
            DrawSstv();
        });
        ModeButton(tools, "Save PNG…", SaveSstv);
    }

    private PacketContext? sstvAudioContext;

    private void DrawSstv()
    {
        byte[][] lines;
        SstvImageInfo? info;
        int shift;
        lock(receiveModeSync)
        {
            if(sstvDrawn==sstvRevision)return;
            sstvDrawn=sstvRevision;
            lines=sstvLines.ToArray();info=sstvInfo;shift=sstvShift;
        }
        if(info==null||lines.Length==0){sstvImage.Source=null;return;}
        int stride=info.Width*3;
        var image=new byte[stride*lines.Length];
        int split=((shift%info.Width+info.Width)%info.Width)*3;
        for(int n=0;n<lines.Length;n++)
        {
            lines[n].AsSpan(split).CopyTo(image.AsSpan(n*stride));
            lines[n].AsSpan(0,split).CopyTo(image.AsSpan((n+1)*stride-split));
        }
        sstvImage.Source=BitmapSource.Create(info.Width,lines.Length,96,96,PixelFormats.Rgb24,null,image,stride);
    }

    private void SaveSstv()
    {
        // Capture the pixels and metadata under the same lock as the decoder.
        // Stop before displaying the file dialog so the saved image is stable.
        SstvImageInfo info;
        lock(receiveModeSync)
        {
            if(sstvInfo==null||sstvLines.Count==0)throw new IOException("Receive at least one image line first.");
            sstvDecoder=null;
            info=sstvInfo;
        }
        DrawSstv();
        if(sstvImage.Source is not BitmapSource bitmap)return;
        var picker=new SaveFileDialog { Filter="PNG image|*.png",
            FileName="MRS-SSTV-"+info.StartedUtc.ToString("yyyyMMdd-HHmmss") };
        if(picker.ShowDialog(this)!=true)return;
        var encoder=new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=File.Create(picker.FileName))encoder.Save(file);
        File.WriteAllText(Path.ChangeExtension(picker.FileName,"json"),
            JsonSerializer.Serialize(info,new JsonSerializerOptions{WriteIndented=true}));
        sstvStatus.Text=$"Saved {info.Rows} rows · {info.Mode} · metadata saved beside the image";
    }

    private void TickSstv()
    {
        lock(receiveModeSync)
        {
            if(sstvDecoder!=null)sstvStatus.Text=sstvDecoder.Status;
            else if(sstvStatus.Text is not null && (sstvStatus.Text.StartsWith("Waiting") ||
                sstvStatus.Text.StartsWith("Martin") || sstvStatus.Text.StartsWith("Sync lost")))
                sstvStatus.Text="Stopped · partial image retained";
        }
        DrawSstv();
    }
}
