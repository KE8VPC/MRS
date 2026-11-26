# M.R.S. active source base

Updated November 21, 2026. Read this first, before editing anything.

## One base

`outputs/MRS` is the only active source tree. Do all new work here.

As of the consolidation commit after `b9fefb8`, `src/` and `Help/` are byte-for-byte identical to `work/mrs-roadmap-integration`. That is the source of the user's roadmap1 evaluation package, `MRS-0.54.0-preview1-roadmap1-win-x64.zip` (SHA-256 832C96AB…8DF9). The user is testing that package. All 313 source files and all 57 Help files were compared by hash. `native`, `decoders`, `maps`, `testdata` and `bandplans` were already shared through junctions.

The base contains:
- Codex's Weather/WEFAX, DMR saving and duplicate review, SSTV Martin M1/M2, ITA2 RTTY, readable VDL2, and the folder/label/website-sync fixes.
- Claude's spectrum layout (docked under fixed main tabs) and the ui1 radio bar, theme, status bar and shortcuts.
- The version label 0.54.0-preview1-roadmap1.

Rollback points:
- `b9fefb8` is the main checkout before consolidation: all roadmap source work, with the older interface.
- `336dc8d` is the earlier checkpoint, from before the combined UI preview.

## Frozen reference copies (do not edit)

- `work/mrs-0540p1-combined` and `work/mrs-0540p1-combined-baseline` hold the original 0.54.0-preview1 candidate.
- `work/mrs-roadmap-integration` is the integration copy. It is now identical to this base. `work/prepare-roadmap-integration.ps1` is retired; do not run it again.
- `work/mrs-0510p2-claude-ui` (ui1) and `work/mrs-0530p1-claude-spectrum` (layout previews) are superseded.

## Working together (Claude and Codex)

The user works with both assistants on all items. There is no fixed split. Each one contributes, cross-checks the other and debugs. To keep that safe:

1. **Start clean.** Run `git status` before starting. If someone else's work is uncommitted, stop and ask the user. Do not overwrite it or fold it into your own commit.
2. **Claim before editing.** Add a line to "In progress" below with your name, the note numbers and the main files. Remove the line when you commit.
3. **One item per commit.** Name the note number(s) from `CLAUDE-NEXT-BUILD-HANDOFF-2026-11-21.md` in the commit message, and list the checks you ran.
4. **Cross-check from git.** The reviewer reads `git show <commit>` or `git diff`, not a separate copy. Review notes go in a `REVIEW-<topic>.md` file or in the handoff.
5. **Files that need care.** These are touched by several items: `MainWindow.xaml(.cs)`, `PacketControls.cs`, `AdditionalReceiveControls.cs`, `AviationPanel.cs`, `AircraftMap.cs`, `SpectrumPlacement.cs`, `ClaudeUi.cs`, `HelpWindow.cs` and `Help/`. Only one in-progress claim at a time per file.
6. **Unchanged rules.** No build, package, push, website upload or API change without the user's go-ahead. Keep library data, Fox Hunt routes and coordinates, credentials and receiver serial numbers out of commits and packages. Keep CRLF line endings in existing files.

## In progress

(none)

## Next-build milestone

Scope: `CLAUDE-NEXT-BUILD-HANDOFF-2026-11-21.md` and the top of `ROADMAP.md`. These are Claude's review findings from November 21. They are starting evidence, not confirmed fixes:

- **Note 8.** The cause is confirmed in Aviation. `PacketControls.cs` connects `receiver.ChannelChanged` to `Aviation.Follow()`. Any receiver retune therefore overwrites the Aviation frequency field and the decoder target, even when Aviation is not running. Check other workspaces for the same pattern.
- **Note 1.** This is a likely cause, not yet confirmed. `LiveTune` refuses silently when the receiver isn't started, when I/Q replay is active, or when the frequency is out of range. The reason only goes to the Log, which most workspaces don't show. A tune also stops trunking, folder scan and the activity scanner without notice.
- **Note 9.** `AircraftMap.cs` already caches viewed OpenStreetMap tiles (up to 2,048, with expiry files). What remains is offline and stale display. Bulk regional download needs a different provider. The map server user-agent still reports 0.35.0.
- **Note 12.** Grant radio IDs already reach the P25 activity panel. The work is persistence and a catalog view, unless verification shows otherwise.
- **Note 4.** About 30 places format times. Build one shared display-time helper before notes 5, 10 and 12 add new timestamps.

## Release priority update — November 21, 2026

Claude and Codex: user added note 16, trusted code signing, as a high-priority public-release requirement. Read the new section at the top of ROADMAP.md and CLAUDE-NEXT-BUILD-HANDOFF-2026-11-21.md before release work. Current roadmap1 assets remain unsigned and unchanged. Documentation update only; no signing setup, build or publication performed.

## Evaluation notes update — November 22, 2026

Claude and Codex: read ROADMAP-EVALUATION-2026-11-22.md before milestone work. It records notes 17–21, troubleshooting evidence, Fox Hunt partial estimates/directional guidance, and heat maps shared with the future Coverage Survey. Current inventory: 20 active items; note 3 rescinded. Documentation only; implementation not started in this update.

## Latest Claude review context — November 22, 2026

Read note 19's Later field clarifications and export review in ROADMAP-EVALUATION-2026-11-22.md. Sleep/wake and missing restart state supersede the unconfirmed GPS-disconnect description. Antenna context, GPX evidence limits and planned KML/KMZ signal styling are documented. Existing notes 1–21 and separate SSTV plans remain intact; no new numbered item or implementation is claimed.

## Saved-hunt analysis and sharing plan — November 22, 2026

