# M.R.S. 0.20.0-preview1 acceptance checks

Extract the entire Windows ZIP into a new folder. Close the previous M.R.S. and run the new MRS.exe with its Decoders and Receivers folders beside it. The title should show 0.20.0-preview1. Existing frequency lists/settings stay in your profile.

## Paging

- In Data, enable POCSAG / FLEX. Expand decoder status/help and confirm there is no missing-worker error.
- For a hardware-independent check, replay Docs/Samples/synthetic-pocsag1200.wav using audio/WAV playback. Expect capcode 1234567 and MRS POCSAG TEST 1200. Test the 512 and 2400 samples too.
- Replay synthetic-flex.wav. Expect MRS FLEX TEST, a complete FLEX message and a passing checksum.
- Replay synthetic-pocsag-inverted-numeric.wav. Expect 9876543210. Trailing-silence address-only rows must not flood the default validated view.
- Replay synthetic-noise.wav: no confirmed page should appear.
- Live or I/Q: select Narrow FM, center the signal, try 16 kHz then 20/25 kHz filters, and enable paging. Compare recovery and checksum counts. Listening squelch/volume should not cut decoder input.
- Select a row and confirm rate, capcode/function or phase/frame, text, source/replay status and validation are readable. Show unvalidated exposes partial/failed results. Export and check selected filters are respected.
- Retune, disable/re-enable, seek/stop/restart replay. Old partial messages should not be combined with a new source. If testing both SDRs, enable paging independently in A/B and confirm source labels.

## Scanner and frequency management included since 0.19.0

- Hover sidebar sliders and use the mouse wheel for fine adjustment; Shift-wheel is finer where applicable. The sidebar must not scroll while the slider consumes the wheel.
- Drag the sidebar divider, close/reopen, and check the width persists and labels remain readable.
- Discover a signal between a zero-origin 12.5 kHz grid's points. Confirm measured centers no longer snap to opposite edges; use grid offset where the channel plan requires it.
- Scan can restrict low/high frequencies, tune a specified frequency, and save discoveries without automatically following them. Advanced timing/grid controls are under the scan expander.
- Automatic saves go into folders by local detection date. User-created entries keep their existing folders/notes.
- Ctrl/Shift-select multiple frequency entries and Delete, then Undo. Pause automatic discovery saving during cleanup if retaining the undo history is important.
- Correct a selected saved entry with Use tuned frequency; also try Alt-dragging its spectrum marker. Escape cancels; ordinary click still tunes. Scanner locking prevents marker moves while active.
- Add a known birdie or capture-relative spur and test its independent display and scanner-exclusion settings. The capture-center guard still applies.
- The TV pilot overlay is removed; TV channel boundaries/centers remain.

Live RF and sustained multi-receiver operation are the remaining acceptance checks. Local/generated tests do not establish reception quality at your antenna.
