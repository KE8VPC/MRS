# AX.25 / APRS receive preview — 0.11.0

## Start decoding

Open the Packets tab beside Spectrum and select Enable packet decoding. The checkbox is saved with preferences. Start a source from Receiver, or open a recording. Decoding continues when you return to Spectrum.

- RTL-SDR: select NFM and the intended packet channel. A 16 kHz receive filter is a useful initial test setting. This decoder expects 1200-baud Bell 202 AFSK; other packet waveforms are not supported.
- Conventional radio: select its Windows audio input, including a connected Digirig, and start audio reception. Use clean receive audio with enough level and without clipping. The radio's own squelch can cut the beginning of bursts; M.R.S. cannot recover samples the radio does not supply.
- WAV: open compatible audio containing packet tones. The existing audio reader converts supported files to the decoder's 48 kHz PCM path.
- I/Q: open a compatible M.R.S. recording with NFM channel metadata. Replay follows its recorded channel and settings; arbitrary raw I/Q/SigMF import is not implemented.

Listening volume, monitor enable and M.R.S. listening squelch do not gate packet decoding. Source changes, retuning and replay seek reset the partial-frame decoder so unrelated samples cannot complete one packet. Previously displayed packets remain until Clear or history eviction.

## Packet inspection

The table displays observed UTC time, source/destination callsigns, type, summary and validation. Select a row for its digipeater path, information payload, raw hexadecimal bytes including FCS, input attribution, replay flag, available channel frequency and approximate source position. Nonprintable payload bytes are escaped; raw bytes are retained.

FCS is the frame's error-detection checksum. A valid FCS does not establish sender identity, and malformed addresses remain explicitly invalid even when the checksum matches. Failed-FCS frames are hidden by default and can be shown for inspection. Only complete, byte-aligned frames are published; this is not a raw continuous-bitstream viewer.

Supported interpretation includes AX.25 UI frames and their PID, basic APRS uncompressed positions (including timestamp syntax), status, messages, message IDs and acknowledgments/rejections. Unsupported AX.25 frame families retain raw contents without guessing connection state. Unsupported APRS formats retain the information payload. Compressed/Mic-E or ambiguous positions do not produce guessed coordinates. APRS timestamps are not used to change the computer clock.

Observed UTC is the PC clock at decoding time, including during replay; it is not the original transmission time. Approximate source position helps locate a replayed packet but is not a precision RF timestamp. NIST and signal-derived synchronization remain planned.

Filter searches packet text and identifiers. Export JSON saves the rows currently displayed by the active filter and failed-FCS option. History retains 500 observations. A separate bounded processing queue retains 256 pending observations and reports overflow; the cumulative FCS counters can exceed visible history. Clear resets history and counters. Nothing is uploaded, digipeated or transmitted.

## Two RTL-SDR Blog V3 receivers

Receiver entries now show device index and reported serial; the identity details below the selector show manufacturer/product/serial. With unique usable identities, selection and sampling-branch preferences follow the receiver when device indexes change. Generic or duplicate serials require manual selection and cannot enable identity-based reconnect. Receiver A/B workspaces now support two independent sources; see RECEIVER-WORKSPACES.md. Device EEPROM/serials are not changed.

Test each physical V3 individually: Find, select the intended unit, confirm its identity, receive a known station, change frequency/gain/rate, and make/replay a short recording. Then change receivers and confirm the correct sampling selection. Live APRS position decoding is demonstrated by supplied I/Q recordings. Physical dual-receiver performance, direct sampling and reconnect still require station acceptance; automated identity checks use simulated receivers.

## Current limits and references

Receive-only experimental decoder. No packet transmission, TNC control, connected-mode sessions, 9600-baud packet, APRS-IS/IGate, mapping, weather/telemetry interpretation or full APRS compliance is claimed. Synthetic noise, clock-offset and framing tests do not establish field sensitivity or interoperability with every transmitter. Other planned digital decoders remain separate milestones.

Protocol references: [TAPR AX.25 version 2.2](https://files.tapr.org/tech_docs/AX25/AX25.2.2.pdf) and [APRS Protocol Reference 1.0.1](https://www.aprs.org/doc/APRS101.PDF). The modem and parser are implemented within M.R.S.; these documents define the framing and supported message syntax.


## Shared Data tab

The former Packets tab is now Data. AX.25/APRS remains the available 1200-baud packet decoder; WFM RDS/RBDS station metadata now appears automatically in the same history with protocol, source, frequency, replay position where available, and validation. Only changed station metadata is added, rather than repeating every received RDS group. JSON exports use MRS-data filenames and add Protocol, Validated and IsValidated fields while retaining existing packet fields. RDS rows are decoded metadata snapshots and do not supply raw group bytes.

Show unvalidated exposes failed AX.25 frame candidates. Activity from P25 or another signal can generate such candidates; they are not identified P25 frames or evidence of protocol recognition. P25 Phase I C4FM now has a separate decoder checkbox and explicit BCH-header/CRC-message validation; see P25.md. DMR and other decoder families remain planned. HD metadata also enters Data, while HD listening and diagnostics remain in the HD Radio tab.

