using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;

namespace MRS;

internal static class SstvTests
{
    // Timings cross-checked against pySSTV's independent Martin encoder.
    // These fixtures carry six wide RGB/color bars, not on-air images.
    private sealed class Signal(SlowScanTelevision decoder, double clockScale=1, int offset=0)
    {
        private double phase, remaining;
        private uint random=1;
        internal void Tone(double hz, double milliseconds, bool noise=false)
        {
            remaining+=milliseconds*48*clockScale;
            int count=(int)remaining;
            remaining-=count;
            for(int i=0;i<count;i++)
            {
                phase+=2*Math.PI*(hz+offset)/48000;
                if(phase>2*Math.PI)phase-=2*Math.PI;
                random=random*1664525+1013904223;
                double disturbance=noise?((random/(double)uint.MaxValue)-.5)*.05:0;
                decoder.Feed(.45*Math.Sin(phase)+disturbance+.02);
            }
        }
        internal void Header(int code, bool badParity=false)
        {
            Tone(1900,300);Tone(1200,10);Tone(1900,300);Tone(1200,30);
            int ones=0;
            for(int i=0;i<7;i++){int bit=(code>>i)&1;ones+=bit;Tone(bit==1?1100:1300,30);}
            bool parity=ones%2==1;
            Tone(parity^badParity?1100:1300,30);Tone(1200,30);
        }
        internal void Lines(SstvMode mode,int rows)
        {
            for(int row=0;row<rows;row++)
            {
                Tone(1200,4.862);Tone(1500,.572);
                foreach(int component in new[]{1,2,0})
                {
                    foreach(var color in Bars)
                        Tone(1500+800*color[component]/255.0,mode.ScanMilliseconds/Bars.Length,noise:true);
                    Tone(1500,.572);
                }
            }
        }
    }
    private static readonly byte[][] Bars =
    [
        [255,0,0], [0,255,0], [0,0,255], [255,255,255], [0,0,0], [128,128,128]
    ];

    private static bool CorrectBars(byte[] pixels,int width)
    {
        for(int bar=0;bar<Bars.Length;bar++)
        {
            int x=(int)((bar+.5)*width/Bars.Length);
            for(int component=0;component<3;component++)
                if(Math.Abs(pixels[x*3+component]-Bars[bar][component])>24)return false;
        }
        return true;
    }

    internal static void Run(Action<bool,string> check)
    {
        foreach(var mode in SstvMode.Supported)
        {
            var receiver=new SlowScanTelevision();
            var lines=new List<byte[]>();
            receiver.Line+=(_,row)=>lines.Add(row);
            var signal=new Signal(receiver);
            signal.Tone(1500,73);
            signal.Header(mode.Vis);
            signal.Lines(mode,4);
            check(receiver.Mode==mode&&lines.Count==4&&lines.All(row=>CorrectBars(row,mode.Width)),
                $"SSTV auto VIS and noisy RGB line reception for {mode.Name} · {lines.Count} rows · {receiver.Status} · " +
                string.Join(",",lines.Select(row=>CorrectBars(row,mode.Width))));
        }
        var manual=new SlowScanTelevision(SstvMode.Supported[0],clockPpm:800,toneOffset:75);
        byte[]? last=null;manual.Line+=(_,row)=>last=row;
        var corrected=new Signal(manual,1.0008,75);
        corrected.Lines(SstvMode.Supported[0],3);
        check(last!=null&&CorrectBars(last,320),
            "Manual SSTV reception starts at line sync and applies tone-offset and slant settings");
        corrected.Tone(1500,1000);
        check(manual.HadSyncLoss&&manual.Status.StartsWith("Sync lost"),
            "A missing SSTV line sync marks the retained image as incomplete");
        var invalid=new SlowScanTelevision();
        int starts=0;invalid.ImageStarted+=_=>starts++;
        var invalidSignal=new Signal(invalid);
        invalidSignal.Header(44,badParity:true);invalidSignal.Lines(SstvMode.Supported[0],2);
        check(starts==0&&invalid.Mode==null,"A failed SSTV VIS parity check cannot start an automatic image");
        invalidSignal.Header(8);invalidSignal.Tone(1500,100);
        check(starts==0&&invalid.Status.StartsWith("Unsupported SSTV VIS 8"),
            "Unsupported SSTV modes remain explicit instead of being rendered with Martin timing");
        var full=new SlowScanTelevision();int rows=0;full.Line+=(_,_)=>rows++;
        var complete=new Signal(full);complete.Header(40);complete.Lines(SstvMode.Supported[1],SstvMode.Height);
        complete.Tone(1500,100);
        check(rows==256&&full.Status.EndsWith("image complete"),"SSTV stops at its bounded complete image height");
    }

    internal static string CheckWav(string path)
    {
        using var input=new WaveFileReader(path);
        if(input.WaveFormat.SampleRate!=48000||input.WaveFormat.BitsPerSample!=16||input.WaveFormat.Channels!=1)
            throw new IOException("The independent SSTV check expects mono 48 kHz PCM16.");
        var decoder=new SlowScanTelevision();
        var lines=new List<byte[]>();
        decoder.Line+=(_,row)=>lines.Add(row);
        var block=new byte[4096];
        int count;
        while((count=input.Read(block,0,block.Length))>0)
            for(int i=0;i+1<count;i+=2)
                decoder.Feed(System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(block.AsSpan(i))/32768.0);
        if(decoder.Mode==null||decoder.HadSyncLoss||lines.Count!=8||!lines.All(row=>CorrectBars(row,decoder.Mode.Width)))
            throw new IOException($"Independent SSTV recording failed · {decoder.Status} · {lines.Count} rows");
        int stride=decoder.Mode.Width*3;
        var pixels=lines.SelectMany(row=>row).ToArray();
        var bitmap=BitmapSource.Create(decoder.Mode.Width,lines.Count,96,96,PixelFormats.Rgb24,null,pixels,stride);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"sstv-independent.png"));
        encoder.Save(output);
        return $"PASS: independent WAV decoded as {decoder.Mode.Name} with {lines.Count} rows and correct RGB bars.";
    }
}
