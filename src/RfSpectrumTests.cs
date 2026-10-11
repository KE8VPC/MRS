namespace MRS;
internal static class RfSpectrumTests
{
    internal static byte[] Tones(int size, params (int Bin,double Amplitude)[] tones)
    {
        var data=new byte[size*2];
        for(int n=0;n<size;n++)
        {double i=0,q=0;foreach(var tone in tones){double phase=2*Math.PI*tone.Bin*n/size;i+=tone.Amplitude*Math.Cos(phase);q+=tone.Amplitude*Math.Sin(phase);}data[2*n]=(byte)Math.Clamp(Math.Round(127.5+i*128),0,255);data[2*n+1]=(byte)Math.Clamp(Math.Round(127.5+q*128),0,255);}
        return data;
    }
    internal static void Run(Action<bool,string> check)
    {
        foreach(int size in RfSpectrumAnalyzer.Sizes)
        {
            var input=Tones(size,(-37,.5),(42,.2));var copy=(byte[])input.Clone();
            var analyzer=new RfSpectrumAnalyzer();analyzer.Append(input[..2000],size);
            check(analyzer.Snapshot()==null,$"RF {size} waits for real samples instead of zero-padding");
            analyzer.Append(input[2000..],size);var spectrum=analyzer.Snapshot()!;
            check(spectrum.Length==size&&Array.IndexOf(spectrum,spectrum.Max())==size/2-37,$"RF {size} retains every shifted FFT bin with correct negative frequency");
            check(Math.Abs(spectrum[size/2-37]+6.02)<.2 && Math.Abs(spectrum[size/2+42]+13.98)<.3,$"RF {size} preserves tone calibration across resolution settings");
            var split=new RfSpectrumAnalyzer();for(int at=0;at<input.Length;at+=4094)split.Append(input[at..Math.Min(at+4094,input.Length)],size);
            check(split.Snapshot()!.SequenceEqual(spectrum)&&input.SequenceEqual(copy),$"RF {size} crosses uneven USB blocks without changing raw I/Q");
            analyzer.Reset();analyzer.Append(input[..2000],size);check(analyzer.Snapshot()==null,$"RF {size} clears old samples on capture boundary");
        }
        var fine=new RfSpectrumAnalyzer();fine.Append(Tones(65536,(100,.4),(106,.4)),65536);var bins=fine.Snapshot()!;
        check(bins[32768+103]<Math.Min(bins[32768+100],bins[32768+106])-25,"High-detail FFT separates close carriers that shared one old display bin");
        fine.Append(new byte[2000],4096);check(fine.Snapshot()==null,"Changing FFT resolution discards the old sample window");
        check(new DisplaySettings{RfFftSize=123}.Normalize().RfFftSize==32768,"Unsupported stored FFT setting restores safe default");
        var view=new SpectrumView();view.SetRf(774400000,9600000);double now=DisplaySettings.Clock;
        view.PushAt(bins,now);view.PushAt(Enumerable.Repeat(-90f,4096).ToArray(),now+.1);
        check(view.BinCount==4096,"Spectrum and waterfall accept resolution changes without mixing old row sizes");
        var history=new WaterfallHistory();var values=new[]{-120f,-80.4f,-12.2f,20f};history.Add(values,0);
        check(history.RetainedBytes==values.Length&&values.Select((v,i)=>Math.Abs(history.Snapshot()[0].Value(i)-v)<=.5).All(v=>v),"Waterfall retains frequency bins with bounded one-byte dB storage");
        history.Clear();check(history.RetainedBytes==0,"Clearing waterfall releases retained byte budget");
    }
}
