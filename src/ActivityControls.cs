using System.IO;
using System.Windows;
namespace MRS;
public partial class MainWindow
{
    private readonly ActivityScanner activity=new();
    private (long Center,int Rate,double Usable,int Gain,int Bins,string Mode,double Width)? activityCapture;
    private ActivityOptions activityOptions=new();
    private double activityProcessed=-1,activitySaved=-1;
    private bool applyingActivity;
    private float? manualActivityThreshold;
    private string activityMessage="Scanner stopped.",activitySaveError="";
    private readonly Dictionary<Guid,ActivityDiscovery> activityPending=[];
    private bool ActivityLive=>rfMode&&!receiver.IsReplay&&receiver.AppliedChannel!=null&&receiver.AppliedFrequency>0;
    private void InitializeActivity()
    {
        ActivityControls.Initialize(Path.Combine(dataFolder,"activity.json"));activityOptions=ActivityControls.Options;
        ActivityControls.StartRequested+=StartActivity;
        ActivityControls.StopRequested+=()=>StopActivity("Scanner stopped.");
        ActivityControls.HoldChanged+=held=>activity.Held=held;
        ActivityControls.SkipRequested+=()=>{FlushActivity();activity.Skip();};
        ActivityControls.ClearIgnoredRequested+=activity.ClearIgnored;
        ActivityControls.RecalibrateRequested+=()=>ResetActivity("Recalibrating; press Start to resume scanning.");
        ActivityControls.Changed+=next=>
        {
            if(activityOptions.GridHz!=next.GridHz)StopActivity("Channel grid changed; press Start to scan again.");
            if(!activityOptions.AutoSquelch&&next.AutoSquelch)manualActivityThreshold=(float)RfSquelchSlider.Value;
            if(activityOptions.AutoSquelch&&!next.AutoSquelch&&manualActivityThreshold is {} manual)
            {applyingActivity=true;try{RfSquelchSlider.Value=manual;}finally{applyingActivity=false;}manualActivityThreshold=null;}
            activityOptions=next;
            if(!next.SaveDiscoveries){activity.Saved(activity.Pending());activityPending.Clear();activitySaveError="";}
            if(!next.ShowLines)Spectrum.SetActivityLevels(null,null);
        };
        RfSquelchSlider.ValueChanged+=(_,_)=>
        {
            if(ready&&!applyingActivity&&ActivityControls.Options.AutoSquelch)
            {manualActivityThreshold=(float)RfSquelchSlider.Value;ActivityControls.DisableAuto();}
        };
    }
    private void StartActivity()
    {
        if(!ActivityLive||activityCapture==null||DisplaySettings.Clock-activity.LastFrame>1)
        {activityMessage="Start a live SDR receiver and wait for its RF spectrum.";return;}
        try
        {
            FlushActivity();
            if(activityPending.Count>0)throw new IOException("Resolve the pending discovery save before starting another scan.");
            ActivityControls.EnableAuto();RfSquelchEnabled.IsChecked=true;
            activity.Start(Spectrum.VisibleCenter-Spectrum.VisibleSpan/2,Spectrum.VisibleCenter+Spectrum.VisibleSpan/2);
            Spectrum.InteractionLocked=true;
            RefreshNavigation();
            activityMessage=$"Locked {Math.Max(activity.Low,Spectrum.VisibleCenter-Spectrum.VisibleSpan/2)/1e6:0.######}–{Math.Min(activity.High,Spectrum.VisibleCenter+Spectrum.VisibleSpan/2)/1e6:0.######} MHz";
            Log("Activity scanner started. "+activityMessage+"; listening mode "+receiver.AppliedChannel!.Mode+". Capture stays fixed.");
        }
        catch(Exception ex){activityMessage=ex.Message;}
    }
    private void FlushActivity()
    {
        foreach(var observation in activity.Pending())activityPending[observation.TrackId]=observation;
        var pending=activityPending.Values.ToArray();if(pending.Length==0)return;
        if(!ActivityControls.Options.SaveDiscoveries){activity.Saved(pending);activityPending.Clear();return;}
        try{if(FrequencyManager.SaveDiscoveries(pending,workspaceSlot)){activity.Saved(pending);activityPending.Clear();activitySaveError="";}}
        catch(Exception ex)
        {
            string error="Discovery save failed; pending entries retained in this session: "+ex.Message;
            if(error!=activitySaveError)Log(error);activitySaveError=error;
        }
    }
    private void StopActivity(string reason)
    {
        bool wasRunning=activity.Running;
        FlushActivity();activity.Stop();Spectrum.InteractionLocked=false;activityMessage=reason;
        RefreshNavigation();
        if(wasRunning)Log(reason);
    }
    private void ResetActivity(string reason)
    {
        StopActivity(reason);activityCapture=null;activityProcessed=-1;Spectrum.SetActivityLevels(null,null);
        if(ActivityControls.Options.AutoSquelch&&manualActivityThreshold is {} manual)
        {applyingActivity=true;try{RfSquelchSlider.Value=manual;}finally{applyingActivity=false;}}
    }
    private void ProcessActivity(float[] spectrum,double time)
    {
        if(!ActivityLive||receiver.AppliedChannel is not {} channel)return;
        if(time-activityProcessed<.1)return;
        if(activityCapture!=null&&time-activity.LastFrame>2)ResetActivity("RF data gap; scanner stopped for recalibration.");
        activityProcessed=time;
        var key=(receiver.AppliedFrequency,receiver.AppliedRate,receiver.AppliedBandwidth,receiver.AppliedGain,spectrum.Length,channel.Mode,channel.Width);
        if(activityCapture!=key)
        {
            StopActivity("Receiver or analysis settings changed; calibrating noise floor.");
            activity.Reset(key.AppliedFrequency,key.AppliedRate,key.AppliedBandwidth,key.Length,channel);activityCapture=key;
        }
        if(manualActivityThreshold==null&&ActivityControls.Options.AutoSquelch)manualActivityThreshold=(float)RfSquelchSlider.Value;
        bool wasReady=activity.Ready;
        long? frequency=activity.Process(spectrum,time,DateTimeOffset.UtcNow,channel,ActivityControls.Options,RfSquelchSlider.Value);
        if(activity.Ready&&!wasReady&&!activity.Running)activityMessage="Noise floor ready. Scanner stopped.";
        if(activity.Ready)
        {
            if(ActivityControls.Options.AutoSquelch)
            {applyingActivity=true;try{RfSquelchSlider.Value=activity.ChannelThresholdDb;}finally{applyingActivity=false;}}
            Spectrum.SetActivityLevels(ActivityControls.Options.ShowLines?activity.FloorBinDb:null,ActivityControls.Options.ShowLines?activity.TriggerBinDb:null);
        }
        if(frequency is {} hz)
        {
            // Intentionally bypass LiveTune/Retune: these can move HackRF capture to maintain its offset.
            try{receiver.SetChannel(channel with{Frequency=hz});}
            catch(Exception ex){StopActivity("Scanner tuning stopped: "+ex.Message);}
        }
        if(time-activitySaved>=5){activitySaved=time;FlushActivity();}
    }
    private void RefreshActivity()
    {
        bool live=ActivityLive;
        if(activityCapture!=null&&(!live||DisplaySettings.Clock-activity.LastFrame>2))ResetActivity("Scanner stopped: RF data interrupted. Restart after reception recovers.");
        string state=!live?"Live SDR required. Replay/audio scanning is unavailable.":!activity.Ready||activityCapture==null?"Calibrating noise floor (about 1.2 seconds)…":activity.Running?
            (activity.Held?"HOLD":activity.Current!=null?"Monitoring":"Searching")+" · "+activityMessage:activityMessage;
        string detail=activity.Ready&&activityCapture!=null?$"Estimated channel floor {activity.ChannelFloorDb:0.0} dB rel.\nRF trigger {activity.ChannelThresholdDb:0.0} dB rel. · {(ActivityControls.Options.AutoSquelch?"automatic":"manual")}\n{(activity.Current is {} hz?$"Listening {hz/1e6:0.######} MHz · ":"")}{activity.Candidates.Count} candidates · {activity.IgnoredCount} ignored\nLines show equivalent FFT-bin levels at the listening channel; the RF gate uses integrated channel power.":"";
        ActivityControls.Display(live,activity.Running,activity.Held,activity.Current!=null,state,detail+(activitySaveError.Length>0?"\n"+activitySaveError:""));
    }
}
