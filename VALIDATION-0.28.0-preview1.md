# Validation — 0.28.0-preview1

- Local SDK Release/win-x64 self-contained publish succeeded. Existing NU1900 warning: NuGet vulnerability data could not be fetched; this is not a security-audit result.
- 1,267 regression assertions passed in check3, including 8 new folder traversal/snapshot/timing cases. No RF hardware was used.
- check5 only adjusts control layout, checkbox text colors and preview heading; scanner logic is unchanged from the passing regression run.
- Final check5 UI smoke passed; rendered frequency tree, compact scan controls and dark-mode labels inspected successfully.
- Saved-folder live scanning, tune latency, signal detection and mixed-band operation still require user acceptance. Manual APRS live packet acceptance remains pending; previously accepted tone transmission is a separate test.
- No personal recordings/settings are included. Package fixture allowlist contains 11 generated recordings.
