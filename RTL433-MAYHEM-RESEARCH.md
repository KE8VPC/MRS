Implementation update: 0.35.0-preview1 now includes rtl_433 sensor/meter profiles and original M.R.S. burst inspection; see SENSORS-METERS.md. The research-only status below describes the earlier inventory. Other Mayhem-derived features remain research.

# Receiver decoder integration research — October 24, 2026

Status: resources added and source inspected; no M.R.S. executable rebuilt, no decoder backend installed, and no live device acceptance claimed. The running 0.34 installation is unchanged. User clarified 433 MHz, not 443 MHz.

## First integration: rtl_433

Official project: https://github.com/merbanan/rtl_433
Official device/config catalog: https://github.com/merbanan/rtl_433/blob/master/conf/rtl_433.example.conf
Test recordings: https://github.com/merbanan/rtl_433_tests
Reviewed commit: 406f9a33550a2b6b3d0d8a8a486417ce5b3f13ab

Selected source and notices were downloaded to the workspace research cache, not the runtime distribution. SOURCES.json pins the review; rtl433-catalog.json indexes upstream protocol IDs/descriptions from that commit's example configuration. This catalog is not a set of enabled decoders and is not a frequency plan.

Integrate a Windows helper using M.R.S.-owned I/Q from live SDR or .mrsiq replay. Shift/filter/resample a valid channel span, provide explicit input format/sample rate, and feed stdin with bounded queues. Parse JSON into source-attributed observations; drain stderr separately and cap diagnostics. Restart/reset on retune/rate/source discontinuity or replay seek; report drops and failures. Never let the helper open the same receiver independently. Speaker audio is not a universal input for OOK/FSK devices.

Initial device families: weather/temperature/humidity, TPMS, leak/contact sensors and selected remotes. Also evaluate upstream SCM (149), SCMplus (154), and Badger ORION variants (223/282/290) as distinct meter targets; IDs are version-specific, not hard-coded contracts. Use ordinary default-enabled decoders first and expose optional profiles with clear validation limits. Preserve fields/units/raw JSON, aliases, first/last seen, repeat counts, live/replay status and recording links. Do not invent raw bits when a backend only reports fields. Integrate under Analysis > Sensors / Short-range devices; keep utility meters distinguishable.

Runtime packaging needs a pinned build/hash, license notices and corresponding source/build instructions. Inspected rtl_433.c permits GPL version 2 or later. Check individual dependencies and any code incorporated into M.R.S.; a helper boundary alone is not a conclusion about licensing obligations.

## Mayhem inventory and portability

Official project: https://github.com/portapack-mayhem/mayhem-firmware
Receiver catalog: https://github.com/portapack-mayhem/mayhem-firmware/wiki/Receivers
Reviewed next-branch commit: fd8edf69e326ab8b9be67c88de7eb2aaf5468c8f

This is upstream source research, not an inventory of the user's installed SD-card apps or exact firmware revision. Mayhem runs on embedded ARM hardware and uses ChibiOS, shared-memory queues, baseband images and PortaPack widgets. Its .ppma apps cannot be dropped into Windows/WPF. Parser/DSP algorithms and protocol tables are reusable candidates after per-file license/provenance review, sample-rate adaptation and tests. The repository license is GPL-3.0; inherited components may have their own terms. Research copies remain outside release source/runtime packages.