Claude and Codex: note 19 now explicitly plans offline saved-run calculations and findings reports, with multi-user sharing/combined localization later. See ROADMAP-EVALUATION-2026-11-22.md for metadata, mixed-equipment grouping, deduplication, privacy and validation requirements. Documentation only; no app/backend changes.

## Field-review handoff — November 22, 2026

Claude: REVIEW-FIELD-2026-11-22.md contains current sensor replay results and requested independent checks, shared backlog/reconnect evidence, note 22 FT8 missed slots and expanded note 19 indicators. Local-only reference index: work/field-review-2026-11-22.md. Current active count: 21. Documentation only; no fixes claimed.

## Note 17 shared DSP fix — November 22, 2026

Codex optimized IqDecimator mixing and FmDemodulator FIRs; 2262 self-checks and receive-modes smoke passed. Read VALIDATION-NOTE17-THROUGHPUT.md and review the code commit. AC-power dropout evidence included. Raw queues retain explicit overflow protection. Reconnect budget and reduced-rate advice are not fixed; field acceptance required. No published package changed.

## Note 8 Aviation target isolation — November 22, 2026

Receiver channel changes now reset Aviation acquisition without overwriting its editable/decoder target. Explicit Aviation Tune updates its active target. See VALIDATION-NOTE8-TARGET.md; 2265 self-checks and tuning/receive UI smoke passed. No public package; field acceptance remains open.

## Note 1 tuning feedback — November 22, 2026

TuneSaved/LiveTune failures and Broadcast staging now report in shared status, and validation precedes scanner interruption. See VALIDATION-NOTE1-TUNING.md; 2,265 self-checks and receive/tuning UI smoke passed. This is an error-path/usability fix, not full field acceptance of every reported Tune failure. No new public package.

## Note 17 recovery follow-up — November 22, 2026

After 30 seconds of continuous live I/Q, the next loss receives a fresh reconnect budget; rapid failures still stop after three attempts. Retry exhaustion is visible in shared status. Reduced-rate ADS-B advice now explains the 1.75 MHz rate limit even with HF filter disabled. See VALIDATION-NOTE17-RECOVERY.md; 2274 checks and UI smoke passed. No new public ZIP; hardware acceptance remains open.

## Note 19 glance indicators — November 22, 2026

Three separate signal/GPS/logging cards with saved size choice, typed GPS state and explicit no-fix/stale/not-recording states are in source. Read VALIDATION-NOTE19-INDICATORS.md and Help/fox-hunt.md. 2291 checks and Fox UI smoke passed; synthetic render inspected. Other note 19 features, field acceptance and release packaging remain open.

## Note 19 gain, overload and elevated-site plan - November 22, 2026

Claude and Codex: before further Fox Hunt estimator, guidance or gain work, read VALIDATION-FOX-KNOWN-SOURCE-2026-11-22.md (blind tower test: about 1.6 km combined error, ring effect near an elevated antenna, overload near the site) and PLAN-NOTE19-FOX-GAIN-AND-ELEVATED-SITES.md (overload indicator, stepped AGC with measured gain steps used in calculations, elevated-site awareness, evidence weighting, guidance that never points at the peak reading). Planning only; no code changed.

## Note 22 prepared TX symbols — November 22, 2026

Message cache and prepared-tone worker transfer avoid re-encoding during RF handoff; first burst uses four seconds while FT8 repeat capture keeps its 1.3-second handoff. Timing diagnostics added. See VALIDATION-NOTE22-PREPARED-TX.md. Combined test-package suite: 2,310 checks and three UI smokes passed; no RF transmitted in tests.

## Note 19 overload awareness — November 22, 2026

ADC input card, near-full-scale/rail evidence, CSV/GPX quality fields, flat/high-input hint and explained estimator exclusion are in source. Legacy evidence remains unknown. See VALIDATION-NOTE19-OVERLOAD.md. Combined test package passed 2,310 self-checks and UI smoke; gain automation, elevated-site modeling and hardware acceptance remain open.

## Current development version — 0.54.0-preview2-test1

User authorized a local portable test package after notes 22/19. Includes all earlier receive/tuning/indicator fixes. UPDATE-0.54.0-preview2-test1.md lists scope and acceptance. This is an unsigned private evaluation build; no GitHub upload or signed-public-release exception inferred. Build inputs are committed; packaging/verification follow. User originals and test profiles excluded.

## Latest portable artifact handoff

Claude: read CLAUDE-BUILD-HANDOFF-0.54.0-preview2-test1.md for artifact SHA-256, exact build commit, notes 22/19 changes, all included fixes, validation and remaining field acceptance. Local unsigned test ZIP has been built and verified; original GitHub release unchanged. Documentation commits after 27856ca do not change that executable.

## Note 23 custom receive frequencies — November 26, 2026

Codex removed ADS-B/UAT exact-frequency locks and preserved custom targets in receiver setup. Read VALIDATION-NOTE23-CUSTOM-RECEIVE.md for Claude review and acceptance limits. 2,317 self-checks, 17 aviation worker fixture cases and full UI smoke passed. Receive only; hardware/sample-rate/coverage requirements remain. No new portable ZIP or publication.

## Note 9 shared offline-map roadmap — November 26, 2026

Claude: read the Note 9 expansion in ROADMAP.md and CLAUDE-NEXT-BUILD-HANDOFF-2026-11-21.md. User requested Aviation Save area for offline use as the starting point, shared saved regions across Aviation/Fox Hunt/future Coverage Survey, region management and provider-permitted downloads. Planning only; no application changes or map downloads.

## Note 24 mesh tuning — November 26, 2026

Codex added Tune receiver in Analysis > Meshtastic / MeshCore using the shared LiveTune action and entered MHz. Read note 24 at the end of ROADMAP.md and the Claude next-build handoff. Release compile passed with zero warnings/errors; whitespace check passed. Live receiver acceptance remains open. No portable ZIP or publication.
