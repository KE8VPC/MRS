# M.R.S. 0.54.0-preview1 (combined update)

One update that combines four sets of changes, for testing together at the station:

- **Weather / WEFAX** (Codex, 2026-11-16): WEFAX moved under the **Weather** workspace, with marine WEFAX channel references. See Help/additional-receive.md.
- **DMR saving fixes** (Codex, 2026-11-16):
  - **Frequencies → Review legacy DMR duplicates…** previews merging duplicate DMR entries for the same carrier. It keeps the decoded record's ID, combines evidence and notes, and removes emptied placeholder folders. Competing decoded identities are kept for review.
  - A later decode **upgrades a uniquely matched provisional entry in place**, keeping its ID, center frequency, custom name and notes.
  - A weak or older decode can no longer replace a stronger one while a frequency editor delays saves.
- **Spectrum layout** (Claude, layout2):
  - The **main tabs are fixed**: one row, always in the same place.
  - The spectrum can be **docked full width under the tabs**, shown on its own tab, or opened in a **separate window**.
  - **Compact** mode keeps the docked spectrum short.
  - The main choice applies to every workspace. An exception can be set per workspace.
  - See Help/spectrum-layout.md.
- **Interface refresh** (Claude, ui1):
  - **Radio bar**: tunable frequency digits, mode, Start/Stop/Rec, volume, squelch, RX/TX indicator.
  - **Dark theme.**
  - **Fixed 3×2 sidebar tabs.**
  - **Live status bar.**
  - **Keyboard shortcuts:** F5, Shift+F5, Ctrl+F, Ctrl+R, PgUp/PgDn, Ctrl+1–9.
  - Readable table headers and hint text.
  - See Help/radio-bar-shortcuts.md.
