# Preview validation

## 0.19.0-preview1 activity scanner — October 12, 2026

Release publish succeeded. All **901 local self-tests** passed, including new tests for noise-only calibration, integrated RF-power conversion, impulse rejection, capture-center/edge rejection, strongest-first selection, minimum activity, Hold, quiet release, Skip/clear, locked scan range, manual override, channel-grid rounding, insufficient-resolution rejection, local noise gradients, persistence/deduplication and preservation of user metadata. Raw Gaussian-noise I/Q at 4K and 32K FFT sizes produced estimated channel floors of about -43.2/-43.1 dB versus -43.3 dB measured by the production demodulator.

The full WPF smoke passed. The new scenario uses a synthetic HackRF worker with its 250 kHz capture offset enabled and independently generated multi-signal spectra. It checks digital channel selection and Skip without changing capture center, worker ID or viewport; saves two discoveries; confirms slider override and manual-tune cancellation; and clears state on reset. This is simulated receiver/UI evidence, not actual USB hardware reception. The level-line render and Scan tab screenshot were visually inspected. Existing saved-click and TV-reference checks remain in the suite.

Operator acceptance for all four features is in ACCEPTANCE-0.19.0-preview1.md. Live RF reliability, performance under the user's selected wideband rate/FFT settings, crowded-spectrum floor estimation and precise discovered centers remain field acceptance items. ATSC decoding and hardware drivers are unchanged.

## 0.18.0-preview2 setup dependency order — October 8, 2026

Preview1's alphabetically generated explicit package lock allowed GDK's post-link helper to run before GLib was linked. The operator reported Windows loader dialogs for gmodule-2.0-0.dll and glib-2.0-0.dll. The original readiness check imported GNU Radio but did not exercise this helper, so it missed that installation defect.

Reordered all 108 unchanged package URLs/hashes from their actual dependency metadata (no dependency cycles or missing packages). A new isolated Windows installation completed successfully with GLib before GDK. Expanded runtime checks exercise the GDK helper and ffprobe/ffplay. Fault injection in the private test runtime removed each reported DLL separately; both probes returned a structured setup error without launching the broken helper, and passed again after restoration.

855 automated checks and full UI checks passed. The clean runtime recovered generated video/audio at 9.6 MS/s with a 1 MHz offset through the application streaming path, raw-I/Q replay queue and localhost playback delivery; cancellation passed. Physical RF hardware was not opened. Over-the-air acceptance remains pending.

## 0.18.0-preview1 ATSC 1.0 HDTV — October 8, 2026

Windows publish passed with 854 automated checks and full offscreen WPF/UI replay checks. Added coverage includes physical-channel centers, minimum rate and complete-channel coverage, capture metadata changes and nonblocking queue overflow. HDTV layout was rendered and visually inspected.

The pinned runtime installer completed in the Windows user-local application-data folder and its decoder probe passed. Real GNU Radio ATSC processing recovered generated MPEG-2 video and AC-3 audio from synthetic 9.6 and 20 MS/s unsigned-I/Q recordings with a 1 MHz offset. The Windows C# stdin path, raw-I/Q receiver replay event path, localhost UDP transport delivery and cancellation after decoder startup passed. Recovered video was rendered and visually inspected. Timing measurements are development-machine observations, not a live performance guarantee.

No physical receiver was opened by these tests. Over-the-air reception, sustained live processing, antenna/gain suitability and playback on the user's station remain acceptance gates. See HDTV.md. The TV runtime is downloaded separately; the main EXE does not embed it.

## 0.17.0-preview3 RF spectrum detail - October 7, 2026

Windows x64 publish and all 839 automated checks passed. New tests cover real-sample FFT accumulation at 4K/16K/32K/64K, signed frequency placement, tone calibration, uneven USB boundaries, unchanged input I/Q, capture/resolution reset, close-tone separation, compact waterfall storage, dynamic display sizes and saved resolution preferences. Full synthetic WPF UI checks passed, including a high-detail spectrum/waterfall render. The previous 64K FFT precision failure is avoided with directly calculated double-precision roots.

These are synthetic and package checks; sustained live HackRF/RTL performance at high detail is pending user evaluation. RF FFT/display processing changed; audio decoding and raw recording contents did not. The operator previously confirmed preview2 automatic offset works.


## 0.17.0-preview2 HackRF capture offset — October 7, 2026

Self-contained Windows x64 publish passed. All 813 automated checks passed, including positive/negative/disabled offsets, hardware and bandwidth limit rejection, per-identity preference serialization, worker startup/live retuning, gain/rate continuity, offset recording metadata and replay without double application. Full offscreen WPF checks passed, including left Start/Apply and top frequency entry, visible capture readout and disabled live offset editing. No physical receiver was opened or live settings changed during these tests.

Field evidence before this build: the operator reports clearer manual offset reception and distortion when tuned on center. The October 7 sample confirms 162.650 MHz capture and 162.400 MHz listening at 9.6 MS/s. Automated-offset build acceptance on the physical HackRF is still pending. Offset changes require stopping; hardware retuning has a short reception gap. Current HD conversion requires disabling offset.


## 0.17.0-preview1 HackRF receive milestone — October 4, 2026

