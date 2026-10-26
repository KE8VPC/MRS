namespace MRS;
public partial class MainWindow
{
    private FrequencyItem[] folderChannels=[];
    private int folderIndex=-1,folderEpoch;
    private bool folderBusy,folderWaiting;
    private string folderName="";
    private double folderRequested,folderApplied,folderFrame=-1;
    private (double Dwell,double Quiet,double Maximum) folderTimes;
    private readonly FolderScanTiming folderTiming=new();
    private bool FolderRunning=>folderChannels.Length>0;
    private void InitializeFolderScan()
    {
        FrequencyManager.FolderScanRequested+=StartFolderScan;
        FrequencyManager.ScanControls.StopRequested+=()=>StopFolderScan("Folder scan stopped.");
        FrequencyManager.ScanControls.SkipRequested+=()=>{if(FolderRunning&&!folderBusy){FrequencyManager.ScanControls.Hold.IsChecked=false;NextFolderChannel();}};
    }
    private void StopFolderScan(string reason)
    {
        bool running=FolderRunning;folderEpoch++;folderChannels=[];folderWaiting=false;
        FrequencyManager.ScanControls.Display(reason,false);if(running)Log(reason);
    }
    private void StartFolderScan(FrequencyItem folder,FrequencyItem[] entries)
    {
        StopTrunk(false);
        if(folderBusy){Log("Wait for the current tuning command to finish.");return;}
        try
        {
            if(!ActivityLive)throw new InvalidOperationException("Start a live SDR receiver before scanning a folder. Replay is not supported.");
            folderTimes=FrequencyManager.ScanControls.Settings();
            var valid=new List<FrequencyItem>();int rejected=0;
            foreach(var item in entries)
            {
                try
                {
                    var channel=new ChannelSettings(item.FrequencyHz,item.Mode,item.BandwidthHz);channel.Validate();
                    if(!receiver.CanTune(item.FrequencyHz)||channel.Width>receiver.AppliedBandwidth)throw new InvalidOperationException();
                    if(receiver.IsHackRf)receiver.Offset.Center(channel,receiver.AppliedRate,receiver.RequestedGain);
                    valid.Add(item);
                }
                catch{rejected++;}
            }
            if(valid.Count==0)throw new InvalidOperationException("Folder has no supported channels for this receiver/rate/filter. Check hidden-entry and subfolder options.");
            StopActivity("Activity discovery stopped for saved-folder scan.");StopFolderScan("Starting folder scan.");
            folderChannels=valid.ToArray();folderIndex=-1;folderName=folder.Name;
            RfSquelchEnabled.IsChecked=true;
            Log($"Folder scan: {folderName}; {valid.Count} channels, {rejected} unsupported entries skipped. Snapshot uses saved order/mode/filter; protocol labels do not enable decoders.");
            NextFolderChannel();
        }
        catch(Exception ex){FrequencyManager.ScanControls.Display(ex.Message,FolderRunning);Log(ex.Message);}
    }
    private async void NextFolderChannel()
    {
        if(!FolderRunning||folderBusy)return;
        int epoch=folderEpoch;folderBusy=true;folderWaiting=true;
        folderIndex=(folderIndex+1)%folderChannels.Length;var item=folderChannels[folderIndex];
        folderRequested=DisplaySettings.Clock;folderApplied=-1;folderFrame=-1;
        ShowFolderScan("Tuning");
        try
        {
            var channel=new ChannelSettings(item.FrequencyHz,item.Mode,item.BandwidthHz,ChannelAgc.IsChecked==true);
            if(receiver.IsHackRf&&receiver.Offset.Enabled || !channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))await receiver.Retune(item.FrequencyHz,channel);
            else receiver.SetChannel(channel);
        }
        catch(Exception ex){if(epoch==folderEpoch)StopFolderScan("Folder scan stopped: "+ex.Message);}
        finally{folderBusy=false;}
    }
    private void ShowFolderScan(string state)
    {
        if(!FolderRunning)return;var item=folderChannels[folderIndex];
        FrequencyManager.ScanControls.Display($"{folderName} · {folderIndex+1}/{folderChannels.Length} · {state}\n{item.Name} · {item.FrequencyHz/1e6:0.######} MHz · {item.Mode} · {new ChannelSettings(item.FrequencyHz,item.Mode,item.BandwidthHz).Width/1000:0.##} kHz",true);
    }
    private void RefreshFolderScan(double? freshFrame)
    {
        if(!FolderRunning)return;
        if(!ActivityLive){StopFolderScan("Folder scan stopped: receiver unavailable.");return;}
        double now=DisplaySettings.Clock;
        if(folderBusy)return;
        var item=folderChannels[folderIndex];var actual=receiver.AppliedChannel;
        var expected=new ChannelSettings(item.FrequencyHz,item.Mode,item.BandwidthHz);
        bool applied=actual!=null&&actual.Frequency==item.FrequencyHz&&actual.Mode==item.Mode&&actual.Width==expected.Width;
        if(!applied){if(now-folderRequested>5)StopFolderScan("Folder scan stopped: channel tuning timed out.");return;}
        if(folderApplied<0){folderApplied=now;folderTiming.Reset(now);}
        if(freshFrame is {} frame&&frame>=folderApplied)folderFrame=frame;
        if(folderFrame<folderApplied || now-folderFrame>2){if(now-folderApplied>3)StopFolderScan("Folder scan stopped: no fresh RF data.");return;}
        // Ignore previous-channel DSP/gate state, and allow automatic floor calibration when enabled.
        double settle=activityOptions.AutoSquelch?1.5:.35;
        if(now-folderApplied<settle || activityOptions.AutoSquelch&&!activity.Ready){if(now-folderApplied>5)StopFolderScan("Folder scan stopped: noise floor not ready.");return;}
        if(folderWaiting){folderWaiting=false;folderTiming.Reset(now);}
        bool signal=receiver.ChannelDb>=receiver.SquelchThreshold;
        bool held=FrequencyManager.ScanControls.Hold.IsChecked==true;
        ShowFolderScan(held?"HOLD":signal?"Activity":"Listening");
        if(freshFrame!=null&&folderTiming.Advance(now,signal,held,folderTimes.Dwell,folderTimes.Quiet,folderTimes.Maximum))NextFolderChannel();
    }
}