| Candidate | Verified upstream evidence | M.R.S. value and limits |
|---|---|---|
| ERT meter RX | proc_ert.cpp, ert_packet.cpp and ERT wiki: SCM, SCM+, IDM; sync, Manchester handling, checksum checks and meter fields | High priority North American meter reference. Usually 902–928 MHz, not the 154 MHz load-control burst. Preserve raw consumption; physical units need device-specific scale. Source processing notes 4.194304 MS/s; wiki RF coverage/default tuning is not an input sample-rate contract. |
| SubGhzD | Pulse level/duration dispatch and individual protocol classes: Princeton, Holtek, Intertechno, Honeywell, Somfy and others | Compare against rtl_433 to avoid duplicate implementations. Recognizing protected/rolling-code frames does not establish decoded protected contents. |
| Weather / TPMS | Separate weather protocol registry and TPMS packet/application paths | Secondary parser references and fixtures; prefer shared rtl_433 backend where covered. |
| Wireless M-Bus | proc_wmbus.cpp plus mode-specific UI; wiki T/C/S profiles and CRC validation | Later meter profile. Primarily European 868 MHz references here. Headers/raw frames may be readable while encrypted measurement payloads remain unavailable. |
| ProtoView | Listed upstream receive app | Useful pulse/burst inspection workflow for Analysis; deeper source inspection before adopting algorithms. |
| FPV Detect | ui_fpv_detect.cpp: sequential channel tuning, power history, candidate/lock state and adjacent-channel comparisons | Possible 5.8 GHz analog FPV activity detector. Not drone identity, geographic position, decoded video or proof a carrier comes from a drone. Confidence is heuristic, not calibrated probability; power is not automatically calibrated dBm. Forty channels are scanned sequentially, not sampled simultaneously. |
| AIS, radiosondes, SSTV, RTTY, WEFAX, two-tone RX, RDS, VOR | Present in official receiver catalog | Additional backlog candidates; catalog presence is not M.R.S. support or validation. Existing APRS/ADS-B/ACARS/paging overlap should not create duplicate pipelines. |

ERT documentation: https://github.com/portapack-mayhem/mayhem-firmware/wiki/ERT
FPV documentation: https://github.com/portapack-mayhem/mayhem-firmware/wiki/FPV-Detect
Wireless M-Bus: https://github.com/portapack-mayhem/mayhem-firmware/wiki/WMBus-RX
SubGhzD: https://github.com/portapack-mayhem/mayhem-firmware/wiki/SubGhzD

## Meter-specific reference: rtlamr

https://github.com/bemasher/rtlamr
https://github.com/bemasher/rtlamr/wiki/Protocol

Documents SCM, SCM+, IDM, netIDM, R900 and R900BCD. Current main.go uses an rtl_tcp client; this is not a ready-made direct HackRF input path. Evaluate as protocol reference or a separately adapted backend. The inspected source is AGPL-3.0-or-later; assess distribution obligations before code reuse. Meter readings do not automatically establish an owner, address, billing unit or calibrated consumption.

## Drone features are separate tracks

1. Analog FPV carrier detection: Mayhem provides a useful concrete implementation reference. Scan gaps and 5.8 GHz antenna/path limitations must be explicit.
2. Broadcast Remote ID: https://github.com/opendroneid/opendroneid-core-c provides payload encoding/decoding resources. Capture Wi-Fi/Bluetooth frames through a supported receiver/bridge; a payload library does not supply RF demodulation. Windows and adapter support must be established. HackRF tuning range alone does not guarantee Remote ID acquisition.
3. DJI proprietary DroneID/control links: no general DJI identity decoder was established by the inspected Mayhem paths. Research separately; do not equate BLE RX or FPV Detect with DJI identification.
4. Video viewing: independent analog video demodulation work. Proprietary digital video compatibility is not established by these references.

## Utility load control is still unidentified

The four 154.463 MHz recordings remain probable tone-coded utility load control. ERT consumption telemetry and generic DNP3/Modbus parsers do not establish their encoding. Continue tone timing, repeat comparison, frame recovery and validation before naming commands or group addresses.

## Validation before release

Test pinned backend input/JSON with independent recordings; malformed/truncated output, unknown/noise bursts, checksum handling, unit conversion, aliases and duplicate grouping. Check live/replay separation, reset on seek/retune, bounded resources, CPU/drop behavior and receiver ownership at normal HackRF rates. Then obtain live acceptance per device/protocol. Preserve original recordings; do not silently enable all experimental decoders or label noise as a known device.