Release publish succeeded. All 800 automated checks passed, including all prior RTL/data/P25 checks and new HackRF ABI layout, signed-to-unsigned sample conversion, gain validation, 6 GHz framing/retuning, command rejection, backend ownership, supported-rate demodulation, anti-alias rejection, live gain/rate acknowledgments, recording provenance and receiver-free replay tests. The shared input FIR now uses vector operations; block continuity and existing RTL anti-alias checks passed after that change.

Full offscreen WPF UI checks passed: HackRF-specific controls, 6 GHz frequency entry reflection, LNA/VGA changes, rate changes, full sampled spectrum span, switching back to RTL, and replaying HackRF data after an RTL selection. Replay clears the stale RTL hardware selection rather than attempting to open it under HackRF. Existing data/P25/workspace screenshots and assertions also passed. All UI test devices and received signals were synthetic.

The bundled native Windows library loaded and its required RX exports/filter table validated. Read-only `--hackrf-probe` exited 0 with an empty device list. No HackRF was exposed on the development PC. Real H4M/Mayhem v2.4 compatibility, antenna reception, USB continuity, simultaneous physical RTL/HackRF use, disconnect/reconnect, sustained high-rate recording and hardware HD reception remain station acceptance gates. No radio was transmitted, flashed, or assigned a new USB driver.

## 0.11.0 Two-receiver milestone

September 29, 2026: all 276 synthetic checks passed. The expanded offscreen UI workflow verifies two concurrent synthetic receiver workers, simultaneous I/Q recording, independent frequencies/rates, retuning A without changing B, stopping A while B continues recording, ownership conflicts, and saving/reopening Receiver B preferences. A/B layouts were visually inspected. The final self-contained publish and extracted-package checks are recorded in the external 0.11.0 package-validation report.

AFSK timing changes retain nominal Bell 202 frequencies, use a 48-sample correlation hypothesis alongside the 32-sample hypothesis, and reduce transition correction from 0.65 to 0.25. Existing noisy/weak/clock-offset, framing, repeated-frame, chunk-boundary and bounded-history checks pass. Offline processing of the user's two complete field I/Q files recovers three checksum-valid KE8VPC-15 position reports in each: the first file previously yielded two; the second previously yielded three. Failed candidates are two and zero respectively with the updated settings. Independent CRC recomputation confirmed the recovered payloads. Raw user recordings are not included in the source or runtime archives.

The first capture has substantial I/Q endpoint clipping; the second, recorded at manual 0 dB gain, has none. These captures establish live APRS reception for the recorded path and a specific decoder improvement, not universal sensitivity or complete packet-loss statistics. They do not identify the physical dongle because receiver identity metadata was absent.

No physical receiver or audio output was opened during this release's automated tests. Simultaneous physical V3 reception, shared USB throughput, hardware output routing and unplug/reconnect remain station acceptance gates. Frequency libraries and settings are separate per A/B workspace; this release has no P25 decoding or automatic trunk voice following.
## 0.10.0 AX.25 / APRS packet milestone

September 29, 2026: Windows x64 self-contained Release publish passed with zero warnings/errors. All 271 synthetic checks passed (41 additional packet/receiver checks). Expanded offscreen WPF smoke checks passed with an explicit success marker and exit status. WAV, I/Q packet inspection and simulated V3 selection layouts were inspected.

- An independent fixture encoder checks CRC, NRZI/HDLC bit stuffing, repeated frames, incomplete/aborted/oversized frame recovery and malformed-address handling. Valid checksums do not bypass syntax checks.
- AFSK recovery covers signal and sample-clock offsets, added noise, weak amplitude, silence/noise rejection and identical results across single-sample versus whole-buffer input. Stereo duplicate audio yields one observation.
- End-to-end NFM I/Q fixtures recover packets at 240, 960 and 1920 kS/s. Audio reaches the decoder with listening volume zero and the application gate closed.
- APRS checks cover uncompressed coordinates, invalid/ambiguous positions, timestamp syntax, message IDs, acknowledgment syntax, status and unsupported payload preservation. Partial state resets prevent joining packets across sources.
- Queue/history bounds, overflow reporting, filtered JSON export, clearing and malformed-frame fuzz cases pass. The UI replay checks show failed FCS on request and preserve replay/frequency attribution.
- Synthetic V3 identities remain distinct across device-index changes; branch preferences follow unique identities. Generic/duplicate serials cannot authorize automatic selection or reconnect.
- The UI diagnostic launcher now uses explicit shutdown so a failed assertion returns a failure status. Stale success/error markers are removed before a run.

No live RF/audio hardware was opened. These fixtures do not establish live AX.25/APRS interoperability, sensitivity, physical V3 performance, hardware audio levels, USB throughput or reconnect acceptance. The decoder is receive-only 1200-baud AFSK with limited APRS interpretation; see PACKETS.md. Earlier release archives remain unchanged. Distribution extraction and source-build results are recorded in the separate 0.10.0 package-validation report.
## 0.9.0 WFM broadcast milestone

September 21, 2026: The user reported "tests passed" for the delivered 0.9.0 preview. Record this as a user-reported milestone test pass. Individual stations, devices, sample rates, decoded fields and test duration were not specified, so no additional per-feature or sustained-performance results are inferred. The local validation below describes the pre-release checks; release archives remain unchanged.

September 18, 2026: Windows x64 self-contained Release publish passed. All 230 synthetic tests passed, including 31 new broadcast checks. Expanded offscreen WPF smoke checks passed. MPX, RF, audio spectrum and stereo waveform screens were visually inspected.

