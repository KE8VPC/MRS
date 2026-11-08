# Receivers and direct control / voice assignments
Context: Receiver fleet
Keywords: add remove assign save control voice trunk group priority device receiver setup

Use Receivers at the top of M.R.S., or Assign control / voice receivers in Trunk following. The manager is a separate nonmodal window: keep it open while selecting devices in receiver windows. Each receiver keeps its own gain/rate/mode, audio output and recordings. Saving an assignment does not start the radio.

## One control and one voice receiver
1. Open Receivers. Select your first workspace, rename it clearly and choose Trunk control.
2. Save assignments. A success message means the configuration was written. The device may still be stopped; this is shown separately.
3. Add receiver, then Open receiver if needed. In that receiver's window choose the family, Find, select the physical device and Start / Apply. Use a different physical SDR from control.
4. In the manager select the control receiver again. Check the desired receiver in Voice receivers assigned to this control. Save assignments. It is changed to Trunk voice automatically.
5. Alternatively select the voice receiver, choose Trunk voice and select the saved Control receiver in the dropdown; Save assignments.
6. Start the control SDR, open its Trunk following tab, configure/save the system profile and Start following. The control remains on control/rest while voice receivers tune to grants.

## More voice receivers and priority
Check additional receivers under the control and save once. Lower priority numbers are selected first. Multiple supported calls may use separate idle receivers; active calls are not preempted. Unchecking a linked voice receiver and saving releases it to Independent. Choose a different control from the voice dropdown to reassign it. New assignments use internal IDs; you do not type matching group names. Legacy typed groups remain readable until edited/migrated by saving.

## Save feedback and unsaved changes
Editing name, role, control, voice checklist or priority marks the form Unsaved. Save validates the name, priority (0–999) and control selection before writing. Errors say Not saved and explain the problem. Switching rows or closing with unsaved edits asks whether to discard them. Reload saved discards the editor changes. Saving roles may stop following, but it does not erase frequencies, labels or recordings. The saved definitions persist on restart; actual receiver windows/sources must be opened and started again.

## Device and activity status
Closed means Open receiver first. Stopped means select/start the SDR in its window. Ready for voice assignments means a live eligible source, not a call in progress. Following for identifies the owning control. Paused means manual tuning/Stop or a failed assignment removed it from automatic eligibility; use Resume voice pool after reviewing the source. I/Q replay cannot serve as live control/voice hardware. A unique physical device cannot be opened twice within this M.R.S. instance.

Skip and Ignore on an assigned voice workspace forward to the control's rules. Quiet/maximum timers end leases. Stopping/closing control releases its voices. Role/control changes release existing assignments. Each voice workspace uses its own listening output and P25/DMR voice gain; idle dedicated voice suppresses discriminator buzz. Separately launched M.R.S. processes do not share a pool.

## Remove and troubleshoot
Remove receiver closes its source/window and removes the definition after confirmation. Saved settings/lists/recordings remain. Dependents of a removed control are released. Use a stable USB hub/controller and sensible sample rates; five virtual workers in local tests are not proof of five-device USB capacity.

If no voice is assigned, check live quadrature mode, frequency coverage, full filter fit, paused state, receiver priority, supported grants and talkgroup filters. In the control choose a valid profile/map; missing DMR carrier mappings stay unresolved. Read Trunk following and Site channel saving for protocol-specific setup.

## Dedicated voice call display and history (pending next release)

The window header shows the assigned talkgroup, saved trunk-profile label and voice frequency while following, even when viewing another tab. The control header lists its assigned voice receivers. Public Safety / Business > Trunk following retains newest-first history on both receivers; the setup controls scroll so they cannot consume the entire history area. Clear view hides displayed rows while retaining history for export; existing Session log files remain. Export history CSV includes the receiver/control names, TG label, frequency and available radio/NAC. The history column, Session log and CSV nac_hex field display NAC in hexadecimal (for example 0x293), matching P25 identifier conventions; talkgroups and radio IDs remain decimal.

Assignment requested means a tune was requested. Tuned means the receiver acknowledged the frequency/capture, not that voice decoded. Decoded audio is logged once when matching decoded audio reaches the listening pipeline; it does not verify the selected speakers are audible. Released includes the lease length and reason. These events also appear in both Session logs. Completed records remain after the receiver becomes idle. Voice call termination or its hang timer releases the voice receiver for another assignment.

## Ended calls and cached grants

When a followed call ends or its hang/maximum timer releases the receiver, that assignment's cached grant is retired from the control receiver's available-call pool. The receiver waits for a fresh qualifying control observation before following that same channel/talkgroup again. Other available calls remain eligible, and a new valid grant can immediately offer the same talkgroup again. Manual stop still pauses the voice receiver; Skip/Ignore retain their separate control-side rules.

DMR assignments also show slot and color code when known, in addition to TG and MHz. Normal following still needs a correctly researched carrier map; a color code alone does not determine the system/network. Call-end/retirement is specific to the channel and slot so another active slot stays eligible. Synthetic mapped-profile checks do not establish live AMBE voice acceptance.

## Dedicated voice controls

A workspace assigned the Trunk voice role shows assignment status and history instead of the control receiver’s profile editor. Configure the system, carrier map and filters on the control receiver. Pause voice receiver releases its current assignment and keeps it out of the pool; Resume voice pool makes it eligible for another assignment. Skip call and Ignore current apply to the owning control receiver. Assign receivers opens the receiver manager. Clear view clears displayed history, without deleting receiver definitions or maps.

## Changing filters during following

Apply filters on the control receiver also checks calls already assigned to its voice receivers. An Ignore, Allow or Hold change releases an excluded talkgroup; calls still permitted continue. This release does not pause the voice receiver. Clearing a filter allows a new qualifying control grant to assign it again. Maximum-call expiry retires the ended cached grant before another assignment is selected, so the receiver waits for fresh control evidence before returning to that same call.

## Receiver tuning coverage

The control receiver checks each available voice grant against the running voice receivers' tuning range and capture bandwidth. A grant that none can receive is skipped for that update; later supported calls can still be assigned. The skipped grant is not marked tuned or decoded, and can become eligible while fresh if a suitable receiver becomes available.
