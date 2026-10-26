# Validation — 0.36.0-preview1

Release candidate: work/mrs-0360-preview1-check7, October 24, 2026 local date.

- Windows x64 single-file publish succeeded. NU1900 indicates NuGet vulnerability-data access was unavailable; no dependency audit is claimed.
- 1,374 local regression assertions passed. New checks cover NXDN valid/invalid/partial control frames and resets, preserving labels/notes on identifier saves, rejecting replay auto-population, exact AM/FM channel centers, missing-only CSV seeding, help keyword search, and CSV color/extra-column validation.
- UI/replay smoke covers the shared AM/FM spectrum, existing stereo/RDS/MPX/audio/waveform views, Audio sidebar order, structured NXDN rows, searchable Help, and actual custom-plan editor save/reload. Editor and Help screenshots were reviewed; explicit dark window backgrounds corrected a pale-on-white problem. Individual controls and tables retain readable foregrounds.
- The supplied MRS-IQ-A-20261024-213912 recording was read only through the updated M.R.S. NFM/digital pipeline. A paced replay of 3,792 frames recovered 72 matching NXDN control identities: system 289, site 12, RAN 12, location 40242C; zero dropped input and zero voice frames. No voice or live trunk-following acceptance is implied. The recording remains outside both archives.
- NXDN control candidates require repeated matching observations; decoder epochs separate restarted/tuned contexts. Automatic save is opt-in/live-only and performed once per confirmed row, preserving names/notes; active user editors defer background writes.
- AM/FM selectors use 118 AM/supplemental centers and 100 FM centers. Two added reference CSVs produce 20 CSV files total; existing reference/custom edits are not overwritten by seeding.
- Searchable offline Help contains 21 editable Markdown topics. Runtime/source packaging includes Help and the acceptance checklist.
- Runtime/source archives are extracted and every staged file hash compared by the packaging script; final ZIP CRC/native hash/document checks are recorded separately.

No live receiver settings were changed, no transmission was performed, and no installed user application was replaced. The user accepted the previous paging, railroad, contrast, navigation, frequency-list and P25 gain checks. The new UI/save workflows need user acceptance; see ACCEPTANCE-0.36.0-preview1.md. Trunk following and broader sidebar cleanup are not included.

Final check7 full UI/replay smoke: PASS, exit 0.
