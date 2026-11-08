# Current release: 0.47.0-preview1 — saved discovery management

Includes informative generic spectrum labels, scope-aware new discovery organization, reviewable cleanup and optional selected-grid snapping with raw-frequency retention. Added versioned bundle/JSON and channel/catalog CSV exchange, legacy-list import, selected-folder export, system/site/TG/radio/map catalog persistence, backups, Undo and conflict-preserving import. This is the data-exchange foundation for a future sharing website; there is no upload/site implementation. Operational saved lists are not rewritten automatically. Details: DISCOVERY-EXCHANGE.md and the 0.47 update/validation/acceptance documents.

# Current release: 0.46.0-preview1

User authorized the combined build November 4, 2026. This release includes the pending dedicated P25 history/audio path, mapped DMR identity/metadata, receiver-pool lifecycle and mixed-coverage fixes, primary Fox Hunt with trend/rough-area modeling, GPX metadata and shared local offline detail. See UPDATE-0.46.0-preview1.md, VALIDATION-0.46.0-preview1.md and ACCEPTANCE-0.46.0-preview1.md. Pending-source entries below are development history now included in this release; their no-packaging statements describe those earlier passes. Live field acceptance remains open as listed in the current checklist.

## Mixed receiver coverage source follow-up - November 4, 2026

Reproduced an untunable first grant blocking later supported calls in the pool dispatcher. Dispatch now skips the unavailable target and continues evaluating later grants without claiming a tune or voice decode for the skipped call. The new five-receiver assertion failed before the change and passed afterward along with the existing pool/lifecycle checks. Compilation and scoped whitespace checks passed. Evidence: VALIDATION-PENDING-FLEET-LIFECYCLE.md. No release ZIP or live hardware test.

## Receiver-pool lifecycle source follow-up - November 4, 2026

Reproduced maximum-call expiry immediately redispatching the same retired cached grant because dispatch used an earlier snapshot. Available-call selection now happens after expired/excluded leases retire. Control-side Allow/Ignore/Hold changes also release already assigned excluded voice calls, retain eligible simultaneous calls, and leave the released receiver eligible for fresh qualifying grants.

Full local suite passed 1,927 assertions; three DMR mapped-profile scenarios, selective-filter changes with two P25 voice receivers, and six P25 voice/log regression cases passed. Evidence and pre-fix reproduction are recorded in VALIDATION-PENDING-FLEET-LIFECYCLE.md. These are synthetic/local checks; installed/live following acceptance remains open. No release ZIP, device operation, installation or Git publishing in this continuation.

## DMR mapped following source follow-up - November 4, 2026

Applied the shared receiver-pool/history review to Capacity Plus, Connect Plus and Tier III. Fixed DMR terminators to require matching TG/slot and bound/expected color code. A first matching validated full LC can bind an unknown voice color code; an established code does not switch during that assignment. Confirmed voice CC/source metadata survives into audio/history attribution.

Added protocol/slot/CC to current-call headers, retained history, Session logs and CSV export. Optional per-carrier CC (0–15) now survives editor save/reload and optional color_code CSV; old CSV remains compatible and imports remain unconfirmed. Explicit control-carrier codes are checked separately from network/site identity. Cap+ honors an explicitly mapped carrier CC.

Dedicated voice workspaces now show assignment status and pause/resume/skip controls instead of an unrelated default profile editor. Three synthetic mapped-profile UI cases passed for repeat confirmation, researched carrier/slot dispatch, metadata/history, release, fresh-grant renewal and pause/resume. The full local suite passed 1,922 assertions, including 37 DMR following assertions; six P25 voice/log regression cases and receiver-fleet UI checks also passed; final evidence is in VALIDATION-PENDING-DMR-FOLLOWING.md. This is not a live DMR/AMBE decode or system acceptance. No release ZIP, live hardware operation or Git publishing in this continuation.

## P25 decoded-audio / log path verification - November 4, 2026

Extended local coverage from synthetic tune assignments to actual P25 voice I/Q through the production downconverter, voice decoder, identity/encryption gating and receiver-window logs. Independent OP25 clear voice vectors recover 18 voice frames, including signed I/Q with +250 kHz capture offset; wrong TG/NAC, encrypted and unknown-encryption cases stay muted. A generated P25 terminator is decoded through that same path.

The terminator test reproduced an ended call being immediately redispatched from a cached grant. Fixed natural call/hang/maximum release to retire only that cached assignment; other calls remain eligible and fresh qualifying control evidence can offer the same TG/channel again. The six-case synthetic control/tune/voice/log UI test passed with one decoded-audio record per clear call on both receiver histories and Session logs, with matching TG/frequency/NAC/label/source attribution. Control observations in this UI test are constructed decoded grants, tune acknowledgement uses a synthetic receiver worker, and RF input is generated test I/Q: this is not hardware or full-system field acceptance. The complete suite passed 1,885 local assertions and the six-case voice/log integration passed. Final evidence is in VALIDATION-PENDING-TRUNK-AUDIO.md. No release ZIP or live RF/GPS operation in this pass.

## Pending update integration review - November 4, 2026

Reviewed the pending source batch across dedicated P25 history, Fox Hunt and shared offline maps. Fixed a Windows preference-file lifetime issue that could block saving Remove/replacement during asynchronous startup map restore. Added explicit measurement count/time and current/historical snapshot labels to Fox estimates; live measurement pauses and backwards clock changes hide the previous model area. Captured cancellation tokens before background work so canceled/disposed work cannot read a disposed token source. Aligned NAC table/Session log/CSV presentation to hexadecimal identifiers.

Added regression checks for startup removal during loading, stale/historical/clock-change estimate presentation and NAC formatting. Combined field checklist: ACCEPTANCE-PENDING-NEXT.md. The full suite passed 1,872 local assertions; synthetic dedicated-receiver and Fox/Aviation UI checks passed. Local verification is recorded in VALIDATION-PENDING-INTEGRATION.md. No release ZIP, installation, Git publishing or live hardware/network reporting is performed by this review.

## Shared offline map source follow-up - November 4, 2026

Extended the pending local road-detail support to Aviation. Aircraft and trails render above the optional offline detail; importing a file switches the online tile option off. Added independent per-receiver Aviation/Fox Hunt map-choice persistence (path only, no copied map data), graceful missing/corrupt-file handling, retained prior detail on invalid replacement, explicit remove, and in-progress import/disposal guards. Automatic preference restore rejects UNC paths and oversized/invalid preference data. No network maps or receiver/decoder operations are activated by restore.

Sixteen local map preference/race checks and offline Aviation rendering checks passed; the full suite passed 1,863 assertions. Evidence is recorded in VALIDATION-PENDING-OFFLINE-MAPS.md. Optional public road file remains separate from the release. No new version, ZIP, installation, live device test or Git publishing in this continuation. Earlier session-only Fox map loading notes are superseded by this saved-reference behavior.

## Fox Hunt source improvements - November 4, 2026

Continued the queued Fox Hunt batch without release packaging: moved to a primary service-neutral tab; added a smoothed signal trend with stale/noise/quality withholding; background opt-in local rough-area estimator with spatial thinning, fixed-scope/usable-signal/GPS checks, one-road/flat-signal/alternative-location/subset ambiguity checks; clearly differentiated receiver/measurements/model-area markers. Estimates are experimental model spread, not calibrated accuracy or measured bearings. New points and scope changes invalidate the previous estimate.

Added optional bounded local WGS84 GeoJSON road/place/boundary detail import, labels/provenance/coverage information and source controls. Embedded Natural Earth remains the base map; MBTiles and automatic regional downloading are not implemented. Prepared an optional public Census Auglaize County road file outside the repository/base release; it contains no user measurements or known transmitter. Full RF/noise/GPS metadata now survives GPX export. The full local suite passed 1,847 assertions and synthetic offline UI checks passed; evidence is recorded in VALIDATION-PENDING-FOX-TOOLS.md; live field acceptance remains open. Older queued notes below are historical scope; no new version or ZIP generated here.

## Dedicated voice history source fix - November 4, 2026

Continued source work on the reported missing P25 dedicated-voice history. Added explicit assignment, acknowledged tune, first decoded audio, release/failure events mirrored to voice/control history and Session logs; available TG label/radio/NAC and receiver names; active-call header across tabs; newest-first bounded history, clear/export; scrollable setup keeps history visible. Dedicated voice now releases when the call/hang ends rather than waiting for the maximum lease timer. External model assignment emits its own Assigned event without a duplicate Start. Local regression suite passed 1,815 assertions and the synthetic receiver UI/dispatch checks passed; final UI recheck recorded in VALIDATION-PENDING-TRUNK-HISTORY.md. Awaiting next explicitly requested release; no new ZIP/version or live hardware test in this pass. Existing field confirmation remains audible voice on second SDR, with full following acceptance open.

## Dedicated P25 voice receiver history - field report November 2, 2026

User reports voice audible from the second SDR during the dedicated control/voice test, but no visible talkgroup/tuned-call log on that receiver; only the control receive monitor has activity. Treat audio reception as reported live evidence, not full trunk-following acceptance. Source review found AssignExternal sets Active directly after Start, bypassing Offer's Follow event; it does not emit the previously described Assigned event. Successful fleet assignment also is not written to Session log. Missing dedicated assignment history is confirmed in code; a completely empty history/display may involve an additional UI issue and has not been reproduced locally.

Next authorized implementation: explicit assignment/tune-success/call-end/release/failure history with receiver/control names, TG and available label, frequency, available radio/NAC and timestamps; visible current-call status on voice and a consolidated control-side assignment view. Preserve distinction between received grants, attempted assignments, successful tuning and decoded audio. Retain records after call completion and make history accessible without a clipped table; test real external-assignment path and bounded history. No build or runtime change during this review.

## ERT utility-meter research reference - November 2, 2026

User reference: https://en.wikipedia.org/wiki/Encoder_receiver_transmitter . ERT (Encoder Receiver Transmitter) is associated with automatic utility-meter reading, including electric/gas/water endpoints. Primary implementation references: https://github.com/bemasher/rtlamr , https://github.com/bemasher/rtlamr/wiki/Protocol and https://github.com/merbanan/rtl_433/blob/master/src/devices/ert_idm.c . Distinguish SCM (total consumption), SCMplus (extended ID/counter), IDM (interval history/status), and NetIDM (different generation/consumption layout); raw counts require model-specific scaling and are not inherently kWh/gallons. ERT reading is not evidence that the earlier 154 MHz suspected load-control recording is ERT.

Current source already selects rtl_433 ERT SCM/SCMplus/IDM in Analysis > Sensors / meters / bursts > Utility meters, with upstream recorded-fixture tests around 912.6 MHz. This is existing integration/replay evidence, not live acceptance of every meter. NetIDM protocol 161 is not selected by the current MeterProtocols list; do not claim it supported. Investigate upstream NetIDM crash report https://github.com/merbanan/rtl_433/issues/3684 before enabling it. Follow-on work can improve named RF presets, per-meter history/counter rollover and reset handling, explicit units/scaling, and supported-model identification. Prefer the existing rtl_433 I/Q path; rtlamr is useful as an independent protocol/reference decoder, with its AGPL-3.0 terms reviewed before any code reuse/distribution. No build, live capture or device changes for this research request.

## MeshCore sniffer implementation reference - reviewed November 2, 2026

User supplied https://github.com/mytechnotalent/meshcore-sniffer as a research reference for expanding the existing experimental direct-SDR mesh work. This MIT-licensed project targets ESP32-S3 plus an SX1262 LoRa transceiver using RadioLib; its hardware performs the LoRa demodulation, so it is not a drop-in HackRF/RTL I/Q decoder. Useful comparison areas: header/route/payload-type parsing, transport/path offsets, advertisements/public keys, raw packet export and modem settings. Its source US preset is 910.525 MHz, 62.5 kHz BW, SF7, CR 4/5, sync 0x12 and 16-symbol preamble; treat this as that project's preset, not a universal regional allocation/default.

Before reuse, compare current official MeshCore wire-format/crypto definitions and independent packet fixtures. README and code differ on some defaults and public-channel decryption; the formatter attempts public-key group decryption without checking the message MAC and can render incompletely initialized output for non-block-aligned ciphertext. Do not adopt it as a validated decoder unchanged. Short source/destination hashes and two-byte MACs can collide; they alone do not establish unique node identity, packet identity or complete topology. Preserve raw bytes and validation status; verify signatures/authentication before presenting authenticated identities/plaintext. Retain applicable MIT/third-party notices if code is reused. Reference only: no decoder change, firmware flashing, RF transmission or new build authorized by this link.

