# User review notes on the spectrum-layout preview "layout1" (2026-11-16)

**Recorded by Claude from the user's station review. Status (2026-11-16 night): the user authorized the spectrum-layout changes. They are built as preview **layout2** in `pr/work/mrs-0530p1-claude-spectrum` (see its `CLAUDE-SPECTRUM-NOTES.md`). The DMR item is Codex's, in the main checkout.**
Preview reviewed: `pr/work/mrs-0530p1-claude-spectrum` (`0.53.0-preview1-layout1`). Background: `CLAUDE-PROPOSAL-SPECTRUM-LAYOUT.md`, and `CLAUDE-SPECTRUM-NOTES.md` in that folder.

## Decisions and findings so far
1. **Choosing "Docked top" still left the spectrum docked right (Amateur).** Cause: a preview per-workspace override ("Amateur uses: Docked right") silently wins over the main setting. The reverse also happened: main "Spectrum tab only" with "Commercial uses: Docked top". **Lesson:** overrides must not silently defeat the main choice. Make them visible, or reset/ask when the main setting changes, or drop the preset overrides.
2. **No spectrum on Aviation.** The preview defaulted Aviation to "Spectrum tab only", because the layout check measured it **with Show aircraft map on**. The user runs with the map off, so the table has plenty of room. **Lesson:** the defaults were too strict. Measure the user's real configuration, and don't hide the spectrum by default.
3. **Dock the spectrum *under* the main tabs.** Docked top currently sits above the main tab row and pushes it down.
4. **The main navigation tabs must stay anchored in one fixed place** (user decision): "I shouldn't have to hunt for the navbar." The tab row must never move, wrap or reorder for any spectrum placement. Sub-tabs are treated as page content unless the user says otherwise.
5. **Remove right-side docking entirely** (user decision). Monitoring needs spectrum **width**, because signals sit at different points across the band. Height and a narrow column don't help. Docked right is also what made the tab row wrap and reorder.
   - **Consequence to resolve:** the layout check found Docked right was the only placement that kept P25 / Commercial / Amateur tables usable at 1040×740 and 1366×768. Top docking (now below the tabs) shortens those tables. The user should choose between compact top docking with shorter tables, or tab-only, per workspace.
6. **DMR discovery organization is a burden.** See `CLAUDE-FINDINGS-DMR-DISCOVERY.md`. Root cause: duplicate entries per carrier block automatic enrichment.

## Resulting direction (pending the user's final notes)
- Spectrum placements: **Spectrum tab only, Docked top (below a fixed main tab row), Separate window.** No Docked right.
- The main tab row is fixed: full width, single row, never reordered.
- Re-run `--spectrum-layout-smoke` with Aviation measured map-off, and with the dock below the tab row.
