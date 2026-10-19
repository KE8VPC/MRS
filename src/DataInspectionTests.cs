using System.Text.Json;
namespace MRS;
internal static class DataInspectionTests
{
    internal static PacketObservation Grant()=>new(DateTimeOffset.UtcNow,"Generated P25",true,774306250,2.5,"TSBK CRC valid","NAC 345","","","Group voice grant","Generated grant",null,"8000041002123401234572B8","P25 Phase I",true);
    internal static void Run(Action<bool,string> check)
    {
        var grant=new DataInspection(Grant());
        check(grant.Bytes.Length==12&&grant.Bits.Length==96&&grant.Provenance.Contains("trellis"),"Inspector distinguishes decoded TSBK bytes from received symbols");
        check(grant.Fields.Any(f=>f.Name=="Talkgroup"&&f.Value=="4660"&&f.BitStart==40&&f.BitLength==16)&&grant.Fields.Any(f=>f.Name=="Source radio ID"&&f.Value=="74565"),"Inspector maps standard group-grant fields to verified byte offsets");
        check(grant.Fields.Where(f=>f.BitStart!=null).All(f=>f.BitStart>=0&&f.BitStart+f.BitLength<=grant.Bits.Length),"Inspector mapped bit selections stay inside the supplied frame");
        ulong nid=P25Framer.EncodeNid(0x3457)^0x1000000000000000UL;
        var header=new DataInspection(Grant() with{Kind="NID header",Validation="NID BCH valid (1 corrected bits); header only",RawHex=nid.ToString("X16")});
        check(header.Fields.Any(f=>f.Name=="Decoded NAC"&&f.Value=="345")&&header.Fields.Any(f=>f.Name=="Received NAC bits"&&f.Value!="345")&&header.Provenance.Contains("before BCH"),"Inspector keeps erroneous received NID bits distinct from corrected decoded fields");
        var protectedFrame=new DataInspection(Grant() with{RawHex="C000041002123401234572B8"});
        check(!protectedFrame.Fields.Any(f=>f.Name=="Talkgroup"),"Inspector does not interpret a protected TSBK payload as a clear grant");
        var paging=new DataInspection(Grant() with{Protocol="FLEX",RawHex="",DecoderJson="{\"baud\":1600,\"k_ok\":true,\"message\":\"Synthetic\"}",PagingRecipients=["1234","5678"]});
        check(paging.Bytes.Length==0&&paging.Fields.All(f=>f.BitStart==null)&&paging.Fields.Any(f=>f.Name=="decoder.k_ok"),"Paging inspector exposes JSON fields without inventing raw bits");
        var recipients=new[]{"1234"};var frozen=new DataInspection(Grant() with{PagingRecipients=recipients});recipients[0]="changed";
        check(frozen.Observation.PagingRecipients![0]=="1234"&&JsonDocument.Parse(frozen.Json).RootElement.GetProperty("PositionSeconds").GetDouble()==2.5,"Inspector snapshot deep-copies arrays and preserves replay position");
        check(new DataInspection(Grant() with{RawHex="bad hex"}).Bytes.Length==0,"Inspector handles unavailable or malformed raw hex without crashing");
        check(new DataInspection(Grant() with{Protocol="FLEX",RawHex="",DecoderJson="not json"}).Fields.Any(f=>f.Name=="Decoder JSON"),"Inspector preserves malformed decoder data as unparsed information");
        var same=new DataComparison(grant,new DataInspection(grant.Observation));
        check(same.Fields.All(f=>f.Change=="Same")&&same.ChangedBits.Length==0&&same.BitStatus.StartsWith("0 differing"),"Identical inspection snapshots compare without differences");
        var modified=new DataInspection(grant.Observation with{RawHex="8000041002123501234572B8",Information="Changed synthetic text"});
        var diff=new DataComparison(grant,modified);
        check(diff.ChangedBits.SequenceEqual(new[]{55})&&diff.Fields.Any(f=>f.Name=="Talkgroup"&&f.Change=="Changed")&&diff.Fields.Any(f=>f.Name=="Information"&&f.Change=="Changed"),"Comparison detects exact changed bit and decoded field/text differences");
        check(new DataComparison(header,grant).BitStatus.Contains("unavailable"),"Comparison refuses received NID versus decoded TSBK bit comparisons");
        var pageBefore=new DataInspection(paging.Observation with{DecoderJson="{\"a\":1,\"b\":2}"});
        var pageAfter=new DataInspection(paging.Observation with{DecoderJson="{\"b\":2,\"a\":1}"});
        check(new DataComparison(pageBefore,pageAfter).Fields.All(f=>f.Change=="Same"),"Comparison pairs decoder fields by identity independent of JSON order");
        var pageDiff=new DataComparison(pageBefore,new DataInspection(paging.Observation with{DecoderJson="{\"b\":3,\"c\":4}"}));
        check(pageDiff.Fields.Any(f=>f.Name=="decoder.a"&&f.Change=="Removed")&&pageDiff.Fields.Any(f=>f.Name=="decoder.c"&&f.Change=="Added")&&pageDiff.Fields.Any(f=>f.Name=="decoder.b"&&f.Change=="Changed")&&pageDiff.BitStatus.Contains("unavailable"),"Paging comparison tracks added removed changed fields without fabricating bit differences");
    }
}
