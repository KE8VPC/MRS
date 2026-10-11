using NAudio.Dsp;
namespace MRS;
// Retain real consecutive samples across USB blocks; never zero-pad to invent detail.
internal sealed class RfSpectrumAnalyzer
{
    internal static readonly int[] Sizes = [4096, 16384, 32768, 65536];
    private byte[] ring = [];
    private Complex[] fft = [];
    private float[] window = [];
    private double[] cosine = [], sine = [];
    private int position, filled;
    internal int Size { get; private set; }
    internal void Reset() { position = filled = 0; }
    internal void Append(byte[] iq, int size)
    {
        if (!Sizes.Contains(size)) throw new ArgumentOutOfRangeException(nameof(size));
        if (iq.Length % 2 != 0) throw new ArgumentException("I/Q requires paired components.");
        if (Size != size)
        {
            Size = size; ring = new byte[size * 2]; fft = new Complex[size];
            cosine=Enumerable.Range(0,size/2).Select(k=>Math.Cos(-2*Math.PI*k/size)).ToArray();
            sine=Enumerable.Range(0,size/2).Select(k=>Math.Sin(-2*Math.PI*k/size)).ToArray();
            window = Enumerable.Range(0,size).Select(n=>(float)(.5-.5*Math.Cos(2*Math.PI*n/(size-1)))).ToArray(); Reset();
        }
        var input=iq.AsSpan();
        if(input.Length>=ring.Length) { input[^ring.Length..].CopyTo(ring);position=0;filled=ring.Length;return; }
        int first=Math.Min(input.Length,ring.Length-position);
        input[..first].CopyTo(ring.AsSpan(position));input[first..].CopyTo(ring);
        position=(position+input.Length)%ring.Length;filled=Math.Min(ring.Length,filled+input.Length);
    }
    internal float[]? Snapshot()
    {
        if(filled<ring.Length || Size==0)return null;
        for(int n=0;n<Size;n++)
        {int i=(position+n*2)%ring.Length;fft[n].X=(ring[i]-127.5f)/128*window[n];fft[n].Y=(ring[i+1]-127.5f)/128*window[n];}
        // Double-precision roots avoid the half-angle float recurrence collapsing at 64K.
        for(int i=1,j=0;i<Size;i++)
        {int bit=Size>>1;for(; (j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j)(fft[i],fft[j])=(fft[j],fft[i]);}
        for(int length=2;length<=Size;length<<=1)
        {
            int half=length/2,step=Size/length;
            for(int start=0;start<Size;start+=length)
            for(int k=0;k<half;k++)
            {
                int a=start+k,b=a+half,root=k*step;
                double x=fft[b].X*cosine[root]-fft[b].Y*sine[root];
                double y=fft[b].X*sine[root]+fft[b].Y*cosine[root];
                var u=fft[a];fft[a].X=(float)(u.X+x);fft[a].Y=(float)(u.Y+y);
                fft[b].X=(float)(u.X-x);fft[b].Y=(float)(u.Y-y);
            }
        }
        var result=new float[Size];
        for(int n=0;n<Size;n++)
        {var b=fft[(n+Size/2)%Size];result[n]=(float)Math.Max(-120,10*Math.Log10(Math.Max(1e-12,4*((double)b.X*b.X+(double)b.Y*b.Y)/((double)Size*Size))));}
        return result;
    }
}
