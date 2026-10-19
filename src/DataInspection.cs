using System.Text;
using System.Text.Json;
namespace MRS;
internal sealed record InspectionField(string Name,string Value,string Origin,int? BitStart=null,int? BitLength=null)
{
    public string Range=>BitStart is int start?$"{start}–{start+BitLength-1}":"—";
}
internal sealed class DataInspection
{
    internal PacketObservation Observation {get;}
    internal List<InspectionField> Fields {get;}=[];
    internal byte[] Bytes {get;}
    internal string Provenance {get;}
    internal string Json {get;}
    internal string Hex=>string.Join(" ",Bytes.Select(b=>b.ToString("X2")));
    internal string Bits=>string.Concat(Bytes.Select(b=>Convert.ToString(b,2).PadLeft(8,'0')));
    internal DataInspection(PacketObservation observation)
    {
        // Snapshot the exported representation, including nested arrays, so live updates cannot alter this inspection.
        Json=JsonSerializer.Serialize(observation,new JsonSerializerOptions{WriteIndented=true});
        Observation=JsonSerializer.Deserialize<PacketObservation>(Json)!;
        try{Bytes=Convert.FromHexString(observation.RawHex);}catch(FormatException){Bytes=[];}
        bool nid=observation.Protocol=="P25 Phase I"&&observation.Kind=="NID header"&&Bytes.Length==8;
        bool tsbk=observation.Protocol=="P25 Phase I"&&observation.Validation.StartsWith("TSBK CRC valid",StringComparison.Ordinal)&&Bytes.Length==12;
        Provenance=nid?"Received NID bits after status-symbol removal, before BCH correction. Decoded NAC/DUID may differ from these received bits. Payload is not verified by this header.":
            tsbk?"Decoded TSBK bytes after status-symbol removal, deinterleaving and trellis decoding; CRC verified. Original received symbols and individual correction locations are not retained.":
            observation.Protocol=="LTR"?"Sliced LTR Standard outbound 40-bit word; CRC-7 checked, no error correction applied.":
            observation.Protocol=="DMR"&&observation.Kind=="Short link control"?"Reassembled and error-corrected 36-bit short LC, padded by four trailing zero bits; CRC-8 validated. Not a raw RF burst.":
            observation.Protocol=="DMR"?"Received sliced 264-bit DMR burst, before FEC correction. Slot Golay validation does not validate the payload; link identifiers require BPTC and RS or CSBK CRC validation. No corrected-bit map is retained.":
            observation.Protocol=="UAT"?"UAT bytes are received with-parity codewords. Fields are RS-decoded; corrections count symbols, not bits. No received-to-corrected bit map is retained.":
            observation.Protocol is "ACARS" or "VDL2" or "ADS-B / Mode S"?"Aviation decoder supplies decoded frame bytes and validation metadata; correction may have occurred. These are not original RF symbols. Field bit maps and exact RF timestamps are unavailable.":
            PagingRules.IsPaging(observation)?"Paging worker supplies decoded fields/text only. Received codewords, corrected-bit locations and waveform samples are not available in this observation.":
            Bytes.Length>0?"Bytes supplied by the decoder. Field positions and received-versus-corrected mapping are not available for this protocol.":"No raw frame bytes were supplied by this decoder.";
        void Add(string name,string value,string origin="Observation",int? start=null,int? length=null)=>Fields.Add(new(name,value,origin,start,length));
        Add("Protocol",observation.Protocol);Add("Type",observation.Kind);Add("Validation",observation.Validation);Add("Source",observation.Input);
        Add("Frequency",observation.FrequencyHz is long hz?$"{hz/1e6:F6} MHz":"Not supplied");
        Add("Observed UTC",observation.ObservedUtc.ToString("O"));Add("Replay position",observation.PositionSeconds is double seconds?$"≈ {seconds:F3} seconds":"Not supplied");
        Add("Label",observation.Label??"","Operator annotation");Add("Recipients",string.Join(", ",observation.PagingRecipients??[observation.Destination]));Add("Reception count",observation.ReceptionCount.ToString());
        if(nid)
        {
            Add("Received NAC bits",Read(0,12).ToString("X3"),"Received NID",0,12);
            Add("Received DUID bits",Read(12,4).ToString("X1"),"Received NID",12,4);
            Add("BCH parity region",Read(16,47).ToString("X12"),"Received NID",16,47);
            Add("Final parity bit",Read(63,1).ToString(),"Received NID",63,1);
            if(P25Framer.DecodeNid(Read(0,64),out int nac,out int duid,out int errors))
            {Add("Decoded NAC",nac.ToString("X3"),"BCH-decoded field");Add("Decoded DUID",duid.ToString("X1"),"BCH-decoded field");Add("NID corrected bit count",errors.ToString(),"BCH decoder");}
        }
        if(tsbk)
        {
            Add("Last block",Read(0,1).ToString(),"Decoded TSBK",0,1);Add("Protected",Read(1,1).ToString(),"Decoded TSBK",1,1);
            Add("Opcode",Read(2,6).ToString("X2"),"Decoded TSBK",2,6);Add("Manufacturer ID",Read(8,8).ToString("X2"),"Decoded TSBK",8,8);
            Add("Payload",Read(16,64).ToString("X16"),"Decoded TSBK; interpretation depends on opcode",16,64);
            Add("CRC",Read(80,16).ToString("X4"),"Decoded TSBK",80,16);
            // The field layout is the same one used by P25Framer.Describe. Do not interpret protected/vendor payloads.
            if(Read(1,1)==0&&Read(8,8)==0&&Read(2,6)==0)
            {
                Add("Service options",Read(16,8).ToString("X2"),"Standard group voice grant",16,8);
                Add("Channel ID",Read(24,16).ToString("X4"),"Standard group voice grant",24,16);
                Add("Talkgroup",Read(40,16).ToString(),"Standard group voice grant",40,16);
                Add("Source radio ID",Read(56,24).ToString(),"Standard group voice grant",56,24);
            }
        }
        if(observation.DecoderJson is {} decoder)
        {
            try{using var doc=JsonDocument.Parse(decoder);Flatten(doc.RootElement,"decoder",Fields,"Decoder JSON (timestamp is processing time)");}
            catch(JsonException){Add("Decoder JSON","Malformed JSON; inspect the snapshot JSON tab","Unavailable interpretation");}
        }
        using var snapshot=JsonDocument.Parse(Json);
        foreach(var property in snapshot.RootElement.EnumerateObject().Where(p=>p.Name.StartsWith("P25",StringComparison.Ordinal)&&p.Value.ValueKind!=JsonValueKind.Null))Flatten(property.Value,property.Name,Fields,"Decoded / resolved P25 metadata; no bit mapping");
    }
    private ulong Read(int start,int count){ulong value=0;for(int n=start;n<start+count;n++)value=(value<<1)|(uint)((Bytes[n/8]>>(7-n%8))&1);return value;}
    private static void Flatten(JsonElement item,string path,List<InspectionField> fields,string origin,int depth=0)
    {
        if(depth>12||fields.Count>=512)return;
        if(item.ValueKind==JsonValueKind.Object){foreach(var p in item.EnumerateObject())Flatten(p.Value,path+"."+p.Name,fields,origin,depth+1);}
        else if(item.ValueKind==JsonValueKind.Array){int n=0;foreach(var p in item.EnumerateArray())Flatten(p,$"{path}[{n++}]",fields,origin,depth+1);}
        else fields.Add(new(path,item.ToString(),origin));
    }
    internal string ByteDump()
    {
        if(Bytes.Length==0)return "No raw bytes available. See decoded fields and snapshot JSON.";
        var text=new StringBuilder("Offset    Hex bytes                                        ASCII\n");
        for(int n=0;n<Bytes.Length;n+=16){var block=Bytes.Skip(n).Take(16).ToArray();text.AppendLine($"{n:X6}    {string.Join(" ",block.Select(b=>b.ToString("X2"))).PadRight(47)}  {new string(block.Select(b=>b>=32&&b<127?(char)b:'.').ToArray())}");}
        return text.ToString();
    }
}
