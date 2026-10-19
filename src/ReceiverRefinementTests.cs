namespace MRS;
internal static class ReceiverRefinementTests
{
    internal static void Run(Action<bool,string> check)
    {
        check(ReceiveDemodulator.Modes.Take(5).SequenceEqual(new[]{"NFM","WFM","AM","USB","LSB"})&&ReceiveDemodulator.Modes[5]=="CW","CW appends mode index without changing legacy I/Q recording modes");
        foreach(var pair in new[]{(1000000L,"AM"),(100100000L,"WFM"),(121500000L,"AM"),(156800000L,"NFM"),(162400000L,"NFM")})
            check(BandModeDefaults.Apply(new(pair.Item1,"USB")).Mode==pair.Item2,$"Optional band defaults choose {pair.Item2} at {pair.Item1}");
        check(BandModeDefaults.Apply(new(146520000,"USB",4000)).Mode=="USB"&&BandModeDefaults.Apply(new(162025000,"USB")).Mode=="USB","Amateur and marine AIS frequencies do not force an analog voice mode");
        check(new DisplaySettings{Zoom=4096}.Normalize().Zoom==4096&&new FrequencyViewport(0,20000000,1,.5).ZoomAt(8192,.5).Zoom==4096,"Tight zoom supports 4096x and clamps larger requests");
        var view=new SpectrumView();view.SetRf(1000000,960000);
        var frame=Enumerable.Repeat(-90f,4096).ToArray(); frame[2048]=-20;
        view.PushAt(frame,10); view.SetRf(1240000,960000);
        var row=view.HistoryRows.Single();
        check(row.CaptureStart==520000&&row.CaptureSpan==960000&&view.HistorySample(row,.25,0)==-20,"Retuning preserves history and shifts the old carrier to its actual RF coordinate");
        check(float.IsNaN(view.HistorySample(row,.99,.001)),"Newly exposed waterfall frequencies are blank rather than stretched edge samples");
        view.PushAt(Enumerable.Repeat(-80f,16384).ToArray(),10.1);
        check(view.HistoryRows.Length==2&&view.HistoryRows[0].Levels.Length==4096,"History retains each row's own FFT size after resolution change");
        view.SetAudio();check(view.HistoryRows.Length==0,"Changing from RF to audio clears incompatible history");
        var stopped=new ActivityScanner();var ch=new ChannelSettings(146400000,"NFM",12500);stopped.Reset(146500000,960000,960000,4096,ch);
        for(int i=0;i<25;i++)stopped.Process(ActivityTests.Frame(4096,960000,146500000,i),i*.15,DateTimeOffset.UtcNow,ch,new(AutoSquelch:true,MarginDb:8),-60);
        check(stopped.Ready&&!stopped.Running&&Math.Abs(stopped.ChannelThresholdDb-stopped.ChannelFloorDb-8)<.01,"Receiver auto squelch estimates floor plus margin with scanning stopped");
        static byte[] Carrier(double hz)
        {
            var iq=new byte[120000];for(int n=0;n<iq.Length/2;n++){double phase=2*Math.PI*hz*n/240000;iq[n*2]=(byte)Math.Round(127.5+50*Math.Cos(phase));iq[n*2+1]=(byte)Math.Round(127.5+50*Math.Sin(phase));}return iq;
        }
        static double Tone(byte[] pcm,double hz)
        {
            double a=0,b=0;int count=pcm.Length/2;for(int n=3000;n<count;n++){double v=BitConverter.ToInt16(pcm,n*2)/32768.0;a+=v*Math.Cos(2*Math.PI*hz*n/48000);b+=v*Math.Sin(2*Math.PI*hz*n/48000);}return Math.Sqrt(a*a+b*b)/(count-3000);
        }
        foreach(double offset in new[]{0.0,25000,-25000})
        {
            var iq=Carrier(offset);var dsp=new ReceiveDemodulator("CW",240000,offset,500,false){CwPitchHz=700};var pcm=dsp.Process(iq);
            check(pcm.Length==24000&&Tone(pcm,700)>.1&&Tone(pcm,700)>20*Tone(pcm,1200),$"CW produces a 700 Hz listening tone for carrier at offset {offset}");
            var split=new ReceiveDemodulator("CW",240000,offset,500,false);var parts=split.Process(iq[..16382]).Concat(split.Process(iq[16382..])).ToArray();
            check(parts.SequenceEqual(pcm),$"CW oscillator and narrow filter remain continuous across blocks at offset {offset}");
        }
        var pitch=new ReceiveDemodulator("CW",240000,0,250,false){CwPitchHz=1000};check(Tone(pitch.Process(Carrier(0)),1000)>.1,"CW pitch adjusts independently of carrier tuning");
        var wanted=new ReceiveDemodulator("CW",240000,0,500,false);var adjacent=new ReceiveDemodulator("CW",240000,0,500,false);
        wanted.Process(Carrier(0));adjacent.Process(Carrier(1500));wanted.Process(Carrier(0));adjacent.Process(Carrier(1500));
        check(wanted.ChannelDb-adjacent.ChannelDb>35,"500 Hz CW filter rejects adjacent carrier 1.5 kHz away");
    }
}

