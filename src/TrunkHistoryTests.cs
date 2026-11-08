namespace MRS;
internal static class TrunkHistoryTests
{
 internal static void Run(Action<bool,string> check)
 {
  var now=DateTimeOffset.UtcNow;var call=new TrunkCall("P25",4660,12345,851025000,0,0x293,now,now,"Synthetic received grant");
  using var radio=new TrunkRadio();radio.StartExternal(call,"Synthetic control");
  lock(radio.Sync){var rows=radio.Model.Events.ToArray();check(rows.Count(e=>e.Kind=="Assigned"&&e.Talkgroup==4660&&e.Hz==851025000)==1,"Dedicated StartExternal emits one explicit assignment with TG/frequency");check(rows.Count(e=>e.Kind=="Start")==1,"External assignment does not restart its model twice");check(radio.Model.Status.Contains("assigned voice"),"External model reports voice rather than control monitoring");radio.Model.End(now.AddSeconds(1),"Synthetic terminator");check(radio.Model.Events.Any(e=>e.Kind=="Call end"&&e.Talkgroup==4660&&e.Hz==851025000),"Dedicated call end retains assigned TG and frequency");}
  var row=new TrunkEvent(now,"Tuned","Synthetic",call.Hz,call.Talkgroup,Nac:0x293);check(row.NacDisplay=="0x293","Trunk history NAC uses hexadecimal consistently with P25 identifiers");check((row with{Nac=null}).NacDisplay==""&&(row with{Nac=-1}).NacDisplay=="","Unknown or invalid NAC is left blank instead of appearing as an identifier");
  var follower=new TrunkFollower();follower.Start(new("synthetic","Synthetic control","P25",851012500,[]),now);follower.ObserveP25(Update039Tests.Grant(851012500,call.Hz,123),now);follower.ObserveP25(Update039Tests.Grant(851012500,call.Hz+12500,124),now);
  follower.RetireAvailable(call with{Talkgroup=123,Nac=0x294});check(follower.AvailableCalls(now).Length==2,"A mismatched NAC cannot retire another system's available call");
  follower.RetireAvailable(call with{Talkgroup=123});check(follower.AvailableCalls(now).Length==1&&follower.AvailableCalls(now)[0].Talkgroup==124,"Finished assignment removes only its cached grant, preserving other calls");
  follower.ObserveP25(Update039Tests.Grant(851012500,call.Hz,123),now.AddMilliseconds(1));check(follower.AvailableCalls(now.AddMilliseconds(2)).Any(c=>c.Talkgroup==123),"A fresh valid grant permits the same talkgroup/channel to be followed again");
  check(follower.AllowsTalkgroup(123)&&!follower.AllowsTalkgroup(0),"Lease eligibility accepts positive unfiltered groups only");
  follower.Allowed=[124];check(!follower.AllowsTalkgroup(123)&&follower.AllowsTalkgroup(124),"Current allow list applies to existing lease eligibility");
  follower.Allowed.Clear();follower.Hold=123;check(follower.AllowsTalkgroup(123)&&!follower.AllowsTalkgroup(124),"Current hold applies to existing lease eligibility");
  follower.Ignored.Add(123);check(!follower.AllowsTalkgroup(123),"Ignore takes precedence over a held talkgroup");
  follower.Hold=null;follower.Ignored.Clear();check(follower.AllowsTalkgroup(123)&&follower.AllowsTalkgroup(124),"Clearing filters restores eligibility without a receiver pause");
  check(radio.AudioFrames==0,"Assignment/tune events do not claim decoded audio");
 }
}
