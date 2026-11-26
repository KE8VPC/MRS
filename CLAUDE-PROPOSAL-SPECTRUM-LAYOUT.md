# PROPOSAL (not implemented): Spectrum placement / View options

**Author:** Claude, 2026-11-16 (revised 2026-11-16: WEFAX is under Weather; added the docked-height layout check). **Status:** proposal only. No code was written and nothing was built.
> **Superseded in part by the user's station review (2026-11-16): see `CLAUDE-LAYOUT1-REVIEW-NOTES.md`.** Right-side docking is removed. The main tabs stay fixed, and the dock goes below them.

**Base reviewed:** `outputs/MRS` at **0.53.0-preview1**.
**2026-11-16: the user accepted this revised proposal as the layout reference. The build hold is still in effect.**
**Do not implement until the user authorizes it.** The user's build hold applies: "wait until I tell you to build".
Related: `CLAUDE-UI-HANDOFF.md` (ui1 radio bar / theme preview, still awaiting station review, not merged).

## Problem (user request)
> "Some of the monitoring you would like to see the spectrum as you search and decode, which is currently not an option in some cases."

0.53 has **one** RF `SpectrumView` (`Spectrum`, inside the `SharedSpectrum` grid, MainWindow.xaml ~line 154). It is visible only on:
- the **Spectrum** workspace tab
- **Broadcast → AM / FM**, because `BroadcastWorkspace.cs` `Move()` re-parents `SharedSpectrum` into `AmSpectrumHost` / `FmSpectrumHost`

It is **not visible** while using P25, Commercial (DMR/LTR/NXDN, POCSAG/FLEX, Railroad, trunk following, LCN), Analysis (sensors, CW/messages, cellular survey, HF beacons), Aviation, Amateur (APRS, CW), Fox Hunt or Weather (including WEFAX). FT8/WSPR has its own audio waterfall, which is not RF.

## Decision: View options, not general pop-outs
Two approaches were compared: popping out each decoder panel into its own window, or a View setting that controls **where the one spectrum is placed**. The recommendation is the **View option**, with "Separate window" as one of its placements.

Reasons against general decoder pop-outs:
- `PreviewKeyDown` handlers (F1 Help) live on the main window.
- Four places branch on the selected tab: `BroadcastControls.cs:27`, `BroadcastWorkspace.cs:18`, `HelpWindow.cs:29` `OpenHelp`, `LcnResearchControls.cs:22` `TickLcnResearch`.
- About 40 dialogs use `Owner=this` / `Window.GetWindow`.
- Receiver B/C windows would multiply.
- Many smoke tests capture the main window after `SelectWorkspace(...)`.

The View approach reuses the pattern Codex already ships for AM/FM, which is re-parenting `SharedSpectrum`. It needs no second `SpectrumView` instance and no duplicate frame feed. Markers, birdies, activity levels, click-to-tune and band plans keep working everywhere.

## Proposed behaviour
New **Display tab → "Spectrum placement"** section. A **View** button on the header row opens the same choices.

