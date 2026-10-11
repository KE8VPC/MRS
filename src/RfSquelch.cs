namespace MRS;

// One instance per reception session. Decisions use filtered channel I/Q power,
// never demodulated speech amplitude. All durations follow processed sample time.
internal sealed class RfSquelch
{
    internal static bool RestoreEnabled(bool? enabled, float legacyThreshold) => enabled ?? legacyThreshold > -120;
    private double belowSeconds;
    internal bool IsOpen { get; private set; }
    internal bool Update(double channelDb, double thresholdDb, double seconds)
    {
        if (!double.IsFinite(channelDb) || !double.IsFinite(thresholdDb) || !double.IsFinite(seconds) || seconds <= 0)
        { IsOpen = false; belowSeconds = 0; return false; }
        if (thresholdDb <= -120) { IsOpen = true; belowSeconds = 0; return true; }
        if (!IsOpen)
        {
            if (channelDb >= thresholdDb) { IsOpen = true; belowSeconds = 0; }
        }
        else if (channelDb < thresholdDb - 3)
        {
            belowSeconds += seconds;
            if (belowSeconds >= .2) { IsOpen = false; belowSeconds = 0; }
        }
        else belowSeconds = 0;
        return IsOpen;
    }
}
