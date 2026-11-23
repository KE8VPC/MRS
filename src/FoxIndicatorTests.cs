namespace MRS;
internal static class FoxIndicatorTests
{
    internal static void Run(Action<bool,string> check)
    {
        var now = DateTimeOffset.UtcNow;
        var fix = new FoxFix(40,-84,now,"Synthetic GPS");
        check(FoxIndicators.Gps(false,FoxGpsConnection.Off,null,now).Tone=="grey", "Fox GPS not started is explicitly off");
        check(FoxIndicators.Gps(false,FoxGpsConnection.Connected,null,now).Label=="WAITING FOR FIX", "Fox connected port without fix is not green");
        check(FoxIndicators.Gps(false,FoxGpsConnection.Connected,fix,now).Tone=="green", "Fox fresh GPS fix is ready independently of logging");
        check(FoxIndicators.Gps(false,FoxGpsConnection.Connected,fix,now.AddSeconds(4)).Tone=="red", "Fox stale GPS fix is a visible fault");
        check(FoxIndicators.Gps(false,FoxGpsConnection.Error,fix,now).Tone=="red", "Fox GPS port error overrides a retained fix");
        check(FoxIndicators.Gps(true,FoxGpsConnection.Off,fix with{Source="Manual stationary"},now).Label=="MANUAL POSITION", "Fox manual position never masquerades as GPS lock");
        check(FoxIndicators.Recording(false,true,fix,now,1,now).Label=="NOT RECORDING", "Fox fresh GPS does not imply recording is active");
        check(FoxIndicators.Recording(true,true,null,now,1,now).Label=="NO GPS POINTS", "Fox armed log without fresh GPS warns no points");
        check(FoxIndicators.Recording(true,false,fix,now,1,now).Label=="NO LIVE RF", "Fox log warns when RF source is unavailable");
        check(FoxIndicators.Recording(true,true,fix,null,1,now).Tone=="amber", "Fox armed log awaiting its first sample is not recording green");
        check(FoxIndicators.Recording(true,true,fix,now.AddSeconds(-10),1,now).Tone=="amber", "Fox stale saved point does not imply ongoing recording");
        check(FoxIndicators.Recording(true,true,fix,now,1,now).Tone=="green", "Fox recent saved point with valid inputs shows recording");
        check(FoxIndicators.Trend(new("↑ Stronger","Synthetic",3),false).Label=="PAUSED", "Fox stopped trend is paused even with recent retained readings");
        check(FoxIndicators.Trend(new("↑ Stronger","Synthetic",2),true).Label=="STRONGER", "Fox stronger indicator includes threshold boundary");
        check(FoxIndicators.Trend(new("↓ Weaker","Synthetic",-2),true).Label=="WEAKER", "Fox weaker indicator includes threshold boundary");
        check(FoxIndicators.Trend(new("→ Steady","Synthetic",0),true).Label=="STEADY", "Fox steady indicator is distinct from paused");
        check(FoxIndicators.Trend(new("Uncertain","Noisy"),true).Label=="UNCERTAIN", "Fox unreliable trend never shows directional strength cue");
    }
}
