# ATSC field recording analysis — October 8, 2026

Source: `hd tv MRS-IQ-A-20261008-023212.mrsiq`. Original file read only and preserved. Analysis outputs and experimental workers are in `work/tv-field-test`; production decoder code was not changed.

## Capture integrity

- 195,455,137-byte MRS I/Q v3 recording, valid completion marker.
- 5,953 frames; 97,533,952 complex unsigned 8-bit samples.
- 9.6 MS/s; 10.1598 seconds of samples and 10.1537 seconds between first/last frame timestamps.
- Fixed capture center 479.500 MHz, listening center 479.000 MHz, 9 MHz usable bandwidth.
- Recorded gain: LNA 32 dB, VGA 32 dB, RF amplifier enabled.
- Largest host frame-timestamp interval: 10.8712 ms. No large recorded gaps; timestamps alone cannot rule out all USB sample loss.
- Sample extrema 6 and 246; no samples at unsigned digital rails 0 or 255. This rules out visible ADC rail clipping in this file, not every form of analog distortion.
- Mean centered I/Q: -2.933 / -2.858 counts. RMS including DC: 21.291 / 21.294 counts.

## Spectrum findings

Averaged 8,192-point windowed spectra show a strong capture-center/DC component at 479.500 MHz. The nonzero sample mean independently confirms this component. Offset tuning positions this artifact relative to the selected TV channel; the 500 kHz offset leaves it inside the 6 MHz channel.

A separate narrow peak is near 479.998 MHz. Its origin is undetermined. A pilot-like peak near 476.309 MHz agrees with the expected lower-edge pilot location for RF channel 15, supporting the recorded tuning rather than an obvious wrong-channel-center diagnosis.

The broad channel has repeated substantial depressions, including near 479.0, 479.8, 480.7 and 481.5 MHz. This is consistent with frequency-selective distortion/multipath, but does not establish its physical cause. The displayed spectrum is not a calibrated MER or channel SNR measurement.

## Controlled decoding comparisons

All tests used the same raw sample sequence extracted without conversion from the recording. None changed the physical receiver or original recording.

| Test | Transport packets reaching RS decoder |
| --- | ---: |
| Current decoder, -500 kHz translation | 0 |
| DC blocker before channel translation | 0 |
| DC blocker plus 2.0 receive-filter samples/symbol instead of 1.5 | 0 |
| DC blocker plus wider 3.1 MHz prefilter cutoff | 0 |
| DC blocker with translation shifted -2 kHz | 0 |
| DC blocker with translation shifted +2 kHz | 0 |

Zero packets means these runs did not acquire enough synchronization to deliver packets to the error-correction stage. It is not a measured 100% packet rejection rate. These results apply to this ten-second excerpt, not every period of the user's reception.

As a control, the unchanged decoder and runtime recovered 57,227 accepted packets from the existing generated ATSC fixture (64,392 total, 7,165 rejected during acquisition). This confirms the analysis path was operational; it does not prove real-channel equalization is adequate.

## Conclusion / next diagnostic

Capture-center DC is real, but removing it alone did not restore decoding. No tested change demonstrated a benefit, so none was promoted to the application. The excerpt cannot distinguish an inadequate received waveform from shortcomings of the current synchronization/equalization implementation.

A useful future comparison is a capture with the receiver's center outside the TV channel: at 19.2 or 20 MS/s, a +3.5 MHz capture offset fits the reported 15 MHz usable bandwidth for this channel and puts DC 500 kHz beyond the TV channel's upper edge. This has not been tested on the user's receiver and increases USB/processing load; it is an experiment, not a verified fix. Preserve the current working gain settings for a controlled comparison. Raw I/Q allows offline decoding without live-player performance confounding the test.
