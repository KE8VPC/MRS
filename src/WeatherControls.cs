using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MRS;

public partial class MainWindow
{
    private static readonly long[] NoaaChannels = Enumerable.Range(0,7).Select(i=>162400000L+25000L*i).ToArray();
    private readonly PacketPanel weatherAlerts = new();
    private readonly CheckBox weatherSame = new() { Content="Decode SAME headers", Foreground=Brushes.Gainsboro, Margin=new(6) };
    private readonly CheckBox weatherTone = new() { Content="Detect 1050 Hz tone", Foreground=Brushes.Gainsboro, Margin=new(6) };
    private readonly TextBlock weatherAlertStatus = ModeStatus(), faxScheduleStatus = ModeStatus();
    private readonly Queue<PacketObservation> weatherTones = new();
    private WeatherAlarmTone alarmTone = new();
    private volatile bool weatherToneEnabled;
    private bool weatherTuneBusy;
    private (ListeningPreset Preset, DateTimeOffset At, long Session, int Worker)? faxTune;

    private TabControl CreateWeatherWorkspace()
    {
        var tabs = new TabControl();
        WorkspaceTabs.Items.Add(new TabItem { Header="Weather", Content=tabs });
        return tabs;
    }

    private void AddFaxSchedule(Panel header)
    {
        var controls = new StackPanel();
        header.Children.Insert(0,new Expander { Header="Broadcast schedule and tuning", IsExpanded=true,
            Foreground=Brushes.Gainsboro, Content=controls, Margin=new(0,0,0,5) });
        var row = new WrapPanel();
        controls.Children.Add(row);
        var scheduleSummary = ModeStatus();
        controls.Children.Add(scheduleSummary);
        controls.Children.Add(faxScheduleStatus);
        var channels = new ComboBox { Width=350 };
        var starts = new ComboBox { Width=100 };
        var custom = new TextBox { Width=75, ToolTip="Optional exact chart start time from the schedule" };
        ModeFields(row,("Station and USB dial",channels),("Session start UTC",starts),("Chart UTC (HH:mm)",custom));
        string path = Path.Combine(filePlans?.Root ?? AppContext.BaseDirectory,
            "reference","marine-wefax","channels.csv");
        if (!File.Exists(path))
            path=Path.Combine(AppContext.BaseDirectory,"bandplans","reference","marine-wefax","channels.csv");

        WeatherFaxStation? Station() => channels.SelectedItem is ListeningPreset p
            ? WeatherFaxStation.All.FirstOrDefault(s=>p.Name.StartsWith("WEFAX "+s.Call+" ",StringComparison.Ordinal))
            : null;
        void LoadSchedule()
        {
            if (channels.SelectedItem is not ListeningPreset preset || Station() is not {} station)
            {
                starts.ItemsSource=null;
                scheduleSummary.Text="";
                return;
            }
            var times=station.StartsUtc.Select(WeatherFaxStation.FormatTime)
                .Where(time=>WeatherFaxStation.TryTime(time,out var utc)&&station.AvailableAt(preset.Hz,utc)).ToArray();
            starts.ItemsSource=times;
            starts.SelectedIndex=0;
            scheduleSummary.Text="Published session starts UTC · "+string.Join("  ·  ",times);
        }
        channels.SelectionChanged+=(_,_)=>LoadSchedule();
        try
        {
            var presets=DiscoveryReferences.Read(path);
            foreach (var p in presets) p.Validate();
            channels.ItemsSource=presets;
            channels.SelectedIndex=0;
            faxScheduleStatus.Text="NOAA session starts. Open the station schedule for chart times.";
        }
        catch(Exception ex)
        { faxScheduleStatus.Text="WEFAX channels unavailable · "+ex.Message; }

        ModeButton(row,"Station schedule",()=>{
            if(Station() is not {} station)throw new IOException("Choose a NOAA station first.");
            try { Process.Start(new ProcessStartInfo(station.ScheduleUrl){UseShellExecute=true}); }
            catch(System.ComponentModel.Win32Exception ex) { throw new IOException("Unable to open the station schedule.",ex); }
        });
        ModeButton(row,"Tune now",()=>{
            if(channels.SelectedItem is not ListeningPreset preset)throw new IOException("Choose a WEFAX channel first.");
            faxTune=null;
            _=TuneWeather(preset,faxScheduleStatus);
        });
        ModeButton(row,"Arm auto-tune",()=>{
            if(channels.SelectedItem is not ListeningPreset preset||Station() is not {} station)
                throw new IOException("Choose a NOAA station and channel first.");
            CheckWeatherTune(preset);
            string time=string.IsNullOrWhiteSpace(custom.Text)?starts.SelectedItem?.ToString()??"":custom.Text;
            if(!WeatherFaxStation.TryTime(time,out var utc))
                throw new IOException("Choose a session or enter a chart start time in UTC (HH:mm).");
            if(!station.AvailableAt(preset.Hz,utc))
                throw new IOException("The published channel window does not include that time.");
            var at=WeatherFaxStation.NextTune(DateTimeOffset.UtcNow,utc);
            faxTune=(preset,at,session,receiver.WorkerId);
            faxScheduleStatus.Text=$"Armed once · {preset} · tune {at:yyyy-MM-dd HH:mm:ss} UTC";
        });
        ModeButton(row,"Cancel auto-tune",()=>{faxTune=null;faxScheduleStatus.Text="Auto-tune cancelled";});
    }

