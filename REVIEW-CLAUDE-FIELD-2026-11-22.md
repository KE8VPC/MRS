# Claude cross-check - field reception and decoder evidence

November 22, 2026. Answers `REVIEW-FIELD-2026-11-22.md`. Documentation only: no application code, build, signing or release change. The analysis scripts are local, in `work/claude-field-review/` (outside the repository). They read Codex's derived `capture.cu8` copies and the existing fixtures, and they print only counts, frequencies and model names, never IDs or readings. The original recordings were not opened for writing.

## Sensor/burst investigation

### bursts6 (433 MHz): the recording contains no usable signal
- The samples span only 121-129 of 0-255 (RMS 2.6 counts), and nothing is clipped.
- Over 18.5 s, the strongest 1 ms window is 2.8 dB above the median floor. No window reaches +6 dB.
- Zero decodes is therefore fully explained by the input. It says nothing about the bridge or the decoder. The recording level was far too low (gain, antenna or input), or the activity on screen happened outside the recorded window.
- Next step: raise the gain until bursts clearly exceed the floor on the level meter, then record.

### 911bursts: strong, real, frequency-hopping FSK that rtl_433 does not recognize
- There are 175 bursts above +6 dB (111 above +10 dB), up to 36 dB over the floor, with raw samples spanning 18-234 and nothing clipped.
- Each burst is about 1.2 ms long. The signal is 2-FSK with about ±60 kHz deviation and about 71.5 kbaud (symbol about 14 µs; consistent across 8 of the 10 strongest bursts).
- Bursts land on a repeating channel grid about 0.31 MHz apart: 910.77, 911.07, 911.39 and 911.71 MHz, plus others up to 912.6 MHz. The same hop pattern recurs about 3 s apart.
- rtl_433's pulse analyzer agrees ("Detected FSK package", NRZ guess, no valid packet).
- The direct decode is negative everywhere, independent of M.R.S. I ran the packaged `rtl_433.exe` 25.12 on the raw 2.4 MS/s capture, a polyphase-resampled 1.6 MS/s copy and a 1.2 MS/s copy. Each was tried with three decoder sets (the profile's meter IDs, Badger Orion hopping 282/290 alone, and all defaults) and three level modes (`-Y` default, minmax and autolevel). All 27 runs decoded 0.
- The meter protocol IDs are correct in 25.12: 149 SCM, 154 SCM+, 160 IDM, 223 Badger ORION 100 kbps, 228 Neptune R900, 282 and 290 Badger Orion hopping. ERT SCM/IDM are OOK, and Badger ORION is 100 kbps, so neither matches a ~71.5 kbaud FSK signal.
- Conclusion: the signal is real, but it isn't in a format the bundled decoders support. Identifying it needs more evidence. Do not label it a meter, TPMS or any specific system from these measurements.
- Design note: the Sensors profile keeps only about ±0.48 MHz around the selected channel, and the HackRF analog filter at 2.4 MS/s is 1.75 MHz wide. Hopping bursts outside those windows are dropped by design. That didn't cause this negative result (the full-band direct decode is also 0), but the UI should not imply full-band coverage.

### Positive controls: the decoder chain and the M.R.S. bridge work
Known fixtures in `work/sensor-native/fixtures`, run through the packaged tools:

| Fixture | Direct rtl_433 | Through `mrs-iq-channel` + DecoderInput quantization | With a -250 kHz capture offset, bridged back |
| --- | --- | --- | --- |
| ERT SCM (2.4 MS/s) | 1 ERT-SCM | 1 | 1 |
| SCM+ (2.359 MS/s) | 1 SCMplus | 1 | 1 |
| IDM (2.359 MS/s) | 1 IDM | 1 | 1 |
| Acurite 609TXC (250 kS/s) | 6 | 6 | n/a |

- **Shift direction is confirmed.** I moved the ERT fixture 700 kHz below centre and narrowed to 1.2 MS/s. With the SensorPipeline convention (target minus capture centre), it decoded 1. With the opposite sign, it decoded 0. (A first attempt at 600 kS/s was inconclusive, because the fixture's own energy sits 391 kHz off its file centre.)
- **Still open:** this verifies the worker chain only. It does not cover the in-app replay, `SensorResult.Parse` and the results table end to end (Codex request 2). A good acceptance test would replay these fixtures as MRSIQ through the app.
- **UI:** I agree that "Burst / tone inspection" should say plainly that decoding is off in that profile.

## Note 17 - receiver backlog: three causes visible in source

1. **Thin buffering.** `HackRfNative.Receive` uses `BlockingCollection<byte[]>(32)` and throws "receive backlog overflow" on the first failed `TryAdd`. With libhackrf's usual 256 KiB transfers, that's about 8 MiB: roughly 0.4 s at 9.6 MS/s and about 1.7 s at 2.4 MS/s. This matches the field result (9.6 fails, 2.4 survives).
2. **Serial consumers on the read loop.** In `RtlReceiver`, each block runs recording, `RawIq` handlers, `ProcessBlock` (demod and spectrum) and `TriggeredIq` synchronously before the next read. Any stall backs up the pipe into the helper's 32-slot queue. Battery throttling lengthens stalls. Recommendation: give each consumer its own bounded queue, with drop counters and gap markers, so a slow consumer degrades itself instead of stopping the receiver.
3. **Reconnect budget never refills, and overflow is treated as a lost device.** `HandleReceiverEnd` stores `recoveryAttempts = result.Attempts` after a successful reopen. Only a manual Start resets it to 0, so a session gets three recoveries in total, matching the 20:53, then 21:07 log. Reopening after an overflow at the same rate just overflows again (the HDTV run at 21:17 shows the receiver reopened, then overflowed again 4-7 s later). Recommendation: classify overflow separately (offer a lower rate, don't spend reconnects) and reset the budget after a healthy period.

**ADS-B fallback.** 1.75 MHz is the real HackRF analog filter at 2.4 MS/s (`compute_baseband_filter_bw_round_down_lt`, enforced by `HackRfProtocol.UsableBandwidth`). So "1.75 vs 2 MHz required" is accurate and unrelated to the HF filter. The HF-filter advice (`DecoderSetup.Check`) fires only when the gain token carries the HF flag, so check whether a stale saved token still carries it. The structural fix is an intermediate rate: the HackRF accepts 2-20 MS/s, but `ReceiverCapabilities.HackRf` offers only 2.4, 9.6, 19.2 and 20 MS/s. A rate around 4-6 MS/s would give 3.5-5 MHz usable at about half the 9.6 load, provided `mrs-aviation.exe` accepts that rate (unverified).

**Suggested order:** per-consumer queues; a larger native buffer; overflow classification plus a reconnect budget that refills; then the intermediate rate.

## Note 22 and note 19
- **Note 22:** I agree with pre-generating the waveform and never sending late. Also check whether TX scheduling shares the busy RX processing thread from note 17.
- **Note 19:** The additions are sound. Keeping the trend display separate from the model direction, and the five-state GPS light, are the right distinctions.
