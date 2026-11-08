using System.IO;
using System.Text.Json;
namespace MRS;
internal static class WeakTimingTests
{
 internal static void Run(Action<bool,string> check)
 {
  static byte[] Request(DateTimeOffset t){var b=new byte[48];b[0]=0x23;NistTime.WriteTimestamp(b.AsSpan(40),t);return b;}
  static byte[] Reply(byte[] q,DateTimeOffset t2,DateTimeOffset t3){var b=new byte[48];b[0]=0x24;b[1]=1;q.AsSpan(40,8).CopyTo(b.AsSpan(24));NistTime.WriteTimestamp(b.AsSpan(32),t2);NistTime.WriteTimestamp(b.AsSpan(40),t3);return b;}
  var t=DateTimeOffset.Parse("2026-10-29T12:00:00Z");var request=Request(t);var response=Reply(request,t.AddSeconds(.270),t.AddSeconds(.275));var reading=NistTime.Parse(response,request,t,t.AddSeconds(.045));
  check(Math.Abs(reading.OffsetSeconds-.25)<.00001&&Math.Abs(reading.DelaySeconds-.04)<.00001,"NTP four-timestamp calculation removes server processing and estimates UTC offset");
  check(!(reading with{Stamp=System.Diagnostics.Stopwatch.GetTimestamp()-(long)(3601.0*System.Diagnostics.Stopwatch.Frequency)}).Fresh,"Stale NIST reference expires instead of claiming fresh synchronization");
  check(reading.UncertaintySeconds>=.0199&&reading.Fresh,"NTP reference exposes delay uncertainty and fresh age");
  bool Reject(byte[] b){try{NistTime.Parse(b,request,t,t.AddSeconds(.045));return false;}catch(IOException){return true;}}
  check(Reject(response[..40]),"Truncated NTP packet rejected");
  var bad=(byte[])response.Clone();bad[0]|=0xc0;check(Reject(bad),"Unsynchronized NTP server rejected");
  bad=(byte[])response.Clone();bad[1]=0;check(Reject(bad),"NTP Kiss-o'-Death/rate-limit response rejected");
  bad=(byte[])response.Clone();bad[24]^=1;check(Reject(bad),"Unmatched NTP originate timestamp rejected");
  bad=(byte[])response.Clone();bad[0]=0x23;check(Reject(bad),"NTP client-mode packet cannot be accepted as server time");
  bad=(byte[])response.Clone();Array.Clear(bad,32,8);check(Reject(bad),"Missing NTP receive timestamp rejected");
  bad=(byte[])response.Clone();bad[8]=0x10;check(Reject(bad),"Excessive NTP root dispersion rejected");
  var future=DateTimeOffset.Parse("2040-01-01T00:00:00Z");var q=Request(future);var r=NistTime.Parse(Reply(q,future.AddSeconds(.27),future.AddSeconds(.275)),q,future,future.AddSeconds(.045));check(Math.Abs(r.OffsetSeconds-.25)<.00001,"NTP era rollover after 2036 resolves near the client date");
  PacketObservation Row(string station,long slot,double error,double correction=1,string protocol="FT8",bool valid=true)=>new(DateTimeOffset.UtcNow,"Generated timing",true,14074000,0,"test","","","","test","",null,"",protocol,valid,SourceKey:"timing-a",DecoderJson:JsonSerializer.Serialize(new{text="CQ "+station+" FN42",dt_error=error,mrs_slot=slot,mrs_correction=correction}));
  var tracker=new RxTimingTracker();tracker.Observe(Row("K1ABC",1,.3));check(tracker.Estimate() is {Stations:1,Samples:1} single&&!single.Confidence.StartsWith("Ready"),"Single received station cannot authorize automatic timing");
  tracker.Observe(Row("K1ABC",1,.3));check(tracker.Estimate()!.Samples==1,"Duplicate station in the same slot does not inflate timing evidence");
  foreach(string call in new[]{"K1ABC","W2XYZ","N3TEST"})foreach(long slot in new long[]{1,2})tracker.Observe(Row(call,slot,.3));
  var e=tracker.Estimate()!;check(e.Stations==3&&e.Samples==6&&e.Slots==2&&e.Confidence.StartsWith("Ready")&&Math.Abs(e.Correction-.7)<1e-8,"Multiple CRC-valid stations and slots establish signed receive timing consensus");
  tracker.Observe(Row("K4OUT",2,1.8));check(tracker.Estimate() is {Stations:3} robust&&Math.Abs(robust.Correction-.7)<1e-8,"Outlying station clock is excluded from receive timing");
  tracker.Observe(Row("K5BAD",3,.3,protocol:"WSPR"));check(tracker.Estimate()!.Stations==3,"WSPR no-CRC evidence is excluded from FT timing");tracker.Observe(Row("K6BAD",3,.3,valid:false));check(tracker.Estimate()!.Stations==3,"Invalid FT decodes cannot update timing");tracker.Observe(Row("K7NEW",3,.2) with{SourceKey="timing-b"});check(tracker.Estimate() is {Stations:1},"Changing receiver/source discards old timing evidence");tracker.Reset();check(tracker.Estimate()==null,"Manual timing reset removes station estimates");
  using(var pipeline=new RadioDataPipeline()){pipeline.Configure("FT4");using var wav=new NAudio.Wave.WaveFileReader(Path.Combine(AppContext.BaseDirectory,"Docs","Samples","synthetic-ft4.wav"));var source=NAudio.Wave.WaveExtensionMethods.ToSampleProvider(wav);float[] samples=new float[90000];int count=source.Read(samples,0,samples.Length);int prefix=86400;for(int at=0;at<480000;at+=4800){double[] values=new double[4800];for(int n=0;n<4800;n++){int index=(at+n-prefix)/4;if(at+n>=prefix&&index<count)values[n]=samples[index]*32768;}pipeline.Feed(values,new("late-frame","Generated late FT4",true,14080000),at/48000.0);Thread.Sleep(2);}pipeline.Complete();var rows=new List<PacketObservation>();var until=DateTime.UtcNow.AddSeconds(20);do{rows.AddRange(pipeline.Drain());if(rows.Any(r=>r.Information.Contains("CQ K1ABC FN42")))break;Thread.Sleep(30);}while(DateTime.UtcNow<until);check(rows.Any(r=>r.Information.Contains("CQ K1ABC FN42")&&r.IsValidated),"Overlapping FT4 window decodes a transmission extending beyond the ordinary slot boundary");long epoch=pipeline.TimingEpoch;pipeline.SetTimeOffset(.25);check(pipeline.TimingEpoch>epoch,"Pending timing corrections invalidate prior-window estimator epochs");pipeline.Feed(new double[4800],new("late-frame","Generated late FT4",true,14080000),10);check(Math.Abs(pipeline.AppliedOffset-.25)<1e-9,"Decoder-only correction is applied after safely discarding incomplete acquisition");}
  using(var clock=new RadioDataPipeline()){bool jumped=false;clock.ClockJump+=()=>jumped=true;clock.Configure("FT8");var ctx=new PacketContext("clock-jump","Generated clock test",false,14074000);clock.Feed(new double[4800],ctx,0,true);long before=clock.TimingEpoch;clock.Feed(new double[4800],ctx,2,true);check(jumped&&clock.TimingEpoch>before&&clock.Status.Contains("Clock/source discontinuity"),"Live clock jump discards acquisition, invalidates old timing evidence and remains visible");}
 }
}
