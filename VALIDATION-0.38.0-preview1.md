# Validation - 0.38.0-preview1

Final candidate: work/mrs-0380-preview1-check8, October 25, 2026 (America/New_York).

- Windows x64 publish succeeded. NU1900 reports unavailable NuGet vulnerability information; no dependency audit is claimed.
- 1,524 local assertions passed, exit 0. New checks cover validated grant source/CRC, identifier resolution, unsupported TDMA/encrypted grants, allow/ignore/hold/skip, hang/maximum timers, serving-identity changes, confirmed/unconfirmed Cap+ carrier maps, both slots, repeated rest evidence, color-code scope changes, bounded fragment parsing and corruption rejection. Independent OP25 voice fixtures exercise matching and mismatched grant NAC/group gates.
- The asynchronous raw-I/Q worker was exercised with independent OP25-encoded P25 control fixtures: it received identifier tables/grants and selected the expected in-span voice frequency/group without input drops. Separate voice fixtures verify the audio gate; this does not establish a real two-carrier trunk call or live RF performance.
- Full isolated UI/replay smoke passed, including saved following profiles, both protocol layouts, map editing display, prior broadcast/recording/decoder/replay/aviation workspaces and MP3/Both recording. The first earlier UI run failed the existing replay EOF-controls timing check while decoder tests were running; its isolated rerun passed. Final tests run sequentially to avoid that contention.
- New following screens were visually reviewed; minimum-width UTC/event/MHz/TG columns and a remaining-space Detail column keep history readable. External offline Help includes 23 topics.
- Input queues, fragments and history are bounded. Input overrun stops following explicitly. Default-off hardware retune permission also applies to mapped rest changes outside capture. No live receiver was opened and no RF transmission occurred.
- Packaging extracts both ZIPs and compares every staged file hash. Final archive CRC, executable match, native sensor hashes, Help/CSV counts and generated-recording guards are recorded in the package-check artifact.

No installed running application, user settings or original recordings were modified. New live following, hardware retunes, real Cap+ mapping/rest changes and actual-rate CPU/USB performance remain pending. Carry all 0.37 acceptance with this release; see ACCEPTANCE-0.38.0-preview1.md. Conventional DMR voice acceptance remains distinct from trunk following.
