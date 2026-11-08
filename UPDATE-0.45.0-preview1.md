# M.R.S. 0.45.0-preview1 — Fox Hunt

Adds a first receive-only GPS signal-strength mapping workspace for the requested road-trip trial. Also includes the APRS preset/Tune-field correction reported after 0.44. Extract the entire ZIP into a new folder; retain companion folders.

## Fox Hunt

- Amateur → Fox Hunt: selectable GPS COM port/baud, saved private connection choices, NMEA RMC/GGA checksum/fix validation, satellite/HDOP/age display and an explicit manual stationary fallback.
- Default baud 57600, suitable for the user's configured u-blox 7; choose COM6 for that setup. No automatic COM connection, GPS reconfiguration, RF transmission or online position upload.
- Logs the current live SDR channel once per second by default, with two/five-second options. Fixed RF gain required; receiver/channel/gain/filter/capture/PPM changes stop the log rather than mix comparisons.
- Relative channel-power average/peak, sample count, squelch evidence, optional matching noise floor and complete receiver/capture metadata. These are not calibrated dBm measurements.
- Offline colored route map with pan/zoom, follow, fit, north and approximate distance scale. Natural Earth provides regional/city reference detail, not street roads. Strongest measurement is not an inferred transmitter location.
- Automatically flushed private CSV; full current-session GPX export with signal/frequency extensions. Stale/no-fix GPS and missing RF pause points. Long gaps break route segments. Session limit 50,000 points / 50 MB; last 5,000 map points and 300 table rows displayed.
- No triangulation, coherent receivers, TDOA, automatic direction bearings or transmitter-location estimator in this initial version.

## APRS preset fix

Preset/manual Tune targets are independent of the actual RX status label. Receiving/tuning FT8 no longer overwrites APRS's 144.390 MHz target. Weak-mode selection refreshes only the weak-signal presets and preserves unrelated APRS target edits. Changing tabs does not automatically retune hardware.

The user's live APRS reporting/aprs.fi and 80/160 m preset confirmations are recorded. Earlier digital TX remains unconfirmed after the reported apparent transmit attempt with low output/no reception reports; this release does not change those RF output limits or claim that issue fixed.

See Help → Fox Hunt, VALIDATION-0.45.0-preview1.md and ACCEPTANCE-0.45.0-preview1.md for setup and actual test coverage. No private coordinates or location logs are shipped.