- Stereo tests cover arbitrary pilot phase, -2/0/+2 Hz pilot offsets, at least 26 dB separation of independent L/R tones in the composite path, sample-block continuity, Force mono and pilot loss.
- End-to-end unsigned-8 I/Q at all three supported rates recovers stereo with over 23 dB separation of fixture tones while decoding RDS station name and RadioText.
- RDS tests cover independent carrier acquisition without a stereo pilot at -3/0/+3 Hz offsets, arbitrary symbol phase, strict block/offset validation, corrupt-group rejection, repeated name/text confirmation, 2A/2B text, PI and A/B resets, no false station on silence/deterministic noise, recovery with added noise and metadata age after signal loss.
- WAV validation checks two-channel 48 kHz recording, exact payload preservation and independence from RF squelch. Existing mono capture/file, receive-mode, recording and recovery checks remain passing.
- UI checks replay a timed synthetic WFM I/Q recording, acquire stereo/RDS, inspect all four views, switch Force mono, clear station metadata on paused seek, and restore the normal layout on Stop. Diagnostic profiles and signals are excluded from packages.

No live RF/audio hardware was opened. These results do not establish live station sensitivity, multipath performance, physical endpoint playback, sustained USB/disk performance or full RDS standards conformance. Initial RDS character support is basic printable ASCII with explicit replacement for unsupported characters. De-emphasis is fixed at 75 us. Advanced RDS services and HD Radio are not included. Earlier release archives remain unchanged.

## 0.8.1 Direct sampling

September 14, 2026: Windows x64 self-contained Release publish passed. All 199 synthetic checks passed, including direct I/Q frequency limits, 7 MHz and 1 Hz tuning, unavailable tuner gain, command/frame branch validation, recording metadata and hardware-free replay. Expanded offscreen WPF smoke checks cover per-receiver branch restoration, receiver changes finalizing recording, replay branch locking and nonnegative spectrum bounds. Screens were inspected. Native initialization ordering and branch readback were reviewed against the pinned library source; no live USB device was opened. Useful lower-frequency response, correct physical branch and hardware tuning accuracy remain unverified. The 0.8.0 archives remain unchanged.

## 0.8.0 Listening and spectrum milestone

September 14, 2026: Windows x64 self-contained Release publish passed with zero warnings/errors. All 177 synthetic tests passed. The expanded WPF smoke workflow also passed and receiver/replay layouts were inspected. Tests run with synthetic workers and isolated profiles; no RF/audio hardware, USB driver setup or network actions were performed.

- New integration checks cover independent native command requests, in-capture software tuning, acknowledged live gain/rate changes without reopening the worker, recording continuity and v3 per-frame gain/channel/rate metadata.
- Offset AM recovery verifies mixing before decimation and oscillator continuity. Existing AM/USB/LSB tone, sideband rejection and block-continuity checks still pass across the three supported rates.
- Replay checks cover sparse indexing, valid seek boundaries, clock/audio freeze while paused, metadata restoration on paused seeks, resume at the destination, cancellation, v1/v2 compatibility and incomplete-tail reporting.
- UI smoke verifies gain/rate/mode/filter/AGC changes during recording, unchanged worker identity, Recording tab visibility, paused seek, loaded-file retention after Stop, restart and controls after EOF.
- Inspection found and corrected stale channel metadata after paused seeks. An asynchronous control continuation deadlock exposed by synchronous test callers was corrected before the final candidate passed.

Remaining station acceptance: live gain/rate changes and native settling behavior; AM/SSB listening and hardware frequency accuracy; Windows output endpoint playback/restoration/unplug handling; sustained CPU/USB/disk throughput and physical receiver recovery. Local tests do not establish these hardware outcomes. The 0.7.0 package remains unchanged.

## Unpackaged AM / SSB checkpoint

September 14, 2026: compilation passed without warnings/errors, all 155 synthetic checks passed, and the existing offscreen WPF smoke workflow passed. The RF filter overlay was visually inspected. AM/USB/LSB each recovered a 1200 Hz tone at 48 kHz output from 240/960/1920 kS/s inputs. Uneven input-block processing was byte-identical to continuous processing. USB/LSB rejected an opposite-sideband 1200 Hz test tone by more than 40 dB after settling, measured before audio AGC. Mode metadata round-tripped and playback output matched the corresponding direct demodulator. These are controlled-tone tests, not broad adjacent-channel, fading, speech-quality, hardware accuracy or sustained real-time performance acceptance. No new executable package was published.

## Unpackaged capture metadata checkpoint

September 14, 2026: Release compilation passed with zero warnings/errors. All 128 synthetic self-tests passed, including version 2 signed-format storage above 4 GHz, independent usable-bandwidth/rate preservation, raw byte ownership/round trip, throughput calculation, invalid capture/alignment rejection and explicit unsupported-DSP rejection. A 6 GHz unsigned-8 recording at a supported rate replayed through the receiver without losing frequency precision. Existing version 1 compatibility, receiver recovery and recording tests remain passing. The offscreen WPF smoke workflow passed, including new version 2 recording through the actual UI and replay. No live hardware or release-package acceptance is inferred; published 0.7.0 packages are unchanged.

## 0.7.0 Receiver and I/Q recording milestone

