# Current preview: 0.39.0-preview1

Multiple receiver workspaces and a dedicated control/voice pool; automatic site-channel saving; separate channel/talkgroup records; mapped Con+/Tier III group grants; saved-map imports; view-only clear and newest-first controls. See UPDATE-0.39.0-preview1.md and ACCEPTANCE-0.39.0-preview1.md. Live acceptance and previous unfinished checks remain pending. Private reference data is excluded from clean-install packages.

# 0.38.0-preview2 - optional confirmation

Adds saved tentative/noisy discovery views and per-profile optional repeat/map confirmation for Capacity Plus research. See UPDATE-0.38.0-preview2.md and ACCEPTANCE-0.38.0-preview2.md. Existing defaults and earlier acceptance remain.

# Current preview: 0.38.0-preview1

See UPDATE-0.38.0-preview1.md and ACCEPTANCE-0.38.0-preview1.md for experimental P25 Phase I C4FM / Capacity Plus group following, system profiles and confirmed carrier maps. Test the carried-forward 0.37 acceptance with this build. Keep the complete extracted folder together.

# Previous preview: 0.37.0-preview1

See UPDATE-0.37.0-preview1.md and ACCEPTANCE-0.37.0-preview1.md for click snapping, MP3 VOX, Aviation field alignment and sensor counter clarification. Keep the full extracted folder together; previous acceptance results are carried forward.

# 0.36.0-preview1

Broadcast AM/FM and MPX/stereo views, Audio sidebar, editable custom bandplans, searchable offline Help, and NXDN control identities. See UPDATE-0.36.0-preview1.md and ACCEPTANCE-0.36.0-preview1.md. Extract the complete ZIP; keep Help, bandplans, Receivers and Decoders beside MRS.exe. Existing acceptance results are carried forward. No trunk following is added; broader sidebar cleanup remains deferred.

# 0.35.0-preview1

Adds sensor/remote and selected utility-meter reception through rtl_433, experimental pulse/tone inspection, grouped service navigation, spectrum follow/lock/free navigation and expanded railroad CSV references. See [SENSORS-METERS.md](SENSORS-METERS.md) and [ACCEPTANCE-0.35.0-preview1.md](ACCEPTANCE-0.35.0-preview1.md). Extract the whole ZIP into a new folder; keep Decoders and Receivers beside MRS.exe. Live acceptance for the new decoders is pending. DMR voice is confirmed but trunk following remains unfinished. Previously accepted automatic I/Q, auto squelch and aviation map/trails remain closed.

# 0.34.0-preview1

Decoder setup checks are under Receiver and accessible from Aviation. Aircraft maps now include selected/all trails and a bundled offline regional basemap with land, state/province boundaries, lakes and city labels. Online streets remain optional. This build includes all 0.33 recording, auto-squelch and railroad features. Test ACCEPTANCE-0.34.0-preview1.md and ACCEPTANCE-0.33.0-preview1.md together; both are included. Read DECODER-SETUP.md and AVIATION-MAP.md for behavior and coverage.

# 0.33.0-preview1

Activity-triggered I/Q recording is under Recording. Receiver > RF squelch offers Auto without scanning. Railroad CSV references are available in Bandplans; older working folders can add them with Create missing reference CSVs. Read AUTOMATIC-RECORDING.md, RAILROAD-BANDPLAN.md and ACCEPTANCE-0.33.0-preview1.md. Previous acceptance checks carry forward. Protocol setup guidance and aircraft trails with an offline aviation basemap are planned for the next pass.

# 0.32.0-preview1 — CSV site bandplans and RX/TX status

Editable spectrum bandplans now load from the working `bandplans/` directory. The Bandplans tab supports reload, folder selection, site visibility, uplinks and an optional calculated grid. Live validated P25 collection writes system/site channel pairs and definitions separately from saved frequencies. The approved RX/TX/Idle light is beside the clock. See [CSV-BANDPLANS.md](CSV-BANDPLANS.md) and [ACCEPTANCE-0.32.0-preview1.md](ACCEPTANCE-0.32.0-preview1.md); prior digital-voice acceptance remains carried forward.

# 0.31.0-preview1 — DMR/NXDN voice and concurrent CW ID

