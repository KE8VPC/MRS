## Live PSK Reporter observation — November 1, 2026

User supplied a PSK Reporter map screenshot filtered to receptions by KE8VPC. It displays monitor locator EN70vn76, Using: M.R.S. 0.43.0-preview1, Rig: HACKRF MRS V.43, and last report AA1ZZ at 2026-11-01 22:13:30 GMT, with 21 transmitters shown for the selected map interval. This confirms successful live report ingestion and software/rig metadata display for the user's tested setup. This is user-supplied service evidence, separate from local packet/mock tests. It does not independently verify every report's frequency/SNR/timing, duplicate suppression, every mode, multi-receiver coordination or long-run reliability. Those checks and unrelated acceptance remain open. No additional report was sent and no new build was made to record this observation.

# 0.43 user/live acceptance

Carry forward unfinished prior acceptance. Local fixtures and generated UI checks do not establish live RF performance, hardware behavior or PSK Reporter service acceptance.

1. Receive live FT8 and FT4; compare message text, sender/destination/grid interpretation, DT and estimated SNR with WSJT-X using comparable audio. SNR is approximate and may differ under filtering/interference/clipping; no claim of improved weak-signal sensitivity. Check WSPR display separately. Switch UTC/local and restart to check persistence.
2. Check newest-on-top across protocol monitoring views. The first upgrade migrates old boolean sort preferences; later chosen order must persist.
3. Try auto-clear off, one cycle and two cycles for FT8/FT4/WSPR. Clear only at completed batches (including empty cycles); keep freshly decoded rows for their cycle. Check retunes, source/mode changes and late decodes. Tracking and report queue must remain independent.
4. Choose a CQ color, watch a callsign with another color and reorder rules. Check sender/destination/either and exact/base portable matching, readable foreground and distinct row selection. Ignore/unignore and Show ignored should hide/reveal recent evidence. Follow sender and return to all. Restart to check saved rules.
5. Open Tracked stations; verify only actual parsed senders count as transmissions, with source/mode/dial/grid/timestamps and latest message. Watch-only filter and explicit clear should work. This tracking history is session-only.
6. Inspect the timing panel while NIST is not checked, current, stale or unavailable and during replay. Offset and applied correction must remain distinct. Windows time must remain unchanged. Expanded details retain manual/NIST/RX controls.
7. Check VHF/UHF quick-tune presets with appropriate receiver coverage. Regions and USB dial semantics must be clear; unsupported bands do not become TX-authorized. Existing edited reference files must remain intact.
8. PSK Reporter starts off. Only when you choose to test live reporting: enter your receiver callsign/grid, confirm optional rig/antenna information and explicitly enable during live SDR USB reception. Check queued/sent/error states, duplicate suppression and appearance on the service using M.R.S. identification. No live service acceptance is claimed until verified. UDP send is not delivery confirmation. Check replay/unknown-frequency/unresolved calls are excluded, separate ignored-report policy, receiver change re-enable and restart-off behavior. Reports share the configured identity/location; no RF transmission is involved.

Other decoder additions, P25/DMR trunk-following completion and carried-forward live RF/device checks remain outstanding.
