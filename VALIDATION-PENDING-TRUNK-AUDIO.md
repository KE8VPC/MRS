# Pending P25 trunk audio / log path validation

November 4, 2026. Development/source-only continuation. Published version/ZIPs remain unchanged. No live SDR, GPS, RF transmission, external reports, installation or Git publishing.

## Fix found by the new test

A generated P25 voice terminator released the dedicated voice receiver, after which the control pool could immediately redispatch the same ended call from an already cached grant. The synthetic receiver-window test reproduced that behavior before the fix.

Natural call end, hang and maximum-lease release now retire that assignment from the control model's available-call pool. Unrelated available calls remain eligible. A fresh qualifying control observation can offer the same TG/channel again; this does not permanently ignore the group. NAC/color-code scope checks protect unrelated identities. Manual stop and Skip/Ignore retain their existing separate behavior.

## New evidence

- Independent OP25 clear HDU/LDU1/LDU2 waveform passes through production I/Q downconversion, P25 framing, metadata/identity gating and IMBE synthesis. Exactly 18 voice frames / 17,280 stereo sample frames are recovered, with the assigned frequency and TG retained.
- Wrong TG, wrong NAC, encrypted and unknown-encryption fixture cases produce no followed audio. A subsequent valid call works. Stopping before queued audio is drained discards that old audio.
- Signed I/Q with a +250 kHz capture offset recovers the same 18 voice frames. This tests representation/downconversion, not physical HackRF reception.
- A generated valid P25 TDU/NID through the same I/Q path produces a call-end event and releases the receiver. The pool no longer redispatches it solely from the cached grant.
- Six synthetic receiver-window cases pass through control grant acceptance, automatic pool assignment, synthetic-worker tuning acknowledgement, real voice I/Q processing and TickTrunk's audio/log handling. Exactly one Decoded audio record appears per clear call on both receiver histories and Session logs; none appear for muted cases. TG/frequency/NAC/label/radio/control/voice metadata matches the synthetic assignment.

The receiver-window control observations are constructed decoded grants. Voice RF input uses generated I/Q from the independent OP25 vectors; tuning uses the existing synthetic receiver worker. Received control I/Q/identifier/grant parsing is covered separately by existing regression tests. These checks do not establish an end-to-end live control/voice RF system, USB reliability or speaker audibility.

## Results

Development compile passed with zero errors (existing NU1900 vulnerability-feed warnings remain). Full local suite passed 1,885 assertions, including ten trunk voice-I/Q assertions and three cached-grant retirement assertions. Six-case receiver-window voice/log integration passed, including actual terminator handling and fresh-grant eligibility. Scoped whitespace checks passed. A processed-block diagnostic counter makes asynchronous worker completion explicit for these checks.

Live P25 call completeness, weak/simulcast behavior, control changes, extended USB operation and DMR following remain separate acceptance. Use ACCEPTANCE-PENDING-NEXT.md after the next explicitly requested package. No new release archive was created in this continuation.