    private void InitializeNoaaWeather(TabControl tabs)
    {
        weatherAlerts.ConfigureWorkspace("RadioData");
        weatherAlerts.ShowUnverified(true);
        AddModePage(tabs,"NOAA Weather Radio",weatherAlerts,out var row,out var header);
        var choices = new ComboBox { Width=145, SelectedIndex=0,
            ItemsSource=NoaaChannels.Select(hz=>PlanCsv.MHz(hz)+" MHz").ToArray() };
        ModeFields(row,("NOAA channel",choices));
        ModeButton(row,"Tune NOAA",()=>{
            int index=choices.SelectedIndex;
            if(index<0)return;
            _=TuneWeather(new ListeningPreset { Name="NOAA Weather Radio",
                FrequencyMHz=PlanCsv.MHz(NoaaChannels[index]), Mode="NFM", BandwidthHz=16000 },weatherAlertStatus);
        });
        row.Children.Add(weatherSame);
        row.Children.Add(weatherTone);
        header.Children.Add(weatherAlertStatus);
        weatherAlertStatus.Text="Choose a NOAA channel and enable the alert decoders you want to monitor.";
        weatherSame.Checked+=(_,_)=>ConfigureSignaling();
        weatherSame.Unchecked+=(_,_)=>ConfigureSignaling();
        weatherTone.Checked+=(_,_)=>{weatherToneEnabled=true;ResetWeatherTone();UpdateDataCapture();};
        weatherTone.Unchecked+=(_,_)=>{weatherToneEnabled=false;ResetWeatherTone();UpdateDataCapture();};
        monitorOrders.Add(new(weatherAlerts.MonitorGrid,Path.Combine(dataFolder,"weather-alert-order.json")));
        receiver.ChannelChanged+=ResetWeatherTone;
        receiver.ReplayGap+=ResetWeatherTone;
        receiver.Ended+=_=>ResetWeatherTone();
        receiver.DataSamples+=samples=>{
            if(!weatherToneEnabled||receiver.AppliedChannel is not {Mode:"NFM"} ch||!IsNoaaChannel(ch.Frequency))return;
            var context=RadioContext("weather-tone",ch.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null);
            lock(receiveModeSync)
            {
                if(!weatherToneEnabled||!alarmTone.Feed(samples))return;
                if(weatherTones.Count>=30)weatherTones.Dequeue();
                weatherTones.Enqueue(new PacketObservation(DateTimeOffset.UtcNow,context.Source,context.Replay,
                    context.FrequencyHz,context.PositionSeconds,"Tone detection","NOAA","","","Attention tone",
                    "Sustained 1050 Hz tone detected",null,"","NOAA 1050 Hz",false,SourceKey:context.Key));
            }
        };
    }

