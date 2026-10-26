# DMR / NXDN voice and CW station ID — 0.31.0-preview1

Open **DMR / LTR / NXDN** and select DMR, NXDN48 (6.25 kHz channels), or NXDN96 (12.5 kHz channels). Start the receiver or I/Q replay in NFM. The selected digital decoder receives flat 48 kHz discriminator samples before voice filtering/squelch. Normal NFM noise audio is replaced by decoded audio while the digital mode is enabled.

For DMR choose Both slots, Slot 1, or Slot 2. Both slots occupy left/right audio channels. Voice gain is independent of receiver gain and P25 voice gain, with output limiting. Switch Off to restore normal receiver audio. Selecting P25 voice turns this decoder off, and selecting DMR/NXDN turns P25 voice off.

Suggested starting receive filters: DMR/NXDN96 12.5–16 kHz; NXDN48 6–8 kHz. Tune accurately. Invert polarity is available for recordings/sources with reversed discriminator polarity. The selected mode, slots and polarity are explicit; this preview does not automatically identify the modulation.

**CW station ID** runs alongside DMR or NXDN from the same receiver's NFM audio. Its toggle is shared with Data. Decoded Morse candidates appear in Data with frequency/source and are unverified estimates, not automatic callsign assignments. CTCSS/DCS enable/status remains above Spectrum and is not listed in Data.

Expand **Decoder diagnostics / received identifiers** to view the last 80 backend lines, including reported RAN, IDs, sync and CRC errors when available. These are backend diagnostics rather than verified M.R.S. NXDN channel records. They clear on tuning, stopping, replay gaps and mode changes. Weak/noisy input can produce false sync and short audio artifacts, especially NXDN; a reported sync or audio frame count alone is not identification evidence. No automatic NXDN frequency labeling is performed.

The helper is DSD-FME AW 2026-34-g69d3115, using stdin/stdout only. It never opens another receiver or controls tuning. Its default encrypted-voice muting is retained; no key loading or decryption controls are exposed. No trunk following, scanning by NXDN identifier, or simultaneous decoding of several RF channels is included.

Extract the entire package. Decoders/Digital must remain beside the other decoder directories. Each receiver owns its helper process; input and diagnostic buffers are bounded. Retuning cancels the previous helper and flushes decoded playback. Decoder errors appear in the status line; select Off then the desired mode to retry.

Live clear-speech intelligibility and live NXDN identifiers still require acceptance on known signals. Synthetic/local plumbing checks do not establish live decode quality.
