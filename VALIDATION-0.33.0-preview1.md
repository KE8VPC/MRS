# Validation — 0.33.0-preview1

- Release publish succeeded with the bundled .NET SDK. NuGet vulnerability-feed metadata was unavailable (NU1900); this is not a dependency security audit.
- 1,319 regression assertions passed in work/mrs-0330-preview1-check4/self-test.txt. The final check5 build adds UI integration assertions and screenshots; production recording/bandplan code is identical to check4.
- Full UI/replay smoke passed in work/mrs-0330-preview1-check5/ui-smoke-result.txt. This includes arming automatic I/Q through the UI against a synthetic HackRF worker, receiving actual worker frames, creating/finalizing a clip without restarting that worker, and railroad plan hide/reload persistence.
- New unit coverage: no files below threshold, buffered pre-trigger and quiet-tail samples, increasing sample-time timestamps, capture/channel metadata, disarm behavior, maximum-duration rotation, retune separation, disk errors, payload budget shutdown/readability, normalization and railroad reference values/user-edit preservation.
- Existing tests cover receiver auto squelch floor plus margin without scanning, scan/receiver synchronization, manual override and persisted preferences. No new duplicate squelch algorithm was introduced.
- Visually reviewed recording controls and railroad spectrum markers at the smoke-test window size. Screenshot artifacts are local synthetic development outputs, not live-RF evidence.
- Runtime and source ZIPs are separately extracted and every file is hash-compared to staging by the packaging script. The runtime reference bundle contains seven CSV files, including the two railroad CSVs. Only the 13 named synthetic recordings are distributed; automatic test captures and personal recordings are excluded.

Live RF recording, hardware throughput, antenna-dependent decoding and field railroad checks remain pending. No RF transmission was performed. Previous acceptance checks are carried forward in ACCEPTANCE-0.33.0-preview1.md. Aircraft trails/offline basemap and protocol setup guidance are roadmap entries only.
