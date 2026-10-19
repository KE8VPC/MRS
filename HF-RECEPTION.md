# HF reception — 0.25.0-preview1

## Start here

Stop reception. Open Receiver > HackRF offset / gain. Enable HF hardware filter (1.75 MHz), select 9.6 MS/s, and use +250 kHz capture offset. Start / Apply. A +1000 kHz offset does not fit this filter and is rejected with guidance. Do not disable offset merely to fit the filter; reducing it to +250 kHz keeps the wanted channel away from the center artifact.

Use USB/LSB and about 2.4 kHz for SSB, AM and about 6 kHz for shortwave broadcast, or CW with 250/500 Hz for Morse listening. Start with RF amplifier off, LNA/VGA around 16/16 dB, audio AGC enabled and RF squelch off while searching. Adjust gains for readability. The update does not automatically change gains, modulation, squelch, offset or sample rate.

## What changed

The optional setting programs HackRF's analog baseband filter to its 1.75 MHz setting independently of the capture sample rate. Previously the filter followed sample rate. Actual nominal filter coverage travels in receiver metadata and I/Q recordings. Tuning/rate changes validate the selected channel against that coverage. The checkbox is changed while stopped and saved with receiver preferences; RF amplifier still defaults off after restoring preferences.

The input indicator samples a complete input block at most ten times per second. RMS and peak are relative to the ADC component full scale, not calibrated RF dBm or SNR. Rail percentage counts unsigned samples at 0/255 after signed HackRF conversion. Orange text indicates possible clipping; reduce gain and compare. No sampled clipping does not rule out analog compression, intermodulation or clipping between measured blocks. Stopped/stale inputs are shown explicitly.

The filter is a baseband filter, not an antenna-side RF preselector. It can reduce surrounding baseband energy before digitization, but cannot undo overload in earlier RF stages. Nominal filter edges have rolloff. High sample rates still consume normal USB/CPU bandwidth. The waterfall may show attenuated spectrum outside usable capture coverage; that region is not valid for selecting a full channel.

Disable HF filtering for wideband uses such as HDTV, or any wanted channel/offset that does not fit. This option can also be used for other narrowband reception but does not identify a band automatically. Older MRS builds may reject recordings made with the new filter setting encoded in the receiver settings token; use this preview or newer for replay.

Noise blanking, notch filtering, adjustable AGC timing and passband shifting remain future work. This preview adds hardware filtering and diagnostics, not a general noise-removal algorithm.

## Validation boundary

Generated checks cover transport, filter/offset limits, worker acknowledgement, recording metadata and input-level calculations. UI checks cover the new control and meter. Real HackRF HF reception improvement requires the user's station comparison; a software fixture cannot measure the physical filter or sensitivity.
