# Pending update integration review

November 4, 2026. Source/development checks only; release remains 0.45.0-preview1. No release ZIP, installation, commit/push, live SDR/GPS operation, RF transmission or network reporting.

## Additional fixes

- Offline preference JSON is closed before awaiting map parsing. Remove/replacement during startup restore can atomically save on Windows without sharing conflicts. Late completion cannot reactivate a removed map.
- Fox Hunt estimates state the point count and last measurement time and identify stopped logs as historical snapshots. A live logging pause beyond twelve seconds hides the old area; a backwards clock change also hides it. Existing new-point/scope-change invalidation remains. Background work captures its cancellation token before the token source can be disposed.
- P25 trunk history, Session log and CSV nac_hex fields consistently use hexadecimal NAC (for example 0x293); talkgroups and radio IDs remain decimal. Unknown/invalid NAC display stays blank/unknown.

## Verification

- Development compilation: PASS, zero errors. Existing NU1900 warnings report unavailable NuGet vulnerability metadata; this is not an audit result.
- Full local regression suite: PASS, 1,872 assertions. Nine additional assertions cover preference removal during startup restoration, historical/stale/clock-change estimate presentation, measurement count/time and NAC formatting.
- Dedicated receiver UI/dispatch smoke: PASS through synthetic receiver workers. Assignment/tuning/history/Session records, call completion/release, manual stop/resume, failed assignments and bounded visible newest-first history checked.
- Fox Hunt / shared Aviation map UI smoke: PASS. Primary navigation, visible estimate status, calculate/clear, optional public road detail, saved-reference restore/removal and zero Aviation tile requests checked.
- Updated voice and Fox Hunt screenshots were visually inspected. All positions/aircraft/signal fixtures are synthetic; the optional public road overlay contains no user measurement or transmitter reference.
- Scoped whitespace check: PASS.

## Handoff

ACCEPTANCE-PENDING-NEXT.md consolidates field/installed-system checks for the next explicitly requested release. Live P25 following completeness, known-source Fox estimation behavior, GPS/SDR continuity, saved maps on the installed system and privacy/no-automatic-reporting behavior remain separate user acceptance items. Source fixes are ready to be included together; prior published ZIPs are unchanged.
