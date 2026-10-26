# Validation — 0.35.0-preview2

Visibility hotfix for nested workspace content. Shared TabControl backgrounds now use the dark application surface; tab header text remains dark on the native light headers. P25 decoder, voice status and gain controls no longer sit on white. DMR and CW control surfaces receive the same correction.

- Windows x64 publish succeeded; NuGet vulnerability-data access warning NU1900 remains (no dependency audit claimed).
- 1,358 existing regression assertions passed on check1, exit 0.
- Full UI/replay smoke passed on check1. P25 voice, DMR mode and CW inspector screenshots were visually reviewed for contrast.
- Final check2 changes only the visible preview version label from check1; final UI smoke is recorded below.
- Prior feature evidence and outstanding live acceptance remain in VALIDATION-0.35.0-preview1.md and ACCEPTANCE-0.35.0-preview2.md. No live receiver controls or installed application files were changed.

Final check2 UI/replay smoke: PASS, exit 0. Runtime and source archives are extracted and every file hash compared by the packaging script.