## Fox Hunt field-test evidence for next estimator/map batch

Private user GPX files and exactly matching automatic CSVs were analyzed locally against a user-provided known transmitter reference. Do not copy source coordinates, route files or the known local transmitter into application releases. All exported points matched their CSV timestamps/power values; GPS quality/freshness and fixed per-session hardware metadata were present. The user confirmed an RF-amplifier change between the two sessions, correctly reflected in CSV metadata; compare sessions separately without an assumed gain normalization.

The recordings demonstrate why the requested estimator needs safeguards: a strongest observed point can be kilometres from the known source; substantial strength variation appears within a small position cluster; many farther observations sit near the estimated floor; and route coverage can remain one-sided despite hundreds of points. Weight/withhold low-margin data, avoid treating stationary duplicates as independent evidence, keep increasing strength distinct from getting closer, and report ambiguity instead of a confident point when coverage/model fit is weak. Do not diagnose a measurement software defect from fading alone.

Preserve full RF scope (including amplifier state), noise/peak/squelch evidence and GPS quality in future standalone exports where practical; current GPX extensions omit context that the CSV retains. Use synthetic/anonymized cases for public regression tests. A public county Census TIGER/Line road layer was used successfully as local analysis context; detailed offline road rendering remains queued application work. Primary Fox Hunt navigation, trend display, rough estimate/uncertainty and improved offline maps remain next-build items. No new build was performed during this review.

## Next Fox Hunt batch: primary workspace and detailed offline maps

User requests Fox Hunt as a primary tab alongside Aviation and Broadcast, recognizing its use across radio services. Move it out of Amateur; retain receive-only, service-neutral labels and unchanged TX safeguards. Update contextual Help and navigation. This is queued for the next authorized build, not a new standalone package now.

Improve offline maps beyond Natural Earth's regional detail: roads/highways with names, town/local-area labels, water and useful local reference detail; pan/zoom/follow/fit must work without internet. Provide user-selected regional map packs/import, clear coverage/version/storage metadata and attribution. Keep large packs optional and separate from the base application; no private/local radio systems or user GPS tracks in fresh-install data. Evaluate public U.S. Census TIGER/Line road/boundary datasets and documented offline-capable map archives (such as raster MBTiles). Vector packs require their styles/fonts and renderer to work fully offline. Check format/provenance/licensing, bounded file loading and missing-coverage behavior before choosing the implementation. Do not bulk-cache standard OpenStreetMap public tiles: that service's offline-prefetch policy forbids this. Sources reviewed: https://www.census.gov/geographies/mapping-files/time-series/geo/tiger-line-file.html ; https://operations.osmfoundation.org/policies/tiles/ .

Current user-supplied GPX tracks are private analysis inputs and must stay outside application release packages. Analyze them locally; do not send coordinates to geocoding/tile services or publish a transmitter estimate. GPX analysis also informs the queued uncertainty/coverage checks, without claiming an estimator is already implemented.

## Next Fox Hunt batch: strength trend and rough transmitter estimate

User requests a rough transmitter spot once enough usable measurements have been gathered, following discussion of stronger/weaker indicators. Queue an opt-in estimated-source overlay alongside a smoothed increasing / decreasing / steady strength indicator and recent dB change. Not implemented in 0.45; await the next explicit build instruction.

Estimator scope: use one comparable fixed-frequency/device/gain/filter/capture session and validated fresh GPS positions. Thin repeated/stationary observations into spatial/time groups so a large point count alone cannot satisfy coverage requirements. Require meaningful spatial spread, route-direction diversity, usable strength variation and signal evidence before offering a location. Reject stale/invalid/simulated positions, mixed settings, clearly unsuitable noise-only/clipped data and unsupported source scenarios. Model a single sufficiently stationary transmitter; explicitly mark unknown/changing-power or multiple-emitter situations as limitations.

Evaluate a bounded, background robust spatial search using relative power with unknown transmitter level/path loss treated as nuisance parameters. Assess alternative candidate locations and sensitivity to subsets/outliers; do not simply promote the strongest receiver measurement into an estimated transmitter position. Do not invent a calibrated confidence percentage or meter accuracy. A single straight road can leave left/right or along-route ambiguity: retain multiple possible areas or withhold a pin when geometry is insufficient. More samples along the same path are not equivalent to additional route coverage.

UI: show Collecting / Not enough coverage / Ambiguous / Rough estimate with plain-language reasons. Show an uncertainty/search area and an optional clearly differentiated estimated-source marker only when supported; keep it visually separate from receiver position and strongest measured point. An arrow to an estimate must be labeled as heading toward an estimated area, never an antenna bearing or measured direction. Invalidate/recompute on session changes and stale evidence; preserve raw observations. No estimate should trigger tuning, transmission, reporting or route instructions automatically.

Validation before release: synthetic known-source routes with varied coverage, unknown output/path-loss conditions and noise/outliers; straight-line/mirrored solutions; stationary/duplicated samples; weak/flat/non-transmitting data; changing-power/multiple-source failures; setting changes; bounded processing and UI responsiveness. Compare model sensitivity and coverage, not an unsupported promise of real-world accuracy. Actual field localization remains separate acceptance. Resume-policy refinements can be considered independently; current stop/pause behavior remains unchanged.

# Current preview: 0.45.0-preview1 — initial Fox Hunt

User authorized adding Fox Hunt and building for a road-trip trial. Initial implementation: Amateur workspace, USB GPS NMEA COM reader, selectable/saved port and baud, checksummed RMC/GGA fix status and freshness, fixed-gain live receiver measurements, optional matching noise evidence, offline colored route, local CSV and full-session GPX, explicit manual stationary fallback, bounded storage/display, no-data pauses and stop-on-RF-configuration-change. No transmitter estimate, triangulation/TDOA, directional bearing entry, online map downloads or automatic TX/location reporting.

The user identifies a u-blox 7 on COM6 at 57600 baud and explicitly authorizes access for testing. A brief live read through the new M.R.S. serial backend succeeded with a fresh fix, 4 satellites, HDOP 5.77 and no checksum errors in the sample; exact coordinates are not stored in release documentation. No SDR was opened for that probe. GPS+SDR route/field acceptance remains open. The COM port was released afterward.

Also includes the source fix for APRS preset/manual Tune targets being overwritten by current RX, plus weak-mode changes unnecessarily resetting APRS edits. Existing APRS reporting/aprs.fi and 80/160 m presets remain user-accepted. Digital TX RF reception remains unconfirmed. Earlier notes below are history; Fox Hunt's initial scope above supersedes its old not-implemented status, with advanced direction-finding methods still queued.

## Digital TX trial: apparent transmit, reception unconfirmed

November 1, 2026: user reports apparent transmission but no reception reports, with low HackRF output limiting confirmation. Keep digital TX acceptance OPEN / UNCONFIRMED; do not count an FT8/FT4/WSPR RF decode or completed contact as passed. Output-power limitation is user-reported, not independently diagnosed. No settings, source, build or package changes made for this report.

## Low-band presets accepted — 0.44.0-preview1

November 1, 2026: user confirms 80 m and 160 m. Missing low-band presets marked PASS. Digital RF transmit/contact acceptance and IBP/HFGCS reference acceptance remain separate. No new build or package generated.

## Live APRS reporting accepted — 0.44.0-preview1

November 1, 2026: user confirms reporting works; aprs.fi screenshot shows KE8VPC-15 and qAO,KE8VPC in the received path with repeated position updates. Mark the basic live RF-to-APRS-IS / downstream aprs.fi reporting path PASS. Extended reconnect/filter/duplicate behavior is not separately live-signed-off. APRS preset/Tune-field correction remains a separate next-build item. No new build or package generated; published ZIPs unchanged. See ACCEPTANCE-0.44.0-preview1.md.

## Pending next build: Amateur preset target overwritten by current RX

User screenshot after 0.44: APRS North America selected at 144.390 MHz, but the Tune field shows the previous FT8 dial at 14.074 MHz. Source fix separates the preset/manual tuning target from actual RX status. Receiver channel-change notifications update only the actual RX label; selecting a preset still loads its target, and manual target edits remain possible. Weak-mode changes refresh only the weak-signal preset bar instead of resetting APRS selection/edits. Shared fix applies to APRS and FT8/FT4/WSPR preset bars. No automatic retune on tab switches. Source reviewed; pending UI/live acceptance in the next authorized build. No new build, package or RF operation performed for this report; 0.44 release ZIPs unchanged.

# Current preview: 0.44.0-preview1

The user's "build when ready" authorizes the reviewed batch. Implemented in this preview: checkbox contrast, six missing low-band presets, bounded native HackRF FT8/FT4/WSPR transmission, standard supervised/manual/optional peer sequence, default-off receive-only APRS-IS reporting, a local external JS8Call API bridge, IBP CW beacon/HFGCS starter references and HF observation logging. See UPDATE-0.44.0-preview1.md for exact limits and ACCEPTANCE-0.44.0-preview1.md for open live checks.

Outstanding from that scope: native direct-SDR JS8 encode/decode (the new bridge does not route HackRF I/Q), broader regional HF/VHF/UHF beacon catalogs, optional numbers/HFGCS transcription, external CAT/PTT and ADIF logging. Numbers-station schedules are linked to original sources, not republished. The HFGCS starter uses four dated 2022 official reference channels. No unrestricted TX, copied local data, automatic heartbeat/Internet-to-RF gating or unattended WSPR repeat was added.

Local generated-waveform, quantized-I/Q, mock network and UI checks are distinct from actual hardware/RF/QSO, installed JS8Call/radio and live APRS-IS/aprs.fi acceptance. No such live tests were performed. Prior user acceptance remains recorded, including 0.43 PSK Reporter ingestion/software metadata. The earlier review/build holds and "not implemented" statements below are historical and are superseded only for the specific implemented items above.

## Historical request notes and remaining roadmap

## Review pending: HF discovery — numbers stations, HFGCS and CW beacons

User requests considering numbers stations and HFGCS alongside the previously discussed amateur CW beacon references. Add receive-only service/reference groups with quick tuning, modes, UTC/local schedules where sourced, plain-language descriptions, recordings and observation notes. Amateur CW beacons should remain under Amateur references; HF military/utility and numbers-station research fit Analysis / HF monitoring. Keep distinct service identities rather than treating HFGCS as a numbers-station network.

Numbers stations: sourced station designators, aliases, reported modes/frequencies and dated schedule references, with last-confirmed/unknown/inactive status and uncertainty. Evaluate Priyom and other original monitoring organizations for provenance and redistribution terms; link where republishing is not permitted. Do not ship a copied third-party schedule without appropriate rights. Separate numbers broadcasts, channel markers and ordinary military/utility voice/data. Optional future CW/spoken-group transcription can capture received groups with confidence and raw recordings; this is not decryption or a claim to know operational meaning/operator identity. Identification from frequency alone is tentative.

HFGCS: source USB voice monitoring centers and network descriptions, including known EAM/SKYKING terminology as descriptions of received format, not inferred operational significance. Historical USAF flight-information documents list 8992/11175 kHz and other channels, but those documents' dates must stay visible; do not label a legacy table as verified current activity or imply all frequencies always simulcast. Check newer authoritative references where available. Provide quick tune, per-frequency activity/recording and raw timestamped logs, with optional future phonetic-group transcription requiring verification. Receiving a message does not reveal encrypted/encoded meaning and does not itself indicate an emergency or threat level.

CW beacons: build the previously discussed amateur receive-reference plan with International Beacon Project and regional HF/VHF/UHF entries, callsign, frequency, grid, schedule/source date and CW identifier monitoring. Schedule matching is an expectation, not confirmation of reception. Keep all three catalog groups independent of TX permission; discovery markers must never unlock military, utility or beacon transmission.

Sources reviewed for planning: https://priyom.org/ ; https://priyom.org/about ; https://www.numbers-stations.com/ ; historical official USAF Flight Information Handbook (2018), https://www.columbus.af.mil/Portals/39/documents/New%20Students/FIH_1MAR18.pdf . Beacon/network data needs source review before dataset implementation. User review/build hold remains active; no code, package, scheduled monitoring, RF operation or recording started.

## Review pending: missing 80m / 160m weak-signal presets

