# Validation — 0.31.0-preview1

- 1,289 local regression assertions passed in check2, including new discriminator PCM bounds, NXDN mono/DMR stereo sample conversion, and enable/reset behavior. Existing P25/CW/FEC and other receiver tests remain passing.
- Full isolated UI/replay smoke passed in check2: all three bundled helper modes started using synthetic discriminator samples; digital/P25 audio selections were exclusive; CW stayed enabled alongside digital voice; channel reset cleared diagnostics; Off stopped the digital mode. Existing P25 voice replay and CW candidate checks passed.
- Final check3 changes only clarify the existing DMR identifier panel help text and include release documentation/notices. No decoding or audio logic changed after check2.
- Standalone staged helper probe exited successfully for DMR, NXDN48 and NXDN96 using synthetic noise through stdin. DMR produced no PCM; NXDN48 reported a false sync/CRC error; NXDN96 produced a short PCM artifact from noise. This is explicitly documented and diagnostics are not promoted to validated IDs or saved labels.
- Runtime DLL closure contains 26 DLLs plus dsd-fme.exe. No additional receiver is opened; no RF transmission occurs in these checks.
- Build warning NU1900: NuGet vulnerability information could not be fetched. Compilation succeeded; this is not a dependency security audit.

Live DMR/NXDN intelligibility, encrypted-call muting on actual RF, RAN/ID accuracy, and CW station IDs on actual repeaters remain unverified. Use ACCEPTANCE-0.31.0-preview1.md; it links the previous acceptance checklist.

Final check3 full UI/replay smoke also passed: PASS: all UI smoke assertions completed. All three digital mode screenshots were generated; DMR and NXDN layouts were visually inspected.
