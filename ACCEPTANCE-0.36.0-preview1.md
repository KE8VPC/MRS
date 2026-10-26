# Acceptance — 0.36.0-preview1

## Already accepted — repeat only if a regression appears

- ADS-B, offline aviation basemap and trails.
- Main receiver automatic squelch and automatic I/Q recording.
- Paging, expanded railroad bandplan, workspace contrast, spectrum navigation and frequency manager (no issues reported so far).
- P25 audio gain.
- DMR decoded voice heard; overall DMR sign-off still waits for trunk following.
- Live NXDN48 control data: system 289 / site 12 / RAN 12. This confirms the backend; new structured display/save controls below need checking.
- Folder scanning operates. Squelch/trigger configuration review and detailed hold/resume scenarios remain open.

## New update checks

1. Broadcast > AM: select a channel and Tune selected. Verify staged tuning while stopped and AM reception when started. Verify the channel list and shared spectrum. Standard RTL tuner mode may not cover medium-wave; choose compatible hardware/sampling settings.
2. Broadcast > FM: tune a known station; verify WFM and RF, MPX, Audio (L+R), Waveform L/R views, pilot/stereo, force mono, RDS/RBDS and meters. Switch between FM, AM and Spectrum; history/zoom should stay consistent and FM-only controls should appear only in FM while WFM runs. The channel list is a tuning reference, not a station directory.
3. Audio sidebar: verify input selection/WAV/test tone, output device, monitoring, volume and input gate. Check Receiver frequency/mode share a row, and the two TX buttons are absent. Broader sidebar cleanup is deliberately deferred.
4. Help: use the top ? button and F1; search MPX pilot, squelch, NXDN, recording and bandplans. Read the matching topic offline. After editing a Help/*.md file, reopen Help to see it.
5. Bandplans: Manage bandplans from Display; toggle plans and reload. Create/edit a custom CSV plan, add/delete rows, validate, export, import with preview, duplicate a supplied plan, and delete a custom plan. Malformed frequencies/colors must be rejected; external edits made while an editor is open must not be overwritten. Supplied/collected plans require duplication for in-app edits. Check custom colors on the spectrum.
6. NXDN: normal-polarity NXDN48 at 461.6375 MHz should show Control data received, system 289, site 12, RAN 12 and increasing counts. A replay row must remain labeled replay and must not be automatically saved into the live frequency list.
7. NXDN saving: with live reception and three matching observations, Save / update selected or opt-in auto-save. Existing names/notes must survive; check stored signaling identifiers. Retuning/restarting starts a separate confirmation context. This does not add trunk following or prove NXDN voice acceptance.

## Carried forward

- Supported sensors/remotes and utility meters: known-device live decode, label persistence and raw JSON export.
- Burst inspector: timings/envelope/export on supplied 154.463 MHz recordings; actual utility-command decoding is not implemented.
- P25 workspace/site/channel collection, concurrent real CW ID, known NXDN voice, decoder setup helper.
- Folder scan squelch/trigger review and detailed hold/resume timing.

P25/DMR trunk following remains unfinished development. APRS TX gain/start intermittency and FCC lookup remain deferred. No live RF or transmission was performed to validate this build.

Build: __________ Last completed check: __________ Settings/result: __________
