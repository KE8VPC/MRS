# Station checks: 0.54.0-preview1 (combined update)

Back up `%LOCALAPPDATA%\MRS` before testing. Extract the ZIP into a new folder. Settings, frequencies and layouts carry over.

## DMR saving (Codex)
- Frequencies → select the DMR folder → **Review legacy DMR duplicates…**. Check the preview against the known duplicate carriers, for example 451.925, 461.800, 452.300 and 462.300 MHz. For each:
  - the decoded Network/Site entry is kept with its ID
  - names and notes are combined
  - empty "Unassigned at …" folders are removed
  - entries with different decoded network/site values stay separate for review

  Apply. Then confirm Undo / Rollback restores the list.
- After the cleanup, monitor a carrier first heard as provisional. When its network/site decodes, confirm the **same** entry is upgraded and moved: no second entry, and the Review queue does not grow.
- Open a frequency editor while decoding is running. Confirm that a later weak decode does not overwrite a stronger saved identity after you close the editor.

## Spectrum layout (layout2)
- Switch between workspaces with the spectrum **docked under tabs**, **Spectrum tab only** and **Separate window**. The main tab row must **never move, wrap or reorder**.
- **Compact** on/off: the docked spectrum still draws, and click-to-tune and markers work in the docked plot.
- Where a page feels too tight while docked (expected on P25 Systems, Trunk following, POCSAG/FLEX, APRS, Aviation, Fox Hunt and WEFAX at small window sizes), set "**<workspace> only: Spectrum tab only**". Confirm the label turns amber. Then pick a main layout again and confirm the exceptions clear.
- Separate window: move it to a second monitor, close it, and confirm the spectrum returns to the Spectrum tab. Reopen it and confirm it remembers its position.
- Drag the docked spectrum's divider, restart, and confirm the height is kept.

## Interface refresh (ui1)
- Radio bar:
  - mouse wheel over a digit tunes live
  - double-click or Ctrl+F types a frequency
  - mode, Start/Apply, Stop and Rec match the Receiver/Audio tabs
  - Vol and Sql move the same settings
  - the RX/TX indicator turns red on transmit (TX test)
- Shortcuts: F5 Start, Shift+F5 Stop, Ctrl+R record, PgUp/PgDn step, Ctrl+1–9 workspaces, F1 Help.
- Status bar: source, RF gate, level, recording (red) and the last receiver message update while running.
- Dark theme: buttons and tables are readable everywhere, including dialogs. Tables that colour rows by status (FT8, website sync) keep their colours.

## Already-known limits
- At 1040×740 and 1366×768, docking under the tabs leaves most table pages below 4 visible rows (see layout-check). Use per-workspace exceptions, a larger window, or the separate window.
- Native scrollbars, check boxes and expander arrows still use the light Windows style.
