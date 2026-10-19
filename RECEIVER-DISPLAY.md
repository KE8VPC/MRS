# Receiver / display preview 0.23.0-preview1

## Receiver controls
- Receiver > RF squelch now has Auto squelch, a 3–30 dB margin, floor/threshold lines and Recalibrate. These share the Scan tab settings and operate with scanning stopped. Allow about 1.2 seconds for calibration after a capture change. The estimate is relative channel power, not a calibrated RF meter. Moving the manual threshold turns automatic adjustment off; disabling RF squelch bypasses muting. Recorded I/Q and decoder input remain unaffected.
- Expanded filters: NFM 6–50 kHz; AM 4–30 kHz; USB/LSB 1.8–6 kHz; WFM up to 200 kHz. The available choices are discrete. Filter width is distinct from capture sample rate and audio bandwidth. NFM listening retains its speech audio filter. The 240 kS/s channel front end limits WFM; larger capture rates do not widen this analog demodulator.
- CW receive mode: 250/500/750/1000/1500 Hz filters and 300–1200 Hz listening pitch (700 Hz default). Uses an audio oscillator after the narrow complex filter. This is listening, not Morse-to-text decoding. Narrow CW processing costs more CPU; evaluate on your station. Existing I/Q mode indices are preserved; new CW recordings require this build or newer.
- Optional Auto mode from U.S. band reference defaults off. Manual tuning selects WFM in FM broadcast, AM in AM broadcast/aviation communications, and NFM near listed analog marine/weather/personal channels. Unknown and amateur ranges keep the selected mode; AIS/DSC are excluded from the analog marine rule. Saved-frequency tuning preserves the saved mode/filter. This is a convenience based on the bundled reference, not protocol recognition.
- Added 1.5/2/2.5/5 MHz tuning steps. They remain subject to receiver coverage.

## Display
- RF waterfall rows retain their original frequency span across capture-center shifts and rate/FFT changes. Overlapping frequencies align; uncovered portions and recording gaps stay blank. New sources, Stop and replay gaps still clear history. The current spectrum/peak restarts after retuning. Retained history remains bounded.
- Zoom now reaches 4096x. Zoom cannot manufacture frequency resolution: the actual Hz/bin remains visible. Choose FFT detail and capture rate accordingly.
- Added Hot signals waterfall palette/theme and optional gradient spectrum trace, colored by the displayed vertical signal level. Other palettes and solid traces remain available.
- Zoom controls share the navigation row and wrap naturally on narrower windows. Receiver device, HackRF, filter/rate, squelch and listening sections can collapse. Display sections can also collapse. Collapse state is session-only.
- Explicit light receiver labels fix black text against the dark background.

## Frequency Manager fixes included
Single click selects; double click tunes. F2 or right-click Edit opens the selected item. Ctrl/Shift selection, Shift+arrows, bulk Delete and context-menu actions use the same selection. Show markers / Hide markers changes spectrum visibility; entries remain in the manager. Hidden parent folders still hide their children.

## Acceptance boundaries
Automated checks use synthetic fixtures and isolated profiles. Live SDR performance, CW listening quality, receiver auto-squelch behavior in real interference and long-running retune history need station acceptance. See ACCEPTANCE-0.23.0-preview1.md. Protocol-tab consolidation, split spectrum/data workspace, Morse ID decoding, scan timed resume, aircraft trails and triggered recording remain future work.
