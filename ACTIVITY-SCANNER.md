# Automatic RF squelch and locked-spectrum activity scanning

Implemented in 0.19.0-preview1 for live RTL-SDR/NESDR and HackRF I/Q. Each receiver has its own **Scan** tab and preferences. Scanning never starts automatically. Conventional-radio audio and I/Q replay do not provide this live tuning workflow.

## Quick start

1. Start a receiver. Choose the receive mode and filter for the signals you expect, set gain, and position/zoom the RF spectrum over the desired range.
2. Open **Scan**. For automatic squelch without scanning, enable **Automatic RF squelch** and leave the scanner stopped. Calibration takes about 1.2 seconds of fresh spectra. The default margin is 8 dB above the estimated channel noise floor.
3. Optionally enable **Show floor / trigger lines**. The horizontal lines are equivalent FFT-bin levels at the listening channel, so they can be compared with the spectrum. The Scan tab separately shows the integrated channel floor and RF gate threshold. Changing display gain does not change RF sensitivity.
4. Press **Start scanner**. This enables automatic RF squelch and the RF audio gate, locks the current visible span, and searches the part of that span that fits inside usable capture coverage. The strongest qualified signal is selected first. The receive mode/filter stay as selected; energy detection does not identify a protocol.
5. The scanner stays on the selected frequency while active, then searches again after the configured quiet delay. **Hold this frequency** prevents automatic hopping. **Skip / ignore** releases it and excludes it for this scan session; **Clear ignored** makes skipped channels eligible again.
6. **Stop** releases the spectrum lock and leaves the receiver on its last listening channel. Automatic squelch remains separately selectable. Clicking a saved frequency, entering a frequency, or other manual tuning stops scanning before applying that action.

## Controls and behavior

- **Above noise floor:** 3–30 dB. Raise it to reject marginal activity. This is relative to the local estimated floor, not calibrated dBm.
- **Minimum activity:** 0.15–3 seconds. Both the current spectrum and smoothed power must indicate sustained activity; isolated impulses do not qualify just because the display decays slowly.
- **Resume after quiet:** 0.3–10 seconds. A 3 dB release hysteresis also reduces chatter. Brief dropouts can be counted as separate activity bursts even while the scanner remains on the same channel.
- **Channel grid:** estimated center by default. Optional 1/2.5/5/6.25/12.5/25/50/100/200 kHz spacing is independent of receive-filter width. The grid offset defaults to 0 Hz; enter an offset in kHz and press Enter or leave the field to apply. For example, 12.5 kHz spacing with a 6.25 kHz offset includes 772.08125 MHz. Only nearby estimates snap to a grid point; a mismatched grid retains the measured center rather than pulling it onto a signal edge. This is a safeguard, not automatic channel-plan identification. SSB dial frequency is inferred from the selected sideband filter and may need manual refinement. Existing incorrectly saved entries are not automatically moved or deleted.
- **Manual override:** uncheck Automatic RF squelch, or move the Receiver tab's RF threshold slider. Detection then uses that integrated RF threshold, with a minimum spectral contrast guard to reject noise. Unchecking Enable RF squelch bypasses audio muting; it does not disable activity analysis.
- **Recalibrate noise floor:** stops the scanner, discards the previous floor estimate, and requires Start to resume. Capture frequency, gain, rate, FFT size, source, mode/filter changes and RF data interruptions also stop/recalibrate. Hold and ignored channels are session-only; control preferences save per receiver on normal close.
- **Locked coverage:** drag/pan/zoom changes are ignored while scanning. Manual capture-shift/tuning controls stop the scanner. Digital channel changes use SetChannel, so HackRF offset mode cannot recenter the hardware for each discovered signal. The existing configured offset is retained for subsequent manual tuning.

## Discovered signals

With **Save discoveries to Frequencies** enabled, all qualified channels are recorded, including channels found while listening to another. This does not decode all channels simultaneously.

New entries go into **Discovered signals / YYYY-MM-DD**, using the PC-local date of the latest observation. Folders are created only when needed. Repeated observations update the same day's entry; detections on another day can create a new dated entry. Existing undated/manual nearby saved frequencies still update in place without moving or duplicating them. Names, frequencies, mode/filter, notes, protocols, colors and visibility remain yours. Existing discoveries are not automatically reorganized. Newly created entries use the selected receive mode/filter but explicitly label modulation and protocol **Unknown**.

In Frequencies, Ctrl-click toggles selection; Shift-click or Shift-Up/Down extends a visible range. Ctrl-Up/Down moves focus without changing the selection; Ctrl-Space toggles the focused entry. Delete or the Delete button removes all selected entries, including children of selected folders, in one operation. Undo restores the batch until library reload or closing; scanner saves can reload the shared library, so stop discovery saving before a cleanup when you need Undo to remain available. Search changes clear selection. Ordinary single clicks still tune; modified selection clicks do not.

