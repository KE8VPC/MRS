# CTCSS / DCS receive preview — 0.15.0-preview1

Enable **Data → Enable CTCSS / DCS**. The setting is saved separately for each receiver. A live readout appears above the spectrum; acquired, changed and lost signals are recorded in Data with source, monitored frequency where known, replay attribution and approximate source position. Data filtering and JSON export include these observations. Detection does not enable tone-controlled squelch or transmission.

## Supported signals and inputs

- CTCSS: the common 50-tone set from 67.0 through 254.1 Hz, plus 62.5 Hz. PL, TPL and QT are aliases for this family. The nominal tone and measured-frequency estimate appear in the observation details.
- DCS: 104 common octal codes, normal and inverted polarity, at nominal 134.4 bit/s. DPL, DQT and DTCS refer to this family. For example, `023N / 047I` describes equivalent cyclic waveforms; the received waveform alone cannot select one name from that pair. Both are shown.
- SDR **NFM** and compatible MRSIQ recordings: uses the 48 kHz discriminator before listening filters, volume and RF squelch. Select the actual channel and suitable channel bandwidth. Detection remains active when the application listening gate closes.
- Windows radio audio and WAV: uses 48 kHz PCM before application listening volume/squelch. The input must preserve low-frequency signaling. Speaker/headphone paths or a radio's own audio filtering/squelch may remove it; a compatible flat/discriminator output is preferable. Stereo PCM is averaged to mono, so opposite-polarity stereo channels can cancel.

WFM, AM and SSB do not feed this decoder from the SDR path. The feature is receive indication only: no new PTT, transmit encoder, tone-selective audio gate, automatic Frequency Manager changes, or DTMF/two-tone/ANI decoder is included.

## Detection behavior and limits

CTCSS uses filtered, overlapping one-second windows, checks tone coherence and measured frequency against the supported list, and requires two consecutive matching windows. DCS requires three identical error-free 23-bit Golay words, confirmed in two consecutive windows. The preview searches multiple bit phases and rates around 134.4 bit/s; it does not guess or correct damaged codewords. Short transmissions, weak/filtered codes and heavily distorted audio may remain unreported.

Typical acquisition takes about 1.2 seconds of continuous suitable input. Loss confirmation also has a delay; this is an analyzer rather than a fast audio squelch controller. Stop, source changes, retunes and replay gaps reset detection. Paused/finished replay is identified explicitly instead of presenting a live code. Historical rows remain available until cleared; reset boundaries do not manufacture an over-the-air loss event.

The Data queue is bounded to 256 pending observations, and the common inspector retains the latest 500 rows. Stable signals generate acquisition/loss events, not one row per analysis window. A tone/code identifies signaling, not the transmitting agency or a uniquely identified station. The reported coherent fraction is a signal measurement, not a calibrated probability of correct classification.

## Quick acceptance checks

1. Enable CTCSS / DCS in Data. Use the existing audio-file playback control to play `Docs/Samples/synthetic-ctcss.wav`. The spectrum readout should show **CTCSS · 100.0 Hz**, and Data should retain an acquired observation even after playback ends.
2. Play `Docs/Samples/synthetic-dcs.mrsiq` using I/Q playback. Expect **DCS · 023N / 047I** and replay-attributed Data at the artificial 446.000000 MHz fixture frequency. These bundled signals are generated examples, not captured radio traffic.
3. On a known analog NFM repeater or a saved recording, allow a few seconds of continuous transmission. Compare the received CTCSS/DCS result with a known receive/output tone or code. A repeater may use different input/output signaling or no output tone.
4. Close the application's RF listening squelch: a still-present tone/code should continue decoding. Retune, stop or switch modes: the prior live result should clear. Audio-input tests also require the physical radio to deliver the subaudible signal.
5. Toggle P25 and CTCSS / DCS independently. Turning either off must leave the other decoder's discriminator path working. Receiver A and B should keep independent settings and results.

Synthetic results do not establish live hardware/RF acceptance. Local tests cover the catalog, independent codeword constants, noisy/offset signals, inverse codes, non-signaling rejection, transitions, source resets, PCM and the production NFM I/Q path. Actual repeater/radio-input acceptance remains pending.

## Reference basis

Reviewed September 30, 2026:

- [Yaesu FT-991A CAT manual, page 7](https://yaesu.com/Files/4CB893D7-1018-01AF-FA97E9E9AD48B50C/FT-991A_CAT_OM_ENG_1711-D.pdf?ts=11%2F12%2F2025+1%3A50%3A42+AM): 50-tone and 104-code catalogs. These are signaling tables, not a claim of FT-991A CAT support.
- [CML CMX138A datasheet, Tables 3 and 5](https://www.mouser.com/datasheet/3/3653/1/CMX138A_ds.pdf): extended 62.5 Hz tone, 23-bit DCS words and least-significant-bit-first order. Manufacturer table text was available through indexed search; direct PDF retrieval was unavailable during this review. Six independent table constants are asserted, including 023 = `763813`, 134 = `2ED85C`, 365 = `2F08F5` and 712 = `0BD9CA`. These are data/reference facts; no manufacturer implementation is copied.
- [Wavecom DCS SELCAL description](https://www.wavecom.ch/content/ext/MonitoringSystemOnlineHelp/worddocuments/dcsselcal.htm): nominal 134.4 bit/s and repeating 23-bit Golay frames. Other reported rates/extended vendor codes are not claimed as fully supported by this preview.

The systematic Golay encoder used to construct the receive codebook is independently checked against the CML words. Rotation/inversion enumeration establishes the equivalent N/I labels shown in the UI. Error-free repetition is deliberately narrower than a general Golay error-correcting decoder.
