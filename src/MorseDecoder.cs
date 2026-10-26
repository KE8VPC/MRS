namespace MRS;
internal sealed record MorseResult(string Text,double Wpm,double ToneHz,bool TimingPlausible);
internal sealed class MorseDecoder
{
    internal static readonly Dictionary<char,string> Alphabet=new(){['A']=".-",['B']="-...",['C']="-.-.",['D']="-..",['E']=".",['F']="..-.",['G']="--.",['H']="....",['I']="..",['J']=".---",['K']="-.-",['L']=".-..",['M']="--",['N']="-.",['O']="---",['P']=".--.",['Q']="--.-",['R']=".-.",['S']="...",['T']="-",['U']="..-",['V']="...-",['W']=".--",['X']="-..-",['Y']="-.--",['Z']="--..",['0']="-----",['1']=".----",['2']="..---",['3']="...--",['4']="....-",['5']=".....",['6']="-....",['7']="--...",['8']="---..",['9']="----.",['/']="-..-.",['-']="-....-"};
    private readonly double[] frame=new double[480];private int count,frameNumber;private bool on;private int began,lastEnd;private double dot=8,tone=800;private string code="",text="";private bool uncertain;private readonly List<int> durations=[];private readonly List<int> marks=[];
    internal event Action<MorseResult>? Result;
    internal void Feed(double[] values){foreach(double value in values){frame[count++]=double.IsFinite(value)?value:0;if(count==480){Analyze();count=0;}}}
    private void Analyze()
    {
        frameNumber++;double mean=frame.Average(),energy=0;foreach(double x in frame)energy+=(x-mean)*(x-mean);
        double best=0;int bestHz=0;
        for(int hz=300;hz<=1600;hz+=50){double coeff=2*Math.Cos(2*Math.PI*hz/48000),a=0,b=0;foreach(double x in frame){double c=x-mean+coeff*a-b;b=a;a=c;}double power=a*a+b*b-coeff*a*b;if(power>best){best=power;bestHz=hz;}}
        bool hit=energy>480*4 && best*2/(480*Math.Max(1e-9,energy))>.68;
        if(hit && (on||code.Length>0||text.Length>0) && Math.Abs(bestHz-tone)>100)hit=false;
        if(hit&&!on){int gap=frameNumber-lastEnd;if(lastEnd>0&&marks.Count>0&&gap>=2&&gap<dot*2.2)dot=Math.Clamp(gap,3,24);on=true;began=frameNumber;tone=bestHz;}
        if(!hit&&on){on=false;int length=frameNumber-began;lastEnd=frameNumber;if(length<2)return;
            if(length>80){uncertain=true;code="";marks.Clear();return;}
            durations.Add(length);if(durations.Count>64)durations.RemoveAt(0);
            var sorted=durations.Order().ToArray();double shortest=sorted[Math.Min(sorted.Length-1,sorted.Length/4)];
            if(durations.Count>=3)dot=Math.Clamp(shortest,3,24);
            marks.Add(length);code="pending";if(marks.Count>6){marks.Clear();code="";uncertain=true;}
        }
        if(!on&&code.Length>0&&frameNumber-lastEnd>=dot*2.2){code=string.Concat(marks.Select(n=>n>dot*2?"-":"."));marks.Clear();var match=Alphabet.FirstOrDefault(p=>p.Value==code);text+=match.Key==default?'?':match.Key;uncertain|=match.Key==default;code="";}
        if(text.Length>=160 || !on&&text.Length>0&&frameNumber-lastEnd>=Math.Max(100,dot*8)){
            Result?.Invoke(new(text,120/dot,tone,!uncertain&&text.Length>=3));text="";uncertain=false;durations.Clear();dot=8;
        }
    }
}
