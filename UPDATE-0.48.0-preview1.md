# M.R.S. 0.48.0-preview1

Discovery research, duplicate prevention and separate P25/Commercial following for Windows x64.

Extract the entire ZIP into a fresh folder and run MRS.exe. Keep all support folders. Existing operational lists are not bulk-cleaned or renamed automatically. Start with Frequencies > Import / export > Library backup.

- Primary P25 and Commercial workspaces replace Public Safety / Business. Each has separate trunk/voice profiles, filters, histories, talkgroup views and assignment state. Separate family settings migrate from legacy mixed profiles without overwriting them. One physical voice SDR cannot be leased by both families at once.
- Auto-save enriches unique compatible saved entries rather than adding entries for every tuning offset/classification update. Custom labels/notes stay intact. Renamed system/site folders retain identity bindings; older renamed folders can be recognized from descendant scope. Ambiguous/conflicting or larger nearby offsets are held for explicit review.
- Frequencies > Research: update existing only prevents new site entries while revisiting saved signals. Review discoveries lets you enrich a compatible entry or intentionally keep a separate channel. The pending queue/research policy persist locally. Existing duplicates still require review rather than automatic deletion.
- Right-click saved spectrum/waterfall markers > Saved entries here, or Ctrl-click overlapping markers, to choose the exact saved record to inspect/edit/tune/hide. Hover includes exact frequency, folder path and identity information.
- Center lock resists drag, Shift-wheel and arrow panning; choose Free view explicitly to unlock. Zoom, full span and tuning preserve the setting. Capture limits and scan interaction constraints still apply.
- Snap: 500 Hz is the click-to-tune default (fourth decimal 0/5). Fine/off, tuning-step and bandplan options remain; exact stored/text-entered frequencies are preserved.
- Right-click a DMR site folder > Research LCN map to collect timed control-grant/voice-LC associations across receivers. Candidates can be exported, copied unconfirmed into the DMR map or labeled LCN ? in the saved list. Correlation is not definitive mapping; verify it independently before enabling confirmed following.

See Help > Discovery matching and Help > P25 and Commercial protocol flows, plus ACCEPTANCE-0.48.0-preview1.md. No live RF/GPS operation, transmission or reception reporting was performed to produce this preview. Software/synthetic/package checks do not close station acceptance. Git publishing is not part of this build request.