User reports missing low-band defaults. Added six sourced receive references from the WSJT-X frequency table: FT8 160m 1.840 MHz and 80m 3.573 MHz; FT4 80m 3.575 MHz plus its separate Region 3 3.568 MHz provisional reference; WSPR 160m 1.836600 MHz and 80m 3.568600 MHz. No 160m FT4 default exists in the reviewed upstream table, so do not invent one; custom presets remain available. Skip special DXpedition FT8 channels for this common-default fix. USB dial labels, regional metadata and source notes retained. Pending CSV now has 42 presets; the released 0.43 ZIP remains unchanged. Review/build hold stays active.

## Next-build scope requested: APRS reporting and FT8 / FT4 / WSPR / JS8 transmit

User explicitly requests APRS-IS receive-only iGate reporting (with aprs.fi downstream visibility) together with FT8, FT4, WSPR and JS8 transmission in the next authorized build. This expands the earlier FT8-only proposal and supersedes its exclusion of those other transmit modes. The review hold remains active: this request adds scope, but is not an instruction to build or perform RF/network operations now. Carry the reported tracking/rules checkbox contrast fixes into the same batch.

FT8 and FT4: standard operator-supervised CQ/answer and contact sequencing, manual control and optional Auto-sequence, using each mode's actual slot timing (15 s FT8; 7.5 s FT4), generated-message preview and valid local/peer addressing. Retain the earlier FT8 waveform, timing, retry/stop, half-duplex receiver-restoration and General-class TX-policy requirements; apply mode-specific occupied bandwidth and emissions checks.

WSPR: valid callsign/grid/power message and waveform, correct two-minute frame scheduling and actual transmission duration, user-selected supported dial/audio frequency and declared/calibrated output power as appropriate. No QSO handshake/auto-sequence is implied: WSPR is a beacon mode. Any repeat/duty-cycle schedule must be explicit, bounded and stopped/disarmed on fault or user Halt. Reported power must not be inferred from an uncalibrated HackRF gain value.

JS8: not currently implemented as a receive decoder or protocol backend. Include JS8 receive/encode integration as a dependency for meaningful directed messaging and transmit/receive handling, assessing backend/source licensing and supported speeds/timing. Provide supported callsign-addressed messaging and manual TX, with any heartbeat/automatic reply/repeat behavior individually opt-in; do not treat JS8 as FT8 QSO sequencing or claim a full JS8Call feature clone. Define supported features/speeds and validate interoperability before release claims.

APRS: include the separately described opt-in RF-to-APRS-IS iGate with eligible validated live packets, loop/path restrictions, credentials and software identity; aprs.fi is a downstream display to verify, not a guaranteed direct upload endpoint. No Internet-to-RF gating, RF digipeating or automatic APRS beacons are added by this reporting request.

All new RF TX remains default-disabled, explicitly armed and constrained by a separate reviewed license/jurisdiction/emission policy. User states U.S. General-class privileges; preserve declaration-versus-verification wording and actual band-edge checks. Existing decoder-only NIST/RX timing does not by itself establish RF-start accuracy. Include fresh timing, late-start refusal, actual hardware transition latency, watchdog/Halt, per-mode limits and suitable output/filtering checks. Support native HackRF first as scoped in the prior request; external transceiver CAT/PTT is a separate choice. Generated waveform/interoperability and simulated timing/state tests precede any separately authorized RF acceptance. No over-the-air test, live APRS upload or new build has been started for this scope update.

## Review pending: APRS Internet reporting / receive-only iGate and aprs.fi

User requests APRS data reporting and identifies aprs.fi as a desired destination/display. Plan opt-in forwarding of eligible live RF-decoded APRS packets to APRS-IS, from which aprs.fi and other APRS-IS consumers can display the data. Do not treat aprs.fi's read API as a packet-upload endpoint or promise immediate/complete map display. Confirm the current service documentation during implementation and label socket delivery separately from verified downstream visibility.

Use an explicit configured iGate callsign/SSID and user-supplied APRS-IS credentials, verified login before uploads, M.R.S. software/version identification, visible connection/gating status and independent enable/disable. APRS-IS read-only login (-1) does not authorize uploads, even though an RF receive-only iGate uploads eligible packets. Initial scope excludes Internet-to-RF gating, RF retransmission/digipeating and automatic station/position beacons; those require separate choices and appropriate RF safeguards. No credentials, position or callsign should be guessed or published by this planning request.

Gate validated live APRS UI frames (AX.25 control 0x03 / PID 0xf0), preserving packet addresses/content and applicable RF path, with the proper receive-only qAO construct. Exclude replay/synthetic/bad-FCS/PASSALL data, non-APRS AX.25 traffic, network-origin loops (TCPIP/TCPXX including encapsulated paths), generic queries and explicit NOGATE/RFONLY paths. Handle third-party packets according to the official rules rather than blind string matching. Use bounded duplicate suppression, line-size/CRLF validation, reconnect backoff and fresh data; do not upload a historical capture backlog after reconnect. Internet ingress is not authenticated RF evidence. Keep gating separate from display clearing/filtering, with any ignore/report interaction explicit. Preserve source/receiver/frequency provenance locally without misrepresenting unavailable fields on the network.

Validation: generated frames and a local mock APRS-IS server first, then separately enabled user live gating and downstream aprs.fi visibility. Primary sources: https://www.aprs-is.net/Connecting.aspx ; https://www.aprs-is.net/IGateDetails.aspx ; https://www.aprs-is.net/q.aspx ; official aprs.fi documentation. User review/build hold remains active. No implementation, connection, upload, RF operation or build authorized during this review.

## Review pending: full FT8 transmit and QSO sequencing

User requests full FT8 transmit with proper contact sequencing and states they hold a General-class amateur license. Queue an explicitly armed, operator-supervised FT8 QSO workflow, not arbitrary wideband TX enablement. Review/build hold remains in effect; no code, package or RF operation authorized during this review. Initial scope is standard FT8 contacts; do not silently expand to FT4/WSPR/JS8 TX, contest/DXpedition multi-stream modes or unattended automatic answering.

Plan CQ and answering-CQ workflows, select decoded station, local callsign/grid, peer callsign/grid, generated message preview, measured receive reports, report/R-report/RR73/73 progression, manual message selection, optional Auto-sequence, transmit-even/odd selection and explicit Enable TX/Halt TX. Use valid messages directed to the active local/peer pair to advance; other-station traffic, stale/replayed/unresolved identities and duplicate decodes must not advance a contact. Bound retries and total session duration; halt/re-arm on completion, timeout, source/device/frequency change, clock discontinuity, backend failure or loss of timing confidence. Preserve emergency Stop and RX/TX indication.

Generate valid 77-bit FT8 messages, CRC/FEC and continuous-phase GFSK waveform with appropriate shaping and actual occupied-bandwidth checks. The encoder library exists, but on-air FT8 waveform generation, scheduled RF output and QSO sequencing are not yet implemented. Use 15-second UTC even/odd slots and measure/calibrate real HackRF startup/drain and RX/TX transition latency. Existing decoder-only NIST correction/RX timing consensus does not establish RF-start synchronization. Require an appropriate fresh timing reference for scheduled TX; test deadline misses and reject late starts rather than transmitting in the peer's slot. HackRF is half-duplex; plan receive restoration using the same device and optional dedicated RX device without disrupting other receiver ownership/recordings.

Extend the reviewed TX policy with U.S. General-class band/emission privileges for the supported FT8 bands; retain jurisdiction and operator declarations as declarations, not verified licensing. Check the entire RF emission including USB dial/audio offset against approved segments, equipment limits and local restrictions. Do not simply relax the existing 3-second NFM test plan or copy its broad envelope into digital HF. Assess HackRF actual output level, spectral purity/filtering and any external radio/PA path before hardware acceptance; gain is not calibrated output power. Confirm CW/identification and station-operation obligations as applicable. Source reviews: WSJT-X user guide standard exchange/auto-sequencing; ARRL frequency/emission privilege references; official HackRF hardware documentation.

Validation progression: deterministic message/state/timing tests, independent WSJT-X decode of generated audio/IQ, non-radiating scheduled-output tests, then separately authorized suitable RF tests. A queued feature is not permission to transmit during development. Contact logging/ADIF and external-radio CAT/PTT can be assessed as subsequent additions rather than silently included.

## Review pending: tracking-window checkbox contrast

User reports the Watched calls only checkbox label in Heard transmitting stations is black against the dark window background and unreadable. Set an explicit theme-appropriate foreground for the tracking toolbar checkbox/labels and review the other newly added dialog toolbars for the same inherited-color issue. Preserve readable grid text on its light background. Screenshot evidence supplied after 0.43 live reporting test. Record as a contrast bug, not an acceptance pass. User explicitly requires no build until this review is complete; collect further feedback and await a build instruction. No implementation or packaging performed for this report.

Additional screenshot: Callsign watch, ignore and colors also has unreadable black checkbox labels for Highlight CQ and Exclude ignored senders from PSK Reporter (separate from hiding rows). Include both in the same theme/foreground fix. Keep dropdown/grid text dark on their light surfaces and review rule-editor feedback text as well. Review/build hold remains in effect.

## Live reporting confirmed — 0.43.0-preview1

User screenshot confirms PSK Reporter ingestion and map display of M.R.S. version and configured rig metadata on November 1, 2026. See ACCEPTANCE-0.43.0-preview1.md for evidence and remaining boundaries. Other weak-signal/live acceptance remains open; release ZIPs unchanged.

# Previous preview: 0.43.0-preview1

Weak-signal table/timing cleanup, measured SNR estimates, callsign colors/watch/ignore/session tracking, protocol-cycle clearing, UTC/local display, newest-first migration, VHF/UHF defaults and opt-in PSK Reporter. See UPDATE-0.43.0-preview1.md for implemented scope and limits. This supersedes the matching queued requests below, retained as history. Live RF and PSK Reporter/map acceptance remain pending; no automated live report or RF transmission is performed.

## Queued: weak-signal callsign ignore controls

Add an editable persistent callsign ignore list and a quick Ignore callsign action from a parsed decode. Default matching is the transmitting/sender callsign; destination/either matching must be explicit. Provide Show ignored and Unignore/manage actions, preserving raw evidence and stored decode data rather than deleting it. Apply ignore before watch/CQ display highlighting; distinguish ignored from invalid/unresolved decodes. Hiding a callsign does not stop reception/decoding of other stations. Protocol-cycle view clearing must not erase ignore rules. Keep display-ignore separate from external reporting policy: do not silently use a hidden row as permission to upload or silently change reporting selection; any ignore-to-report exclusion setting should be explicit. No build or PSK Reporter upload authorized by this request.

## Queued: weak-signal highlighting and callsign tracking

User requests configurable color highlighting and tracking for FT8, FT4 and related weak-signal modes: highlight CQ calls using a user-defined color and watched callsigns using separate colors. Build on correctly parsed sender, destination, CQ/type and grid fields rather than matching arbitrary summary/diagnostic text. CQ is a general call, not a destination callsign; support directed CQ modifiers and leave hashed/unresolved callsigns unresolved. Preserve the original decoded message.

Provide an accessible highlighting settings/rules editor with enable/disable, color preview, user-defined foreground/background with contrast checks, and editable callsign watchlist. Start with CQ/general-call and exact normalized callsign rules, allowing sender/destination/either selection. Callsign suffix/portable matching should be explicit rather than silently treating distinct identifiers as identical. Define visible rule priority, with a watched-call rule above general CQ highlighting, and distinguish persistent rule colors from the transient selected-row indication. Color must supplement readable text. Avoid regex-only configuration for ordinary users.

Track watched callsigns across received decodes with first/last seen, count, protocol, receive dial frequency/band, available audio offset, parsed reported grid and latest message. Keep sender observations separate from a callsign merely mentioned as the destination; do not infer presence/transmission from being addressed. Provide a quick Add to watchlist action from a decoded row and a filtered/follow-callsign view. Received grid is general reported location, not verified precise position. Keep watches persistent, preserve user choices, and make view clearing/protocol-cycle auto-clear independent of the watchlist and session tracking history; provide an explicit clear tracking action. Switching protocol/receiver should retain source attribution and not conflate stale records with current activity. WSPR requires mode-appropriate rules (no CQ/destination field); JS8 and other unimplemented decoders remain future integrations. No automatic TX, spotting, sound notification or map feature is implied by this request. Queued for the next authorized build; no implementation/package started for highlighting.

## Next update: VHF/UHF weak-signal receive presets

