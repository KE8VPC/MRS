using System.IO;

using System.Text.Json;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Media;

namespace MRS;

public partial class MainWindow

{

    private readonly TriggeredIqRecorder automaticIq=new();

    private readonly VoxMp3Recorder automaticAudio=new();

    private string automaticMessage="";
    private int automaticFormat;private double voxThreshold=-35;private bool voxStereo;

    private readonly ComboBox automaticFormatBox=new(){ItemsSource=new[]{"I/Q (RF trigger)","MP3 audio (VOX)","Both MP3 + I/Q (VOX)"},SelectedIndex=0};

    private sealed record TriggerGate(long Center,int Rate,int Gain,ChannelSettings Channel,double Threshold,DateTimeOffset Updated);

    private TriggerGate? triggerGate;

    private readonly TextBlock automaticIqStatus=new(){Foreground=Brushes.LightGreen,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};

    private Button automaticIqArm=new(){Content="Arm automatic I/Q"},automaticIqStop=new(){Content="Disarm / finish clip"};

    private string automaticFolder="";

    private TriggeredIqOptions automaticOptions=new();

    private sealed record TriggerPreferences(string Folder,TriggeredIqOptions Options,int Format=0,double VoxThreshold=-35,bool Stereo=false);

    private void InitializeTriggeredRecording()

    {

        automaticFolder=Path.Combine(diagnosticWorkspace?dataFolder:Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"MRS Recordings","Automatic");

        string settings=Path.Combine(dataFolder,"automatic-iq.json");

        try{if(File.Exists(settings)&&JsonSerializer.Deserialize<TriggerPreferences>(File.ReadAllText(settings)) is {} p){automaticFolder=p.Folder;automaticOptions=p.Options.Normalize();automaticFormat=Math.Clamp(p.Format,0,2);voxThreshold=double.IsFinite(p.VoxThreshold)?Math.Clamp(p.VoxThreshold,-80,-3):-35;voxStereo=p.Stereo;}}catch(Exception ex){Log("Automatic recording preferences: "+ex.Message);}

        var body=TriggeredRecordingHost;var folder=new TextBlock{Text=automaticFolder,Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap};body.Children.Add(folder);

        var choose=new Button{Content="Choose recording folder…"};body.Children.Add(choose);

        void Save(){try{File.WriteAllText(settings,JsonSerializer.Serialize(new TriggerPreferences(automaticFolder,automaticOptions,automaticFormat,voxThreshold,voxStereo)));}catch(Exception ex){automaticIqStatus.Text="Settings save failed: "+ex.Message;}}

        choose.Click+=(_,_)=>{if(automaticIq.Armed||automaticAudio.Armed)return;var picker=new Microsoft.Win32.OpenFolderDialog();if(picker.ShowDialog(this)==true){folder.Text=automaticFolder=picker.FolderName;Save();}};

        void Slider(string title,double min,double max,Func<TriggeredIqOptions,double> read,Func<TriggeredIqOptions,double,TriggeredIqOptions> set,double wheel)

        {

            var label=new TextBlock{Foreground=Brushes.White,Margin=new Thickness(0,6,0,0)};var slider=new Slider{Minimum=min,Maximum=max,Value=read(automaticOptions)};label.Text=$"{title}: {slider.Value:0.##}";body.Children.Add(label);body.Children.Add(slider);SliderWheel.Enable(slider,wheel);

            slider.ValueChanged+=(_,_)=>{automaticOptions=set(automaticOptions,slider.Value).Normalize();label.Text=$"{title}: {read(automaticOptions):0.##}";Save();};

        }

        body.Children.Add(automaticFormatBox);automaticFormatBox.SelectedIndex=automaticFormat;

        automaticFormatBox.SelectionChanged+=(_,_)=>{if(automaticIq.Armed||automaticAudio.Armed){automaticFormatBox.SelectedIndex=automaticFormat;return;}automaticFormat=automaticFormatBox.SelectedIndex;Save();};

        var voxLabel=new TextBlock{Text=$"Audio VOX threshold: {voxThreshold:0} dBFS",Foreground=Brushes.White};var voxSlider=new Slider{Minimum=-80,Maximum=-3,Value=voxThreshold};SliderWheel.Enable(voxSlider,1);body.Children.Add(voxLabel);body.Children.Add(voxSlider);voxSlider.ValueChanged+=(_,_)=>{voxThreshold=voxSlider.Value;voxLabel.Text=$"Audio VOX threshold: {voxThreshold:0} dBFS (next Arm)";Save();};

        var stereo=new CheckBox{Content="Stereo MP3 (FM broadcast)",IsChecked=voxStereo,Foreground=Brushes.White};body.Children.Add(stereo);stereo.Checked+=(_,_)=>{voxStereo=true;Save();};stereo.Unchecked+=(_,_)=>{voxStereo=false;Save();};

        Slider("Pre-trigger seconds (64 MB cap)",0,3,p=>p.PreSeconds,(p,v)=>p with{PreSeconds=v},.1);

        Slider("Quiet tail seconds",.2,15,p=>p.TailSeconds,(p,v)=>p with{TailSeconds=v},.1);

        Slider("Maximum clip seconds",1,300,p=>p.MaximumSeconds,(p,v)=>p with{MaximumSeconds=v},1);

        Slider("Storage limit MB (per recorder)",64,16384,p=>p.BudgetMb,(p,v)=>p with{BudgetMb=(int)v},64);

        body.Children.Add(new TextBlock{Text="I/Q uses the calibrated RF threshold. MP3 uses squelched listening audio before speaker volume (including decoded voice); mono 64 kbps or stereo 128 kbps. Both uses audio VOX for both recorders. Encoding is background work; temporary PCM files count toward the MP3 storage limit. Failed encodes retain WAV evidence and report an error. Settings apply at Arm. Stop/disconnect disarms; retune starts a separate clip.",Foreground=Brushes.LightSteelBlue,TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,6,0,4)});

