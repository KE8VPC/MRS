# M.R.S. 0.44.0-preview1

This preview adds an operator-controlled HackRF FT8/FT4/WSPR transmit workflow, opt-in APRS-IS reporting, an external JS8Call bridge and public HF discovery references. Extract the entire ZIP to a new folder and keep its companion folders. Keep your working 0.43 build for comparison; private receiver profiles, saved frequencies, identities and recordings are not part of this package.

## Included

- Fix unreadable checkbox labels in callsign rules and heard-station tracking, with explicit dark-dialog contrast.
- Add the missing sourced 80/160 m receive presets: FT8 1.840/3.573 MHz, WSPR 1.836600/3.568600 MHz, and FT4 3.575 MHz plus the separate provisional Region 3 3.568 MHz entry. No invented 160 m FT4 default.
- Add standard FT8/FT4 encoding, preview, manual CQ/peer exchange steps, optional peer-specific sequence advancement, even/odd slots, bounded sessions/retries, Halt, late-slot refusal, timing checks, and acknowledged USB receive restoration. Automatic exchanges allow an extra receive cycle for this first half-duplex backend. Contest/free-text/hashed formats and ADIF logging are not included.
- Add one WSPR beacon per explicit arm, 120-second frame scheduling, actual declared output-power field, full 162-symbol waveform and a same-device cooldown. Gain does not measure RF power.
- Keep digital TX authorization separate from the older short NFM/APRS test limits. Support a conservative subset of U.S. Region 2 data segments, declared license class, own-call checks, fresh exact-request/device permits, low gain, RF amplifier/antenna power off and independent worker deadlines. No arbitrary-frequency or non-amateur TX unlock.
- Add receive-only RF-to-APRS-IS iGate reporting using verified login and qAO, with original-frame validation, path/loop restrictions, bounded deduplication/reconnect, no replay or stale backlog, and no Internet-to-RF forwarding. aprs.fi is a downstream APRS-IS display. Reporting is off at startup; the passcode stays in memory.
- Add a local JS8Call TCP bridge for received observations and one manually submitted Normal-speed message through a separately configured JS8Call 3.x backend. M.R.S. checks fresh backend state, station declaration and supported data frequencies before submission. JS8Call owns its radio/audio, encoder, timing and PTT. Native M.R.S./HackRF JS8 remains unimplemented; this is not a full JS8Call replacement.
- Add a five-frequency IBP CW beacon CSV, scheduled-reference indicator, four dated HFGCS USB references, source links for numbers-station research, and private manual HF observation logging. Regional beacon catalogs and current copied numbers-station schedules are not included.
- Expand searchable Help and About with the new workflows, limitations and external sources. Clean installs retain public reference data only.

## Where to start

For digital TX: start live HackRF USB reception on a supported dial, choose Amateur → FT8 / FT4 / WSPR → Transmit. Review the preview and declaration before arming. Use Help → FT8, FT4 and WSPR transmit for the sequencing and timing behavior. A selected received peer or Use latest decoded caller helps prepare an answer.

For APRS: Amateur → AX.25 / APRS → APRS-IS / aprs.fi reporting. Enter your own iGate identity/passcode and explicitly enable only when you intend to share valid live RF receptions.

For JS8: Amateur → JS8Call bridge; configure the external program's TCP API and its own radio/audio connection first. This bridge does not feed HackRF I/Q into JS8Call.

For discovery: Analysis → HF listening / beacons; the public CSVs also appear in bandplan management/import. Scheduled station names and historical channel references are not confirmed received identities.

## Validation boundary

See VALIDATION-0.44.0-preview1.md and ACCEPTANCE-0.44.0-preview1.md. Generated FT8/FT4/WSPR audio has decoded in the separately installed WSJT-X decoder tools; quantized streaming I/Q and long-duration phase handling have local tests. APRS-IS and JS8 networking are tested against local mock servers. These are not live RF, completed-QSO, actual JS8Call/radio, APRS-IS ingestion or aprs.fi visibility acceptance. No over-the-air transmission or live reporting was performed during this build.
