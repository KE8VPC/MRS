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
