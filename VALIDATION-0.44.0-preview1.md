# Validation — 0.44.0-preview1

Validation uses synthetic inputs and local mock services. No real radio transmission, live APRS upload or external JS8Call/radio operation is part of these checks.

## Automated coverage

The final executable passes 1,780 local assertions. The current/new workspace UI smoke also passes on that executable; the broad UI/replay smoke passed on check4 immediately before the final selected-caller preparation adjustment. The full local self-test suite covers the new standard encoder/waveform paths, complete quantized streaming I/Q extraction, WSPR phase lookup after 80 seconds, license/data-segment/own-call/request/device checks, slot parity/lead, selected-peer QSO progression and rejection of other-station/replay data. Existing receive/recording/scanning/trunk/bandplan/weak-timing/PSK regression checks remain included.

APRS tests validate original AX.25 UI/F0/FCS, exact body/path preservation and qAO, stale/replay/unknown payload rejection, queries, TCPIP/NOGATE third-party paths, allowed RF third-party unwrap, CRLF injection prevention, byte limits, deduplication, verified-login gating and disable behavior. A localhost mock server checks the actual login and uploaded line; no production endpoint is contacted.

JS8 tests use a localhost mock TCP API for required JSON framing and received events. The UI checks exercise APRS/JS8/HF pages, default-off reporting, the transmit dialog's dark background, and actual rules/tracking checkbox foregrounds. The established weak-signal UI smoke remains included. Full UI/replay smoke is run separately on the final executable.

## Independent waveform check

The separately installed WSJT-X 2.6.1 jt9.exe successfully decodes generated FT8 and FT4 WAVs extracted from the same signed 8-bit, 2 MS/s I/Q generator used by the RF callback. Both yield CQ K1ABC FN42 at 1500 Hz; displayed DT is approximately -0.1 s for FT8 and 0.0 s for FT4 in these fixtures. This checks encoding, GFSK shaping and quantization independently of M.R.S.'s receive library.

WSJT-X wsprd.exe independently decodes the generated WSPR waveform as K1ABC FN42 0, including a principal decode near 14.097102 MHz for a 14.095600 MHz USB dial. The input is synthetic, strong and noise-free; duplicate/secondary decoder candidates do not represent separate stations or RF reception. The late-WSPR I/Q regression separately ensures the long-duration phase-table index remains modulated beyond a 32-bit index boundary.

Three generated transmit WAVs are shipped alongside the existing synthetic samples. They contain no RF capture or private station data. Corresponding native source, original licenses/notices, build script and updated helper SHA256 are included.

## Build and package

The final Release / win-x64 self-contained publish succeeds. NuGet emits NU1900 because its vulnerability-data service cannot be reached; this is an unavailable dependency-audit feed, not a successful vulnerability audit. ZIP CRC, staged/extracted file hashes, final executable identity, helper hashes, source correspondence, public reference inventory and private-data exclusions are checked before delivery. Exact totals and artifact hashes are in MRS-0.44.0-preview1-PACKAGE-CHECKS.txt and SHA256SUMS.txt beside the ZIPs.

Live RF start latency, actual power/frequency/purity, USB drain/restore on the user's HackRF, completed FT8/FT4 QSOs, live WSPR reception, installed JS8Call interoperability, APRS-IS ingestion and aprs.fi display remain unaccepted. See ACCEPTANCE-0.44.0-preview1.md. Native direct-SDR JS8 is still planned; the external bridge is not presented as native support.
