using System.IO;
using System.Numerics;
namespace MRS;

// Streaming anti-alias/fractional-delay conversion for NRSC-5's unsigned I/Q pipe.
internal sealed class HdIqConverter
{
    internal const int OutputRate=1488375;
    private const int Taps=128, Phases=1024;
    private readonly float[][] kernels=new float[Phases][];
    private readonly float[] iHistory=new float[Taps*2],qHistory=new float[Taps*2];
    private readonly int rate;
    private int position;
    private long phase;
    internal HdIqConverter(int inputRate)
    {
        if(inputRate<960000 || inputRate>3200000)throw new IOException("HD currently requires 960 kS/s–3.2 MS/s centered raw I/Q. For HackRF select 2.4 MS/s; wider HackRF captures are not supported by the HD converter yet.");
        rate=inputRate;
        for(int p=0;p<Phases;p++)
        {
            var h=kernels[p]=new float[Taps];double sum=0,cutoff=300000.0/rate;
            for(int k=0;k<Taps;k++)
            {
                double x=k-(Taps-1)/2.0-p/(double)Phases;
                double v=(Math.Abs(x)<1e-12?2*cutoff:Math.Sin(2*Math.PI*cutoff*x)/(Math.PI*x))*(.42-.5*Math.Cos(2*Math.PI*k/(Taps-1))+.08*Math.Cos(4*Math.PI*k/(Taps-1)));
                h[k]=(float)v;sum+=v;
            }
            for(int k=0;k<Taps;k++)h[k]/=(float)sum;
        }
    }
    internal byte[] Process(byte[] input)
    {
        if(input.Length%2!=0)throw new IOException("Incomplete complex I/Q sample.");
        var output=new byte[(int)(((long)input.Length/2*OutputRate+phase)/rate)*2];int written=0;
        for(int n=0;n<input.Length;n+=2)
        {
            position=(position+Taps-1)%Taps;
            iHistory[position]=iHistory[position+Taps]=input[n]-127.5f;
            qHistory[position]=qHistory[position+Taps]=input[n+1]-127.5f;
            phase+=OutputRate;
            while(phase>=rate)
            {
                phase-=rate;var h=kernels[(int)(phase*Phases/OutputRate)];
                var vi=Vector<float>.Zero;var vq=Vector<float>.Zero;
                for(int k=0;k<Taps;k+=Vector<float>.Count)
                {
                    var c=new Vector<float>(h,k);
                    vi+=c*new Vector<float>(iHistory,position+k);vq+=c*new Vector<float>(qHistory,position+k);
                }
                output[written++]=(byte)Math.Clamp((int)Math.Round(Vector.Sum(vi)+127.5),0,255);
                output[written++]=(byte)Math.Clamp((int)Math.Round(Vector.Sum(vq)+127.5),0,255);
            }
        }
        return output;
    }
    internal static void Convert(string source,string destination,CancellationToken cancel,Action<string>? progress=null)
    {
        cancel.ThrowIfCancellationRequested();
        using var reader=new IqReader(source);
        using var file=new FileStream(destination,FileMode.CreateNew,FileAccess.Write);
        IqFrame? first=null;HdIqConverter? converter=null;long previous=0;int frames=0;
        while(reader.Read() is {} frame)
        {
            cancel.ThrowIfCancellationRequested();
            first??=frame;
            if(frame.Format!=IqSampleFormat.Unsigned8 || frame.Frequency!=first.Frequency || frame.Rate!=first.Rate || frame.Capture.UsableBandwidthHz<600000)
                throw new IOException("HD file analysis requires one fixed frequency/rate and at least 600 kHz of unsigned-8 I/Q coverage.");
            if(frame.Channel is {} channel && channel.Frequency!=frame.Frequency)throw new IOException("For this HD preview, record with the station centered in the RF capture.");
            if(frames>0 && (frame.ElapsedTicks-previous)/1e7>.25)throw new IOException("Recording contains a reception gap; use a continuous capture for HD analysis.");
            converter??=new(frame.Rate);file.Write(converter.Process(frame.Data));previous=frame.ElapsedTicks;
            if(++frames%100==0)progress?.Invoke($"Preparing HD samples… {frames} frames");
        }
        if(frames==0)throw new IOException("Recording contains no samples.");
    }
}
