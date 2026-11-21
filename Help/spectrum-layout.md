# Spectrum layout: docked under the tabs, or a separate window
Context: Spectrum
Keywords: spectrum layout dock docked under tabs separate window pop out compact waterfall view while decoding P25 DMR paging workspace exception

M.R.S. has one RF spectrum and waterfall. **Spectrum layout** chooses where it appears while another workspace is open, so you can watch the band while you search and decode.

Change it from the **Layout ▾** button beside About, or from the **Spectrum** row at the top of the spectrum.

| Choice | What happens |
|---|---|
| Spectrum tab only | The original layout. The spectrum shows on the Spectrum tab and in Broadcast → AM / FM. |
| Docked under tabs | The spectrum sits full width directly **under the main tabs**, above the open workspace. Drag the divider to resize it. |
| Separate window | The spectrum gets its own window, for example on a second monitor. Close that window, or use **Return to main window**, to bring it back to the Spectrum tab. |

The **main tabs never move**. They stay in one row in the same place whatever layout or workspace you choose. The spectrum always docks below them.

**Compact** (when docked) hides the tuning toolbar and keeps the dock short. Click-to-tune, markers, zoom and band plans still work in the plot. The full toolbar returns on the Spectrum tab. The spectrum needs at least about 175 px of docked height to draw, so the dock cannot be dragged smaller than that.

**The main choice applies to every workspace.** Choosing it again clears any exceptions, so what you see always matches what you picked. To treat one workspace differently, open it and use **"<workspace> only"** (for example "Aviation only: Spectrum tab only"). The label turns amber while an exception is active. **Layout ▾ → Clear exceptions** removes them all.

The Spectrum tab and Broadcast → AM / FM always show the spectrum in their own page. In Separate window mode, the Spectrum tab shows a button to bring it back.

The layout is saved per receiver workspace in `spectrum-layout.json`. Delete that file to return to the defaults (docked under the tabs, compact, no exceptions). Settings from the earlier layout1 preview that chose "Docked right" load as docked under the tabs.
