# 0.42.0-preview1 local validation — November 1, 2026

Final executable: work/mrs-0420-preview1-check3/MRS.exe. Published self-contained Windows x64 with the local .NET 10 SDK. Build succeeded; NU1900 reports that the NuGet vulnerability feed was unavailable. This is not a successful dependency vulnerability-feed review.

- Final self-test: 1,664 assertions PASS. Includes TX missing/declaration/expiry/device/request/band-edge denial, complete-batch reference validation, persistence/deduplication, and Aviation retarget/explicit-Stop behavior, plus the prior regression suite.
- Final --discovery-smoke: PASS. APRS and weak-signal preset bars, hidden sidebar text, aviation message routing, searchable Help and initial unarmed TX window. Simulated RTL receiver changes from 129.350 to 131.550 MHz without replacing the receiver worker or disabling ACARS; folder scan follows target; explicit Stop remains stopped. Checks volume click-to-position and preserved wheel increment configuration. No physical receiver was opened.
- Broader --ui-smoke: PASS on check2 before the final volume property/deduplication adjustment; final targeted smoke and self-test include those changes. Prior replay/map/fleet/UI workflows passed. Rendered new APRS and Aviation message screenshots reviewed for layout and contrast.
- Final direct --hackrf-tx-test without a permit exited 1 with TX locked, before native library/device initialization. No RF was emitted by this check. TX permit is an operator-declaration safeguard, not independent license/identity/location/power verification or tamper-proof access control.
- 28 supplied CSV files across existing and new public references; 38 Help topics. Sources/uncertain ACARS candidates retained. HFDL remains reference-only. New CSV parsing and reference imports validated by the application parser.

Package checks are recorded separately in MRS-0.42.0-preview1-PACKAGE-CHECKS.txt: ZIP CRC, executable identity, native helper hashes, matching source, samples/public-data boundaries and every staged/extracted file hash. Original local captures, DSD/Unitrunker backups, saved station libraries and private profiles are not release content.

Live ACARS folder-scan reception, real protocol-switch behavior, pointer/audio usability, current TX user workflow and the carried-forward RF/device/trunk-following acceptances remain pending. HD Radio/HDTV retain explicit restart behavior after receiver changes. No new decoder engine or full marine/business dataset is claimed.
