# 0.24.0-preview2 acceptance — DMR / Cap+ / LTR

Extract the complete archive into a new folder; keep Receivers and Decoders beside MRS.exe. Confirm Trunk identification preview 0.24.0-preview2 in the header.

- [ ] Replay the supplied longer LTR recording: pending progress should reach 1/3, 2/3, then confirm area 0 / home 4 / idle 255 near 27.5 seconds at normal speed. A fourth burst should update the row. Replay must not auto-save live frequencies.
- [ ] In DMR / LTR enable decoding, select NFM and start with a 12.5–16 kHz filter.
- [ ] Clear observations and monitor a stable channel: isolated alternate color codes must not create rows; a confirmed identity requires three matching acceptable observations within ten seconds.
- [ ] Monitor each of the two CC6 channel groups. Check that frequencies remain separate; note system type, talkgroups, radio IDs, network/site fields and rest LSN when present.
- [ ] Unknown network/site must remain unknown. An adjacent-site announcement must not become the current site; a color code must not become a network ID.
- [ ] Monitor a suspected LTR repeater through several idle bursts and voice. Look for repeated CRC-valid area/home/group observations. Record/replay a sample if none appear.
- [ ] Compare live and replay; replay rows stay separate. Auto-save should require three matching confirmed updates and preserve saved names/notes.
- [ ] Switch Spectrum/Data/DMR-LTR during busy reception. Repeated rows should update counts without unlimited growth or UI stalls. Check the inspector's raw bytes and JSON.
- [ ] Tune away/back, stop/restart and seek replay. Partial messages should not be assembled across changes.
- [ ] On the supplied 464.700 MHz paging channel, compare 6/8/12.5 kHz filters. Some offline POCSAG512 was recovered, but message text and addresses remain unconfirmed.

No automatic trunk following or digital voice is included. LTR is experimental until checked against local RF.

Stopping point: __________________ Date: __________________
Failures, receiver/rate/filter: _____________________________

## Carried-forward checks
Repeat unfinished items below on 0.24.0-preview2; older version labels identify their original milestone.

# 0.23.0-preview1 acceptance

Extract the complete ZIP into a new folder. Keep Receivers and Decoders beside MRS.exe. Confirm Receiver / display preview 0.23.0-preview1 in the header. Existing user settings and saved frequencies remain in the profile.

- [ ] Receiver > RF squelch: enable Auto squelch while scanning is stopped. Wait for calibration; verify the displayed floor/threshold and optional spectrum lines. Change margin, then verify the same setting on Scan. Moving the manual threshold should disable Auto. Turn RF squelch off to bypass muting, then back on.
- [ ] Move through capture centers with Step/Shift. Existing waterfall signals should shift with their RF frequency; newly exposed areas should be blank. Zoom/pan and change FFT detail; check historical positions. Stop/restart should clear history.
- [ ] Test higher zoom and Hot signals / gradient spectrum. Verify strong signals are easier to distinguish and the Hz/bin label remains accurate.
- [ ] Try expanded filters on NFM/AM/SSB/WFM. Confirm sample-rate choices remain independent. Check CPU and decoder drops, especially on HackRF.
- [ ] Select CW on a known carrier. Compare 500/1000 Hz filters and 500/700/1000 Hz pitch. Verify saved CW entries tune correctly. Morse text decoding is not included.
- [ ] Enable optional Auto mode; manually tune FM broadcast, aviation voice, weather and an amateur frequency. Verify sensible defaults, amateur mode preservation, and saved entries retaining their own mode/filter. Disable Auto to keep fully manual selection.
- [ ] Test 1.5/2/2.5/5 MHz steps within your receiver range. Collapse sidebar sections and check readable dark-mode labels, including while stopped and running.
- [ ] Frequency Manager: single-click P25 folder then Edit; it must edit P25, not a previous DMR selection. Double-click a frequency to tune. Test Ctrl-click, Shift-click, Shift+arrows, bulk delete/undo, and right-click Edit/Delete/Tune.
- [ ] Hide/show a folder's markers, then an individual entry. The list remains visible; the spectrum markers should follow the folder hierarchy.
- [ ] Repeat sustained DMR control-channel reception while switching Spectrum/Data/DMR Channels. Confirm grouped counts update without UI stalls or crashes.

