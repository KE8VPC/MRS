# Pending DMR mapped-following verification — November 4, 2026

## Scope

Source changes extend shared receiver assignment/history behavior to Capacity Plus, Connect Plus and DMR Tier III. They are pending the next explicitly requested release; the published 0.45.0-preview1 ZIP is unchanged.

- Fixed the voice identity gate so a terminator must match the talkgroup, slot and bound/expected color code. Failed link validation, excessive slot corrections and another slot are ignored. A matching validated full link-control header can bind an initially unknown color code; an established code cannot switch during the assignment. A mismatching valid header withholds voice until matching identity returns.
- Preserve confirmed voice color code/source metadata and show protocol, slot and CC in the active call, retained history, Session logs and CSV. Tuning and decoded audio remain separate events.
- Added optional per-carrier color codes (0–15) to the mapping editor and optional color_code CSV field. Save/reload preserves them; older CSV without the field remains usable. Imports remain unconfirmed. Explicit control-carrier CC remains an identity check even with repeat confirmations disabled; network/site checks remain separate. Cap+ honors an explicit mapped carrier code.
- Dedicated voice workspaces hide the unrelated profile editor and expose assignment status, Pause voice receiver, Resume voice pool, Skip call, Ignore current and Assign receivers above the retained history. Configure maps and filters on the control receiver.

## Local results

- Development compilation passed with zero errors. Existing NU1900 warnings indicate the NuGet vulnerability metadata feed could not be reached; this check is not a dependency security audit.
- Full local suite: **1,922 PASS assertions**, zero FAIL lines, including **37 DMR following assertions**. Core tests cover mapped grants, normal/tentative confirmation behavior, network/site guard, optional CC import compatibility, slot-specific retirement, fresh grants, Cap+ rest movement and voice identity/terminator mismatches.
- Final DMR receiver-window checks: **3 protocol profiles passed** (Capacity Plus, Connect Plus, Tier III). These exercise editor save/reload, repeated-grant dispatch, synthetic tuning acknowledgement, protocol/TG/MHz/slot/CC headers and history, call release, cached-grant retirement, fresh-grant renewal, and actual dedicated pause/resume buttons. A tune-only assignment produces no Decoded audio success event. Rendered screens were inspected; the dedicated view no longer displays the unrelated P25 profile editor.
- P25 receiver-window regression: **6 generated-I/Q voice/log cases passed**, including matching clear calls, wrong TG/NAC, encrypted and unknown-encryption inputs, actual terminator release and fresh-grant renewal. Exactly one decoded-audio success record per clear call appears on both receiver histories.
- Existing synthetic receiver-fleet UI checks passed after the dedicated-view change.

Evidence is local under work/dmr-follow-path-check and work/dmr-follow-path-final, including self-test.txt, dmr-follow-ui.txt, trunk-audio-ui.txt and ui-dmr-follow-0/1/2.png. Those development artifacts are outside the release package.

## Evidence limits and field checklist

DMR control observations in the receiver-window checks are constructed decoded grants; tuning uses the existing synthetic receiver worker. Identity checks include an independent parsed full-LC vector with supplied slot/CC metadata. They do not feed an actual DMR AMBE voice RF waveform through the complete receiver path. No live SDR, GPS, transmitter, network report or real trunk system was operated in this continuation. DMR voice/trunk-following acceptance remains open.

Use ACCEPTANCE-PENDING-NEXT.md for the installed/live checklist: actual researched carrier/slot mapping, full-LC/terminator behavior, decoded clear AMBE voice, Cap+ rest handoff, weak signals/late entry, USB recovery and longer operation. CC agreement alone does not identify a network or supply a missing carrier-frequency map.
