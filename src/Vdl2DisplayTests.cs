using System.Text.Json.Nodes;

namespace MRS;

internal static class Vdl2DisplayTests
{
    internal static void Run(Action<bool,string> check)
    {
        var body = JsonNode.Parse("""
        {"decode":{"crc_ok":true},"raw":"01020304","body":{"type":"vdl2","kind":"xid","details":{
        "src":{"addr":"ABC123","kind":"aircraft"},"dst":{"addr":"10340A","kind":"ground_delegated"},
        "params":[{"group":240,"id":131,"name":"destination-airport","text":"KORD"},
        {"group":240,"id":64,"name":"autotune-frequency","freq_mhz":136.975}],
        "info_len":45,"info_hex":"01020304"}}}
        """)!.AsObject();
        AviationMessage Parse() => AviationData.Parse(body.ToJsonString(),"vdl2",
            AviationTests.Context with { FrequencyHz=136975000 })!;
        var xid=Parse();
        check(xid.Packet.From=="ABC123" && xid.Packet.To=="10340A" &&
              xid.Packet.Type=="Data-link settings" && xid.Packet.Description.Contains("Reported destination KORD") &&
              xid.Packet.Description.Contains("136.975 MHz") && xid.Latitude==null &&
              xid.Packet.DecoderJson==body.ToJsonString() && xid.Packet.RawHex=="01020304" &&
              xid.Packet.Information.Contains("info_hex"),
            "VDL2 XID summaries expose endpoints, destination and advertised channel while retaining raw evidence");
        body["decode"]!["crc_ok"]=false;
        var bad=Parse();
        var aircraft=new AircraftTable();aircraft.Observe(bad);
        check(bad.Identity==null && bad.Packet.DisplaySummary==null && aircraft.Rows.Length==0,
            "Failed VDL2 validation cannot produce a readable decoded claim or aircraft row");
        body["decode"]!["crc_ok"]=true;
        body["body"]!["kind"]="avlc-rnr";
        body["body"]!["details"]!["params"]=null;
        body["body"]!["details"]!["src"]!["kind"]="ground_icao";
        body["body"]!["details"]!["dst"]!["kind"]="aircraft";
        var busy=Parse();
        check(busy.Packet.Description.StartsWith("Receiver busy") &&
              busy.Identity=="ICAO (VDL2):10340A" && busy.Packet.From=="ABC123",
            "VDL2 link-control messages identify ground senders and retain an addressed aircraft separately");
        body["body"]!["kind"]="atn";
        body["body"]!["details"]!["atn"]=JsonNode.Parse("""
        {"cotp":{"app":{"application":"CPDLC","message":{"elements":[
        {"element":"dM0NULL","phrase":"WILCO","text":"WILCO"},
        {"element":"uM20Altitude","phrase":"CLIMB TO [altitude]","argument_type":"Altitude"}]}}}}
        """);
        var cpdlc=Parse();
        check(cpdlc.Packet.Type=="CPDLC message" && cpdlc.Packet.Description.Contains("WILCO") &&
              cpdlc.Packet.Description.Contains("Partially decoded element CLIMB TO [altitude]"),
            "VDL2 displays decoded CPDLC text and marks elements with missing argument values as partial");
        body["body"]!["kind"]="avlc-future";
        body["body"]!["details"]!["atn"]=null;
        check(Parse().Packet.Description.Contains("VDL2 frame (avlc-future)") &&
              Parse().Packet.Description.Contains("45 payload bytes") &&
              AviationData.Parse("""{"decode":{"crc_ok":true},"body":{"type":"vdl2"}}""",
                  "vdl2",AviationTests.Context)!=null,
            "Unknown or incomplete VDL2 layouts retain a bounded summary and their raw payload");
    }
}
