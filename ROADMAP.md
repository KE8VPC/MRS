# M.R.S. product roadmap

## Platform and equipment

Windows only, initially Windows 11 Home x64. No macOS, Linux, mobile or browser application target. Preserve separation between receiver sources, signal processing, decoders, station control and the operator interface so features can grow without tying the product solely to SDR hardware.

Initial station: two Nooelec NESDR Mini 2+ (RTL2832U/R820T2); Baofeng UV-5Rs with microphone/audio connections and internal VOX; Yaesu FT-8800 with an unverified data cable; Digirig Mobile USB audio and RTS PTT. PTT support is independent of CAT tuning support. Direct I/Q sampling is available for station testing below the normal tuner range; useful coverage depends on the actual dongle wiring and antenna. An upconverter or dedicated HF receiver remains an alternative.

## Milestones

### HackRF / PortaPack receive integration — 0.17.0-preview1

Implemented an isolated libhackrf USB receive adapter for HackRF One-compatible PortaPack hardware. The user's H4M / Mayhem v2.4 is the station acceptance target. Receiver-family selection, serial discovery/reconnect, mixed RTL/HackRF A/B ownership, 64-bit 1–6000 MHz tuning, 2.4/9.6/19.2/20 MS/s capture, LNA/VGA/RF amplifier controls and version-3 I/Q provenance are included. Existing audio/channel/data processing is shared; no new protocol decoders, TX, firmware changes, sweep mode or coherent merging are claimed. Native library discovery and synthetic tests do not establish live hardware compatibility. See HACKRF.md for exact scope, setup, bandwidth distinctions and pending station tests. WiNRADiO and HackRF Pro remain separate future adapters.

### Station equipment update — September 29, 2026

The user now has two RTL-SDR Blog V3 receivers available for testing, in addition to the previously listed station equipment. Include these in upcoming receiver acceptance tests: discovery/selection, distinguishing each physical unit, saved settings, tuning, direct sampling, recording and reconnect behavior. No V3-specific test pass is inferred from availability. Version 0.11.0 provides two independent receiver workspaces; simultaneous physical reception still requires station acceptance.

### Aviation messaging and aircraft surveillance — requested TODO

Scope clarification, September 30, 2026: MLAT is excluded at the user's request. Retain the ADS-B, Mode S, UAT, ACARS and VDL Mode 2 plans below; no MLAT network, position-feed integration or multilateration solver is planned.

Explicitly include ACARS, VDL Mode 2 / ACARS over AVLC, 1090 MHz ADS-B, Mode S and 978 MHz UAT as planned receive-decoder targets. Treat each waveform, framing layer and supported message family as a separately validated capability; verify technical specifications, input requirements and implementation dependencies before development. ACARS messaging, surveillance data and aircraft identity enrichment are separate functions.

For ACARS and VDL Mode 2, provide aircraft identifiers, message labels/types, decoded message text where available, raw packet inspection, validation results and searchable timestamped history. For ADS-B, Mode S and UAT, display decoded identifiers, position, altitude, velocity and other supported fields only when actually available. Evaluate UAT traffic and weather/product decoding separately. Do not infer position from every Mode S message or treat an aircraft address as a verified registration or flight number; any optional lookup enrichment must show its source separately.

Add an aircraft table and optional map with tracks, last-seen times, message counts, field freshness and source/decoder status. Preserve distinctions between live reception and replay. Plan suitable recording/replay, CSV/JSON export, Frequency Manager links and integration with the shared packet/event inspector. Map-provider, offline-map and optional identity-database requirements remain to be selected.

Support SDR I/Q and compatible recorded signals, plus conventional-radio audio/data inputs only for decoders whose verified input requirements permit them. Record required sample rates and bandwidth per decoder; existing RTL acquisition/DSP settings do not establish compatibility with these new waveforms. Simultaneous aviation channels depend on receiver resources and captured bandwidth. Validate protocol fixtures, malformed/partial messages, noisy recordings, stale-data handling and actual station reception before claiming support.

Approved roadmap scope only: these decoders and aircraft views are not implemented in 0.8.1. No executable rebuild is requested by this TODO.

### Railroad HOT / EOT telemetry decoding — requested TODO

Add receive-only decoding and inspection of head-of-train (HOT) and end-of-train (EOT / EOTD / FRED) equipment transmissions. Start with North American railroad telemetry on the 452.9375 / 457.9375 MHz pair; verify channel direction, modulation, framing, field definitions and equipment variants against authoritative technical material and representative recordings before implementing each decoder profile.

Provide a railroad telemetry panel with timestamps, frequency, device identifiers, message type/direction, raw bits/hex, packet validation and decoded fields. Show brake-pipe pressure and, where present and verified for that format, battery, motion, marker-light, link and other status fields. Preserve unknown fields without inventing meanings; distinguish failed or partial packets and stale observations. Correlate HOT/EOT observations only when decoded identifiers support the association; do not infer a locomotive number, train identity or location from a device ID alone.

Support live SDR I/Q, recorded I/Q and suitable discriminator/data audio after the input requirements are validated. Include per-device last-seen/history, searchable event logs, CSV/JSON export, Frequency Manager links and optional spectrum labels. Simultaneous monitoring of both channels depends on multiple receivers or adequate source bandwidth; scanning must report observation gaps. Validate using protocol fixtures and representative RF recordings, with documented support per equipment variant. This TODO adds decoding/display scope only; no train-control transmission or executable rebuild is included.

