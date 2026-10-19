using System.Collections.Concurrent;



using System.Collections.ObjectModel;



using System.Text.Json;



using System.Threading.Channels;



using System.Windows;



using System.Windows.Controls;



using System.Windows.Data;



using System.Windows.Media;



namespace MRS;



internal sealed record DmrReception(DmrObservation Fields,PacketContext Context,DateTimeOffset Seen);



internal sealed class DmrPipeline : IDisposable



{



    private readonly Channel<(double[] Samples,PacketContext Context,long Epoch)> input=Channel.CreateBounded<(double[],PacketContext,long)>(64);



    private readonly ConcurrentQueue<(DmrReception Row,long Epoch)> output=new();private readonly CancellationTokenSource stop=new();private long epoch,dropped;internal volatile bool Enabled;



    internal long Dropped=>Interlocked.Read(ref dropped);



    internal DmrPipeline(){_ = Task.Run(async()=>{DmrDecoder decoder=new();LtrDecoder ltr=new();PacketContext? current=null;string? key=null;long generation=-1;



        void Receive(DmrObservation m){if(current!=null){if(output.Count>=256){output.TryDequeue(out _);Interlocked.Increment(ref dropped);}output.Enqueue((new(m,current,DateTimeOffset.UtcNow),generation));}}

        void Attach(){decoder.Message+=Receive;ltr.Message+=Receive;}



        Attach();try{await foreach(var block in input.Reader.ReadAllAsync(stop.Token)){if(block.Epoch!=Interlocked.Read(ref epoch)||!Enabled)continue;if(generation!=block.Epoch||key!=block.Context.Key){decoder=new();ltr=new();Attach();generation=block.Epoch;key=block.Context.Key;}current=block.Context;decoder.Process(block.Samples);ltr.Process(block.Samples);}}catch(OperationCanceledException){} });}



    internal void Reset()=>Interlocked.Increment(ref epoch);



    internal void Feed(double[] samples,PacketContext context){if(!Enabled)return;if(!input.Writer.TryWrite(((double[])samples.Clone(),context,Interlocked.Read(ref epoch)))){Interlocked.Increment(ref dropped);Reset();}}



    internal DmrReception[] Drain(){var list=new List<DmrReception>();while(list.Count<128&&output.TryDequeue(out var row))if(row.Epoch==Interlocked.Read(ref epoch))list.Add(row.Row);return list.ToArray();}



    public void Dispose(){Enabled=false;stop.Cancel();input.Writer.TryComplete();}



}



public sealed class DmrPanel : UserControl



{



    private sealed class Row



    {



        public long Frequency{get;init;}public string MHz=>(Frequency/1e6).ToString("F6");public int CC{get;init;}public bool Replay{get;init;}public string Source{get;init;}="";



        public string Slots=>string.Join(", ",SlotSet.Order());public string Talkgroups=>string.Join(", ",Groups.Order());public string Radios=>string.Join(", ",RadioSet.Order());



        public string Protocol{get;init;}="DMR";public string Identity{get;init;}="";

        internal readonly HashSet<string> Types=[],Networks=[],Sites=[];

        public string SystemTypes=>string.Join(", ",Types);public string NetworkIds=>Networks.Count==0?"Unknown / not decoded":string.Join(", ",Networks);public string SiteIds=>Sites.Count==0?"Unknown / not decoded":string.Join(", ",Sites);

        public string Routing{get;set;}="";

        public string Confidence=>Count>=3?"Repeated observations":"Single observation";

        public int Count{get;set;}public string LastSeen=>Seen.ToLocalTime().ToString("HH:mm:ss");public DateTimeOffset Seen;internal readonly Dictionary<string,(int Count,DmrReception Reception,bool Saved)> Confirmations=[];internal readonly HashSet<int> SlotSet=[];internal readonly HashSet<uint> Groups=[],RadioSet=[];internal DmrReception? Latest;



    }



    private readonly TrunkEvidence evidence=new();private readonly DmrPipeline pipeline=new();private readonly ObservableCollection<Row> rows=[];



    private readonly CheckBox enable=new(){Content="Decode DMR / Cap+ and LTR",Foreground=Brushes.White};



    private readonly CheckBox save=new(){Content="Auto-save/update identified frequencies (live only)",Foreground=Brushes.White};



    private readonly TextBlock status=new(){Foreground=Brushes.Aquamarine,TextWrapping=TextWrapping.Wrap};