See [DIGITAL-VOICE.md](DIGITAL-VOICE.md) and [ACCEPTANCE-0.31.0-preview1.md](ACCEPTANCE-0.31.0-preview1.md). This adds a tuned-channel DSD-FME voice backend with DMR slot selection, NXDN48/NXDN96 selection, polarity and gain. Live speech quality remains to be validated. CTCSS/DCS stays above Spectrum.

## 0.30.0-preview1 — workspace and P25 audio

Implemented priorities 1–5: P25 gain/limiting, compact spectrum header/reference strips, remembered spectrum/waterfall divider, dedicated POCSAG/FLEX and consolidated P25 tabs, plus system/catalog and channel-activity split. See WORKSPACE-REFINEMENTS.md for scope and ACCEPTANCE-0.30.0-preview1.md for live checks. Historical requested items below are superseded for this implemented subset. CSV band plans, railroad dataset, Unitrunker import and automatic voice following remain future work.

## 0.29.0-preview1 — CW IDs and clear P25 voice

Data now offers concurrent CW ID candidates and manually tuned P25 Phase I C4FM clear voice. See [VOICE-CW.md](VOICE-CW.md) and [acceptance checks](ACCEPTANCE-0.29.0-preview1.md). Live validation remains pending; AES key support, DMR voice, trunk following and automatic CW label association remain future work. Previous APRS TX gain instability is deferred; folder scan acceptance is partial.

## 0.28.0-preview1 — Saved-folder scanning

Implemented: right-click a saved frequency folder → Scan folder; saved frequency/mode/filter order, optional subfolders/hidden exclusion, dwell/quiet delay, maximum hold for control channels, Hold/Skip/Stop and current-channel status. See [FOLDER-SCANNING.md](FOLDER-SCANNING.md). Synthetic and UI validation are separate from pending live receiver acceptance. Prior APRS preview remains included; live APRS packet acceptance is still pending.

# Current preview: 0.27.0-preview1 — manual APRS transmit

See APRS-TRANSMIT.md and ACCEPTANCE-0.27.0-preview1.md. Explicit single-send status/position packets; live RF packet acceptance pending.

# Current preview: 0.26.0-preview1 — manual HackRF TX test

See HACKRF-TX-TEST.md. RX remains the default; the manually enabled test emits a brief FM tone. Hardware TX acceptance is pending.

# Current preview: 0.25.0-preview1 — HF reception

See HF-RECEPTION.md and ACCEPTANCE-0.25.0-preview1.md. Optional narrow HackRF hardware filter and input-level diagnostics; live HF acceptance pending.

# Current preview: 0.24.0-preview2

Fixes confirmation of LTR idle bursts spaced more than ten seconds apart. See ACCEPTANCE-0.24.0-preview2.md.

# Current preview: 0.24.0-preview1

DMR / Capacity Plus / LTR identification: see TRUNK-IDENTIFICATION.md and ACCEPTANCE-0.24.0-preview1.md. Includes prior receiver/display refinements.

# M.R.S. — Mike’s Radio Software

## 0.21.0-preview1 aviation milestone

Adds ACARS, VDL2, ADS-B/Mode S and UAT receive decoding from I/Q, a stable aircraft table, JSON export and an optional offline coordinate map. Includes RTL 2.4 MS/s and the pending inspector comparisons. Read AVIATION.md and ACCEPTANCE-0.21.0-preview1.md for startup instructions, limits and the carried-forward checks. Local RF reception remains unverified.

## October 15 inspector comparison checkpoint (now included in 0.21.0-preview1)

Adds a per-receiver pinned inspection reference and field/text/compatible-bit comparisons. Included in the aviation preview. Preview2 has partial user stability feedback (remained running without a crash); individual checks remain unconfirmed. The aviation preview includes ACCEPTANCE-0.21.0-preview1.md, which carries all preview2 checks forward and adds comparison checks with a place to record progress.

## 0.20.0-preview2 paging management and Data inspector

