# Note 23 — custom receive frequencies

User request November 26, 2026: allow protocol reception outside normal frequency allocations, including ADS-B at an editable target. The screenshot alone does not establish Doppler, signal identity or unauthorized use.

## Changes for Claude to review

- Removed ADS-B/UAT exact-frequency rejection in AviationPipeline.Check.
- DecoderProfile frequencies are defaults rather than fixed requirements. Receiver setup preserves an explicit Aviation target, then the current receiver field, falling back to the profile default only if neither is available.
- Normal Aviation mode presets remain 1090/978 MHz. The worker already accepts arbitrary target RF and offset and passes target frequency into every aviation message conversion; no Rust/backend replacement was necessary.
- Retained sample format, sample rate, full channel coverage, capture center and hardware tuning constraints. Transmit restrictions are unchanged.
- Audited shared decoder profiles, ATSC and HD Radio capture checks: remaining checks concern capture compatibility, not RF allocations. This does not add automatic protocol recognition or support for unknown waveforms.

## Acceptance

Local isolated test publish succeeded. All 2,317 self-checks passed. Aviation worker fixture suite passed 17 cases: four modes, unsigned/signed 8-bit I/Q, standard/custom targets, plus the existing 19.2 MS/s offset stress case. Frequency attribution was checked for each standard/custom case. Reports are local at work/note23-check/self-test.txt and aviation-test.txt. Custom-frequency tests use existing generated I/Q with changed receive metadata, not captured off-channel aircraft emissions. A decoded address is reported packet content, not independently verified operator identity.

The full --ui-smoke suite passed, including new assertions that applying ADS-B setup retains 1090.100 MHz and UAT setup retains 915 MHz without starting reception. Existing standard target/capture recommendations also passed. Report: work/note23-check/ui-smoke-result.txt. No live RF or transmitter hardware was used.

## Field check for next package

Enter a custom Aviation MHz target, Tune receiver, use Check / setup receiver and confirm the target remains unchanged. Start the selected decoder with adequate capture coverage. Repeat at a standard preset as a baseline. Capture center, viewport center and listening target may differ; do not infer carrier frequency from one waterfall peak alone.

No portable ZIP, public release, website/API change or hardware operation is part of this source update.