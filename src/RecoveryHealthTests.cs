namespace MRS;
internal static class RecoveryHealthTests
{
    internal static void Run(Action<bool,string> check)
    {
        var health = new ReceiverRecovery.Health();
        check(!health.Sustained, "Reconnect budget requires received I/Q, not an opened handle");
        for (long t = 0; t < 30000; t += 1000) health.Observe(t);
        check(!health.Sustained, "Short repeated reconnect runs cannot reset retry budget");
        health.Observe(30000);
        check(health.Sustained, "Thirty seconds of continuous I/Q earns a fresh reconnect budget");
        health.Observe(33000);
        check(!health.Sustained, "I/Q gap restarts recovery health qualification");
        for (long t = 34000; t <= 63000; t += 1000) health.Observe(t);
        check(health.Sustained, "Continuous reception after a gap can qualify again");
        health.Observe(1);
        check(!health.Sustained, "Backwards monotonic test time cannot falsely qualify health");
        health.Reset();
        check(!health.Sustained, "Stop/session reset removes previous recovery evidence");
        var frame = new IqFrame(0,1090250000,2400000,new byte[32768],1750000);
        string? message = AviationPipeline.Check("adsb",1090000000,frame);
        check(message != null && message.Contains("even with the HF filter disabled"), "Reduced-rate ADS-B advice does not blame an enabled HF filter");
        check(AviationPipeline.Check("adsb",1090000000,frame with{Rate=9600000,UsableBandwidthHz=9000000})==null, "Wide HackRF ADS-B capture still passes setup");
    }
}