Added 14 sourced FT8/FT4/WSPR USB dial references to the shared Amateur preset CSV using the WSJT-X default frequency table: 6m (including FT8 intercontinental), 4m, 2m, 1.25m, 70cm and 23cm where the upstream table defines a mode frequency. IARU regional scope is retained in notes and non-global labels. No 70cm FT4 default was invented: the reviewed upstream table has none; user-defined presets remain available. These entries use the existing per-mode quick-tune controls and public bandplan layer in the next build. No additional decoder engines or TX permission implied. Existing user-edited working CSVs remain protected; reference refresh/merge for those installations should preserve edits. No new package created.

## Next update: newest-first across protocol views

User authorized setting all protocol monitoring views to newest on top. Shared MonitorOrder now defaults to newest-first, including Amateur, Aviation, paging, public-safety/business and Analysis/radio-data monitoring tables. On first use of the new code, legacy boolean sort preferences migrate to newest-first; versioned settings then retain any subsequent explicit oldest/newest choice. This applies at application startup in the next executable; existing running installations and the 0.42 release ZIP remain unchanged. Source change only; no new build/package created.

# Current preview: 0.42.0-preview1

Volume track click-to-position (wheel adjustment preserved), discovery references, Amateur quick tuning, aviation message consolidation and retune continuity, annotated sidebar cleanup, and default-locked U.S. amateur TX safeguards. See UPDATE-0.42.0-preview1.md for implemented scope and limitations; live acceptance remains pending. This implementation supersedes the corresponding November 1 queued requests below, which remain request history. Broader marine/business packs and new decoder engines remain future work.

# Current preview: 0.41.0-preview2

NIST reference and FT8/FT4 received-station timing, overlapping windows and clock-drift handling. Live RF acceptance remains pending.

## Queued: Amateur tuning and editable channel presets (November 1, 2026)

User requests direct tuning within Amateur, specifically AX.25 / APRS, with an APRS receive preset and editable common local repeater/simplex presets. Provide a compact preset selector, editable MHz field and explicit Tune action, showing the actual receiver frequency and keeping manual/frequency-manager/scan changes synchronized. Tune the active receiver through the shared receiver path, applying appropriate receive mode/filter settings and preserving decoder continuity where compatible. Do not tune automatically merely when browsing/editing presets. Handle audio-only/replay or unavailable/busy receivers explicitly.

Include a clearly regional APRS starting preset, verifying its frequency/source during implementation; for the U.S./North American reference assess 144.390 MHz NFM. Allow users to add, rename, edit, delete and persist local repeater/simplex presets, including label, receive frequency, mode/bandwidth and optional tone/protocol/notes. Keep reference presets and user-local data separate; preserve edits and avoid shipping personal station lists. Where practical share saved Frequency Manager entries/folders rather than maintain competing channel lists. Repeater input/offset information may be retained as metadata but must not trigger transmission. All tuning here is receive-only; TX authorization remains separate. Validate restart persistence, actual tuned-frequency display and appropriate APRS settings in the next authorized batch. No implementation or rebuild started.

## Queued: per-protocol Amateur quick-tune presets (November 1, 2026)

Extend the Amateur tuning/preset request to FT8, FT4, WSPR, JS8 and other supported or future protocol tabs with common operating frequencies. Each implemented mode should expose relevant band/frequency presets and a direct Tune action in its own workspace, plus editable saved user presets/manual entry. Switching FT8/FT4/WSPR mode must refresh the appropriate choices without silently retuning. JS8 is a future decoder; presets do not imply it is implemented. Apply the same pattern when other requested decoders are implemented.

Source and review regional common operating/dial frequencies during implementation. Label dial versus signal/occupied-channel frequency explicitly; weak-signal USB dial presets must not be mistaken for audio-offset signal centers. Include mode, receive filter and band/region metadata where appropriate. Follow actual shared receiver tuning, preserve timing/window reset behavior on retunes and show unavailable/replay/busy states. Let users add/edit/remove local presets without overwriting bundled references on update; reuse the shared reference/frequency system where practical. Preset browsing/editing does not auto-tune; explicit Tune performs a receive-only operation. No TX activation or new decoder implementation is authorized by this preset request. Queued for the next authorized build; no rebuild started.

## Queued: consolidate aviation messages under Aviation (November 1, 2026)

User screenshot shows decoded ACARS message rows in Analysis > Messages / CW / inspector. Incorporate aviation message history and inspection into the Aviation workspace so monitoring does not require switching to Analysis. Cover ACARS, VDL2 and supported ADS-B/Mode S/UAT raw/detail messages; retain aircraft/map access. Provide an Aviation Messages / Inspector view with protocol/filter controls, newest-first ordering, clear/reset distinctions, selected-message validation/source details, raw inspection and export. Preserve receiver/frequency/time attribution, bounded history and existing decoder data; do not duplicate decoding workers or reset reception when switching views. Route ordinary aviation message display out of the generic Analysis message list while retaining genuinely cross-protocol diagnostic tools where useful. Verify clear/filter/export and retune attribution alongside the queued folder-scan fixes. No message content from the screenshot needs to be copied into fixtures. Queued for the next authorized batch; no build started.

## Queued: Manage bandplans button text encoding (November 1, 2026)

User screenshot shows a replacement/corrupted glyph after “Manage bandplans” in the Display sidebar. Inspect the source label and encoding path; use clean “Manage bandplans...” or another verified supported label, and check adjacent UI labels for the same corruption. Screenshot alone does not establish whether this is source mojibake or font/rendering failure. Verify the button in the built UI during the next authorized batch. Queued only; no rebuild started.

## Queued: targeted Receiver sidebar text cleanup (November 1, 2026)

User annotated five text areas for removal from the Receiver sidebar. Remove the visible text and reclaim its layout space, preserving the related controls and behavior:

1. The device detail block below the device selector: HackRF serial text and the unique-identity/selection-and-sampling-restoration explanation. Keep the device dropdown and its identifying information.
2. The note below Reconnect same receiver on loss: “Requires a unique non-default serial. Stop cancels retries.” Keep the reconnect checkbox.
3. “HackRF quadrature I/Q · no RTL direct-sampling branch.”
4. “HackRF receive gain · applies on selection.” Keep LNA/VGA controls.
5. The note below Enable RF amplifier: “Mayhem: select HackRF mode. RX only; antenna power off. Hardware changes briefly pause USB reception.” Keep the amplifier checkbox.

Corrected annotated request: keep the Decoder setup checks heading/expander, protocol dropdown and Apply recommended settings button. Remove only the text block beneath the button: Current receiver, the capture-fit diagnostic paragraph, Recommendation text and the Applying stops reception explanation. Collapse the removed text space before RF squelch. Retain detailed guidance in Help/tooltips as appropriate. This supersedes the earlier interpretation to remove the entire section.

Recording sidebar annotated request: remove the explanatory paragraph in Automatic recording / VOX between Storage limit MB and the Arm/Disarm buttons, beginning “I/Q uses the calibrated RF threshold” and ending “retune starts a separate clip.” Collapse its layout space. Keep the folder/path, recording-format selector, threshold/stereo/pre-trigger/quiet-tail/maximum-duration/storage controls, Arm/Disarm buttons and current recording status/errors. Preserve the removed operational/encoding details in recording Help/tooltips as appropriate; recording behavior is unchanged by this visual cleanup.

Retain useful setup explanations in relevant Help/tooltips rather than permanent sidebar rows. This is the specific annotated cleanup now requested; broader sidebar redesign remains deferred. Queue for the next authorized implementation/build batch; no build started.

## Queued: transmission authorization safeguards (November 1, 2026)

User requires transmission disabled unless appropriately authorized. See PROJECT-OVERVIEW.md, Transmission safeguards. Plan default-disabled TX, jurisdiction/license privileges, declared versus checked authorization status, a separate reviewed TX policy, emission/band-edge and applicable equipment checks, deliberate session arming, timeout/Stop and shared enforcement across APRS/manual/helper RF start paths. Editable receive references/imports must never grant TX permission. Missing or unsupported authorization coverage leaves TX blocked. A license lookup alone is insufficient; ISM/license-free does not imply unrestricted SDR transmission. Queued only: current preview has not been changed to implement this interlock. No build authorized.

## Project direction: enthusiast signal discovery (November 1, 2026)

Make common-frequency/channel identification and learning a core product feature, alongside decoding. See PROJECT-OVERVIEW.md. Expand editable, sourced references for HF aviation, ACARS/VDL2, WWV/time standards, ISM bands and published channelized services. Offer compact spectrum labels, plain-language service descriptions, region/mode/setup guidance, decoder availability and optional saved-folder/scan additions. Frequency/band matches suggest candidates; they do not establish an observed signal's identity. Preserve tentative discoveries, unknowns and user labels. Keep public reference packs separate from private local monitoring data. Current aviation CSV additions are a starting point; WWV and broader ISM/channel coverage require a sourced coverage review in a future authorized batch. Documentation/planning only; no rebuild authorized.

## HF aviation reference data prepared (November 1, 2026)

Added HF voice (26 station/network entries), VOLMET (4) and HFDL (46) reference CSV layers with station/network, USB mode, sources and coverage limits; see AVIATION-FREQUENCIES.md. Include in the next authorized build and the queued opt-in known-frequency folder import. HFDL decoding remains unimplemented. No rebuild authorized.

## Aviation frequency reference data prepared (November 1, 2026)

Added 20 deduplicated ACARS monitoring centers and a separate 136.975 MHz VDL2 reference CSV. Nine ACARS centers have published monitoring references; eleven retain user-supplied candidate status. See AVIATION-FREQUENCIES.md. Existing CSV packaging includes these in the next authorized build; existing ZIPs remain unchanged. Queue opt-in Frequency Manager addition/import into Aviation / ACARS and Aviation / VDL2 folders, preserving labels, deduplicating existing entries and retaining candidate/source metadata. Generic frequency-manager CSV import is not implemented by this data-only change. Pair this with the scan/retune continuity investigation below; no rebuild authorized.

## Queued: folder-scan decoder continuity and tuned-frequency synchronization (November 1, 2026)

User reports that scanning a saved ACARS frequency folder stops aviation decoding after a receiver/frequency change. Manual tuning through Frequency Manager also leaves the Aviation frequency field/status targeting the previous channel. Screenshot shows a selected 131.55 MHz ACARS entry while Aviation still displays 129.35 MHz; selection alone is not proof of a completed tune. Reproduce both an actual Tune/double-click action and folder-scan retunes before identifying the cause.

Preserve the enabled decoder through compatible scan/manual retunes on the same receiver. Update its channel target and frequency display from the actual applied receiver tune, reset incomplete acquisition state at channel boundaries, and resume reception automatically once settled. Keep observations attributed to the correct frequency/source and prevent buffered packets from being assigned to the next channel. A single receiver can receive only its current usable capture: scanning away does not provide simultaneous reception of the previous channel. For incompatible mode, bandwidth or capture settings, show an explicit paused/unsupported reason and resume when compatible rather than silently stopping or displaying stale receiving status. Define whether each frequency field follows the receiver or is an independent target; show any intentional difference clearly.

Cross-check Aviation (ACARS, VDL2, ADS-B/Mode S, UAT) and other decoder workspaces for the same lifecycle/display issue, including paging, DMR/LTR/NXDN, P25, Amateur modes and Analysis. Exercise folder scan, next/previous channel, manual Tune/double-click, spectrum click-to-tune, scan stop/resume and capture restart as applicable. Respect explicit decoder Stop, deliberate protocol changes and receiver ownership/trunk-following assignments. Check dwell/settling requirements and report when dwell is too short for acquisition. Add a focused ACARS folder-scan acceptance case plus cross-protocol checks, distinguishing local/replay verification from live RF acceptance.

Queued bug investigation and behavior refinement only; no implementation or rebuild authorized by this TODO.

## Queued: additional Amateur and Broadcast decoders

User-requested additions, queued for a future authorized build:

- Amateur: D-STAR; C4FM / Yaesu System Fusion; PSK31; RTTY; SSTV.
- Broadcast: DRM (Digital Radio Mondiale).

Plan receive identification/header information and voice/audio or text decoding appropriate to each mode. Assess compatible decoder/vocoder backends and licensing during implementation. These requests do not claim existing support or authorize a build by themselves.

## Queued: Amateur — SSTV image reception

