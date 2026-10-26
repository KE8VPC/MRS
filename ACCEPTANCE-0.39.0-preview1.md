# Acceptance - 0.39.0-preview1

## Receiver management and following

1. In Receivers add/open workspaces for the devices you intend to use. Confirm each has an independent name, device, gain/rate/mode and audio output. Duplicate device selection must be rejected. Test default/ambiguous serial dongles manually; no guessed reconnect identity. Remove one workspace: its source stops and window closes; saved settings/lists/recordings remain. Reopen and check settings.
2. Use one known P25 Phase I C4FM control receiver and one started voice receiver. Assign Trunk control and Trunk voice with the same group, save, then start following in the control workspace. Verify the voice frequency/TG/NAC against grants. Control capture and observations must continue during voice tuning/audio. Test out-of-span voice frequencies on the dedicated voice SDR.
3. Add a second eligible voice receiver and verify separate simultaneous calls use separate devices. Test receiver priority and an independent receiver that must not be commandeered. A different trunk group must not receive assignments. Review CPU/drops/USB behavior at your actual sample rates with up to five devices; local virtual tests do not establish hardware capacity.
4. Test quiet/maximum call release, control Stop/close, serving-identity change, failed tuning, unavailable receivers and source loss. Manual tuning/Stop on assigned voice must pause its pool eligibility. Resume voice pool must restore eligibility. Skip/Ignore on voice must affect the owning control's follow rules. Check correct frequency labels and no stale audio/VOX clips across assignments. Reconnect behavior needs hardware review.
5. DMR: test mapped Capacity Plus first. Test Connect Plus and standard Tier III separately with known decoded group grants and correct carrier maps. Con+/Tier III imported network/site must match actual decoding before following. Missing/incorrect maps remain explicit; false CRC, private/data and absolute-MBC grants must not be treated as supported group assignments. Check target slot and full voice identity. Late-entry/rest loss remain separate live checks. Do not equate a passing Cap+ test with Con+/Tier III acceptance.

## Discovery, saved channels and imports

6. Enable Auto-save site channels in P25 Systems. Receive identifier tables, site status and voice grants. Frequency Manager should populate P25 / WACN-System / RFSS - Site. Repeated grants for different TGs on one carrier must update one channel, without TG/radio text becoming its name or permanent carrier attribute. Preserve a manually edited name/notes on later updates. Check uplink, channel ID, role and last seen.
7. Save existing site discoveries from a retained catalog. Check unresolved and provisional status counts. Enable/disable Include provisional / tentative. Advertised neighbor channels belong to their target site, not the monitored site, and must remain labeled advertised. A target system with unknown WACN must remain provisional.
8. Under DMR site frequency saving, test decoded network/site folders, existing-row saving and mapped voice-carrier saving. The same CC on unrelated systems must not merge them. Unverified rows must not silently become verified saved identities. Inspect the separate Talkgroups tab for scoped IDs, labels and reception updates; frequency records must remain channel-centric.
9. Import a chosen DSDPlus.P25data or DSDPlus.frequencies file. Preview/select DMR system/site. Preserve carrier/OTA numbering, unresolved entries, historical provenance and unconfirmed status. Reject conflicting carrier frequencies. Con+/Tier III identity matching must protect against applying another system's map. Existing local labels/settings must survive. Private reference backups are not automatically imported.
10. Verify a clean data profile starts without localized frequencies/sites/maps/aliases/receiver assignments. Nationwide reference CSVs may exist. Verify an upgrade retains your real data. Confirm no original backup or recording was modified.

## Monitoring views

11. In POCSAG/FLEX select Newest on top and receive a new page and a grouped repeat. Both must sort by last received time. Switch to Oldest on top, reopen and verify preference. Repeat on other monitoring tables. Keep a row selected and scroll away while messages arrive; selection/reading position should remain useful.
12. Clear view on P25 catalog/activity/messages, DMR/NXDN, aircraft, sensors/meters/bursts, talkgroups and trunk history. Display clears while reception continues; saved lists/maps/aliases/recordings remain. New observations reappear. Switch grouped/raw and live/replay views to check filtering. Distinguish Reset message history / existing reset actions from view-only clearing. Check HD/TV, diagnostics and session clear actions.

## Previous acceptance carried forward

Complete all outstanding ACCEPTANCE-0.38.0-preview2.md, 0.38.0-preview1 and 0.37.0-preview1 checks with this package: optional confirmation/noisy discovery, live trunk-following/maps/rest changes, snap-to tuning, MP3 VOX/Both/digital recording, stereo/clip boundaries, Aviation alignment and sensor counter clarity.

Still open from earlier previews: Broadcast AM/FM/MPX layout, Audio sidebar, searchable Help, custom bandplan manager, NXDN structured display/save; supported sensors/meters and aliases/export; pulse/burst inspector; P25 site collection; concurrent real CW ID; known NXDN voice; decoder helper; folder-scan squelch/trigger/hold review.

Already user accepted: ADS-B/offline aviation map/trails, main auto squelch, automatic I/Q, paging, railroad bandplan, workspace contrast, spectrum navigation, frequency manager, P25 audio gain, DMR conventional voice heard and NXDN48 control data. Reopen only for regressions. Overall DMR/trunk-following sign-off remains pending.

Broader sidebar cleanup and APRS TX intermittency remain deferred. No live RF/hardware/transmission acceptance is claimed by automated checks.

Build: ______ Last completed check: ______ Device/rate/settings/result: ______
