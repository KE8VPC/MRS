# Note 22 — prepared digital messages and first-slot handoff

November 22, 2026. Targeted mitigation for user-reported missed FT8 slots, not hardware acceptance.

Previously Arm encoded a message then discarded the tones; the RF worker launched the encoder again after receive shutdown/device reopening. A bounded 64-message cache now retains mode/message tones, validates plans on every request and returns defensive copies. Mode/message changes select a new entry; slot/audio changes retain the encoded symbols but still generate phase/IQ from the actual authorized plan. Arm reselects a sufficiently future slot after preparation. Receive-side ticks prepare changed messages while there is handoff time available.

The sender embeds prepared symbols in the short-lived existing TX permit. Worker verifies the permit and symbol count/range before using them, avoiding a second encoder process during the slot handoff. Legacy permits without prepared data retain the old encode path. Station, band, time, identity and cancellation checks remain active. No late burst is permitted. No RF was transmitted by tests.

The first burst begins handoff four seconds before its selected slot. Repeated FT8 uses the existing 1.3 seconds so full reply capture is not intentionally truncated. No change to even/odd 15-second slots or protocol waveform/spectral parameters. Preparation does not guarantee successful OS/device scheduling: logs now report handoff/ready margin; missed-slot errors include lateness and worker setup duration.

New tests cover cache mutation isolation, symbol reuse/invalidation, invalid symbols and plan/permit transfer, plus preserved repeat timing. Existing independent waveform decode tests remain part of the full suite. Local test package is unsigned and not a public release. Field test needs actual selected-slot timing and any handoff/ready/error log, not an assumption that a prepared message confirms RF delivery.

Combined test-package validation: 2310 self-checks and Fox/digital-TX/receive UI smoke passed on the final self-contained executable. Render inspected with correct preview2-test1 header. Local synthetic tests only. Full-rate ADC measurement microbenchmark added about 0.035 seconds per second of 9.6 MS/s input on this machine (0.163 to 0.198); not a field guarantee. Evidence: work/mrs-054-preview2-test1-publish.
