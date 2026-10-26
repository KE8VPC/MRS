using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
namespace MRS;
internal static class HackRfTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(Marshal.SizeOf<HackRfNative.Transfer>() == 40 && Marshal.OffsetOf<HackRfNative.Transfer>("ValidLength").ToInt32() == 20 && Marshal.SizeOf<HackRfNative.DeviceList>() == 48, "HackRF x64 native transfer and device-list ABI layouts");
        var values = Enumerable.Range(0, 256).Select(n => (byte)n).ToArray(); HackRfProtocol.Normalize(values);
        check(values[0] == 128 && values[127] == 255 && values[128] == 0 && values[255] == 127, "HackRF signed I/Q maps zero and both extrema to unsigned I/Q");
        HackRfProtocol.Normalize(values); check(values.SequenceEqual(Enumerable.Range(0,256).Select(n=>(byte)n)), "HackRF sample conversion is lossless for every 8-bit value");
        foreach (int lna in new[] {0,8,16,24,32,40}) foreach (int vga in new[] {0,2,16,32,62}) foreach (bool amp in new[] {false,true})
        { var g = new HackRfGain(lna,vga,amp); check(HackRfGain.Decode(g.Token) == g, $"HackRF gain round trip LNA {lna}, VGA {vga}, amplifier {amp}"); }
        foreach (int bad in new[] {-1000,0,600,new HackRfGain(7,2).Token,new HackRfGain(48,2).Token,new HackRfGain(8,63).Token})
        { bool rejected=false;try{HackRfGain.Decode(bad);}catch(ArgumentException){rejected=true;}check(rejected,"HackRF rejects unsupported gain token " + bad); }
        TransmitTests.Run(check);
        HfTests.Run(check);
        CaptureOffsetTests.Run(check);
        int gain = new HackRfGain().Token;
        using var stream = new MemoryStream(); HackRfProtocol.Write(stream, new byte[4096], 6000000000L,19200000,gain);
        var header = HackRfProtocol.Header(stream.ToArray()[..HackRfProtocol.HeaderSize]);
        check(header.Frequency == 6000000000L && header.Rate == 19200000 && header.UsableBandwidth == 15000000, "HackRF transport preserves 6 GHz frequency and analog usable coverage separately from sample rate");
        var corrupt = stream.ToArray()[..HackRfProtocol.HeaderSize]; corrupt[0] = 0;
        bool invalid=false;try{HackRfProtocol.Header(corrupt);}catch(IOException){invalid=true;}check(invalid,"HackRF rejects corrupt framing");
        var commands = new HackRfProtocol.Commands(false); commands.Accept("TUNE 6000000000");commands.Accept("RATE 19200000");commands.Accept("GAIN " + gain);
        check(commands.Take() == (6000000000L,19200000,gain), "HackRF live commands preserve upper-range tuning and independent gains");
        commands.Accept("TUNE 6000000001");commands.Accept("RATE 3200000");commands.Accept("GAIN -1000");
        check(commands.Take() == (0L,0,0), "HackRF rejects out-of-range tuning, RTL rates and automatic gain");
        var ownerA=new object();var ownerB=new object();var ownerC=new object();
        try
        {
            check(ReceiverOwnership.Acquire(ownerA,0,null,"A",out _) && ReceiverOwnership.Acquire(ownerB,0,null,"B",out _,"HackRF") && !ReceiverOwnership.Acquire(ownerC,0,null,"C",out _,"HackRF"), "RTL index zero and HackRF index zero are independent; duplicate HackRF ownership is blocked");
        }
        finally{ReceiverOwnership.Release(ownerA);ReceiverOwnership.Release(ownerB);ReceiverOwnership.Release(ownerC);}
        var device=new RtlNative.Device(0,"HackRF","HackRF","HackRF One","0000000000000000a1b2c3d4e5f67890","HackRF");
        check(ReceiverIdentity.Match(new[]{device with{Index=2}},ReceiverIdentity.Key(device))?.Index==2 && ReceiverIdentity.Key(device)!=ReceiverIdentity.Key(device with{Backend="RTL"}), "HackRF identity survives enumeration changes and is namespaced by backend");
        foreach(int rate in ReceiverCapabilities.HackRf.SampleRates)
        {
            byte[] iq=SelfTests.MakeFm(false,rate/10,rate);
            var clock=Stopwatch.StartNew();byte[] pcm=new FmDemodulator(false,rate).Process(iq);clock.Stop();
            check(pcm.Length==9600,$"HackRF {rate} S/s demodulation produces exactly 48 kHz audio ({clock.ElapsedMilliseconds} ms for 100 ms I/Q)");
            var split=new FmDemodulator(false,rate);byte[] splitPcm=split.Process(iq[..32766]).Concat(split.Process(iq[32766..])).ToArray();
            check(pcm.SequenceEqual(splitPcm),$"HackRF {rate} S/s DSP preserves state across uneven USB blocks");
            float[] samples=Enumerable.Range(0,2048).Select(n=>BitConverter.ToInt16(pcm,4096+2*n)/32768f).ToArray();var spectrum=SignalMath.Spectrum(samples);
            check(Math.Abs(Array.IndexOf(spectrum,spectrum.Max())*48000.0/2048-1000)<24 && SignalMath.Level(samples)>-40,$"HackRF {rate} S/s recovers the NFM modulation tone");
            var wide = new FmDemodulator(true,rate); byte[] widePcm=wide.Process(SelfTests.MakeFm(true,rate/10,rate));
            check(widePcm.Length==9600 && wide.StereoAudio.Length==19200,$"HackRF {rate} S/s WFM produces 48 kHz mono and stereo monitoring");
            byte[] outside = new byte[rate/5];
            for(int n=0;n<outside.Length/2;n++){double angle=2*Math.PI*241000*n/rate;outside[2*n]=(byte)(127.5+90*Math.Cos(angle));outside[2*n+1]=(byte)(127.5+90*Math.Sin(angle));}
            var reject = new FmDemodulator(false,rate); reject.Process(outside);reject.Process(outside);
            check(reject.ChannelDb < -45,$"HackRF {rate} S/s anti-alias filter rejects a carrier that would fold onto the tuned channel");
        }
        string file=Path.Combine(Path.GetTempPath(),"mrs-hackrf-"+Guid.NewGuid()+".mrsiq");
        try
        {
            using var receiver=new RtlReceiver(); using var applied=new ManualResetEventSlim();string? ended=null;
            receiver.Ended+=message=>ended=message;
            receiver.Applied+=(_,_)=>applied.Set();
            receiver.Start(0,5800000000,gain,false,fixture:true,sampleRate:2400000,fixtureSeconds:15,backend:"HackRF");
            check(applied.Wait(6000),"HackRF fixture starts above uint frequency range: "+ended);
            int worker=receiver.WorkerId;
            receiver.RecordIq(file,new IqMetadata(3,DateTimeOffset.UtcNow,false,gain,ReceiverIdentity.Key(device)));
            applied.Reset();receiver.Retune(6000000000).GetAwaiter().GetResult();check(applied.Wait(5000)&&receiver.AppliedFrequency==6000000000,"HackRF live retune acknowledges 6 GHz without reopening process");
            applied.Reset();receiver.SetGain(new HackRfGain(32,24,true).Token).GetAwaiter().GetResult();check(applied.Wait(5000)&&receiver.AppliedGain==new HackRfGain(32,24,true).Token,"HackRF live gain acknowledgment carries LNA VGA and amplifier");
            applied.Reset();receiver.SetRate(9600000).GetAwaiter().GetResult();check(applied.Wait(5000)&&receiver.AppliedRate==9600000&&receiver.WorkerId==worker,"HackRF rate change preserves worker ownership");
            Thread.Sleep(150);receiver.Stop();
            using(var reader=new IqReader(file))
            {
                check(reader.Metadata.Backend=="HackRF","HackRF I/Q recording identifies gain encoding and source backend");
                IqFrame? frame;bool high=false;while((frame=reader.Read())!=null){frame.Capture.RequireCurrentDsp();high|=frame.Frequency==6000000000;}
                check(high,"HackRF recording retains 64-bit frequency through control changes");
            }
            receiver.StartReplay(file);check(SpinWait.SpinUntil(()=>receiver.ReplayFinished,6000)&&receiver.IsHackRf&&receiver.WorkerId==0,"HackRF recorded I/Q replays without receiver hardware");receiver.Stop();
        }
        finally{if(File.Exists(file))File.Delete(file);}
    }
}