Adds fixed-row paging duplicate grouping across capcodes, saved labels, ignore/restore controls, wrapped payloads, and a frozen Data inspector. Select a Data row and click Inspect selected. P25 NID/TSBK fields have explicit received/decoded byte provenance and supported bit selections; paging exposes decoder fields and JSON without fabricated raw bits. See DATA-INSPECTOR.md and ACCEPTANCE-0.20.0-preview2.md. Live user acceptance remains pending.

## 0.20.0-preview1 paging and scanner refinements

POCSAG/FLEX reception is now in Data, with an offline bundled worker, validation/fragment labels, and audio or NFM I/Q input. This build also packages the pending scanner centering, dated discoveries, multi-delete, fine slider wheel adjustments, birdie exclusions, marker correction and sidebar layout work. See PAGING.md and ACCEPTANCE-0.20.0-preview1.md. User field acceptance is pending.

## 0.19.0-preview1 automatic squelch and activity scanner

The new Scan tab estimates the local RF noise floor, applies an adjustable automatic squelch margin, and optionally draws floor/trigger lines. Start scanner locks the visible captured span, monitors the strongest sustained signal, holds until quiet, and supports Hold and Skip. Digital channel changes preserve the HackRF capture center and offset setting. Qualified discoveries update Frequency Manager with timestamps, burst counts and signal measurements while preserving user labels and folders. See ACTIVITY-SCANNER.md.

Includes the previous single-click saved-frequency tuning and TV RF-channel overlay. Use ACCEPTANCE-0.19.0-preview1.md to test all four features together. Scanning is a live-SDR preview and does not start automatically; field acceptance remains pending.

## 0.18.0-preview3 saved-frequency tuning and TV references

Single-click a saved signal to select it; Edit or F2 opens its editor. Double-click to tune that receiver and apply its supported receive mode/filter. Folders and library refreshes do not tune; drag-and-drop organization remains available. The Tune button remains available for keyboard selection. See FREQUENCY-MANAGER.md.

Display now includes independent TV RF 2–36 channel boundaries/centers and optional ATSC 1.0 pilot references. Hover for exact frequencies. The overlay follows zoom, pan, theme and above/below placement, with channel gaps and RF 37 excluded. See BAND-PLAN.md. This update does not change the HDTV decoder. Automatic noise-floor squelch and locked-spectrum activity scanning remain a combined future milestone.



## 0.18.0-preview2 HDTV setup correction

Fixed the first-install GDK helper errors for missing GLib DLLs. The exact same 108 pinned packages now install in dependency order, and runtime checks cover the helper plus ffplay/ffprobe. Use Set up TV decoder again after closing the previous build. See HDTV.md.

Experimental ATSC 1.0 HDTV reception is now available in the HDTV tab. One-time setup downloads an isolated decoder/player runtime; then decode a live HackRF capture or continuous M.R.S. I/Q recording, save MPEG-TS, list programs and open a separate viewing window. Supports 9.6/19.2/20 MS/s HackRF captures and capture offsets when the full 6 MHz channel fits. See HDTV.md for setup, physical RF channel selection and acceptance. ATSC 3.0 is not supported. Generated TV video/audio recovery passed; live station acceptance remains pending.



Windows-only radio workbench for Windows 11 x64. This is a receive-only testing milestone, not the complete all-in-one application. SDR adapters support RTL-SDR/NESDR and an experimental HackRF/PortaPack USB receive path; conventional radios can feed a Windows audio input such as Digirig. WiNRADiO remains planned. Basic HackRF reception and automatic-offset NFM have been field tested; see HACKRF.md.



## New in 0.17.0-preview3

Higher-resolution RF spectrum and waterfall: Display > RF spectrum detail offers 4K, 16K, 32K (default), and 64K FFT points. Real samples span USB blocks; all frequency bins are retained for zoom. The waterfall now follows panel size and display scaling rather than a fixed 768-pixel width. Resolution applies live and saves with display preferences. See SPECTRUM-DETAIL.md for performance, memory and acceptance details. Confirmed-working HackRF capture offset is retained.

## Added in 0.17.0-preview2



Configurable HackRF capture offset defaults to enabled +250 kHz. Enter the desired listening frequency normally; capture center is shown directly above the left frequency box. Stop before editing the offset. Applied settings are saved per unique receiver identity in each workspace. Tuning, reconnect and I/Q replay preserve separate capture/listening frequencies. Disable offset for the current HD Radio path. See HACKRF.md for limits and acceptance steps.