The entry details show first/last seen UTC, activity-burst count, estimated occupied bandwidth, strength above floor and last observing receiver. Repeat spectra update metadata rather than create rows or count new bursts. Updates are batched about every five seconds and flushed on Stop/Skip. Writes wait while a frequency editor is open. Both windows share the resulting library and markers. Deleting a signal while it is still active allows it to be rediscovered.

The detector retains at most 512 tracked candidates and considers at most 128 distinct channels per frame. The library permits up to 1,000 entries with discovery metadata. Save errors are shown in Scan and Session log; pending changes remain in memory for retry during the current session. Correct the file/limit problem before closing if those updates matter. Background metadata writes do not grow the Undo history.

## Measurement limits and validation

The estimator uses local frequency regions, a robust noise percentile and slow upward adaptation. It excludes capture edges and a center-artifact guard of at least ±2 kHz (or three FFT bins). Channels whose receive filter overlaps that guard are deliberately skipped; tune the capture off-center before scanning if needed. Four FFT bins across the receive filter are required. Increase RF spectrum detail or reduce sample rate when resolution is insufficient.

The noise estimate assumes enough unoccupied spectrum remains in each region. Strong interference, dense occupancy, gain changes, analog filter roll-off and receiver overload can affect it. Center and bandwidth are energy estimates, not verified channel assignments. A manually reviewed saved-frequency list remains useful for exact tuning.

Local tests exercise independent noise/signal spectra, raw noisy I/Q at 4K/32K FFT sizes against the production demodulator, transient/edge/DC rejection, hold/skip/resume, persistence, shared metadata and viewport locking. The WPF smoke uses a synthetic HackRF worker and offset configuration to check digital channel changes without moving capture or replacing the worker. Live RF acceptance remains for operator testing; the tests do not certify reception under every antenna/noise condition.

## Scan limits and discovery-only operation (source update)
The Scan tab accepts a listening frequency in MHz with Enter or Tune frequency; this is manual tuning and stops a running scan through the normal receiver tuning checks. Lower and upper scan limits are separate. Blank limits use the visible usable capture edges. Apply scan limits or Start commits both entries; invalid, reversed, too-narrow or out-of-capture ranges are rejected. Channel filters must fit inside the chosen range. Changing committed limits, grid spacing/offset or discovery-only mode stops scanning and requires Start again.

Discover only keeps the listening channel fixed while detecting across the configured capture range. Enable Save discoveries to Frequencies for unattended collection. Hold and Skip are disabled in this mode. It does not decode every detected channel simultaneously or sweep outside capture. Start still enables automatic RF squelch. Threshold, timing and grid adjustments are grouped under a collapsible section to keep the main Scan controls accessible.

## Known birdies (source update)
Scan → Manage known birdies opens a local list with Add, Update selected and Remove selected. Fixed entries use MHz; capture-relative entries use kHz offset from the hardware capture center (0 marks the center spike). Width is the full reference/exclusion width in kHz. Save commits the list; Cancel leaves it unchanged. Names explain the operator's identification; no decoded data alone is not evidence of a birdie.

Show birdie reference markers displays outlined bands without removing raw spectrum/waterfall data. Exclude known birdies from discoveries prevents overlapping detected signal regions from becoming scanner candidates or new saved discoveries. The two toggles are independent. List/exclusion changes stop scanning and require Start again. Entries persist per receiver workspace, not per physical device; review them after changing hardware. Existing saved entries are not deleted. The scanner's built-in capture-center safety guard still applies even with custom exclusions disabled. Up to 128 entries, widths 0.1–1000 kHz.

## Visually correcting a saved marker (source update)
Stop scanning, hold Alt and drag a single saved marker line. A dashed preview shows old/new frequencies; release inside the plot or waterfall to save, or press Escape to cancel. Release outside valid plotting space, loss of mouse capture, receiver capture changes or resizing cancel the gesture. Ordinary dragging still pans and ordinary clicking still tunes. Markers within six screen pixels of one another cannot be dragged ambiguously; zoom in or select the exact entry in Frequencies and use Use tuned frequency instead.

Correction preserves the entry ID, custom name, folder, notes and activity history; generated Signal-frequency names follow the new center. The Frequencies tab opens after saving and its Undo restores the prior library state until reload/closing. Background discovery updates never overwrite a saved center; if the new estimate lies outside deduplication tolerance it can still create another entry. No automatic snapping or receiver retuning is performed by marker correction.