| Setting | Behaviour |
|---|---|
| **Tab only** | Current behaviour (Spectrum tab, plus AM/FM hosts) |
| **Docked top** (proposed default) | `SharedSpectrum` sits above `WorkspaceTabs` on every workspace, with a horizontal `GridSplitter` |
| **Docked right** | Same idea, to the right of `WorkspaceTabs`, with a vertical splitter, for wide screens |
| **Separate window** | `SharedSpectrum` moves into a non-modal `Window` (Owner = the receiver's MainWindow, title "M.R.S. Spectrum — Receiver A"). Closing it returns the spectrum to "Tab only". Position and size are saved. |

Extra options:
- **Compact docked spectrum**: lower minimum height, shorter waterfall (uses `DisplaySettings.SpectrumFraction` / `HistorySeconds`), and the second toolbar row (snap / view mode / readout) is hidden.
- **Per-workspace override** (optional, phase 2): e.g. docked on P25/Commercial, tab-only on Fox Hunt and Aviation, where maps need the room.
- While docked or in a separate window, the **Spectrum tab** shows a short note ("Spectrum is docked above / in its own window") with a "Bring back here" button. It could also be collapsed.

## Implementation sketch (for whoever builds it)
1. **One placement routine** replaces `BroadcastWorkspace.Move()`'s private logic, e.g. `PlaceSpectrum()` in a new `SpectrumPlacement.cs`.
   - Detaches `SharedSpectrum` from its current parent (`SpectrumTab.Content`, any `Panel` or the pop-out `Window.Content`).
   - Attaches it to the target host, chosen from the placement setting plus the current tab.
   - Precedence: Separate window > Docked > AM/FM host when Broadcast AM/FM is selected > SpectrumTab.
   - Re-run on `WorkspaceTabs.SelectionChanged`, `BroadcastTabs.SelectionChanged` and when the placement setting changes.
   - After a move, keep calling `RefreshBroadcast(); BroadcastViewChanged(...)`, as `Move()` does today.
2. **MainWindow.xaml:** wrap `WorkspaceTabs` (Grid.Column=2) in a new Grid named e.g. `WorkspaceArea`, with an empty `DockTopHost` row, a splitter row, a tabs row, and an optional `DockRightHost` column. Collapse the hosts and splitters when they are not in use. No existing control is renamed or removed.
3. **Settings:** add `SpectrumPlacement` (string or int), `DockedSpectrumHeight` / `DockedSpectrumWidth` and `SpectrumWindowBounds` as **new optional trailing parameters** of `record Preferences` (MainWindow.xaml.cs:28), with defaults, so old `settings.json` files still load. Save per receiver workspace, as the existing settings are.
4. **Pop-out window:**
   - Forward `PreviewKeyDown` to the owner for F1/F5.
   - Its `Closing` handler sets placement back to "Tab only" and re-runs `PlaceSpectrum()` (re-parent; do not dispose).
   - Close it from MainWindow `Closing`.
   - Clamp saved bounds to a visible screen.
5. **Smoke tests:**
   - `Update036Tests.cs:31-32` assert `SharedSpectrum.Parent == AmSpectrumHost/FmSpectrumHost`. Keep that true in "Tab only" mode, or update the assertion to accept the dock host when docked.
   - Smoke runs should force "Tab only" at start, so existing screenshots stay comparable.
   - Add one smoke screenshot per placement (top, right, window).
6. **Help:** a new page `Help/spectrum-placement.md`, linked from the Display help.

## Risks / checks
- Re-parenting a WPF element that has focus or mouse capture during a drag (spectrum split or marker drag). Release capture before moving: `Spectrum.InteractionLocked` exists.
- `SpectrumView` size changes trigger `CaptureSpanChanged` / navigation refresh. Verify the readout and the tuned marker after each move.
- **Main layout check: does Docked top leave enough room?** This is the deciding acceptance item. Check it at the minimum window size (1040×740) and at 1366×768, with the docked spectrum in Compact mode and in normal mode:
  - **P25 → P25 Systems:** the discoveries grid (row `*`, `MinHeight` 100) and the Channel activity / Messages grid (row `1.3*`, `MinHeight` 130). The discoveries grid is already near its minimum without docking. Both grids must still show several readable rows.
  - **P25 → Trunk following / Talkgroups** and **Commercial → DMR/LTR/NXDN**, which have long control stacks above their tables.
  - **Map-heavy workspaces:** Aviation → Aircraft / map, Fox Hunt and Weather (WEFAX image). The map or image must stay usable.
  - If a workspace fails, fall back to one of these, in order:
    1. Compact docked spectrum by default
    2. A docked height cap, e.g. 30% of the workspace area, with a minimum of about 140 px
    3. A per-workspace override that defaults to Docked right or Tab only for that workspace (move the per-workspace override from phase 2 into phase 1 if needed)
  - Capture smoke screenshots of P25 Systems, Aviation map, Fox Hunt and Weather in each placement for the station review.
- Logic gated on `IsVisible` (`MonitoringViews.cs`, `PacketPanel.xaml.cs`, `AviationPanel.cs`) is unaffected; only the spectrum moves.
- Receiver B/C windows each own their own placement and pop-out.

## Relationship to the ui1 preview
`CLAUDE-UI-HANDOFF.md` describes a separate ui1 preview (radio bar, dark theme, status bar, shortcuts), built on 0.51.0-preview2 and not merged. If the user approves both, the suggested order is to:
1. rebase ui1 onto the current version (0.53+)
2. add spectrum placement on top of it
3. test both together in one station pass.

The ui1 radio bar keeps frequency, mode, Start/Stop and squelch visible, and docking keeps the spectrum visible, so the two features complement each other.

## Coordination
- If you (Codex) change `MainWindow.xaml` layout, `BroadcastWorkspace.cs`, `record Preferences` or the `SharedSpectrum` structure before this is built, please note it in ROADMAP.md so this proposal can be adjusted.
- Whoever implements it should add an UPDATE/ACCEPTANCE entry as usual. Live acceptance, especially the separate window on a second monitor, needs the user's station.