## Added in 0.17.0-preview1



Select HackRF / PortaPack (USB mode) in the receiver panel, put Mayhem in HackRF mode, and click Find. Supports 1–6000 MHz tuning, 2.4/9.6/19.2/20 MS/s capture, separate LNA/VGA/RF amplifier controls, full-rate I/Q recording/replay and existing compatible receive processing. Keep the Receivers folder beside MRS.exe. This is receive-only; live hardware changes briefly pause reception. See HACKRF.md for setup, HD converter limits and acceptance checks.



## Added in 0.16.0-preview1



P25 identifier updates resolve channel frequencies and TDMA slot metadata. Expanded grant, secondary-control and adjacent-site messages feed the new P25 Systems discovery catalog. Live discoveries persist per receiver; replay discoveries stay separate. Add per-discovery names/notes and review resolved live channels in the frequency editor before saving. No automatic voice following or P25 voice playback is included. See P25.md and P25-SYSTEMS.md.



Try Docs/Samples/synthetic-p25-expanded-control.mrsiq with P25 enabled, then select Replay in P25 Systems. This is a fictional generated sample, not local traffic.



## Added in 0.15.0-preview3



Corrected P25 grouping keeps changing NID raw bits and undecoded TSBK payloads in category rows. Use P25 message types to hide individual categories or choose Voice grants and site/network status. Choices are remembered per receiver; recent hidden messages remain available by selecting Show all. Export follows visible filters.



## Added in 0.15.0-preview2



P25 repeat grouping keeps network/site information in stable rows with reception counts and last-seen times. The Data header is compact with collapsible status/help. Drag the divider to resize the message list and details; divider position is not yet saved across restarts. The session log now has its own tab and no longer occupies the Spectrum view.



## Added in 0.15.0-preview1



CTCSS / DCS receive detection is available in Data, with a live spectrum readout, acquisition/loss history and JSON export. Supports 50 common CTCSS tones plus 62.5 Hz and 104 DCS codes with equivalent normal/inverted names. Inputs include NFM SDR/IQ and compatible low-frequency-preserving radio audio/WAV. Application listening squelch does not gate decoding. See SUBAUDIBLE.md for limits and bundled-sample acceptance checks.



The optional CISA NIFOG interoperability reference overlay is included in this package: 246 channel/plan rows at 236 RF centers, with code/mode details and source pages. Enable it under Display. See NIFOG-OVERLAY.md. Prior P25, HD, WFM/RDS, dual-receiver and frequency-library features remain available.



## Added in 0.14.0-preview1



P25 Phase I C4FM control-data reception is now available in Data. Verified NAC headers are distinguished from CRC-valid network/site messages and group voice grants. SDR NFM and I/Q replay are supported; voice, CQPSK simulcast and trunk following remain future work. See P25.md for scope and acceptance tests. HD listening, RDS/RBDS metadata, MPX guides and previous receiver features remain available.



## Run



Extract the entire Windows x64 ZIP into a new folder and open MRS.exe. Keep its supporting files together. The package is self-contained and needs no separate .NET installation. It is unsigned and has no installer. Preferences and your frequency library remain in %LOCALAPPDATA%\MRS, outside the program folder. Back up that folder before testing a new release.



## New in 0.12.1



- Fixes a crash when enabling reference overlays in a short spectrum panel. Reference rows temporarily hide below 240 display units and return when space permits; saved choices remain enabled.

- Includes the post-0.12.0 AM, aviation and selected marine reference additions documented in SERVICE-OVERLAYS.md.

- Retest with Receiver A at 774 MHz and Receiver B at 90.7 MHz, enabling ARRL and resizing both windows.



## New in 0.12.0



- Optional U.S. band outlines and service channel markers, plus independent ARRL recommendations across 28 band groups.

- Above/below-spectrum placement, saved display preferences, readable crowded labels and hover source details.

- Shared A/B frequency library with automatic same-process refresh and one-time preservation of legacy Receiver B entries.

- Corrected WFM panel text contrast.



