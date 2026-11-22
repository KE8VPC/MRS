# Evaluation roadmap additions — November 22, 2026

User requested recording all subsequent notes and suggestions. Planning/documentation only: no implementation, live investigation, capture, build, signing setup or publication. ACTIVE-BASE.md controls source coordination. Notes 1–16 remain in CLAUDE-NEXT-BUILD-HANDOFF-2026-11-21.md; note 3 is rescinded. There are now 20 active numbered items (1–21 excluding 3), including explicitly future research.

## 17. ADS-B receiver dropouts — reliability investigation

User reports frequent source/receiver interruption during heavy ADS-B traffic on HackRF at 1090 MHz. Screenshot shows Source ended; queued aviation data processed; start again to resume, with 2,665 decoded and 1 dropped. This confirms interruption, not its cause. NFM versus AM made no difference; reducing LNA/VGA made no difference. Laptop was on battery during assessment.

Correlate receiver/USB faults, decoder worker exits, queue pressure, CPU/memory, UI/map load and power-management state. Distinguish receiver stream failure from worker/decoder failure and intentional stop. Consider a controlled battery/AC comparison without silently changing power settings. Acceptance: sustained high-traffic reception, bounded queues/UI load and actionable failure diagnostics; recovery must not invent continuous coverage. Synthetic/replay stress and live hardware evidence remain separate.

## 18. APRS reporting passcode persistence

Persist APRS-IS reporting passcode for the configured callsign across restarts; this is the APRS-IS reporting path that can appear on aprs.fi, not an aprs.fi account password. Callsign change must invalidate the association and require confirmation/re-entry before reporting resumes. Protect credentials using appropriate Windows credential storage; exclude secrets from logs, exports, sync, source and packages. Acceptance: same-call restart retention, changed-call invalidation and no secret exposure.

## 19. Fox Hunt continuity and estimation improvements

Reported loss: approximately 30 minutes of drive data after sleep/wake and returning to Fox Hunt without noticing GPS acquisition/logging was not started. This supersedes the initial description of a GPS disconnection: a hardware disconnect is not confirmed.

### Later field clarifications and export review

- User sequence: laptop slept; GPS dongle light stayed on; user woke laptop, tried Aviation, returned to Fox Hunt and drove the next route without noticing GPS was not started. Device light does not establish an application connection, fresh fix or active recording.
- Explicitly test suspend/resume and Aviation-to-Fox-Hunt transitions. Reconnect configured GPS as appropriate, verify fresh fixes and display a persistent Not recording GPS points warning until recording is actually active. Preserve a visible acquisition gap; never fill it with stale positions. Receiver/decoder operation and GPS/logging states must be distinguishable.
- Mobile RF context supplied for reference: external roof-mounted VHF/UHF dual-band vehicle antenna. Antenna response and vehicle effects affect RF comparison; this does not establish a cause for GPS/logging state failure. Do not assume calibrated gain or suitable response at every monitored frequency.
- Read-only review of the supplied November 22 GPX: 1,326 points, one continuous segment, 09:18:28–09:41:25 EST (14:18:28–14:41:25 UTC), about 22m57s. Frequency 774.306250 MHz; relative level range -39.40 to -5.07 dB, strongest sample around 09:36:09 EST. Largest point interval about 1.24 s. Frequency/gain/filter/sample-rate context unchanged within the file. GPS fixes were recent through its final point; this file does not diagnose what happened after recording stopped. GPS quality varied (4–8 satellites, maximum recorded HDOP 7.58). Do not treat relative dB as calibrated dBm. Original file unchanged; no route, coordinates, serials or raw recordings copied into source/release inputs.
- Google Earth GPX import shows a uniform route style, not radio-strength colours. Exporter already includes relativeDb and RF metadata under custom GPX extensions; this is not proof those fields are rendered by external viewers.
- Plan a Google-Earth-friendly KML/KMZ export with strength-coloured points/segments, explicit legend and units, timestamps/frequency/settings in point details, preserved gaps and no false transmitter pin. Retain GPX/CSV originals. This is an export enhancement under notes 19/21, not implemented support. Ensure any conversion preserves metadata and makes the styling scale/comparability explicit.

- Automatically connect the configured GPS when Fox Hunt starts; reconnect after loss, with explicit status and optional audible warning.
- Clearly distinguish receiving signal samples, valid GPS fixes and actually recorded geolocated points. Do not silently attach stale positions to new samples.
- New hunt / Resume hunt / Import-add to current hunt choices. Persist enough state to resume after restart; append older-run data without duplicate samples or loss of original timestamps/provenance. Preserve source runs and back up before any rewrite.
- Save receiver/antenna context, LNA/VGA, RF amplifier, filters and settings changes by sample or segment. Allow changes during research; flag incomparable segments rather than preventing analysis. Do not assume raw levels across devices/settings are calibrated.
- Show preliminary estimates on incomplete data, with an explicit exploratory display option. Exploratory/standard/strict evidence thresholds control admission, not a manufactured confidence percentage.
- Report spatial coverage, signal/noise quality, consistency and alternative candidate areas. Reject or label noise-only, mixed-transmitter, flat-signal and inadequate-route cases. Count alone is not evidence of adequate geometry.
- Show sampled and missing directional sectors relative to an explicit search centre, selected site or provisional estimate. Keep route/sample extent distinct from an inferred transmitter boundary.
- A southern survey-edge line may indicate the limit of useful sampling. If strongest readings lie there or strength is increasing toward it, suggest more southern sampling; do not claim that line locates the transmitter.
- Show candidate areas, elongation/multiple alternatives and uncertainty; changing settings or data must invalidate stale results appropriately.
- Suggested search-direction arrow from current GPS to the best-supported candidate area, with a direction cone reflecting ambiguity. Separate estimated transmitter direction from recommended next survey direction. Show multiple candidates when needed, smooth updates, and mark stale GPS/insufficient evidence explicitly. This is model-derived guidance, not a measured antenna bearing or a calibrated probability.
- Shared heat-map capability with note 21: measured points/tracks remain inspectable; bounded optional interpolation is labelled estimated and unsurveyed areas stay blank.

