# Acceptance - 0.38.0-preview1

## New trunk-following checks

1. Start a live quadrature receiver at a known P25 Phase I C4FM control channel. Open Public Safety / Business > Trunk following. Create/save a P25 profile. Confirm identifier updates/grants arrive; unresolved channel IDs must remain unresolved until a table is received.
2. Start following with control and a known voice frequency inside the usable capture. Confirm active TG, radio, frequency and decoded audio against the control grant. Control observations must continue while voice plays. Wrong NAC/group and encrypted/Phase II calls must not play.
3. Test Allow, Ignore and Hold, then Apply filters. Skip must return and avoid the skipped TG for five seconds. Quiet hang and maximum call timers must return to control. Save a TG label and profile, restart M.R.S., and confirm restoration.
4. Test an out-of-span call with hardware retunes disabled: no hardware jump or invented voice. With explicit retune enabled, confirm the hardware leaves control, status shows control suspended, and hang/maximum or Stop / return brings it back. Verify capture offset/usable bandwidth and failure messages. Manual tuning and either scanner must cancel following.
5. Capacity Plus: create a separate system profile and current rest frequency. Record observed CC, slots, advertised rest LSN and group status. Unknown/unconfirmed carrier mappings must remain silent and unresolved. Do not confirm an LCN/frequency association solely because a rest LSN was advertised there.
6. Enter independently verified carrier LCNs/frequencies and evidence. Confirm them. LCN 1 maps LSN 1/2; LCN 2 maps LSN 3/4. Verify real group/slot audio, repeated mapped rest changes and return behavior. A changed control CC must stop for profile review; equal CC across profiles must not merge them. Missed rest hops may require restarting on the actual rest carrier.
7. Export/import the Cap+ CSV: imported rows must be unconfirmed. Check duplicate/invalid LCN rejection, editable notes and per-profile labels. Verify profile persistence. No claim of Connect Plus/Tier III/LTR/NXDN following.
8. Arm MP3 VOX and receive a clear followed call. Confirm playable decoded audio and followed frequency in recording metadata/name. On control-only or mismatched/encrypted calls no voice should trigger. Check no stale audio between calls or after Stop. Review CPU and dropped counts at your actual HackRF rate; an overrun must stop following explicitly.

## Carried forward - please test with this build

Complete all nine checks in ACCEPTANCE-0.37.0-preview1.md: spectrum/waterfall snap and persistence; MP3 VOX pre-roll/tail/playback/muted monitoring; digital voice recording; stereo and clip bounds; Both MP3 + I/Q; retained encoding failures; centered Aviation entry; current-run versus retained sensor totals.

Still open: 0.36 Broadcast AM/FM/MPX layout, Audio sidebar, offline searchable Help, custom bandplan manager, NXDN structured display/save. Older live checks: supported sensors/meters and aliases/export, burst inspector, P25 site collection/workspace, concurrent real CW ID, known NXDN voice, decoder helper and folder-scan squelch/trigger/hold review.

Already accepted: ADS-B/offline aviation map/trails, main auto squelch, automatic I/Q, paging, railroad bandplan, workspace contrast, spectrum navigation, frequency manager, P25 audio gain, DMR conventional voice heard, NXDN48 control data (system 289/site 12/RAN 12). DMR overall sign-off awaits trunk following. Reopen accepted items only for regressions.

Broader sidebar cleanup and APRS TX gain/start intermittency remain deferred. No live hardware acceptance is claimed by local tests.

Build: __________ Last completed check: __________ Settings/result: __________
