# Trunk following setup and troubleshooting
Context: Trunk following
Keywords: walkthrough P25 DMR Cap+ Connect Plus Tier III control voice mapping NAC no audio

## P25 with dedicated voice SDR
Start the control SDR on a known Phase I C4FM control channel in NFM with roughly 16 kHz filter. Open Assign control / voice receivers, save that workspace as Trunk control, select/start another SDR and check it as a voice receiver. Save assignments. Return to the control's Trunk following tab, create a named P25 profile and enter its control frequency. Save profile and Start following.

Wait for received identifier tables and supported group grants. Unknown channel frequencies stay unresolved until a valid bandplan resolves them. Verify grant MHz/TG/NAC against Channel activity. The voice SDR should tune while control capture stays unchanged. Audio is gated on supported clear voice and matching identity; a visible grant does not prove audio was heard. Use Allow, Ignore and Hold filters deliberately. Skip temporarily suppresses the group; Ignore updates the control's rules. Hang/maximum timers bound a lease.

## DMR carrier maps
Choose Capacity Plus, Connect Plus or DMR Tier III according to actual decoded signaling. Enter or import the correct site map. Save a profile. Import DSDPlus map lets you choose a historical system/site, retains OTA evidence and imports carriers unconfirmed. Missing or conflicting frequencies cannot be inferred from CC or TG alone. Cap+ carrier 1 has slots/LSNs 1 and 2, carrier 2 has 3 and 4. Con+/Tier III numbering conversion follows its imported format rather than reusing a Cap+ number blindly.

Keep Require repeat / map confirmation enabled for normal use. Disabling it allows supported single CRC-valid observations and entered candidate maps; it does not make them verified or bypass identity/encryption checks. Expected network/site on Con+/Tier III must match decoding when configured. A shared color code does not identify a system.

## Diagnose a silent assignment
Check control status, available live voice SDR, supported RF coverage, full filter fit and paused state. Resume voice pool after manual tuning or an assignment failure. Check Allow/Ignore/Hold, current slot, full voice LC, encryption state and map frequency. A voice receiver should show its owning control and followed frequency. A failed tune pauses that receiver for review; it must not silently keep playing an old call.

Independent/single-receiver mode is different: control and voice can coexist only inside usable capture, unless Allow hardware retunes permits leaving control temporarily. Dedicated control mode instead dispatches to voice receivers; starting only the control is insufficient. Multiple physical devices add USB/CPU load. Test one voice receiver first, then add others.

## Supported limits
Phase I C4FM and supported mapped DMR group grants only. No Phase II/CQPSK, private/data/absolute-MBC, LTR or NXDN trunk following. Missing late-entry identity can leave a call muted. Test P25, Cap+, Con+ and Tier III separately; a passing protocol is not evidence for all others. Real control/rest changes, weak RF and USB recovery need live acceptance.
