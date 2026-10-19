using System.IO;
namespace MRS;
internal static class DmrTests
{
    internal static bool[] Bits(byte[] bytes)=>bytes.SelectMany(b=>Enumerable.Range(0,8).Select(n=>(b&(128>>n))!=0)).ToArray();
    internal static string[] Vectors(){using var s=typeof(DmrTests).Assembly.GetManifestResourceStream("MRS.DmrVectors.tsv")!;using var r=new StreamReader(s);return r.ReadToEnd().Split('\n',StringSplitOptions.RemoveEmptyEntries);}
    internal static string Iq(string file)
    {
        using var reader=new IqReader(file);ReceiveDemodulator? dsp=null;var decoder=new DmrDecoder();var rows=new List<DmrObservation>();decoder.Message+=rows.Add;long frames=0;string metadata="";
        while(reader.Read() is {} frame){var ch=frame.Channel??throw new IOException("No channel metadata");dsp??=new ReceiveDemodulator("NFM",frame.Rate,ch.Frequency-frame.Frequency,16000){CaptureData=true};dsp.Process(frame.Data);decoder.Process(dsp.DataSamples);frames++;metadata=$"Rate {frame.Rate}; capture {frame.Frequency}; channel {ch.Frequency}";}
        return metadata+ $"; frames {frames}; observations {rows.Count}; verified link control {rows.Count(r=>r.LinkValid)}\n"+string.Join("\n",rows.GroupBy(r=>new{r.ColorCode,r.Slot,r.Source,r.Destination,r.LinkValid,r.DataType}).Select(g=>$"{g.Key}: {g.Count()}"));
    }
    internal static void Run(Action<bool,string> check)
    {
        foreach(string line in Vectors()){var parts=line.Trim().Split('\t');int cc=int.Parse(parts[0]),type=int.Parse(parts[1]);var bits=Bits(Convert.FromHexString(parts[2]));var d=DmrBits.Parse(bits,new bool[24]);check(d is {LinkValid:true,Source:12345,Destination:91,Group:true,Slot:1}&&d.ColorCode==cc&&d.DataType==type,$"Independent DMR vector CC {cc} type {type}: Golay BPTC RS and IDs");}
        check(DmrBits.Golay(1)==0x18EB,"ETSI Golay known codeword for data 01");
        var good=Bits(Convert.FromHexString(Vectors()[14].Trim().Split('\t')[2]));var damaged=(bool[])good.Clone();damaged[2]=!damaged[2];damaged[180]=!damaged[180];check(DmrBits.Parse(damaged,null) is {LinkValid:true,Source:12345,Slot:null},"DMR product code repairs isolated errors without inventing mobile slot");
        var invalid=(bool[])good.Clone();for(int i=98;i<102;i++)invalid[i]=!invalid[i];check(DmrBits.Parse(invalid,null)==null,"DMR rejects ambiguous four-bit slot-code damage");
        foreach(int sign in new[]{1,-1})
        {
            var modem=new DmrDecoder();var found=new List<DmrObservation>();modem.Message+=found.Add;var rng=new Random(123);var stream=new List<double>();
            for(int repeat=0;repeat<4;repeat++){foreach(bool[] frame in new[]{new bool[24],good})for(int i=0;i<frame.Length;i+=2){int dibit=(frame[i]?2:0)|(frame[i+1]?1:0);double level=dibit switch{0=>648,1=>1944,2=>-648,_=>-1944};for(int n=0;n<10;n++)stream.Add(sign*level+210+(rng.NextDouble()-.5)*45);}}
            for(int i=0;i<stream.Count;i+=137)modem.Process(stream.Skip(i).Take(137).ToArray());check(found.Count==4&&found.All(f=>f.LinkValid&&f.Source==12345),$"DMR 48 kHz noisy block-boundary receive, polarity {sign}, phase deduplication");
        }
        var noise=new DmrDecoder();int falseHits=0;noise.Message+=_=>falseHits++;var random=new Random(9);noise.Process(Enumerable.Range(0,48000).Select(_=>(random.NextDouble()-.5)*5000).ToArray());check(falseHits==0,"DMR noise does not identify a channel");
        var p=AircraftMap.Project(0,0);check(Math.Abs(p.X-.5)<1e-9&&Math.Abs(p.Y-.5)<1e-9,"Basemap Mercator equator and origin");check(AircraftMap.Project(90,180).Y>=-1e-8&&AircraftMap.Wrap(1.1)<.101,"Basemap polar clipping and longitude wrapping");
        string path=Path.Combine(Path.GetTempPath(),"mrs-dmr-"+Guid.NewGuid()+".json");
        try{var library=new FrequencyLibrary(path);var item=new FrequencyItem{Name="My label",FrequencyHz=461125000,Notes="Keep these notes"};library.Upsert(item);var field=DmrBits.Parse(good,null)!;var row=new DmrReception(field,new("test","Receiver A",false,461125000),DateTimeOffset.UtcNow);library.SaveDmr(row,"A");library.SaveDmr(row,"A");var saved=library.Items.Single(i=>i.Id==item.Id);check(saved.Name=="My label"&&saved.Notes=="Keep these notes"&&saved.DmrIdentifiers.Split('\n').Length==1,"DMR identifier persistence preserves labels and deduplicates");int count=library.Items.Count;library.SaveDmr(row with{Context=row.Context with{Replay=true,FrequencyHz=462000000}},"A");check(library.Items.Count==count,"DMR replay cannot auto-save live discoveries");}finally{if(File.Exists(path))File.Delete(path);}
    }
}
