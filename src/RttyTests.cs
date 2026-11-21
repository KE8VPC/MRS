using System.Text;

namespace MRS;

internal static class RttyTests
{
    internal static void Run(Action<bool,string> check)
    {
        foreach(double baud in new[]{45.45,50,75})
        foreach(bool reverse in new[]{false,true})
        foreach(int shift in new[]{170,425,850})
        {
            var receiver=new RttyReceiver(baud,shiftHz:shift,reverse:reverse);
            var text=new StringBuilder();
            receiver.Character+=character=>text.Append(character);
            double phase=0, samples=0;
            void Bit(bool mark,double bits=1)
            {
                double hz=mark^reverse?2125:2125+shift;
                samples+=48000/baud*bits;
                int count=(int)samples;samples-=count;
                for(int i=0;i<count;i++)
                {
                    phase+=2*Math.PI*hz/48000;
                    receiver.Feed(.4*Math.Sin(phase)+.01*Math.Sin(i*1.89));
                }
            }
            void Code(int code,bool stop=true)
            {
                Bit(false);
                for(int i=0;i<5;i++)Bit((code&(1<<i))!=0);
                Bit(stop,1.5);
            }
            Bit(true,10);
            // LTRS, R,Y,R,Y,space,T,E,S,T,space,FIGS,1,2,3,LTRS,CR,LF.
            foreach(int code in new[]{31,10,21,10,21,4,16,1,5,16,4,27,23,19,1,31,8,2})Code(code);
            Bit(true,3);
            check(text.ToString()=="RYRY TEST 123\r\n",
                $"RTTY US ITA2 decodes letters, figures and line endings at {baud} baud / {shift} Hz with reverse {reverse}");
            text.Clear();
            Code(10,stop:false);Bit(true,4);
            check(text.Length==0&&receiver.Rejected>0,$"RTTY rejects a missing stop bit at {baud} baud / {shift} Hz");
        }
        var silence=new RttyReceiver();int count=0;silence.Character+=_=>count++;
        for(int n=0;n<48000;n++)silence.Feed(0);
        check(count==0,"Silent RTTY input does not produce decoded text");
    }
}
