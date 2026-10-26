# Validation — 0.30.0-preview1

- check3 self-contained Release/win-x64 publish and all 1,284 regression assertions passed. New checks cover +12 dB voice boost, limiter bounds/polarity and split-setting normalization. The relocated bottom band-plan hover/axis interaction test was updated for the new layout and passed.
- check3 full UI smoke passed, including P25 control/voice I/Q replay, dedicated paging grouping/labels/ignore/inspection, spectrum-only tone indication and independent receiver preferences. Explicit check rejects P25 observations leaking into general Data.
- check4 added minimum message-column widths and protocol-specific help text. Its UI run twice exposed a synthetic test sequencing race between receiver mode publication and queued UI reflection. check5 adds a UI-settle wait before the next synthetic filter edit; final check5 publish and full UI smoke passed. Paging, spectrum and P25 screenshots were visually reviewed. Runtime decoder logic is unchanged from the 1,284-check regression run.
- Existing NU1900 warning: NuGet vulnerability metadata could not be fetched. No security-audit claim is made.
- Synthetic/local checks do not establish live voice loudness, weak-signal intelligibility, long-running grants, actual mouse ergonomics or RF hardware acceptance. No RF transmission was performed.
- Runtime/source ZIPs are extracted and compared by file hashes. Only the 13 allowlisted synthetic samples are distributed. No Unitrunker exports, personal messages, recordings or user profiles are bundled.
- CSV band-plan loading, railroad datasets, Unitrunker import, AES decryption and trunk following are outside this release.
