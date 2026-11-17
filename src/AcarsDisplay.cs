using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MRS;

internal static class AcarsDisplay
{
    private const string CoordinatePair = @"([NS])[ \t]*([0-9]{1,2}\.[0-9]{3})[ \t]*";
    private const string Longitude = @"([EW])[ \t]*([0-9]{1,3}\.[0-9]{3})";
    // Match the observed layouts, including the coordinate field's position.
    // Other messages under the same label can carry entirely different data.
    private static readonly Regex Label16 = new(
        @"\A[0-9]{6},[0-9]{5},[0-9]{4},[ \t]*[0-9]{1,3},[ \t]*" +
        CoordinatePair + @"[ \t]+" + Longitude + @"[ \t]*\z", RegexOptions.CultureInvariant);
    private static readonly Regex Label30 = new(
        @"\A[0-9]{1,3},[A-Z0-9]{2},[0-9]{4},[0-9]+,[A-Z]{4},[ \t]*" +
        CoordinatePair + @",[ \t]*" + Longitude + @"(?=[ \t]*(?:,|\z))", RegexOptions.CultureInvariant);

    internal static (double? Latitude, double? Longitude) Position(
        JsonElement body, string label, string text)
    {
        string block = AviationData.Str(body, "block_id");
        if (block.Length != 1 || !char.IsAsciiDigit(block[0]))
            return (null, null);

        var position = AviationData.Obj(AviationData.Obj(body, "app"), "position");
        var lat = AviationData.Num(position, "latitude");
        var lon = AviationData.Num(position, "longitude");
        if (AviationData.Position(lat, lon))
            return (lat, lon);

        if (AviationData.Obj(body, "more_to_come").ValueKind != JsonValueKind.False)
            return (null, null);
        var pattern = label switch { "16" => Label16, "30" => Label30, _ => null };
        var match = pattern?.Match(text);
        if (match is not { Success: true })
            return (null, null);
        lat = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        lon = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
        if (match.Groups[1].Value == "S") lat = -lat;
        if (match.Groups[3].Value == "W") lon = -lon;
        return AviationData.Position(lat, lon) ? (lat, lon) : (null, null);
    }

    internal static string Summary(JsonElement body, string label, string text, double? lat, double? lon)
    {
        var app = AviationData.Obj(body, "app");
        var nested = AviationData.Obj(app, "app");
        var application = nested.ValueKind == JsonValueKind.Object ? nested : app;
        string type = AviationData.Str(application, "app") switch
        {
            "adsc" => "ADS-C report",
            "cpdlc" => "CPDLC message",
            "flight_plan" => "Flight plan",
            "media_advisory" => "Datalink advisory",
            "cfb" => "Maintenance report",
            "ohma" => "Aircraft health report",
            _ => AviationData.Str(application, "description")
        };
        if (label == "H1" && (AviationData.Str(body, "sublabel") == "DF" ||
            text.StartsWith("#DFB", StringComparison.Ordinal)))
            type = "Flight data acquisition report";
        if (type.Length == 0)
            type = lat.HasValue ? "Position report" : "ACARS message";

        var fields = new List<string> { type };
        string flight = AviationData.Str(body, "flight");
        if (flight.Length > 0) fields.Add("Flight " + flight);
        foreach (var (key, name) in new[] { ("depa", "From"), ("dsta", "To") })
        {
            string value = AviationData.Str(app, key);
            if (value.Length > 0) fields.Add(name + " " + value);
        }
        foreach (var (key, name) in new[] { ("gtout", "Gate out"), ("wloff", "Wheels off"),
            ("wlin", "Wheels on"), ("gtin", "Gate in"), ("eta", "ETA") })
        {
            string value = AviationData.Str(app, key);
            if (value.Length == 4 && value.All(char.IsAsciiDigit))
                fields.Add($"{name} {value[..2]}:{value[2..]} UTC");
        }
        if (lat is double latitude && lon is double longitude)
            fields.Add(FormattableString.Invariant($"{latitude:0.#####}, {longitude:0.#####}"));
        if (text.Contains("LDGWDLCK,FLAP,SLAT", StringComparison.Ordinal))
            fields.Add("Parameters mentioned: landing gear downlock, flaps, slats");
        if (type == "ACARS message" && text.Trim().Length > 0)
        {
            string preview = text.Trim();
            fields.Add(preview.Length <= 120 ? preview : preview[..120] + "…");
        }
        if (AviationData.Obj(body, "more_to_come").ValueKind == JsonValueKind.True)
            fields.Add("More blocks follow");
        return string.Join(" · ", fields);
    }
}
