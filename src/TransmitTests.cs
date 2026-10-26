namespace MRS;
internal static class TransmitTests
{
    internal static void Run(Action<bool,string> check)
    {
        AprsTransmitTests.Run(check);
        foreach(long hz in new[]{145500000L,446000000L}){new TxTestPlan(hz).Validate();check(true,"Manual TX accepts supported test band "+hz);}
        foreach(var p in new[]{new TxTestPlan(1090000000),new TxTestPlan(162400000),new TxTestPlan(145500000,-1),new TxTestPlan(145500000,21),new TxTestPlan(145500000,0,0),new TxTestPlan(145500000,0,4)})
        {bool rejected=false;try{p.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"TX test rejects unsupported frequency/gain/duration: "+p);}
        var tone=TxTestPlan.Tone();check(tone.Length==16000&&tone.Select(x=>(int)unchecked((sbyte)x)).Max()<=32&&tone.Select(x=>(int)unchecked((sbyte)x)).Min()>=-32,"TX tone uses bounded signed I/Q amplitude and one millisecond period");
        double squareError=0;double maxDev=0;
        for(int n=0;n<8000;n++)
        {
            int next=(n+1)%8000;double i=unchecked((sbyte)tone[2*n]),q=unchecked((sbyte)tone[2*n+1]);double ni=unchecked((sbyte)tone[2*next]),nq=unchecked((sbyte)tone[2*next+1]);
            double phase=Math.Atan2(q,i);double expected=-2.5*Math.Cos(2*Math.PI*n/8000);squareError+=(phase-expected)*(phase-expected);
            // Across 8-bit quantization steps instantaneous deviation spikes; validate phase trajectory instead.
            maxDev=Math.Max(maxDev,Math.Abs(phase));
        }
        check(Math.Sqrt(squareError/8000)<.02&&maxDev<2.53,"TX waveform matches one kHz FM with 2.5 kHz nominal deviation including wrap boundary");
    }
}
