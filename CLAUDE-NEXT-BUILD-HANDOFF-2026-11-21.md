# Claude review handoff — next-build evaluation milestone

> Latest build handoff: CLAUDE-BUILD-HANDOFF-0.54.0-preview2-test1.md records the verified portable test artifact, source commits, completed fixes and remaining acceptance. Original requirements below remain; use newer validation notes for implementation status.

> Field-review update: read REVIEW-FIELD-2026-11-22.md for sensor I/Q reference tests, explicit multi-protocol HackRF backlog failures, CW success at 2.4 MS/s, note 22 FT8 missed slots/preparation, and note 19 at-a-glance indicators. Current inventory is 21 active notes (1–22 excluding 3). Local recording/script index: work/field-review-2026-11-22.md; keep raw data out of source/releases.

> Saved-hunt analysis plan approved November 22: note 19 now includes offline GPX/CSV analysis and findings reports inside M.R.S.; later multi-user sharing and combined localization are a separate phase. Read the two new subsections in ROADMAP-EVALUATION-2026-11-22.md. No implementation or API changes yet.

> Latest November 22 clarification: note 19's missed route followed laptop sleep/wake, Aviation use and return to Fox Hunt with GPS not started; hardware disconnect is unconfirmed. ROADMAP-EVALUATION-2026-11-22.md now includes recovery acceptance, external roof-mounted VHF/UHF antenna context, read-only GPX findings and planned signal-coloured KML/KMZ export for Google Earth. No implementation changes.

> November 22 update: read ROADMAP-EVALUATION-2026-11-22.md for notes 17–21 and expanded Fox Hunt estimation/heat-map requirements. Current numbered inventory has 20 active items; note 3 remains rescinded. Shared documentation only; no implementation claimed.

## High-priority release requirement — note 16: code signing

User elevated code signing on November 21 after SmartScreen reported Unknown publisher for the GitHub download. Public trust is a priority. Review trusted publisher signing before the next public release; any unsigned exception requires an explicit user decision. The existing roadmap1 release remains unsigned and unchanged.

Plan trusted Windows executable signing with an appropriate publisher identity, trusted timestamping, signature verification before packaging and after extraction, and SHA-256 release checksums. Keep signing keys/certificates and service credentials protected and out of source control. Define signing coverage for bundled executable components while preserving valid upstream signatures. Signing establishes publisher identity and integrity; it does not guarantee software safety or immediate SmartScreen reputation. Do not advise disabling SmartScreen globally.

Claude: include this in release planning ahead of lower-priority enhancements. Provider/cost/identity choices and credential provisioning need user input before procurement or setup. This note authorizes planning/documentation, not purchases, key creation, rebuilds or replacement release assets. Notes 11/13 remain later research; note 3 remains rescinded. Active list is now 15 items.


> **Superseded in part (November 21, 2026):** source consolidation is complete and there is no fixed assistant ownership split. `ACTIVE-BASE.md` is authoritative for the active source tree, rollback points and coordination rules. The requirements and acceptance checks below remain current.

November 21, 2026. User requested documenting the active evaluation notes as the next-build milestone and preparing this handoff for Claude to review. This document records scope, not completed fixes or an instruction to start implementation. No message has been sent to Claude.

## Baseline and coordination

- User evaluation baseline: `outputs/MRS-0.54.0-preview1-roadmap1-win-x64.zip`, packaged November 21. SHA-256: `832C96AB62272CBC98AD4F20271123A990C87CB3110FA64B1C27CA3E9B058DF9`.
- Combined source used for that package: `work/mrs-roadmap-integration`; portable staging: `work/MRS-0.54.0-preview1-roadmap1-portable`. The original combined UI candidate is `work/mrs-0540p1-combined` and the main source remains `outputs/MRS`. Read CODEX-ROADMAP-HANDOFF.md and VALIDATION-PENDING-ROADMAP.md before reconciling them.
- Roadmap1 includes Martin M1/M2 SSTV, ITA2 RTTY, readable VDL2 and later folder/label/sync changes. Do not overwrite those with the older combined candidate. Main source has uncommitted work; preserve unrelated changes.
- Packaged executable passed 2,252 local self-checks and receive-modes UI smoke; archive executable hash matched the tested publish. These are local checks, not RF, hosted API, DPI or physical-monitor acceptance.
- No next release number assigned. There is no fixed assistant split: Claude and Codex both contribute to M.R.S. and cross-check each other. Shared files (MainWindow, navigation, tuning, time formatting, Help) use one in-progress claim at a time, as set out in ACTIVE-BASE.md.
- Website/API work stays a separate project from the M.R.S. client, and contract changes still require agreement on both sides before implementation. M.R.S. client work is open to both assistants. Notes 11 and 13 are future research, not promised working integrations for this release.

