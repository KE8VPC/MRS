# Validation — 0.45.0-preview1

## Fox Hunt local checks

The full self-test suite includes 30 new Fox Hunt assertions (1,810 total): independently checksummed NMEA GGA/RMC examples, multi-GNSS talker/hemisphere decoding, fix-quality and checksum failures, simulated/dead-reckoned-only rejection, stale-fix handling, coordinate bounds, Windows DCB layout, bounded RF queue and RF freshness, fixed-gain enforcement, no invented location/power, linear-power averaging/peak, source/gain mismatch rejection, squelch filtering, route gaps, invariant CSV, GPX segments/extensions, no-overwrite behavior and relative map color endpoints.

The dedicated UI smoke includes the preceding weak-signal/APRS/JS8/HF screens and new Fox Hunt synthetic route display. It verifies stopped-at-start logging, new tab, route/table population and offline rendering. A synthetic receiver worker exercises a channel-change callback to check that the APRS target stays at its preset/custom value; changing FT4 mode must not reset APRS edits. This worker opens no actual RF device.

## Authorized live GPS probe — November 1 local / November 2 UTC, 2026

The user identified a connected working u-blox 7 on COM6 at 57600 baud and explicitly offered access for testing. The new M.R.S. Win32 serial/NMEA backend opened COM6 at that baud and received a fresh coordinate fix. The brief sample reported 848 bytes, two checksum-valid supported NMEA sentences, zero checksum errors, four satellites and HDOP 5.77. Exact coordinates are omitted from this record and no captured location log is included in the package. The reader disconnected and released the port afterward.

This establishes live serial connectivity, supported NMEA parsing and a fresh GPS fix through the implemented backend. It does not independently establish geographic accuracy, moving-route quality, the GUI's saved connection behavior, a combined GPS+SDR field log or transmitter localization. The probe does not initialize an SDR or transmit/upload positions. The final release uses the same GPS backend as the live probe.

## Build/package and existing features

Release / win-x64 self-contained publish succeeds. NuGet NU1900 reports that vulnerability-feed data is unavailable; compilation is successful but this is not a dependency vulnerability audit. The package retains companion helpers, their source/notices and public reference CSVs. ZIP CRC, final executable identity, helper hashes, source correspondence, staged/extracted hashes and private-log/profile exclusions are verified before delivery; artifact checks/hashes are beside the release ZIPs.

No new digital RF transmit test, live APRS upload or GPS+SDR drive test was performed. Previous user acceptance remains: APRS reporting/aprs.fi and 80/160 m presets PASS. The apparent low-power TX trial remains reception-unconfirmed. New field acceptance is in ACCEPTANCE-0.45.0-preview1.md. Offline map data remains regional/city reference detail, not a street map or a precise transmitter estimate.
