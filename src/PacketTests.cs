using System.Text;
namespace MRS;
internal static class PacketTests
{
    internal static byte[] Frame(string information, byte control=3, byte pid=0xf0)
    {
        var bytes=new List<byte>();
        void Address(string name,int ssid,bool last,bool repeated=false)
        { foreach(char c in name.PadRight(6))bytes.Add((byte)(c<<1));bytes.Add((byte)(0x60|(ssid<<1)|(last?1:0)|(repeated?128:0))); }
        Address("APMRS",0,false);Address("N0CALL",7,false);Address("WIDE1",1,false,true);Address("WIDE2",1,true);
        bytes.Add(control);bytes.Add(pid);bytes.AddRange(Encoding.ASCII.GetBytes(information));
        // Independent CCITT polynomial division in forward bit order, then reverse/complement.
        ushort crc=0xffff;
        foreach(byte b in bytes)for(int n=0;n<8;n++){bool feedback=((crc&0x8000)!=0)^((b&(1<<n))!=0);crc=(ushort)(crc<<1);if(feedback)crc^=0x1021;}
        ushort fcs=0;for(int n=0;n<16;n++)fcs=(ushort)((fcs<<1)|((crc>>n)&1));fcs=(ushort)~fcs;
        bytes.Add((byte)fcs);bytes.Add((byte)(fcs>>8));return bytes.ToArray();
    }
    internal static List<bool> Bits(IEnumerable<byte[]> frames)
    {
        var bits=new List<bool>();
        void Flag(){for(int n=0;n<8;n++)bits.Add((0x7e&(1<<n))!=0);}
        for(int n=0;n<32;n++)Flag();
        foreach(var frame in frames)
        {
            int ones=0;
            foreach(byte b in frame)for(int n=0;n<8;n++){bool bit=(b&(1<<n))!=0;bits.Add(bit);ones=bit?ones+1:0;if(ones==5){bits.Add(false);ones=0;}}
            Flag();Flag();
        }
        for(int n=0;n<8;n++)Flag();return bits;
    }
    internal static byte[] Audio(IEnumerable<byte[]> frames,double offset=0,double clockError=0,double noise=0,double amplitude=.6,int leading=137)
    {
        var bits=Bits(frames); var tones=new bool[bits.Count];bool tone=false;
        for(int n=0;n<bits.Count;n++){if(!bits[n])tone=!tone;tones[n]=tone;}
        int count=(int)Math.Ceiling(bits.Count*40/(1+clockError)); var bytes=new byte[(count+leading+200)*2];
        double phase=.723;var random=new Random(715);
        for(int n=0;n<count;n++)
        {
            int bit=Math.Min(tones.Length-1,(int)(n*(1+clockError)/40));double frequency=(tones[bit]?1200:2200)+offset;
            phase=(phase+2*Math.PI*frequency/48000)%(2*Math.PI);
            short sample=(short)Math.Clamp((amplitude*Math.Sin(phase)+(random.NextDouble()*2-1)*noise+.025)*32767,short.MinValue,short.MaxValue);
            bytes[(n+leading)*2]=(byte)sample;bytes[(n+leading)*2+1]=(byte)(sample>>8);
        }
        return bytes;
    }
    internal static void Run(Action<bool,string> check)
    {
        check(Ax25Parser.Fcs(Encoding.ASCII.GetBytes("123456789"))==0x906e,"AX.25 FCS matches the independent CRC-16/X-25 check vector");
        byte[] frame=Frame("!4903.50N/07201.75W>Packet test");var parsed=Ax25Parser.Parse(frame);
        check(parsed.FcsValid && parsed.Source=="N0CALL-7" && parsed.Destination=="APMRS" && parsed.Path=="WIDE1-1*,WIDE2-1","AX.25 UI preserves callsigns, SSIDs and repeated digipeater flags");
        check(parsed.Aprs?.Latitude is double lat && Math.Abs(lat-49.0583333)<.000001 && parsed.Aprs.Longitude is double lon && Math.Abs(lon+72.0291667)<.000001,"APRS uncompressed position converts degrees/minutes with correct hemisphere");
        var bad=(byte[])frame.Clone();bad[^3]^=1;var invalid=Ax25Parser.Parse(bad);
        check(!invalid.FcsValid && invalid.Aprs==null && invalid.Source=="","Bad FCS never produces trusted callsigns or APRS coordinates");
        var hdlc=new HdlcReceiver();var received=new List<byte[]>();hdlc.Frame+=received.Add;
        foreach(bool bit in Bits([frame,frame]))hdlc.Push(bit);
        check(received.Count==2 && received.All(r=>r.SequenceEqual(frame)),"HDLC flags, bit stuffing and repeated frames round-trip independently encoded data");
        foreach(var scenario in new[]{(0.0,0.0,0.0,.6,137),(30.0,.003,.06,.45,19),(-30.0,-.003,.04,.4,31),(0.0,0.0,0.0,.025,7)})
        {
            byte[] audio=Audio([frame,Frame(":N0TEST   :Hello from MRS{42"),Frame(">Receive-only test")],scenario.Item1,scenario.Item2,scenario.Item3,scenario.Item4,scenario.Item5);
            var modem=new Afsk1200();var packets=new List<Ax25Packet>();modem.Frame+=(raw,_)=>{var p=Ax25Parser.Parse(raw);if(p.FcsValid)packets.Add(p);};
            for(int n=0;n<audio.Length;n+=638)modem.Process(audio.AsSpan(n,Math.Min(638,audio.Length-n)));
            check(packets.Count==3 && packets[0].Raw.SequenceEqual(frame),$"AFSK1200 decodes three packets with offset {scenario.Item1} Hz, clock {scenario.Item2:P1}, amplitude {scenario.Item4}, noise {scenario.Item3}");
            check(packets[1].Aprs?.Addressee=="N0TEST" && packets[1].Aprs?.MessageId=="42" && packets[2].Aprs?.Type=="Status","APRS message addressee/ID and status are interpreted without transmission");
        }
        check(Ax25Parser.Parse(Frame("!9963.50N/19201.75W>bad")).Aprs?.Latitude==null,"Out-of-range APRS coordinates remain unlocated");
        check(Ax25Parser.Parse(Frame("!4903.  N/07201.  W>ambiguous")).Aprs?.Latitude==null,"Ambiguous APRS position is not represented as an exact location");
        var zeros=new Afsk1200();int falseFrames=0;zeros.Frame+=(raw,_)=>{if(Ax25Parser.Parse(raw).FcsValid)falseFrames++;};zeros.Process(new byte[96000]);
        check(falseFrames==0,"AFSK silence produces no validated packets");
        var timestamped=Ax25Parser.Parse(Frame("@291230z4903.50N/07201.75W>Timestamped"));
        check(timestamped.Aprs?.Latitude!=null && Ax25Parser.Parse(Frame("@999999z4903.50N/07201.75W>Bad timestamp")).Aprs?.Latitude==null,"APRS timestamped position accepts valid day/time and rejects invalid time");
        check(Ax25Parser.Parse(Frame(":N0TEST   :ack42")).Aprs?.MessageId=="42" && Ax25Parser.Parse(Frame(":N0TEST   :acknowledge this")).Aprs?.Type=="Message","APRS acknowledgment syntax does not misclassify normal message text");
        check(Ax25Parser.Parse(Frame("ordinary packet text")).Aprs==null && Ax25Parser.Parse(Frame("!4903.50N/07201.75W>",pid:0xcf)).Aprs==null,"Non-APRS UI data and other PIDs are retained without inferred APRS fields");
        var abort=new HdlcReceiver();int abortFrames=0;abort.Frame+=_=>abortFrames++;
        foreach(bool bit in Bits([frame]).Take(32*8+50))abort.Push(bit);for(int n=0;n<8;n++)abort.Push(true);
        foreach(bool bit in Bits([frame]))abort.Push(bit);
        check(abortFrames==1,"HDLC abort discards the partial frame and reacquires a subsequent packet");
        for(int n=0;n<30000;n++)abort.Push(false);foreach(bool bit in Bits([frame]))abort.Push(bit);
        check(abortFrames==2,"Oversized unterminated HDLC input remains bounded and later reacquires");
        var random=new Random(811);var noiseBytes=new byte[96000];random.NextBytes(noiseBytes);zeros.Process(noiseBytes);
        check(falseFrames==0,"Broadband noise produces no validated AX.25 packets");
        byte[] packetAudio=Audio([frame]);
        foreach(int rate in ReceiverStream.Rates)
        {
            var dsp=new ReceiveDemodulator("NFM",rate);var modem=new Afsk1200();int count=0;
            modem.Frame+=(raw,_)=>{if(raw.SequenceEqual(frame))count++;};
            byte[] iq=Iq(packetAudio,rate);
            for(int n=0;n<iq.Length;n+=16384)modem.Process(dsp.Process(iq.AsSpan(n,Math.Min(16384,iq.Length-n)).ToArray()));
            check(count==1,$"NFM I/Q at {rate} S/s recovers the complete AX.25 packet");
        }
        var pipeline=new PacketPipeline{Enabled=true};var context=new PacketContext("test","Fixture",true,144390000,0);
        pipeline.Feed(Audio([frame,bad]),1,context);var observations=pipeline.Drain();
        check(observations.Count(p=>p.Validation=="FCS valid")==1 && pipeline.Counts.Bad>=1 && observations.Any(p=>p.Callsign=="N0CALL-7" && p.Replay && p.PositionSeconds>0),"Packet pipeline labels replay/frequency/position and counts failed FCS separately");
        pipeline.Clear();pipeline.Feed(packetAudio.AsSpan(0,packetAudio.Length/4*2),1,context);pipeline.Reset();pipeline.Feed(packetAudio.AsSpan(packetAudio.Length/4*2),1,context);
        check(pipeline.Counts.Good==0,"A source discontinuity cannot combine two halves into a packet");
        pipeline.Clear();pipeline.Enabled=false;pipeline.Feed(packetAudio,1,context);
        check(pipeline.Counts.Good==0,"Disabled decoder consumes no packets");
        pipeline.Enabled=true;
        using(var engine=new AudioEngine())
        {
            engine.PcmReady+=(bytes,count,channels,position)=>pipeline.Feed(bytes.AsSpan(0,count),channels,context with{PositionSeconds=position});
            engine.StartExternal();engine.Volume=0;engine.Squelch=0;engine.FeedExternal(packetAudio,false);
            check(pipeline.Counts.Good==1,"Packet decoding receives audio with volume zero and the listening gate closed");
        }
        var v3a=new RtlNative.Device(0,"RTL-SDR Blog V3","RTLSDRBlog","Blog V3","MRS-V3-A");var v3b=v3a with{Index=1,Serial="MRS-V3-B"};
        string? identity=ReceiverIdentity.UniqueKey([v3a,v3b],v3a);
        check(identity!=null && ReceiverIdentity.Match([v3b with{Index=0},v3a with{Index=1}],identity)?.Serial=="MRS-V3-A","Two synthetic V3 identities retain selection when enumeration order changes");
        check(ReceiverIdentity.UniqueKey([v3a,v3a with{Index=1}],v3a)==null && ReceiverIdentity.Key(v3a with{Serial="00000001"})==null,"Duplicate or generic V3 serials cannot authorize automatic restoration/reconnect");
        check(v3a.DisplayName!=v3b.DisplayName && v3a.DisplayName.Contains(v3a.Serial),"Receiver labels distinguish indexes and reported serials");
        var malformed=(byte[])frame.Clone();malformed[0]|=1;ushort updated=Ax25Parser.Fcs(malformed.AsSpan(0,malformed.Length-2));malformed[^2]=(byte)updated;malformed[^1]=(byte)(updated>>8);
        var malformedPacket=Ax25Parser.Parse(malformed);
        check(malformedPacket.FcsValid && malformedPacket.Source=="" && malformedPacket.Aprs==null,"Valid FCS does not bypass address syntax validation");
        var noFlags=new HdlcReceiver();int truncated=0;noFlags.Frame+=_=>truncated++;
        foreach(bool bit in Bits([frame]).Take(32*8+20))noFlags.Push(bit);
        check(truncated==0,"An incomplete packet is never published before its closing flag");
        var continuous=new Afsk1200();var splitModem=new Afsk1200();var wholeFrames=new List<string>();var splitFrames=new List<string>();
        continuous.Frame+=(raw,_)=>wholeFrames.Add(Convert.ToHexString(raw));splitModem.Frame+=(raw,_)=>splitFrames.Add(Convert.ToHexString(raw));
        continuous.Process(packetAudio);for(int n=0;n<packetAudio.Length;n+=2)splitModem.Process(packetAudio.AsSpan(n,2));
        check(wholeFrames.SequenceEqual(splitFrames) && wholeFrames.Count==1,"AFSK frame recovery is identical across single-sample and whole-buffer input");
        var stereoModem=new Afsk1200();int stereoCount=0;stereoModem.Frame+=(raw,_)=>{if(raw.SequenceEqual(frame))stereoCount++;};stereoModem.Process(AudioEngine.DuplicateMono(packetAudio),2);
        check(stereoCount==1,"Duplicated stereo packet audio decodes once");
        var queue=new PacketPipeline{Enabled=true};
        for(int n=0;n<260;n++)queue.Feed(packetAudio,1,context);
        var retained=queue.Drain(1000);
        check(queue.Counts.Good==260 && retained.Length==256 && queue.Counts.Dropped==4,"Packet queue remains bounded and reports overflow instead of growing without limit");
        var panel=new PacketPanel();foreach(var row in retained)panel.Add(row);foreach(var row in retained)panel.Add(row);
        check(panel.Count==500,"Packet history retains at most 500 observations");
        panel.SetFilter("does-not-match");
        check(panel.VisibleCount==0 && panel.ExportJson()=="[]","Packet export respects the active filter");
        panel.ClearRows();check(panel.Count==0,"Clearing packet history removes prior observations");
        var fuzz=new Random(422);bool safe=true;
        for(int n=0;n<200;n++)
        {
            byte[] data=new byte[fuzz.Next(17,200)];fuzz.NextBytes(data);ushort crc=Ax25Parser.Fcs(data.AsSpan(0,data.Length-2));data[^2]=(byte)crc;data[^1]=(byte)(crc>>8);
            try{Ax25Parser.Parse(data);}catch{safe=false;break;}
        }
        check(safe,"Malformed frames with valid checksums are bounded and do not crash the parser");
    }
    internal static byte[] Iq(byte[] audio,int rate)
    {
        int factor=rate/48000;var iq=new byte[audio.Length*factor];double phase=0;
        for(int n=0;n<iq.Length/2;n++)
        {
            int sample=n/factor;double fraction=(n%factor)/(double)factor;
            double a=BitConverter.ToInt16(audio,sample*2)/32768.0,b=BitConverter.ToInt16(audio,Math.Min(sample+1,audio.Length/2-1)*2)/32768.0;
            phase=(phase+2*Math.PI*3000*(a+(b-a)*fraction)/rate)%(2*Math.PI);
            iq[n*2]=(byte)Math.Clamp(Math.Round(127.5+90*Math.Cos(phase)),0,255);iq[n*2+1]=(byte)Math.Clamp(Math.Round(127.5+90*Math.Sin(phase)),0,255);
        }
        return iq;
    }
    internal static void WriteIq(string path,byte[] iq,int rate=240000,long frequency=144390000)
    {
        using var file=System.IO.File.Create(path);using var writer=new System.IO.BinaryWriter(file);
        var json=System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new IqMetadata(3,DateTimeOffset.UtcNow,false,-1000,null,"NFM"));
        writer.Write(IqRecording.Magic);writer.Write(json.Length);writer.Write(json);
        for(int n=0;n<iq.Length;n+=16384)
        {
            int count=Math.Min(16384,iq.Length-n);writer.Write((long)(n/(rate*2.0)*TimeSpan.TicksPerSecond));writer.Write(count);writer.Write(frequency);writer.Write(rate);writer.Write((double)rate);writer.Write(0);
            writer.Write(frequency);writer.Write(0);writer.Write(16000.0);writer.Write((byte)1);writer.Write(-1000);writer.Write(-1000);writer.Write(iq,n,count);
        }
        writer.Write(-1L);
    }
}