## Active requirements and acceptance

### 1. Tune fails after workspace switches
Reported: monitor Commercial/FLEX, switch to Broadcast, select channel and click Tune repeatedly; receiver does not tune. Also reported elsewhere. Inspect active receiver targeting, ownership/scanner/decoder conflicts, event routing and hidden errors; cause is unconfirmed. Acceptance: a single click tunes the intended receiver, or shows a specific actionable reason. Test stopped/running receivers and transitions among multiple workspaces.

### 2. Aviation map/table crowding
Screenshot shows Compact spectrum docking still leaves only a thin map strip and little table space. Condense controls, collapse setup/help and review adjustable table/map sizing or map-focused view. Acceptance: usable map and several readable table rows at 1040x740 and 1366x768; inspect actual content dimensions with compact/full docking and fallback placement. Do not treat a placement-only smoke pass as proof of adequate space.

### 3. Rescinded — aviation message readability
User withdrew the earlier ACARS/VDL2 readability complaint after switching from older 0.54.0-preview1 to roadmap1. Retain existing readable summaries/raw inspector; do not count this as an active defect or infer all proprietary message formats are decoded.

### 4. Settings and application-wide time
Add top-bar Settings alongside existing header actions, with extensible categories. Provide UTC, computer-local or selected time zone. Automatically convert displayed timestamps throughout tables, logs, history, schedules and details; label zones and handle DST. Keep UTC internally and retain protocol timing, original timestamps and raw received text. Derived explanations may show converted time. Define export timestamp conventions explicitly. Acceptance: preference survives restart, all affected screens agree, UTC/local/selected-zone and DST-boundary tests pass, FT8 slot timing remains unchanged.

### 5. WEFAX Next Due broadcast
Show next scheduled station, frequency, program/chart when known, start time and countdown, with Tune. Use application-wide time preference and UTC schedule calculations. Make missing/outdated schedule information clear; scheduled activity is not confirmed reception. Acceptance: next-day rollover, tied entries, empty schedules and time-zone changes behave correctly.

### 6. Cellular survey Tune and possible sweep
Add direct Tune to selected band. Assess a sweep across selected bands/ranges using usable capture bandwidth, overlapping steps and suitable dwell; single/repeat, progress/current frequency and Stop. Do not silently compete with another tuning owner. Show covered ranges and observation times, not simultaneous/full-band coverage claims. Sweep feasibility and scope need review before promising implementation. Acceptance: direct Tune works; any implemented sweep respects receiver limits and cancel/release behavior.

### 7. Bandplans organization
Replace flat checklist with collapsible P25 > system > site, Railroad, Reference and Custom categories. Use readable names plus identifiers, search and group selection with mixed state. Consider a main navigation tab, subject to header room review. Preserve CSVs, selected overlays and collection behavior. Acceptance: selections survive restart/reload, group toggles work and no files/data are rewritten merely by reorganizing the view.

### 8. Protocol tune-target field contamination
Screenshot: VDL2 Tune field shows 912.786 MHz while monitoring status shows 136.975 MHz. Reported after protocol/workspace switches. Preserve selected protocol default/preset or its saved user target, separate from actual receiver frequency; inspect status-update overwrites. Tab browsing alone must not retune. Acceptance: switching protocols refreshes the correct target, user edits persist appropriately and Tune uses that visible target. Review together with item 1, without assuming a common cause.

### 9. Aviation persistent offline map cache
Reuse tiles viewed/downloaded online when offline, with cached coverage/zoom, storage usage, bounds and cache management. Keep aircraft/trails above the basemap and missing areas explicit. First inspect existing cache implementation and provider terms; offline persistence and bulk regional downloads are different capabilities. Choose a permitted provider/offline source if current terms cannot support the requested behavior. Acceptance: restart/network-loss tests show permitted cached coverage; no silent bulk fetching or false full-coverage claims.

### 10. ACARS channel/folder scanning
User has eight or more saved ACARS frequencies but Aviation exposes one target. Add a saved-folder dropdown, include-subfolders and ACARS filtering; reuse the shared scanner where practical. Provide Start/Stop, Hold, Skip and previous/next channel with message-aware dwell/hold. Preserve frequency/receiver/time provenance. Research simultaneous decoding within one captured span separately from stepped scanning outside it. Acceptance: mixed-folder filtering, mid-message hold, Stop/release, transitions to other workspaces and missed-channel limitations are clear. Shared folder scanning may later serve other compatible protocols.

