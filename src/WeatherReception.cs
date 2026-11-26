using System.Globalization;

namespace MRS;

internal sealed record WeatherFaxStation(string Call, int[] StartsUtc, string ScheduleUrl)
{
    internal static readonly WeatherFaxStation[] All =
    [
        new("NMF", [230,745,1400,1720,1900], "https://ocean.weather.gov/shtml/atlsch.php"),
        new("NMG", [0,600,1200,1800], "https://tgftp.nws.noaa.gov/fax/hfgulf.txt"),
        new("NOJ", [340,950,1540,2150], "https://ocean.weather.gov/shtml/AKpacsch.php"),
        new("NMC", [140,655,1120,1400,1840,2320], "https://ocean.weather.gov/shtml/pacsch.php"),
        new("KVM70", [519,1719], "https://tgftp.nws.noaa.gov/fax/hfhi.txt")
    ];

    internal bool AvailableAt(long dialHz, TimeSpan utc)
    {
        int minutes = (int)utc.TotalMinutes;
        return (Call,dialHz + 1900) switch
        {
            ("NMF",4235000) => minutes >= 150 && minutes < 640,
            ("NMF",12750000) => minutes >= 840 && minutes < 1360,
            ("NMG",17146400) => minutes >= 720 && minutes < 1246,
            ("NMC",4346000) => minutes >= 100 && minutes < 969,
            ("NMC",22527000) => minutes >= 1120 && minutes < 1437,
            ("KVM70",9982500) => minutes >= 319 && minutes < 957,
            ("KVM70",16135000) => minutes >= 1039 || minutes < 237,
            _ => true
        };
    }

    internal static DateTimeOffset NextTune(DateTimeOffset now, TimeSpan broadcastUtc)
    {
        var earliestBroadcast = now.ToUniversalTime().AddSeconds(30);
        var broadcast = new DateTimeOffset(earliestBroadcast.Date + broadcastUtc, TimeSpan.Zero);
        if (broadcast <= earliestBroadcast) broadcast = broadcast.AddDays(1);
        return broadcast.AddSeconds(-30);
    }

    internal static string FormatTime(int hhmm) =>
        FormattableString.Invariant($"{hhmm / 100:00}:{hhmm % 100:00}");

    internal static bool TryTime(string text,out TimeSpan utc) =>
        TimeSpan.TryParseExact(text.Trim(),@"hh\:mm",CultureInfo.InvariantCulture,out utc);
}

internal sealed class WeatherAlarmTone
{
    private const int Block = 4800;
    private static readonly double[] coefficients = new[] { 1020, 1050, 1080 }
        .Select(hz => 2 * Math.Cos(2 * Math.PI * hz / 48000)).ToArray();
    private readonly double[] first = new double[3], second = new double[3];
    private int samples, consecutive;
    private double energy;

    internal bool Feed(ReadOnlySpan<double> pcm)
    {
        bool detected = false;
        foreach (double value in pcm)
        {
            energy += value * value;
            for (int i = 0; i < first.Length; i++)
            {
                double next = value + coefficients[i] * first[i] - second[i];
                second[i] = first[i];
                first[i] = next;
            }
            if (++samples != Block) continue;
            double Power(int n) => first[n] * first[n] + second[n] * second[n] - coefficients[n] * first[n] * second[n];
            double tone = Power(1);
            bool match = energy > 0 && tone > Block * energy * .32 && tone > 3 * Math.Max(Power(0),Power(2));
            if (!match) consecutive = 0;
            else if (consecutive < 20 && ++consecutive == 20) detected = true;
            samples = 0;
            energy = 0;
            Array.Clear(first);
            Array.Clear(second);
        }
        return detected;
    }
}
