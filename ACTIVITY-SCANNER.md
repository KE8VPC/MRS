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
- **Channel grid:** estimated center by default. Optional 1/2.5/5/6.25/12.5/25/50/100/200 kHz rounding uses a grid referenced to 0 Hz; choose spacing appropriate to the band. No automatic band-plan modulation selection is implied. SSB dial frequency is inferred from the selected sideband filter and may need manual refinement.
- **Manual override:** uncheck Automatic RF squelch, or move the Receiver tab's RF threshold slider. Detection then uses that integrated RF threshold, with a minimum spectral contrast guard to reject noise. Unchecking Enable RF squelch bypasses audio muting; it does not disable activity analysis.
- **Recalibrate noise floor:** stops the scanner, discards the previous floor estimate, and requires Start to resume. Capture frequency, gain, rate, FFT size, source, mode/filter changes and RF data interruptions also stop/recalibrate. Hold and ignored channels are session-only; control preferences save per receiver on normal close.
- **Locked coverage:** drag/pan/zoom changes are ignored while scanning. Manual capture-shift/tuning controls stop the scanner. Digital channel changes use SetChannel, so HackRF offset mode cannot recenter the hardware for each discovered signal. The existing configured offset is retained for subsequent manual tuning.

## Discovered signals

With **Save discoveries to Frequencies** enabled, all qualified channels are recorded, including channels found while listening to another. This does not decode all channels simultaneously.

New entries go into **Discovered signals**, created only after the first qualifying new signal. An existing nearby saved frequency is updated instead of duplicated, regardless of folder. Its name, frequency, mode/filter, notes, protocol, color, visibility and folder remain yours. Newly created entries use the selected receive mode/filter but explicitly label modulation and protocol **Unknown**.

The entry details show first/last seen UTC, activity-burst count, estimated occupied bandwidth, strength above floor and last observing receiver. Repeat spectra update metadata rather than create rows or count new bursts. Updates are batched about every five seconds and flushed on Stop/Skip. Writes wait while a frequency editor is open. Both windows share the resulting library and markers. Deleting a signal while it is still active allows it to be rediscovered.

The detector retains at most 512 tracked candidates and considers at most 128 distinct channels per frame. The library permits up to 1,000 entries with discovery metadata. Save errors are shown in Scan and Session log; pending changes remain in memory for retry during the current session. Correct the file/limit problem before closing if those updates matter. Background metadata writes do not grow the Undo history.

## Measurement limits and validation

The estimator uses local frequency regions, a robust noise percentile and slow upward adaptation. It excludes capture edges and a center-artifact guard of at least ±2 kHz (or three FFT bins). Channels whose receive filter overlaps that guard are deliberately skipped; tune the capture off-center before scanning if needed. Four FFT bins across the receive filter are required. Increase RF spectrum detail or reduce sample rate when resolution is insufficient.

The noise estimate assumes enough unoccupied spectrum remains in each region. Strong interference, dense occupancy, gain changes, analog filter roll-off and receiver overload can affect it. Center and bandwidth are energy estimates, not verified channel assignments. A manually reviewed saved-frequency list remains useful for exact tuning.

Local tests exercise independent noise/signal spectra, raw noisy I/Q at 4K/32K FFT sizes against the production demodulator, transient/edge/DC rejection, hold/skip/resume, persistence, shared metadata and viewport locking. The WPF smoke uses a synthetic HackRF worker and offset configuration to check digital channel changes without moving capture or replacing the worker. Live RF acceptance remains for operator testing; the tests do not certify reception under every antenna/noise condition.