Research references checked September 14, 2026: [FCC railroad EOT frequency-pair and telemetry description](https://docs.fcc.gov/public/attachments/DA-10-2293A1.pdf); [FRA Wireless Digital Train Line report describing EOT telemetry](https://railroads.dot.gov/sites/fra.dot.gov/files/2023-02/WiDTL%20Phase3.pdf). These establish context, not a complete decoder specification. Implementation and live acceptance remain pending.

### U.S. band-plan and channel-allocation overlay — requested TODO

Add an optional spectrum/waterfall overlay with published channel-center markers and shaded allocation ranges. Initial service layers: FRS, GMRS, CB and NOAA Weather Radio; extend to MURS, amateur bands, marine, aviation, broadcast and public-safety/business allocations as authoritative datasets are verified. Distinguish fixed nationwide channel plans, broader service allocations and individual licensed transmitter assignments. Label channels/services on hover with frequency, channel designation, applicable bandwidth/mode information and source/date; do not infer a live signal, nearby transmitter or permission to transmit from an allocation marker.

Use FCC rules and allocation tables for FCC-regulated services, and NOAA/NTIA or other responsible official sources where appropriate. Verify exact frequencies, channel numbering, exceptions and geographic applicability during dataset implementation. Weather channel markers should be separate from optional location-specific transmitter records. Keep amateur voluntary band-plan recommendations distinct from regulatory allocations. Retain source URLs, verification dates and dataset versions; support bundled offline data with explicit versioned updates.

Provide a master overlay checkbox, per-service toggles, configurable colors/opacity and zoom-dependent label density. At wide spans show service ranges; reveal individual channel labels as space permits. Keep overlay markers aligned during zoom, pan, capture shifts and I/Q replay. Preserve distinguishable styles for published allocations, saved user frequencies and detected activity. Store overlays separately from the Frequency Manager; do not populate starter folders or automatically create saved entries. Offer explicit Save channel to Frequency Manager and supported click-to-tune behavior without disturbing drag/marker gestures or replay restrictions.

Queue for a future combined milestone. This entry records scope only; no frequency dataset or overlay is implemented and no executable rebuild is requested.

### Current milestone: 0.11.0 — independent receiver workspaces

Implemented Receiver A/B windows with independent acquisition, tuning, gain/rate/mode, listening, recording, packet pipelines and saved settings. A retains the existing profile; B has its own directory and frequency library. In-process ownership checks prevent conflicting use of the same RTL receiver, including reconnect. Hardware dual-dongle acceptance remains pending. See RECEIVER-WORKSPACES.md.

Packet timing now uses a 48-sample correlation hypothesis and gentler clock correction, retaining the second 32-sample hypothesis and failed-FCS inspection. Both supplied I/Q recordings recover three valid frames; existing decoder tests remain required. Local fixtures are not distributed with releases.

Research is consolidated in REFERENCE-RESEARCH.md. Explicit refinements include POCSAG interpretation overrides, ERMES/Mobitex research, FST4W/Echo, many-to-many scan lists, multiple VFOs per capture, rolling replay, radio calibration, versioned external modem interfaces and reversible system imports. These remain planned, not implemented by the A/B milestone.

Next: preview/validated Unitrunker MARCS-IP import, then P25 control-channel inspection and receiver-role assignment, followed by voice following and separately validated voice decoding. Shared frequency-library editing, more than two workspaces and physical-radio-specific profile management remain future work.

### Previous milestone: 0.10.0 — AX.25 / APRS receive preview

Implemented 1200-baud AFSK/HDLC and AX.25 FCS/address parsing, basic APRS uncompressed positions/status/messages/acknowledgments, and a Packets workspace with raw inspection, filtering and JSON export. Sources include SDR NFM, conventional-radio Windows audio, WAV and compatible M.R.S. I/Q replay. Listening volume and application squelch do not gate decoding. See PACKETS.md for exact limits and VALIDATION.md for synthetic results; live station acceptance is pending.

Receiver labels now expose index and serial, with identity details and synthetic checks for two uniquely identified V3 units across enumeration changes. Version 0.10.0 retained one-source operation; 0.11.0 adds independent A/B receiver workspaces and their saved settings. Full station profiles, connected packet sessions, packet TX, 9600 baud, APRS mapping/IGate and richer APRS message families remain planned.

The A/B workspace milestone is implemented in 0.11.0, pending physical two-V3 acceptance. Subsequent work includes data imports and digital control-channel decoding; broader decoder families remain in the all-in-one roadmap below.

### Previous milestone: 0.9.0 — WFM broadcast

Implemented stereo WFM, pilot detection/lock and mono fallback, Force mono, two-channel SDR listening/WAV recording, MPX/audio spectrum and separate L/R waveform views, left/right meters, and a broadcast-information strip. The initial experimental RDS/RBDS decoder supports PI, PTY naming region, confirmed PS and 2A/2B RadioText with strict block validation and retune/seek/source-loss resets. See BROADCAST.md for supported fields and limitations. HD Radio, full RDS character mapping and advanced RDS services remain planned. The user reported "tests passed" on September 21, 2026 for the 0.9.0 preview. Individual hardware paths, stations and test duration were not specified; see VALIDATION.md for the user report and local test results.

Earlier WFM TODO descriptions below are retained as historical requirements. The implemented subset is described here and in BROADCAST.md; additional protocol families, dual receivers, overlays and radio control remain future milestones. No change to aviation or railroad scope.

### Previous milestone: 0.8.1 — Direct sampling

Implemented quadrature/direct-I/direct-Q selection, mode-specific frequency validation including selectable 1 Hz, native branch readback, disabled tuner gain in direct modes, unique receiver branch preferences, reconnect branch retention, I/Q recording/replay branch metadata and nonnegative RF display clipping. Branch or receiver changes stop reception and finalize recordings. All 199 synthetic checks and expanded UI smoke checks passed. Native USB operation, best branch, HF sensitivity and useful lower-frequency limit remain pending station tests.

### Previous milestone: 0.8.0 — Listening and spectrum

Completed for the Windows x64 test package: AM/USB/LSB plus adjustable mode-specific filters and AM/SSB audio AGC; software channel tuning within a stationary capture; spectrum filter shading, drag/navigation/fine steps and explicit capture shifts; automatic gain/sample-rate selection with frame acknowledgements; Recording tab with WAV/IQ controls and indexed I/Q Play/Pause/Stop/seek; monitoring enabled by default on a selected available endpoint and output identity restoration; saved filter widths; usable-bandwidth display cropping; version 3 per-frame channel/filter/AGC/gain metadata with v1/v2 compatibility.

Release compilation/publish, 177 synthetic checks and expanded offscreen WPF smoke checks passed. The UI workflow exercised live gain/rate/mode/filter changes during recording, retained worker identity, paused seeking, restart after Stop and controls after EOF. Source, recordings and packaged runtime use 64-bit frequency metadata where generic; the RTL native API remains model-specific. Additional hardware adapters and wider sample conversion/DSP are not included. Live USB, listening output, AM/SSB speech quality and sustained performance acceptance remain pending.

The development checkpoints and earlier requirement descriptions below are historical context; items fulfilled here no longer block 0.8.0. The next feature milestone can address WFM stereo/MPX/RDS and remaining receiver refinements after station feedback. HD decoding, digital decoders, external receiver adapters, clock synchronization, CAT/PTT and imports remain separately planned.

### AM / USB / LSB — development checkpoint, not packaged

Added AM envelope detection and complex-filter USB/LSB selection alongside the existing FM processor. Mode selector, preferences, saved-frequency recall, reconnect plans and version 2 recording/replay retain the selected mode. Legacy recordings without a Mode field use their original WideFm flag. New modes output 48 kHz mono PCM through existing listening, RF squelch and WAV recording paths. Bounded audio AGC is included for AM/SSB; RF squelch measures filtered complex-channel power independently of audio gain.

Initial filters are fixed: AM nominal -5 to +5 kHz, USB +300 to +3000 Hz, LSB -3000 to -300 Hz relative to tuned center. The spectrum/waterfall displays a shaded nominal passband. Added 10 Hz and 100 Hz tuning steps. Filters have transition bands; shading is not a brick-wall response guarantee. Fine steps currently retune hardware, and changing receive mode still restarts the source/finalizes recordings. Adjustable bandwidth, AGC controls, software channel tuning within stationary I/Q, further interference tests and live RF/performance acceptance remain outstanding.

Release compilation had zero warnings/errors; all 155 synthetic checks and the WPF smoke workflow passed. Added 27 checks for tone recovery at all current rates, block continuity, opposite-sideband rejection and mode-aware recording playback. RF overlay rendering was inspected. Existing published 0.7.0 packages remain unchanged; this does not claim station acceptance or a complete listening-mode release milestone.

### Capture metadata foundation — development checkpoint, not packaged

Added an adapter capability definition supplying the RTL frequency range, sample format and sample-rate menu. Added a receiver-independent capture descriptor with 64-bit center frequency, sample rate, separate usable bandwidth, explicit integer I/Q format and calculated raw throughput. Version 2 .mrsiq frames preserve these fields and original sample bytes; new UI recordings use version 2. Legacy version 1 files remain readable. Applied-frequency events and replay/display handoff now retain 64-bit frequency values.

Storage supports unsigned/signed 8-bit and signed little-endian 16/24/32-bit components. Current FM processing remains unsigned 8-bit at the three validated RTL rates; unsupported playback formats/rates are rejected explicitly. The plot still uses sample-rate span, and RTL native tuning/IPC remains adapter-specific 32-bit. General DSP conversion, useful-bandwidth display/filter handling, overview metadata, future source workers and hardware capability discovery remain outstanding. No HackRF/WiNRADiO adapter is claimed.

Validation: zero-warning/error Release compilation, 128 synthetic checks and offscreen WPF smoke workflow passed. New checks cover >4 GHz storage/replay, sample representation/usable-bandwidth round trip, throughput, invalid metadata/alignment and unsupported-DSP handling; legacy compatibility checks still pass. No new release package or live RF acceptance.

### Spectrum navigation — development checkpoint, not packaged

Implemented the first navigation slice: frequency entry beside the spectrum with Enter-to-tune, selectable tuning steps with left/right buttons, Full span, Center view, Zoom in/out and visible/full span readouts. Receiver frequency entry also applies on Enter through the frequency-only live tuning path. Dragging pans spectrum and waterfall together within captured coverage; movement above the drag threshold suppresses click-to-tune. Full span remains bounded by the active capture. Existing replay tuning guards remain in place.

Release compilation completed with zero warnings/errors; 118 synthetic checks passed and the existing offscreen WPF receiver/recording smoke workflow passed. RF navigation layout was visually inspected. No new distributable was produced and no live hardware acceptance is claimed. Full 64-bit worker/file-format migration, capability discovery, wider I/Q formats, automatic gain/rate changes, recording-tab/playback work and monitoring defaults remain outstanding for the combined milestone.

### HackRF and WiNRADiO research — recorded September 14, 2026

See [receiver research and official sources](RECEIVER-RESEARCH-HACKRF-WINRADIO.md). Plan native libhackrf and model-specific WiNRADiO SDK source adapters, with capabilities discovered per model, firmware, RF input, connection and installed options. HackRF One and Pro have different precision/rate options; WiNRADiO overview width can exceed continuous DDC capture bandwidth, and USB/Ethernet limits can differ. Do not promise vendor-software features solely because a receiver supports them.

Extend the wideband foundation with 64-bit Hz frequency storage throughout workers/IPC/recording, explicit signed/unsigned and wider I/Q formats, independently reported overview/capture/sample-rate values, and model-specific gain stages. Show a movable capture window and demodulator markers on a broader overview. Scale bounded processing and recording for tens of MHz without imposing another fixed application-wide ceiling. Preserve existing recording compatibility through format versioning.

Verify native DLL architecture, callback rules and redistribution terms before packaging. Resolve the G39 documentation discrepancy about individual DDC widths through its API/hardware. Keep UTC synchronization separate from external frequency reference, 1 PPS and coherent sampling. Receive-only adapter milestones precede separately scoped TX support. This is researched planning, not implemented hardware support; no rebuild requested.

### Receiver-dependent wideband display — architecture requirement

The current NESDR acquisition choices and 1.92 MHz maximum exposed span are adapter-specific implementation limits, not application-wide limits. Plan the source interface around reported capabilities: supported sample rates, usable instantaneous bandwidth, frequency coverage, sample format and processing constraints. Build acquisition and view-bandwidth choices dynamically from the active source, allowing future receivers with substantially wider coverage to display their full usable captured span.

Remove assumptions tying DSP, spectrum axes, navigation, recording/replay validation or UI choices to the RTL adapter's fixed rate list. Carry actual center frequency, applied sample rate and usable bandwidth with source/frame metadata; these are distinct values and must not be assumed identical for every receiver. Version recording formats as capabilities expand, retaining compatibility with existing files. Replay should derive coverage from the recording rather than attached hardware.

Scale FFT sizing, frequency resolution, rendering and recording throughput for wider inputs; use bounded buffers and explicit performance/unsupported-rate reporting. Keep full-span display and narrower channel selection available independently. Respect hardware bandwidth limits and distinguish simultaneous capture from any future swept/stitched panorama. This is a forward compatibility requirement, not a claim that additional receiver adapters or wider rates are implemented now.

### Spectrum frequency navigation and wider views — requested TODO

Add a compact frequency-navigation bar near the spectrum with an editable frequency/Enter-to-tune control, step left/right buttons, selectable tuning step, visible center/span readouts and Center on tuned frequency / Full span actions. Synchronize it with the Receiver frequency field and confirmed tuning state.

Allow direct mouse dragging to move the spectrum and waterfall horizontally together, supplementing the existing Shift+wheel pan. Distinguish a drag from a click so releasing after a drag cannot accidentally trigger click-to-tune. Preserve frequency ticks, cursor mapping and saved-marker alignment. Panning within captured coverage changes the view only; provide explicit band-shift controls to retune the capture center when navigating beyond it. Indicate capture limits rather than displaying uncaptured regions as received data. During I/Q replay, navigation must respect the recorded coverage and must not command live hardware.

Expose wider view-bandwidth choices next to the plot, including the current full 1.92 MHz capture at 1.92 MS/s. Evaluate additional receiver-supported acquisition rates for wider simultaneous coverage, with corresponding DSP, worker, file-format and throughput validation. Do not imply that display zoom alone increases captured bandwidth, and do not silently change acquisition rate merely to enlarge the view. Wider hardware support and panoramic scanning/stitching remain distinct options; a swept display must be labeled as sequential rather than simultaneous reception. Queue for a combined future milestone; no immediate rebuild.

### WFM MPX, stereo and audio views — approved planned feature

Add RF / MPX / Audio view selectors for WFM broadcast reception, complementing the compact RDS/HD station-information strip above the plot. MPX is the composite FM-demodulated signal before listening-audio filtering; label the L+R audio region, 19 kHz stereo pilot, L-R region around 38 kHz and RDS/RBDS around 57 kHz. Provide decoded audio spectrum/waveform, separate left/right level meters and clipping indicators. Show pilot detection, stereo-decoder lock and actual mono/stereo output separately, with a Force mono option. Do not infer valid stereo decoding merely from pilot energy.

Implementation requires a separate composite-signal path before the current 15 kHz mono audio filter, pilot recovery, stereo decoding and a stereo-capable listening/recording path. Keep RF display, MPX analysis and listening controls independent where appropriate. SDR I/Q and compatible recorded I/Q can supply the source; ordinary radio speaker/audio inputs must not be presented as full MPX sources. Conventional-radio MPX support requires an appropriate wideband discriminator/composite output and capture interface. HD decoding remains a separate RF/IQ decoder dependency, not an MPX component. Validate with known synthetic composite signals and live broadcast reception. Approved for the roadmap, not yet implemented; no immediate rebuild.

### WFM broadcast information above spectrum — requested TODO

When WFM is selected for FM broadcast reception, show a broadcast-information strip above the spectrum. Display decoded RDS/RBDS station name, RadioText, PI/program identification and program type when available, plus decoded HD Radio station/service information, HD subchannels and artist/title metadata when supported. Distinguish decoder unavailable, searching and valid decoded data; do not infer RDS or HD presence merely from WFM selection or saved frequency labels. Clear stale station metadata on retune/source changes and hide the strip outside applicable broadcast WFM reception. This depends on implementing RDS/RBDS and HD Radio/NRSC-5 decoding; HD reception also requires sufficient captured bandwidth and suitable I/Q input. Current mono-WFM audio output alone is not an HD decoder. Queue the UI and decoder dependencies in the roadmap; no immediate rebuild or claim of current decoding support.

### Recording sidebar tab — requested TODO

Add a Recording tab alongside Receiver, Frequencies and Display at the top of the left sidebar. Move both existing recording sections into it: WAV recording (Record WAV, Finish and status) and RF I/Q recording/replay (Record I/Q, Finish I/Q, Open I/Q recording and status). Preserve their state and source-dependent availability when switching tabs. Place the planned I/Q playback controls with the I/Q section and show them only while an I/Q file is loaded. Remove the moved sections from the Receiver tab to free space for receiver/listening controls. Queue for the next combined milestone; no immediate rebuild.

### I/Q playback controls — requested TODO

Show a dedicated playback control bar while an I/Q file is open, including Play/Pause, Stop, an elapsed/total-duration display and a seek/progress slider. Keep controls available after pause or end-of-file so the loaded recording can be resumed or restarted; hide them for live reception and ordinary audio sources. Seeking must use validated frame boundaries, restore the recorded frequency/rate/mode for the destination, clear stale audio and reset affected DSP/display state. Preserve recorded timing gaps during normal playback; pause must suspend the playback clock. Build a bounded frame index as needed, handle incomplete recordings explicitly and define how seeking affects any active WAV export so it cannot silently produce a misleading timeline. Queue for the next combined milestone; no immediate rebuild.

### Monitoring on by default and remembered output — requested TODO

Enable audio monitoring by default and remember the last listening output device. Restore the saved output using a stable Windows endpoint identity where available, rather than a changing device index; retain volume preferences. If the saved output is available and uniquely identified, enable monitoring automatically for the next source. Monitoring startup must not start reception or playback by itself. On first use, or if the remembered device is missing/ambiguous, show that an output must be selected and keep audio unrouted until selection; do not silently substitute another output. Allow monitoring to be switched off during use, with the next launch returning to the requested on-by-default behavior. Handle device-open failures visibly while keeping the app usable. This supersedes the earlier always-off-at-startup policy. Queue for the next combined milestone; no immediate rebuild.

### Apply sample rate on selection — requested TODO

Selecting an SDR sample rate should apply it immediately during live reception without requiring Start / Apply. Reconfigure the running receiver worker and processing pipeline, verify the actual applied rate, discard transition samples and reconcile RF span, view bandwidth, frequency mapping and DSP state before accepting new data. Preserve the receiver connection where the native API permits; report any necessary interruption clearly. Preserve active recording sessions where supported, with explicit sample-rate/timing transitions in I/Q metadata and 48 kHz demodulated WAV output. Do not apply unrelated pending settings. While stopped, retain the selected rate for the next Start. During I/Q replay, use the recorded rate and issue no hardware commands. Guard initialization/programmatic updates from triggering changes and handle rejection by restoring the last confirmed setting. Queue with Enter-to-tune and automatic gain selection for the next combined milestone; no immediate rebuild.

### Apply receiver gain on selection — requested TODO

Selecting Auto gain or a manual tuner-gain value should apply that gain immediately to the active SDR, without requiring Start / Apply. Extend the running worker command interface for live gain changes, acknowledge the applied automatic/manual state and actual supported gain, and update reconnect settings only after success. Preserve the receiver connection and active recordings; include gain changes in I/Q recording metadata using a versioned format update rather than leaving capture metadata inaccurate. Do not apply other pending receiver settings. While stopped, retain the selection for the next Start; during I/Q replay, do not issue hardware commands. Guard initialization/programmatic control updates against unintended gain changes. Queue for the next combined milestone; no immediate rebuild.

### Frequency entry shortcut — requested TODO

Pressing Enter while the receiver frequency field has focus should apply the entered frequency through the existing validated live-tuning path, without requiring a click on Start / Apply. Keep the same frequency range and input validation. During active reception, apply only the frequency; do not silently apply other pending device, mode, gain or sample-rate changes. If no live SDR source is running, show that reception must be started first; do not tune hardware from I/Q replay. Keep Start / Apply available for starting reception and applying other settings. Queue for the next combined milestone; no immediate rebuild.

### Current checkpoint: 0.7.0 Receiver and I/Q recording milestone

C# / WPF, Windows x64 self-contained package. Audio input/replay/recording and spectrum hover were tested successfully by the user in 0.1.x. Version 0.2.0 adds native RTL-SDR enumeration, isolated I/Q reception, tuning, gain, NFM/mono-WFM DSP and absolute RF spectrum/hover. Local synthetic checks pass, and the user reported “rf pass” with the NESDR setup: basic live RF reception is user-confirmed. Detailed mode, gain, reconnect and sustained-operation acceptance remains separate. No protocol decoder is implemented yet.

Version 0.3.0 adds the first Frequency Manager: local nested folders, metadata editing, search, drag/drop and ordering, recoverable session deletion, save-current/save-pointed frequency, RF annotations and supported-mode recall. The user reported the test passed. Version 0.3.1 removes automatically supplied starter folders: the user's earlier tree was an illustration of folder organization, not requested default content. New libraries start empty and folder organization is entirely user-defined.

### Receiver reliability and I/Q — implemented in 0.7.0

Usable unique manufacturer/product/serial identities restore receiver selection across index changes; blank, factory-default and duplicate identities stay manual. The worker verifies an expected identity after opening. Opt-in reconnect attempts only the same receiver, using 2/5/10-second backoff and three attempts per manual session. Stop/new source cancels pending recovery. Recordings finalize on loss and are not silently resumed.

Native .mrsiq recording preserves full-span 8-bit I/Q, per-frame center/rate/timestamps and capture metadata. A bounded writer queue reports overload/storage errors. Replay uses recorded timing and center/mode, drives the existing RF/audio display path without radio hardware, and reports truncated/incomplete tails after playing complete preceding frames. Live retunes can share one file. Initial limitations: no replay seeking/speed control, software channel selection, arbitrary raw/SigMF import/export, file rotation or USB-port identity fallback. Live unplug/reconnect and sustained disk throughput still require station acceptance.

112 self-tests and the recording/replay UI smoke check passed locally. The preceding 0.6.0 user pass remains recorded; 0.7.0 user acceptance is pending.

### Interactive receiver controls — implemented in 0.6.0

- **RF squelch:** lower/looser on the left and higher/tighter on the right, explicitly labeled and checked against gating behavior. A separate Enable RF squelch checkbox preserves the threshold while bypassing; settings migrate legacy minimum-means-OFF to an unchecked checkbox.
- **View bandwidth:** explicit visible span choices plus Full span, synchronized with zoom/pan. Choices stay inside captured coverage and the 1–32× zoom range. Channel-filter bandwidth remains separate future work.
- **Click-to-tune:** RF spectrum/waterfall left-click tunes using current view coordinates. Ctrl+click selects a nearby saved marker; right-click Save and wheel zoom/pan remain available. Audio-only views cannot tune hardware.
- **Live hardware retuning:** frequency-only clicks, direct entry and compatible saved-frequency recall send commands to the running worker, preserving its USB connection and an active recording. Frequency-tagged I/Q frames acknowledge the change; stale/transition samples are discarded and frequency-dependent history clears. Applied events enter the session log. WAV sample time omits the brief reception gaps. Moving a software channel within a stationary capture remains future work.
- **Sample rates:** 240 kS/s, 960 kS/s and 1.92 MS/s, accepted by the pinned RTL library. Higher rates use anti-alias filtering/decimation while audio remains 48 kHz. Requested/applied rates are checked; unsupported/mismatched settings fail explicitly. Changes to sample rate, mode, gain or device still restart acquisition and finalize recording. Rate preference is saved.

All 89 synthetic checks and the WPF interaction/render checks pass. The user reported "passed" for the delivered 0.6.0 milestone on September 14, 2026. Specific sample rates, hardware paths and sustained-test duration were not stated, so those individual results are not inferred. This is one combined milestone release.

### First SDR receiver

Basic 0.2.0 RF reception on the user's NESDR setup has passed the user's test. Native enumeration, streaming, tuning/gain, basic channel filtering, FM demodulation, fixed-size processing buffers, driver error messages and explicit range checking are implemented. Remaining work: detailed station acceptance, FM noise-based squelch refinement, USB-port identity for indistinguishable dongles, broader recovery diagnostics, I/Q seeking and SigMF import/export, additional validated sample rates and adjustable channel filters and calibration before expanding to simultaneous receivers.

### Remove Station roadmap panel — implemented in 0.5.0

Removed the lower-left "Station roadmap" group from the Receiver sidebar, reclaiming space for receiver controls. Project planning stays in this document.

### Receive squelch correction — RF power implementation in 0.5.0

SDR listening now uses relative power measured from I/Q after the receive-channel filter. Version 0.5.0 introduced an adjustable threshold with OFF at minimum (replaced by the separate checkbox in 0.6.0), live channel level, open/closed state, 3 dB hysteresis and 200 ms hang. Listening volume stays independent; analysis and recording retain ungated samples. The old audio-amplitude gate is bypassed for SDR. All 62 local synthetic checks pass; live RF acceptance is pending.

Conventional radio audio inputs do not inherently provide RF strength. Where a radio supplies a usable signal-strength or squelch-status interface, evaluate that input; otherwise expose an appropriate audio-derived noise gate or the radio's own squelch without labeling it RF strength. RF levels remain relative until calibrated. Validate silence during a received carrier, weak signals, idle-channel FM noise and threshold behavior on live RF. The conventional audio gate remains clearly labeled; radio-feedback integration and an FM noise discriminator remain future work. Channel-power squelch is sensitive to tuner gain, interference and DC offset; live station validation is still required.

### Usable receive alpha

Two independently configured receivers, persistent station profiles, AX.25/APRS 1200-baud receive decoding, recording management and hardware acceptance on the user's station. Demonstrate stop/start, unplug/reconnect, restoration of profiles, weak/noisy input and sustained recording.

### Initial transmit alpha

Dedicated TX routing; Digirig RTS PTT and radio-VOX choices; selected packet transmit modes; FT-8800 cable/levels validation; lead/tail timing, timeout and release handling. An explicit operator action enables transmission. Prevent incidental OS/listening audio from entering the TX path. Do not infer CAT support from an available COM port.

### Expansion

Native multimode decoding, station control, trunking/system exploration, imports and advanced analysis added in independently verifiable releases. Earlier estimates of weeks apply to a narrow alpha, not the full scope below. Re-estimate after the first hardware test.

### Spectrum hover readout — implemented in 0.1.1

Version 0.1.1 shows audio frequency in Hz under the mouse over the spectrum or waterfall; the user confirmed that it works. Version 0.2.0 adds absolute RF frequency in the SDR view. Mapping follows window resizing; hovering does not retune anything. Version 0.4.0 preserves this mapping through zoom and pan.

## Agreed long-term feature inventory

### Spectrum and waterfall display controls — implemented in 0.4.0

Implemented display gain, automatic/manual levels, pointer-anchored zoom, pan, timestamped waterfall history, speed, refresh limit, averaging and peak hold. Dark, Light, High contrast and Amber plot themes, four waterfall palettes, custom colors and named presets are included. Local software checks passed; user acceptance of this milestone is pending. See DISPLAY-CONTROLS.md. EXE/ZIP releases are batched at major milestones, following the user preference.

- **Display gain and level range:** spectrum vertical scale/reference level and waterfall brightness/contrast with adjustable low/high level limits. Include automatic/manual level scaling and reset-to-default controls. Keep these display adjustments distinct from the receiver's RF/tuner gain; they must not alter recorded samples or decoder input.
- **Zoom and navigation:** frequency-axis zoom for spectrum and waterfall, centered on the pointer or tuned frequency, plus horizontal pan and reset-to-full-span. Keep frequency ticks, hover readout and saved-frequency markers aligned with the displayed span. Zooming the view does not silently retune the receiver or change channel bandwidth. Digital zoom is limited to the captured span and does not itself increase spectral resolution.
- **Speed and smoothing:** adjustable waterfall scroll speed, with a time scale in seconds; separate spectrum refresh-rate and averaging/peak-hold controls. Changing display speed must not change acquisition, audio playback, recording or decoder timing.
- **Persistence and usability:** save display preferences, provide keyboard-accessible sliders with numeric readouts alongside mouse shortcuts, and allow spectrum/waterfall settings to be linked where useful. Preserve correct frequency/time mapping for waterfall history after zooming or changing speed; clear history with an indication if a mapping cannot be retained.

### Frequency manager — first implementation in 0.3.0

The initial local library, nested-folder sidebar, descriptive metadata, RF labels/visibility and explicit recall passed the user's test. No starter folders or stations are supplied as of 0.3.1. See [FREQUENCY-MANAGER.md](FREQUENCY-MANAGER.md) for the implemented subset and remaining features such as import/export, bandwidth shading and structured/automatic protocol metadata.

### Other agreed capabilities

These are research/implementation targets, not promises of existing capability. Proprietary formats, codecs, protocol licenses and external service terms require individual evaluation.

- **Sources and control:** conventional receiver audio, data/discriminator outputs, TX audio/PTT, external TNCs/keyers, audio/IQ replay; RTL-SDR and later other SDRs including HackRF; Hamlib, RigCAT and compatible control adapters. No dependency on DSDPlus or Unitrunker as the product's engine: those applications are capability benchmarks.
- **Amateur modes:** fldigi/MultiPSK breadth; CW, RTTY, PSK families, Olivia, FT8/FT4, JS8, packet/APRS, Winlink-related transport support, VARA/VarAC interoperability subject to available interfaces, weak-signal families including WSPR/Q65/MSK144/JT modes/FST4, and FreeDV.
- **Digital voice and systems:** DMR/MOTOTRBO, P25 Phase I/II, NXDN/NEXEDGE/iDAS, D-Star, System Fusion, dPMR, ProVoice and additional researched systems. Control-channel and packet inspectors; IDs, talkgroups, channel grants, aliases, metadata and encryption indicators. Authorized-key workflows may be researched; no claim that recording encrypted signals makes unknown-key AES decryption practical.
- **Trunking:** system/site/channel/talkgroup database, receiver roles, control-channel search/chase, channel plans, priorities/holds/lockouts, activity history and persistent discovery. Include legacy Motorola, EDACS, MPT1327, LTR and later TETRA evaluation.
- **Signaling:** ANI as a functional category; DTMF, CTCSS (PL/TPL/QT aliases), DCS (DPL/DQT/DTCS aliases), MDC1200, FleetSync/I/II, GE-STAR, BIIS, two-tone paging, Quick Call, GE99, Plectron, CCIR/ZVEI and other multi-tone systems; clarify ambiguous CTC/DTC labels rather than inventing protocols. POCSAG 512/1200/2400, FLEX research, DSC, SELCAL, ATIS and SAME.
- **Broadcast and telemetry:** AM/FM/SSB, RDS/RBDS, FM HD Radio/NRSC-5, DRM/DAB research, aviation/maritime/weather/telemetry and satellite data where technically feasible.
- **Time:** operator-selectable NIST/network synchronization, received-signal averaging and PC-clock-only modes. Internal offset/confidence/holdover and source history. No silent source fallback or system-clock adjustment. A frequency reference and an absolute time reference are distinct.
- **Analysis:** passive known-signal classification, demodulation candidates, bit/packet views, validation metrics and exploratory unknown-stream analysis. Unknown classification is tentative and does not imply successful decoding or decryption.
- **RadioReference:** optional authorized API import with applicable developer access and user subscription; previews, system/site/talkgroup mappings, source IDs, protected local edits, history, CSV and rollback. Confirm current terms before implementation.
- **Operator workspace:** customizable panels, receiver mixer, separate listening/recording/decoder paths, per-receiver volume/squelch/mute, code/digital gating, monitor/hang time, keyboard/accessibility support and Basic/Advanced/Engineering views.
- **Records and reliability:** replay/pre-event buffers, SigMF/provenance, searchable recordings/events, retention and privacy options, disk/resource monitoring, decoder isolation, backups/recovery, safe imports and diagnostic exports.
- **Station operations:** net rosters, message state tracking, station/accessory routing, transverters, satellite passes/orbital updates/Doppler/rotators, remote permissions separated into RX/tune/TX, receive-only operation, TX audit and protection for nearby receivers.

### fldigi reference refinements — September 29, 2026

Local profile inspection adds explicit Contestia, DominoEX, Hell, MFSK, MT63, THOR, THROB/ThrobX, FSQ, IFKP, NAVTEX/SITOR-B, WEFAX and separately validated PSK/OFDM variants to the amateur/data inventory. Add RSID notification and controlled mode selection; a multi-signal text browser; detailed CW/RTTY parameters; RX transcripts, TX composition and reviewed macros; QSO/contest/ADIF workflows; NBEMS/FLMSG/FLAMP interoperability research; independent audio-clock calibration; and WWV/FMT/frequency-analysis views. Preserve RF dial frequency, sideband and audio offsets separately in imports. WEFAX slant/alignment and NAVTEX/SYNOP structured records need their own acceptance tests.

These remain planned. This inspection does not change the next implementation order or establish installed companion software, native mode compatibility, calibrated measurements or absolute UTC synchronization. See [Reference research](REFERENCE-RESEARCH.md) for local evidence and import boundaries. No executable rebuild accompanies this documentation update.

## Acceptance boundary

Local builds and synthetic tests establish software behavior for those tested paths only. Hardware drivers, actual RF decoding, radio audio levels, PTT timing and station operation need field tests. Every release must distinguish working features, optional integrations, experimental capabilities and roadmap items.













## Next milestone progress — shared frequencies

Implemented in source: shared A/B frequency manager with same-process refresh, one-time preservation/import of legacy B entries, revision conflict protection and broadcast-panel text contrast correction. Pending milestone packaging and user acceptance. Combined spectrum remains a later optional feature; this change does not combine SDR captures.


## Spectrum band reference — implemented in source

Optional top-of-spectrum U.S. band/channel bar, zoom-aligned with sourced amateur band outlines, CB, FRS/GMRS, MURS, NOAA weather and FM broadcast. Includes per-receiver persistence and hover provenance. Selected coverage only; full FCC 2.106 table and additional layers remain planned. See BAND-PLAN.md for dates and scope. Awaiting next milestone package.


Allocation-layer refinement: preserve stacked services, primary/secondary status and independent Federal/non-Federal rights from the NTIA chart; retain named bands and Part 95 channel centers as separate layers. September 2025 poster uses March 31, 2025 FCC data. Full structured transcription and 2.106 footnote validation remain pending. See NTIA-ALLOCATION-REVIEW.md.


ARRL reference refinement: implemented an independently selectable operating-recommendations layer across 28 band groups from LF through microwave, preserving overlaps and local coordination notes. Compare actual allocation, privilege and recommendation changes independently; publication dates alone do not imply changed allocations. The ARRL 60-meter reference is included; direct current FCC privilege verification remains pending. See ARRL-BAND-PLAN-REVIEW.md.




Overlay placement implemented in source: above/below spectrum selector, saved per receiver, shared by U.S./ARRL reference rows. Frequency-axis spacing and waterfall mapping remain aligned. Pending milestone package.

## UI cleanup — session log

The original request was to remove Session log from the main receiver screen. After the user's renewed request on September 30, 2026, the viewer was moved to a separate Session log tab and its reserved spectrum row removed. Diagnostic logging remains available. This is completed in source after 0.15.0-preview1 and will ship with the next milestone; existing ZIPs are unchanged. Automatic log-file output remains unimplemented.
Service overlay expansion implemented in source after 0.12.0: AM broadcast, aviation navigation/communications and emergency markers, selected marine voice/DSC/duplex/AIS references. See SERVICE-OVERLAYS.md. Full allocation coverage remains unfinished; existing ZIPs unchanged.
3.2 MS/s added in development as an experimental RTL setting. A three-phase 3/40 rate converter preserves the existing 240 kHz DSP and 48 kHz audio clocks. Full-span display can show 3.2 MHz; physical USB reliability remains unverified. Pending next milestone package.
Station branding: KE8VPC added right-aligned in the lower-right footer of both receiver windows. Implemented in source; queued for next package.

### HD Radio — 0.13.0-preview1 recording milestone

Implemented offline MRSIQ conversion and NRSC-5 process integration, HD1–HD4 selection, diagnostic metadata, WAV export and cancellation. Known-sample native decoding and simulated 3.2 MS/s conversion recovered audio. Fresh local station acceptance, live HD audio, structured metadata/service discovery and redistribution packaging remain pending. See HD-RADIO.md.


### Shared Data inspector

Rename Packets to Data and retain a protocol-neutral source/validation history for future P25, DMR, NXDN, D-STAR, paging and other supported decoder events. Implemented now: AX.25/APRS plus changed WFM RDS/RBDS metadata snapshots, searchable protocol labels and shared JSON export. Failed AX.25 frame candidates do not establish P25 or any other protocol. HD diagnostics remain separate pending structured integration. No new P25 decoder is implied by this UI change.


### HD / Data preview — 0.13.0-preview2

Implemented internal 44.1 kHz stereo HD listening from paced I/Q recordings and the centered WFM receiver stream; station/title/artist/service metadata; Data-tab HD observations; stop-on-retune/source-stop/replay-seek/pause; bounded live input with explicit overflow failure; analog monitor suspension while HD plays. HD program selection is manual and requires stopping/restarting HD. MPX reference markers and shared Data tab are included. Native fixture PCM and metadata recovery, cancellation and synthetic checks validate software paths; speaker playback and live local HD reception remain unverified. Preview2 packaging includes the native backend, matching source archives and notices. The receiver replay/raw-I/Q event path also passed known-sample decoding.


### HD sample acceptance — September 30, 2026

User passed 0.13.0-preview2 with the provided HD recording. Internal sample playback is accepted; local over-the-air HD reception remains unverified because no local signal could be decoded. Next proposed decoder milestone: P25 Phase I control-channel data, beginning with validated framing and system/site information in Data, then talkgroup activity and channel grants. Voice decoding and trunk following remain separate subsequent work.


### P25 control-data preview — 0.14.0-preview1

Implemented receive-only Phase I C4FM synchronization, conservative BCH NAC/DUID validation, TSBK trellis/CRC decoding, standard network/site status and group voice grants in Data. Inputs: NFM SDR discriminator and I/Q replay. Separate checkbox, counters, source attribution, bounded history and JSON export. See P25.md. Synthetic protocol/IQ and UI validation are required; local RF acceptance remains pending.

Next: identifier-update channel/frequency resolution, expanded control messages and activity presentation. Receiver-role assignment, trunk following, CQPSK/LSM and P25 voice decoding are separate later milestones. Preserve the remaining all-in-one roadmap.

### CISA NIFOG interoperability overlay — implemented for next package

Added an independent NIFOG 2.02 (January 2025) RF channel-reference layer with 246 channel/plan entries and 236 RF centers. Covers low-band, VHF/UHF, federal IR/LE, medical, 700/800 MHz, air-ground, low-power, deployable trunk frequencies, weather and SAR references. Displays both mobile RX/TX sides, direct aliases, mode, tones/NACs, source pages and qualifications. Offline embedded data; per-receiver visibility; shared above/below placement; no local-activity inference or automatic radio programming. See NIFOG-OVERLAY.md. Existing release ZIPs are unchanged; distribution waits for the next milestone.

### CTCSS / DCS and NIFOG milestone — 0.15.0-preview1

Receive-side subaudible detection is implemented for 50 common CTCSS tones plus 62.5 Hz and 104 DCS codes in normal/inverted polarity. Equivalent cyclic DCS names are reported together. Data checkbox and saved per-receiver setting; live spectrum readout; acquisition/loss history, filtering and JSON export. Uses the unsquelched NFM discriminator or compatible low-frequency-preserving Windows audio/WAV. No tone-controlled listening gate or transmit signaling is added. See SUBAUDIBLE.md for conservative acquisition requirements and test scope.

This milestone also packages the previously completed CISA NIFOG overlay. Next P25 work remains identifier-update channel/frequency resolution and expanded control activity; tone squelch, other signaling families, voice decoding, trunk following and remaining all-in-one features stay on the roadmap. Live CTCSS/DCS repeater and conventional-radio audio acceptance remain pending.

### Meshtastic / LoRa receive decoding and U.S. frequency folder — requested TODO

Requested September 30, 2026. Plan a LoRa chirp-spread-spectrum receive decoder and a separate Meshtastic packet interpreter. Begin with the Meshtastic US region, then expand validated modem presets and custom configurations. Preserve raw packet bytes, PHY validation status, frequency, bandwidth, spreading factor, coding rate, timing and signal-quality measurements. Show verified Meshtastic headers and available node IDs, packet IDs, routing/hop fields, text, position and telemetry in Data with filtering, replay attribution and export. Report only fields supported by the decoded packet/version; encrypted or unrecognized payloads remain labeled accordingly. Support plaintext and compatible channel traffic with the documented public/default key or user-supplied keys; do not imply that arbitrary private encrypted traffic becomes readable. Node/position views and Frequency Manager links are planned follow-ons.

Use SDR I/Q and suitable I/Q recordings for RF decoding; ordinary NFM/speaker audio does not preserve the LoRa waveform. A compatible LoRa radio's serial/USB packet interface is a distinct possible input, not proof of SDR demodulator support. Keep LoRa PHY, Meshtastic and LoRaWAN separate: decoding one does not establish the others. Validate known independent packet fixtures, supported presets, timing/frequency offsets, damaged packets, recordings and real radio traffic before claiming support. No decoder or new EXE is delivered by this TODO.

Add a default **Meshtastic / LoRa (US)** folder to the shared Frequency list when implementing this feature. Interpret “all known US channels” here as all documented Meshtastic US-region RF slots across supported presets, organized by preset/bandwidth. Version the dataset against official firmware/configuration definitions; include exact integer-Hz centers, slot numbers, modem parameters and default-slot labels. The reviewed official documentation specifies US 902–928 MHz; LongFast has 104 slots and its usual default is slot 20 at 906.875 MHz. Slot counts and center frequencies depend on bandwidth, so 104 LongFast entries alone are not the entire catalog. Treat custom frequency overrides as explicit user entries rather than claiming an exhaustive list of every possible LoRa frequency. Logical encrypted channels are distinct from RF slots; do not treat their names as additional RF allocations. LoRaWAN US915 plans, if added later, must be separately named and sourced.

This explicitly requested folder is an exception to the earlier no-starter-folders preference; do not reinstate the user's old illustrative folders. Seed once for new and existing libraries using a migration/dataset ID, preserve existing folders/edits and avoid duplicates, including across Receiver A/B. Keep it collapsed initially, removable and restorable on request; deletion must not be undone on every launch. Use existing marker visibility and density handling so hundreds of reference entries do not obscure the spectrum. These are reference slots, not evidence of active nearby nodes.

References reviewed September 30, 2026: [official radio settings and slot calculator](https://meshtastic.org/docs/overview/radio-settings/), [LoRa configuration and RF-slot versus logical-channel settings](https://meshtastic.org/docs/configuration/radio/lora/), [encryption formats](https://meshtastic.org/docs/overview/encryption/). Recheck the authoritative firmware definitions at implementation because available presets and defaults can change.

### Short-range sensors and remote-control reception — requested planning addition

September 30, 2026: include weather stations, remote thermometers, vehicle TPMS, key fobs and garage-door remotes as receive-analysis targets. The user confirmed 433 MHz (often 433.92 MHz) and 315 MHz as the intended bands; the earlier 443 MHz mention was a typo. Preserve user-selectable frequencies for device-specific variants. Additional manufacturer-specific frequencies and 345/868/915 MHz variants can be added with verified device references; these are not a universal fixed-channel plan.

Build a reusable ASK/OOK and FSK burst/pulse analysis path with device-specific framing, checksum and field parsers. Target weather temperature/humidity/wind/rain/battery fields; TPMS sensor ID, pressure, temperature and status where present; and recognizable remote identifiers/button fields where the protocol exposes them. Preserve unknown fields and raw frames. Rolling/encrypted portions may be recognizable as structured traffic without their protected contents being decoded. An observed TPMS ID does not establish vehicle identity or wheel position.

Integrate validated observations into Data with protocol/model, input frequency, source/replay status, timestamp, available signal quality, validation scope and raw bits/hex. Plan a sensor list with aliases, last-seen times, deduplicated burst counts, measurement history/units and export. Unknown signals should expose pulse timing and raw captures without speculative device labels. Support explicit saving of discovered devices/frequencies to Frequency Manager; no additional automatic starter folder is requested here.

Use live SDR I/Q and recorded I/Q first. Compatible conventional-radio data/baseband paths require separate validation; ordinary speaker audio is not a universal substitute. Plan independent 315 MHz and 433 MHz monitoring with two receiver workspaces. A single narrow capture cannot cover both simultaneously, and scanning can miss brief transmissions. Packet coverage is per device/protocol/version, not a claim to decode all devices sharing a frequency. Validate independent fixtures, repeats, damaged frames, noise, unit conversions, unknown-burst handling and actual device recordings before a release.

Research reference: [rtl_433 official project and device catalog](https://github.com/merbanan/rtl_433), reviewed September 30, 2026. It documents RTL2832 support, pulse analysis and numerous weather, TPMS and remote protocols. Use as a capability/protocol reference; integration versus native M.R.S. decoders, licensing and distribution remain implementation decisions. This records receive-decoding scope only; no new executable or transmitter-control feature is included in this planning update.

### P25 Data summary readability — source addition after 0.15.0-preview1

User's local reception screenshot shows rapid repeated P25 control messages. Added default-on, per-receiver “Group P25 repeats”: stable site/network rows update in place; identical headers/grants/other messages are counted, while different raw activity remains distinct. Count, last-seen, first-seen and content-change details; separate keys for source sessions/frequency/NAC; optional individual-message view; bounded summaries/history; filtered export of the selected view. See P25.md. Existing release ZIPs remain unchanged until the next milestone.

### Spectrum/log and Data divider layout — source addition after 0.15.0-preview1

Completed the previously requested removal of Session log from the main Spectrum view: diagnostics are retained in a separate Session log tab, with the former reserved spectrum row removed. Added a visible horizontal GridSplitter between the Data list and selected-message details, supporting vertical drag and keyboard resizing. Default split favors the data list; both panels retain usable minimum heights. Split persistence across application restarts remains future work. Existing distribution ZIPs are unchanged; these layout corrections join the next milestone package.

Follow-up on the user's compact-window screenshot concern: shortened decoder labels and moved detailed decoder status/counters/help into an initially collapsed, height-bounded expander. This reduces header height rather than merely dividing the remaining space. Compact visual validation includes the four-row synthetic P25 summary. Still a source addition for the next milestone package.

### Packaged checkpoint - 0.15.0-preview2
P25 grouped rows, compact Data header, draggable Data divider, and separate Session log tab are now packaged in 0.15.0-preview2. 604 self-tests and extracted-package UI smoke checks passed. User acceptance is pending; divider position persistence remains planned.


### P25 category grouping correction - October 1, 2026 (source only)
Corrected raw-bit differences in NID headers and changing undecoded TSBK payloads previously produced many Count=1 rows. Group headers by decoded identity and unknown/protected/manufacturer TSBKs by kind and opcode/manufacturer description within the existing source/session/frequency/NAC scope. Latest payload and content-change count remain visible; recent individual raw observations remain available with grouping off. Distinct grants remain separate. Existing preview2 ZIP is unchanged.


### P25 message visibility - October 1, 2026 (source only)
Added compact message-type menu, individual category checkboxes, grants/status preset, show-all action, hidden-count indication, and per-receiver preference persistence. Hiding preserves bounded history; filters apply to both views and exports. Not yet packaged.


### 0.15.0-preview3 packaging checkpoint
P25 category grouping correction and per-receiver message-type visibility are included in preview3. Earlier source-only notes above describe development history. Field acceptance remains pending.


### October 3: preview3 accepted; P25 channel-frequency source milestone
User accepted 0.15.0-preview3. Implemented identifier updates 3D/34/33, receiver-local channel tables, resolved frequencies and TDMA slot metadata on existing grants/network/site messages, structured JSON fields, identifier summary rows and visibility filtering. 647 self-tests and full UI I/Q replay passed with independently generated protocol fixtures. Live RF channel-frequency acceptance remains pending. Packaged preview3 remains unchanged. Next P25 work: more grant/control formats and persistent system/site/channel activity; voice and automatic following remain later.



### October 3: expanded P25 control messages (source)
Implemented standard dual group-grant update 02, explicit group-grant update 03, secondary control-channel announcement 39, and adjacent-site announcement 3C. Added structured assignment/site exports, explicit uplink handling, per-neighbor and per-secondary-set summary keys, individual visibility categories, and expanded grants/status preset. 671 self-tests and full I/Q/UI smoke passed using independent protocol vectors. Live RF acceptance remains pending. Next: persistent system/site/channel activity and more control formats; automatic following/voice remain later. Desktop preview3 unchanged.



### October 3: persistent P25 Systems catalog (source)
Added a separate searchable P25 Systems tab with receiver-local live discovery persistence and separate unsaved replay discoveries. Stores band plans, control/voice-channel assignments and site announcements with identity/source provenance, first/last seen and counts. Provisional identities are not guessed into known systems. Versioned JSON export, atomic autosave, read-error preservation and a visible 5,000-entry limit are included. 686 self-tests and full UI smoke passed, including explicit checks against collapsed discovery columns. Live RF acceptance remains pending. See P25-SYSTEMS.md. Desktop preview3 unchanged.



### P25 operator annotations and frequency review (source)
Added per-discovery names/notes preserved across incoming observations and live catalog reopen. Added explicit resolved-live-channel review in the frequency editor with source/identity provenance, selected folder, fresh entry identity and existing duplicate handling. Replay, unresolved and band-base entries are excluded. Target neighbor metadata is distinct from receiving NAC/site. 698 self-tests and full UI smoke passed; field acceptance remains pending. Desktop preview3 unchanged.



### October 4: 0.16.0-preview1 packaging checkpoint
Packages identifier-table frequency resolution, expanded P25 control messages, persistent live/replay-separated discoveries, annotations and explicit frequency-library review. Earlier source-only notes describe development history. Live field acceptance of these additions remains pending.


### October 7: expanded receive-filter bandwidth options (planned)
User requested more and wider receive-filter bandwidth choices as a future update. Provide mode-appropriate ranges and consider a validated custom-width entry. Respect the active receiver's capture/analog coverage and the demodulator or decoder input-rate limits; increasing FFT detail or hardware sample rate alone does not expand the current channel-processing bandwidth. Preserve capture-offset fit checks, recording/replay metadata, and independent receiver settings. This is planned, not implemented in 0.17.0-preview3.

### October 7: CW receive mode (planned)
Add CW to the receive-mode selector alongside AM, NFM, WFM, USB and LSB. Include CW-appropriate narrow receive filters and an adjustable listening pitch (sidetone/BFO offset). Define the displayed frequency as the received carrier frequency and account for the listening pitch separately from the HackRF capture offset. Preserve mode/filter settings in frequency entries and I/Q replay metadata, with explicit compatibility handling for older recordings/builds. CW audio reception is distinct from automatic Morse text decoding and CW transmission/keying, which remain separate roadmap capabilities. Future work only; not included in 0.17.0-preview3.

### October 7: explicit CW / Morse decoder (planned)
User reaffirmed automatic CW-to-text decoding as future work. Decode received Morse into readable text in the Data tab, with estimated sending speed (WPM), adjustable/automatic tone tracking and timing, and clear handling of uncertain characters. Support SDR-demodulated CW and suitable conventional-radio audio input. Treat weak signals, interference, fading and irregular hand-sent timing as acceptance cases; do not imply error-free decoding. This complements the planned CW receive mode and does not add CW transmission/keying. Not implemented in 0.17.0-preview3.

### October 8: 0.18.0-preview1 ATSC 1.0 HDTV
Added the HDTV tab, user-local pinned decoder setup, physical RF channel tuning, full-channel offset-aware GNU Radio ATSC 1.0 decoding from live raw I/Q or recorded I/Q, transport-stream recording, program inspection and separate-window playback. Generated 9.6/20 MS/s TV recordings recovered video/audio with a 1 MHz offset; Windows installer, streaming, cancellation and synthetic receiver/UDP paths passed. 854 automated checks and full WPF smoke passed. Live station and sustained real-time acceptance remain pending. ATSC 3.0, integrated video, PSIP guide and EPG remain future work. See HDTV.md.

### October 8: 0.18.0-preview2 HDTV setup fix
Corrected explicit package installation order after user-observed GDK/GLib missing-DLL dialogs. Preserves all 108 dependency versions/hashes. Expanded health checks cover GLib, GDK and playback executables, with structured errors instead of child loader dialogs. Fresh installation, both missing-DLL fault cases, generated TV streaming and 855 self-tests/UI checks passed. Live RF remains unverified.

### October 11: VDL2 decoder explicitly requested (planned)
User requested VDL2 (VHF Data Link Mode 2). Reaffirm the existing aviation decoder target as an explicit receive-only protocol item alongside ACARS and ADS-B. Integrate validated decoded messages, aircraft identifiers when present, raw AVLC frames, timestamps, filtering and export into the Data tab. Support live SDR I/Q and compatible recordings; verify input requirements and supported message families before implementation. This entry records scope only: VDL2 decoding is not yet implemented or included in the current build.

### October 11: locked-spectrum activity scanner and automatic discovery (planned)
User requested: keep the spectrum stationary, estimate the noise floor, set RF squelch, detect a frequency exceeding squelch, jump there to monitor, and add it to Frequency Manager.

- Add an explicit Start/Stop activity-scanner control per receiver. While enabled, keep the hardware capture center and spectrum viewport fixed; change only the listening frequency within the usable captured span. Respect channel-filter fit at the edges. A display zoom or sweep does not create additional simultaneous RF coverage.
- Estimate a robust noise floor from recent RF spectra, excluding occupied regions, invalid edge bins and known capture-center artifacts. Allow local floor estimates where the baseline slopes. Add an adjustable dB-above-floor margin, manual override and optional visible noise-floor/trigger lines. Slowly update the background estimate without chasing active transmissions; recalibrate after gain, sample-rate or capture-center changes.
- Detect sustained above-threshold channel activity with minimum duration, adjacent-bin grouping, bandwidth-aware power measurements and hysteresis to reject brief spikes and avoid repeated tuning. Tune to an estimated signal/channel center, not the tallest individual FFT bin. Use configured mode/filter or a separately validated channel-plan rule; energy detection does not identify a protocol.
- Monitor one selected signal at a time, hold while active, then resume discovery after an adjustable quiet delay. Provide pause/hold, skip/ignore and a documented strongest-first or round-robin policy when several signals are active. Keep detection across the capture running while listening to one channel; do not imply simultaneous decoding of all signals.
- Automatically upsert qualified discoveries into a scanner-owned Frequency Manager folder, shown in the sidebar and as spectrum markers. Store frequency, selected receive mode/filter, estimated occupied bandwidth, first/last seen, hit count and signal-above-floor strength. Mark protocol/modulation as unknown unless actually identified; do not confuse selected demodulation with detected modulation. Deduplicate with channel-aware tolerances; update repeat hits and preserve existing user names, notes and folder organization. Create the folder when the feature is first used, not as a starter-folder preset.
- Stop scanning on incompatible source changes, or explicitly re-arm after recalibration. Preserve manual tuning/stop authority. Validate with synthetic multi-signal/noise fixtures, edge cases, persistent discoveries and live RF before claiming acceptance.

Scope is planned; this entry does not implement the scanner or rebuild the application.