- Release build/publish passed without warnings/errors. All 112 self-tests passed, including 23 new checks for stable identity across index changes, duplicate/default/missing identity refusal, bounded reconnect backoff/cancellation, I/Q ownership/metadata/timing round trips, clean/incomplete file endings, invalid signatures, worker-free replay, recorded gaps, retune recovery, prompt replay stop and actual synthetic-worker recording across a live retune.
- The WPF smoke check recorded synthetic I/Q, finalized it and reopened it through the actual replay UI without an SDR worker. Receiver and replay controls were rendered and inspected. The profile and recordings are isolated test data.
- Reconnect tests inject discovery results and delays; they do not establish physical USB unplug/replug behavior. I/Q tests use local synthetic files, not sustained full-rate RF capture or disk-failure hardware tests. User acceptance of 0.7.0 is pending.

### 0.7.0 station test

1. Find and select a receiver, start it, then close/reopen M.R.S. and Find again. A usable unique serial should restore selection even if its index changes. Default/duplicate serials should remain manual and must not enable automatic recovery by index.
2. On an identifiable receiver, enable reconnect, start reception, unplug/replug it and verify the same receiver resumes with the last applied settings. Confirm another receiver is never substituted, recordings finalize on loss, and Stop cancels pending retries.
3. Record 10–20 seconds of I/Q with a frequency-only retune in the middle, Finish I/Q, then stop reception and Open I/Q recording. Check frequency/rate changes, spectrum, audio, recorded timing gaps and Stop. Replay must not open the SDR.
4. Repeat a longer capture at the highest rate your station sustains. Observe disk growth, audio continuity and any recording/recovery messages. Record WAV during replay if you want an audio export; its time omits reception gaps.

## 0.6.0 Interactive tuning milestone

- User feedback on September 14, 2026: "passed" for the delivered 0.6.0 milestone. Recorded as an overall successful user test; individual sample rates, hardware paths and sustained-test duration were not specified.
- Release compilation/publish passed with zero warnings/errors. All 89 local self-tests passed, including 27 new cases covering checkbox migration/direction, RF/waterfall click mapping, audio-only and Ctrl-click exclusions, view-span bounds, I/Q metadata validation, higher-rate NFM/WFM audio rate/tone recovery, uneven block boundaries, anti-alias rejection and live retuning in one worker with one WAV recording.
- The WPF smoke test toggled RF squelch without losing its threshold, applied a frequency-only change through Start / Apply, clicked to a second frequency, verified acknowledgement and frequency field updates, and confirmed that the worker stayed the same. It also rendered the receiver controls and view-bandwidth selector at 960 kS/s with a 240 kHz view; both renders were inspected.
- Developer checks used explicitly labeled synthetic worker data and an isolated profile; they did not exercise physical SDR, radio audio, antenna or USB throughput. The subsequent user-reported pass is recorded separately above.

### 0.6.0 station test

1. Start a known FM channel at 240 kS/s and adjust RF squelch. Confirm lower/looser left and higher/tighter right; toggle the checkbox and verify the threshold is retained.
2. Click another frequency in the spectrum and waterfall, including after zoom/pan. Confirm tuning-field/marker agreement, continued receiver operation and a history reset. Ctrl+click should select a saved marker without tuning.
3. Record a short WAV while making frequency-only changes. Confirm the recording remains open and the applied frequencies appear in the log. Brief tuning gaps are omitted from WAV time; it is not a gap-preserving I/Q recording.
4. Select 960 kS/s and then 1.92 MS/s using Start / Apply. Check applied rate, capture width, NFM/WFM sound, CPU load and sustained operation. A rate change finalizes recording. Return to 240 kS/s if the station cannot sustain a higher rate.
5. Choose View bandwidth under Display; verify it changes the view without changing acquisition rate, and hover/markers stay aligned. Restart to check saved rate, squelch checkbox/threshold and display settings.

## 0.5.0 Receiver refinement milestone

- Release publish and offscreen WPF smoke test passed. Receiver render shows RF threshold/status controls and removal of Station roadmap.
- All 62 synthetic self-tests passed. Thirteen new checks cover RF threshold opening/closing, hysteresis, hang timing/recovery, OFF, invalid measurements, quiet audio passing an open RF gate, loud noise muted by a closed gate, rejection of an off-channel carrier by channel filtering, an unmodulated centered carrier, 6 dB power tracking, and uninterrupted analysis/WAV samples while RF listening is gated.
- No live RF or audio hardware was exercised by these checks. User acceptance of 0.4.0 display controls and 0.5.0 receiver changes remains pending.

### RF squelch station test

Receive a known local analog channel. Start at RF squelch OFF, observe idle-channel level, and raise the threshold until noise mutes. Confirm the desired carrier opens it and quiet speech pauses do not close it. Check weak signals, threshold chatter, loss of carrier and Stop/restart. Adjust again after changing tuner gain or NFM/WFM mode. Record a short WAV while the listening gate is closed and verify the original received audio is retained. Restart M.R.S. and check the RF threshold restores independently of the audio-input gate. RF levels are relative; center/DC offset and interference can cause false opening. FM noise estimation and calibration are future refinements.

## 0.4.0 Spectrum and waterfall milestone

