# HackRF and WiNRADiO receiver research

Implementation update, October 4, 2026: 0.17.0-preview1 now includes an experimental HackRF One / PortaPack USB receive adapter. See HACKRF.md for implemented controls, rates, dependencies and pending station acceptance. The broader manufacturer capabilities below remain research references; WiNRADiO, HackRF Pro, TX and sweep features are not implemented.

Research date: September 14, 2026. Official manufacturer documentation reviewed. These are planning requirements, not implemented or hardware-tested M.R.S. features. No executable rebuild accompanies this research.

## Bandwidth terminology

Keep tuning range, spectrum-overview width, continuous I/Q capture bandwidth, sample rate and demodulator bandwidth separate. An overview can span frequencies unavailable in the current I/Q stream. Sweeping across a tuning range is sequential reception. Internal ADC rate/resolution is not necessarily the format or rate delivered to the PC.

## HackRF

| Model | Specified operating range | Streaming capabilities | Interface and operating constraints |
|---|---|---|---|
| HackRF One | 1 MHz–6 GHz | 2–20 MS/s; 8-bit I and 8-bit Q | USB 2.0 High Speed; half-duplex RX/TX |
| HackRF Pro | 100 kHz–6 GHz | Normal 8-bit mode to 20 MS/s; manufacturer lists 16-bit extended-precision samples at lower rates and 4-bit mode to 40 MS/s | USB 2.0 over USB-C; half-duplex RX/TX |

