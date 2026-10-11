using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace MRS;
public sealed class TvPanel : UserControl
{
    private readonly Button setup=new(){Content="Set up TV decoder"},live=new(){Content="Decode current receiver"},file=new(){Content="Decode I/Q recording..."},stop=new(){Content="Stop TV",IsEnabled=false};
    private readonly Button watch=new(){Content="Watch live",IsEnabled=false},play=new(){Content="Play saved TV",IsEnabled=false},programs=new(){Content="Read programs",IsEnabled=false},tune=new(){Content="Tune RF channel"};
    private readonly ComboBox channel=new(){Width=200},program=new(){Width=260};
    private readonly TextBlock state=new(){Text="ATSC 1.0 decoder",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,8,0,8)};
    private readonly TextBlock destination=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue};
    private readonly TextBox log=new(){IsReadOnly=true,AcceptsReturn=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,TextWrapping=TextWrapping.Wrap,Background=new SolidColorBrush(Color.FromRgb(16,23,34)),Foreground=Brushes.LightSteelBlue};
    private CancellationTokenSource? cancellation;
    private TvLiveInput? input;
    private bool liveActive,released;
    private string? output,working;
    private int udpPort;
    private long goodPackets;
    private Process? player;
    private sealed record RfChannel(int Number,long Center){public override string ToString()=>$"RF {Number} · {Center/1000000.0:0.###} MHz";}
    private sealed record TvProgram(int Id,string Name){public override string ToString()=>Name;}
    internal Func<TvCapture?>? CurrentCapture;
    internal Func<long,Task>? Tune;
    internal event Action<bool>? Active;
    public TvPanel()
    {
        var root=new DockPanel{Margin=new Thickness(12)};var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        top.Children.Add(new TextBlock{Text="HDTV · ATSC 1.0 · EXPERIMENTAL",Foreground=Brushes.White,FontWeight=FontWeights.Bold});
        top.Children.Add(new TextBlock{Text="Start the HackRF at 9.6, 19.2 or 20 MS/s. Tune the physical RF channel (not the on-screen virtual channel number). TV uses the full 6 MHz channel directly; the ordinary audio-mode filter does not apply. Capture offset is supported when the entire channel fits.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,8,0,8)});
        var tuning=new WrapPanel();channel.ItemsSource=Enumerable.Range(2,35).Select(n=>new RfChannel(n,TvCapture.ChannelCenter(n))).ToArray();channel.SelectedIndex=12;tuning.Children.Add(channel);tuning.Children.Add(tune);top.Children.Add(tuning);
        var actions=new WrapPanel{Margin=new Thickness(0,8,0,0)};foreach(var button in new[]{setup,live,file,stop})actions.Children.Add(button);top.Children.Add(actions);
        top.Children.Add(new TextBlock{Text="One-time decoder setup needs internet and about 1.3 GB of runtime storage, plus download cache. ATSC 3.0 is not supported. Playback opens a separate TV window.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,FontSize=11,Margin=new Thickness(0,6,0,0)});
        var playback=new WrapPanel{Margin=new Thickness(0,8,0,0)};program.Items.Add(new TvProgram(0,"First available program"));program.SelectedIndex=0;playback.Children.Add(program);foreach(var button in new[]{programs,watch,play})playback.Children.Add(button);top.Children.Add(playback);
        top.Children.Add(state);top.Children.Add(destination);root.Children.Add(log);Content=root;
        state.Text=TvRuntime.Available?"Decoder runtime found. Ready to attempt ATSC 1.0 reception.":"Use Set up TV decoder before the first attempt.";
        setup.Click+=async(_,_)=>await Setup();live.Click+=async(_,_)=>await Decode(true);file.Click+=async(_,_)=>await Decode(false);
        stop.Click+=(_,_)=>cancellation?.Cancel();watch.Click+=(_,_)=>Play(true);play.Click+=(_,_)=>Play(false);programs.Click+=async(_,_)=>await ReadPrograms();
        tune.Click+=async(_,_)=>{if(channel.SelectedItem is RfChannel selected && Tune!=null)try{await Tune(selected.Center);}catch(Exception ex){state.Text=ex.Message;}};
    }
    internal void Feed(IqFrame frame)=>Volatile.Read(ref input)?.Offer(frame);
    internal void StopLive(string reason)=>Dispatcher.BeginInvoke(new Action(()=>{if(liveActive){state.Text=reason;cancellation?.Cancel();}}));
    internal void Release(){released=true;cancellation?.Cancel();ClosePlayer();}
    private void ClosePlayer(){try{if(player is {HasExited:false})player.Kill();}catch(InvalidOperationException){}player?.Dispose();player=null;}
    private void Busy(bool busy){setup.IsEnabled=live.IsEnabled=file.IsEnabled=tune.IsEnabled=channel.IsEnabled=!busy;stop.IsEnabled=busy;}
    private void Report(string text)
    {
        string? status=null;
        try
        {
            using var json=JsonDocument.Parse(text);var root=json.RootElement;
            if(root.TryGetProperty("kind",out var kind)&&kind.GetString()=="status")
            {
                long packets=root.GetProperty("packets").GetInt64(),bad=root.GetProperty("bad_packets").GetInt64();Interlocked.Exchange(ref goodPackets,Math.Max(0,packets-bad));
                status=$"ATSC packets accepted: {Interlocked.Read(ref goodPackets):N0} · rejected: {bad:N0} · corrected symbols: {root.GetProperty("corrected").GetInt64():N0}";
            }
        }
        catch(JsonException){}
        Dispatcher.BeginInvoke(new Action(()=>{
            if(released||cancellation==null)return;
            if(status!=null){state.Text=status;watch.IsEnabled=liveActive&&goodPackets>0;programs.IsEnabled=goodPackets>0;return;}
            log.AppendText(text+Environment.NewLine);if(log.Text.Length>60000)log.Text=log.Text[^45000..];log.ScrollToEnd();
        }));
    }
    private async Task Setup()
    {
        if(cancellation!=null)return;cancellation=new();Busy(true);
        try{await TvRuntime.Install(cancellation.Token,text=>Dispatcher.BeginInvoke(new Action(()=>{if(!released)state.Text=text;})));state.Text="TV decoder ready.";}
        catch(OperationCanceledException){state.Text="TV setup cancelled. Run setup again to finish.";}
        catch(Exception ex){state.Text="TV setup failed: "+ex.Message;}
        finally{cancellation.Dispose();cancellation=null;Busy(false);}
    }
    private async Task Decode(bool fromReceiver)
    {
        if(cancellation!=null)return;
        string? recording=null;TvCapture capture;
        try
        {
            if(!TvRuntime.Available)throw new IOException("Use Set up TV decoder first.");
            if(fromReceiver)capture=CurrentCapture?.Invoke()??throw new IOException("Start a live SDR receiver or I/Q replay first.");
            else{var open=new OpenFileDialog{Filter="M.R.S. I/Q recording|*.mrsiq"};if(open.ShowDialog()!=true)return;recording=open.FileName;capture=TvDecoder.RecordingCapture(recording);}
            capture.Validate();
        }
        catch(Exception ex){state.Text=ex.Message;return;}
        var save=new SaveFileDialog{Filter="TV transport stream|*.ts",FileName=$"MRS-TV-{capture.Station/1000000.0:0.000}-{DateTime.Now:yyyyMMdd-HHmmss}.ts"};if(save.ShowDialog()!=true)return;
        if(recording!=null&&string.Equals(Path.GetFullPath(recording),Path.GetFullPath(save.FileName),StringComparison.OrdinalIgnoreCase)){state.Text="Choose a different output file.";return;}
        ClosePlayer();output=null;working=save.FileName+"."+Guid.NewGuid()+".partial.ts";goodPackets=0;play.IsEnabled=programs.IsEnabled=watch.IsEnabled=false;
        program.Items.Clear();program.Items.Add(new TvProgram(0,"First available program"));program.SelectedIndex=0;
        using(var socket=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))udpPort=((IPEndPoint)socket.Client.LocalEndPoint!).Port;
        cancellation=new();var token=cancellation.Token;liveActive=fromReceiver;Busy(true);log.Clear();destination.Text="Output: "+save.FileName;
        state.Text=$"Acquiring ATSC 1.0 at {capture.Station/1000000.0:F6} MHz · capture {capture.Center/1000000.0:F6} MHz...";
        if(fromReceiver)Active?.Invoke(true);
        string outcome="Decode complete";
        try
        {
            async Task Feed(Stream stream,CancellationToken ct)
            {
                if(!fromReceiver){await TvDecoder.FeedRecording(recording!,capture,stream,ct);return;}
                var queue=new TvLiveInput(capture);Volatile.Write(ref input,queue);
                try{await queue.Feed(stream,ct);}finally{Volatile.Write(ref input,null);}
            }
            await Task.Run(()=>TvDecoder.Run(capture,working,Feed,Report,token,fromReceiver?udpPort:0),token);
        }
        catch(Exception)when(token.IsCancellationRequested){outcome="TV decoding stopped";}
        catch(Exception ex){outcome="TV stopped: "+ex.Message;}
        finally
        {
            Volatile.Write(ref input,null);liveActive=false;watch.IsEnabled=false;ClosePlayer();if(fromReceiver)Active?.Invoke(false);
            cancellation.Dispose();cancellation=null;Busy(false);
            if(File.Exists(working))
            {
                if(new FileInfo(working).Length>=188 && goodPackets>0)
                {try{File.Move(working,save.FileName,true);output=save.FileName;play.IsEnabled=programs.IsEnabled=true;}catch(Exception ex){outcome+="; output remains at "+working+": "+ex.Message;}}
                else File.Delete(working);
            }
            working=null;state.Text=outcome+(output!=null?$" · saved {goodPackets:N0} accepted packets to TV file.":" · no accepted TV packets recovered.");
        }
    }
    private void Play(bool fromReceiver)
    {
        string? path=fromReceiver?$"udp://127.0.0.1:{udpPort}?fifo_size=1000000&overrun_nonfatal=1":output;if(path==null)return;
        try
        {
            ClosePlayer();var info=TvRuntime.Command(Path.Combine(TvRuntime.Runtime,"Library","bin","ffplay.exe"),"-hide_banner","-loglevel","error","-window_title","M.R.S. ATSC 1.0 TV","-autoexit","-i",path);
            info.RedirectStandardOutput=info.RedirectStandardError=false;
            if(program.SelectedItem is TvProgram {Id:>0} selected){info.ArgumentList.Add("-vst");info.ArgumentList.Add("p:"+selected.Id);info.ArgumentList.Add("-ast");info.ArgumentList.Add("p:"+selected.Id);}
            player=Process.Start(info);
        }
        catch(Exception ex){state.Text="Playback: "+ex.Message;}
    }
    private async Task ReadPrograms()
    {
        string? path=working??output;if(path==null||!File.Exists(path))return;
        programs.IsEnabled=false;
        try
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            string text=await TvRuntime.Run(TvRuntime.Command(Path.Combine(TvRuntime.Runtime,"Library","bin","ffprobe.exe"),"-v","quiet","-probesize","5000000","-analyzeduration","2000000","-show_programs","-of","json",path),timeout.Token);
            using var json=JsonDocument.Parse(text);program.Items.Clear();program.Items.Add(new TvProgram(0,"First available program"));
            foreach(var item in json.RootElement.GetProperty("programs").EnumerateArray())
            {int id=item.GetProperty("program_id").GetInt32();string name=$"Program {id}";if(item.TryGetProperty("tags",out var tags)&&tags.TryGetProperty("service_name",out var label))name+=" · "+label.GetString();program.Items.Add(new TvProgram(id,name));}
            program.SelectedIndex=0;
        }
        catch(Exception ex){state.Text="Program list: "+ex.Message;}
        finally{programs.IsEnabled=output!=null||liveActive&&goodPackets>0;}
    }
}