- Release compilation and self-contained Windows x64 publish passed with zero warnings/errors.
- All 49 local self-tests passed. Display checks cover pointer-anchored zoom, frequency/marker mapping, capture-bound clamping, settings normalization, timestamped history and missing-frame gaps, input-buffer preservation, palettes, theme isolation, preference persistence and named preset reload.
- Offscreen WPF smoke rendering completed for audio, synthetic RF, the signal editor and four plot themes. Theme renders were inspected for readable labels and cursor/marker alignment at 4× zoom. This uses an isolated diagnostic profile; no demo entries are added to the user's library.
- Display controls do not modify acquisition or recorded samples. Live receiver performance, mouse/keyboard comfort and sustained operation with this milestone still need the user's station test.

### Display station test

Receive a known signal and open Display. Zoom at the signal, pan and reset full span; confirm its hover frequency and saved marker stay aligned. Adjust display gain and manual/auto levels, then change visible history and verify the seconds scale. Try themes, palettes, averaging and peak hold. Save a named preset, change settings, reload it and restart M.R.S. to confirm persistence. Check that listening and a short WAV recording remain normal while adjusting the display.

## 0.3.1 empty-library defaults

The user reported that the Frequency Manager test passed and clarified that the example folders were illustrative only. Automatic starter-folder creation has been removed. Release build and all 35 self-tests passed, including empty new-library initialization. Existing libraries are loaded as-is; no ongoing folder-name-based deletion is performed by the application. A separate user-authorized cleanup of the current local library removed the observed example folders after creating a backup, reparenting retained signals rather than deleting them.

## 0.3.0 Frequency Manager

- Release build passed without warnings/errors.
- All 34 local self-tests passed. New checks cover exact fractional-MHz conversion, sub-Hz/invalid input rejection, persistence of nested entries and metadata, duplicate frequencies, cycle prevention, inherited marker visibility, folder deletion/Undo, backup creation, stale-window protection and storing frequencies outside active receiver coverage.
- Offscreen rendering exercised the sidebar, RF markers, hover label and signal editor using an isolated diagnostic profile. The renders were visually inspected; no synthetic records were added to the user's real library.
- The user reported “test passed” for the Frequency Manager. This records the overall user result without inferring which individual checklist items were exercised.

### Frequency Manager station test

Save a received frequency into a folder with notes and protocol labels. Check the RF marker, then move the entry into a nested folder, search for it and restart M.R.S. to confirm persistence. Hide/show the folder and use explicit Tune with a supported mode. Delete and Undo a test folder before closing. Unsupported stored modes/frequencies must remain intact and report why they cannot be tuned.

## 0.2.0 single-receiver preview

User feedback: “rf pass”, following confirmation of RTL2838 identification on the user's NESDR dongles. Basic live RF reception is recorded as user-confirmed. The report did not specify tested frequencies, NFM versus WFM, gain settings, recording, reconnect behavior or test duration; those individual checks are not inferred from this result.

- Release compilation passed without warnings/errors.
- All 23 self-tests passed, including generated narrow/wide FM recovery, correct 48 kHz output, preservation of DSP state across input blocks, RF FFT orientation and the isolated worker/pipe/stop path.
- Audio and synthetic RF windows were rendered and visually inspected, including the 146.520000 MHz hover label. Synthetic RF is explicitly labeled in the test render.
- The pinned native-library download, checksum and installation succeeded. A native probe loaded the library successfully and returned an empty device list. No accessible receiver was discovered, so this is not a live RF or Windows USB-driver acceptance result.
- Physical NESDR tuning, gain behavior, demodulated sound quality, unplug/reconnect and sustained reception still require station testing.

### RTL station test

Connect one NESDR with antenna, close other SDR programs, click Find (Set up RTL library if needed), select a receiver and a known active frequency, then Start / Apply. Try WFM on a strong FM broadcast station and NFM on a known local analog channel. Select PC speakers/headphones and enable monitoring. Verify RF hover, manual/auto gain changes, retuning, short WAV recording and Stop. Record errors from the session log if a step fails. Follow README.md for the separate WinUSB-driver prerequisite.

## 0.1.1 hover readout

Adds an audio-frequency cursor to spectrum and waterfall. Release publish passed and all 13 audio self-tests passed. The UI smoke render positioned the cursor at 1500 Hz over the generated tone; the resulting label and marker were visually inspected and aligned with that tone. The user subsequently reported that it works.

## 0.1.0 foundation

Verified locally on September 12, 2026:

- Release compilation: zero warnings and zero errors.
- Self-contained Windows x64 publish completed.
- Executable self-test: 13 checks passed, including FFT frequency, RMS calibration, silence, listening squelch/volume, preservation of input samples, WAV format/duration, file replay, recording finalization and stop cancellation.
- Offscreen WPF window test: opened the actual application, played its synthetic tone and rendered the window successfully. Render inspected for readable labels, controls and spectrum.

The test tone is generated software data. Developer validation did not exercise live audio capture, physical SDR, radio RF, Digirig audio loop, PTT or drivers. No protocol decoder is included in this checkpoint.

## User test feedback

The user reported “passed” for the 0.1.0 preview. The specific checklist steps and hardware paths exercised were not stated, so this records a successful user-reported preview test without marking every hardware acceptance item complete. The user also requested a future spectrum mouse-hover frequency readout; it is recorded in ROADMAP.md.

## Station acceptance checklist

