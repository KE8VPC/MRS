namespace MRS;
internal sealed record ComparedField(string Name,string Reference,string Current,string Change);
internal sealed class DataComparison
{
    internal List<ComparedField> Fields {get;}=[];
    internal int[] ChangedBits {get;}
    internal string BitStatus {get;}
    internal string Summary {get;}
    internal DataComparison(DataInspection reference,DataInspection current)
    {
        // Pair by field identity, not row order. Decoder JSON may reorder its properties.
        var before=reference.Fields.GroupBy(f=>(f.Name,f.Origin)).ToDictionary(g=>g.Key,g=>string.Join(" | ",g.Select(f=>f.Value)));
        var after=current.Fields.GroupBy(f=>(f.Name,f.Origin)).ToDictionary(g=>g.Key,g=>string.Join(" | ",g.Select(f=>f.Value)));
        before[("Message","Decoded text")]=reference.Observation.Description;
        after[("Message","Decoded text")]=current.Observation.Description;
        before[("Information","Decoded text")]=reference.Observation.Information;
        after[("Information","Decoded text")]=current.Observation.Information;
        foreach(var key in before.Keys.Union(after.Keys))
        {
            bool hasBefore=before.TryGetValue(key,out var a),hasAfter=after.TryGetValue(key,out var b);
            Fields.Add(new(key.Name,a??"—",b??"—",!hasBefore?"Added":!hasAfter?"Removed":a==b?"Same":"Changed"));
        }
        bool comparable=reference.Bytes.Length>0&&reference.Bytes.Length==current.Bytes.Length&&
            reference.Observation.Protocol==current.Observation.Protocol&&reference.Observation.Kind==current.Observation.Kind&&
            reference.Provenance==current.Provenance&&reference.Fields.Any(f=>f.BitStart!=null)&&current.Fields.Any(f=>f.BitStart!=null);
        var referenceBits=reference.Bits;var currentBits=current.Bits;
        ChangedBits=comparable?Enumerable.Range(0,referenceBits.Length).Where(n=>referenceBits[n]!=currentBits[n]).ToArray():[];
        BitStatus=comparable?$"{ChangedBits.Length} differing bits of {reference.Bits.Length}; zero-based, MSB first. Differences are not a bit-error rate or correction count.":
            "Bit comparison unavailable: requires matching mapped frame types, byte lengths and provenance. Decoded fields/text can still be compared.";
        Summary=$"Reference: {reference.Observation.ObservedUtc:O} · {reference.Observation.Input} · {reference.Observation.FrequencyHz?.ToString()??"unknown"} Hz\n"+
            $"Current: {current.Observation.ObservedUtc:O} · {current.Observation.Input} · {current.Observation.FrequencyHz?.ToString()??"unknown"} Hz\n"+
            "Observation metadata (including timestamps and counts) is compared as well as decoded fields. Different sources/frequencies do not establish the same transmitter.\n"+BitStatus;
    }
    internal string BitReport()=>BitStatus+(ChangedBits.Length>0?"\nChanged bit positions: "+string.Join(", ",ChangedBits):"");
}
