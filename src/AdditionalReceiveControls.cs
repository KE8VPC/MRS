using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MRS;

public partial class MainWindow
{
    private readonly object receiveModeSync = new();
    private ConversationCw? conversationCw;
    private WeatherFax? weatherFax;
    private RailDecoder railReceiver = new();
    private volatile bool railEnabled;
    private readonly Queue<PacketObservation> railPending = new();
    private readonly StringBuilder cwPending = new();
    private readonly List<byte[]> faxLines = [];
    private PacketContext? railContext;
    private string? audioModeSource;
    private readonly TextBox cwTranscript = new() { IsReadOnly=true, TextWrapping=TextWrapping.Wrap,
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    private readonly TextBlock cwStatus = ModeStatus(), faxStatus = ModeStatus(), cellularSurveyStatus = ModeStatus();
    private readonly Image faxImage = new() { Stretch=Stretch.None, HorizontalAlignment=HorizontalAlignment.Left,
        VerticalAlignment=VerticalAlignment.Top };
    private readonly PacketPanel railMessages = new();
    private readonly ComboBox surveyBand = new() { Width=260, ItemsSource=CellularActivity.Bands, SelectedIndex=0 };
    private readonly TextBox surveyMargin = new() { Text="8", Width=56 };
    private readonly TextBox surveyResults = new() { IsReadOnly=true, TextWrapping=TextWrapping.Wrap };
    private volatile bool surveyActive;
    private int activeSurveyMargin = 8;
    private (float[] Bins,long Center,int Rate,double Usable,long Stamp)? surveyFrame;
    private long nextSurveyAt, lastSurveyFrame;
    private int displayedFaxRows, faxShift;

    private static TextBlock ModeStatus() => new() { Foreground=Brushes.LightSteelBlue,
        TextWrapping=TextWrapping.Wrap, Margin=new(0,5,0,5) };
    private static DockPanel AddModePage(TabControl tabs,string name,UIElement body,out WrapPanel tools,out StackPanel header)
    {
        var root=new DockPanel{Margin=new(10)};
        header=new StackPanel();
        DockPanel.SetDock(header,Dock.Top);
        root.Children.Add(header);
        tools=new WrapPanel();
        header.Children.Add(tools);
        root.Children.Add(body);
        tabs.Items.Add(new TabItem { Header=name, Content=root });
        return root;
    }
    private static void ModeButton(Panel row,string title,Action action)
    {
        var button=new Button { Content=title, Margin=new(3), Padding=new(9,4,9,4) };
        row.Children.Add(button);
        button.Click+=(_,_)=>{
            try { action(); }
            catch(Exception ex) when(ex is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
            { MessageBox.Show(ex.Message,title); }
        };
    }
    private static void ModeFields(Panel row, params (string Label, Control Field)[] entries)
    {
        foreach(var (label,field) in entries)
        {
            var box=new StackPanel { Margin=new(3) };
            box.Children.Add(new TextBlock{Text=label,Foreground=Brushes.Gainsboro});
            box.Children.Add(field);
            row.Children.Add(box);
        }
    }
    private static int ModeNumber(TextBox box,string name,int min,int max)
    {
        if(!int.TryParse(box.Text,NumberStyles.Integer,CultureInfo.InvariantCulture,out int n)||n<min||n>max)
            throw new ArgumentException($"{name} must be {min}–{max}.");
        return n;
    }
    private void InitializeAdditionalReceive()
    {
        var amateur=(TabControl)AmateurTab.Content;
        InitializeSstv(amateur);
        InitializeRtty(amateur);
        var pitch=new TextBox{Text="700",Width=65};
        var wpm=new TextBox{Text="18",Width=55};
        var spacing=new TextBox{Text="18",Width=55};
        AddModePage(amateur,"CW",cwTranscript,out var cwTools,out var cwHeader);
        ModeFields(cwTools,("Tone Hz",pitch),("Character WPM",wpm),("Spacing WPM",spacing));
        cwHeader.Children.Add(cwStatus);
        cwStatus.Text="Stopped · CW/USB/LSB receiver audio, Windows audio input or WAV";
        ModeButton(cwTools,"Use receiver pitch",()=>pitch.Text=receiver.CwPitchHz.ToString("F0",CultureInfo.InvariantCulture));
        ModeButton(cwTools,"Start CW decoding",()=>{
            if(rfMode&&receiver.AppliedChannel?.Mode is not ("CW" or "USB" or "LSB"))
                throw new IOException("Select CW, USB or LSB before decoding SDR audio.");
            var decoder=new ConversationCw(ModeNumber(pitch,"Tone",300,1600),ModeNumber(wpm,"Character WPM",5,45),
                ModeNumber(spacing,"Spacing WPM",3,45));
            decoder.Character+=c=>{if(cwPending.Length<4000)cwPending.Append(c);};
            lock(receiveModeSync){conversationCw=decoder;cwPending.Clear();}
            cwStatus.Text="Decoding · match tone and speed to the station";
        });
        ModeButton(cwTools,"Stop",()=>{
            lock(receiveModeSync){conversationCw?.FlushCharacter();conversationCw=null;}
            cwStatus.Text="Stopped";
        });
        ModeButton(cwTools,"Clear text",()=>{lock(receiveModeSync)cwPending.Clear();cwTranscript.Clear();});

        var ioc=new ComboBox{ItemsSource=new[]{576,288},SelectedIndex=0,Width=64};
        var lpm=new ComboBox{ItemsSource=new[]{60,90,120,180,240},SelectedIndex=2,Width=64};
        var offset=new TextBox{Text="0",Width=55};
        var ppm=new TextBox{Text="0",Width=60};
        var align=new TextBox{Text="0",Width=55};
        var weatherTabs=CreateWeatherWorkspace();
        AddModePage(weatherTabs,"WEFAX",new ScrollViewer { Content=faxImage, HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility=ScrollBarVisibility.Auto },out var faxTools,out var faxHeader);
        AddFaxSchedule(faxHeader);
        InitializeNoaaWeather(weatherTabs);
        ModeFields(faxTools,("IOC",ioc),("Lines/min",lpm),("Tone offset Hz",offset),("Slant ppm",ppm),("Shift pixels",align));
        faxHeader.Children.Add(faxStatus);
        faxStatus.Text="Manual start · use USB with a clear 1500–2300 Hz fax tone, or open an audio WAV";
        ModeButton(faxTools,"Start image",()=>{
            if(rfMode&&receiver.AppliedChannel?.Mode!="USB")
                throw new IOException("Select USB before starting an SDR weather fax.");
            var decoder=new WeatherFax((int)ioc.SelectedItem,(int)lpm.SelectedItem,
                ModeNumber(offset,"Tone offset",-300,300),ModeNumber(ppm,"Slant",-10000,10000));
            decoder.Line+=line=>{if(faxLines.Count<2400)faxLines.Add(line);};
            lock(receiveModeSync){faxLines.Clear();weatherFax=decoder;}
            displayedFaxRows=0;faxShift=0;faxImage.Source=null;
            faxStatus.Text="Receiving · manual start";
        });
        ModeButton(faxTools,"Stop",()=>{lock(receiveModeSync)weatherFax=null;faxStatus.Text="Stopped";});
        ModeButton(faxTools,"Align",()=>{faxShift=ModeNumber(align,"Shift",-1809,1809);DrawFax(true);});
        ModeButton(faxTools,"Save PNG…",()=>{
            DrawFax(true);
            if(faxImage.Source is not BitmapSource bitmap)throw new IOException("Capture at least one line first.");
            var dialog=new SaveFileDialog{Filter="PNG image|*.png",FileName="MRS-WEFAX-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")};
            if(dialog.ShowDialog(this)!=true)return;
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output=File.Create(dialog.FileName);encoder.Save(output);
        });

        railMessages.ConfigureWorkspace("RadioData");
        AddModePage((TabControl)ProfessionalTab.Content,"Railroad",railMessages,out var railTools,out var railHeader);
        var railStatus=ModeStatus();
        railStatus.Text="NFM · tune the applicable channel, then enable railroad decoding";
        railHeader.Children.Add(railStatus);
        var railToggle=new CheckBox{Content="Decode HOT / EOT",Foreground=Brushes.Gainsboro,Margin=new(6)};
        railTools.Children.Add(railToggle);
        railToggle.Checked+=(_,_)=>{railEnabled=true;UpdateDataCapture();};
        railToggle.Unchecked+=(_,_)=>{railEnabled=false;ResetAdditionalReceive(false);UpdateDataCapture();};
        ModeButton(railTools,"Tune HOT 452.9375 MHz",()=>TuneRail(452937500));
        ModeButton(railTools,"Tune EOT 457.9375 MHz",()=>TuneRail(457937500));
        monitorOrders.Add(new(railMessages.MonitorGrid,Path.Combine(dataFolder,"rail-monitor-order.json")));

        AddModePage(AnalysisTabs,"Cellular survey",surveyResults,out var surveyTools,out var surveyHeader);
        ModeFields(surveyTools,("U.S. reference band",surveyBand),("Above floor dB",surveyMargin));
        surveyHeader.Children.Add(cellularSurveyStatus);
        cellularSurveyStatus.Text="Start a live HackRF capture. Survey measures RF activity within the current capture.";
        ModeButton(surveyTools,"Start survey",()=>{
            if(!rfMode||receiver.IsReplay||!receiver.IsHackRf)throw new IOException("Start the live HackRF receiver first.");
            activeSurveyMargin=ModeNumber(surveyMargin,"Margin",3,30);
            surveyActive=true;
        });
        ModeButton(surveyTools,"Stop",()=>{surveyActive=false;cellularSurveyStatus.Text="Stopped";});
        surveyBand.SelectionChanged+=(_,_)=>{surveyResults.Clear();lastSurveyFrame=0;};

        receiver.Spectrum+=bins=>{
            if(!surveyActive || receiver.IsReplay || !receiver.IsHackRf)return;
            lock(frameSync)surveyFrame=(bins,receiver.AppliedFrequency,receiver.AppliedRate,receiver.AppliedBandwidth,Environment.TickCount64);
        };
        receiver.DataSamples+=samples=>{
            if(!railEnabled || receiver.AppliedChannel is not {Mode:"NFM"} ch || RailProfile(ch.Frequency)==null)return;
            var context=RadioContext("rail",ch.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null);
            lock(receiveModeSync)
            {
                if(!railEnabled)return;
                if(railContext?.Key!=context.Key)NewRailDecoder();
                railContext=context;
                railReceiver.Feed(samples);
            }
        };
        receiver.Audio+=pcm=>{
            if(receiver.AppliedChannel is {Mode:"CW" or "USB" or "LSB" or "AM" or "NFM"} ch)
                FeedAdditionalAudio(pcm,1,RadioContext("audio-modes",ch.Frequency,
                    receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null),ch.Mode=="USB");
        };
        engine.PcmReady+=(bytes,count,channels,seconds)=>{
            if(!rfMode)FeedAdditionalAudio(bytes.AsSpan(0,count),channels,
                RadioContext("audio-modes",null,packetAudioReplay?seconds:null),true);
        };
        receiver.ChannelChanged+=()=>ResetAdditionalReceive(true);
        receiver.ReplayGap+=()=>ResetAdditionalReceive(true);
        receiver.Ended+=_=>ResetAdditionalReceive(true);
        engine.PcmBoundary+=()=>ResetAdditionalReceive(true);
        engine.Ended+=_=>ResetAdditionalReceive(true);
        Closed+=(_,_)=>ResetAdditionalReceive(true);
    }

    private async void TuneRail(long hz)
    {
        try
        {
            if(!rfMode || receiver.IsReplay)throw new IOException("Start live SDR reception before tuning.");
            if(!receiver.CanTune(hz))throw new IOException("Frequency is outside the active receiver range.");
            if(trunkRadio.Running||dmrTrunkRadio.Running||lcnSurvey.Running||surveyTuneBusy)StopAllTrunk();
            StopFolderScan("Folder scan stopped for railroad tuning.");
            StopActivity("Scanner stopped for railroad tuning.");
            var channel=new ChannelSettings(hz,"NFM",12500);
            if((!receiver.IsHackRf||!receiver.Offset.Enabled)&&channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))
                receiver.SetChannel(channel);
            else await receiver.Retune(hz,channel);
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Railroad tuning");}
    }

    internal static string? RailProfile(long? hz) => hz switch {452937500=>"HOT",457937500=>"EOT",_=>null};

    private void NewRailDecoder()
    {
        railReceiver=new RailDecoder();
        railReceiver.Frame+=frame=>{
            if(railContext is not {} context || RailProfile(context.FrequencyHz) is not string mode)return;
            if(railPending.Count>=128)railPending.Dequeue();
            bool hot=mode=="HOT";
            railPending.Enqueue(new PacketObservation(DateTimeOffset.UtcNow,context.Source,context.Replay,context.FrequencyHz,
                context.PositionSeconds,"BCH accepted",hot?"":frame.Field(7,17).ToString(CultureInfo.InvariantCulture),
                "","",mode,frame.Summary(hot),null,frame.Raw,"Railroad "+mode,true,SourceKey:context.Key));
        };
    }

    private void ResetAdditionalReceive(bool audio)
    {
        lock(receiveModeSync)
        {
            railContext=null;
            if(!audio)return;
            conversationCw?.FlushCharacter();
            conversationCw=null;weatherFax=null;sstvDecoder=null;rttyReceiver=null;audioModeSource=null;sstvAudioContext=null;
        }
    }

    private void FeedAdditionalAudio(ReadOnlySpan<byte> pcm,int channels,PacketContext context,bool faxCompatible)
    {
        lock(receiveModeSync)
        {
            if(conversationCw==null&&weatherFax==null&&sstvDecoder==null&&rttyReceiver==null)return;
            if(audioModeSource!=null&&audioModeSource!=context.Key)
            {
                conversationCw?.FlushCharacter();
                conversationCw=null;weatherFax=null;sstvDecoder=null;rttyReceiver=null;audioModeSource=null;sstvAudioContext=null;
                return;
            }
            audioModeSource=context.Key;
            sstvAudioContext=context;
            int stride=2*channels;
            for(int i=0;i+stride<=pcm.Length;i+=stride)
            {
                double value=0;
                for(int ch=0;ch<channels;ch++)
                    value+=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm[(i+2*ch)..]);
                value/=32768*channels;
                conversationCw?.Feed(value);
                sstvDecoder?.Feed(value);
                rttyReceiver?.Feed(value);
                if(faxCompatible&&faxLines.Count<2400)weatherFax?.Feed(value);
            }
        }
    }