1. Extract ZIP and launch MRS.exe without a separate runtime installation.
2. Load the test tone; verify the 1.5 kHz peak and approximately −9 dBFS input.
3. Connect Digirig Mobile, refresh inputs and select its USB audio input.
4. Start radio receive audio; verify the input level and waterfall change with actual signals.
5. Select PC speakers/headphones explicitly; verify monitoring, volume and squelch. Avoid radio-connected outputs during receive testing because VOX can key from audio.
6. Record a short WAV, finish it, reopen it and compare playback to the received audio.
7. Restart the application and check preferences. Monitoring must remain off until explicitly enabled.
8. Report device names, exact log errors and the step that failed if any behavior differs.





# Unpackaged spectrum navigation checkpoint

September 14, 2026: Release compilation passed with zero warnings/errors using the workspace .NET SDK. All 118 synthetic self-tests passed, including new checks for a 64 MHz viewport above 6 GHz, capture-edge clamping, drag suppression of tuning, small-movement click tuning, center-view zoom preservation and full-span restoration. Existing receiver, recovery and I/Q tests passed. The offscreen WPF smoke workflow exited successfully; the RF navigation render was inspected. These results cover source development only, not a new release package or live RF acceptance. Existing 0.7.0 distributables remain unchanged.






## User acceptance and pending UI fix — September 29, 2026

User reported the latest compact 0.11.0 build passed, with a readability defect in the WFM broadcast panel. The supplied screenshot shows black heading, pilot/stereo status and channel labels against its dark background. Source now sets the broadcast container text foreground explicitly to the light application text color; controls with their own foreground retain it. This correction is pending the next milestone package. The user's overall pass does not itemize every hardware acceptance case.

Validation of the correction: Release build completed with zero warnings/errors; isolated self-contained UI smoke completed with exit 0. Broadcast screenshot visually checked for light heading, stereo status and L/R labels while dropdown text remains dark on its light background. No replacement release ZIP produced.

Shared frequency phase: self-contained development publish succeeded; 284 automated checks passed and UI smoke exited 0. New checks cover hierarchy-preserving legacy import, unchanged legacy file, persistent migration flag, both-panel marker refresh, stale-write rejection, shared delete/Undo and malformed-input preservation. Tests used isolated temporary libraries; live user profiles were not migrated. Distribution ZIPs remain unchanged pending the next milestone.


Band reference phase: development publish succeeded; all 293 self-test checks and UI smoke passed. Dark/light rendered views were visually checked for the 144–148 MHz amateur bar and CB channel order/labels. Tests cover boundaries, channel counts/gaps, preference serialization/theme retention, hover provenance and suppression in audio views. No release ZIP rebuilt; no RF hardware required for these display checks.


September 29, 2026 — all-band ARRL source milestone: isolated Windows x64 development publish completed without warnings/errors. All 303 synthetic checks passed; WPF UI smoke passed. New checks cover 28 band groups, LF boundary precision, HF calling markers, current-page 60 m dial/segment distinctions, 13 cm gap, 9 cm cutoff, overlapping 70 cm recommendations, 64-bit microwave values, independent preferences and hover provenance. Dark HF/70 cm and light microwave renders were visually reviewed. Dataset contains 256 interval/point records. No live receiver was used and no distribution ZIP was rebuilt.


September 29, 2026 — overlay placement: development publish passed with zero warnings/errors; all 309 synthetic checks and WPF UI smoke passed. New checks exercise bottom-row hover, spectrum/waterfall click mapping, rejection of clicks in the frequency-label gap, tooltip clearing and placement serialization/theme retention. Combined U.S./ARRL bottom layout was visually inspected. No hardware used; distribution archives unchanged.


September 29, 2026 — overlay readability: development publish, all 309 synthetic checks and WPF UI smoke passed. Visually reviewed the wide two-meter view with both bottom reference layers: interval text uses ellipses and very narrow intervals retain blocks without broken text fragments. ARRL point hover now reports one marker frequency. Release archives unchanged.


0.12.0 compact milestone packaged: extracted runtime passed 309 synthetic checks and UI smoke; extracted source built with zero warnings/errors. See ../MRS-0.12.0-PACKAGE-VALIDATION.txt and ACCEPTANCE-0.12.0.md. Station acceptance pending.

Service overlay expansion: development publish passed; 313 synthetic checks and UI smoke passed. AM, aviation and marine renderings visually inspected. No live hardware used; existing release ZIPs unchanged.
0.12.1 crash fix: Windows Application event at 2026-09-29 20:05:54 identified ArgumentException (negative rectangle height) in SpectrumView.DrawMarkers. A compact-layout regression reproduced the same exception with the fix removed. Fixed rendering suppresses overlay rows below 240 display units and defensively bounds shading height. Extracted hotfix passed 314 self-test checks, including 24 combinations of reported frequencies, panel heights and placement. Hardware remains for user retest.
Experimental 3.2 MS/s source addition: Windows x64 development publish passed; all 332 synthetic checks and UI smoke passed. Coverage includes AM/SSB/FM tone recovery and timing, uneven blocks, opposite-sideband/alias rejection, WFM stereo/RDS, AX.25, and recorded 3.2 MS/s replay. Updated the synthetic RDS generator to use exact rational timing rather than integer-only interpolation. No physical SDR tests performed; 0.12.1 ZIP remains unchanged.
0.12.2 packaged at user request. Extracted runtime passed all 332 synthetic checks and UI smoke; extracted source built without warnings/errors. Version verified. Hardware acceptance pending. See ../MRS-0.12.2-PACKAGE-VALIDATION.txt.
User-supplied 0.12.2 test recording inspected: 59,099,570-byte version-3 MRSIQ file, 1,800 frames, clean completion marker. Capture/channel 102.1 MHz, WFM 190 kHz, 3.2 MS/s, tuner gain 29.7 dB. 58,982,400 sample bytes represent 9.216 seconds. Frame arrival interval max 6.4404 ms; timestamp span 9.2398282 s. Actual RtlReceiver replay completed successfully and emitted 9.216 seconds of 48 kHz mono analysis audio. This verifies this recorded capture/replay path, not long-run USB sample-loss absence, subjective audio quality, or simultaneous two-receiver stability. Original user recording was read-only.

