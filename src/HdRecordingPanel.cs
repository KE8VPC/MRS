using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
namespace MRS;

public sealed class HdRecordingPanel : UserControl
{
    private readonly TextBox decoder=new(){IsReadOnly=true,MinWidth=300};
    private readonly TextBox log=new(){IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=new SolidColorBrush(Color.FromRgb(16,23,34)),Foreground=Brushes.LightSteelBlue};
    private readonly TextBlock status=new(){Text="HD recording preview · decoder not configured",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};
    private readonly ComboBox program=new(){ItemsSource=new[]{"HD1","HD2","HD3","HD4","HD5","HD6","HD7","HD8"},SelectedIndex=0,Width=90};
    private readonly Button run=new(){Content="Decode I/Q recording…"},stop=new(){Content="Cancel",IsEnabled=false},play=new(){Content="Open decoded WAV",IsEnabled=false};
    private CancellationTokenSource? cancellation;
    private string? result;
    private bool released;
    private readonly Button listen=new(){Content="Listen to I/Q recording…"};
    private readonly Button live=new(){Content="Listen to current receiver"};
    private HdLiveInput? liveFrames;
    private bool liveActive;
    private bool metadataReplay=true;
    internal Func<(long Frequency,bool Replay)?>? LiveSource;
    internal event Action<bool>? Listening;
    private readonly Slider volume=new(){Minimum=0,Maximum=1,Value=.35,Width=120,ToolTip="HD listening volume"};
    private readonly TextBlock station=new(){Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
    private HdMetadata metadata=new();
    private NAudio.Wave.WaveOutEvent? audioOutput;
    private NAudio.Wave.BufferedWaveProvider? playingBuffer;
    private string metadataSource="HD recording";
    private long? metadataFrequency;
    internal event Action<PacketObservation>? Observation;
    public HdRecordingPanel()
    {
        var root=new DockPanel{Margin=new Thickness(12)};var controls=new StackPanel();DockPanel.SetDock(controls,Dock.Top);root.Children.Add(controls);
        controls.Children.Add(new TextBlock{Text="HD RADIO · EXPERIMENTAL",FontWeight=FontWeights.Bold,Foreground=Brushes.White});
        controls.Children.Add(new TextBlock{Text="Decode a centered FM station from a continuous M.R.S. I/Q recording (960 kS/s or higher). Listen to a recording or the current centered WFM receiver. HD uses the Windows default audio output. Stop before changing HD programs.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
        var pathRow=new DockPanel();var browse=new Button{Content="Select nrsc5.exe…"};DockPanel.SetDock(browse,Dock.Right);pathRow.Children.Add(browse);pathRow.Children.Add(decoder);controls.Children.Add(pathRow);
        var actions=new WrapPanel{Margin=new Thickness(0,8,0,8)};actions.Children.Add(program);actions.Children.Add(run);actions.Children.Add(stop);actions.Children.Add(play);controls.Children.Add(actions);controls.Children.Add(status);root.Children.Add(log);Content=root;
        actions.Children.Insert(0,listen);actions.Children.Add(new TextBlock{Text="HD volume",Foreground=Brushes.LightSteelBlue,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,4,0)});actions.Children.Add(volume);controls.Children.Add(station);
        actions.Children.Insert(1,live);
        listen.Click+=async(_,_)=>await Listen();live.Click+=async(_,_)=>await Listen(true);volume.ValueChanged+=(_,_)=>{if(audioOutput!=null)audioOutput.Volume=(float)volume.Value;};
        string bundled=Path.Combine(AppContext.BaseDirectory,"Decoders","nrsc5","nrsc5.exe");if(File.Exists(bundled)){decoder.Text=bundled;status.Text="Ready for HD recording analysis";}
        browse.Click+=(_,_)=>{var dialog=new OpenFileDialog{Filter="NRSC-5 decoder (nrsc5.exe)|nrsc5.exe"};if(dialog.ShowDialog()==true)decoder.Text=dialog.FileName;};
        run.Click+=async(_,_)=>await Decode();stop.Click+=(_,_)=>cancellation?.Cancel();
        play.Click+=(_,_)=>{if(result!=null)try{Process.Start(new ProcessStartInfo(result){UseShellExecute=true});}catch(Exception ex){status.Text=ex.Message;}};
    }
    internal void Release(){released=true;cancellation?.Cancel();audioOutput?.Stop();}
    internal void FeedLive(IqFrame frame)
    {
        var queue=Volatile.Read(ref liveFrames);if(queue==null)return;
        queue.Offer(frame);
    }
    internal void StopLive(string reason)
    {
        Dispatcher.BeginInvoke(new Action(()=>{if(!liveActive)return;cancellation?.Cancel();metadata=new();station.Text=metadata.Summary;Report(reason);}));
    }
    private void ResetMetadata(string path)
    {
        metadata=new();metadataReplay=true;station.Text=metadata.Summary;metadataSource="HD recording · "+Path.GetFileName(path);
        using var reader=new IqReader(path);metadataFrequency=reader.Read()?.Frequency;
    }
    private void Report(string text)=>Dispatcher.BeginInvoke(new Action(()=>
    {
        if(released)return;log.AppendText(text+Environment.NewLine);if(log.Text.Length>120000)log.Text=log.Text[^90000..];log.ScrollToEnd();
        if(cancellation?.IsCancellationRequested==false && metadata.Accept(text))
        {
            if(!metadata.Locked)playingBuffer?.ClearBuffer();
            station.Text=metadata.Summary;
            Observation?.Invoke(new PacketObservation(DateTimeOffset.UtcNow,metadataSource,metadataReplay,metadataFrequency,null,
                metadata.Locked?"Decoder synchronized":"Decoder status",metadata.Station,"","","HD metadata",metadata.Summary,null,"","NRSC-5",metadata.Locked));
        }
    }));
    private async Task Listen(bool fromReceiver=false)
    {
        if(cancellation!=null)return;
        if(!File.Exists(decoder.Text)){status.Text="Select the Windows nrsc5 decoder first.";return;}
        var source=fromReceiver?LiveSource?.Invoke():null;
        if(fromReceiver && source==null){status.Text="Start a centered WFM receiver at 960 kS/s or higher in tuner mode first.";return;}
        var input=new OpenFileDialog{Filter="M.R.S. I/Q recording|*.mrsiq"};if(!fromReceiver && input.ShowDialog()!=true)return;
        cancellation=new();var token=cancellation.Token;string executable=decoder.Text;int selected=program.SelectedIndex;
        live.IsEnabled=listen.IsEnabled=run.IsEnabled=program.IsEnabled=play.IsEnabled=false;stop.IsEnabled=true;stop.Content="Stop HD";log.Clear();liveActive=fromReceiver;
        try
        {
            if(fromReceiver){metadata=new();metadataReplay=source!.Value.Replay;metadataFrequency=source.Value.Frequency;metadataSource="HD current receiver";station.Text=metadata.Summary;}
            else ResetMetadata(input.FileName);
            status.Text=$"HD{selected+1} · {(fromReceiver?"current receiver":"recording playback")} · Windows default audio output · acquiring digital signal…";
            var buffer=new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(44100,16,2)){BufferDuration=TimeSpan.FromSeconds(10),DiscardOnBufferOverflow=false};
            playingBuffer=buffer;
            audioOutput=new NAudio.Wave.WaveOutEvent{DesiredLatency=200,Volume=(float)volume.Value};audioOutput.Init(buffer);Listening?.Invoke(true);audioOutput.Play();long bytes=0;
            var queue=new HdLiveInput(source?.Frequency??0);
            if(fromReceiver)Volatile.Write(ref liveFrames,queue);
            async Task Feed(Stream stream,CancellationToken ct)
            {
                if(!fromReceiver){await HdStreamDecoder.FeedRecording(input.FileName,stream,ct);return;}
                await queue.Feed(stream,ct);
            }
            await Task.Run(()=>HdStreamDecoder.Run(executable,selected,Feed,
                (pcm,count)=>{buffer.AddSamples(pcm,0,count);Interlocked.Add(ref bytes,count);},Report,token),token);
            while(buffer.BufferedBytes>0)await Task.Delay(50,token);
            status.Text=bytes>0?"HD recording playback complete.":"No HD audio recovered for this service.";
        }
        catch(Exception ex)when(token.IsCancellationRequested){status.Text="HD playback stopped.";_ = ex;}
        catch(Exception ex){status.Text="HD: "+ex.Message;Report(ex.Message);}
        finally
        {
            Volatile.Write(ref liveFrames,null);liveActive=false;audioOutput?.Stop();audioOutput?.Dispose();audioOutput=null;Listening?.Invoke(false);cancellation.Dispose();cancellation=null;
            playingBuffer=null;
            if(fromReceiver || token.IsCancellationRequested){metadata=new();station.Text=metadata.Summary;}
            else station.Text=metadata.Summary.Replace("Synchronized","Last decoded (playback ended)");
            live.IsEnabled=listen.IsEnabled=run.IsEnabled=program.IsEnabled=true;stop.IsEnabled=false;stop.Content="Cancel";
            play.IsEnabled=result!=null && File.Exists(result);
        }
    }
    private async Task Decode()
    {
        if(cancellation!=null)return;
        if(!File.Exists(decoder.Text)){status.Text="Select the Windows nrsc5 decoder first.";return;}
        var input=new OpenFileDialog{Filter="M.R.S. I/Q recording|*.mrsiq"};if(input.ShowDialog()!=true)return;
        var output=new SaveFileDialog{Filter="Decoded HD audio|*.wav",FileName=Path.GetFileNameWithoutExtension(input.FileName)+"-"+program.SelectedItem+".wav"};if(output.ShowDialog()!=true)return;
        if(string.Equals(Path.GetFullPath(input.FileName),Path.GetFullPath(output.FileName),StringComparison.OrdinalIgnoreCase)){status.Text="Choose a different output file to preserve the original recording.";return;}
        string executable=decoder.Text;int selected=program.SelectedIndex;
        string raw=Path.Combine(Path.GetTempPath(),"mrs-hd-"+Guid.NewGuid()+".cu8");
        string partial=output.FileName+"."+Guid.NewGuid()+".partial.wav";
        cancellation=new();var token=cancellation.Token;live.IsEnabled=listen.IsEnabled=run.IsEnabled=false;stop.IsEnabled=true;play.IsEnabled=false;program.IsEnabled=false;log.Clear();result=null;status.Text="Preparing HD samples…";
        try
        {
            ResetMetadata(input.FileName);
            await Task.Run(()=>HdIqConverter.Convert(input.FileName,raw,token,Report),token);
            status.Text="Decoding HD…";
            var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true,WorkingDirectory=Path.GetDirectoryName(executable)!};
            foreach(string arg in new[]{"-r",raw,"-o",partial,"-l","2",selected.ToString()})info.ArgumentList.Add(arg);
            using var process=new Process{StartInfo=info};
            process.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)Report(e.Data);};process.OutputDataReceived+=(_,e)=>{if(e.Data!=null)Report(e.Data);};
            process.Start();process.BeginErrorReadLine();process.BeginOutputReadLine();
            using var registration=token.Register(()=>{try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}});
            await process.WaitForExitAsync();token.ThrowIfCancellationRequested();
            if(process.ExitCode!=0)throw new IOException($"HD decoder exited with code {process.ExitCode}; see details below.");
            if(!File.Exists(partial))throw new IOException("No audio file was produced. The selected service may not have decoded.");
            using(var wav=new NAudio.Wave.WaveFileReader(partial))
                if(wav.Length==0)throw new IOException("No HD audio recovered. Try a longer recording or another service; absence of decoded audio does not prove the station has no HD signal.");
            File.Move(partial,output.FileName,true);result=output.FileName;play.IsEnabled=true;status.Text="Decoded audio saved. Open WAV to listen.";
        }
        catch(OperationCanceledException){status.Text="HD decoding canceled.";}
        catch(Exception ex){status.Text="HD: "+ex.Message;Report(ex.Message);}
        finally
        {
            foreach(string ownFile in new[]{raw,partial})try{File.Delete(ownFile);}catch(IOException){}catch(UnauthorizedAccessException){}
            cancellation.Dispose();cancellation=null;live.IsEnabled=listen.IsEnabled=run.IsEnabled=true;stop.IsEnabled=false;program.IsEnabled=true;
        }
    }
}
