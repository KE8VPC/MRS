namespace MRS;
internal static class VoiceIdTests
{
    internal static double[] Morse(string text,int wpm=15,int hz=800,double noise=0)
    {
        var output=new List<double>();var random=new Random(92);int unit=57600/wpm;
        void Add(int n,bool tone){for(int i=0;i<n;i++)output.Add((tone?9000*Math.Sin(2*Math.PI*hz*output.Count/48000):0)+noise*(random.NextDouble()*2-1));}
        Add(4800,false);foreach(char c in text){foreach(char bit in MorseDecoder.Alphabet[c]){Add(unit*(bit=='-'?3:1),true);Add(unit,false);}Add(unit*2,false);}Add(96000,false);return output.ToArray();
    }
    internal static void Run(Action<bool,string> check)
    {
        DigitalVoiceTests.Run(check);
        var boosted=VoiceAudioGain.Apply(BitConverter.GetBytes((short)1000),12);
        check(BitConverter.ToInt16(boosted)>3900&&BitConverter.ToInt16(boosted)<4100,"P25 +12 dB gain boosts quiet audio about fourfold");
        var limited=VoiceAudioGain.Apply(BitConverter.GetBytes(short.MinValue).Concat(BitConverter.GetBytes(short.MaxValue)).ToArray(),30);
        check(BitConverter.ToInt16(limited,0)>=-30000&&BitConverter.ToInt16(limited,2)<=30000&&BitConverter.ToInt16(limited,0)<0,"P25 limiter preserves polarity and avoids PCM overflow");
        check(new DisplaySettings{SpectrumFraction=double.NaN}.Normalize().SpectrumFraction==.4&&new DisplaySettings{SpectrumFraction=9}.Normalize().SpectrumFraction==.8,"Spectrum split normalizes invalid and out-of-range settings");
        foreach(int duid in new[]{0,5,10}){var bytes=new byte[duid==0?15:duid==5?9:12];for(int n=0;n<bytes.Length;n++)bytes[n]=(byte)(n*13);var coded=ImbeVocoder.EncodeMetadata(duid,bytes);check(ImbeVocoder.ReadMetadata(duid,coded)?.SequenceEqual(bytes)==true,$"P25 voice native FEC round trip DUID {duid}");coded[0]^=1;check(ImbeVocoder.ReadMetadata(duid,coded)?.SequenceEqual(bytes)==true,$"P25 voice single-bit metadata repair DUID {duid}");}
        using var voice=new P25VoiceDecoder();var audio=new List<byte>();voice.Audio+=audio.AddRange;
        var symbols=VoiceFixtures.Hdu.Concat(VoiceFixtures.Ldu1).Concat(VoiceFixtures.Ldu2).ToArray();
        var samples=P25Tests.Signal(symbols:symbols);for(int at=0;at<samples.Length;at+=960)voice.Feed(samples.Skip(at).Take(960).ToArray());
        check(voice.Frames==18&&audio.Count==18*160*6*4,"Independent OP25 clear HDU/LDU vectors produce exactly 18 IMBE frames without phase duplicates");
        var pcm=audio.ToArray();double energy=0;int crossings=0;short prior=0;
        for(int i=4000;i<pcm.Length/4;i++){short value=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i*4,2));energy+=(double)value*value;if(prior<=0&&value>0)crossings++;prior=value;}
        double tone=crossings*48000.0/Math.Max(1,pcm.Length/4-4000);
        check(energy>100000&&tone>950&&tone<1070,$"IMBE independent 1011-Hz reference synthesizes expected tone ({tone:0} Hz)");
        string dir=AppContext.BaseDirectory;using(var wav=new NAudio.Wave.WaveFileWriter(System.IO.Path.Combine(dir,"synthetic-p25-voice-decoded.wav"),new NAudio.Wave.WaveFormat(48000,16,2)))wav.Write(pcm,0,pcm.Length);
        using var encrypted=new P25VoiceDecoder();int count=0;encrypted.Audio+=_=>count++;
        foreach(var chunk in new[]{VoiceFixtures.EncryptedHdu,VoiceFixtures.Ldu1,VoiceFixtures.EncryptedLdu2}){var signal=P25Tests.Signal(symbols:chunk);for(int at=0;at<signal.Length;at+=960)encrypted.Feed(signal.Skip(at).Take(960).ToArray());}
        check(count==0&&encrypted.Status.Contains("84"),"AES-256 header/ESS never produces clear audio");
        using var unknown=new P25VoiceDecoder();unknown.Audio+=_=>count++;unknown.Feed(P25Tests.Signal(symbols:VoiceFixtures.Ldu1));check(count==0,"Late entry without encryption metadata remains muted");
        foreach(int wpm in new[]{12,20,25}){var cw=new MorseDecoder();var texts=new List<MorseResult>();cw.Result+=texts.Add;cw.Feed(Morse("KE8VPC",wpm,800,500));check(texts.Any(r=>r.Text=="KE8VPC"),$"CW ID at {wpm} WPM with noise: {string.Join('/',texts.Select(r=>r.Text))}");}
        var noiseDecoder=new MorseDecoder();int falseIds=0;noiseDecoder.Result+=_=>falseIds++;var random=new Random(1);noiseDecoder.Feed(Enumerable.Range(0,144000).Select(_=>10000*(random.NextDouble()*2-1)).ToArray());check(falseIds==0,"Broadband noise does not produce CW IDs");
    }
}