User requested adding SSTV to the decoder list. Planned placement: Amateur → SSTV. Support reception from SDR-demodulated audio, Windows audio input and WAV replay; assess decoder backends and licenses during implementation. Provide automatic mode detection plus manual selection for supported modes, progressive image display, slant/alignment adjustment, reset/clear and image saving with reception frequency, mode, time and source metadata. Preserve original samples and partial-image/failed-sync states. Keep SSTV distinct from WEFAX and clearly distinguish band-reference markers from working decoder support. Validate with generated/known recordings before separate live RF acceptance. Receive-only scope; await an explicit build instruction before implementation or packaging.

## Queued: Home Automation / IoT — Zigbee

User requested this addition and has multiple Wi-Fi/Zigbee-controlled devices, including Philips Hue lighting, for eventual real-world testing. Planned placement: Analysis → Home Automation / IoT. Confirm each device/bridge model and radio protocol before implementation; do not assume every device uses both Wi-Fi and Zigbee.

Start with receive-only 2.4 GHz IEEE 802.15.4/Zigbee capture through HackRF, selected-channel monitoring and optional channel scanning with explicit coverage/missed-burst limitations. Show channel, available PAN/network and device addresses, frame types, packet counts, CRC status, relative signal level and raw packet export. Separate discovered addresses from verified device identities; allow user labels and preserve source/provenance.

Follow with user-supplied network/application-key support for the user's own network and supported Zigbee device-report parsing, including suitable lighting/device-state information for Hue test hardware. Preserve encrypted/unknown payloads explicitly when keys or parsers are unavailable. Retain raw packets and distinguish local/replay checks from live device acceptance. Assess existing decoder backends and licenses during the authorized implementation batch.

Wi-Fi/802.11 capture and any Hue bridge/API integration are separate research/integration paths; this Zigbee item does not promise generic Wi-Fi payload decoding or automatic device control. No pairing, commands or device/network changes are authorized by this planning request. Await an explicit build instruction before implementation or packaging.

## Queued: Home Automation / IoT — Wi-Fi

User requested Wi-Fi viewing on the feature list. Planned placement: Analysis → Home Automation / IoT → Wi-Fi, alongside the separately queued Zigbee view.

Start with a Windows Wi-Fi adapter network survey: SSID, BSSID, channel/frequency, signal strength and advertised security/capabilities where available. Handle adapter capability, Windows permissions, unavailable/no-data states and stale observations explicitly.

Follow with packet inspection and PCAP/PCAPNG capture/import/export through a compatible Wi-Fi adapter and capture backend. Evaluate monitor-mode/Npcap support and licensing; show the actual capture capabilities and distinguish host-interface traffic from raw over-the-air 802.11 frames. Expose management/control frames, visible addresses, retries, packet counts and radio metadata only when the capture source supplies them. Preserve original packets and validation/provenance.

For the user's own network, assess optional key-assisted inspection with appropriate session/handshake material. Do not promise that a password alone decrypts all WPA2/WPA3 sessions or application encryption. No automatic device/network changes or credential collection are part of this planning request.

Later share GPS logging/map components with Fox Hunt for BSSID-specific signal-strength trails and coverage maps. Keep access points with the same SSID distinct, record adapter/channel metadata and present relative measurements rather than a precise transmitter location. A Wi-Fi adapter is the initial capture path; HackRF RF activity viewing and SDR-based Wi-Fi decoding are separate capabilities/research.

Queued only; await an explicit build instruction before implementation, capture testing or packaging.

## Queued: Cellular survey — receive-only

User requested adding the cellular survey concept to the roadmap. Planned placement: Analysis → Cellular survey. Start with cellular band/channel references and HackRF spectrum activity measurements: observed frequency range, approximate occupied bandwidth, relative signal level/noise and observation time/source. Label measured energy separately from positively identified cellular signals. Frequency coverage is not a guarantee of full-channel capture or decoder support.

Assess receive-only LTE cell search and supported broadcast-information decoding, followed by suitable 5G NR broadcast metadata where feasible. Potential fields include physical cell ID, channel number, bandwidth/configuration and network identifiers only when actually decoded. Evaluate SDR/backend compatibility, decoder licensing, CPU/sample-rate requirements and replay support before choosing a backend. Carrier/operator labels must come from decoded identifiers or an explicitly identified reference source, not a spectrum-shape guess.

Show tower downlink and phone uplink distinctions, intermittent/absent activity, capture coverage and stale/unresolved states. HackRF's 1 MHz–6 GHz range excludes higher-frequency millimeter-wave bands, and its sample-rate/usable-bandwidth limits prevent complete capture of some wide cellular channels. An uplink burst is not proof of a specific phone, person or reliable phone-presence detection.

Scope is spectrum survey and public broadcast/network metadata. No call/message content interception, subscriber identification, base-station impersonation, network attachment or cellular transmission is part of this item. Separate synthetic/replay validation from actual live RF acceptance. Queued only; await an explicit build instruction before implementation or packaging.

## Queued: Aviation — Drones / Remote ID

User requested drone detection on the feature plan. Planned placement: Aviation → Drones / Remote ID. Prioritize passive standard Broadcast Remote ID reception, evaluating OpenDroneID decoding and compatible Bluetooth, Wi-Fi Beacon and Wi-Fi NAN capture sources. Check actual adapter/driver/transport coverage; ordinary Wi-Fi network discovery, generic BLE scanning, rtl_433 sensor support and RF energy detection are not equivalent to Remote ID decoding.

Show broadcast drone identifier, message source/transport, reported position, altitude, velocity, signal strength where supplied, last-seen age, trails and raw-message export. Distinguish standard-drone control-station location from add-on-module takeoff location. Preserve unavailable fields and source provenance. Label coordinates/identities as received claims unless independently authenticated; do not infer a person's identity from a broadcast identifier. Handle stale/no-position and conflicting observations explicitly.

Later investigate DJI-specific DroneID with HackRF as a separate decoder/research path. Assess model/firmware compatibility, capture bandwidth/sample rate, frequency coverage, decoder licensing and CPU requirements. Do not promise all DJI models or conflate proprietary DroneID with standard Broadcast Remote ID. RF control/video-link activity surveys can supplement observation but must not label energy alone as a confirmed drone.

Coverage is partial: absent messages do not establish absence of drones. No universal detection range, automatic access to video feeds, device control or transmission is promised. Keep synthetic/replay checks separate from known-drone live acceptance. Await an explicit build instruction before implementation, RF testing or packaging.

## Queued: Fox Hunt — GPS signal-strength mapping

Planned Amateur workspace name: Fox Hunt. User has a USB GPS dongle. Investigate its NMEA/COM interface, then plan GPS-tagged channel power/noise measurements, a colored trail/grid, gain/filter metadata and clear no-fix/coverage states. Use fixed receiver gain and bandwidth for meaningful relative comparisons; multipath and changing transmit power limit localization. Present a general search area, not a precise triangulated point. Manual directional bearings and coherent-hardware integration can follow. GPS position alone and NIST time do not synchronize RF samples. This feature is not implemented in this timing rebuild.

## Previous preview

# Current preview: 0.41.0-preview1

Adds SAME/EAS, DTMF, MDC1200/FleetSync, FT8/FT4/WSPR and experimental direct-SDR Meshtastic/MeshCore reception. See the 0.41 update and live acceptance checklist. Next priorities remain live validation, expanded mesh payload/history support and P25/DMR trunk-following acceptance; no extra release is authorized merely by listing these.

## Previous preview

# Current preview: 0.40.0-preview1

Implements direct receiver/control/voice selection with explicit saved/unsaved feedback, assignment persistence checks, fractional RX PPM correction for RTL/HackRF, readable FLEX control escapes, expanded offline Help and About/dependency sources. See UPDATE-0.40.0-preview1.md and ACCEPTANCE-0.40.0-preview1.md. These supersede the October 25–27 queued sections below; those remain request history. Live hardware acceptance and prior unfinished checks are still pending. No new sidebar overhaul or APRS TX change.

# Current preview: 0.39.0-preview1

Multiple receiver workspaces and a dedicated control/voice pool; automatic site-channel saving; separate channel/talkgroup records; mapped Con+/Tier III group grants; saved-map imports; view-only clear and newest-first controls. See UPDATE-0.39.0-preview1.md and ACCEPTANCE-0.39.0-preview1.md. Live acceptance and previous unfinished checks remain pending. Private reference data is excluded from clean-install packages.

# TODO - simplify receiver management and trunk voice assignment (October 27, 2026)

User reports Add / Remove / Assign receiver workflow is confusing and awkward for trunk voice following. Redesign the workflow around an explicit receiver list showing name, physical device, running/stopped state, role, assigned control/site and current voice activity. Make Add receiver, device selection/start, rename and Remove discoverable, with clear feedback that removing a workspace preserves saved data. Provide direct control-receiver and voice-receiver selection in the trunk-following workspace; avoid requiring users to coordinate matching free-text group names across separate windows. Show eligible receivers, why a receiver is unavailable, assignment priority and a clear release/resume action. Preserve existing device-ownership protection, receiver settings, independent workspaces and control/voice routing; explain idle, paused and failed states plainly. Include a short setup walkthrough in expanded Help. Resolve final layout during the authorized implementation round, with an end-to-end usability check for one control plus one or more voice receivers.

Additional user report: assignment options need work and assignments could not be saved; user is unsure whether the workflow or persistence failed. Treat this as an open functional bug, not assumed operator error. Reproduce Save assignment from the existing UI, check required role/group/device inputs, success/error feedback, the persisted receiver definition and reload/restart behavior. Distinguish saved configuration from whether an SDR is open/running and eligible for voice assignment. The redesigned UI must clearly show unsaved changes, actionable validation errors, successful saving and the assignment currently in effect. Verify one-control/one-voice and multiple-voice setup, changed assignments and restart persistence. No cause or fix is confirmed yet.

Queued usability redesign; await the user's explicit build instruction before implementation, application testing or packaging.

# TODO - FLEX escaped line breaks (October 27, 2026)

User reports some FLEX messages show literal \r\n escape text rather than readable line breaks. Investigate the paging helper JSON output, parsing and message display to locate whether escaping occurs in the payload or formatting. Render actual CR/LF and confirmed escaped line-break sequences appropriately in the message detail view; keep summary rows compact and preserve original raw decoder output/export evidence. Avoid blanket unescaping of unrelated backslashes or double-decoding JSON. Verify with real affected output when available and a representative regression case.

Screenshot evidence received October 27: literal \n appears at message boundaries in both FLEX table summaries and selected-data details, on complete and reassembled messages. Selected data reports checksum passed; this does not establish the escaping source. Inspect helper output and shared message-text handling before attributing it to reception or only the table renderer. Use redacted/synthetic text for regression fixtures; do not copy intercepted personal/medical message contents into source, docs or test data.

Queued bug report; await the user's explicit build instruction before implementation, application testing or packaging.

# TODO - receiver PPM correction options (October 27, 2026)

Add per-receiver manual frequency-error correction in parts per million (PPM), with enable/disable, signed fractional entry, reset to zero and saved settings tied to device identity where reliable. Show requested RF frequency and applied correction clearly. Keep oscillator calibration separate from capture offset, tuning step, channel snapping and saved channel frequencies; do not rewrite a channel frequency to compensate for hardware error. Apply consistently across live tuning, scanning, spectrum labeling, decoder targeting and control/voice assignments, using backend capabilities with explicit unsupported status where needed. Include calibration guidance and positive/negative correction examples in Help. Preserve original I/Q and its capture metadata; replay must not silently apply a live-device correction twice.

Queued only; await the user's explicit build instruction before implementation, application testing or packaging.

# Next update TODO - detailed Help and About (October 25, 2026)

- Expand searchable offline Help with task-based setup guides, control/function explanations, recommended starting settings, examples and troubleshooting. Cover receiver roles, trunk following and channel mapping, site discovery, recording/VOX, bandplans and protocol-specific limits. Link relevant Help from each screen and distinguish supported, experimental and pending features.
- Add an About section showing M.R.S. version/build information and third-party credits. For each required or optional dependency, identify its purpose, official project/source repository, bundled or separately installed status, version/revision where known, license and included license/source notices. Explain which features require each helper and link installation/troubleshooting instructions. Include applicable library, decoder, receiver-driver and map/data attribution sources; verify details against the dependencies actually shipped when implemented.

Planning only: await the user's next explicit build instruction before implementation, application testing or packaging. The 0.39.0-preview1 release archives remain unchanged.

# Queued requirements - private references and clean-install defaults (October 25, 2026)

