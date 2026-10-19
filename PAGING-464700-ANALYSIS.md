# Supplied 464.700 MHz recording — 2026-10-18

Input: MRS-IQ-A-20261018-212339.mrsiq. Original recording left unchanged; not included in release packages.

- HackRF, 19.2 MS/s; capture center 465.200 MHz, selected channel 464.700 MHz, NFM, 12.5 kHz filter.
- Approximately 15.313 seconds of I/Q; 15.593 seconds between first/last host frame timestamps. These host times are not hardware gap measurements.
- No input component samples at the 0/255 rails in this recording.
- Independent offline frequency shift/resampling and FM discrimination found POCSAG 512-baud frame sync, including exact sync-word matches.
- Paging checks with 6.25–15 kHz low-pass cutoffs (12.5–30 kHz full widths) produced no decoder output.
- Tighter 2.5–5 kHz low-pass cutoffs produced sparse POCSAG512 decoder output. Recovered addresses/text varied with filtering, so no reliable message or sender identity is asserted. POCSAG lacks an end-to-end message CRC.
- No FLEX messages were recovered. The new DMR/LTR discriminator check produced zero observations.

Conclusion: POCSAG512 is present; the original failure is consistent with weak/noisy reception and excess noise admitted by the wider filtering, rather than an unsupported paging protocol. This does not establish a station/licensee identity or guarantee recovery of every page.

Try the receiver's existing 6 kHz NFM filter and compare 8 kHz. This approximates the tighter offline bandwidth; it is not the same filter implementation. Improve antenna placement and capture a longer active transmission. More SDR sample bandwidth does not by itself improve the selected narrow channel's SNR. Preserve raw I/Q for subsequent comparison.