    private void DrawFax(bool force)
    {
        byte[][] lines;
        lock(receiveModeSync)
        {
            if(!force&&faxLines.Count==displayedFaxRows)return;
            lines=faxLines.ToArray();
        }
        if(lines.Length==0)return;
        int width=lines[0].Length;
        var image=new byte[width*lines.Length];
        int shift=(faxShift%width+width)%width;
        for(int n=0;n<lines.Length;n++)
        {
            lines[n].AsSpan(shift).CopyTo(image.AsSpan(width*n));
            lines[n].AsSpan(0,shift).CopyTo(image.AsSpan(width*(n+1)-shift));
        }
        faxImage.Source=BitmapSource.Create(width,lines.Length,96,96,PixelFormats.Gray8,null,image,width);
        displayedFaxRows=lines.Length;
    }

    private void TickAdditionalReceive()
    {
        TickWeather();
        TickSstv();
        TickRtty();
        PacketObservation[] packets;
        string text;
        bool cwRunning, faxRunning;
        lock(receiveModeSync)
        {
            packets=railPending.ToArray();railPending.Clear();
            text=cwPending.ToString();cwPending.Clear();
            cwRunning=conversationCw!=null;faxRunning=weatherFax!=null;
        }
        foreach(var packet in packets)railMessages.Add(packet);
        if(text.Length>0)
        {
            cwTranscript.AppendText(text);
            if(cwTranscript.Text.Length>30000)cwTranscript.Text=cwTranscript.Text[^24000..];
            cwTranscript.ScrollToEnd();
        }
        if(!cwRunning&&cwStatus.Text.StartsWith("Decoding",StringComparison.Ordinal))cwStatus.Text="Stopped · source changed";
        DrawFax(false);
        if(faxRunning)faxStatus.Text=displayedFaxRows>=2400?"Image limit reached · save or start a new image":$"Receiving · {displayedFaxRows} lines";
        else if(faxStatus.Text.StartsWith("Receiving",StringComparison.Ordinal))faxStatus.Text=$"Stopped · {displayedFaxRows} lines retained";
        if(!surveyActive||Environment.TickCount64<nextSurveyAt)return;
        nextSurveyAt=Environment.TickCount64+500;
        ShowCellularActivity();
    }

