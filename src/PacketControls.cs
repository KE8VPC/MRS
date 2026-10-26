namespace MRS;

public partial class MainWindow

{

    private readonly PacketPipeline packetDecoder=new();

    private readonly PagingPipeline pagingDecoder=new();

    private readonly P25Pipeline p25Decoder=new();

    private readonly SubaudiblePipeline subaudibleDecoder=new();

    private string packetAudioSource="Audio input";

    private bool packetAudioReplay;

    private string? lastRdsObservation;

    private bool hdListening, tvActive;

    private void InitializePackets()

    {

        InitializeVoiceIds();
        Dmr.Observation+=AnalysisPackets.Add;

        Dmr.SaveChannel=r=>FrequencyManager.SaveDmr(r,workspaceSlot);

        Dmr.EnabledChanged+=value=>UpdateDataCapture();

        receiver.ChannelChanged+=Dmr.Reset;receiver.ReplayGap+=Dmr.Reset;receiver.Ended+=_=>Dmr.Reset();

        Aviation.SetupRequested=OpenDecoderSetup;Aviation.Tune=hz=>_ = LiveTune(hz);Aviation.Observation+=AnalysisPackets.Add;

        receiver.RawIq+=frame=>Aviation.Feed(frame,new PacketContext($"aviation:{Interlocked.Read(ref session)}",$"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":receiver.ReceiverName),receiver.IsReplay,frame.Channel?.Frequency??frame.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

        receiver.ChannelChanged+=()=>Aviation.Stop("Receiver configuration changed; start aviation again");

        receiver.ReplayGap+=()=>Aviation.Stop("Replay gap; start aviation again");

        receiver.Ended+=_=>Aviation.Finish();

        Television.CurrentCapture=()=>rfMode && !receiver.ReplayPaused && !receiver.ReplayFinished && receiver.SamplingMode==0 && receiver.AppliedChannel is {} ch ? new TvCapture(receiver.AppliedFrequency,receiver.AppliedRate,receiver.AppliedBandwidth,ch.Frequency):null;

        Television.Tune=LiveTune;

        Television.Active+=active=>{tvActive=active;if(!closing)ListeningChanged(this,new System.Windows.RoutedEventArgs());};

        receiver.RawIq+=Television.Feed;

        receiver.ChannelChanged+=()=>Television.StopLive("Receiver configuration changed; start TV again.");

        receiver.ReplayGap+=()=>Television.StopLive("Replay gap; start TV again.");

        receiver.Ended+=_=>Television.StopLive("Receiver stopped.");

        HdRecordings.Observation+=AnalysisPackets.Add;

        HdRecordings.Listening+=active=>{hdListening=active;if(!closing)ListeningChanged(this,new System.Windows.RoutedEventArgs());};

        HdRecordings.LiveSource=()=>rfMode && !receiver.ReplayPaused && !receiver.ReplayFinished && receiver.SamplingMode==0 && receiver.AppliedRate>=960000 && receiver.AppliedChannel is {Mode:"WFM"} hdChannel && hdChannel.Frequency==receiver.AppliedFrequency ? (receiver.AppliedFrequency,receiver.IsReplay):null;

        receiver.RawIq+=HdRecordings.FeedLive;

        receiver.ChannelChanged+=()=>HdRecordings.StopLive("Receiver settings changed; start HD again to reacquire.");

        receiver.ReplayGap+=()=>HdRecordings.StopLive("Replay seek/gap reset HD.");

        receiver.Ended+=_=>HdRecordings.StopLive("Receiver source ended.");

        Packets.EnabledChanged+=value=>packetDecoder.Enabled=value;

        Packets.ClearRequested+=packetDecoder.Clear;

        P25Packets.P25Changed+=value=>{p25Decoder.Enabled=value;UpdateDataCapture();};

        Packets.SubaudibleChanged+=value=>{subaudibleDecoder.Enabled=value;UpdateDataCapture();RefreshSubaudible();};

        PagingPackets.PagingChanged+=value=>{pagingDecoder.Enabled=value;UpdateDataCapture();};

        PagingPackets.ClearRequested+=pagingDecoder.Clear;

        receiver.ChannelChanged+=pagingDecoder.Reset;

        receiver.ReplayGap+=pagingDecoder.Reset;

        receiver.Ended+=message=>{_ = pagingDecoder.CompleteAsync();};

        engine.PcmBoundary+=pagingDecoder.Reset;

        engine.Ended+=message=>{_ = pagingDecoder.CompleteAsync();};

        Packets.ClearRequested+=subaudibleDecoder.Clear;

        receiver.ChannelChanged+=subaudibleDecoder.Reset;

        receiver.ReplayGap+=subaudibleDecoder.Reset;

        receiver.Ended+=_=>subaudibleDecoder.Reset();

        engine.PcmBoundary+=subaudibleDecoder.Reset;

        engine.Ended+=_=>subaudibleDecoder.Reset();

        P25Packets.ClearRequested+=p25Decoder.Clear;

        receiver.ChannelChanged+=p25Decoder.Reset;

        receiver.ReplayGap+=p25Decoder.Reset;

        receiver.Ended+=_=>p25Decoder.Reset();

        receiver.DataSamples+=samples=>

        {

            if(receiver.AppliedChannel is not {Mode:"NFM"} channel)return;

            voiceIds.Feed(samples,VoiceContext(channel.Frequency),true);
            digitalVoice.Feed(samples,VoiceContext(channel.Frequency));
            Dmr.Feed(samples,new PacketContext($"dmr:{Interlocked.Read(ref session)}:{channel.Frequency}",$"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":"SDR discriminator"),receiver.IsReplay,channel.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

            pagingDecoder.Feed(samples,new PacketContext($"paging:{Interlocked.Read(ref session)}:{channel.Frequency}",

                $"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":"SDR discriminator"),receiver.IsReplay,channel.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

            p25Decoder.Feed(samples,new PacketContext($"p25:{Interlocked.Read(ref session)}:{channel.Frequency}",

                $"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":"SDR discriminator"),receiver.IsReplay,channel.Frequency,

                receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

            subaudibleDecoder.FeedDiscriminator(samples,new PacketContext($"sub:{Interlocked.Read(ref session)}:{channel.Frequency}",

                $"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":"SDR discriminator"),receiver.IsReplay,channel.Frequency,

                receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

        };

        engine.PcmBoundary+=packetDecoder.Reset;

        engine.PcmReady+=(bytes,count,channels,position)=>

        {

            if(rfMode)return;

            var context=new PacketContext($"audio:{Interlocked.Read(ref session)}",$"Receiver {workspaceSlot} · {packetAudioSource}",packetAudioReplay,null,position);

            packetDecoder.Feed(bytes.AsSpan(0,count),channels,context);

            subaudibleDecoder.FeedPcm(bytes.AsSpan(0,count),channels,context);

            pagingDecoder.FeedPcm(bytes.AsSpan(0,count),channels,context);

        };

        receiver.Audio+=pcm=>

        {

            if(!packetDecoder.Enabled || receiver.AppliedChannel is not { Mode:"NFM" } channel)return;

            packetDecoder.Feed(pcm,1,new PacketContext($"rf:{Interlocked.Read(ref session)}:{channel.Frequency}",$"Receiver {workspaceSlot} · "+(receiver.IsReplay?"I/Q recording":receiver.ReceiverName+" NFM"),receiver.IsReplay,channel.Frequency,receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null));

        };

    }

    private void RefreshPackets()

    {
        RefreshDigitalVoice();

        Aviation.Refresh();
        Packets.SetVoiceStatus(voiceIds.Status);P25Packets.SetVoiceStatus(voiceIds.Status);foreach(var row in voiceIds.Drain())AnalysisPackets.Add(row);

        var rds=receiver.Rds;

        if(rfMode && receiver.AppliedChannel is {Mode:"WFM"} fm && rds?.Pi is {} pi && rds.AgeSeconds<=10)

        {

            string protocol=RdsRegion.SelectedIndex==0?"RBDS":"RDS";

            string identity=$"{session}:{fm.Frequency}:{protocol}:{pi}:{rds.Pty}:{rds.Ps}:{rds.Text}";

            if(identity!=lastRdsObservation)

            {

                lastRdsObservation=identity;

                AnalysisPackets.Add(new PacketObservation(DateTimeOffset.UtcNow,$"Receiver {workspaceSlot} · WFM",receiver.IsReplay,fm.Frequency,

                    receiver.IsReplay?receiver.ReplayPosition.TotalSeconds:null,"Decoded RDS groups",rds.Ps??"","","","Station metadata",

                    $"PI {pi:X4} · PTY {rds.Pty} · {rds.Ps} · {rds.Text}",null,"",protocol,true));

            }

        }

        else lastRdsObservation=null;

        Dmr.Refresh(rfMode&&receiver.AppliedChannel?.Mode=="NFM");

        foreach(var row in pagingDecoder.Drain())PagingPackets.Add(row);

        PagingPackets.SetPagingStatus(pagingDecoder.Enabled&&rfMode&&receiver.AppliedChannel?.Mode!="NFM"?"Paging: select NFM; start with 16–25 kHz receive filter":pagingDecoder.Status);

        foreach(var row in packetDecoder.Drain())Packets.Add(row);

        foreach(var row in p25Decoder.Drain()){P25Packets.Add(row);P25Systems.Observe(row);siteCollector?.Observe(row);P25Activity.Observe(row);}

        P25Systems.Tick();P25Activity.Tick();

        subaudibleDecoder.Drain(); // Tone status belongs above the spectrum.

        RefreshSubaudible();

        var p25=p25Decoder.Counts;

        string p25Input=!p25Decoder.Enabled?"P25 off":receiver.IsReplay&&receiver.ReplayFinished?"P25 replay finished":receiver.IsReplay&&receiver.ReplayPaused?"P25 replay paused":!rfMode||!engine.Active?"P25 waiting for SDR or I/Q replay":receiver.AppliedChannel?.Mode!="NFM"?"P25: select NFM":"P25 C4FM searching / receiving";

        P25Packets.SetP25Status($"{p25Input} · verified headers {p25.Headers} · CRC messages {p25.Messages}"+(p25.Dropped>0?$" · queue overflow {p25.Dropped}":""));

        string input=!packetDecoder.Enabled?"Decoder off":!engine.Active?"Waiting for a source":rfMode && receiver.AppliedChannel?.Mode!="NFM"?"Select NFM for SDR packet reception":rfMode?receiver.IsReplay?"Decoding NFM I/Q replay":"Decoding NFM SDR audio":$"Decoding {packetAudioSource}";

        if(rfMode && receiver.AppliedChannel?.Mode=="WFM")input="RDS/RBDS metadata active · AX.25 requires NFM/audio";

        Packets.SetStatus(input,packetDecoder.Counts);

    }

    private async Task RenderPacketSmoke(Action<string> capture)

    {

        Stop();packetDecoder.Clear();Packets.ClearRows();Packets.DecoderEnabled=true;

        var frames=new[]{PacketTests.Frame("!4903.50N/07201.75W>Simulated APRS position"),PacketTests.Frame(":N0TEST   :Receive-only demo{42"),PacketTests.Frame(">Synthetic station status")};

        var bad=(byte[])frames[0].Clone();bad[^3]^=1;

        byte[] audio=PacketTests.Audio(frames.Append(bad));

        string wav=System.IO.Path.Combine(dataFolder,"packet-smoke.wav"),iq=System.IO.Path.Combine(dataFolder,"packet-smoke.mrsiq");

        using(var writer=new NAudio.Wave.WaveFileWriter(wav,AudioEngine.Format))writer.Write(audio,0,audio.Length);

        PlayFile(wav,true);SelectWorkspace(PacketsTab);

        for(int n=0;n<500 && (packetDecoder.Counts.Good<3 || packetDecoder.Counts.Bad<1);n++)await Task.Delay(20);

        await Task.Delay(100);RefreshPackets();

        if(packetDecoder.Counts.Good!=3 || packetDecoder.Counts.Bad<1 || Packets.VisibleCount!=3)throw new InvalidOperationException($"Packet WAV replay counts: {packetDecoder.Counts}; visible {Packets.VisibleCount}.");

        UpdateLayout();capture("ui-packets-wav.png");

        Packets.SetFilter("N0TEST");

        using(var json=System.Text.Json.JsonDocument.Parse(Packets.ExportJson()))

            if(json.RootElement.GetArrayLength()!=1 || !json.RootElement[0].GetProperty("Replay").GetBoolean())throw new InvalidOperationException("Packet filter/export lost replay attribution.");

        Packets.SetFilter("");Packets.IncludeFailed(true);

        if(Packets.VisibleCount<4)throw new InvalidOperationException("Failed FCS inspection was unavailable.");

        Packets.IncludeFailed(false);

        Stop();packetDecoder.Clear();Packets.ClearRows();PacketTests.WriteIq(iq,PacketTests.Iq(audio,240000));

        PlayIq(iq);SelectWorkspace(PacketsTab);SidebarTabs.SelectedIndex=0;

        for(int n=0;n<500 && packetDecoder.Counts.Good<3;n++)await Task.Delay(20);

        RefreshPackets();UpdateLayout();capture("ui-packets-iq.png");

        if(packetDecoder.Counts.Good!=3)throw new InvalidOperationException("Packet I/Q replay failed NFM decoding.");

        using(var json=System.Text.Json.JsonDocument.Parse(Packets.ExportJson()))

            if(json.RootElement[0].GetProperty("FrequencyHz").GetInt64()!=144390000)throw new InvalidOperationException("Packet replay lost channel frequency.");

        Stop();Packets.DecoderEnabled=false;WorkspaceTabs.SelectedIndex=0;

        var a=new RtlNative.Device(0,"RTL-SDR Blog V3 (synthetic)","RTLSDRBlog","Blog V3","MRS-V3-A");var b=a with{Index=1,Serial="MRS-V3-B"};

        RtlDevices.ItemsSource=new[]{a,b};RtlDevices.SelectedItem=a;SamplingModeBox.SelectedIndex=2;

        RtlDevices.SelectedItem=b;SamplingModeBox.SelectedIndex=0;

        RtlDevices.ItemsSource=new[]{b with{Index=0},a with{Index=1}};RtlDevices.SelectedIndex=1;

        if(SelectedSamplingMode!=2 || !ReceiverIdentityStatus.Text.Contains("MRS-V3-A"))throw new InvalidOperationException("V3 sampling preference did not follow receiver identity after index changes.");

        RtlDevices.SelectedIndex=0;if(SelectedSamplingMode!=0)throw new InvalidOperationException("V3 selection inherited the other receiver's sampling mode.");

        ReceiverScroll.ScrollToTop();UpdateLayout();capture("ui-v3-selection.png");

    }

}





