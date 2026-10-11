# M.R.S. 0.8.0 display controls

Use the Display sidebar tab while receiving RF, capturing radio audio or replaying a WAV. Scroll down for themes and presets.

## Gain and contrast

Display gain shifts plotted levels by -40 to +40 dB. It does not change receiver tuner gain, audio level or recorded samples. Spectrum top/range and waterfall floor/ceiling provide separate manual scales; their Auto checkboxes select adaptive levels instead. Waterfall ceiling stays at least 5 dB above its floor. RF levels are relative, not calibrated dBm.

## Zoom, view bandwidth and tuning

The View bandwidth selector chooses the visible span directly, with Full span and widths supported by the current capture and 1–32× zoom. Wheel zoom updates it to a matching selection or Custom. A display preset saves zoom; its absolute bandwidth follows the active capture rate. Acquisition sample rate is selected separately under Receiver.

Left-click tunes a software channel within the available captured span when its complete filter fits. The capture center and waterfall history stay fixed for channel-only changes. Tuning outside capture moves the hardware center and resets frequency history. Ctrl+click selects a saved marker; right-click saves. Dragging pans and suppresses click-to-tune on release. Gain/rate selections and mode/filter controls apply live with I/Q metadata acknowledgements; device changes still reopen the source. Shift −/+ explicitly moves capture center. Recording preserves successful control changes; native transition gaps are omitted from WAV sample time.

Zoom from 1× to 32× using the slider or mouse wheel over the plot. Wheel zoom keeps the frequency under the pointer fixed where capture boundaries permit. Shift+wheel pans. Tab-focus the plot, then use +/- for centered zoom, arrows for pan and Home for full span. Center on tuned RF frequency centers the view on the selected channel; in audio mode it centers the audio span.

Ticks, hover labels, right-click Save and saved-frequency markers follow the visible span. Zoom does not retune, change channel bandwidth or improve FFT resolution. Existing waterfall rows are reprojected when zoom or pan changes.

## Speed and smoothing

Visible history controls how many seconds fit vertically: 5 seconds scrolls fastest; 120 seconds scrolls slowest. The time scale shows age in seconds, with newest data at the top. Up to 125 seconds of timestamped analysis history are retained in memory; unavailable frames appear as gaps. A source change or hardware retune clears history to avoid mixing frequency mappings. History is not saved across application restarts.

The 2–30 fps refresh setting limits normal display redraws; actual updates also depend on incoming data and PC performance. It applies to both spectrum and waterfall. Averaging (0–5 seconds) smooths the spectrum trace; zero disables smoothing. Peak hold retains maxima; Clear held peak resets them. These settings leave acquisition, playback and recording timing unchanged.

## Themes and saved presets

Plot themes: Dark, Light, High contrast and Amber. They change the spectrum/waterfall area; application controls retain the dark interface. Waterfall palettes: Blue/cyan, Grayscale, Heatmap and High contrast.

Expand Custom colors to enter #RRGGBB values for background, grid, text, trace, held peak, cursor, tuned-frequency line and saved markers. Disable Use each saved entry's marker color to apply the common marker color. Invalid color text is highlighted and restored to the last valid value when focus leaves the field.

Theme selection changes colors and palette while preserving zoom, speed and gain. A named preset stores the complete display configuration. Enter a name and Save as named preset; saving the same name replaces that preset. Select a preset and Load selected preset to restore it. Reset display defaults restores the default view without deleting named presets.

Preferences save automatically to %LOCALAPPDATA%\MRS\display.json, with the previous saved file in display.json.bak. If the settings file cannot be read, it is preserved and changes remain preview-only for that session. Close M.R.S. before manually restoring a backup.

