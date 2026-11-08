# M.R.S. 0.42.0-preview1 — discovery references and receiver workflow

Extract the full Windows ZIP into a new folder and run MRS.exe. Keep Help, bandplans, Decoders and Receivers together. Existing saved stations and local profiles remain separate; no localized monitoring data is included.

- Aviation stays enabled through compatible scan/manual retunes and reacquires at the actual tuned channel. Saved-channel tuning outside an RTL capture now retunes without reopening the receiver. Explicit decoder Stop remains stopped. Invalid capture/fixed-frequency conditions show their existing reason.
- Aviation now has Aircraft / map and Messages / Inspector subtabs. ACARS, VDL2, ADS-B/Mode S and UAT message details, filtering, clear and export live there rather than in generic Analysis.
- Amateur APRS and FT8/FT4/WSPR have mode-specific receive presets, editable MHz and Tune, plus saved custom local/repeater/simplex presets. USB presets identify dial frequencies. JS8 and other future decoders are not added.
- Added sourced editable aviation, HF voice/VOLMET/HFDL, WWV, Amateur preset and selected U.S. ISM references. Candidate ACARS channels retain uncertain status. HFDL is reference-only. Bandplans can add selected reference channels to Frequency Manager folders with deduplication and label preservation. Missing supplied discovery CSVs can seed an existing working folder without overwriting edits.
- Volume track clicks now set the level at the pointer position; mouse-wheel fine adjustment is preserved. Monitoring remains the audio on/off control.
- Removed the annotated Receiver and Recording explanatory text; kept setup selector/Apply, controls, current results and error feedback. Details remain in Help. Fixed the corrupted Manage bandplans label.
- Added default-locked TX with fresh U.S. amateur operator declaration, exact request/device checks, conservative emission-edge limits, explicit one-burst arm and matching APRS callsign. A declaration is not independent license verification; power/location/equipment compliance is operator-confirmed. Other services/jurisdictions stay blocked. The guard is separate from editable receive references.

No new decoder engine, marine/business reference expansion or trunk-following completion is claimed. HD Radio/HDTV still require restart after receiver configuration changes. Existing pending acceptance remains pending. See ACCEPTANCE-0.42.0-preview1.md and VALIDATION-0.42.0-preview1.md.