    private static bool IsNoaaChannel(long? hz) => hz is long value && Array.IndexOf(NoaaChannels,value)>=0;

    private void ResetWeatherTone()
    {
        lock(receiveModeSync)alarmTone=new WeatherAlarmTone();
    }

    private void RouteWeatherSame(PacketObservation packet)
    {
        if(weatherSame.IsChecked==true&&packet.Protocol=="SAME / EAS"&&IsNoaaChannel(packet.FrequencyHz))
            weatherAlerts.Add(packet);
    }

    private void CheckWeatherTune(ListeningPreset preset)
    {
        if(!ActivityLive||receiver.WorkerId==0||!receiver.CanTune(preset.Hz))
            throw new IOException("Start a live SDR that covers the selected frequency.");
        if(weatherTuneBusy||weakTxHandoff||weakTransmitWindow!=null||txIndicatorActive||
            activity.Running||FolderRunning||trunkRadio.Running||dmrTrunkRadio.Running||
            lcnSurvey.Running||surveyTuneBusy||fleetOwner!=null||dmrFleetOwner!=null||
            receiver.IsRecording||FinishRecordButton.IsEnabled||automaticIq.Armed||automaticAudio.Armed||hdListening||tvActive)
            throw new IOException("The receiver is busy. Stop scanning, following, recording or transmitting before tuning.");
    }

    private async Task TuneWeather(ListeningPreset preset,TextBlock status)
    {
        try { CheckWeatherTune(preset); }
        catch(IOException ex) { status.Text=ex.Message;return; }
        weatherTuneBusy=true;
        long tuneSession=session;
        try
        {
            var channel=new ChannelSettings(preset.Hz,preset.Mode,preset.BandwidthHz);
            status.Text=$"Tuning {preset} · {preset.Mode}";
            if((!receiver.IsHackRf||!receiver.Offset.Enabled)&&channel.Fits(receiver.AppliedFrequency,receiver.AppliedBandwidth))
                receiver.SetChannel(channel);
            else
                await receiver.Retune(channel.Frequency,channel);
            for(int i=0;i<50&&session==tuneSession&&receiver.AppliedChannel!=channel;i++)
                await Task.Delay(100);
            if(closing)return;
            status.Text=session==tuneSession&&receiver.AppliedChannel==channel
                ? $"Tuned {preset} · {preset.Mode}" : "Receiver did not confirm the weather channel.";
        }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or ArgumentException or TimeoutException)
        { if(!closing)status.Text="Weather tune failed · "+ex.Message; }
        finally { weatherTuneBusy=false; }
    }

    private void TickWeather()
    {
        PacketObservation[] tones;
        lock(receiveModeSync){tones=weatherTones.ToArray();weatherTones.Clear();}
        foreach(var packet in tones)weatherAlerts.Add(packet);
        if(faxTune is not {} schedule)return;
        if(session!=schedule.Session||receiver.IsReplay||!rfMode||receiver.WorkerId!=schedule.Worker)
        {
            faxTune=null;
            faxScheduleStatus.Text="Auto-tune cancelled · receiver changed";
            return;
        }
        var now=DateTimeOffset.UtcNow;
        if(now<schedule.At)return;
        faxTune=null;
        // Ignore a timer event delayed by sleep or a substantial clock adjustment.
        if(now-schedule.At>TimeSpan.FromSeconds(30))
            faxScheduleStatus.Text="Auto-tune missed · arm another session";
        else
            _=TuneWeather(schedule.Preset,faxScheduleStatus);
    }
}
