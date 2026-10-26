## Manual TX preview update

0.26.0-preview1 adds an explicitly enabled, bounded FM tone test. See HACKRF-TX-TEST.md. The receive-adapter description below documents the original RX path; TX is a separate worker command and never starts automatically.

# HackRF / PortaPack receive adapter



## Scope



Windows x64 receive adapter for HackRF One and compatible PortaPack combinations, including the user's H4M with Mayhem v2.4. Select **HackRF mode** in Mayhem to expose the radio to Windows, then select **HackRF / PortaPack (USB mode)** in M.R.S., click **Find**, select the device and **Start / Apply**. Reset the PortaPack to return to standalone Mayhem use. No firmware flash is required by this integration.



Keep `Receivers/HackRF` beside MRS.exe. The bundled libhackrf backend is isolated in an owned worker process. A missing library or inaccessible device produces a diagnostic; M.R.S. does not install USB drivers or alter firmware. Windows must expose the HackRF with its compatible WinUSB driver. Close other applications using the same radio before starting reception.



Implemented:



- USB enumeration of HackRF One devices, serial-based selection/reconnect and independent receiver A/B ownership. An RTL device and HackRF may each be device index zero without conflicting.

- 1–6000 MHz tuning using 64-bit frequency values throughout the control and recording path.

- 2.4, 9.6, 19.2 and 20 MS/s capture. Default 9.6 MS/s; 2.4 MS/s is a reduced-rate experiment, with greater aliasing risk than rates at or above 8 MS/s. Higher rates depend on USB/CPU/disk throughput.

- LNA gain 0–40 dB in 8 dB steps; VGA gain 0–62 dB in 2 dB steps; an explicit RF amplifier checkbox. There is no invented automatic hardware gain setting. RF amplifier starts off and is not restored as enabled from saved preferences. Antenna power is explicitly disabled.

- Spectrum/waterfall, navigation, receive filters, AM/NFM/WFM/USB/LSB processing and existing compatible data decoders. Adding this adapter does not add new waveform decoders or Mayhem standalone applications.

- Live gain/rate/tuning commands. Hardware changes stop RX, discard old queued buffers, apply settings and restart RX inside the same worker. Expect a short reception gap; new frames carry new settings. An in-capture channel change can remain in software.

- Full-rate unsigned 8-bit I/Q recording/replay, with HackRF source and gain provenance. Native signed samples are translated losslessly by XOR 0x80 on every component. Host timestamps do not claim hardware timing or phase coherence.

- Bounded USB callback queue. Overflow stops reception with a diagnostic rather than silently losing samples. No transmit functions are exposed.



## Capture offset (0.17.0-preview2)



HackRF defaults to enabled +250 kHz capture offset. Both frequency entry boxes specify the listening frequency. For example, listening at 162.400 MHz captures at 162.650 MHz and digitally selects the station 250 kHz below center. This avoids placing the wanted signal on the hardware center artifact. This is not PPM correction or software DC subtraction.



Stop reception to edit Enable capture offset and signed Offset (kHz), then Start. Valid entries are -5000 to +5000 kHz; the actual offset plus channel filter must fit the selected sample rate and the hardware 1–6000 MHz range. Invalid combinations are rejected without silently changing the offset. The capture center and actual offset are displayed above the left frequency box. Applied settings are saved by unique receiver identity within each receiver workspace. Devices without a unique identity use session settings only.



With offset enabled, typed tuning, spectrum clicks, steps and saved-frequency tuning move the capture center to retain the configured offset. Capture-shift buttons instead explain that offset must be disabled for independent capture shifting; visual pan/zoom remain available. Tuning briefly pauses hardware reception, while I/Q recording remains open and stores each frame's actual capture center and listening channel. Replay uses those recorded values without adding the offset again. Reconnect preserves the listening frequency and offset.



For the current HD Radio path, disable capture offset before starting centered WFM reception at 2.4 MS/s. RTL tuning remains unchanged. RF amplifier still starts off; its enabled state is not restored.



Station evidence: the user reported distorted on-center HackRF NFM and clearer offset reception. The October 7 recording confirms 162.650 MHz capture / 162.400 MHz listening. Automatic-offset software tests do not replace live acceptance of this build.



Quick acceptance: select HackRF, leave +250 enabled, enter 162.400 MHz on the left and Start. Expect capture 162.650 MHz and clear weather audio. Enter another frequency above the spectrum and verify capture remains 250 kHz above it. Stop, disable offset, and verify direct tuning; re-enable afterward. Close/reopen and Find the same serial to check persistence. Record/replay a short offset capture.



## Captured span versus analog coverage



The displayed FFT span is the sample rate. The configured analog filter is narrower: 1.75 MHz at 2.4 MS/s, 9 MHz at 9.6 MS/s, and 15 MHz at 19.2/20 MS/s. M.R.S. stores that filter width as usable coverage and keeps a selected channel filter inside it. This is nominal filter coverage, not a guarantee of flat response right to its edges. These are contiguous captures from one receiver, not merged RTL streams.



At 20 MS/s, raw I/Q alone is 40 MB/s (about 2.4 GB/minute), before headers. A full-rate recording requires a sufficiently fast drive. A replay file from this preview needs a version that understands HackRF metadata and these rates; do not use older M.R.S. builds to interpret its gain fields.



HD Radio's current rate converter accepts 960 kS/s through 3.2 MS/s. Use HackRF **2.4 MS/s**, centered on the WFM station, for that experimental path. Wider HackRF captures produce an explicit unsupported-rate message in HD. Hardware HD reception remains unverified.



## Acceptance at the station



1. Select HackRF mode on the H4M, connect a USB data cable and use Find. Confirm the expected serial. No HackRF was present during development discovery; local tests do not establish device acceptance.

2. Start at 9.6 MS/s on a known strong station with a suitable antenna. Begin with RF amplifier off and moderate LNA/VGA gains. Check spectrum, audio, squelch and relevant data decoding.

3. Enter another frequency, click the spectrum, change LNA/VGA, and select another sample rate. Check acknowledgments and that old samples are not presented under new settings. A hardware control change has a short gap.

4. Record I/Q, finish it, stop the radio and replay. Confirm frequency, mode, span, audio and data. Test the 19.2 and 20 MS/s options separately; report any backlog/USB errors.

5. Use receiver B with an RTL dongle. Verify both windows remain independent and cannot open the same HackRF twice. Stop and disconnect to confirm clean UI recovery; optionally test serial-based reconnect.

6. At the upper end, verify a suitable known signal and antenna before judging sensitivity. Synthetic 6 GHz tests verify numeric/control correctness only.



## References



- [Mayhem HackRF mode](https://github.com/portapack-mayhem/mayhem-firmware/wiki/HackRF)

- [HackRF One specifications](https://hackrf.readthedocs.io/en/latest/hackrf_one.html)

- [HackRF sample-rate guidance](https://hackrf.readthedocs.io/en/latest/sampling_rate.html)

- [libhackrf v2024.02.1 API](https://github.com/greatscottgadgets/hackrf/blob/v2024.02.1/host/libhackrf/src/hackrf.h)



Native library provenance, notices and corresponding source archives are in `Receivers/HackRF` in the binary package and `native/HackRF` in the source tree.

