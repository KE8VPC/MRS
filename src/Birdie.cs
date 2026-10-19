namespace MRS;
internal sealed record Birdie(string Name,double PositionHz,double WidthHz,bool Relative=false)
{
    internal double Frequency(double captureCenter)=>Relative?captureCenter+PositionHz:PositionHz;
    internal bool Valid=>!string.IsNullOrWhiteSpace(Name)&&Name.Length<=80&&double.IsFinite(PositionHz)&&Math.Abs(PositionHz)<=1e12&&
        (Relative||PositionHz>0)&&double.IsFinite(WidthHz)&&WidthHz>=100&&WidthHz<=1e6;
    internal bool Overlaps(double frequency,double width,double captureCenter)=>Math.Abs(Frequency(captureCenter)-frequency)<=(WidthHz+width)/2;
    public override string ToString()=>$"{Name} · {(Relative?$"capture {PositionHz/1000:+0.###;-0.###;0} kHz":$"{PositionHz/1e6:0.######} MHz")} · {WidthHz/1000:0.###} kHz wide";
}
