# hd2.mrsiq analysis — October 8, 2026

Original Desktop recording read only and preserved. Working files are under work/tv-field-hd2. No production software or live receiver settings changed.

## Recorded settings and integrity

- 183.000 MHz listening and capture center: RF channel 8, zero offset.
- 19.2 MS/s; 15 MHz reported usable bandwidth.
- LNA 32 dB, VGA 36 dB, RF amplifier enabled.
- 198,148,096 complex samples in 12,094 complete frames; clean completion marker.
- Sample duration 10.3202 seconds; first-to-last timestamp span 10.3268 seconds.
- Largest frame timestamp interval 5.9255 ms. No large recorded timestamp gaps; this does not independently prove absence of all USB sample loss.

## Clipping

7.5349% of individual unsigned I/Q component values are at 0 or 255 (I 7.5158%, Q 7.5540%). Centered component RMS is about 70 counts, compared with 21.3 counts and zero rail hits in the earlier 479 MHz recording. This is strong evidence of capture clipping, independently of displayed spectrum height. It does not identify which received signal contributes most to overload or exclude additional receiver distortion.

Lowering amplitude after capture cannot recover clipped information. A lower-gain recording is needed for a controlled comparison.

## Current decoder result

The unchanged decoder acquired enough synchronization to process 130,776 Reed-Solomon packets. It classified 109,969 as bad (84.09%) and 20,807 as accepted. This is different from the earlier 479 MHz excerpt, which delivered zero packets to that stage. Accepted RS counts alone do not establish intact program tables or video.

The media parser reported program IDs 3 and 4 with video/audio streams, but emitted video-header errors and inconsistent video identification. Treat these as provisional parser results, not a validated station/subchannel listing. This investigation did not implement or certify PAT/PMT validation.

## Next comparison

Keep 183 MHz, 19.2 MS/s and the existing offset state unchanged; first reduce VGA from 36 to 20 dB while holding LNA at 32 dB and RF amp on. This is a proposed test, not a tested optimum. Restart TV decoding after the gain change if necessary, allow acquisition to settle, and compare error-counter growth or make another short raw I/Q capture. Reassess clipping before changing other parameters. The amplifier-off result from the earlier UHF station should not be assumed to apply to this different VHF channel.
