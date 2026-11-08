# Acceptance - 0.40.0-preview1

## Receiver assignments (priority)
1. Open Receivers and leave it open while opening/starting each physical device. Add, rename and select workspaces; check that source/status feedback is accurate and a device cannot be opened twice.
2. Save one receiver as Trunk control. Check one voice receiver, Save assignments and verify the success message. Reopen the manager, restart M.R.S. and confirm the roles/control relationship remain. Actual SDR sources must be started separately after restart.
3. On a voice receiver use the Control receiver dropdown. Test missing/invalid control and priority inputs: a clear Not saved reason should appear without losing edits. Check Unsaved feedback, reload and discard prompts. Save a name and role without starting hardware; distinguish stored routing from live eligibility.
4. Add a second voice receiver, verify priority and simultaneous independent calls. Uncheck one and save; verify Independent state. Move a voice receiver to another control. Change/remove a control and confirm old dependents/leases are released. Existing receiver settings/lists/recordings must remain.
5. From Trunk following use Assign control / voice receivers. Test P25 and mapped DMR with real radios, including manual pause/resume, stop/release, failed tuning, absent receiver, source loss and restart. Confirm control stays receiving and no stale audio/recording carries between calls. Same-process receivers only.

## FLEX display
6. Receive alphanumeric complete and reassembled FLEX pages containing line breaks. Table text should be compact without literal backslash-n; readable details should show real CR/LF/TAB layout. Decoder fields/raw export should retain original escapes. Verify validation and recipients unchanged. POCSAG/numeric output must remain unchanged. Use redacted examples when reporting bugs.

## RX PPM
7. For each uniquely identified RTL/HackRF, stop, enable correction, enter signed fractional PPM, Save and Start / Apply. Compare a stable known carrier before/after. Verify displayed nominal center, applied PPM and hardware command. Positive increases command; choose the sign that corrects your device. Check gain/offset changes, manual tuning, scanning and dedicated voice retunes.
8. Select another device and confirm independent settings; return/restart and confirm restored calibration. Generic/duplicate serials must explain session-only correction. Reset to zero and Save. Invalid values and corrected commands beyond hardware range must be rejected. This is RX frequency correction, not TX or sample-clock correction.
9. Record manual and automatic I/Q with correction, then replay. Confirm original samples/nominal frequencies and correction provenance remain; no second correction occurs. Old recordings must still open. Do not overwrite original recordings.

## Help / About
10. Browse About sources/notices/status with bundled helpers present/missing. Presence must not imply a healthy decoder. Verify source links, version/build and setup instructions; no automatic downloads from simply opening About. Keep notices/source with redistributed helpers.
11. Search Help for control voice, PPM, MP3, FLEX, missing helper and bandplans. Follow setup guides. Verify F1/context controls and source links, readable dark/light UI and usable layouts at typical/enlarged display scaling.

## Carried-forward acceptance
All unfinished ACCEPTANCE-0.39.0-preview1.md and previous carried checks remain: live P25/Cap+/Con+/Tier III following, site saving/imports, multi-device USB/reconnect, clear/sort behavior, MP3/Both/stereo clip boundaries, snapping, sensor/meter observations, concurrent real CW and NXDN voice, decoder helper and folder-scan timing. Prior user passes stay accepted unless a regression appears. Fresh installations must not contain private/local site data. Broader sidebar work and APRS TX intermittency remain deferred.

Build: ____ Device/rate/settings: ____ Result / last completed check: ____
