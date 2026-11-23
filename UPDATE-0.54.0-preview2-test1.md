# M.R.S. 0.54.0-preview2-test1 — private portable evaluation build

Prepared at the user's request after two further updates: note 22 prepared digital TX messages and note 19 Fox overload evidence. Not published to GitHub; unsigned testing package. No signing identity has been provisioned. SmartScreen may report Unknown publisher. This is not the next signed public release.

## Included since roadmap1

- Shared receive DSP optimization for backlog pressure, sustained-I/Q reconnect retry-budget reset, visible retry exhaustion and corrected reduced-rate setup advice.
- Aviation target isolation and visible Tune/staged-channel failures.
- Large independent signal-trend/GPS/logging indicators with saved size choices.
- Prepared digital-message cache/worker transfer, longer first-slot handoff and timing diagnostics.
- ADC overload card, raw quality fields in new Fox CSV/GPX, flat-reading hint and explained overload exclusion from the existing estimator.

## Test priorities

1. ADS-B and HDTV at 9.6 MS/s: sustained runtime and overflow/reconnect logs. Rate remains too narrow for ADS-B at 2.4 MS/s in this adapter.
2. Aviation target after unrelated receiver tuning; Commercial-to-Broadcast Tune and staged/error feedback.
3. Fox signal/GPS/logging separation, size retention, input overload warning and new CSV/GPX fields.
4. FT8 first and repeated selected slots; capture handoff/worker-ready margin and missed-slot details. Message prepared is not proof of RF transmission or reception reports.

Keep an existing build and back up your saved library/settings. Extract all ZIP contents to a new folder, then run MRS.exe. A separate executable folder does not isolate existing per-user M.R.S. settings/library. No .NET install required; preserve bundled decoder, receiver, Help and reference directories.

## Still pending

GPS auto-reconnect/sleep resume/import, offline saved-hunt analysis, gain normalization/stepped AGC, heat maps, elevated-site model/guidance, collaborative sharing and the rest of the evaluation milestone. SSTV remains Martin M1/M2; the 17 additional modes are planned. No website/API change or live transmit performed in packaging/testing. Local/synthetic validation remains separate from field acceptance.

## Local verification

Combined source passed 2,310 self-checks and Fox tools, digital-TX and receive/tuning UI smoke. Synthetic render inspected. Final executable is rebuilt from the committed version and its archive checked against that executable. Checksums supplied separately. Hardware/RF/hosted acceptance remains open.
