# Acceptance — 0.33.0-preview1

## Live acceptance status — October 24, 2026

- **Main receiver auto squelch: PASS / user accepted.** User reported "auto squelch pass" while testing 0.34, which includes this feature.
- **Automatic I/Q recording: PASS / user accepted October 24, 2026.** User reported "auto record pass" after the remaining checks were identified. Four previously supplied automatic captures were complete and readable. The feature is accepted; individual edge-case observations were not separately supplied. Later issues will be fixed as reported.
- Railroad overlay acceptance remains pending; expanded railroad references are deferred to the next update.

## New live checks

1. **PASS / user accepted:** Receiver > RF squelch: enable Auto without starting a scan. Confirm the estimated floor settles and threshold follows floor + chosen margin. Toggle spectrum lines, adjust the margin with the wheel, and verify Scan controls remain synchronized. Move manual threshold to override Auto. Retune/change gain and verify recalibration.
2. **Accepted as part of automatic recording:** Recording > Activity-triggered I/Q: choose a folder, arm with short settings, and monitor an intermittent signal. Confirm quiet input creates no files, activity creates a clip, and the quiet tail closes it. Replay to hear/check the beginning before the trigger and the end after activity. Verify capture/channel frequencies and increasing replay time.
3. **Reference check; feature accepted:** Leave a continuous signal: verify maximum duration rotates clips. Change frequency/mode/gain and verify clips do not combine channels. Disarm/Stop and confirm finalization. Replay and reconnect must not automatically rearm recording. Manual I/Q recording must be unavailable while armed.
4. **Reference check; feature accepted:** Use a modest session budget; verify the recorder disarms at the budget without deleting clips. Confirm visible status and counts. Check dropped counts while recording at your normal HackRF rate; disk/CPU/USB hardware acceptance remains pending.
5. Bandplans: select reference/railroad. Check AAR 007 at 160.215000, 073 at 161.205000, 097 at 161.565000, HOT 452.937500 and EOT 457.937500 MHz. Toggle the railroad plan off, reload, and confirm it stays hidden. Edit a label and reload. Use Create missing reference CSVs with an older working folder; existing labels must survive.

## Carry forward

All unfinished checks in ACCEPTANCE-0.32.0-preview1.md remain included: P25 site collection and preservation, site overlays, RX/TX state, DMR/NXDN voice, concurrent CW ID, receiver transitions and workspace layout. ADS-B is user-confirmed working with the HF hardware filter disabled; keep that setting disabled for ADS-B. Automatic I/Q recording and main receiver auto squelch have passed user acceptance. Railroad overlay checks remain pending. Recording edge cases above are retained for reference, not acceptance blockers.

Next pass: protocol setup checks (item 1), aircraft trails and an offline usable aviation basemap (item 5). APRS TX gain/start inconsistency remains deferred.