## HD recording preview 0.13.0-preview1 — September 30, 2026

- Managed publish succeeded; 344 synthetic checks passed, including 12 HD conversion/file validation checks.
- UI smoke passed; HD tab image inspected for readable labels and controls.
- Windows nrsc5 known-sample decode and MRSIQ simulated 3.2 MS/s conversion/decode each produced 2,028,508-byte audio WAVs plus station/title messages. Native executable ran with only its staged DLLs, without toolchain PATH changes.
- Local test executable: work/mrs-hd-check/MRS.exe; SHA256 ff344c3551497af09ff8e027f45dc9d14e1635eff2c42aa3ddf5cf5b33f9ba6a.
- Local RF HD reception and live HD listening remain unverified/not implemented respectively. No receivers were opened for these tests. No distribution ZIP was produced.


## 0.13.0-preview2 packaged HD / Data milestone

See ../MRS-0.13.0-preview2-VALIDATION.txt for final package checks and checksums. 350 synthetic checks, extracted UI smoke, native streaming/replay PCM recovery, cancellation, archive hash equality and extracted-source build passed. Local RF HD and audible speaker output remain unverified.


User acceptance — September 30, 2026
The user reports that 0.13.0-preview2 passed with the provided sample following the internal HD playback acceptance instructions. Sample-based playback is accepted. No local station/data could be decoded; physical over-the-air HD reception remains unverified. This does not establish that local stations lack HD or that the live RF path has passed. No new package or executable was built for this acceptance update.

## P25 control-data preview — 0.14.0-preview1, September 30, 2026

Self-contained Windows publish completed without warnings/errors. All 377 automated checks passed, including independent OP25 packet vectors, BCH correction limits, TSBK CRC rejection, one/two/three-block framing, noisy/inverted/offset C4FM, arbitrary input boundaries, 100 ppm symbol timing offset, 240 kS/s and 1.92 MS/s I/Q through the 12.5 kHz NFM discriminator, bounded queues and reset behavior. Native HD direct-stream and receiver-replay regressions each recovered 2,015,232 stereo PCM bytes plus KUT metadata/HD1-HD2 services; cancellation passed. No physical receiver or audio endpoint was opened. Live P25 and CQPSK/LSM acceptance are not claimed. Final package checks are in ../MRS-0.14.0-preview1-VALIDATION.txt.

Development UI smoke also passed: P25 I/Q replay recovered exactly 8 headers / 24 CRC messages with squelch closed; filtering and export retained P25 protocol and recorded frequency. Updated Data view was visually inspected. Physical station testing remains pending.

## CISA NIFOG overlay — next milestone, September 30, 2026

Development Windows x64 publish passed without warnings/errors. All 401 automated checks and the full UI smoke passed. Reviewed NIFOG 2.02 PDF tables visually; independently cross-checked all 227 standard paired-table rows against layout text extraction. Added seven receive-only weather references and twelve SAR operations references: 246 rows, 236 RF centers. Exact frequencies, RX/TX split directions, federal/700 NACs, recommended-tone and proposed/inland qualifications, grouped aliases, embedded-resource loading, actual checkbox save/reload, dark/light/bottom rendering, long-detail scrolling, audio/MPX exclusion and all-three-layer compact layouts were validated. Dark/light bars and the wrapped shared-frequency detail panel were visually inspected. No physical receiver used.

Existing 0.14.0-preview1 runtime/source ZIP hashes remain unchanged. No new distribution package was built; this addition is queued for the next milestone. Source provenance and coverage are in NIFOG-OVERLAY.md. Development checks are under work/mrs-nifog-check.

## CTCSS / DCS and NIFOG preview — 0.15.0-preview1, September 30, 2026

Self-contained Windows x64 publish passed without warnings/errors. All 590 automated checks passed, including 51 CTCSS tones with noise/frequency offset, all 104 DCS codes in both polarities, six independent manufacturer codeword constants, nominal-rate offsets, DC/noise, off-list tones, hum, damaged codewords, silence, strong voice-band components, P25 false-identification rejection, short transmissions, state transitions, independent pipelines, PCM and production NFM I/Q paths. DCS acceptance is deliberately based on repeated error-free words; error-correction performance is not claimed.

Full development UI smoke passed CTCSS WAV/live readout, DCS I/Q replay with closed application RF squelch, independent P25/CTCSS-DCS enablement, source/frequency/replay export, paused/stopped status and Receiver B preference save/reload. The Data inspector no longer repeats identical metadata details. Compact Data layout uses flexible table/details rows so both remain accessible. Standard and compact UI screenshots are inspected before packaging. No physical receiver, audio endpoint or PTT is used in automated checks.

