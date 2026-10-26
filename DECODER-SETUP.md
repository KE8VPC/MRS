# Decoder setup checks — 0.34.0-preview1

Open Receiver > Decoder setup checks, or click Check / setup receiver in Aviation. Choose ADS-B, UAT, ACARS, VDL2, ATSC 1.0, HD Radio, DMR, NXDN48, NXDN96, P25 Phase I C4FM, POCSAG/FLEX or CW carrier/Morse.

The report distinguishes applied live/replay capture from selected settings while stopped. It checks target frequency, sample rate, usable captured coverage, the HackRF HF hardware filter, sampling mode, HD Radio capture centering, and listening mode/filter where the decoder uses demodulated audio. Raw-I/Q aviation and ATSC checks do not incorrectly treat the listening audio filter as decoder bandwidth. The aviation pipeline's coverage error now reports center, usable width and required width, with an HF-filter hint for 1.75 MHz captures.

Apply recommended settings stops reception, finalizes current recordings, disarms automatic recording, disables automatic modulation changes and updates the controls. It preserves gain, amplifier selection, saved channels and decoder enable/voice selections. It does not start a receiver or decoder. Click Start / Apply on the receiver, then start the selected decoder. It will not change settings during replay; stop replay first. Changing live settings cannot recover frequencies absent from an existing recording.

HackRF ADS-B baseline: 1090 MHz, 9.6 MS/s, +250 kHz offset, HF hardware filter off. UAT uses 978 MHz and the same HackRF wide-capture baseline. RTL-SDR ADS-B/UAT use 2.4 MS/s. HD Radio uses centered capture with WFM. ATSC requires HackRF with the available adapters (RTL-SDR rates are too low). Narrowband profiles provide starting filters, not proof of protocol identification or live reception. POCSAG 512 can retain the user's successful 6 kHz filter; generic paging starts at 12.5 kHz. CW preserves an existing direct-sampling selection for an RTL HF station.

Passing capture/settings checks means the input is suitable for an attempt. It does not establish signal quality, antenna suitability, actual protocol identity, encrypted-voice support, trunk following or successful decoding. Decoder status/drop counters remain available for the result.
