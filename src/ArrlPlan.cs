using System.Globalization;
namespace MRS;
internal sealed record ArrlEntry(long Low,long High,string Band,string Label,string Note)
{
    public bool IsPoint => Low==High;
}
internal static class ArrlPlan
{
    internal const string Source="ARRL operating recommendations · reviewed 2026-09-29 · https://www.arrl.org/band-plan";
    internal static readonly ArrlEntry[] Entries=Build();
    internal static IEnumerable<ArrlEntry> Visible(double low,double high)=>Entries.Where(x=>x.IsPoint ? x.Low>=low&&x.Low<=high : x.Low<high&&x.High>low);
    private static ArrlEntry[] Build()
    {
        var result=new List<ArrlEntry>(); string band="";
        foreach(string line in Data.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
        {
            if(line.StartsWith('#')){band=line[1..];continue;}
            var fields=line.Split('|');var range=fields[0].Split('-');
            long Hz(string value)=>checked((long)(decimal.Parse(value,CultureInfo.InvariantCulture)*1000000));
            string note="Voluntary operating guide; local coordination can differ. Not a license-privilege map.";
            if(band=="2 m")note+=" ARRL labels this plan proposed; 146.40 MHz can be a repeater input locally.";
            if(band=="23 cm")note+=" Nearby FAA radar may restrict use; regional alternatives overlap.";
            if(band=="13 cm")note+=" 2305–2310 MHz is secondary to WCS; regional use varies.";
            if(band=="9 cm")note+=" No amateur coverage above 3450 MHz. Some legacy pairings are no longer available; consult coordinator. Astronomy segments require coordination.";
            if(band=="60 m")note="ARRL current-page reference. USB dial markers are 1.5 kHz below CW/data centers; segment limits are RF edges. Verify current FCC limits before transmitting.";
            result.Add(new(Hz(range[0]),Hz(range[^1]),band,fields[1],note));
        }
        return result.OrderBy(x=>x.Low).ThenByDescending(x=>x.High).ToArray();
    }
    // MHz endpoints and concise factual labels transcribed from the current ARRL page.
    private const string Data="""
#2200 m
.1357-.1378|Band outline; no detailed subdivision
#630 m
.472-.479|Band outline; no detailed subdivision
#160 m
1.8-2|CW
1.8-1.81|Digital
1.81|QRP CW
1.843-2|Voice / image / wide modes
1.91|QRP SSB
1.995-2|Experiments
1.999-2|Beacons
#80 / 75 m
3.57-3.6|Data / RTTY
3.59|DX data / RTTY
3.79-3.8|DX window
3.845|SSTV
3.885|AM calling
#60 m
5.3305|USB dial (CW/data center 5.332)
5.3465|USB dial (CW/data center 5.348)
5.3515-5.3665|60 m RF segment
5.3715|USB dial (CW/data center 5.373)
5.4035|USB dial (CW/data center 5.405)
#40 m
7.04|DX data / RTTY
7.08-7.125|Data / RTTY
7.171|SSTV
7.29|AM calling
#30 m
10.13-10.14|RTTY
10.14-10.15|Packet
#20 m
14.07-14.095|RTTY
14.095-14.0995|Packet
14.1|Beacon network
14.1005-14.112|Packet
14.23|SSTV
14.286|AM calling
#17 m
18.1-18.105|RTTY
18.105-18.11|Packet
#15 m
21.07-21.11|Data / RTTY
21.34|SSTV
#12 m
24.92-24.925|RTTY
24.925-24.93|Packet
#10 m
28-28.07|CW
28.07-28.15|RTTY
28.15-28.19|CW
28.2-28.3|Beacons
28.3-29.3|Voice
28.68|SSTV
29-29.2|AM
29.3-29.51|Satellite
29.52-29.59|Repeater inputs
29.6|FM simplex
29.61-29.7|Repeater outputs
#6 m
50-50.1|CW / beacons
50.06-50.08|Beacon segment
50.1-50.3|SSB / CW
50.1-50.125|DX window
50.125|SSB calling
50.3-50.6|All modes
50.6-50.8|Nonvoice
50.62|Packet calling
50.8-51|Remote control
51-51.1|Pacific DX
51.12-51.48|Repeater inputs
51.12-51.18|Digital inputs
51.5-51.6|Simplex
51.62-51.98|Repeater outputs
51.62-51.68|Digital outputs
52-52.48|Repeater inputs; exceptions
52.02|FM simplex
52.04|FM simplex
52.2|Test input
52.5-52.98|Repeater outputs; exceptions
52.525|Primary FM simplex
52.54|Secondary FM simplex
52.7|Test output
53-53.48|Repeater inputs; exceptions
53|Remote-base simplex
53.02|Simplex
53.1|Remote control
53.2|Remote control
53.3|Remote control
53.4|Remote control
53.5-53.98|Repeater outputs; exceptions
53.5|Remote control
53.6|Remote control
53.7|Remote control
53.8|Remote control
53.52|Simplex
53.9|Simplex
#2 m
144-144.05|CW moonbounce
144.05-144.1|CW / weak signals
144.1-144.2|SSB weak signals / moonbounce
144.2|SSB calling
144.2-144.275|SSB
144.275-144.3|Beacons
144.3-144.5|Satellite segment
144.5-144.6|Translator inputs
144.6-144.9|FM repeater inputs
144.9-145.1|Weak signals / FM simplex; packet
145.1-145.2|Translator outputs
145.2-145.5|FM repeater outputs
145.5-145.8|Experiments / miscellaneous
145.8-146|Satellite segment
146.01-146.37|Repeater inputs
146.4-146.58|Simplex; local exceptions
146.52|FM simplex calling
146.61-146.97|Repeater outputs
147-147.39|Repeater outputs
147.42-147.57|Simplex
147.6-147.99|Repeater inputs
#1.25 m
222-222.15|Weak signals
222-222.025|Moonbounce
222.05-222.06|Beacons
222.1|SSB / CW calling
222.1-222.15|CW / SSB weak signals
222.15-222.25|Coordinator option
222.25-223.38|FM repeater inputs
223.4-223.52|FM simplex
223.52-223.64|Packet / digital
223.64-223.7|Links / control
223.71-223.85|Coordinator option
223.85-224.98|Repeater outputs
#70 cm
420-426|ATV / links / experiments
421.25|ATV carrier
426-432|ATV simplex
427.25|ATV carrier
432-432.07|Moonbounce
432.07-432.1|CW weak signals
432.1|Calling frequency
432.1-432.3|Weak signals / mixed modes
432.3-432.4|Beacons
432.4-433|Weak signals / mixed modes
433-435|Links
435-438|Satellite
438-444|ATV inputs / links
439.25|ATV carrier
442-445|Repeater local option
445-447|Links / repeaters / simplex local option
446|Simplex calling
447-450|Repeater local option
#33 cm
902-902.075|Inputs or weak signals; regional
902.075-902.1|CW / SSB
902.1|Weak-signal calling; regional
902.1-902.125|CW / SSB
902.125-903|Repeater inputs
903-903.1|Beacons / weak signals
903.1|Weak-signal calling; regional
903.1-903.4|CW / SSB
903.4-909|Mixed modes / links
909-915|Broadband; regional
915-921|Broadband; regional
921-927|Broadband; regional
927-927.075|Repeater outputs
927.075-927.125|FM / digital simplex
927.125-928|Repeater outputs; simplex exceptions
927.5|FM calling; regional alternative possible
#23 cm
1240-1246|ATV 1
1246-1248|Links paired with 1258–1260
1248-1252|Digital
1252-1258|ATV 2
1258-1260|Links paired with 1246–1248
1240-1260|FM ATV regional option
1260-1270|Satellite uplink / experiments / ATV
1270-1276|Inputs paired with 1282–1288
1270-1274|Regional inputs paired with 1290–1294
1276-1282|ATV 3
1282-1288|Outputs paired with 1270–1276
1288-1294|Broadband / ATV
1290-1294|Regional outputs paired with 1270–1274
1294-1295|FM simplex
1294.5|FM calling
1295-1297|Narrow modes
1295-1295.8|Narrow image / experiments
1295.8-1296.08|Moonbounce
1296.08-1296.2|CW / SSB weak signals
1296.1|CW / SSB calling
1296.2-1296.4|Beacons
1296.4-1297|General narrow modes
1297-1300|Digital
#13 cm
2300-2303|Analog / digital paired with 2390–2393
2303-2303.75|Analog / digital paired with 2393–2393.75
2303.75-2304|Weak signals
2304-2304.1|Moonbounce
2304.1-2304.3|Weak signals
2304.1|Weak-signal calling
2304.3-2304.4|Beacons
2304.4-2304.75|Weak signals / narrow FM
2304.75-2305|Analog / digital paired with 2394.75–2395
2305-2310|Analog / digital; secondary to WCS
2390-2393|Analog / digital paired with 2300–2303
2393-2393.75|Analog / digital paired with 2303–2303.75
2393.75-2394.75|Experiments
2394.75-2395|Analog / digital paired with 2304.75–2305
2395-2400|Analog / digital paired with 2305–2310
2400-2410|Satellite
2410-2450|Broadband; regional
2424.1|Japan EME transmit reference
#9 cm
3300-3309|Analog / digital paired with 3430–3439
3309-3310|Experiments
3310-3330|Analog / digital paired with 3410–3430
3330-3332|Experiments
3332-3339|Radio astronomy coordination
3339-3345.8|Analog / digital paired with 3439–3445.8
3345.8-3352.5|Radio astronomy coordination
3352.5-3355|Analog / digital; former pairing unavailable
3355-3357|Experiments
3357-3360|Analog / digital; former pairing unavailable
3360-3400|Broadband
3360-3380|ATV regional option
3400-3410|Satellite
3400-3400.3|Moonbounce
3400.1|EME calling
3400.3-3401|Terrestrial weak signals
3410-3430|Analog / digital paired with 3310–3330
3430-3439|Analog / digital paired with 3300–3309
3439-3445.8|Analog / digital paired with 3339–3345.8
3445.8-3450|Experiments
#5 cm
5650-5670|Satellite uplink
5650-5675|Experiments
5675-5750|Analog / digital paired with 5850–5925
5750-5756|Analog / digital paired with 5820–5826
5756-5759|Analog / digital paired with 5826–5829
5759-5760|Weak signals
5760-5760.1|Moonbounce
5760.1-5760.3|Weak signals
5760.1|Weak-signal calling
5760.3-5760.4|Beacons
5760.4-5761|Weak signals
5761-5775|Experiments
5775-5800|Experiments
5800-5820|Experiments
5820-5826|Analog / digital paired with 5750–5756
5826-5829|Analog / digital paired with 5756–5759
5829-5850|Experiments
5830-5850|Satellite downlink
5850-5925|Analog / digital paired with 5675–5750
#3 cm
10000-10050|Experiments
10050-10100|Analog / digital paired with 10300–10350
10100-10115|Analog / digital paired with 10350–10365
10115-10117|Analog / digital paired with 10365–10367
10117-10120|Experiments
10120-10125|Analog / digital paired with 10370–10375
10125-10200|Broadband paired with 10375–10450
10200-10300|Wideband Gunnplexers
10300-10350|Analog / digital paired with 10050–10100
10350-10365|Analog / digital paired with 10100–10115
10365-10367|Analog / digital paired with 10115–10117
10367-10368.3|Weak signals / narrow FM
10368.1|Weak-signal calling
10368.3-10368.4|Beacons
10368.4-10370|Weak signals / narrow FM
10370-10375|Analog / digital paired with 10120–10125
10375-10450|Broadband paired with 10125–10200
10450-10500|Space / earth / telecommand
#1.2 cm
24000-24250|Band outline; no detailed ARRL subdivision
#6 mm
47000-47200|Band outline; no detailed ARRL subdivision
#4 mm
76000-81000|Band outline; no detailed ARRL subdivision
#2.5 mm
122250-123000|Band outline; no detailed ARRL subdivision
#2 mm
134000-141000|Band outline; no detailed ARRL subdivision
#1 mm
241000-250000|Band outline; no detailed ARRL subdivision
""";
}
