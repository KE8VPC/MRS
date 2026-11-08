# Acceptance — 0.45.0-preview1

0.44 user confirmations carry forward: live APRS reporting/downstream aprs.fi display and 80/160 m presets PASS. FT8/FT4/WSPR RF transmit remains unconfirmed; user reported apparent transmission but no reception reports with limited HackRF output. Native SDR JS8 remains future work.

Fox Hunt field acceptance remains open:

1. Live GPS serial backend at the user's COM6 / 57600: PASS (fresh supported NMEA fix; details in VALIDATION-0.45.0-preview1.md). GUI connection/reconnection, saved port/baud and moving-position quality still need user confirmation. Exact coordinates are omitted from release documentation.
2. Start live reception at fixed RF gain, then Fox Hunt logging. Confirm receiver-position trail and relative signal changes during the trip. Setup/adjust controls while parked or have a passenger operate them.
3. Confirm CSV rows save, GPX opens in another compatible tool, Stop finalizes and logs survive exit. These files are private location history; clean release ZIPs exclude them.
4. Confirm loss of GPS fix pauses new points, reconnection to the same configured source resumes, and long gaps produce separate segments. Changing GPS connection/source requires a new session.
5. Confirm gain/filter/frequency/capture changes stop logging and retain the prior comparable session. New measurements require a new file. Automatic RTL RF gain should be refused.
6. Check offline map and manual stationary fallback on the user's display. Offline basemap has regional detail, not street roads. Strongest observation is not transmitter location or proof a target transmitted.
7. Confirm the separate APRS Tune field now retains its preset/custom target when switching protocols or tuning elsewhere.

Local/synthetic parser, measurement, export and UI evidence does not establish successful live GPS+SDR route recording or transmitter localization. No Fox Hunt RF transmission, online map request or position upload is required.