Fresh installs may include nationwide reference bandplans/channel allocations, but must not include localized systems, site/channel maps, saved frequency lists, talkgroup/radio aliases, paging assignments or historical reception data. Apply this boundary to runtime and source packages. Local operational data comes from explicit import, manual entry or user-enabled reception discovery. Reference allocations must not imply observed activity. Preserve existing user data during upgrades.

A private, checksum-verified source snapshot and provenance notes are held outside the release tree at the task workspace's work/radio-reference-data-2026-10-25/README.md. It includes the user-supplied UniTrunker ODS and selected historical DSDPlus/programming references. Use it to develop imports, overlapping-source comparison and mapping behavior; do not bundle its datasets or silently seed installations from it. Identical copied files are duplicate evidence, not independent confirmations. UniTrunker-versus-DSDPlus cross-comparison remains to be performed.

Queued functional work: dynamic Add/Remove receiver workspaces; system-scoped control-to-voice receiver assignments; automatic resolved site-channel saving into Frequency Manager; explicit unresolved/candidate maps; protocol-specific P25, Connect Plus, Capacity Plus and Tier III handling. Keep imported evidence and original labels. Await the user's explicit build instruction before implementation, application testing or packaging.

# 0.38.0-preview2 - optional confirmation

Adds saved tentative/noisy discovery views and per-profile optional repeat/map confirmation for Capacity Plus research. See UPDATE-0.38.0-preview2.md and ACCEPTANCE-0.38.0-preview2.md. Existing defaults and earlier acceptance remain.

# 0.38.0-preview1 - experimental trunk following

Implemented P25 Phase I C4FM group-grant following and initial Capacity Plus research/mapped group following, with shared saved profiles, allow/ignore/hold/skip, hang/maximum return and labels. Separate decoders retain control inside the same capture; optional hardware retunes explicitly suspend it. User-confirmed Cap+ carrier mapping is required; color code alone never groups systems. Live acceptance is pending. Phase II/CQPSK, other DMR trunk variants, two physical receiver coordination and automatic map inference remain future work. See UPDATE-0.38.0-preview1.md and the combined acceptance checklist.

Carry all unfinished 0.37 and earlier checks. Broader sidebar cleanup and APRS TX gain/start remain deferred.

# 0.35.0-preview1 — sensors, meters and inspection

Implemented first three requested additions using a pinned rtl_433 Windows helper: default sensor/remote decoders, selected ERT/ORION/R900 meter profiles, and bounded envelope/tone burst inspection. Includes source-attributed device results, persistent aliases, raw JSON export and receiver-owned live/replay I/Q. New service navigation moves AX.25/APRS to Amateur, existing P25/DMR/paging to Public Safety / Business, and inspection/general observations to Analysis. Follow tuned / Center lock / Free view improves high-zoom tuning. Expanded railroad CSV layers are included and seeded only when absent, preserving edits. See SENSORS-METERS.md and ACCEPTANCE-0.35.0-preview1.md.

No new trunk following or identification of the 154.463 MHz load-control format is claimed. DMR voice is user-confirmed; overall DMR sign-off awaits trunk following. Aviation map/trails, auto squelch and automatic I/Q recording remain accepted. This implementation supersedes the research-only status of rtl_433 and the deferred railroad inclusion below; further Mayhem-derived features remain research.

# Requested next integration: sensor backend and Mayhem receiver references — October 24, 2026

Add rtl_433 resources and implement a bounded Windows helper fed by M.R.S.-owned live/replay I/Q, with JSON device observations, aliases, counts, raw results and explicit validation. Initial targets are supported 315/433 MHz sensors/remotes and selected utility-meter families. Resource manifest and versioned catalog are under research/receiver-decoders/; see RTL433-MAYHEM-RESEARCH.md. Resources are now added; runtime integration and acceptance remain future work.

Evaluate Mayhem ERT SCM/SCM+/IDM, SubGhzD, weather/TPMS, Wireless M-Bus, ProtoView and analog FPV detection for adaptation. Keep meter reading, 154 MHz utility load control, FPV activity, Broadcast Remote ID and proprietary DJI/video decoding separate. Mayhem embedded apps are not Windows plugins. Pin and review licenses/provenance before packaging code.

Carry forward this suggestion round: organize service workspaces as Amateur, Public Safety / Business and Analysis (not Other), with protocol subtabs and a shared underlying decoder per protocol. Move AX.25/APRS out of Data into Amateur. Preserve Aviation and Spectrum access, user labels and separate service/protocol metadata; frequency suggestions must not override assignments. Background CW remains available outside Amateur. Implement Follow tuned / Center lock / Free view navigation so normal tuning keeps the channel visible at high zoom; preserve capture offset and overlapping waterfall history. These UI/navigation items are requested work, not implemented by this research update.

Utility load-control tone decoding remains experimental future work; FCC lookup is deferred at user request. DMR voice is confirmed but overall sign-off awaits trunk following.

# Queued for next update: expanded railroad dataset — October 24, 2026

Expanded the limited starter railroad dataset into a CSV-only add-on with eight selectable layers, 467 channel definitions and 281 distinct centers: full original/splinter/NXDN AAR references plus ATCS, PTC, ITC, ACSES and telemetry. User requested inclusion in the next update while continuing to test 0.34. The temporary addition to the active working bandplan folder was moved back out after verifying it was unchanged; current testing retains the previous dataset. Include the expanded CSV folders in the next release and account for the increased CSV count in packaging validation. Original datasets and user labels are preserved. See RAILROAD-BANDPLAN.md for sources, overlap semantics and the flagged PTC-7 discrepancy. This is reference-data expansion, not a new decoder or live-RF validation. Aviation map/trails acceptance remains complete.

# 0.34.0-preview1 — setup guidance and offline aviation trails

Implemented requested next-pass items 1 and 5: shared decoder setup checks/recommendations, explicit HackRF HF-filter diagnosis, selected/all aircraft trails, and an embedded offline Natural Earth regional basemap. Includes all 0.33 automatic I/Q/auto-squelch/railroad features. See DECODER-SETUP.md, AVIATION-MAP.md and both 0.34/0.33 acceptance lists. Aviation map/trails live acceptance was completed by the user on October 24, 2026; later issues will be fixed as reported. Main receiver auto squelch and automatic I/Q recording were user accepted on October 24, 2026. Decoder-setup and railroad checks remain pending. Street-level offline detail, airport/airspace layers and persistent flight-history storage remain future work.

# 0.33.0-preview1 — automatic I/Q captures and railroad references

Implemented: bounded RF activity-triggered I/Q recording with pre-trigger buffer, quiet tail, maximum clips and session storage budget; verified existing main-receiver auto squelch shared with Scan; editable railroad CSV references (AAR 007–097, HOT and EOT). See AUTOMATIC-RECORDING.md, RAILROAD-BANDPLAN.md and ACCEPTANCE-0.33.0-preview1.md. Main receiver auto squelch and automatic I/Q recording passed user acceptance on October 24, 2026; railroad checks remain pending. Recording edge cases are retained as reference checks.

Next requested pass: protocol setup/conflict guidance (item 1) and aircraft trails (selected/all) plus an offline usable aviation basemap (item 5). The offline map must remain usable without network access; data coverage, attribution, storage and updates should be explicit. These are planned, not included in 0.33.

# 0.32.0-preview1 — implemented bandplan and status milestone

Implemented: editable CSV spectrum references, site-selectable P25 channel maps collected from validated live control-channel observations, paired uplinks/downlinks, distinct calculated grids, and RX/TX/Idle indicator beside the clock. CSV edits reload without rebuilding. Pending observations are isolated by reception context; conflicting definitions retain evidence. See CSV-BANDPLANS.md for limits and ACCEPTANCE-0.32.0-preview1.md for new checks plus the carried-forward 0.31/0.30 checks. No trunk following or change to DMR/NXDN decoding is claimed. Earlier requested entries below are historical and superseded for this implemented subset.

## Requested: P25 bandplans collected by site

- Collect spectrum bandplan data from validated control-channel definitions and channel assignments, separately from the saved-frequency/scanning library.
- Organize CSV datasets as `bandplans/p25/<WACN>-<system>/site-<RFSS>-<site>/channels.csv`, with site-specific `spacing.csv` for advertised identifier/base/spacing/offset/slot definitions. Example: `bandplans/p25/BEE00-348/site-001-022/`. The UI may display the friendly folder name `Site 22`.
- Keep each observed LCN's downlink and uplink together in one channel record, with role, evidence/source, first/last seen and optional user label. Preserve LCN text and leading zeros. Unknown uplinks remain blank.
- Preserve separate site membership even when sites share frequencies. Enable/show/hide sites independently; when selected overlays overlap, retain all contributing site identities in details rather than duplicating unreadable labels.
- Attribute serving-site assignments only after system/RFSS/site identity is established; retain unresolved observations separately until then. Neighbor-site announcements belong to their explicitly advertised site, not automatically the monitored site.
- Show actually observed channels separately from an optional calculated spacing grid; definitions alone do not mark every possible channel as observed. Flag conflicting definitions and preserve user labels and evidence.
- This supersedes the earlier request to flatten site membership for the collected P25 bandplan. Planned feature; no current build change.

## Requested: compact RX/TX indicator beside clock

- Add a small indicator box or circle in the upper-right corner beside the clock, without adding another header row.
- Green = receiver actively running; red = transmission actively running. TX takes display priority while transmitting; use neutral gray when stopped or idle.
- Drive the indicator from actual receiver/transmitter lifecycle state, not merely an enabled control or selected mode. Clear TX on completion, cancellation or failure.
- Include a short RX/TX/Idle tooltip and accessible state text. Planned future feature; not included in the current build.

# 0.31.0-preview1 update

**Acceptance update — October 24, 2026:** User confirmed hearing decoded DMR voice. Overall DMR sign-off remains pending completion and acceptance of trunk following. Trunk following is required future work, not a capability established by this voice result.

Implemented: tuned-channel DMR and NXDN48/96 voice backend, DMR slot selection, concurrent CW candidates, bounded diagnostics and reset on tuning. Live acceptance pending. Validated structured NXDN channel records and automatic labeling remain future work. No new trunk following or key input.

## 0.30.0-preview1 — workspace and P25 audio

Implemented priorities 1–5: P25 gain/limiting, compact spectrum header/reference strips, remembered spectrum/waterfall divider, dedicated POCSAG/FLEX and consolidated P25 tabs, plus system/catalog and channel-activity split. See WORKSPACE-REFINEMENTS.md for scope and ACCEPTANCE-0.30.0-preview1.md for live checks. Historical requested items below are superseded for this implemented subset. CSV band plans, railroad dataset, Unitrunker import and automatic voice following remain future work.

## Requested: compact spectrum workspace

- Reduce oversized top header, logo/title margins and vertical padding. Remove the static RX / MANUAL TX TEST banner; keep actual active-transmission status in the relevant TX controls. Use a compact clock without the permanent OS synchronization explanation.
- Move audio level to the right end of the CTCSS/DCS status strip. Keep tone status on the left; avoid a separate audio/header row. Remove redundant RF SPECTRUM / WATERFALL heading if the Spectrum tab already identifies the view.
- Condense band-plan overlays: automatically hide no-match rows (such as NIFOG/TV outside their coverage), compact matched lanes with minimal gaps, and show detailed allocation text on hover or selection. Proposed default is at most two compact lanes plus an expand control for overlaps. Keep explicit per-plan show/hide and allow all band-plan lanes to collapse.
- Add a draggable horizontal divider between spectrum and waterfall, independent of the Data pane split. Preserve the chosen ratio and enforce small usable minimum heights.
- Remove permanent explanatory text beneath the waterfall. Put mouse/keyboard instructions in a help tooltip/menu and technical display statistics in an optional details view or existing toolbar status. Retain frequency and time axes.
- Keep tune/step/shift/zoom controls compact in a single row when space allows, with sensible overflow at narrower window sizes. Reclaim saved vertical space for the plots.
- Planned layout changes based on user screenshots; not yet implemented or packaged.

## Requested: railroad CSV band plan

- Include a dedicated U.S. railroad dataset in the planned bandplans/ CSV system, with verified AAR channel numbers, frequencies and spectrum labels.
- Research authoritative current channel assignments before creating the dataset. Distinguish analog/digital channel numbering and mode suggestions where applicable.
- Identify railroad telemetry allocations/channels separately (including HOT/EOT and relevant data systems) without implying decoder support or local occupancy.
- Keep generic channel references separate from user-entered railroad/operator labels. Support the same plan selection, show/hide and reload behavior as other CSV plans.
- Existing saved Railroad folder scanning remains separate. Dataset and spectrum overlay are planned, not implemented.