See ACCEPTANCE-0.12.0.md for station tests and BAND-PLAN.md for sourced coverage and limitations. Full FCC allocation tables remain planned. The reference bars do not represent individual transmit privileges.



## Retained from 0.11.0



- Independent Receiver A and B windows: separate controls, spectrum, audio output, packet history and recordings. Open the other receiver from the top button.

- Separate persistent receiver settings, with a shared frequency library as of 0.12.0. Receiver B settings use the ReceiverB subfolder. Unique device identities retain selection after Find.

- Duplicate-device ownership checks between the two windows, including reconnect. Stop or close one window without stopping the other.

- More tolerant AFSK symbol timing: three checksum-valid APRS frames recovered from each supplied field recording, including one missed by 0.10.0 in the first capture.



See RECEIVER-WORKSPACES.md for setup and hardware tests. Two-window checks use simulated receiver workers; physical simultaneous reception is not yet verified. REFERENCE-RESEARCH.md consolidates the recent application inspections and future requirements.



## Retained from 0.10.0



- Receive-only 1200-baud AX.25/APRS from SDR NFM, conventional-radio Windows audio inputs, WAV and compatible M.R.S. I/Q replay.

- Packets workspace with callsigns, digipeater paths, raw frame inspection, checksum results, filtering and JSON export.

- Basic APRS uncompressed position, status, message and acknowledgment interpretation. Unsupported formats retain their raw payload.

- Receiver index/serial labels and identity details, plus synthetic two-V3 selection and sampling-preference checks.



See PACKETS.md for operation, supported formats, history limits and receiver testing. The 0.10.0 release used one source. Live APRS position decoding is now demonstrated by supplied recordings; broader decoder and physical dual-V3 acceptance remain pending.



## Retained from 0.9.0



- Stereo WFM listening and recording, pilot/lock status, Force mono, left/right meters and clipping indication.

- RF / MPX / Audio spectrum and separate left/right waveform views.

- Experimental RDS/RBDS PI, program type, station name and RadioText, shown above the spectrum. Strict group validation, repeated text confirmation and stale-data resets.

- SDR WAV recordings are now 48 kHz stereo; mono SDR modes appear in both channels. Audio-input and ordinary WAV-file analysis remain mono.



See BROADCAST.md for controls, supported RDS fields, recording behavior and current limits. HD Radio remains planned. Test on a strong local FM station first; synthetic validation does not establish live reception quality.



## Direct sampling retained from 0.8.1



Select Quadrature, Direct sampling — I branch, or Direct sampling — Q branch in Receiver. Quadrature retains 25–1750 MHz tuning. Direct modes allow positive frequency entries from 1 Hz (0.000001 MHz) through 28.8 MHz; this is a software selection range, not verified reception coverage. Exactly 0 Hz is DC and is not selectable. Antenna coupling, branch wiring, noise and images determine useful low-frequency reception; no near-DC performance is promised.



For your first HF comparison, select a branch, enter 7.000000 MHz, choose an appropriate AM/USB/LSB mode, and Start. Disable RF squelch while comparing branches. Changing branch or receiver stops reception and finalizes recordings; press Start again. Direct sampling bypasses tuner gain, so gain controls are disabled. Branch preferences are restored only for uniquely identified receivers; generic or duplicate serials require manual selection. I/Q recordings retain the branch and replay displays it without opening hardware.



## Features retained from 0.8.0



- AM, USB and LSB reception alongside NFM and WFM, with selectable receive-filter width and optional bounded audio AGC for AM/SSB.

- Software channel tuning within captured coverage: clicking or entering a frequency moves the receive channel without moving the capture center when the complete filter fits. The shaded filter follows the channel. Tuning outside coverage moves the hardware center.

- Frequency entry and Enter-to-tune beside the spectrum and in Receiver; steps from 10 Hz through 1 MHz; explicit Shift −/+ controls move the capture center by half a span.

- Drag-to-pan, wheel zoom, Full span and Center view. Dragging does not accidentally tune. Full span uses the source's usable bandwidth; sample rate and usable bandwidth are separate metadata values.

- Gain and sample rate apply when selected. Applied settings arrive with I/Q frames; nearest supported manual gain is reported. The USB worker and active recordings stay open on successful changes. Mode/filter/AGC selections also apply during reception.

