# Acceptance — 0.34.0-preview1

## Status — October 24, 2026

**Aviation map and trails: COMPLETE / user accepted.** The user closed this acceptance after live HackRF ADS-B testing and will report any later issues for correction. Confirmed evidence: decoded messages without reported drops, aircraft identities/callsigns, altitude/speed, live coordinates, aircraft dots on the offline map with Online street map unchecked, zoom, and a selected aircraft highlighted with its trail. SWA949 and EJA913 were visible in the supplied live screenshots.

Remaining map edge cases below are retained as reference checks, not blockers to this accepted feature. Individual expiry timers, network-disconnection/cache-empty behavior, every toggle and replay edge case were not all separately demonstrated in the live session; synthetic coverage is recorded in VALIDATION-0.34.0-preview1.md.

**Main receiver auto squelch: PASS / user accepted October 24, 2026.** **Automatic I/Q recording: PASS / user accepted October 24, 2026**, following four complete user recordings and the user's "auto record pass" confirmation. Individual recording edge cases remain reference checks, not blockers. Decoder-setup checks and railroad overlay acceptance remain pending.

## Decoder setup

- With receiver stopped, reproduce the prior HackRF ADS-B conflict: 1090 MHz, 9.6 MS/s, HF hardware filter on. In Aviation click Check / setup receiver. Verify the report names the 1.75 MHz filter and usable-capture limit.
- Apply recommended settings. Confirm receiver remains stopped, HF filter is off, sample rate is 9.6 MS/s, offset +250 kHz, target 1090 MHz and gain is preserved. Start / Apply, then Start aviation, and verify live aircraft decoding.
- Try UAT, narrowband DMR/P25 and HD Radio profiles as equipment/signals allow. HD Radio should disable capture offset. Selecting ATSC on RTL-SDR should explain why it needs a wider-capability receiver. Do not use a settings profile as proof of signal identity.
- During replay, apply must be refused without changing the recorded source. While recording live, applying settings must finalize/disarm recording and leave the receiver stopped.

## Offline aviation map and trails

- Disable Online street map and disconnect the computer from the network. Show aircraft map and verify land/lakes/state boundaries/city labels remain visible without a tile cache. Zoom and pan; regional detail should be present, with finer detail explicitly outside this dataset.
- Select an aircraft table row: its position and trail should highlight. Click a map dot: its table row should select. Toggle Selected trail off/on and All trails off/on. Fit aircraft and Follow selected should still work; drag should leave follow mode.
- Monitor moving aircraft. Verify trail follows received positions, does not connect across gaps over 30 seconds, and remains separated by source/live/replay identity. No fresh position means the dot disappears after 60 seconds; history can remain for ten minutes. Clear aircraft removes paths.
- Replay and seek backward: trails must break at the rewind. Check that enabling the map causes no increased decoder drops during a normal run.
- Optional online map remains opt-in; if a tile fails, the embedded offline reference remains visible beneath it.

## Test both updates

Use ACCEPTANCE-0.33.0-preview1.md for activity-triggered I/Q, main receiver auto squelch, railroad CSVs and carried-forward 0.32 checks. They are included in this ZIP; one 0.34 installation contains both updates. APRS TX gain/start inconsistency remains deferred.