## Requested: dedicated POCSAG / FLEX tab

- Move paging decoder controls and POCSAG/FLEX observations from the general Data tab into a dedicated POCSAG / FLEX tab.
- Keep message grouping/counts, wrapped payload summaries, source/capcode labels, ignore/restore rules, filtering, inspection and export available in that tab.
- Preserve existing saved paging labels/rules and receiver/source attribution. Use a draggable split between messages and selected-message details, with compact collapsible diagnostics.
- Coordinate with the planned P25 Systems consolidation and CTCSS/DCS move to receiver/spectrum status. These UI migrations remain planned, not completed.

## Requested: editable external band-plan datasets

- Replace hard-coded band-plan datasets with files in a working bandplans/ subdirectory. Ship default datasets there and allow users to add/edit plans without rebuilding M.R.S.
- Format decision: CSV, confirmed by the user. Use one folder per plan under bandplans/, with optional bands.csv, channels.csv and spacing.csv files. Document columns for frequency ranges, channel/LCN markers, base/spacing rules, labels and optional modulation/filter suggestions. Accept human-readable MHz/kHz using invariant decimal parsing and convert to exact integer Hz internally. Preserve LCN text and leading zeros; flag date-converted identifiers rather than silently guessing. Support quoted fields and UTF-8 labels.
- Include examples/templates and migrate existing datasets while preserving their labels and behavior. The extracted BEE00-348 map is an initial candidate for a custom plan.
- Add plan selection, per-plan show/hide and Reload band plans. Validate files with useful filename/record errors; reject invalid entries without crashing or replacing a previously valid plan silently.
- Keep user datasets intact during upgrades. Define a writable working-directory location and avoid requiring administrator rights for editing. Do not overwrite customized files with shipped defaults.
- Distinguish reference channel mappings from live decoded system/site information. Define overlap precedence and make auto modulation/filter suggestions opt-in.
- Planned only; current builds do not load the exported band-plan files.

## Requested next: P25 workspace and audio refinements

- Boost decoded P25 voice audio; provide separate P25 gain with limiting to avoid clipping without changing normal receiver audio.
- Consolidate all P25 observations and decoder/voice controls from Data into P25 Systems, preserving filtering, grouping, inspection and export access.
- Future split view inspired by the user's Unitrunker reference: system/site information in the upper pane; voice-channel activity in the lower pane, separated by a draggable divider.
- Upper pane: WACN, system ID, NAC, RFSS/site, control/alternate channels and known channel/frequency mappings. Repeated status updates stay in place.
- Lower pane: stable channel rows showing frequency/channel ID, talkgroup and label, source radio ID and label, call state, encryption indication and last activity where actually decoded. Unknown fields remain blank/unknown; stale calls visibly expire.
- Keep a filterable event/history view available within P25 Systems. Use readable state colors plus text, compact controls and preserved user labels. Showing a grant does not imply voice following or audio decoding.
- This is planned work, not included in the existing 0.29.0-preview1 package.

## 0.29.0-preview1 — CW IDs and clear P25 voice

Data now offers concurrent CW ID candidates and manually tuned P25 Phase I C4FM clear voice. See [VOICE-CW.md](VOICE-CW.md) and [acceptance checks](ACCEPTANCE-0.29.0-preview1.md). Live validation remains pending; AES key support, DMR voice, trunk following and automatic CW label association remain future work. Previous APRS TX gain instability is deferred; folder scan acceptance is partial.

## 0.28.0-preview1 — Saved-folder scanning

Implemented: right-click a saved frequency folder → Scan folder; saved frequency/mode/filter order, optional subfolders/hidden exclusion, dwell/quiet delay, maximum hold for control channels, Hold/Skip/Stop and current-channel status. See [FOLDER-SCANNING.md](FOLDER-SCANNING.md). Synthetic and UI validation are separate from pending live receiver acceptance. Prior APRS preview remains included; live APRS packet acceptance is still pending.

# APRS transmit — 0.27.0-preview1

Implemented manual status/position composition, preview, AX.25/AFSK/FM generation and single-packet native TX drain. Software independent decoder acceptance passed; live RF packet acceptance pending. Automatic channel access, beaconing, GPS and acknowledged messaging remain future work.

# Manual HackRF TX test — 0.26.0-preview1

Implemented bounded one-shot FM tone generation and explicit manual TX window. Native hardware reception/stop acceptance remains pending. APRS transmit framing, channel access, messages and beacons are not implemented.

# HF reception increment — 0.25.0-preview1

Implemented optional 1.75 MHz HackRF hardware filter with capture coverage validation and sampled ADC input diagnostics. Pending live HF acceptance, noise blanker, notch filter, AGC timing and passband shifting.

## 0.24.0-preview1 identification update

DMR Cap+ CSBK/full LC, short-LC network/site fields and experimental LTR Standard outbound identification added. See TRUNK-IDENTIFICATION.md for exact limits. Local RF acceptance and automatic system association/trunk following remain pending.

﻿# M.R.S. product roadmap

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

Integrate validated observations into Analysis > Sensors / Short-range devices (superseding the earlier Data placement), with protocol/model, input frequency, source/replay status, timestamp, available signal quality, validation scope and raw bits/hex. Plan a sensor list with aliases, last-seen times, deduplicated burst counts, measurement history/units and export. Unknown signals should expose pulse timing and raw captures without speculative device labels. Support explicit saving of discovered devices/frequencies to Frequency Manager; no additional automatic starter folder is requested here.

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

### October 12: 0.18.0-preview3 saved-frequency clicks and TV channel overlay
Implemented single-click saved-signal tuning through the existing receiver-specific mode/filter and tuning checks. Programmatic selection, shared-library refreshes, folders and drag organization do not trigger tuning. Added an independent U.S. TV RF 2–36 reference layer with boundaries, center ticks and optional nominal ATSC 1.0 pilot ticks. Preferences persist per receiver and respect themes, zoom/pan, placement and compact-window safeguards. This milestone changes receiver navigation/reference UI, not the HDTV decoder.

### October 12: 0.19.0-preview1 automatic squelch and locked-spectrum activity scanner
Implemented the combined milestone for live SDRs: local noise-floor estimation and channel-power conversion, automatic/manual RF threshold, optional level lines, fixed-viewport/capture scanning, strongest-first qualification, hold/quiet-delay/skip, and batched persistent discoveries preserving user edits. Source/gain/rate/filter/FFT changes stop and recalibrate; digital listening changes preserve HackRF capture offset. See ACTIVITY-SCANNER.md for bounded work/storage, excluded center/edges, manual override, synthetic verification and remaining field acceptance. Previous saved-click and TV-reference features are included in the same package.

### October 11: original activity-scanner requirements (implemented in the October 12 preview; field validation pending)
User requested: keep the spectrum stationary, estimate the noise floor, set RF squelch, detect a frequency exceeding squelch, jump there to monitor, and add it to Frequency Manager.

- Add an explicit Start/Stop activity-scanner control per receiver. While enabled, keep the hardware capture center and spectrum viewport fixed; change only the listening frequency within the usable captured span. Respect channel-filter fit at the edges. A display zoom or sweep does not create additional simultaneous RF coverage.
- Estimate a robust noise floor from recent RF spectra, excluding occupied regions, invalid edge bins and known capture-center artifacts. Allow local floor estimates where the baseline slopes. Add an adjustable dB-above-floor margin, manual override and optional visible noise-floor/trigger lines. Slowly update the background estimate without chasing active transmissions; recalibrate after gain, sample-rate or capture-center changes.
- Detect sustained above-threshold channel activity with minimum duration, adjacent-bin grouping, bandwidth-aware power measurements and hysteresis to reject brief spikes and avoid repeated tuning. Tune to an estimated signal/channel center, not the tallest individual FFT bin. Use configured mode/filter or a separately validated channel-plan rule; energy detection does not identify a protocol.
- Monitor one selected signal at a time, hold while active, then resume discovery after an adjustable quiet delay. Provide pause/hold, skip/ignore and a documented strongest-first or round-robin policy when several signals are active. Keep detection across the capture running while listening to one channel; do not imply simultaneous decoding of all signals.
- Automatically upsert qualified discoveries into a scanner-owned Frequency Manager folder, shown in the sidebar and as spectrum markers. Store frequency, selected receive mode/filter, estimated occupied bandwidth, first/last seen, hit count and signal-above-floor strength. Mark protocol/modulation as unknown unless actually identified; do not confuse selected demodulation with detected modulation. Deduplicate with channel-aware tolerances; update repeat hits and preserve existing user names, notes and folder organization. Create the folder when the feature is first used, not as a starter-folder preset.
- Stop scanning on incompatible source changes, or explicitly re-arm after recalibration. Preserve manual tuning/stop authority. Validate with synthetic multi-signal/noise fixtures, edge cases, persistent discoveries and live RF before claiming acceptance.

The October 12 preview implements this workflow within its documented estimator and live-SDR limits. Local synthetic verification and live RF acceptance remain distinct.

### October 12: scanner, frequency correction and sidebar refinements (planned)
User-requested TODO milestone; these changes are not yet implemented or packaged.

- Remove the optional ATSC pilot-reference overlay and its setting. Interpret "pilot bandplan view" as the TV pilot references introduced in the previous milestone; retain TV channel boundaries/centers and the separate FM MPX stereo-pilot/RDS guides.
- Make the 25 kHz receive-filter choice easy to find and keep amateur-band operation flexible. NFM already offers 25 kHz; do not add a duplicate or impose NFM-only restrictions on amateur allocations. Coordinate with the wider/custom-filter roadmap item and keep modulation, receive-filter width, tuning step and spectrum span separate. Validate choices against actual receiver and demodulator capabilities.
- Add frequency controls to Scan. Proposed scope: direct listening-frequency entry and editable scan start/end frequencies, constrained to the usable locked capture span and full filter fit. Keep channel-grid spacing separately labeled. Frequency ranges outside that span require an explicit future sweep workflow, not silent capture retuning.
- Improve unattended discovery and automatic frequency saving. The current scanner already has "Save discoveries to Frequencies"; make that behavior prominent and add a discovery-only option that logs activity without following each signal. Preserve deduplication, user edits and first/last-seen/count metadata; provide clear running/save-failure/storage-limit status and useful history/export. Automatically estimated frequencies must not overwrite manually corrected centers.
- Add easy visual correction of saved frequencies: a spectrum-marker context action to set the entry to the current tuned frequency, plus an explicit marker-adjust mode with drag preview, commit/cancel and undo. Update the same library entry, preserving its folder, name, notes and activity history. Avoid accidental edits during ordinary click-to-tune or spectrum panning; distinguish overlapping markers and protect corrected centers from later scanner jitter.
- Add user-confirmed birdie markers and optional scanner/auto-save exclusions. Support both fixed RF frequencies and capture-relative artifacts, including the visible hardware capture-center/DC spike associated with offset tuning. Show the exclusion width and reason; move capture-relative markers when the capture center changes. Allow review, editing and removal without hiding the underlying RF trace. An undecoded signal or absence of voice is not sufficient evidence to automatically classify a birdie.
- Make the left sidebar horizontally resizable with a draggable divider and a remembered width per receiver. Improve grouping, spacing and label wrapping; keep common controls prominent and place advanced controls in compact collapsible sections. Verify minimum window sizes and Windows display scaling so frequency, gain, offset and scanner text remain readable without clipping.

Acceptance targets: preserve TV channel and MPX references while removing TV pilot references; expose the existing 25 kHz filter clearly; constrain scan bounds correctly; save repeated unattended discoveries without duplicate entries or overwriting corrections; correct a marker with undo; verify fixed versus capture-relative birdie behavior; and resize the sidebar at typical and enlarged Windows scaling. Live scanner acceptance remains a separate operator test.

### October 12: sidebar slider wheel adjustment (source)
Enabled fine hover-wheel adjustment for Receiver volume/RF squelch/audio gate, all Scan and Display sliders, and I/Q playback position. Up increases and down decreases; Shift gives ten-times-finer increments except integer refresh FPS. Values clamp at existing limits, partial wheel deltas accumulate, and enabled sliders consume wheel events to prevent simultaneous sidebar scrolling. Tooltips describe the controls. No release ZIP rebuilt.

