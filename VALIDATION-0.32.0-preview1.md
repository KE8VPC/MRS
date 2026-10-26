# Validation — 0.32.0-preview1

Final candidate: `mrs-0320-preview1-check6`.

- **1,305 local regression assertions passed.** New checks exercise reference CSV round trips, quoted/multiline labels, uplink visibility, per-plan hiding, late P25 band definitions, duplicate grants with label preservation, site/neighbor attribution, replay/invalid-frame exclusion, conflicting mappings, malformed-file protection, reception-boundary isolation and calculated-grid distinction.
- **Full isolated UI/replay smoke passed:** `PASS: all UI smoke assertions completed`. Includes the previous digital-voice helper startup modes, concurrent CW, P25 replay/audio, paging and other receiver checks; new tests exercise live-collection plumbing with synthetic observations, site CSV reload, overlay membership/hiding, shared reference selection, TX priority and return to idle.
- Screenshots of the Bandplans controls, overlapping P25 site markers and simulated red TX light were visually inspected. TX state was simulated in-process; no transmitter worker or RF hardware was used.
- Existing reference datasets are shipped as five CSV files in four folders. Source/review dates remain unchanged. Their displayed overlays now load from editable files; seed data and legacy automatic modulation defaults remain in source.
- P25 observations include a distinct source-generation key so queued observations from an earlier reception cannot be associated with a later tuning session.
- Build completed with NU1900 vulnerability-feed availability warnings. This is not a dependency security audit.

The Windows/source packages are extracted and each file is compared by SHA256 against its staging source during packaging. The package allowlist requires exactly five reference CSVs and thirteen synthetic recording fixtures; collected site CSVs, unresolved user observations, frequency lists and private I/Q recordings are not bundled.

Live P25 collection against the user's control channel, actual RX/TX hardware indication and the carried-forward DMR/NXDN/CW live acceptance remain pending. See ACCEPTANCE-0.32.0-preview1.md and CSV-BANDPLANS.md for instructions and limits. This release does not fix the deferred APRS TX gain/start behavior and does not add trunk following.
