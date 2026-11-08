# 0.41.0-preview2 validation

Final candidate: mrs-0410-preview2-check4. Windows x64 / .NET 10 WPF, local date 2026-10-29.

- 1,630 application assertions passed, including the full prior decoder suite. New tests cover NTP four-timestamp offset/delay calculation, response/originate validation, invalid/rate-limited/unsynchronized packets, excessive dispersion, missing timestamps, post-2036 era rollover and stale-reference expiry.
- Received timing tests cover signed absolute correction, station/slot duplicate suppression, multi-station consensus, outlier rejection, exclusion of WSPR/invalid decodes, source changes and reset. Clock jumps invalidate old timing epochs and discard acquisition visibly.
- Generated FT4 extending beyond the ordinary slot boundary decoded through the overlapping window. All seven generated audio modes and direct I/Q-to-LoRa/mesh regression checks still pass. Live FT8/FT4 clock-drift, receiver latency and station-consensus behavior remain acceptance items.
- Timing UI smoke passed in an isolated hidden profile: manual/automatic estimate application, correct sign, bounded automatic adjustment/cooldown, replay exclusion of NIST time, complete WAV EOF decoding, new pages and searchable Help. Timing screenshots were inspected. The broader existing UI/replay smoke suite passed separately.
- A live NIST UDP query succeeded using the same protocol path: measured correction +3.952 seconds, round-trip delay about 76.8 ms, estimated uncertainty about 39.0 ms at that check. This is one network measurement, not ongoing clock certification. Windows time was unchanged. No PSK Reporter submission occurred.
- SDK publish completed with NU1900 warnings because its package-vulnerability feed was unavailable. No successful vulnerability-feed check is claimed. `git diff --check` passed.

Packaging checks verify the final executable, native helper hashes, corresponding sources, 36 Help topics, 20 nationwide CSV files, generated-sample allowlist, ZIP CRC and staged/extracted file hashes. Private profiles, local system backups and original RF recordings are excluded. See the separate PACKAGE-CHECKS artifact for final hashes.

All new live RF and long-duration timing acceptance remains pending. See ACCEPTANCE-0.41.0-preview2.md. Fox Hunt and optional PSK Reporter integration are not part of this rebuild.
