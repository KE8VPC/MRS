# Validation — 0.48.0-preview1

November 8, 2026. Published self-contained Windows x64 preview.

## Local results

- Final published full suite: **2,002 PASS assertions**, no FAIL lines, complete report, including **37 new update assertions**.
- New assertions cover identity-bound renamed folders and legacy adoption; cleanup rename preservation; same-channel tuning offsets; classification enrichment and partial-information retention; conflicting/ambiguous held observations; update-only persistence; explicit separate/merge review; queue coalescing; manual-placement preservation; firm pan/drag/zoom/tune/full-span center lock; 500 Hz defaults/fine mode; overlapping stable IDs and rendered (+N) label hit-testing; and bounded LCN correlation, validation/slot/TG gating, repeat counting, conflicts and stop behavior.
- Targeted dual-flow UI checks passed: distinct P25/Commercial models and views; mixed legacy profile migration; independent auto-save toggles; simultaneous P25/DMR control-to-voice dispatch; physical receiver double-lease prevention; separate histories; independent stopping; held-discovery and research screens.
- Existing DMR following (three mapped protocols), five-receiver fleet, six P25 generated-I/Q voice/log scenarios, library sharing and Fox/offline map integration checks passed on the published candidate.
- Reference-tooltip tests explicitly disable click-snap hints so they continue to test reference-layer visibility independently of the new click-snap default. Rendered protocol/queue/research screens were inspected.
- Publication completed with zero compilation errors and the existing two NU1900 warnings: NuGet vulnerability metadata unavailable. This is not a dependency security audit. Scoped whitespace checks are recorded separately.

## Evidence boundaries

Control grants, voice identity observations, tuning workers, saved libraries and route/map fixtures are synthetic/isolated. P25 voice regression uses generated I/Q through the production decoder path; DMR following/research tests do not prove live AMBE or a real-world LCN map. Correlation counts are samples rather than independently verified calls, and candidates remain unconfirmed. No live SDR/GPS, TX, internet reception report or public upload was performed. No operational saved list was bulk-rewritten. Existing station acceptance remains open.

Runtime/source archives are checked for integrity, source/executable identity, helper hashes, current Help/docs and absence of operational profiles, routes and user recordings. Final archive/extracted launch checks are recorded in the adjacent MRS-0.48.0-preview1-PACKAGE-CHECKS.txt. This build does not commit or push source changes; that requires a subsequent request.
