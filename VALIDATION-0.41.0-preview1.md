# 0.41.0-preview1 local validation

Final candidate: mrs-0410-preview1-check7, Windows x64, .NET 10 WPF. Local checks completed 2026-10-29. Live RF/hardware reception of the new modes remains pending.

- 1,607 application self-test assertions passed. This includes the carried regression suite, structured SAME/DTMF parsing, mesh public payload/CRC/sync/MAC checks, bounded malformed protobuf handling, all seven generated-audio replay modes, retune resets, and the complete raw-I/Q channel bridge → LoRa → mesh-envelope pipeline.
- Generated SAME, DTMF, MDC1200, FleetSync, FT8, FT4 and WSPR signals decoded their expected fields/messages. WSPR used a complete generated two-minute frame. The FT4 timing-search boundary and WAV end-of-file final-window handling were corrected and retested.
- Direct generated SF11/BW250k/CR4/5/sync0x2B LoRa recovered the exact payload with CRC. Header-only/CRC/sync evidence remains separate. Meshtastic and MeshCore public application tests use generated packets, not captured local traffic. Other RF profiles require live acceptance.
- Silence produced no audio/LoRa messages in the native negative checks. Invalid weak-helper frame lengths were rejected. Queue limits and failure statuses remain explicit; a test that outran playback was corrected to use bounded streaming rather than conceal the overrun.
- New-workspace UI smoke passed for signaling, weak signal and mesh pages, searchable Help, receiver discriminator enablement and compatible profile changes. A real-time FT4 WAV decoded after EOF. Screenshots were inspected locally. The broader existing UI/replay smoke suite passed separately.
- The distributed native rebuild script compiled all three helpers with the selected MinGW toolchain. Native hashes, corresponding sources, FFTW source/license, provenance notes and original upstream notices are included.
- `git diff --check` was checked after text hygiene. The SDK reported NU1900 because the package-vulnerability feed was unavailable; build and runtime checks completed. No successful vulnerability-feed check is claimed.

Release packaging verifies ZIP CRC, staged/extracted file hashes, final executable identity, required helpers and Help, generated-sample allowlisting and exclusion of private profiles/backups/recordings. See the separate MRS-0.41.0-preview1-PACKAGE-CHECKS.txt for artifact hashes/checks.

No live SDR, transmitter, Internet spot service, mesh node, station authentication or emergency alert delivery was validated by these local checks. Earlier user acceptance remains recorded separately; the 0.40 live acceptance checklist is carried forward.
