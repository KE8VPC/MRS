# M.R.S. 0.54.0-preview2-test2 — private portable evaluation

November 26, 2026. User requested the next portable update. Includes all test1 functionality plus:

- Note 23: custom ADS-B/UAT receive targets; receiver setup preserves entered frequencies. Normal presets remain, as do capture/sample-rate requirements. HOT/EOT retains standard frequencies at the user's request.
- Note 24: Tune receiver in Analysis > Meshtastic / MeshCore, also serving Generic LoRa. Uses entered MHz without changing SF/BW/CR/sync. Start the receiver first; Start mesh reception remains separate.
- Existing Commercial > Railroad HOT/EOT reception is included: EOT supported fields and BCH validation; HOT frame/type/raw information with unresolved field interpretation. DPU remains reference channels only.

All test1 buffer/DSP, reconnect, Aviation target, Fox indicators/overload and prepared FT8-message updates carry forward. See UPDATE-0.54.0-preview2-test1.md for their scope and remaining acceptance.

## Field priorities

1. Confirm custom Aviation Tune and Check / setup receiver preserve entered targets; verify standard presets still work and insufficient capture coverage is explained.
2. Confirm mesh Tune on supported local frequencies, parameter retention and stopped/replay feedback.
3. Test HOT/EOT using Commercial > Railroad; distinguish no validated frames from missing UI or unimplemented DPU.
4. Continue test1 ADS-B/HDTV sustained-runtime, reconnect, Fox status/quality and FT8 slot acceptance.

## Not included

Sensor filtering improvements, partial modulation/encoding inspector, selected-burst I/Q export, GM keyfob protocol decoding and shared saved offline map regions are not implemented in this update. User recordings were analyzed locally and are not bundled. No claim of new sensor protocol support.

## Installation and validation boundaries

Extract the entire ZIP into a new folder and run MRS.exe; no .NET install needed. Retain all bundled decoder/receiver/Help/reference directories. Separate folders still share per-user settings/library; keep the rollback build and back up station data.

Unsigned private test update, not a public GitHub release. Signing is not configured. Local automated/replay checks do not establish live RF, GPS or transmitter acceptance. No physical transmitter is operated by package validation. See accompanying Claude handoff/checksums for final validation and artifact provenance.