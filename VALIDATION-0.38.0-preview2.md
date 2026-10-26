# Validation - 0.38.0-preview2

Final publish: work/mrs-0380-preview2-check4, October 25, 2026.

- Windows x64 publish succeeded. NU1900 indicates unavailable NuGet vulnerability data; no dependency audit is claimed.
- 1,531 local assertions passed on check3. Seven new assertions cover optional single-status/unconfirmed-map following, single-observation rest mapping, failed-CRC action rejection, discovery visibility/quality labels and saved-profile default/round-trip compatibility. Default-on behavior keeps the existing 0.38-preview1 strict tests passing. Check4 changes only Evidence column placement in the DMR UI; decoder/follower/test code is identical.
- Full isolated UI/replay smoke passed on check3, including saving optional confirmation profiles and filtering synthetic tentative/noisy rows in all three modes. The first run caught a collection-view switching problem; explicit view ownership fixed it. The final check4 UI/replay smoke was rerun for the Evidence column move next to Identity.
- Discovery selection persists per receiver. Existing profiles missing the new RequireConfirmations setting retain true. Switching views preserves retained rows. CRC-failed and heavily corrected evidence stays isolated from qualifying rows and excluded from automatic frequency saves.
- Disabled repeat/map confirmation permits one CRC-valid supported Cap+ group/rest observation with an entered candidate carrier map. It does not mark the map confirmed, invent a missing frequency, disable hardware-retune permission, or bypass voice identity/encryption gates. Tentative following is labeled in history.
- Runtime/source ZIPs are extracted and all staged file hashes compared. Final archive CRC, executable match, native hashes, 20 CSVs, 23 Help topics and generated-recording-only checks are recorded in the package-check artifact.

No live hardware or RF transmission was used. Original recordings, the running installation and user receiver settings were not changed. Weak/noisy-signal usability and trunk following still need live acceptance. All unfinished 0.38-preview1 and 0.37 acceptance remains carried forward.