Record stopping point: ____________________  Date: ____________________
Failures / settings / receiver / sample rate: ___________________________

## Carried-forward checks
The earlier checklist follows. Historical version labels describe the original test; repeat unfinished checks on 0.23.0-preview1. Prior stability reports do not mark individual tests complete.

# Next build acceptance checklist — includes all preview2 checks

Status on October 15: preview2 stayed running without a reported crash. Testing started, but the stopping point is unknown; all individual checks remain unconfirmed. Mark completed items and record your stopping point below.

Extract the entire ZIP into a new folder and run MRS.exe with Decoders and Receivers beside it. Confirm the title matches the next release package. Your saved settings/frequency lists remain in the existing profile.

- [ ] Enable POCSAG / FLEX and monitor or replay a paging source. Enable Group paging duplicates. Identical complete messages sent to different capcodes should share a stable row, with an increasing Count and all capcodes retained. Fragments and failed results remain separate.
- [ ] Select a paging row. Under Paging labels / ignore, label its capcodes; test the label in the text filter. Give several capcodes a common label if you want a searchable source group. Use the management window to rename labels.
- [ ] Ignore a capcode and verify other recipients' copies remain visible. Ignore an exact message to hide all matching copies on that protocol/frequency. Show ignored should reveal retained matches. Unignore or remove the rule in the management window and verify restoration.
- [ ] Close/reopen M.R.S. and verify labels/rules and paging grouping preference persist. Clear should clear observations, not the saved rules.
- [ ] Verify long Summary text wraps. Resize the column and data/details divider to suit your screen.
- [ ] Select a paging message and click Inspect selected. Confirm decoded fields, source/replay position, recipients, labels, text and JSON are readable. Paging must report that raw codewords are unavailable.
- [ ] Keep the inspector open during reception and verify its snapshot does not change. Export the snapshot and verify its captured message and grouped recipient/count fields.
- [ ] Inspect a P25 NID header and a CRC-validated TSBK. Selecting a mapped field should highlight the correct bit span. NID received bits and BCH-decoded fields must be distinguished; TSBK bytes must be labelled as decoded data. Try the Bytes tab and resize the inspector.

Grouping/ignore changes rebuild from the latest 500 individual observations; this is not a permanent history archive. Rules are scoped by protocol/frequency per receiver. User labels do not establish physical transmitter identity. Live reception quality and sustained hardware operation remain field tests.

The previous generated paging samples remain under Docs/Samples; see PAGING.md for baseline decoding checks and limitations.


## New: inspector comparison

- [ ] Inspect a P25 observation and choose Pin as reference. Inspect another observation in the same receiver and open Compare. Verify reference/current identity, changed fields and Only changed fields toggle.
- [ ] Compare two matching P25 frame types. Verify differing bit positions are shown; these are differences, not a bit-error-rate measurement. Comparing incompatible types must report bit comparison unavailable.
- [ ] Compare two paging messages. Verify text/decoder-field differences, with no fabricated raw-bit comparison.
- [ ] Verify the pinned reference remains frozen during reception and after closing/reopening the inspector. Clear reference removes it; Receiver A and B do not share references. A receiver restart clears it.

Last completed item: ____________________
Build tested: ____________________  Date: ____________________
Notes / failed items: ___________________________________________________


