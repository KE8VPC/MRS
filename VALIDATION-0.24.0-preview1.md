# Validation — 0.24.0-preview1

- Release publish succeeded. NuGet vulnerability-audit lookup was unavailable (NU1900); this is not a completed dependency vulnerability audit.
- 1,218 local regression assertions passed, including independent encoded DMR vectors, CSBK corruption, short LC, color-code evidence gating and synthetic LTR polarity/noise tests.
- All UI smoke assertions passed. DMR/LTR screenshot inspected: separate same-color frequencies and grouped LTR rows; additional fields are horizontally scrollable.
- Supplied 464.700 MHz recording contains POCSAG512 sync and sparse filter-dependent output. No reliable complete payload claim; see PAGING-464700-ANALYSIS.md.
- Local/synthetic checks do not establish live Cap+, Connect Plus, Tier III or LTR acceptance. No digital voice or automatic trunk following.
- Packaging verifies extracted binary-package file hashes and allows only eleven named synthetic recordings. User recordings and private decoded messages are excluded.
