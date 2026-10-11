using System.Globalization;
using System.IO;
namespace MRS;

internal sealed record NifogChannel(string Name,long MobileRxHz,long? MobileTxHz,string RxCode,string TxCode,
    string Mode,string Category,string Assignment,string Note,int PrintedPage)
{
    internal string LabelAt(long hz)=>Name+(Category=="Low band"&&Assignment.Contains('*')?" (proposed)":"")+(hz!=MobileRxHz?" TX":"");
    private static string Code(string value)=>string.Join(" / ",value.Split('/').Select(part=>
    {
        string text=part.Trim();
        return text.StartsWith('$')?"NAC "+text[1..]+" (hex)":text=="CSQ"?"Carrier squelch":text.Length>0&&char.IsDigit(text[0])?"CTCSS "+text+" Hz":text;
    }));
    internal string DetailsAt(long hz)
    {
        string role=MobileTxHz==null?"Receive only":MobileTxHz==MobileRxHz?"Simplex / direct":hz==MobileRxHz?"Mobile RX / normally repeater output":"Mobile TX / normally repeater input";
        string tx=MobileTxHz is long frequency?$"{frequency/1000000.0:0.00000} MHz · {Code(TxCode)}":"Not assigned (receive only)";
        return $"{Name} · {Category} · {role}\n{Assignment} · {Mode}\nMobile RX: {MobileRxHz/1000000.0:0.00000} MHz · {Code(RxCode)}\nMobile TX: {tx}\n{Note}\nCISA NIFOG 2.02 (January 2025), p. {PrintedPage} (PDF p. {PrintedPage+7})";
    }
}

internal static class NifogPlan
{
    internal const string SourceUrl="https://www.cisa.gov/sites/default/files/2024-12/NIFOG%202.02_508%20FINAL%20VERSION%2012%2003%202024.pdf";
    internal static readonly NifogChannel[] Channels=Load();
    internal static readonly PlanChannel[] Markers=Channels.SelectMany(c=>c.MobileTxHz is long tx && tx!=c.MobileRxHz?new[]{c.MobileRxHz,tx}:new[]{c.MobileRxHz}).Distinct().Order()
        .Select(hz=>
        {
            var entries=Channels.Where(c=>c.MobileRxHz==hz||c.MobileTxHz==hz).ToArray();
            var labels=entries.Select(c=>c.LabelAt(hz)).Distinct().ToArray();
            string label=string.Join(" / ",labels.Take(2))+(labels.Length>2?$" +{labels.Length-2}":"");
            return new PlanChannel(hz,label,string.Join("\n\n",entries.Select(c=>c.DetailsAt(hz))));
        }).ToArray();
    internal static IEnumerable<PlanChannel> Visible(double start,double end)=>Markers.Where(m=>m.Frequency>=start&&m.Frequency<=end);
    private static NifogChannel[] Load()
    {
        using var stream=typeof(NifogPlan).Assembly.GetManifestResourceStream("MRS.NifogChannels.tsv")??throw new InvalidDataException("Missing NIFOG reference dataset.");
        using var reader=new StreamReader(stream);reader.ReadLine();var result=new List<NifogChannel>();
        while(reader.ReadLine() is {} line)
        {
            if(string.IsNullOrWhiteSpace(line))continue;
            var c=line.Split('\t');if(c.Length!=10)throw new InvalidDataException("Invalid NIFOG reference row.");
            result.Add(new(c[0],long.Parse(c[1],CultureInfo.InvariantCulture),c[2].Length==0?null:long.Parse(c[2],CultureInfo.InvariantCulture),c[3],c[4],c[5],c[6],c[7],c[8],int.Parse(c[9],CultureInfo.InvariantCulture)));
        }
        return result.ToArray();
    }
}