Sources: [HackRF One product](https://greatscottgadgets.com/hackrf/one/), [One technical documentation](https://hackrf.readthedocs.io/en/stable/hackrf_one.html), [HackRF Pro product](https://greatscottgadgets.com/hackrf/pro/).

For One, rates below 8 MS/s are not recommended because of ADC and baseband-filter limitations. Capture at a suitable higher rate and digitally filter/decimate for narrow channels. Distinguish API-accepted rates from recommended operating rates. Do not apply this One-specific advice to Pro without verification. [Sampling-rate guidance](https://hackrf.readthedocs.io/en/latest/sampling_rate.html)

One has separate RF amplifier, IF LNA and baseband VGA controls: RF off/on (approximately 11 dB), LNA 0–40 dB in 8 dB steps and VGA 0–62 dB in 2 dB steps. A generic single gain slider cannot represent all stages accurately. [Gain controls](https://hackrf.readthedocs.io/en/latest/setting_gain.html)

One provides external clock input/output and software-controlled 3.3 V antenna power up to 50 mA. Pro adds a TCXO, FPGA, trigger facilities and hardware TX-disable capability. Pro can tune beyond its specified operating range; tunability must not be represented as guaranteed performance. Its extended-precision mode is listed at roughly 9–11 effective bits, not native 16-bit ADC performance. Verify firmware/API support and packed formats before offering Pro advanced modes. [One](https://greatscottgadgets.com/hackrf/one/), [Pro](https://greatscottgadgets.com/hackrf/pro/)

Plan a native libhackrf source worker. Official Windows build instructions exist; match library, tools and firmware versions. HackRF sample handling uses signed 8-bit components, unlike the current RTL unsigned-byte path. SoapyHackRF is another possible adapter route. [Installation](https://hackrf.readthedocs.io/en/latest/installing_hackrf_software.html), [Software/API support](https://hackrf.readthedocs.io/en/latest/software_support.html)

At 20 MS/s with two 8-bit components, calculated uncompressed payload is 40 MB/s or 2.4 GB/minute, excluding metadata. A 20 MS/s nominal complex-sample span does not guarantee a flat usable 20 MHz passband. Receive support and future transmit support must be separate capabilities; half duplex cannot receive and transmit simultaneously.

## WiNRADiO

WiNRADiO is a family of receivers with different RF paths, transports and APIs. Model and connection discovery must drive M.R.S. controls. These representative models cover relevant architectural cases; this is not an exhaustive inventory of legacy products. [Receiver family matrix](https://winradio.com/home/receivers.htm)

| Receiver | Tuning coverage | Real-time overview | Maximum continuous DDC/recording bandwidth | Connection |
|---|---|---|---|---|
| WR-G33DDC Excalibur Pro | 9 kHz–49.995 MHz | 50 MHz | 4 MHz | USB 2.0 |
| WR-G35DDCi Excalibur Ultra | 1 kHz–45 MHz | 45 MHz | 32 MHz | PCIe |
| WR-G39DDCe Excelsior | 9 kHz–3.5 GHz | 16 MHz | 4 MHz maximum; individual DDC limits need confirmation | USB 2.0 |
| WR-G65DDCe Excalibur Sigma | 1 kHz–88 MHz OR 118–190 MHz | 88 or 72 MHz | USB 3: 64 MHz; USB 2: 6.4 MHz; Gigabit Ethernet: 16 MHz | USB / Ethernet with PoE |
| WR-G69DDCe Excelsior Ultra | 8 kHz–80 MHz OR 43 MHz–8 GHz | 80 or 34 MHz | USB 3: 32 MHz; USB 2: 6.4 MHz; Gigabit Ethernet: 16 MHz | USB / Ethernet with PoE |

Sources: [G33](https://winradio.com/home/g33ddc.htm), [G35](https://winradio.com/home/g35ddci.htm), [G39 specifications](https://winradio.com/home/g39ddce-s.htm), [G65 specifications](https://winradio.com/home/g65ddce-s.htm), [G69 specifications](https://winradio.com/home/g69ddce-s.htm).

G65 and G69 use mutually exclusive RF input paths. Their listed overview widths do not imply that two ranges can be captured simultaneously. G65's gap between 88 and 118 MHz makes it unsuitable for covering the usual 88–108 MHz broadcast FM band. G69 specifications include WFM. Confirm actual hardware version and any regional tuning restrictions when discovering capabilities.

G33 and G35 illustrate a useful UI pattern: a broad overview, a movable DDC capture window and multiple demodulators within that window. Both advertise three demodulators, variable DDC bandwidth, preselection and measurement/recording tools. These are useful design references; vendor software features do not establish that every function is exposed through its SDK. [G33](https://winradio.com/home/g33ddc.htm), [G35](https://winradio.com/home/g35ddci.htm)

G39's product page is inconsistent: its introduction describes two independent 4 MHz channels, while the detailed software section describes one channel up to 4 MHz and another up to 2 MHz. Record this as unresolved; query per-channel SDK capabilities before promising two simultaneous 4 MHz streams. [G39 product description](https://winradio.com/home/g39ddce.htm)

G65/G69 list 16/24-bit I/Q output. Do not confuse these host-stream formats with their internal 16-bit ADC rates. Packed representation, actual sample rate, alignment and endianness need API validation. Wider streams require measured host and disk throughput. [G65 specifications](https://winradio.com/home/g65ddce-s.htm), [G69 specifications](https://winradio.com/home/g69ddce-s.htm)

External reference and coherent-operation options are model/configuration dependent. For example, G65 offers external-reference/coherent options and 1 PPS facilities. Frequency-reference synchronization, coherent sampling and UTC clock synchronization are separate functions. [G65 options](https://winradio.com/home/g65ddce-options.htm)

## Windows integration findings

- WiNRADiO provides model-specific SDKs with C/C++ and C# examples. Prefer source adapters around these APIs; integrating the vendor's application is not required. Verify SDK redistribution terms before packaging native libraries. [G33 SDK](https://winradio.com/home/g33ddc_sdk_oop-nav.htm), [G39 SDK](https://winradio.com/home/developer-g39ddc.htm), [G65 SDK](https://winradio.com/home/developer-g65ddc.htm)
- G39 developer text describes a 32-bit DLL but also discusses 64-bit applications. Confirm the installed DLL architecture rather than assuming direct loading into M.R.S. x64. An isolated 32-bit worker is an option if required. [G39 developer page](https://winradio.com/home/developer-g39ddc.htm)
- G39 SDK structures distinguish SampleRate, Bandwidth and BitsPerSample, and expose overview width separately. Its callbacks run on SDK threads; calling the receiver API from those callbacks can deadlock. Queue samples promptly and send control operations from a separate execution path. [G39 SDK reference](https://www.winradio.net/home/g39ddc_sdk-doc.htm)
- G65 USB connection takes priority over Ethernet. Transport changes can alter available bandwidth and connection state; the application must refresh capabilities. [G65 developer page](https://winradio.com/home/developer-g65ddc.htm)

## Requirements to carry into M.R.S.

1. Discover capabilities per receiver, firmware, active RF input, transport and installed options. Avoid an RTL-derived global bandwidth ceiling.
2. Use 64-bit frequency fields throughout adapters, IPC, navigation and versioned recording metadata. Frequencies above approximately 4.295 GHz exceed unsigned 32-bit Hz storage.
3. Represent sample rate, usable capture bandwidth and overview width independently. Show the capture window and tuned channels on the overview; identify uncaptured regions clearly.
4. Support explicit sample representations: signed/unsigned integer components, component width, container width, packing, byte order and normalization. Add wider formats through versioned recording changes with existing-file compatibility.
5. Expose model-specific gain stages, attenuation, preselectors, reference inputs and antenna power only where supported. Antenna power and TX are explicit operator controls.
6. Size FFTs and bounded queues for the chosen rate; separate display refresh from acquisition. Provide required recording throughput, dropped-sample reporting and eventual file rotation.
7. Distinguish multiple software demodulators from independent RF tuners. Plan channelization within a shared captured span before assuming independent coverage.
8. Retain conventional radio audio/CAT/PTT adapters alongside SDR adapters. These models expand hardware support rather than redefine M.R.S. as SDR-only.

Suggested sequence: capability/schema foundation; receive-only HackRF adapter; one selected WiNRADiO model validated with its SDK and hardware; then additional model adapters and advanced modes. Selection of a particular WiNRADiO model remains open. No additional adapter, TX path or vendor decoder is implemented by this research.
