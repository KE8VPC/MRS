using System.Buffers.Binary;

namespace MRS;
internal static class SubaudibleTests
{
    internal static double[] Tone(double hz,double seconds=1.8,double amplitude=.12,double noise=0)
    {
        var random=new Random(625);return Enumerable.Range(0,(int)(48000*seconds)).Select(n=>amplitude*Math.Sin(2*Math.PI*hz*n/48000)+noise*(random.NextDouble()*2-1)).ToArray();
    }
    // Independent word constants below come from the CML table, not the production encoder.
    internal static double[] Dcs(uint word,double seconds=1.8,double baud=134.4,double amplitude=.12,double noise=0,double offset=0)
    {
        var random=new Random(1344);return Enumerable.Range(0,(int)(48000*seconds)).Select(n=>(((word>>((int)(n*baud/48000+.37)%23))&1)==1?amplitude:-amplitude)+offset+noise*(random.NextDouble()*2-1)).ToArray();
    }
    internal static byte[] Pcm(double[] signal,int channels=1)
    {
        byte[] bytes=new byte[signal.Length*2*channels];
        for(int n=0;n<signal.Length;n++)for(int ch=0;ch<channels;ch++)BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan((n*channels+ch)*2,2),(short)Math.Clamp(Math.Round(signal[n]*32767),short.MinValue,short.MaxValue));
        return bytes;
    }
    private static void Feed(SubaudibleDecoder decoder,double[] signal)
    {
        for(int n=0;n<signal.Length;n+=173)decoder.Process(signal.AsSpan(n,Math.Min(173,signal.Length-n)));
    }
    internal static void Run(Action<bool,string> check)
    {
        check(SubaudibleDecoder.Tones.Length==51&&SubaudibleDecoder.Codes.Length==104,"Subaudible catalog contains 50 common CTCSS tones plus 62.5 Hz and 104 octal DCS codes");
        foreach(var pair in new[]{("023",0x763813u),("025",0x6B7815u),("047",0x0FD827u),("134",0x2ED85Cu),("365",0x2F08F5u),("712",0x0BD9CAu)})
            check(SubaudibleDecoder.DcsWord(pair.Item1)==pair.Item2,$"DCS {pair.Item1} encoding matches independent CML Table 5 word {pair.Item2:X6}");
        foreach(double tone in SubaudibleDecoder.Tones)
        {
            var decoder=new SubaudibleDecoder();Feed(decoder,Tone(tone+.12,noise:.02));
            check(decoder.Current?.Protocol=="CTCSS"&&decoder.Current.Value==tone.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" Hz",$"CTCSS {tone:F1} Hz tolerates noise, frequency offset and arbitrary buffer boundaries");
        }
        foreach(string code in SubaudibleDecoder.Codes)
        {
            bool valid=true;
            for(int polarity=0;polarity<2;polarity++)
            {
                var decoder=new SubaudibleDecoder();Feed(decoder,Dcs(SubaudibleDecoder.DcsWord(code)^(polarity==0?0:0x7FFFFFu)));
                valid&=decoder.Current?.Protocol=="DCS"&&decoder.Current.Value.Contains(code+(polarity==0?"N":"I"));
            }
            check(valid,$"DCS {code} is detected in both polarities with equivalent aliases retained");
        }
        foreach(double baud in new[]{133.8,134.4,135.0})
        {
            var decoder=new SubaudibleDecoder();Feed(decoder,Dcs(0x763813,baud:baud,noise:.04,offset:.03));
            check(decoder.Current?.Value=="023N / 047I",$"Independent DCS 023N / 047I waveform survives {baud:F1} bit/s, DC and noise");
        }
        foreach(var scenario in new[]{("silence",new double[96000]),("DC",Enumerable.Repeat(.1,96000).ToArray()),("60 Hz hum",Tone(60)),("off-list 70.6 Hz",Tone(70.6)),("DCS turn-off tone",Tone(134.4)),("corrupted DCS",Dcs(0x763813^1)),("audio-only 1 kHz",Tone(1000)),("alias at 2500 Hz",Tone(2500)),("noise",Tone(0,seconds:3,amplitude:0,noise:.3))})
        {
            var decoder=new SubaudibleDecoder();var acquired=new List<SubaudibleDetection>();decoder.Changed+=(_,next)=>{if(next!=null)acquired.Add(next);};
            Feed(decoder,scenario.Item2);check(acquired.Count==0,$"Subaudible detector rejects {scenario.Item1} without inventing a tone or code");
        }
        var transition=new SubaudibleDecoder();var events=new List<(SubaudibleDetection?,SubaudibleDetection?)>();transition.Changed+=(a,b)=>events.Add((a,b));
        Feed(transition,Tone(100,seconds:.4));check(transition.Current==null,"Short tone burst is not promoted to a stable CTCSS result");
        Feed(transition,Tone(100,seconds:2));Feed(transition,Tone(100,seconds:2));
        check(events.Count==1&&transition.Current?.Value=="100.0 Hz","Continuous CTCSS produces one acquisition, not repeated history spam");
        Feed(transition,Tone(123,seconds:2));check(transition.Current?.Value=="123.0 Hz","A new CTCSS tone replaces the prior live result");
        Feed(transition,new double[96000]);check(transition.Current==null&&events.Last().Item2==null,"Tone loss clears the live indication");
        var digitalVoice=new SubaudibleDecoder();int falseTones=0;digitalVoice.Changed+=(_,b)=>{if(b!=null)falseTones++;};
        Feed(digitalVoice,P25Tests.Signal(20).Select(x=>x/5000).ToArray());check(falseTones==0,"P25 C4FM fixture is not misidentified as subaudible signaling");
        var interrupted=new SubaudibleDecoder();Feed(interrupted,Dcs(0x763813,seconds:.34));Feed(interrupted,new double[96000]);
        check(interrupted.Current==null,"Two DCS repetitions are insufficient for a stable code");
        var mixed=new SubaudibleDecoder();var speech=Tone(151.4,seconds:2);
        for(int n=0;n<speech.Length;n++)speech[n]+=.35*Math.Sin(2*Math.PI*700*n/48000)+.2*Math.Sin(2*Math.PI*1700*n/48000);
        Feed(mixed,speech);check(mixed.Current?.Value=="151.4 Hz","CTCSS detection survives stronger voice-band components");
        var context=new PacketContext("one","Synthetic subaudible",true,446000000,2.5);
        var pipeline=new SubaudiblePipeline{Enabled=true};pipeline.FeedDiscriminator(Tone(100).Select(x=>x*5000).ToArray(),context);
        var rows=pipeline.Drain();
        check(rows.Length==1&&rows[0].Protocol=="CTCSS"&&rows[0].Replay&&rows[0].FrequencyHz==446000000&&rows[0].PositionSeconds==2.5,"Subaudible Data observations retain protocol, frequency, replay and source position");
        pipeline.FeedDiscriminator(new double[200],context with{Key="two",FrequencyHz=146520000});check(pipeline.Current==null,"Source/retune boundary clears the previous subaudible detection");
        pipeline.Reset();byte[] pcm=Pcm(Dcs(0x2ED85C),channels:2);
        for(int n=0;n<pcm.Length;n+=692)pipeline.FeedPcm(pcm.AsSpan(n,Math.Min(692,pcm.Length-n)),2,context);
        check(pipeline.Current?.Value=="134N / 223I","Stereo PCM path decodes an independent DCS word across small blocks");
        pipeline.Reset();check(pipeline.Current==null,"Explicit stop/seek reset clears the live subaudible state");
        var independent=new SubaudiblePipeline{Enabled=true};independent.FeedPcm(Pcm(Tone(123)),1,context);
        pipeline.FeedPcm(Pcm(Tone(100)),1,context);pipeline.Reset();
        check(independent.Current?.Value=="123.0 Hz","Resetting one receiver does not clear the other receiver's tone");
        pipeline.Enabled=false;pipeline.Clear();pipeline.FeedPcm(pcm,2,context);check(pipeline.Current==null&&pipeline.Drain().Length==0,"Disabled subaudible decoder emits no observations");
        foreach(var fixture in new[]{(Tone(100),"100.0 Hz"),(Dcs(0x0BD9CA),"114I / 712N")})
        {
            pipeline=new(){Enabled=true};var dsp=new ReceiveDemodulator("NFM",240000,bandwidth:12500){CaptureData=true};byte[] iq=PacketTests.Iq(Pcm(fixture.Item1),240000);
            for(int n=0;n<iq.Length;n+=8192){dsp.Process(iq.AsSpan(n,Math.Min(8192,iq.Length-n)).ToArray());pipeline.FeedDiscriminator(dsp.DataSamples,context);}
            check(pipeline.Current?.Value==fixture.Item2,$"Production NFM I/Q discriminator preserves {fixture.Item2}");
        }
    }
}
