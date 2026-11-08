namespace MRS;

// Per-assignment identity gate. Color code learned from a validated matching
// voice LC is retained until the next assignment, not inferred from signal power.
internal enum DmrFollowAction{Ignored,HeaderAccepted,HeaderRejected,Terminator}
internal sealed class DmrFollowIdentity(TrunkCall call)
{
 internal bool Confirmed{get;private set;}
 internal int? ColorCode{get;private set;}=call.ColorCode;
 internal DmrFollowAction Observe(DmrObservation observation)
 {
  if(!observation.LinkValid||observation.SlotCorrections>1||observation.ColorCode is <0 or >15||observation.Slot!=call.Slot)return DmrFollowAction.Ignored;
  bool matches=observation.Group&&observation.Destination==call.Talkgroup&&(!ColorCode.HasValue||ColorCode==observation.ColorCode);
  if(observation.DataType==1){Confirmed=matches;if(matches)ColorCode??=observation.ColorCode;return matches?DmrFollowAction.HeaderAccepted:DmrFollowAction.HeaderRejected;}
  return observation.DataType==2&&matches&&ColorCode.HasValue?DmrFollowAction.Terminator:DmrFollowAction.Ignored;
 }
}
