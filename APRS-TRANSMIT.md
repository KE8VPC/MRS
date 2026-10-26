# Manual APRS transmit — 0.27.0-preview1

## First packet

Select the HackRF in Mayhem USB mode, then open Receiver > HackRF offset / gain > Send APRS packet. Opening stops this workspace's receiver/scanning/recording; it does not transmit. Use a separate receiver/decoder to monitor.

Enter an authorized clear RF frequency, your callsign with optional SSID, and short status text. Leave path blank for the first direct test. The preview shows the exact APRS address/payload representation. Start with a short Status packet. The user's previous tone test worked at 8 dB TX gain, but gain remains zero by default and is not a calibrated power setting.

Enable one burst and click Send APRS packet once. Each send requires enabling again. STOP TX and closing the dialog stop the owned worker. Receive operation is resumed manually with Start / Apply after closing the dialog. Listen for a clear channel before opening the sender: this preview has no automatic busy-channel sensing.

A voice radio can hear the packet sound, but successful decoding requires an APRS decoder. For MRS Receiver B with a separate RTL-SDR, select NFM, approximately 12.5–16 kHz filter, the same RF frequency, and AX.25/APRS decoding. Check callsign, payload and valid checksum. Do not cable the transmitter directly into a receiver input.

## Included

- Manual status report or uncompressed position report with decimal latitude/longitude, primary-table house symbol and comment. West/south coordinates are negative.
- Callsign/SSID validation, up to two optional path addresses, printable ASCII text up to eighty characters, exact packet preview. Blank path means direct; no digipeater path is inserted automatically.
- AX.25 UI frame, PID F0, reflected CRC-16/X-25, bit stuffing, NRZI and continuous-phase 1200/2200 Hz AFSK at 1200 baud, FM with nominal 2.5 kHz deviation.
- APZMRS destination identifier for this experimental sender. No claim of a registered product identifier.
- One packet, lead/tail flags, bounded waveform below three seconds. RF amplifier off, TX gain 0–20 dB; existing two-meter/70-cm test ranges.
- Packet-specific native TX flush callback waits for buffered output completion before shutdown. Completion is not an acknowledgement from a receiver. Session log distinguishes TX start from worker stop; settings/arming are not persisted.

No timed beacons, APRS-IS uploads, GPS input, message acknowledgements/retries, digipeater or automatic channel access. The same HackRF cannot receive while transmitting. Hardware packet reception remains an acceptance check even though the FM tone path previously passed on the user's equipment.

## Validation

Synthetic status and position waveforms decode to their exact frames through the built-in modem. An independent check demodulated the actual 8 MS/s signed I/Q waveform with scipy and recovered its synthetic status packet with multimon-ng AFSK1200. No RF transmissions occurred during software checks. Native buffer flush behavior and over-the-air packet reception require hardware validation.

Protocol references: APRS 1.0.1 and errata (https://www.aprs.org/doc/APRS101.PDF and https://www.aprs.org/errata.html); libhackrf 2024.02.1 transmit/flush callback contracts. Synthetic fixtures use N0CALL and are not station settings.