Later research: multi-user hunt upload/download, source attribution, comparison and combined estimates. Keep contributor, device, antenna, gain, time and frequency context. Signal-strength localization differs from triangulation using measured bearings. Coordinate schemas/API before implementation; no sharing enabled by this note.

Acceptance: disconnect/startup warnings, no-fix recording state, restart/import/resume/deduplication, mixed-setting segmentation, incomplete north/east route and untested south/west visualization, ambiguous arrows and known-source replay tests. Validate qualitative evidence labels before use; numerical confidence requires calibration. Never infer missing measurements.

## 20. Listening squelch/mute independent of decoding

Provide an accessible listening mute/squelch option while reception, P25 control decoding and Fox Hunt signal/GPS logging continue. Keep it independent of RF measurement thresholds, scan triggers and hold rules. Investigate the existing audio-monitor controls before duplicating functionality. Acceptance: silent control-channel monitoring retains decoding/measurement throughput and visible recording status; unmute restores audio without retuning or resetting acquisition.

## 21. Future Coverage Survey side project

RF coverage mapping / drive-test survey / coverage-hole detection, reusing Fox Hunt components while retaining its separate monitoring purpose.

- Log GPS route, signal level, noise floor, available validated SNR estimates, decode success and dropout duration. Distinguish actual coverage loss from receiver/GPS failure and an idle transmitter.
- Detailed offline-capable city/rural maps are core. Support transmitter-site pin selection/entry, coordinates, label and source; known site is optional for Fox Hunt but supports range analysis and estimator validation.
- Configurable distance rings, per-point site distance and reception by direction/distance. Report maximum observed usable reception on surveyed routes under a stated criterion, never guaranteed coverage radius.
- Heat maps of strong/weak reception, noise, decoding success and dropouts when measured. Show legends, original points/routes, sampling density and missing coverage. No colour filling of unobserved areas as established reception; bounded smoothing must be labelled estimated.
- Filter/compare by frequency, run, device, antenna and settings segments. Preserve originals and expose calibration limits; different gains/devices must not silently form one absolute signal scale.
- Repeat-route comparisons and later multi-user import/export/sharing with provenance. Suspected RFI/building/terrain causes remain hypotheses unless supported by additional evidence.
- Share mapping, heat-map and model-evidence components with note 19; preserve uncertainty, sampled sectors, survey edges and candidate/search-direction distinctions.

Acceptance planning: known synthetic coverage grids, empty/partial routes, intermittent sources, gain changes, transmitter-pin/range calculations and offline rendering. Field testing needed before any accuracy claim. This side project is future research, not promised next-release delivery.

## Current numbered inventory

| Note | Scope | Status |
| --- | --- | --- |
| 1 | Cross-workspace Tune failure | Fix/investigate |
| 2 | Aviation map/table space | UI improvement |
| 3 | Aviation message readability complaint | Rescinded |
| 4 | Settings; application-wide UTC/local/selected zone | Planned |
| 5 | WEFAX Next Due broadcast | Planned |
| 6 | Cellular Tune; assess sweep | Planned/research |
| 7 | Categorized Bandplans; assess main tab | Planned |
| 8 | Protocol target overwritten by receiver frequency | Fix/investigate |
| 9 | Aviation offline tile-cache behavior/management | Planned |
| 10 | ACARS saved-folder scan/step; multi-channel research | Planned/research |
| 11 | Additional website data sharing | Later research |
| 12 | P25 radio/unit-ID persistence/catalog | Planned/investigate |
| 13 | Audio/data streaming | Later research |
| 14 | Categorized Help/navigation | Planned |
| 15 | Compact responsive controls | Planned |
| 16 | Trusted code signing and release verification | High-priority release requirement |
| 17 | ADS-B source dropouts | Reliability investigation |
| 18 | Callsign-bound APRS passcode storage | Planned |
| 19 | Fox Hunt continuity, incomplete estimates, arrows/heat maps | Planned; sharing later |
| 20 | Independent listening mute/squelch | Planned |
| 21 | Coverage Survey, maps/rings/heat maps | Future side project |

Separate previously approved roadmap: 17 additional SSTV modes (Scottie S1/S2/DX; Robot36/8BW/24BW; PD90/120/160/180/240/290; Pasokon P3/P5/P7; Wraase SC2-120/180), bringing total to 19. VIS/manual selection only; line-timing detection excluded. Other long-term protocol/backend items remain in ROADMAP.md; this addendum does not delete or claim completion of them.
