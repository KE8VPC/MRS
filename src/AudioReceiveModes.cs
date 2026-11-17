using System.Text;

namespace MRS;

internal sealed class ConversationCw(int pitch, int wpm, int spacingWpm)
{
    private static readonly Dictionary<string,char> Alphabet = MorseDecoder.Alphabet
        .Concat(new Dictionary<char,string>{{'.',".-.-.-"},{',',"--..--"},{'?',"..--.."},{'=',"-...-"},{'+',".-.-."},{'@',".--.-."}})
        .ToDictionary(x=>x.Value,x=>x.Key);
    private readonly StringBuilder symbols = new();
    private readonly double coefficient = 2*Math.Cos(2*Math.PI*pitch/48000);
    private readonly double dot = 240.0/wpm; // 5 ms detector frames.
    private readonly double spaceDot = 240.0/spacingWpm;
    private int samples, run, consecutive;
    private bool keyed, wordPending, previousHit;
    private double a,b,energy,peak;
    internal event Action<char>? Character;

    internal void Feed(double x)
    {
        double next = x + coefficient*a-b;
        b=a; a=next;
        energy+=x*x;
        if (++samples<240) return;
        double level=2*(a*a+b*b-coefficient*a*b)/(240.0*240);
        peak=Math.Max(level,peak*.993);
        bool hit=level>Math.Max(2e-7,peak*.12)&&level*240>energy*.42;
        samples=0; a=b=energy=0;
        consecutive=hit==previousHit?Math.Min(consecutive+1,2):1;
        previousHit=hit;
        if(hit!=keyed && consecutive==2)
        {
            if(keyed && run>=dot*.38)
            {
                if(symbols.Length<7) symbols.Append(run<dot*2?'.':'-');
            }
            keyed=hit;
            run=0;
        }
        run=Math.Min(run+1,10000);
        if(keyed) return;
        if(symbols.Length>0 && run>=1.9*spaceDot) FlushCharacter();
        if(run>=5*spaceDot && wordPending)
        {
            Character?.Invoke(' ');
            wordPending=false;
        }
    }
    internal void FlushCharacter()
    {
        if(symbols.Length==0) return;
        Character?.Invoke(Alphabet.GetValueOrDefault(symbols.ToString(),'?'));
        symbols.Clear();
        wordPending=true;
    }
}

internal sealed class WeatherFax(int ioc,int linesPerMinute,int offset,int clockPpm)
{
    internal int Width { get; } = (int)(Math.PI*ioc);
    private readonly double samplesPerLine = 48000.0*60/linesPerMinute*(1+clockPpm/1e6);
    private long count;
    private double baseline, previous, crossing = -1, brightness, phase;
    private int lastPixel=-1;
    private byte[]? line;
    private double sum;
    private int accumulated;
    internal event Action<byte[]>? Line;
    internal void Feed(double x)
    {
        baseline += .08*(x-baseline);
        double sample=x-baseline;
        count++;
        if(previous<=0 && sample>0)
        {
            double time=count-1-previous/(sample-previous);
            double period=time-crossing;
            if(crossing>=0 && period is >=16 and <=39)
                brightness=Math.Clamp((48000/period-1500-offset)/800,0,1);
            crossing=time;
        }
        previous=sample;
        line??=new byte[Width];
        int pixel=Math.Min(Width-1,(int)(phase*Width/samplesPerLine));
        if(pixel!=lastPixel)
        {
            if(lastPixel>=0) line[lastPixel]=(byte)Math.Round(255*sum/accumulated);
            lastPixel=pixel;
            accumulated=0;
            sum=0;
        }
        sum+=brightness;
        accumulated++;
        if(++phase>=samplesPerLine)
        {
            line[pixel]=(byte)Math.Round(255*sum/accumulated);
            Line?.Invoke(line);
            phase-=samplesPerLine;
            line=null;
            lastPixel=-1;
        }
    }
}