## Aviation milestone
- [ ] Confirm 0.21.0-preview3 is running. Repeat all carried-forward paging/inspector checks above; prior partial stability feedback does not mark them passed.
- [ ] Select RTL-SDR 2.4 MS/s, tune 1090 MHz, select ADS-B / Mode S and start aviation. Verify Data observations, stable aircraft rows and increasing counts where local traffic is received.
- [ ] Toggle the optional map. Only aircraft with fresh decoded positions should appear; drag the divider to resize it. Map points from replay are orange; live points are green. Coordinates/position age remain visible in the aircraft table.
- [ ] Test local ACARS and VDL2 channels separately. Verify registration/flight or available link identifiers and full messages in Data/inspector. No reception remains inconclusive, not a decoder pass/fail.
- [ ] Test 978 MHz UAT if local traffic is available. Verify aircraft/status and supported FIS-B details, with RS validation wording. No local UAT transmission remains an untested case.
- [ ] Check a too-low rate or target outside usable capture reports a clear explanation. Change frequency/rate or seek a recording; decoding should stop and request restart.
- [ ] Stop/restart reception, clear aircraft, export the aircraft snapshot and test simultaneous different modes on receivers A/B. Verify independent sources, no crashes and acceptable CPU/drop counters.
- [ ] Allow a target to go stale: position markers and displayed altitude/speed disappear after 60 seconds without those fields; aircraft rows expire after 15 minutes.


## Preview2 regression: HackRF ADS-B overload
- [ ] Repeat 1090 MHz with 19.2 MS/s and +750 kHz capture offset. Run for at least five minutes. Confirm decoded count increases with traffic and input-overrun restarts no longer recur. Record drops and CPU if they do.
- [ ] Compare 9.6 MS/s and 19.2 MS/s; verify the broad spectrum remains available while aviation decoding runs separately.
- [ ] Change tuning/rate and restart aviation as prompted. Verify normal stop/start, replay and other aviation modes remain usable.


## Preview3 receiver DC regression
- [ ] Replay MRS-IQ-A-20261017-212449.mrsiq; select ADS-B 1090 MHz and start aviation before playing the full recording. Expect aircraft A3F133 / DAL1207, increasing count and decoded positions. Exact live/replay timing may affect retained display counts.
- [ ] Repeat live HackRF reception at 9.6 MS/s +250 kHz and 19.2 MS/s +750 kHz. Check aircraft updates and overrun counter. Reception still depends on local traffic and antenna conditions.

## 0.22.0-preview2: basemap and DMR
- [ ] Confirm version 0.22.0-preview2. Prior live ADS-B success is acknowledged; unresolved checks above remain available for regression testing.
- [ ] With aircraft positions visible, enable Online street map. Verify streets/place labels load; wheel zoom and drag pan. Updates must not undo manual panning. Fit aircraft and Follow selected should work. Disable online mode and verify coordinate view remains useful.
- [ ] Test map while disconnected: reception continues and unavailable tiles are indicated.
- [ ] Enable DMR identifiers, select NFM and 16 kHz, and replay the supplied DMR recordings. The second control sample should show repeated CC 0 observations in both slots. No IDs is a valid result when supported full-link-control headers are absent.
- [ ] Monitor a candidate DMR channel live. Check stable row/count updates, Data inspector bytes and independent receiver operation.
- [ ] Optional: enable auto-save/update. After repeated matches, verify existing labels/notes remain and metadata is searchable. Replay must not auto-save. Close/reopen and check saved metadata.
- [ ] Run reception for at least five minutes and record CPU, drop counts and any stalls.

Supplied recordings are not bundled. The first proposed control sample remains unidentified when no DMR result is produced. Voice audio and trunk following are not part of this preview.


## Data-tab hang regression
- [ ] Receive the same busy control channel with DMR enabled. Switch repeatedly between Spectrum, Data and DMR Channels for five minutes. The UI must stay responsive.
- [ ] With Group DMR repeats enabled (default), repeated CC/slot/type observations should update Count in stable rows. Select a row and verify latest details and raw bytes.
- [ ] Disable Group DMR repeats briefly: the latest 500 individual observations should be available. Enable it again and check readability. Rebuilt groups count retained history only.
- [ ] Stop reception, change channels, restart and verify controls still respond. Record drops and CPU if problems recur.
