# M.R.S. 0.36.0-preview1 — workspace and identification update

- Broadcast AM/FM channel selectors and shared spectrum; existing FM MPX, stereo-pilot, audio spectrum, waveform and RDS controls moved into FM.
- Audio sidebar tab, side-by-side frequency/mode, removal of Receiver APRS/manual-TX buttons. Broader sidebar redesign deferred.
- Bandplan manager: new custom plans, CSV import preview/editor, duplicate/export/delete custom plans, validation, external-edit conflict detection and optional #RRGGBB marker colors. Supplied/collected plans remain protected in-app.
- Offline searchable Help under Help/*.md, accessible with ? and F1.
- Structured NXDN control identities and control-data status; repeated observations confirm, live-only opt-in save/update preserves existing labels.

Extract the complete ZIP into a new folder. Keep Help, bandplans, Receivers and Decoders beside MRS.exe. Do not replace a running installation. Existing profile/frequency data is retained. Read ACCEPTANCE-0.36.0-preview1.md for new and carried-forward checks.

Broadcast references: 47 CFR 73.14 (117 AM centers, 540–1700 kHz), plus supplemental 530 kHz travelers-information reference; 47 CFR 73.201 (100 FM channels, 88.1–107.9 MHz). These are channel references, not local station assignments. Existing CSVs are seeded only when absent.

P25/DMR trunk following is not added. NXDN48 control reception was user accepted; NXDN voice remains unverified. User-provided I/Q recordings are not included in release archives.
