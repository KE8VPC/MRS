# NOTE FOR OTHER ASSISTANTS: Claude previews (updated 2026-11-16, 19:10)

> **Superseded in part (November 21, 2026):** the combined UI is now in outputs/MRS (commit 112d3d1); the "do not edit until tested" hold no longer applies. See `ACTIVE-BASE.md`.

> **CURRENT: the combined 0.54.0-preview1 candidate** (section 5 below) supersedes the separate layout1, layout2 and ui1 previews. **Codex: please commit a git checkpoint of `outputs/MRS` now.** The candidate will be promoted over it after the user's station test.

**Claude has not changed any existing file in this folder (`outputs/MRS`).** Its only additions are this note, `CLAUDE-PROPOSAL-SPECTRUM-LAYOUT.md`, `CLAUDE-LAYOUT1-REVIEW-NOTES.md` and `CLAUDE-FINDINGS-DMR-DISCOVERY.md`. No git commits or branch changes were made. Both previews below are in **separate copies** and wait for the user's station review. **Do not merge either one until the user says so.**

## 1. Spectrum-layout preview "layout1" (2026-11-16): user-authorized build
- Copy: `pr/work/mrs-0530p1-claude-spectrum/`. Based on this folder's state on 2026-11-16 14:33 (0.53.0-preview1 plus the uncommitted Weather/WEFAX changes). Version label `0.53.0-preview1-layout1`.
- It implements `CLAUDE-PROPOSAL-SPECTRUM-LAYOUT.md`: Spectrum tab only / Docked top / Docked right / Separate window, compact mode, height cap and per-workspace overrides. It is built in two new files plus five one-line hooks.
- **Read first:** `pr/work/mrs-0530p1-claude-spectrum/CLAUDE-SPECTRUM-NOTES.md`. Patch: `CLAUDE-SPECTRUM-layout1.patch`. Side-by-side layout check: `layout-check/index.html`. Package: `package/`.
- Main finding: top docking leaves too little room for most table workspaces at 1040×740 and 1366×768, while docked right works for them. The preview therefore defaults P25/Commercial/Amateur to docked right and Aviation/Fox Hunt/Weather/Analysis to the Spectrum tab. The notes also list workspaces that were already short **before** docking (the P25 discoveries grid, the Aviation and Fox Hunt maps, Analysis Sensors).
- If you change `MainWindow.xaml` layout, `BroadcastWorkspace.cs` `Move()`, or add top-level workspaces, please note it in ROADMAP.md so the preview can be re-based.

## 2. ui1 refresh preview (2026-11-13): kept separate on purpose
- Copy: `pr/work/mrs-0510p2-claude-ui/` (based on 0.51.0-preview2): radio bar, dark theme, status bar and shortcuts. Notes: `CLAUDE-UI-NOTES.md` there. Packages: `pr/outputs/MRS-0.51.0-preview2-ui1-*`.
- It is **not** included in layout1, so the layout can be judged on its own. If both are approved, merge layout1 first, then rebase ui1.

## 3. Notes added 2026-11-16 evening (read these)
- `CLAUDE-LAYOUT1-REVIEW-NOTES.md`: the user's station review of layout1. Decisions: main tabs fixed in place, **no right docking**, dock below the main tabs. The preset overrides confused the user. The review is still open.
- `CLAUDE-FINDINGS-DMR-DISCOVERY.md`: why DMR discoveries stop updating saved entries. Duplicate entries per carrier make the matcher hold every new decode. Verified read-only against the user's library. Includes ranked fixes and tests. Analysis only, nothing changed.

## 4. layout2 (2026-11-16 night): user-authorized, spectrum layout only
- Same copy: `pr/work/mrs-0530p1-claude-spectrum/`, now `0.53.0-preview1-layout2`. It **supersedes layout1** and implements the decisions in `CLAUDE-LAYOUT1-REVIEW-NOTES.md`:
  - the main tab row is anchored (one row, never moves, checked)
  - the spectrum docks under the tabs, full width
  - no right docking
  - no preset exceptions (a main choice clears them)
  - default compact 180 px
- **Read first:** `CLAUDE-SPECTRUM-NOTES.md` there (layout2 section at the top). Patch: `CLAUDE-SPECTRUM-layout2.patch`. Package: `package/`.
- Includes a correction for you: `SpectrumView` draws nothing below 140 px tall, which made layout1's 140 px compact results invalid. The notes also flag an `App.xaml.cs` smoke-hook pattern (`w.Close()` before `Shutdown(1)`) that can report a failure as exit 0, and the same pattern exists in `--website-sync-smoke`.
- The DMR saving work (duplicate review + in-place provisional upgrade) is yours in this main checkout. layout2 does not touch DMR code. If both touch the same window code, reconcile when the user approves layout2 for merge.

## 5. Combined 0.54.0-preview1 candidate (2026-11-16 evening): user-requested "one big update"
- Folder: `pr/work/mrs-0540p1-combined/`. Built from a snapshot of **this folder at 18:40**, which includes Codex's finished DMR saving work (last edit 18:07), plus layout2 plus ui1 (rebased onto 0.53).
- **Read first:** `CLAUDE-0.54-MERGE-NOTES.md` there. Also `UPDATE-0.54.0-preview1.md`, `ACCEPTANCE-0.54.0-preview1.md`, and `CLAUDE-0.54-combined.patch` (dry-run verified against this folder).
- **Codex's DMR files were not modified.** Small merge edits:
  - `Update043Tests.cs`: the PSK version literal is now 0.54
  - `App.xaml.cs`: the `--website-sync-smoke` exit-code order is fixed
  - the radio-bar slider widths
  - the layout-report wording
- Validation: self-test **2193 PASS / 0 FAIL** (includes the 8 new DMR tests), with the layout check and all 16 existing smoke suites passing.
- **Please don't edit this folder's source further until the user has tested the candidate, or tell the user if you must.** Changes made here now would need re-merging.