Includes the previously validated NIFOG overlay. Only generated CTCSS, DCS and P25 recordings are bundled. Packaging now uses an explicit native-runtime file allowlist to exclude the old local recording found in the earlier decoder folder. The final archive, extracted-source and native HD regression results are recorded in ../MRS-0.15.0-preview1-VALIDATION.txt. Live subaudible reception on actual SDR/conventional-radio inputs remains a user acceptance gate.

## P25 Data grouping — source update after 0.15.0-preview1, September 30, 2026

Development self-contained publish succeeded without warnings/errors; all 604 automated checks passed. Added checks for 1,000 repeated observations in one summary row while raw history stays bounded, first-seen/count export, changed status fields, filter refresh, receiver/frequency/NAC/replay/session separation, distinct unknown raw payloads, grant changes, unchanged non-P25 behavior, view switching, bounded summary eviction with status retention and Clear resets. Full UI smoke verifies eight repeated network-status observations in one row, individual-message access, summary export and independent Receiver B preference persistence. Development evidence is under work/mrs-data-summary-check. User screenshot shows local CRC-valid site/network reception; no new RF capture or physical receiver was used during this change.

Existing 0.15.0-preview1 runtime/source ZIPs remain unchanged. The readability change is queued for the next milestone package.

## Session log tab and draggable Data divider — September 30, 2026

Development publish completed without warnings/errors and the full UI smoke passed under work/mrs-layout-check. Visually inspected the expanded spectrum without the old log panel, Data with its visible horizontal splitter, and the separate Session log tab with retained diagnostic text. Uses the standard WPF GridSplitter with previous/next row resizing, preview, minimum pane heights and keyboard support; physical mouse-drag acceptance remains to be checked by the user. No new distribution ZIP was produced and no hardware receiver was opened.

## Compact Data header follow-up — September 30, 2026

The prior minimum-window screenshot remained too crowded. Shortened checkbox captions, moved counters/help into an initially collapsed expander with bounded scrolling, and balanced the default split to two-thirds list / one-third details with an 80-unit minimum details height. Development publish and full UI smoke passed under work/mrs-compact-data-check. Added a minimum-size visual capture with four P25 summary rows; visually checked that the rows and details area remain visible. This is UI validation, not a new RF/decoder acceptance result. Existing release ZIPs remain unchanged.

October 1 P25 type visibility source check: 615 self-tests passed, including grouped/raw filtering, export, hidden history retention, and non-P25 isolation. Full UI smoke passed, including receiver B hidden-type preference save/reopen. No new packaged ZIP or live RF test.


October 3, 2026: user reported 0.15.0-preview3 passed. This accepts the provided P25 grouping/filter build; it does not establish acceptance for future channel-frequency decoding.


October 3 P25 identifier source milestone: 647 self-tests passed. Independent OP25-generated FDMA/VUHF/TDMA vectors traverse C4FM and pipeline; full UI I/Q replay resolves expected downlink/uplink/slot values and grants/status filtering. Tests cover missing/changed/reserved/zero tables, CRC/protected/manufacturer rejection, NAC/system/site and source/gap reset, integer JSON metadata, per-ID grouping, and monitored-frequency preservation. Build passed without warnings. Live RF channel-frequency acceptance remains pending. Existing Desktop ZIP unchanged.


October 3 expanded-control source milestone: 671 self-tests passed; clean publish and full UI smoke passed. Independent OP25 vectors cover dual grant 02, explicit grant 03, secondary 39 and adjacent 3C through C4FM and production I/Q replay. Checks include separate/absent uplink tables, distinct slots, neighbor identity isolation, CRC/protected/manufacturer rejection, grouped discovery rows, structured export and raw/grouped type filters. No live RF or packaged acceptance is claimed; Desktop preview3 unchanged.


October 3 persistent P25 Systems source milestone: 686 self-tests passed. Full UI replay smoke passed and P25 Systems screenshot reviewed after correcting first-show column collapse. Live/replay separation, independent file save/reopen, incomplete and conflicting identity handling, JSON export, bounded growth, and invalid-file preservation were verified locally. Desktop preview3 unchanged; real receiver acceptance remains pending.


P25 discovery annotations/frequency review source milestone: 698 self-tests and full UI smoke passed. Tests cover annotation retention through updates/reopen, clearing names, input length, selected live/replay action availability, target-vs-receiving identity, safe frequency drafts and independent library entries. Visual P25 Systems review passed. Field acceptance and user-driven frequency editor workflow remain pending; Desktop preview3 unchanged.


## October 12, 2026 — 0.18.0-preview3 navigation and TV references

Release publish completed successfully. All 872 local synthetic self-tests and the full WPF UI smoke passed. Added checks cover the saved-frequency mouse-down/up event path, complete frequency/mode/filter delivery, receiver-panel isolation, repeated clicks, folder/cancelled-drag/mismatched-release handling and shared-library refresh. TV checks cover RF 2–36 boundaries, VHF gaps, RF 37 exclusion, the RF 15 nominal pilot, independent checkbox persistence/theme retention, hover, bottom placement, all four layers at compact sizes, and exclusion from MPX. Dark/top and light/bottom reference screenshots were visually reviewed.

No live radio was opened for these tests. Hardware click-tuning and TV-overlay operator acceptance are described in ACCEPTANCE-0.18.0-preview3.md. The ATSC decoder is unchanged; earlier TV field reception limitations remain. Automatic noise-floor squelch and locked-spectrum discovery are one future milestone.
