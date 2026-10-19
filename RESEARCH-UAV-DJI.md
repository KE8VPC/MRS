# UAV / DJI radio tracking feasibility — October 18, 2026

Research only. No UAV decoder, video decoder, hardware capability or live acceptance has been added to M.R.S.

## Assessment

1. Broadcast Remote ID is the best first tracking target. FAA describes public identification/location broadcasts over technologies such as Wi-Fi and Bluetooth. Standard Remote ID includes drone and control-station information; broadcast modules report takeoff location instead of necessarily providing a moving controller location. Coverage is not universal: exemptions, FRIAs, equipment and reception conditions matter. Treat reported coordinates as broadcast claims, not independently measured positions.
2. DJI proprietary DroneID is a distinct experimental target. RUB-SysSec's NDSS 2023 receiver was tested with a USRP B205-mini and DJI Mini 2 / Mavic Air 2; its authors explicitly call it a proof of concept not optimized for weak RF, performance or range. This does not establish support for every DJI model/firmware. Their supplied recordings offer an offline starting point.
3. HackRF feasibility is credible: 1 MHz–6 GHz tuning and up to 20 MS/s cover the relevant bands and a single approximately 10 MHz occupied DroneID burst (15.36 MHz including guard carriers in proto17's description). Bandwidth fitting is an engineering inference, not an M.R.S. decoder validation. A high-rate recording/resampling path would be needed. One receiver cannot continuously monitor all 2.4 and 5.8 GHz channels; scanning loses observations. RTL-SDR Blog V3 does not directly tune those bands and its narrow capture is unsuitable for the documented wide DroneID waveform.
4. RF energy detection alone cannot identify a drone, provide GPS, or establish range. A directional antenna can assist bearing work but one ordinary single-antenna receiver cannot triangulate a location. Prefer decoded coordinates and distinguish them from direction-finding measurements.
5. DJI video is separate from identification. DJI documents AES-256 protection for OcuSync Enterprise; exact behavior is model/generation dependent. No general-purpose passive DJI video decoder was established by this review. Capturing RF or decoding DroneID does not decrypt video. A supported owner-provided controller/SDK livestream is a practical alternative; DJI Cloud API documents RTMP/RTSP and other protocols on supported enterprise integrations. Compatibility must be checked against the exact drone/controller.
6. Unencrypted analog FPV video is a plausible separate HackRF feature. A project reports PAL hardware reception with HackRF/PortaPack at 20 MS/s; its current app is macOS, not a drop-in Windows component. It reports NTSC synthetic validation and limitations from the 8-bit ADC/capture bandwidth. Its CC BY-NC license needs evaluation before reuse; do not import it by assumption. This is not DJI OcuSync video.

## Suggested M.R.S. sequence (proposal, not scheduled implementation)

- UAV table/map: identifier, protocol, reported position/altitude/velocity, last-seen age, source, signal level and raw observation. Unknown fields stay blank; stale positions expire. Keep operator/takeoff locations distinct from drone positions and preserve local-only operation.
- Standards-based Remote ID input: evaluate compatible BLE/Wi-Fi receiver or a local bridge. OpenDroneID core C and Android receiver provide protocol/receiver references. A generic Windows Wi-Fi/Bluetooth adapter must not be assumed to support all Beacon/NAN/extended-advertising transports.
- DJI DroneID: decode published samples first, preserve provenance, then validate a known consenting test drone with HackRF recordings. Keep detection candidates separate from validated frames and record channel-scanning gaps.
- Analog FPV: separate PAL/NTSC demodulation and sync work; validate owned/authorized sample video. Optional owner-supplied video streams are a separate integration.

No promise of detection range, all-drone coverage, owner-name lookup, universal modern DJI support or passive encrypted-video access. No interference, control takeover or transmission is part of the proposed receive scope.

## Primary sources reviewed

- FAA Remote ID overview: https://www.faa.gov/uas/getting_started/remote_id
- FAA standard Remote ID fields: https://www.faa.gov/sites/faa.gov/files/uas/getting_started/remote_id/industry/Remote_ID_Standard.pdf
- OpenDroneID core C: https://github.com/opendroneid/opendroneid-core-c
- OpenDroneID Android receiver and transport limitations: https://github.com/opendroneid/receiver-android
- RUB-SysSec DJI DroneID receiver / NDSS 2023 research: https://github.com/RUB-SysSec/DroneSecurity
- DJI DroneID waveform research: https://github.com/proto17/dji_droneid
- Additional research reporting HackRF prototype: https://arxiv.org/abs/2207.10795
- File decoder candidate (not executed): https://github.com/anarkiwi/samples2djidroneid
- HackRF manufacturer documentation: https://hackrf.readthedocs.io/en/latest/hackrf_one.html
- RTL-SDR manufacturer overview: https://www.rtl-sdr.com/about-rtl-sdr/comment-page-2/
- DJI OcuSync Enterprise security: https://enterprise-insights.dji.com/de/blog/was-macht-die-matrice-300-rtk-so-sicher
- DJI supported enterprise livestream API: https://developer.dji.com/doc/cloud-api-tutorial/en/api-reference/pilot-to-cloud/mqtt/rc-plus/live.html
- Analog FPV receiver, hardware claims and license: https://github.com/martinbogo/hackrf-fpv-receiver

Repository claims are feasibility evidence, not independently verified performance. No third-party programs were installed or executed during this research.
