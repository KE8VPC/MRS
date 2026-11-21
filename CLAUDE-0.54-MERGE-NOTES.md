# 0.54.0-preview1 combined update: merge record (Claude, 2026-11-16)

**Read this first if you are another assistant (e.g. Codex) working on M.R.S.**

The user asked for **one combined update** with all pending work, to test everything at once.

| | |
|---|---|
| Candidate folder | `pr/work/mrs-0540p1-combined/` |
| Base | Snapshot of `outputs/MRS` taken 2026-11-16 18:40. Codex's last source edit was 18:07. The snapshot contains 0.53.0-preview1, the Weather/WEFAX changes and Codex's finished DMR saving work. |
| Untouched base copy | `pr/work/mrs-0540p1-combined-baseline/` (`src`, `Help`) |
| Patch | `CLAUDE-0.54-combined.patch`: everything in this folder that differs from the base |
| Main tree | **`outputs/MRS` was not changed.** It is the rollback point. **Codex: please commit a git checkpoint of `outputs/MRS` before this is promoted.** Claude does not run git in that tree. |
| Package | `package/MRS-0.54.0-preview1-win-x64.zip`, plus the source zip and SHA256SUMS |
| Status | **Waiting for the user's station test.** Promote into `outputs/MRS` only when the user approves. |

## What is merged
1. **Codex DMR saving.** Taken as-is from the snapshot:
   - `DmrDuplicateReview.cs` (new; "Review legacy DMR duplicates…" preview)
   - `DmrSavingTests.cs` (new, called from `Update048Tests.cs`)
   - `DiscoveryMatching.cs`, `SiteChannelSaving.cs` (`QueueForSave` keeps the stronger decode while writes are deferred), `DiscoveryReview.cs`, `FrequencyOrganization.cs`, `FrequencySharingControls.cs`, `SiteSavingControls.cs`

   Claude did not change any of these files.
2. **Claude layout2** (spectrum docked under fixed main tabs). `CLAUDE-SPECTRUM-layout2.patch` applied cleanly (dry-run verified, then applied). Details: `pr/work/mrs-0530p1-claude-spectrum/CLAUDE-SPECTRUM-NOTES.md`.
3. **Claude ui1** (radio bar, dark theme, 3×2 sidebar tabs, status bar, shortcuts, table and hint polish). Rebased from 0.51.0-preview2:
   - All of its hook points were unchanged in 0.53 (`App.xaml` byte-identical to 0.51.0-preview2; the header StackPanel, footer Grid/TextBlock, `RxTxPanel` and every control it mirrors still exist).
   - Files copied: `ClaudeUi.cs`, `Themes/ClaudeTheme.xaml`, `Help/radio-bar-shortcuts.md`, and ui1's `App.xaml`.
   - Hooks re-applied: `HeaderPanel`, `FooterGrid`, `FooterStatus`, and `InitializeClaudeUi()` called right after `InitializeSpectrumPlacement()`.

   Details: `pr/work/mrs-0510p2-claude-ui/CLAUDE-UI-NOTES.md`.

## Changes made during the merge
- **Version 0.54.0-preview1** in `MRS.csproj` and the header text.
- **`Update043Tests.cs`:** the PSK self-test expects the literal `"M.R.S. 0.53.0-preview1"`. Updated to `"M.R.S. 0.54.0-preview1"`, following the existing per-release convention.
  - Note: layout1/layout2 passed only because `0.53.0-preview1-layoutN` contains that text.
  - Consider comparing against the assembly version instead.
- **`ClaudeUi.cs`:** the radio-bar Vol/Sql sliders were narrowed (120/110 → 90 px). At 1040 px wide they clipped the "Vol" label once the window also held the docked-spectrum layout.
- **`SpectrumLayoutSmoke.cs`:** fixed the report-wording bug noted for layout2. A workspace passing at one size is now labelled with that size.
- **`App.xaml.cs`, `--website-sync-smoke`:** swapped `w.Close(); Shutdown(1);` to `Shutdown(1); w.Close();`. With the old order a failing website-sync smoke exited 0. `--spectrum-layout-smoke` already used the new order.
- **Docs:** `UPDATE-0.54.0-preview1.md` and `ACCEPTANCE-0.54.0-preview1.md` (one station checklist for all four parts), plus a top entry in README.md and ROADMAP.md.

## How the parts interact (checked)
- **Main tab row:** ui1's dark TabItem template plus layout2's single-row header still fit at 1040×740. The row sits in the same place in all 96 layout-check screenshots, at (352, 105) including ui1's radio bar.
- **Vertical space:** ui1's radio bar takes about 50 px. With it, DMR / LTR / NXDN passes docked only at 1366×768. Without it (layout2 alone) it passed at both sizes. Layout-check results are in `layout-check/`.
- **Header:** ui1's "?" button and layout2's "Layout ▾" button both sit in the header next to About.
- **Theme:** the per-workspace layout combos use ui1's dark ComboBox style.

## Validation
`claude-smoke-summary.txt` has the full results. In summary:
- Release publish via `build.ps1` with the portable SDK at `pr/work/tooling/dotnet` (10.0.401).
- `--self-test`: **2193 PASS, 0 FAIL**. That is 2185 plus Codex's 8 new DMR saving tests, all passing.
- `--spectrum-layout-smoke` plus all 16 existing smoke suites.
- **Not tested:** live RF, real monitors and DPI, mouse dragging, and the DMR duplicate review on the user's real library. Those are in ACCEPTANCE-0.54.0-preview1.md.

## Promoting (only after the user approves)
1. Codex commits a checkpoint of `outputs/MRS` (git), if not already done.
2. Copy this folder's `src/`, `Help/` and the new or updated docs over `outputs/MRS`, or apply `CLAUDE-0.54-combined.patch` to that checkpoint. Exclude `build/`, `package/` and `layout-check/`.
3. Rebuild and run `--self-test` plus all smoke suites, then package as usual.
