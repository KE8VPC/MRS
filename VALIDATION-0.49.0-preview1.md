# Validation — 0.49.0-preview1

Windows x64 portable preview. Local and synthetic evidence only.

## Results

- Published full suite: 2,050 PASS assertions, no FAIL lines, including 48 single-receiver discovery assertions.
- New model checks cover validated control gating; failed CRC/replay/foreign identity rejection; one candidate per assignment; applied-tune acknowledgement; wrong slot/CC and contradictory calls; repeated-packet and missing-source-ID deduplication; observed terminator/Cap+ idle boundaries; configurable three-call pass; competing associations; explicit rejection; inventory-unknown/incomplete status; stopped resume; malformed/structurally invalid file preservation; bounded follow/call-end waits; control reacquisition; independent receiver state; atomic saved metadata/Undo; and rejected/excluded history.
- Generated DMR RF traverses the production raw-I/Q survey worker, NFM downconverter and decoder. Old-generation observations/audio are discarded at a tuning boundary; unsupported sample format is explicit failure. This is not a live RF test.
- Survey UI integration uses a synthetic receiver process: actual command/acknowledgement path, outside-capture candidate retune, return to control, evidence persistence, separate receiver research, generated discovery screen, exact saved-entry preservation, automatic tentative > passed > conflict downgrade and protection of a different manual definitive LCN. A conflicting library update is checked for no partial write.
- Six UI/integration scenarios pass: single-receiver survey; independent P25/Commercial flows; mapped DMR following; five-receiver fleet; generated-I/Q P25 voice/logging; discovery exchange/sharing.
- Rendered discovery interface inspected for version, readable controls, evidence/conflict status and saved-progress display.
- Publish completed with zero compilation errors and the existing two NU1900 warnings (NuGet vulnerability metadata unavailable). This is not a dependency security audit. CRLF-aware tracked whitespace checks pass.

## Boundaries and acceptance

No live SDR/GPS, RF transmission, internet reception reporting or operational frequency-list changes were performed. Synthetic passed associations are not evidence of a real site's correct LCNs or audible AMBE voice. Live tuner latency, short calls, moving rest channels and two-system operation require the station checklist. A passed mapping meets the configured independent-call rule, not a guarantee of correctness. This preview conservatively requires control/voice color-code agreement; mixed-CC sites may remain unresolved. Absolute-frequency/MBC and additional vendor formats are outside this change.

The archives are checked for CRC/integrity, executable/source identity, current Help and release documents, public bandplan/sample allowlists and exclusion of local operational data. Extracted-package checks are recorded in the adjacent PACKAGE-CHECKS file. This request does not commit or push the source changes.