### 11. Later research — additional website sharing
Potential aircraft-seen and other reception summaries: identify record schemas, provenance, deduplication, live/replay distinctions and opt-in preview. Heard aircraft need not have a decoded position. Keep credentials, hardware serials, local paths and private notes out of shared payloads. Deliver a proposal for website/API coordination, not a hosted change or automatic upload.

### 12. P25 radio/unit IDs
Screenshot shows Talkgroups only. Determine whether unit IDs are decoded/stored but hidden, or require more decoding/persistence. Add a Radios/Unit IDs view with system identity, ID, editable alias, first/last seen and count; show evidenced talkgroup/frequency associations. Scope IDs by system and distinguish transmitting/source from addressed/destination IDs. Acceptance: show known decoded IDs, leave missing IDs unknown and never derive unit IDs from TGIDs; verify persistence and provenance.

### 13. Later research — audio/data streaming
Define selected receiver monitored/decoded audio and decoded event/data sources, local/LAN/remote destinations, formats, buffering, reconnect, access controls and bandwidth. Preserve call identity through trunk following and expose gaps/drops. Deliver an integration proposal; no streaming service or external transmission authorized by this note.

### 14. Help/manual revamp
Collapsible task/workspace categories; searchable topics with category context, Back/Forward, related links and context-sensitive entry points. Organize setup, control reference and troubleshooting. Review outdated version-specific pages and keep history clearly labelled. Improve adjustable sidebar and theme readability. Acceptance: offline operation, search/back navigation, new SSTV/RTTY pages and all existing topics remain reachable.

### 15. Compact controls throughout UI
Size short-value fields/selectors to contents and place related controls side by side where space permits. Prioritize Receiver filter/sample rate, offset/gains and excessive full-width controls. Keep long device identities available, explanations collapsible and narrow-panel layouts responsive. Preserve readable labels, keyboard access, usable targets and DPI scaling. Acceptance: reduced scrolling without clipping at supported window widths and DPI settings; coordinate with Aviation/layout work.

## Suggested review and implementation sequence

1. Done November 21: source consolidated into outputs/MRS (commit 112d3d1); rollback b9fefb8. See ACTIVE-BASE.md.
2. Investigate items 1/8 together; establish reliable receiver/tune ownership before adding scanning.
3. Plan shared UI sizing/navigation and time formatting (2/4/7/14/15).
4. Implement scoped WEFAX, map cache, ACARS/folder scan, cellular tuning and P25 ID work with focused validation.
5. Return separate research proposals for 11/13 and undecided sweep/multi-channel backends.
6. Run regression/package checks and keep local/synthetic results separate from station acceptance.

The separately planned 17-mode SSTV expansion remains in ROADMAP.md, using VIS/manual selection and excluding line-timing detection. It is not automatically part of this evaluation-fix milestone. No code changes, build, commit, push or deployment were performed for this handoff.

### Note 9 expansion — shared saved offline maps (November 26, 2026)

User approved adding this workflow to the roadmap. Start with Aviation's online map: navigate to an area, select **Save area for offline use**, choose geographic coverage and detail/zoom levels, review estimated download/storage size, then name and save the region. Provide one shared Offline maps manager accessible from Settings and map windows. Saved regions should be reusable by Aviation, Fox Hunt and the future Coverage Survey, without downloading separate copies for each workspace.

Manage saved coverage, available detail levels, source/licence, download date, storage use, updates and removal. Support manual region selection and automatic use of available saved coverage when offline. Keep aircraft, hunt measurements and survey overlays separate from the underlying map. Show incomplete downloads, unavailable detail levels and uncovered areas explicitly; preserve the existing GeoJSON import path.

The current viewed-tile cache is a starting point, not an existing save/export feature or a guaranteed complete regional map. Research and select a provider or offline dataset whose terms explicitly permit the requested downloads; do not turn the current online tile service into a bulk downloader without that permission. Define a managed region package and portable import/export workflow after choosing the source and format; current Load offline detail accepts GeoJSON, not arbitrary tile packages.

Acceptance: save a named permitted-source area; restart with the network disabled; render its saved detail in both Aviation and Fox Hunt; verify coverage/detail boundaries, interrupted-download recovery, shared storage, update/removal and import/export where supported. Test overlays remain usable and no aircraft, GPS tracks or observations are uploaded by saving map detail.

Planning only. This expands existing note 9 rather than creating a duplicate item. No implementation, map downloads, build or release is authorized by this roadmap edit.
