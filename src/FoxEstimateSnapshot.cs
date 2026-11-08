namespace MRS;

internal sealed record FoxEstimateSnapshot(FoxEstimate Result,DateTimeOffset CalculatedUtc,DateTimeOffset? LastMeasurement,int Count)
{
 internal bool Paused(bool logging,DateTimeOffset now)=>logging&&(LastMeasurement==null||now-LastMeasurement>TimeSpan.FromSeconds(12)||now<LastMeasurement.Value.AddSeconds(-1));
 internal bool ClockChanged(DateTimeOffset now)=>now<CalculatedUtc.AddSeconds(-1);
 internal FoxEstimate? Overlay(bool logging,DateTimeOffset now)=>Paused(logging,now)||ClockChanged(now)?null:Result;
 internal string Describe(bool logging,DateTimeOffset now)
 {
  if(ClockChanged(now))return "Estimate unavailable: clock moved backwards; calculate a new snapshot";
  string through=LastMeasurement?.ToLocalTime().ToString("HH:mm:ss")??"unknown";
  if(Paused(logging,now))return $"Estimate paused: no recent comparable measurement · last measured {through}; area hidden until recalculated";
  return $"{Result.State}: {Count} points · measured through {through} · {(logging?"current session snapshot":"historical session snapshot")} · {Result.Detail}";
 }
}
