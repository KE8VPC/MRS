namespace MRS;
internal enum FoxGpsConnection { Off, Opening, Connected, Error }
internal sealed record FoxIndicator(string Symbol, string Label, string Tone, string Detail);
internal static class FoxIndicators
{
    internal static FoxIndicator Gps(bool manual, FoxGpsConnection connection, FoxFix? fix, DateTimeOffset now)
    {
        if (manual) return new("◆", fix == null ? "SET MANUAL POSITION" : "MANUAL POSITION", "amber", "Fixed position; movement is not GPS tracked.");
        if (connection == FoxGpsConnection.Off) return new("○", "GPS OFF", "grey", "Connect GPS and wait for a fresh fix.");
        if (connection == FoxGpsConnection.Error) return new("✕", "GPS ERROR", "red", "GPS connection failed; no position recording.");
        if (connection == FoxGpsConnection.Opening) return new("◷", "CONNECTING GPS", "amber", "Opening the configured port; not a valid fix yet.");
        if (fix == null) return new("◷", "WAITING FOR FIX", "amber", "Port connected; incoming NMEA alone does not establish a position.");
        return fix.Fresh(now) ? new("✓", "GPS FIX", "green", "Fresh GPS position; check recording status separately.")
            : new("✕", "GPS FIX STALE", "red", "No recent position; new geolocated points cannot be recorded.");
    }
    internal static FoxIndicator Recording(bool running, bool liveRf, FoxFix? fix, DateTimeOffset? lastPoint, int interval, DateTimeOffset now)
    {
        if (!running) return new("■", "NOT RECORDING", "grey", "Start Fox Hunt log to save measurements.");
        if (fix == null || !fix.Fresh(now)) return new("Ⅱ", "NO GPS POINTS", "red", "Log is armed but no fresh position is available.");
        if (!liveRf) return new("Ⅱ", "NO LIVE RF", "red", "No live receiver measurements available.");
        if (lastPoint == null || lastPoint > now || now - lastPoint > TimeSpan.FromSeconds(Math.Max(4, interval * 2 + 2)))
            return new("◷", "WAITING FOR RF POINT", "amber", "No recent point saved; check signal/squelch and measurement status.");
        return new("●", "RECORDING POINTS", "green", "Recent signal and position measurements are being saved.");
    }
    internal static FoxIndicator Trend(FoxTrend trend, bool running)
    {
        if (!running || trend.State == "Paused") return new("Ⅱ", "PAUSED", "grey", "No current trend; retained measurements may be historical.");
        if (trend.State == "Uncertain") return new("?", "UNCERTAIN", "amber", trend.Detail);
        if (trend.Change is >= 2) return new("▲", "STRONGER", "green", trend.Detail);
        if (trend.Change is <= -2) return new("▼", "WEAKER", "red", trend.Detail);
        if (trend.Change != null) return new("▬", "STEADY", "neutral", trend.Detail);
        return new("◷", "COLLECTING", "amber", trend.Detail);
    }
}
