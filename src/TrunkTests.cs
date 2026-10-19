namespace MRS;
internal static class TrunkTests
{
    internal static string Analyze(string file)
    {
        byte[] bytes=System.IO.File.ReadAllBytes(file);var samples=new double[bytes.Length/8];Buffer.BlockCopy(bytes,0,samples,0,bytes.Length);
        var rows=new List<DmrObservation>();var dmr=new DmrDecoder();var ltr=new LtrDecoder();dmr.Message+=rows.Add;ltr.Message+=rows.Add;
        for(int i=0;i<samples.Length;i+=4800){var chunk=samples.AsSpan(i,Math.Min(4800,samples.Length-i));dmr.Process(chunk);ltr.Process(chunk);}
        return $"Samples {samples.Length}; observations {rows.Count}\n"+string.Join("\n",rows.GroupBy(x=>new{x.Protocol,x.SystemType,x.ColorCode,x.LinkValid,x.DataType,x.Network,x.Site,x.RestChannel}).Select(g=>$"{g.Key}: {g.Count()}"));
    }
    internal const string CapCall="429D8AF0167C1CDC7FA05E00184DFF57D75DF5DEFDB90168198034843A493E9303";
    internal const string CapStatus="205B40F48052013D0438003018EDFF57D75DF5DA042B406882AB06240648098016";
    internal static ulong LtrWord(int area=0,int channel=2,int home=2,int group=12,int free=6)
    {
        ulong word=(0x158UL<<31)|((ulong)area<<30)|((ulong)channel<<25)|((ulong)home<<20)|((ulong)group<<12)|((ulong)free<<7);return word|(uint)LtrWords.Checksum(word);
    }
    internal static double[] LtrSamples(ulong word,int repeats=4,int sign=1)
    {
        var random=new Random(24);var values=new List<double>();
        for(int repeat=0;repeat<repeats;repeat++)for(int bit=39;bit>=0;bit--)for(int i=0;i<160;i++)values.Add((((word>>bit)&1)!=0?550:-550)*sign+175+(random.NextDouble()-.5)*30);
        values.AddRange(new double[160]);return values.ToArray();
    }
    internal static void Run(Action<bool,string> check)
    {
        var call=DmrBits.Parse(DmrTests.Bits(Convert.FromHexString(CapCall)),null)!;
        check(call is {SystemType:"Capacity Plus",Source:12345,Destination:91,RestChannel:3,LinkValid:true},"Independent MMDVM Cap+ call vector: manufacturer LC identifiers");
        var status=DmrBits.Parse(DmrTests.Bits(Convert.FromHexString(CapStatus)),null)!;
        check(status is {SystemType:"Capacity Plus",RestChannel:1,LinkValid:true,Network:null,Site:null},"Independent Cap+ status vector: CRC validated, no invented network/site");
        var evidence=new TrunkEvidence();var seen=DateTimeOffset.UtcNow;var reception=new DmrReception(status,new("test","Synthetic",true,464412500),seen);
        check(evidence.Observe(reception)==1&&evidence.Observe(reception with{Seen=seen.AddSeconds(1)})==2&&evidence.Observe(reception with{Seen=seen.AddSeconds(2)})==3,"Color-code identity requires three matching observations");
        foreach(int cc in new[]{14,7,10,1,3,2})check(evidence.Observe(reception with{Fields=status with{ColorCode=cc}})==1,$"One-off color code {cc} cannot become a confirmed row");
        check(evidence.Observe(reception with{Context=reception.Context with{FrequencyHz=464075000}})==1,"Same color code at another frequency has independent evidence");
        check(evidence.Observe(reception with{Seen=seen.AddSeconds(11)})==1,"Old candidate evidence expires after ten seconds");
        check(evidence.Observe(reception with{Fields=status with{LinkValid=false}})==0&&evidence.Observe(reception with{Fields=status with{SlotCorrections=3}})==0,"Failed payload CRC and heavily corrected slot type cannot confirm a color code");
        var slow=new TrunkEvidence();var idle=reception with{Fields=LtrWords.Parse(LtrWord(0,4,4,255,4))!};
        check(slow.Observe(idle)==1&&slow.Observe(idle with{Seen=seen.AddSeconds(11)})==2&&slow.Observe(idle with{Seen=seen.AddSeconds(22)})==3,"LTR idle bursts eleven seconds apart confirm within sixty seconds");
        check(slow.Observe(idle with{Seen=seen.AddSeconds(66)})==4,"Confirmed LTR identity retains evidence while bursts continue within sixty seconds");
        check(slow.Observe(idle with{Seen=seen.AddSeconds(127)})==1,"LTR evidence expires after sixty seconds without a matching observation");
        var sparse=new TrunkEvidence();sparse.Observe(idle);
        check(sparse.Observe(idle with{Seen=seen.AddSeconds(61)})==1,"Unconfirmed LTR evidence cannot accumulate beyond sixty seconds");
        check(sparse.Progress(seen.AddSeconds(62)).Contains("1/3")&&!sparse.Progress(seen.AddSeconds(122)).Contains("1/3"),"Pending status reports confirmation count and removes expired progress");
        var p=Convert.FromHexString("BE10E1000000000000004E15");
        for(int bit=0;bit<96;bit++){var bad=(byte[])p.Clone();bad[bit/8]^=(byte)(128>>(bit%8));check(!TrunkSignaling.Enrich(status with{SystemType="DMR (type unknown)",RestChannel=null},bad).LinkValid,$"CSBK detects single-bit corruption {bit}");}
        byte[] slc=DmrShortControl.Decode(DmrTests.Bits(Convert.FromHexString("90C5363A05A5C3CC50")).Take(68).ToArray())!;
        check(slc!=null&&DmrShortControl.Parse(slc,call) is {SystemType:"Connect Plus",Network:0x123,Site:0x45},"Independent MMDVM short LC product code, CRC and 12-bit network identity");
        var control=new DmrShortControl();var raw=DmrTests.Bits(Convert.FromHexString("90C5363A05A5C3CC50"));DmrObservation? found=null;
        for(int i=0;i<4;i++)found=control.Feed(Cach(raw.Skip(i*17).Take(17).ToArray(),new[]{1,3,3,2}[i]),call,i*1440);
        check(found is {Network:0x123,Site:0x45},"CACH fragment order and timing assemble a network identity");
        var modem=new DmrDecoder();var decoded=new List<DmrObservation>();modem.Message+=decoded.Add;
        var rf=DmrSamples(CapStatus,true);for(int i=0;i<rf.Length;i+=137)modem.Process(rf.AsSpan(i,Math.Min(137,rf.Length-i)));
        check(decoded.Any(x=>x.Network==0x123&&x.Site==0x45)&&decoded.Count(x=>x.DataType==3)==4,"DMR symbol stream carries Cap+ status and independent Connect Plus SLC test identity");
        control=new();control.Feed(Cach(raw.Take(17).ToArray(),1),call,0);check(control.Feed(Cach(raw.Skip(51).Take(17).ToArray(),2),call,1440)==null,"Missing CACH fragments cannot publish identities");
        var word=LtrWord();check(LtrWords.Parse(word) is {Area:0,Home:2,LtrGroup:12,Channel:2,FreeChannel:6},"LTR Standard field extraction");
        for(int i=0;i<40;i++)check(LtrWords.Parse(word^(1UL<<i))==null,$"LTR rejects corrupted word bit {i}");
        foreach(int sign in new[]{1,-1}){var decoder=new LtrDecoder();var rows=new List<DmrObservation>();decoder.Message+=rows.Add;var samples=LtrSamples(word,4,sign);for(int i=0;i<samples.Length;i+=137)decoder.Process(samples.AsSpan(i,Math.Min(137,samples.Length-i)));check(rows.Count==4&&rows.All(r=>r.Home==2&&r.LtrGroup==12),$"LTR noisy DC-offset discriminator blocks, polarity {sign}: {rows.Count} words");}
        check(LtrWords.Parse(LtrWord(channel:2,group:255,free:2))?.Detail=="Idle","LTR idle word is not a subscriber talkgroup");
        var noise=new LtrDecoder();int hits=0;noise.Message+=_=>hits++;var rng=new Random(9);noise.Process(Enumerable.Range(0,480000).Select(_=>(rng.NextDouble()-.5)*3000).ToArray());check(hits==0,"LTR rejects ten seconds of broadband noise");
    }
    internal static bool[] Cach(bool[] fragment,int flag)
    {
        var bits=new bool[24];int coded=flag<<3;int[] checks=[5,7,6,3];for(int i=0;i<4;i++)if((flag&(8>>i))!=0)coded^=checks[i];
        for(int i=0;i<7;i++)bits[DmrShortControl.Order[i]]=(coded&(64>>i))!=0;for(int i=0;i<17;i++)bits[DmrShortControl.Order[i+7]]=fragment[i];return bits;
    }
    internal static double[] DmrSamples(string burst,bool shortControl=false)
    {
        var values=new List<double>();var raw=DmrTests.Bits(Convert.FromHexString("90C5363A05A5C3CC50"));
        for(int repeat=0;repeat<4;repeat++)
        {
            var cach=shortControl?Cach(raw.Skip(repeat*17).Take(17).ToArray(),new[]{1,3,3,2}[repeat]):new bool[24];
            var bits=cach.Concat(DmrTests.Bits(Convert.FromHexString(burst))).ToArray();
            for(int i=0;i<bits.Length;i+=2){int d=(bits[i]?2:0)|(bits[i+1]?1:0);double level=d switch{0=>648,1=>1944,2=>-648,_=>-1944};for(int n=0;n<10;n++)values.Add(level);}
        }
        return values.ToArray();
    }
}