- Recording tab containing WAV, full-span I/Q recording and I/Q playback. Playback includes Play, Pause, Stop, elapsed/total time and seek. The loaded file stays available after Stop or EOF.

- Monitoring checked on by default; last selected Windows listening endpoint restored by ID when available. No output is silently substituted if it is missing. Source reception still requires Start.

- Frequency Manager retains filter width in addition to frequency, mode, notes, signaling and protocol labels.

- New .mrsiq version 3 stores capture center/rate/usable bandwidth, channel/mode/filter/AGC and requested/applied gain per frame. Versions 1 and 2 remain readable. Older executables cannot read new version 3 files.



## Start your NESDR



1. Close other applications using the dongle. Click Find. If necessary, use Set up RTL library, then Find again.

2. Setup downloads the pinned RTL-SDR Blog v1.3.6 x64 release, verifies its SHA-256, and installs its three runtime DLLs under %LOCALAPPDATA%\MRS\rtl-v1.3.6-x64. It does not change Windows USB drivers.

3. If USB access fails, consult the [Nooelec NESDR installation guide](https://support.nooelec.com/hc/en-us/articles/360005298053-NESDR-Installation-Guide) for WinUSB setup. The app does not perform that driver installation.

4. Select your receiver, frequency and mode, then Start / Apply. Quadrature tuning is 25–1750 MHz; direct I/Q modes expose 1 Hz–28.8 MHz for hardware testing. Acquisition choices are 240 kS/s, 960 kS/s and 1.92 MS/s.

5. Choose a listening output. With monitoring checked, a selected available output plays audio. A radio-connected output may activate that radio's VOX; use the intended speakers/headphones for listening.

6. Gain and rate changes apply live. Reducing capture span recenters a channel if its filter no longer fits; the applied channel frequency is shown. Device changes still require Start / Apply and reopen the source. Failures are reported; a rejected native operation may stop reception rather than continue with uncertain settings.



Find restores a receiver only when manufacturer/product/serial identifies exactly one usable device. Blank, generic and duplicate serials remain manual. Optional reconnect retries the same identity up to three times with backoff; Stop cancels retries. Reconnect never resumes recording automatically.



## Listen and navigate



| Mode | Selectable nominal receive bandwidth |

|---|---|

| AM | 4, 6, 8, 10 or 15 kHz total |

| USB / LSB | 1.8, 2.4, 2.7, 3 or 4 kHz; begins 300 Hz from carrier on the chosen side |

| NFM | 8, 12.5, 16, 20 or 25 kHz total |

| WFM | 150, 170 or 190 kHz total; stereo/mono audio |



Filter shading shows nominal boundaries; real filters have transition bands. AM/SSB AGC can be disabled; listening volume remains independent. Fine tuning inside the capture is digital, but hardware frequency error still affects reception. Software channels at the edge may be constrained by the complete filter width.



Click tunes; Ctrl+click selects a saved marker; right-click saves a frequency. Drag pans the spectrum and waterfall within captured coverage. Wheel zooms around the pointer; Shift+wheel pans; Home restores full span. Shift −/+ changes hardware coverage explicitly. Center view recenters the view on the tuned channel while retaining zoom. Display settings include palettes, themes, intensity/range, averaging, peak hold, speed/history, zoom/pan and named presets. Zoom does not create extra RF bandwidth or FFT resolution.



RF squelch is a relative channel-power gate before listening volume/AGC. Lower is left, higher is right; its checkbox bypasses gating without losing the threshold. Recordings and analysis remain ungated. It is not calibrated RSSI; noise, tuner gain and a DC spike can affect it. Audio-input/WAV sources retain a separately labeled audio gate.



Frequency Manager starts without preset folders. Create folders and save signals with notes, mode, filter width, modulation details, protocol/signaling labels, tags, colors and visibility. Saved labels do not activate protocol decoders. Unsupported modes or filter widths are retained but cannot be recalled for reception.



## Record and replay



Open Recording to record demodulated audio to 48 kHz PCM WAV (stereo for SDR; mono for audio input/file analysis), full captured I/Q to .mrsiq, or both. I/Q rate is approximately 29/115/230 MB per minute for the three RTL rates, plus metadata. A bounded 64-frame queue reports storage overload and ends the recording. No file rotation or automatic retention exists; files are limited to seven elapsed days.



Successful gain/rate/channel changes stay in the same I/Q file. Native hardware transitions discard settling samples; timestamps retain observed reception gaps. WAV exports omit these missing intervals. Stop, source changes, loss and closing finalize recordings. Incomplete I/Q tails are reported after complete preceding frames are recovered.



Open I/Q recording to replay without a receiver. Indexing runs in the background before playback; large files can take time to scan. Pause freezes the playback clock. Stop retains the loaded file; Play restarts it. Seeking uses validated frame boundaries and restores recorded mode/rate/channel settings. Seeking finalizes any active WAV export so it cannot silently join discontinuous positions. Pausing an active export omits the paused time. At EOF the playback controls remain available. Replay follows recorded channel settings; freely tuning recorded I/Q is a future feature.



Storage can preserve wider integer I/Q formats and 64-bit frequencies, but current DSP playback accepts unsigned 8-bit at the three validated RTL rates. Unsupported formats/rates produce a clear message. Version 3 layout is documented in IQ-FORMAT.md.



## Audio and conventional radios



Windows audio input supports a selected microphone/line interface, including Digirig. Capture and WAV playback use a 48 kHz mono analysis path. Ordinary radio speaker audio is already demodulated; selecting an SDR receive mode does not recover RF I/Q from it. There is no CAT or PTT control in this release. Monitoring starts enabled only on a selected available output; it does not start any source. The input device preference remains name-based; output uses an endpoint ID.



## Validation and remaining limits



See VALIDATION.md and self-test-results.txt for this candidate's local results. Synthetic tests and offscreen WPF checks do not establish live RF quality, sustained disk/USB throughput, physical unplug recovery, or hardware listening endpoint acceptance. Existing user acceptance of earlier releases does not imply acceptance of 0.11.0.



Not implemented: CW decoding, additional digital protocol decoders beyond those described above and advanced RDS services, more than two receiver workspaces, WiNRADiO adapter, PTT/CAT, trunk tracking, RadioReference import, NIST/signal-derived clock sync, arbitrary raw/SigMF import, independently tunable replay channels or swept spectrum overviews. PC/OS time is used. Future architecture requirements remain in ROADMAP.md.



## Build and diagnostics



Requires Windows and .NET 10 SDK; NAudio 2.2.1 is used under its MIT license. The native RTL library is downloaded separately when requested. This program implements its own receive processing and does not integrate DSDPlus or Unitrunker executables.



Run build.ps1, or publish src/MRS.csproj for win-x64 with --self-contained true. MRS.exe --self-test writes self-test.txt; MRS.exe --ui-smoke uses an isolated diagnostic profile and writes PNG previews. Tests do not open RF/audio hardware or download libraries. Keep diagnostics in a writable folder. Normal reception is offline; network access is used only for operator-requested RTL library setup. Session logs may include local recording paths.

















## Included in 0.12.2



Experimental 3.2 MS/s RTL capture is available in this package. Choose the sample rate, then Full span to view 3.2 MHz. Actual clean passband is smaller near filter edges. I/Q storage is approximately 6.4 MB/s per receiver before file overhead. If reception or audio breaks up, return to 1.92 MS/s. Hardware performance at 3.2 MS/s must be tested on your station; older archives remain unchanged.





KE8VPC appears in the lower-right footer. Quick check: select 3.2 MS/s and Full span, listen on a known station, try A/B together, then record and replay a short I/Q capture. Return to 1.92 MS/s if the USB connection cannot sustain the experimental rate.










0.21.0-preview2 corrects high-rate HackRF ADS-B input overruns. Above 4 MS/s,
the aviation worker shifts/filters/downsamples its own input while retaining
the wide main spectrum. See AVIATION.md and ACCEPTANCE-0.21.0-preview2.md.

0.21.0-preview3 removes receiver DC in the ADS-B branch. The supplied small-offset
HackRF recording now decodes; see AVIATION.md and ACCEPTANCE-0.21.0-preview3.md.
