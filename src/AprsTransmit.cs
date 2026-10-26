using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace MRS;
internal sealed record AprsTransmit(string Source,string Path,string Text,bool Position=false,double Latitude=0,double Longitude=0,char Symbol='-')
{
    private static readonly Regex AddressPattern=new(@"^[A-Z0-9]{1,6}(-([0-9]|1[0-5]))?$",RegexOptions.CultureInvariant);
    internal static string Address(string value)
    {
        value=value.Trim().ToUpperInvariant();if(!AddressPattern.IsMatch(value))throw new ArgumentException("Use a callsign of up to six letters/digits and optional SSID 0–15.");return value;
    }
    internal string[] Digipeaters=>string.IsNullOrWhiteSpace(Path)?[]:Path.Split(',').Select(Address).ToArray();
    internal void Validate()
    {
        string call=Address(Source);if(!call.Split('-')[0].Any(char.IsLetter)||!call.Split('-')[0].Any(char.IsDigit))throw new ArgumentException("Enter your callsign and optional SSID.");
        if(Digipeaters.Length>2)throw new ArgumentException("Use no more than two path addresses, or leave path blank for direct reception.");
        if(Text.Length>80||Text.Any(c=>c<32||c>126))throw new ArgumentException("Status/comment must be at most 80 printable ASCII characters.");
        if(!Position&&string.IsNullOrWhiteSpace(Text))throw new ArgumentException("Enter status text.");
        if(Position&&(!double.IsFinite(Latitude)||!double.IsFinite(Longitude)||Math.Abs(Latitude)>90||Math.Abs(Longitude)>180))throw new ArgumentException("Enter latitude -90 to 90 and longitude -180 to 180.");
        if(Symbol<'!'||Symbol>'~')throw new ArgumentException("Select a printable APRS symbol.");
    }
    private static string Coordinate(double value,bool latitude)
    {
        long hundredths=(long)Math.Round(Math.Abs(value)*6000,MidpointRounding.AwayFromZero);long degrees=hundredths/6000;double minutes=(hundredths%6000)/100.0;
        return degrees.ToString(latitude?"00":"000",CultureInfo.InvariantCulture)+minutes.ToString("00.00",CultureInfo.InvariantCulture)+(latitude?(value<0?"S":"N"):(value<0?"W":"E"));
    }
    internal string Information {get{Validate();return Position?"!"+Coordinate(Latitude,true)+"/"+Coordinate(Longitude,false)+Symbol+Text:">"+Text;}}
    internal string Preview=>Address(Source)+">APZMRS"+(Digipeaters.Length==0?"":","+string.Join(",",Digipeaters))+":"+Information;
    internal byte[] Frame()
    {
        Validate();var data=new List<byte>();var addresses=new[]{"APZMRS",Address(Source)}.Concat(Digipeaters).ToArray();
        for(int a=0;a<addresses.Length;a++){var parts=addresses[a].Split('-');foreach(char c in parts[0].PadRight(6))data.Add((byte)(c<<1));int ssid=parts.Length==2?int.Parse(parts[1],CultureInfo.InvariantCulture):0;data.Add((byte)(0x60|(ssid<<1)|(a==0?0x80:0)|(a==addresses.Length-1?1:0)));}
        data.Add(3);data.Add(0xf0);data.AddRange(Encoding.ASCII.GetBytes(Information));
        ushort crc=0xffff;foreach(byte value in data){crc^=value;for(int k=0;k<8;k++)crc=(ushort)((crc&1)!=0?(crc>>1)^0x8408:crc>>1);}crc=(ushort)~crc;
        data.Add((byte)crc);data.Add((byte)(crc>>8));return data.ToArray();
    }
    internal double[] Audio()
    {
        var bits=new List<bool>();void Flag(){for(int i=0;i<8;i++)bits.Add((0x7e&(1<<i))!=0);}
        for(int i=0;i<40;i++)Flag();int ones=0;
        foreach(byte value in Frame())for(int bit=0;bit<8;bit++){bool one=(value&(1<<bit))!=0;bits.Add(one);ones=one?ones+1:0;if(ones==5){bits.Add(false);ones=0;}}
        for(int i=0;i<12;i++)Flag();var audio=new double[bits.Count*40+1920];bool mark=false;double phase=0;int n=960;
        foreach(bool bit in bits){if(!bit)mark=!mark;double step=2*Math.PI*(mark?1200:2200)/48000;for(int k=0;k<40;k++){audio[n++]=Math.Sin(phase);phase=(phase+step)%(2*Math.PI);}}
        return audio;
    }
    internal byte[] Waveform(int rate=TxTestPlan.Rate)
    {
        var audio=Audio();int count=(int)Math.Ceiling(audio.Length*(double)rate/48000.0);if(count>rate*3)throw new ArgumentException("Packet exceeds three-second limit.");
        var iq=new byte[count*2];double phase=0;
        for(int n=0;n<count;n++){double pos=n*48000.0/rate;int at=(int)pos;double sample=audio[at]*(1-(pos-at))+(at+1<audio.Length?audio[at+1]:0)*(pos-at);phase=(phase+2*Math.PI*2500*sample/rate)%(2*Math.PI);double ramp=Math.Min(1,Math.Min(n,count-1-n)/(rate*.005));iq[n*2]=unchecked((byte)(sbyte)Math.Round(32*ramp*Math.Cos(phase)));iq[n*2+1]=unchecked((byte)(sbyte)Math.Round(32*ramp*Math.Sin(phase)));}
        return iq;
    }
}
