# Acceptance — 0.35.0-preview2

## Already accepted; do not repeat unless a regression appears

- ADS-B and offline aviation map/trails: accepted by user.
- Main receiver auto squelch: PASS.
- Automatic I/Q recording: PASS.
- DMR decoded voice: confirmed. Overall DMR sign-off remains OPEN until trunk following is completed and accepted. This build adds no trunk following.

## New checks

1. Analysis > Sensors / meters / bursts: start receiver at 433.920 or a known 315 MHz device frequency, choose Sensors / remotes and Start analysis. Verify known device measurements and ID, backend integrity status, source and increasing counts. A quiet band/no supported device is inconclusive, not a decoder failure. A controlled known weather sensor is preferable to unidentified traffic.
2. Label a selected device; receive another message and reopen M.R.S. to confirm the label persists while its decoded ID remains unchanged. Clear observations must not remove labels. Export a snapshot and compare raw JSON/measurements with the selected device.
3. Try Utility meters around a known supported 902–928 MHz transmission; use 9.6 MS/s and HF filter off initially. Check meter ID, raw consumption/status and integrity. Do not interpret an unscaled counter as a billing unit. Try 911.600/912.600 only as starting points; no packets is inconclusive.
4. Replay a known recording, pause/seek/retune and stop/restart analysis. Old partial frames must not become new-channel results. Live/replay and receiver A/B must remain separated. Stop the receiver and verify helpers finish/stop. Watch dropped counts during a sustained run; reduce sample rate for sensors if needed.
5. Select Burst inspection / narrow NFM and replay one of the supplied 154.463 MHz files with quiet pre-trigger data. Select a burst row; verify duration, timings, envelope waveform and raw export are readable. No utility command decoding is expected yet. Margin adjustment takes effect at the next Start.
6. Spectrum: test Follow tuned and Center lock at high zoom using steps, saved-channel tuning and click-to-tune. Tuned channel must remain visible. In Free view, panning may leave it off-screen; status and Center view should recover it. Dragging changes to Free view. Capture offset remains independent of display center, and overlapping waterfall history remains visible.
7. Workspace navigation: AX.25/APRS is under Amateur; P25/DMR/paging under Public Safety / Business; general observations/CW IDs, sensors/meters/bursts and Bandplans under Analysis. Check decoding continues while changing tabs and CTCSS/DCS remains above Spectrum. Unimplemented amateur modes are not shown as working decoders.
8. Expanded railroad CSVs: check eight selectable layers under railroad-expanded, including original AAR, splinter, NXDN references, ATCS/PTC/ITC/ACSES and telemetry. Existing custom CSVs/labels must survive; toggle a layer and reload. Original starter railroad remains available; overlapping references are intentional. PTC-7 source discrepancy remains flagged.

## Carried forward

Continue unfinished checks from ACCEPTANCE-0.34.0-preview1.md and earlier lists: decoder setup helper, concurrent real CW ID, known NXDN voice, P25 voice/workspace/site collection, frequency-list selection/edit/bulk delete/hide, saved-folder scan hold/resume and paging labels/ignore/inspector. Accepted automatic recording and auto squelch are closed despite historical checklist wording. APRS TX gain/start inconsistency remains deferred. FCC lookup is deferred.

Build: __________ Last completed item: __________ Result/settings: __________

Preview2 visibility check: P25 Phase I / voice controls, status and gain must be readable against a dark content surface; verify DMR and CW controls too. All prior pending checks remain carried forward.


## User acceptance update — October 24, 2026

- Paging: PASS; user reports working.
- Expanded railroad bandplan: PASS.
- Workspace layout/contrast: PASS.
- Spectrum navigation: PASS; no issues reported so far.
- Frequency manager: PASS; no issues found so far.
- Saved-folder scanning: scanning confirmed working. Squelch/trigger settings review remains OPEN; this does not independently close every hold/resume timing scenario.
- NXDN48 control data: live decoding confirmed, system 289 / site 12 / RAN 12. NXDN voice remains OPEN. Structured NXDN identification rows are planned for the next update.

These user results supersede the corresponding pending items above. Reopen accepted items only if a regression appears.

- P25 audio gain: PASS; user confirms level is satisfactory. P25 workspace/site collection and trunk following remain separate open items.
