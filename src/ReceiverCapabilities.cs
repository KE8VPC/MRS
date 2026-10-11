namespace MRS;

internal sealed record ReceiverCapabilities(string Model, long MinimumHz, long MaximumHz, IReadOnlyList<int> SampleRates, IqSampleFormat Format)
{
    internal bool CanTune(long hz) => hz >= MinimumHz && hz <= MaximumHz;
    internal static ReceiverCapabilities HackRf { get; } = new("HackRF", 1000000, 6000000000,
        Array.AsReadOnly(new[] { 2400000, 9600000, 19200000, 20000000 }), IqSampleFormat.Unsigned8);
    internal static bool SupportsDspRate(int rate) => Rtl.SampleRates.Contains(rate) || HackRf.SampleRates.Contains(rate);
    // Validated choices for the current adapter, not application-wide limits.
    internal static ReceiverCapabilities Rtl { get; } = new("RTL-SDR", 25000000, 1750000000,
        Array.AsReadOnly(new[] { 240000, 960000, 1920000, 3200000 }), IqSampleFormat.Unsigned8);
}