    private void ShowCellularActivity()
    {
        (float[] Bins,long Center,int Rate,double Usable,long Stamp)? frame;
        lock(frameSync)frame=surveyFrame;
        if(!rfMode||receiver.IsReplay||!receiver.IsHackRf||frame is not {} snapshot||
            Environment.TickCount64-snapshot.Stamp>1500||snapshot.Center!=receiver.AppliedFrequency||
            snapshot.Rate!=receiver.AppliedRate||snapshot.Usable!=receiver.AppliedBandwidth)
        {
            cellularSurveyStatus.Text="Waiting for a fresh live HackRF spectrum";
            surveyResults.Clear();
            return;
        }
        if(snapshot.Stamp==lastSurveyFrame || surveyBand.SelectedItem is not CellularBand reference)return;
        lastSurveyFrame=snapshot.Stamp;
        var result=CellularActivity.Measure(snapshot.Bins,snapshot.Center,snapshot.Rate,snapshot.Usable,reference,activeSurveyMargin);
        if(result==null)
        {
            cellularSurveyStatus.Text="The selected band is outside the usable capture. Tune into the band with Receiver or Spectrum.";
            surveyResults.Clear();
            return;
        }
        cellularSurveyStatus.Text=FormattableString.Invariant(
            $"Captured {result.LowHz/1e6:F3}–{result.HighHz/1e6:F3} MHz · estimated floor {result.Floor:F1} dBFS/bin · bins ±2 around capture center excluded");
        surveyResults.Text=result.Activity.Length==0?$"No bins in this snapshot exceeded the estimated floor by {activeSurveyMargin} dB.":
            "Observed RF energy · approximate ranges, protocol unidentified"+Environment.NewLine+
            string.Join(Environment.NewLine,result.Activity.Select(v=>FormattableString.Invariant(
                $"{v.LowHz/1e6:F4}–{v.HighHz/1e6:F4} MHz · peak {v.Peak:F1} dBFS/bin")));
    }
}