        var actions=new WrapPanel();actions.Children.Add(automaticIqArm);actions.Children.Add(automaticIqStop);body.Children.Add(actions);body.Children.Add(automaticIqStatus);

        automaticIqArm.Click+=(_,_)=>

        {

            try

            {

                automaticMessage="";
                if(automaticFormat!=1&&(!ActivityLive||receiver.IsRecording))throw new InvalidOperationException("Start live SDR and finish manual I/Q recording first.");

                if(automaticFormat==1&&!engine.Active)throw new InvalidOperationException("Start a listening source before arming MP3 VOX.");

                if(automaticFormat==0&&(Volatile.Read(ref triggerGate) is not {} g||DateTimeOffset.UtcNow-g.Updated>TimeSpan.FromSeconds(2)))throw new InvalidOperationException("Wait for RF noise-floor calibration before arming I/Q.");

                if(automaticFormat!=0)automaticAudio.Arm(automaticFolder,workspaceSlot,new(voxThreshold,automaticOptions.PreSeconds,automaticOptions.TailSeconds,automaticOptions.MaximumSeconds,automaticOptions.BudgetMb,voxStereo));

                try{if(automaticFormat!=1)automaticIq.Arm(automaticFolder,workspaceSlot,new(3,DateTimeOffset.UtcNow,receiver.AppliedChannel!.Mode=="WFM",receiver.RequestedGain,recoveryPlan?.Identity,receiver.AppliedChannel.Mode,receiver.SamplingMode,receiver.Backend),automaticOptions);}catch{automaticAudio.Disarm();throw;}Save();



            }

            catch(Exception ex){automaticMessage=ex.Message;automaticIqStatus.Text=ex.Message;Log("Automatic recording: "+ex.Message);}

        };

        automaticIqStop.Click+=(_,_)=>{automaticMessage="";automaticIq.Disarm();automaticAudio.Disarm();};

        engine.RecordingAudio+=(pcm,channels)=>automaticAudio.Feed(pcm,pcm.Length,channels,$"{(receiver.IsReplay?"replay":"source")}:{Interlocked.Read(ref session)}:{(trunkRadio.Running?trunkAudioFrequency:receiver.AppliedChannel?.Frequency)}:{receiver.AppliedChannel?.Mode}",rfMode?(trunkRadio.Running?trunkAudioFrequency:receiver.AppliedChannel?.Frequency??0):0);

        engine.PcmBoundary+=automaticAudio.Boundary;

        receiver.TriggeredIq+= (frame,db)=>

        {

            var g=Volatile.Read(ref triggerGate);

            bool valid=g!=null&&g.Center==frame.Frequency&&g.Rate==frame.Rate&&g.Gain==frame.Gain&&g.Channel==frame.Channel&&DateTimeOffset.UtcNow-g.Updated<TimeSpan.FromSeconds(2);

            automaticIq.Feed(frame,Volatile.Read(ref automaticFormat)==2?automaticAudio.SignalOpen:valid&&double.IsFinite(db)&&db>=g!.Threshold);

        };

        receiver.Ended+=_=>{automaticIq.Disarm("Disarmed - receiver ended");automaticAudio.Disarm("Disarmed - receiver ended");};

    }

    private void UpdateTriggerGate()

    {

        Volatile.Write(ref triggerGate,ActivityLive&&activity.Ready&&receiver.AppliedChannel is {} ch?new(receiver.AppliedFrequency,receiver.AppliedRate,receiver.AppliedGain,ch,activity.ChannelThresholdDb,DateTimeOffset.UtcNow):null);

    }

    private void RefreshTriggeredRecording()

    {

        automaticIqArm.Content=automaticFormat==0?"Arm automatic I/Q":automaticFormat==1?"Arm MP3 VOX":"Arm MP3 + I/Q VOX";

        automaticIqArm.IsEnabled=(automaticFormat==1?engine.Active:ActivityLive&&!receiver.IsRecording)&&!automaticIq.Armed&&!automaticAudio.Armed;

        automaticIqStop.IsEnabled=automaticIq.Armed||automaticAudio.Armed;

        automaticFormatBox.IsEnabled=!automaticIq.Armed&&!automaticAudio.Armed;

        if(automaticFormat==2&&automaticIq.Armed&&!automaticAudio.Armed)automaticIq.Disarm("Disarmed · audio VOX stopped");

        if(automaticFormat==2&&automaticAudio.Armed&&!automaticIq.Armed)automaticAudio.Disarm("Disarmed - I/Q recorder stopped");
        automaticIqStatus.Text=automaticFormat==0?$"{automaticIq.Status}\n{automaticIq.Clips} I/Q clips · {automaticIq.TotalBytes/1000000.0:0.0} MB":$"{automaticAudio.Status}\n{automaticAudio.Clips} MP3 clips · {automaticAudio.Bytes/1000000.0:0.0} MB"+(automaticFormat==2?$"\n{automaticIq.Status} · {automaticIq.Clips} I/Q clips":"");
        if(automaticMessage.Length>0)automaticIqStatus.Text=automaticMessage+"\n"+automaticIqStatus.Text;

    }

}