    private readonly DataGrid grid=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,Foreground=Brushes.Black,Background=Brushes.White};



    internal event Action<bool>? EnabledChanged;internal event Action<PacketObservation>? Observation;internal Func<DmrReception,bool>? SaveChannel;



    internal bool Enabled=>pipeline.Enabled;



    public DmrPanel()



    {



        var root=new DockPanel{Margin=new Thickness(10)};var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);



        head.Children.Add(new TextBlock{Text="DMR / MOTOTRBO · CHANNEL IDENTIFICATION",FontSize=17,Foreground=Brushes.White});



        var controls=new WrapPanel();controls.Children.Add(enable);controls.Children.Add(save);var clear=new Button{Content="Clear channel observations"};clear.Click+=(_,_)=>{rows.Clear();evidence.Clear();};controls.Children.Add(clear);head.Children.Add(controls);head.Children.Add(status);



        head.Children.Add(new TextBlock{Text="Select NFM, typically 12.5–16 kHz. DMR / Cap+ control IDs and LTR Standard outbound signaling run alongside FM audio. NET/SITE appear only when decoded; equal color codes never merge frequencies. LTR area/home/group is not a unique system ID. Rows require three matching observations: DMR within 10 seconds, LTR within 60 seconds. Auto-save needs repeated confirmed observations; your labels stay intact. No digital voice or trunk following. Use Data for raw fields.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,6,0,8)});



        foreach(var (title,path,width) in new[]{("MHz","MHz",110),("Protocol","Protocol",70),("Identity","Identity",100),("System type","SystemTypes",150),("Network (wire)","NetworkIds",150),("Site (wire)","SiteIds",150),("Routing / status","Routing",260),("Slots","Slots",65),("Talkgroups","Talkgroups",150),("Radio IDs","Radios",180),("Evidence","Confidence",145),("Count","Count",60),("Last seen","LastSeen",80),("Replay","Replay",60),("Source","Source",220)})grid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(path),Width=width});



        grid.ItemsSource=rows;root.Children.Add(grid);Content=root;



        enable.Checked+=(_,_)=>Change();enable.Unchecked+=(_,_)=>Change();



        void Change(){pipeline.Enabled=enable.IsChecked==true;pipeline.Reset();EnabledChanged?.Invoke(pipeline.Enabled);}



    }



    internal async Task Smoke()



    {



        enable.IsChecked=true;var bits=DmrTests.Bits(Convert.FromHexString(DmrTests.Vectors()[14].Trim().Split('\t')[2]));var samples=new List<double>();



        for(int repeat=0;repeat<4;repeat++)foreach(var frame in new[]{new bool[24],bits})for(int i=0;i<frame.Length;i+=2){int d=(frame[i]?2:0)|(frame[i+1]?1:0);double level=d switch{0=>648,1=>1944,2=>-648,_=>-1944};for(int n=0;n<10;n++)samples.Add(level);}



        Feed(samples.ToArray(),new("dmr-smoke","Synthetic replay",true,461125000));for(int n=0;n<40&&rows.Count==0;n++){await Task.Delay(25);Refresh(true);}



        if(rows.Count!=1||rows[0].Count!=4||rows[0].Talkgroups!="91"||rows[0].Radios!="12345")throw new InvalidOperationException("DMR stable UI rows failed");
        Feed(TrunkTests.DmrSamples(TrunkTests.CapCall),new("cap-smoke","Synthetic replay",true,461150000));
        for(int n=0;n<40&&rows.Count<2;n++){await Task.Delay(25);Refresh(true);}
        Feed(TrunkTests.DmrSamples(TrunkTests.CapStatus),new("cap-smoke2","Synthetic replay",true,461175000));
        for(int n=0;n<40&&rows.Count<3;n++){await Task.Delay(25);Refresh(true);}
        Feed(TrunkTests.LtrSamples(TrunkTests.LtrWord()),new("ltr-smoke","Synthetic replay",true,461200000));
        for(int n=0;n<60&&rows.Count<4;n++){await Task.Delay(25);Refresh(true);}
        if(rows.Count!=4||rows.Count(r=>r.CC==6)!=2||rows.Single(r=>r.Protocol=="LTR").Count!=4)throw new InvalidOperationException("Trunk UI must keep same-color-code frequencies separate and group LTR repeats");
        enable.IsChecked=false;



    }



    internal void Feed(double[] samples,PacketContext context)=>pipeline.Feed(samples,context);



    internal void Reset()=>pipeline.Reset();internal void Release()=>pipeline.Dispose();



    private long lastRefresh;

    internal void Refresh(bool nfm)



    {



        long now=Environment.TickCount64;if(now-lastRefresh<200)return;lastRefresh=now;

        string? error=null;bool changed=false;



        foreach(var reception in pipeline.Drain())



        {



            changed=true;

            if(reception.Context.FrequencyHz is not {} hz)continue;var f=reception.Fields;



            int evidenceCount=evidence.Observe(reception);if(evidenceCount<3)continue;
            var row=rows.FirstOrDefault(r=>r.Frequency==hz&&r.CC==f.ColorCode&&r.Protocol==f.Protocol&&r.Identity==f.Identity&&r.Replay==reception.Context.Replay&&r.Source==reception.Context.Source);



            if(row==null){if(rows.Count>=500)rows.RemoveAt(0);row=new(){Count=evidenceCount-1,Frequency=hz,CC=f.ColorCode,Protocol=f.Protocol,Identity=f.Identity,Source=reception.Context.Source,Replay=reception.Context.Replay};rows.Add(row);}



            string signature=$"{reception.Context.Key}:{f.ColorCode}:{f.Slot}:{f.Source}:{f.Destination}:{f.LinkValid}:{f.Protocol}:{f.Identity}:{f.SystemType}:{f.Network}:{f.Site}";



            if(row.Confirmations.TryGetValue(signature,out var confirmation))row.Confirmations[signature]=(confirmation.Count+1,reception,confirmation.Saved);else if(row.Confirmations.Count<128)row.Confirmations[signature]=(1,reception,false);



            row.Count++;if(f.SystemType!="DMR (type unknown)"){row.Types.Remove("DMR (type unknown)");row.Types.Add(f.SystemType);}else if(row.Types.Count==0)row.Types.Add(f.SystemType);

            if(f.Network is {} net&&row.Networks.Count<32)row.Networks.Add(net.ToString());if(f.Site is {} site&&row.Sites.Count<32)row.Sites.Add(site.ToString());if(f.Detail.Length>0)row.Routing=f.TrunkSummary;

            row.Seen=reception.Seen;row.Latest=reception;if(f.Slot is {} slot)row.SlotSet.Add(slot);if(f.LinkValid){if(f.Source is {} id&&row.RadioSet.Count<128)row.RadioSet.Add(id);if(f.Group&&f.Destination is {} tg&&row.Groups.Count<128)row.Groups.Add(tg);}



            string summary=f.Protocol=="LTR"?f.TrunkSummary:f.TrunkSummary+$" · CC {f.ColorCode} · slot {f.Slot?.ToString()??"unknown"}"+(f.Source is {} src?$" · radio {src} · {(f.Group?"TG":"destination")} {f.Destination}":"");



            Observation?.Invoke(new(reception.Seen,reception.Context.Source,reception.Context.Replay,hz,reception.Context.PositionSeconds,f.Validation,f.Source?.ToString()??"",f.Destination?.ToString()??"","",f.Protocol=="LTR"?f.Detail:f.DataType==-2?"Short link control":f.DataType==3?"Control signaling":f.LinkValid?"Full link control":"Slot identification",summary,null,f.Raw,f.Protocol,f.DataType==3?f.LinkValid:true,SourceKey:reception.Context.Key,DecoderJson:JsonSerializer.Serialize(f)));







        }



        if(save.IsChecked==true)foreach(var row in rows.Where(r=>!r.Replay))foreach(var entry in row.Confirmations.Where(c=>c.Value.Count>=3&&!c.Value.Saved&&(c.Value.Reception.Fields.DataType!=3||c.Value.Reception.Fields.LinkValid)).ToArray())



        {try{if(SaveChannel?.Invoke(entry.Value.Reception)==true)row.Confirmations[entry.Key]=(entry.Value.Count,entry.Value.Reception,true);}catch(Exception ex){error=ex.Message;}}



        if(changed)grid.Items.Refresh();status.Text=error!=null?"Save failed: "+error:!Enabled?"DMR / LTR off":!nfm?"DMR / LTR: select NFM on the current SDR channel":$"DMR / LTR searching / receiving · {rows.Count} confirmed identities · {evidence.Progress(DateTimeOffset.UtcNow)} · dropped {pipeline.Dropped}";



    }



}



