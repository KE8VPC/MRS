# M.R.S. 0.18.0-preview3 operator checks

Extract the entire Windows ZIP into a new folder, then run MRS.exe. Existing profiles remain in LocalAppData/MRS. This update retains the working TV decoder installation; it does not require another TV setup.

## Saved-frequency click tuning

1. Start a receiver and open Frequency Manager. Click a saved signal once. Confirm its frequency, supported mode and saved filter width are recalled.
2. Tune elsewhere and click the same entry again. Confirm it tunes back. Repeat in receiver B; receiver A should stay unchanged.
3. Click a folder, expand/collapse it, and drag a saved entry into it. Confirm organization does not retune. A library update should appear in both receiver windows without retuning the other window.
4. Use keyboard selection followed by Tune. This existing path remains available. Unsupported saved modes or frequencies should report the reason instead of silently substituting settings. I/Q replay keeps its recorded tuning constraints.

## TV channel reference overlay

1. In Display > U.S. band reference, enable Show TV RF channels (2–36). Enable Show ATSC 1.0 pilot references (TV layer) if wanted. Both options start disabled.
2. View 479 MHz with a wide enough capture. Confirm RF 15 spans 476–482 MHz, its center marker is 479 MHz, and its nominal pilot reference is 476.309441 MHz. Hover over the bar for exact details and the source.
3. Pan/zoom, change dark/light theme and choose Below spectrum. Confirm references remain aligned and readable. Other reference layers may stay off. Very short windows hide reference rows to preserve spectrum/waterfall room.
4. Save display preferences, close and reopen. Confirm the two switches and placement are retained for that receiver.

These are physical RF channel references, not virtual program numbers or detection of an active TV station. Pilot references describe ATSC 1.0 only. The HDTV decoder itself is unchanged.

Local automated tests cover channel endpoints/gaps, preference controls and reload, reference hover, compact rendering, saved-click event routing and receiver-panel isolation. The live hardware checks above remain for operator acceptance.
