using System.IO;
namespace MRS;
internal static class AprsTransmitTests
{
    internal static void Run(Action<bool,string> check)
    {
        var status=new AprsTransmit("N0CALL-7","","MRS synthetic TX check");var packet=Ax25Parser.Parse(status.Frame());
        check(packet.FcsValid&&packet.Source=="N0CALL-7"&&packet.Destination=="APZMRS"&&packet.Path=="","APRS sender produces valid direct AX.25 UI/FCS");
        var position=new AprsTransmit("N0CALL-7","WIDE1-1,WIDE2-1","Synthetic",true,49.058333333,-72.029166667);
        check(position.Information=="!4903.50N/07201.75W-Synthetic","APRS position format has fixed-width coordinates and symbol");
        check(Ax25Parser.Parse(position.Frame()).Path=="WIDE1-1,WIDE2-1","APRS TX path has no already-repeated flags");
        check(new AprsTransmit("N0CALL","","",true,12.999999,-179.999999).Information.StartsWith("!1300.00N/18000.00W"),"APRS coordinate rounding carries minutes into degrees");
        foreach(var bad in new[]{status with{Source="BAD*"},status with{Source="N0CALL-16"},status with{Text="bad\nline"},status with{Text=new string('A',81)},status with{Path="A1,B2,C3"},position with{Latitude=double.NaN},position with{Longitude=181}})
        {bool rejected=false;try{bad.Frame();}catch(ArgumentException){rejected=true;}check(rejected,"APRS sender rejects invalid address/payload/coordinates");}
        foreach(var item in new[]{status,position})
        {
            var pcm=item.Audio().SelectMany(x=>BitConverter.GetBytes((short)(x*16000))).ToArray();var modem=new Afsk1200();var frames=new List<byte[]>();modem.Frame+=(raw,_)=>frames.Add(raw);modem.Process(pcm);
            check(frames.Count==1&&frames[0].SequenceEqual(item.Frame()),"APRS AFSK waveform decodes to exactly one intended frame");
        }
        var iq=status.Waveform(240000);HackRfProtocol.Normalize(iq);var dsp=new ReceiveDemodulator("NFM",240000,0,12500,false);var decoder=new Afsk1200();var recovered=new List<byte[]>();decoder.Frame+=(raw,_)=>recovered.Add(raw);var demodulated=dsp.Process(iq);decoder.Process(demodulated);
        check(recovered.Count==1&&recovered[0].SequenceEqual(status.Frame()),"APRS FM I/Q demodulates to the exact transmitted packet");
        var longest=new AprsTransmit("N0CALL-15","WIDE1-1,WIDE2-1",new string('X',80),true,90,180);
        check(longest.Audio().Length<48000*3,"Maximum APRS form fits the bounded three-second transmitter");
        var nativeIq=status.Waveform();check(nativeIq.Length==(int)Math.Ceiling(status.Audio().Length*(double)TxTestPlan.Rate/48000)*2,"Native eight MS/s APRS waveform has the expected bounded sample count");
        File.WriteAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"aprs-tx-synthetic-8m.s8"),nativeIq);
        // Public synthetic fixture for an independent decoder, never transmitted.
        File.WriteAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"aprs-tx-synthetic-48k.pcm"),status.Audio().SelectMany(x=>BitConverter.GetBytes((short)(x*16000))).ToArray());
        File.WriteAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,"aprs-tx-synthetic-iq-demod-48k.pcm"),demodulated);
    }
}
