# Pending combined update - field acceptance

These checks apply after the next explicitly requested release package. This document does not authorize packaging, installation, RF transmission or external reporting. Existing 0.45.0-preview1 ZIPs remain unchanged.

## P25 dedicated voice following

- With one live control SDR and one assigned voice SDR, verify that the control stays on control and the second tunes the grant. Confirm current TG/frequency and optional saved label appear in the voice window header.
- Compare Tuned and first Decoded audio records on both receivers' trunk histories and Session logs. A tune without supported clear decoded voice must not create a Decoded audio record. Received grants remain distinct from followed calls.
- Check hexadecimal NAC consistency (0x prefix), talkgroup/radio IDs, newest-first ordering, Clear view and CSV export. Confirm completed history remains after a call ends.
- After a real call terminator, check that the receiver does not immediately return to that ended call solely from cached grant data. Verify a new valid grant for the same group can be followed again.
- During two simultaneous calls, apply Allow/Ignore/Hold changes on the control receiver. Verify only excluded calls release and eligible calls continue. Clearing filters and receiving a fresh qualifying grant must allow following again.
- With receivers of different tuning coverage, verify an unavailable grant does not prevent later supported calls from being assigned. No unavailable channel should be marked Tuned or Decoded audio.
- At maximum-call expiry, verify the same ended call does not get reassigned from cached data within that update. A fresh grant may renew it.
- Follow several successive calls. Verify call end/hang releases the receiver; manual Stop pauses eligibility and Resume voice pool restores it. Check a failed tune is clearly reported and paused.

## Fox Hunt

- Open the main Fox Hunt tab. Connect the user's GPS at its configured port/baud (u-blox 7, COM6/57600 where still current). Use one fixed-frequency/gain/antenna session and verify fresh GPS/RF points and CSV logging.
- Check Stronger/Weaker/Steady with temporal coverage; missing/stale/noisy observations must be reported without a directional claim. Strength increase does not by itself establish decreasing distance.
- Stop the log for a stable snapshot, Calculate rough source area, and inspect count/time plus historical labeling. Poor coverage, a mostly straight road, inadequate noise/GPS evidence or mixed settings must withhold a pin. Compare any model result with a known source; do not infer calibrated accuracy from its dashed area.
- While logging, check a long measurement pause hides the previous area. New points and scope changes clear it. Export GPX and verify gain/noise/quality metadata alongside the original CSV.

## Offline map detail

- Load a supported local regional GeoJSON file in Fox Hunt and Aviation. Verify roads/names at suitable zoom, pan/follow/fit, source attribution and outside-coverage status. Aircraft/trails must remain visible above detail.
- Confirm Aviation offline import leaves Online street map unchecked. Restart each receiver and verify its independent local file choice restores.
- Remove detail and restart: the removed file must stay unselected. Move a chosen map file: the base map should remain usable with a missing-detail message. Invalid replacement should preserve the existing map.
- Confirm no local map or route appears on a fresh installation unless explicitly imported. Optional regional files remain separate from the base package.

Local development checks and synthetic UI evidence are recorded in the VALIDATION-PENDING documents; they do not replace these live/installed-system checks.

## Mapped DMR following

- Test Capacity Plus, Connect Plus and Tier III separately with known researched carrier mappings. Check profile save/reload and optional color_code CSV preservation. Imported rows remain unconfirmed until deliberately verified.
- In a dedicated voice workspace, verify the control-profile editor is hidden, Pause releases and withholds the receiver, and Resume makes it eligible again. Confirm Skip and Ignore apply to the owning control receiver.
- Compare actual carrier MHz, TG, slot and expected/confirmed CC against the voice-window header, history and Session log. Confirm an assignment without actual decoded voice produces no Decoded audio record.
- Check matching full LC and terminators; another slot/TG/color code must not end the followed call. Verify call end releases only that assignment, and a fresh valid grant can renew it.
- Check expected network/site and declared control-carrier color code against the monitored system. Color-code agreement alone does not identify the network.
- For Cap+ rest changes outside the control capture, review Allow hardware retunes outside capture and verify the control reaches the mapped rest carrier while a dedicated voice receiver keeps following its call.
- Clear DMR AMBE voice, weak-signal/late-entry behavior, rest handoff, USB recovery and longer operation remain live acceptance items.