### October 12: dated discoveries and batch deletion (source)
New scanner-created entries use Discovered signals / YYYY-MM-DD folders based on the observation's local date. Repeat detections update that day's entry; a later date can have its own entry. Existing undated/manual entries keep their location and continue receiving metadata updates; no automatic migration of existing folders. Date identity survives folder renaming. Frequency-list Ctrl-click toggles selection, Shift-click/arrows select visible ranges, and Ctrl-arrows navigate focus with Ctrl-Space toggling selection. Modified selection does not tune. Delete button/key removes a selection across folders in one atomic operation, includes descendants of selected folders and supports one-step Undo. Search changes clear selection to prevent deleting hidden search results. No release ZIP rebuilt.

### October 12: paging decoder priority (planned)
User supplied a spectrum/waterfall reference near 929.93 MHz identifying local paging activity and requested decoding soon. Prioritize POCSAG 512/1200/2400 receive decoding, followed by FLEX as a separate decoder milestone. Integrate protocol/rate, capcode/address, function where applicable, numeric/alphanumeric content, timestamp, receiver/frequency and validation/error status into Data with filtering, repeat grouping and export. Support live SDR and recorded I/Q first; assess conventional-radio discriminator/audio input separately. Preserve uncertain or raw content rather than claiming corrupted text is valid. Validate against independent known samples and then a local active-burst I/Q recording when available. The screenshot alone does not distinguish POCSAG from FLEX or establish a bit rate. This is prioritization only; neither decoder is newly implemented by this entry.

### October 13: sidebar resizing, retired TV pilot overlay and saved-center correction (source)
Added a draggable sidebar divider with 260–520 logical-pixel limits, a 330-pixel default and per-receiver width persistence. Sidebar labels wrap and group spacing is reduced. Removed the optional TV pilot control; normalized legacy preferences disable its ticks, preserving TV channel boundaries/centers and MPX references. Frequency Manager now offers Use tuned frequency for one selected signal, updating its center and generated default name while preserving custom names, folder, notes and history. Existing Undo applies. Drag-based correction, fuller receiver-control compaction, scan range controls, discovery-only mode and birdie exclusions remain pending. No new release ZIP built.

### October 13: visual Data Inspector / Frame Analyzer (planned)
Define a resizable inspector opened from a Data row: linked protocol field layout, raw bits/hex/text, field positions and decoded values, received versus corrected data and explicit validation scope. Start with paging and existing P25 data, add frame comparisons, waveform/symbol timing and recording-position links as decoder provenance becomes available. Preserve original data separately from decoder corrections and distinguish unknown fields from interpreted ones. Do not show fabricated waveforms or imply raw-symbol access when a decoder only exposes decoded messages.

### October 13: scanner range, direct tuning and discovery-only mode (source)
Added Scan-tab listening-frequency entry, explicit lower/upper limits within the visible usable capture and persisted discovery-only selection. Blank bounds use capture/view limits; invalid bounds are rejected before arming. Discovery-only keeps listening and capture frequencies fixed while detecting/saving candidates. Grid-offset changes now also stop/rearm the scan. Threshold, timing and grid controls are collapsible. Birdie management and drag-based saved-marker correction remain pending. No release ZIP rebuilt.

### October 13: known birdie manager and references (source)
Added named fixed-RF and capture-relative birdie entries, widths, add/update/remove controls, per-workspace persistence, independent marker/exclusion toggles and outlined spectrum/waterfall bands. Enabled exclusions filter overlapping activity candidates before tracking/tuning/auto-save; raw samples and existing saved frequencies remain intact. Custom exclusion changes stop/rearm scanning. Center offset 0 follows hardware capture center. Drag correction and further compact-layout work remain pending. No release ZIP rebuilt.

### October 13: deliberate saved-marker drag correction (source)
Added Alt-drag preview/release-to-save with Escape/capture-loss/outside-release/resize cancellation, no receiver tuning or spectrum panning, scanner-lock protection and refusal of ambiguous overlapping markers. Commits use the existing validated library update with original-frequency checking, preserve entry identity and metadata, update generated names, and support library Undo. Physical interaction acceptance remains pending. No release ZIP rebuilt.


### October 13: 0.20.0-preview1 paging milestone

Implemented POCSAG 512/1200/2400 and FLEX receive integration through an isolated, bounded multimon-ng worker, Data controls, source-aware observations, JSON fields and validation labels. Bundles all source-only scanner/UI/frequency-manager refinements since 0.19.0. Generated and local recording validation are documented in PAGING.md; live RF acceptance remains with the user. Full frame inspector, additional paging controls and long-term message archive remain later work.

### October 13: paging management source checkpoint

Added independent complete-page duplicate grouping across recipient capcodes, retained recipient sets/counts in JSON, wrapped Summary text, persistent capcode/message labels, exact message or recipient ignore rules, Show ignored and a rule editor for rename/unignore/removal. 941 local checks and full WPF smoke passed. Ready for the next packaged build; the existing 0.20.0-preview1 ZIP is unchanged. Further frame-inspector and paging archive work remains planned.

### October 14: 0.20.0-preview2 paging management and first Data inspector

Combined the paging labels/grouping/ignore/wrapped-summary work with Inspect selected: frozen fields, bytes, message text and exportable snapshot JSON. P25 NID and TSBK include available bit mappings and explicit received/decoded provenance. Paging exposes supplied metadata without inventing raw codewords. 950 checks and full WPF smoke passed. Waveform/symbol timing, frame comparisons, complete protocol maps and long-term archive remain future work.


### October 15: acceptance carryover and inspector comparison (source)

User reports preview2 stayed running without a crash; acceptance testing was started but the stopping point is unknown. This is partial stability feedback, not completion of individual acceptance checks. ACCEPTANCE-NEXT.md carries all eight preview2 checks forward with checkboxes and a resume note, plus new comparison checks. Include it with the next release.

Added per-receiver in-memory pinned inspector reference, side-by-side field/text differences, changed-only filtering and bit-position differences for compatible mapped frames. References remain frozen until replaced/cleared or receiver closure. No persistent archive or waveform analysis. Existing release ZIPs remain unchanged.

### October 17: aviation milestone implementation — 0.21.0-preview1

Added receive-only ACARS, VDL2, ADS-B/Mode S and UAT worker integration from receiver-owned I/Q, the Aviation tab, bounded aircraft table, JSON export and optional offline coordinate map with adjustable divider. Added RTL 2.4 MS/s choice. UAT worker changes preserve incomplete bursts across blocks and validate timing candidates before skipping; native 2.4 MS/s avoids unnecessary resampling. Includes the previously source-only inspector comparisons and the full carried-forward acceptance checklist. See AVIATION.md for exact capability limits, validation and field-test requirements. MLAT remains excluded; maps do not yet have basemap tiles, tracks or weather overlays. Local live reception is not yet accepted.

October 17: preview2 fixes the high-rate ADS-B worker path and queue duration;
live HackRF acceptance at 19.2 MS/s +750 kHz remains pending.

October 17 preview3: receiver DC suppression verified against supplied ADS-B recording;
live reception and continued overload checks pending operator acceptance.

## Next preview target - zoomable aviation basemap and DMR identification (October 17)

Requested after preview3 live ADS-B acceptance (127 messages, zero drops, three aircraft, two plotted positions at HackRF 9.6 MS/s +250 kHz). This section records approved scope, not completed implementation.

1. Aviation basemap: optional OpenStreetMap street tiles, mouse-wheel zoom anchored at pointer, drag pan, +/- controls, Fit aircraft and follow-selected-aircraft. Manual pan/zoom must not be reset by incoming aircraft updates. Preserve existing splitter, fresh/stale rules and offline coordinate view. Online mode is explicitly selectable; tile service receives map-area requests, not decoded aircraft messages. Visible attribution, identifying User-Agent, bounded asynchronous requests, cancellation, provider-compliant cache and graceful offline/failure status. No bulk tile downloads. Source: https://operations.osmfoundation.org/policies/tiles/ .
2. DMR/MOTOTRBO receive identification first: 4FSK synchronization, validated color code, slot when reliably established, burst type, validated link-control source/radio ID and destination/talkgroup where transmitted. Preserve unknown fields and received/decoded provenance. Full voice playback, trunk following, Capacity Plus/Connect Plus/Tier III expansion are later increments; generic DMR detection alone does not identify Motorola equipment or an organization.
3. Channel catalog: stable rows by receiver/frequency/color code, observed slots, talkgroups/radio IDs, first/last seen, counts and editable aliases. Keep frequency, color code, talkgroup and radio namespaces separate: IDs are reused across systems. Preserve original identifiers and user labels. Unsupported manufacturer data remains raw/unknown.
4. Scanner integration: separate opt-in Save/update identified channels. During an actual tuned-channel dwell, require repeated validated observations before attaching identifiers to an existing discovered frequency or creating a new dated discovery. Off-center RF detections and merely seeing sync are not enough. Store multiple observed talkgroups/slots under a frequency; do not create a frequency per radio ID. Never overwrite a user label, infer an agency name, merge systems by ID alone, or update the live discovery catalog from replay by default. Expose decode dwell/hold so the scanner has time to receive identifying bursts.

Implementation and acceptance gates: map zoom/pan/follow/offline UI checks; DMR known independently sourced vectors and generated noise/error cases; replay then user live DMR sample; scanner deduplication, identifier conflicts, label preservation and dated-folder persistence. Carry all unfinished aviation/paging/inspector checks forward. No next-preview release number assigned until scope is implemented and validated.

## Future capture aid
Signal-triggered recording: audio VOX and RF-triggered I/Q, pre-trigger buffer, hang time, automatic frequency/time naming, and bounded disk/file limits. Intended for unattended decoder-debug samples; not included in 0.22.0-preview1.


## October 18 — receiver/display refinements, 0.23.0-preview1

Implemented expanded filters, CW listening/pitch, tighter zoom, Hot signals/gradient trace, optional band-based manual tuning, larger steps, frequency-aligned RF waterfall history, compact navigation, collapsible sidebar sections, dark receiver labels, and the pending Frequency Manager fixes. Receiver auto squelch shares Scan settings and operates without scanning. See RECEIVER-DISPLAY.md and the carried-forward ACCEPTANCE-0.23.0-preview1.md. Local automated verification does not establish station acceptance. Timed scan resume, CW ID decoding, aircraft trails, protocol workspace reorganization and triggered recording remain pending.

## Queued update delivered in 0.36.0-preview1

Broadcast AM/FM and existing MPX/stereo views, Audio sidebar split and specifically requested Receiver changes, editable custom bandplan manager, searchable offline Help, structured NXDN control identities. See UPDATE-0.36.0-preview1.md and ACCEPTANCE-0.36.0-preview1.md. Broad sidebar cleanup and trunk following remain deferred/outstanding respectively.


## 0.37.0-preview1
Click-to-tune snapping, automatic MP3 VOX / combined I/Q, Aviation frequency alignment, sensor run/table counter clarification. Live user acceptance is pending. Trunk following and broader sidebar cleanup remain outstanding/deferred.

## Queued: PSK Reporter software and receiver identity

User requests M.R.S. identification in PSK Reporter monitor metadata (Using / Rig). Include decoderSoftware with the actual M.R.S. version/build (for example M.R.S. 0.43.0-preview1 only if that is the actual future build), optional rigInformation describing the actual selected receiver model, and optional editable antennaInformation. Do not expose hardware serials, private device identifiers or file paths. Rig description can be edited to reflect an external radio/audio input instead of falsely identifying the SDR. Keep metadata associated with its reporting receiver; review behavior if several receivers share one callsign/grid. Protocol support for rigInformation is documented but its service template/display handling must be verified during integration; do not promise an exact popup layout or prepopulate hardware from unrelated receivers. Source: https://pskreporter.info/pskdev.html . Opt-in reporting remains unimplemented; no reports sent or new build authorized.

## Queued: optional PSK Reporter reception reports

Official DLL PDF and current UDP/IPFIX developer protocol were reviewed. User requested this on the next feature list alongside Fox Hunt. An opt-in feature will report resolved transmitting callsigns, receiver callsign/grid, mode, RF frequency and UTC time. SNR may be omitted until a real estimate exists; sync score is not SNR. Exclude replay/synthetic/unresolved records, batch and deduplicate per service guidance, and show transmission status without claiming UDP delivery is confirmed. No reports have been submitted and implementation is not included in preview2.
