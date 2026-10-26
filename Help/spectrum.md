# Spectrum, waterfall and navigation
Context: Spectrum
Keywords: follow tuned center lock free view zoom pan markers click step history

Click the RF plot to tune. Drag to pan. Wheel zooms around the pointer; Shift+wheel pans. Ctrl+click selects a saved marker. Alt+drag corrects a saved marker center.

Follow tuned keeps tuning visible at high zoom. Center lock centers the tuned channel. Free view permits independent panning; Center view brings tuning back. Dragging changes to Free view.

Use Step for frequency increments, Shift to move capture center, Full span to reset zoom. Drag the horizontal spectrum/waterfall divider to change their height. Overlapping history is retained when capture changes; newly exposed frequencies have no older samples. Display gain changes the picture, not hardware gain.

## Click-to-tune snapping
Snap off preserves exact click tuning. Snap: tuning step rounds spectrum and waterfall clicks to the selected step, including 6.25 kHz. Snap: bandplan uses the nearest enabled explicit channel center within eight pixels; otherwise it uses the selected step. The hover tooltip previews the snapped frequency. Bandplan channels handle offsets such as FM broadcast centers. Ctrl marker selection and marker correction are separate operations. Manual entry retains full precision: four decimals in MHz is 100 Hz, six is 1 Hz. Display rounding alone does not correct off-center tuning.
