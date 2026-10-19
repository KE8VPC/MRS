using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MRS;
public sealed class ActivityPanel:UserControl
{
    private ActivityOptions options=new();private string? path;private bool loading;
    private readonly CheckBox automatic=new(){Content="Automatic RF squelch",Foreground=Brushes.White};
    private readonly TextBlock status=new(){Text="Start a live SDR receiver to measure the floor.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGreen,Margin=new Thickness(0,12,0,8)};
    private readonly TextBlock levels=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue};
    private readonly Button start=new(){Content="Start scanner"},stop=new(){Content="Stop"},skip=new(){Content="Skip / ignore"};
    private readonly CheckBox hold=new(){Content="Hold this frequency",Foreground=Brushes.White};
    internal ActivityOptions Options=>options;
    internal event Action? StartRequested,StopRequested,SkipRequested,ClearIgnoredRequested,RecalibrateRequested;
    internal event Action<bool>? HoldChanged;
    internal event Action<ActivityOptions>? Changed;
    internal event Action<long>? TuneRequested;
    private readonly List<Action> load=[];
    public ActivityPanel()
    {
        var body=new StackPanel{Margin=new Thickness(8)};Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        void Text(string value)=>body.Children.Add(new TextBlock{Text=value,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,5,0,10),FontSize=12});
        Text("RF activity scanner · live SDR only");body.Children.Add(automatic);
        automatic.Checked+=(_,_)=>Publish(options with{AutoSquelch=true});automatic.Unchecked+=(_,_)=>Publish(options with{AutoSquelch=false});
        load.Add(()=>automatic.IsChecked=options.AutoSquelch);
        void Check(string name,Func<ActivityOptions,bool> read,Func<ActivityOptions,bool,ActivityOptions> write)
        {var c=new CheckBox{Content=name,Foreground=Brushes.White,Margin=new Thickness(0,8,0,0)};body.Children.Add(c);load.Add(()=>c.IsChecked=read(options));c.Checked+=(_,_)=>Publish(write(options,true));c.Unchecked+=(_,_)=>Publish(write(options,false));}
        Check("Show floor / trigger lines",s=>s.ShowLines,(s,v)=>s with{ShowLines=v});
        Check("Save discoveries to Frequencies",s=>s.SaveDiscoveries,(s,v)=>s with{SaveDiscoveries=v});
        Check("Discover only (keep listening frequency)",s=>s.DiscoveryOnly,(s,v)=>s with{DiscoveryOnly=v});
        Check("Exclude known birdies from discoveries",s=>s.ExcludeBirdies,(s,v)=>s with{ExcludeBirdies=v});
        Check("Show birdie reference markers",s=>s.ShowBirdies,(s,v)=>s with{ShowBirdies=v});
        var birdies=new Button{Content="Manage known birdies…"};body.Children.Add(birdies);
        birdies.Click+=(_,_)=>
        {
            var editor=new BirdieEditor(options.Birdies??[]){Owner=Window.GetWindow(this)};
            if(editor.ShowDialog()==true&&editor.Result is {} values)
            {
                Publish(options with{Birdies=values});
                try{Save();}catch(Exception ex){status.Text="Birdies active in this session, but settings could not be saved: "+ex.Message;}
            }
        };
        Text("Listening frequency (MHz) · manual tuning stops scanning");
        var tune=new TextBox{ToolTip="Enter a listening frequency in MHz, then press Enter"};body.Children.Add(tune);
        void Tune(){if(FrequencyLibrary.TryFrequency(tune.Text.Trim(),out long hz))TuneRequested?.Invoke(hz);else status.Text="Enter a valid listening frequency in MHz.";}
        tune.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){Tune();e.Handled=true;}};
        var tuneButton=new Button{Content="Tune frequency"};body.Children.Add(tuneButton);tuneButton.Click+=(_,_)=>Tune();
        Text("Scan limits in MHz (blank uses the visible capture). Apply while stopped; Start also applies edits.");
        var low=new TextBox{ToolTip="Lower scan frequency, MHz"};var high=new TextBox{ToolTip="Upper scan frequency, MHz"};
        Text("From MHz");body.Children.Add(low);Text("To MHz");body.Children.Add(high);
        load.Add(()=>{low.Text=options.ScanLowHz is {} a?(a/1e6).ToString("0.######",System.Globalization.CultureInfo.InvariantCulture):"";high.Text=options.ScanHighHz is {} b?(b/1e6).ToString("0.######",System.Globalization.CultureInfo.InvariantCulture):"";});
        bool ApplyRange()
        {
            long? a=null,b=null;
            if(!string.IsNullOrWhiteSpace(low.Text)){if(!FrequencyLibrary.TryFrequency(low.Text.Trim(),out long hz)){status.Text="Invalid lower scan frequency.";return false;}a=hz;}
            if(!string.IsNullOrWhiteSpace(high.Text)){if(!FrequencyLibrary.TryFrequency(high.Text.Trim(),out long hz)){status.Text="Invalid upper scan frequency.";return false;}b=hz;}
            if(a.HasValue&&b.HasValue&&a>=b){status.Text="Lower scan frequency must be below upper frequency.";return false;}
            Publish(options with{ScanLowHz=a,ScanHighHz=b});return true;
        }
        var applyRange=new Button{Content="Apply scan limits"};body.Children.Add(applyRange);applyRange.Click+=(_,_)=>ApplyRange();
        low.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){ApplyRange();e.Handled=true;}};
        high.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){ApplyRange();e.Handled=true;}};
        int advancedStart=body.Children.Count;
        void Slider(string name,double min,double max,Func<ActivityOptions,double> read,Func<ActivityOptions,double,ActivityOptions> write,string unit)
        {var label=new TextBlock{Foreground=Brushes.White,Margin=new Thickness(0,12,0,0)};var slider=new Slider{Minimum=min,Maximum=max};SliderWheel.Enable(slider,unit=="dB"?.1:.01);body.Children.Add(label);body.Children.Add(slider);load.Add(()=>{slider.Value=read(options);label.Text=$"{name}: {slider.Value:0.##} {unit}";});slider.ValueChanged+=(_,_)=>{label.Text=$"{name}: {slider.Value:0.##} {unit}";Publish(write(options,slider.Value));};}
        Slider("Above noise floor",3,30,s=>s.MarginDb,(s,v)=>s with{MarginDb=v},"dB");
        Slider("Minimum activity",.15,3,s=>s.MinimumSeconds,(s,v)=>s with{MinimumSeconds=v},"s");
        Slider("Resume after quiet",.3,10,s=>s.QuietSeconds,(s,v)=>s with{QuietSeconds=v},"s");
        Text("Channel grid (use the correct spacing for your band)");
        var grid=new ComboBox();double[] choices=[0,1000,2500,5000,6250,12500,25000,50000,100000,200000];
        foreach(double value in choices)grid.Items.Add(value==0?"Estimate center (no grid)":$"{value/1000:0.###} kHz");
        load.Add(()=>grid.SelectedIndex=Array.IndexOf(choices,options.GridHz));grid.SelectionChanged+=(_,_)=>{if(grid.SelectedIndex>=0)Publish(options with{GridHz=choices[grid.SelectedIndex]});};body.Children.Add(grid);
        Text("Grid offset (kHz): e.g. 6.25 for centers halfway between 12.5 kHz multiples. Enter to apply.");
        var origin=new TextBox();body.Children.Add(origin);
        load.Add(()=>origin.Text=(options.GridOffsetHz/1000).ToString("0.######",System.Globalization.CultureInfo.InvariantCulture));
        void ApplyOrigin()
        {
            if(double.TryParse(origin.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double khz)&&double.IsFinite(khz)&&Math.Abs(khz)<=1000)
            {Publish(options with{GridOffsetHz=khz*1000});origin.ClearValue(ToolTipProperty);}
            else origin.ToolTip="Enter a finite offset between -1000 and 1000 kHz.";
        }
        origin.LostKeyboardFocus+=(_,_)=>ApplyOrigin();
        origin.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){ApplyOrigin();e.Handled=true;}};
        Text("Grid snapping applies only near an estimated center; otherwise the measured frequency is retained. Grid spacing is independent of receive-filter width.");
        var advanced=new StackPanel();
        while(body.Children.Count>advancedStart){var child=body.Children[advancedStart];body.Children.RemoveAt(advancedStart);advanced.Children.Add(child);}
        body.Children.Add(new Expander{Header="Threshold, timing and channel grid",Foreground=Brushes.White,Content=advanced,Margin=new Thickness(0,8,0,8)});
        var actions=new WrapPanel{Margin=new Thickness(0,12,0,8)};actions.Children.Add(start);actions.Children.Add(stop);body.Children.Add(actions);
        start.Click+=(_,_)=>{if(ApplyRange())StartRequested?.Invoke();};stop.Click+=(_,_)=>StopRequested?.Invoke();
        body.Children.Add(hold);hold.Checked+=(_,_)=>{if(!loading)HoldChanged?.Invoke(true);};hold.Unchecked+=(_,_)=>{if(!loading)HoldChanged?.Invoke(false);};
        var more=new WrapPanel();more.Children.Add(skip);var clear=new Button{Content="Clear ignored"};more.Children.Add(clear);body.Children.Add(more);
        skip.Click+=(_,_)=>SkipRequested?.Invoke();clear.Click+=(_,_)=>ClearIgnoredRequested?.Invoke();
        var reset=new Button{Content="Recalibrate noise floor"};body.Children.Add(reset);reset.Click+=(_,_)=>RecalibrateRequested?.Invoke();
        body.Children.Add(status);body.Children.Add(levels);
        Text("Start locks the current visible RF span and uses the selected receiver mode/filter. Strongest qualified signal first; stay until quiet. Skip ignores that channel until Stop/Start or Clear ignored. One listening channel at a time. Manual tuning stops the scanner.");
        Text("Starting enables automatic RF squelch. Uncheck it to use the Receiver tab's manual threshold. Disable RF squelch there to bypass audio muting. Energy detection does not identify modulation or protocol. Capture-center artifacts and filter-overrun edges are excluded.");
        Refresh();
    }
    internal void Initialize(string file)
    {
        path=file;
        try{if(File.Exists(file))options=(JsonSerializer.Deserialize<ActivityOptions>(File.ReadAllText(file))??new()).Normalize();}
        catch(Exception ex){status.Text="Scan preferences not restored: "+ex.Message;path=null;}
        Refresh();
    }
    private void Refresh(){loading=true;try{foreach(var action in load)action();}finally{loading=false;}}
    private void Publish(ActivityOptions next){if(loading)return;options=next.Normalize();Changed?.Invoke(options);}
    internal void SetReceiverAuto(bool enabled,double margin,bool lines) { options=(options with{AutoSquelch=enabled,MarginDb=margin,ShowLines=lines}).Normalize(); Refresh(); Changed?.Invoke(options); }
    internal void EnableAuto(){options=options with{AutoSquelch=true};Refresh();Changed?.Invoke(options);}
    internal void DisableAuto(){options=options with{AutoSquelch=false};Refresh();Changed?.Invoke(options);}
    internal void Save(){if(path!=null){File.WriteAllText(path+".tmp",JsonSerializer.Serialize(options));File.Move(path+".tmp",path,true);}}
    internal void Display(bool live,bool running,bool held,bool hasCurrent,string message,string detail)
    {
        start.IsEnabled=live&&!running;stop.IsEnabled=running;skip.IsEnabled=running&&hasCurrent&&!options.DiscoveryOnly;hold.IsEnabled=running&&!options.DiscoveryOnly;
        loading=true;hold.IsChecked=held;loading=false;status.Text=message;levels.Text=detail;
    }
}
