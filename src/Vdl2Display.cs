using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MRS;

internal sealed record Vdl2Presentation(string From, string To, string Aircraft, string AddressType,
    string Type, string Summary);

internal static class Vdl2Display
{
    internal static Vdl2Presentation Read(JsonElement body)
    {
        var details = AviationData.Obj(body, "details");
        var source = AviationData.Obj(details, "src");
        var destination = AviationData.Obj(details, "dst");
        string from = Address(source), to = Address(destination);
        string aircraft = "", addressType = "";
        if (AviationData.Str(source, "kind") == "aircraft")
        {
            aircraft = from;
            addressType = "ICAO (VDL2)";
        }
        else if (AviationData.Str(destination, "kind") == "aircraft")
        {
            aircraft = to;
            addressType = "ICAO (VDL2)";
        }
        string kind = AviationData.Str(body, "kind");
        string type = kind switch
        {
            "xid" => "Data-link settings",
            "avlc-rr" => "Receive acknowledgement",
            "avlc-rnr" => "Receiver busy",
            "avlc-rej" or "avlc-srej" => "Retransmission request",
            "avlc-disc" => "Disconnect request",
            "avlc-dm" => "Link disconnected",
            "avlc-ua" => "Link acknowledgement",
            "avlc-sabme" => "Link setup",
            "avlc-frmr" => "Rejected frame",
            "avlc-test" => "Link test",
            "atn" => "Aeronautical network data",
            "avlc-ui" or "avlc-i" => "Data-link frame",
            _ => "VDL2 frame" + (kind.Length > 0 ? " (" + kind + ")" : "")
        };
        var fields = new List<string> { type };
        if (from.Length > 0) fields.Add(Endpoint(source) + " → " + Endpoint(destination));

        if (kind == "xid")
        {
            var parameters = AviationData.Obj(details, "params");
            if (parameters.ValueKind == JsonValueKind.Array)
            {
                foreach (var parameter in parameters.EnumerateArray().Take(64))
                {
                    string name = AviationData.Str(parameter, "name");
                    string text = AviationData.Str(parameter, "text");
                    if (name is "destination-airport" or "nearest-airport-id" &&
                        Regex.IsMatch(text, @"\A[A-Z0-9]{4}\z", RegexOptions.CultureInvariant))
                        fields.Add((name == "destination-airport" ? "Reported destination " : "Nearest airport ") + text);
                    if (name == "autotune-frequency" && AviationData.Num(parameter, "freq_mhz") is double mhz &&
                        mhz is >= 117.975 and <= 137)
                        fields.Add("Advertised tuning " + mhz.ToString("0.####", CultureInfo.InvariantCulture) + " MHz");
                    if (name == "frequency-support-list")
                    {
                        var frequencies = AviationData.Obj(parameter, "freq_support");
                        if (frequencies.ValueKind == JsonValueKind.Array)
                        {
                            var supported = frequencies.EnumerateArray().Take(8)
                                .Select(p => AviationData.Num(p, "freq_mhz"))
                                .Where(v => v is >= 117.975 and <= 137)
                                .Select(v => v!.Value.ToString("0.####", CultureInfo.InvariantCulture));
                            string list = string.Join(", ", supported);
                            if (list.Length > 0) fields.Add("Advertised channels " + list + " MHz");
                        }
                    }
                }
            }
        }

        var network = AviationData.Obj(details, "atn");
        var transport = AviationData.Obj(network, "cotp");
        var application = AviationData.Obj(transport, "app");
        string applicationName = AviationData.Str(application, "application");
        if (applicationName == "CPDLC")
        {
            type = "CPDLC message";
            fields[0] = type;
            var message = AviationData.Obj(application, "message");
            var elements = AviationData.Obj(message, "elements");
            if (elements.ValueKind == JsonValueKind.Array)
                foreach (var element in elements.EnumerateArray().Take(5))
                {
                    string text = AviationData.Str(element, "text");
                    if (text.Length > 0) fields.Add(text);
                    else
                    {
                        string phrase = AviationData.Str(element, "phrase");
                        fields.Add(phrase.Length > 0 ? "Partially decoded element " + phrase : "Undecoded CPDLC element");
                    }
                }
            if (elements.ValueKind != JsonValueKind.Array)
                fields.Add("Control exchange " + AviationData.Str(application, "pdu"));
        }
        else if (kind == "atn")
        {
            string protocol = AviationData.Str(details, "protocol");
            if (protocol.Length > 0) fields.Add(protocol);
            if (AviationData.Obj(network, "reassembling").ValueKind == JsonValueKind.True ||
                AviationData.Obj(transport, "tsdu_reassembling").ValueKind == JsonValueKind.True)
                fields.Add("Waiting for remaining segments");
            if (applicationName.Length > 0) fields.Add(applicationName);
        }

        var rejection = AviationData.Obj(details, "frmr");
        foreach (var (key, text) in new[]
        {
            ("w_invalid_control", "Unsupported control field"),
            ("x_info_not_allowed", "Unexpected information field"),
            ("y_info_too_long", "Information field too long"),
            ("z_invalid_nr", "Invalid receive sequence")
        })
            if (AviationData.Obj(rejection, key).ValueKind == JsonValueKind.True) fields.Add(text);
        if (AviationData.Num(details, "info_len") is double length && length >= 0)
            fields.Add(length.ToString("0", CultureInfo.InvariantCulture) + " payload bytes");
        string summary=string.Join(" · ",fields);
        if(summary.Length>1400)summary=summary[..1400]+"…";
        return new(from, to, aircraft, addressType, type.Length>128?type[..128]:type, summary);
    }

    private static string Address(JsonElement endpoint)
    {
        string value = AviationData.Str(endpoint, "addr");
        return Regex.IsMatch(value, @"\A[0-9A-Fa-f]{6}\z") ? value.ToUpperInvariant() : "";
    }

    private static string Endpoint(JsonElement endpoint)
    {
        string address = Address(endpoint);
        string kind = AviationData.Str(endpoint, "kind") switch
        {
            "aircraft" => "Aircraft",
            "ground_icao" or "ground_delegated" => "Ground station",
            "all_stations" => "All stations",
            _ => "Station"
        };
        return address.Length > 0 ? kind + " " + address : kind + " unknown";
    }
}
