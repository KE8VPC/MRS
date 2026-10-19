# OpenITC railroad decoding research - October 16, 2026

Research only; no decoder integrated, downloaded executable run, or release changed.
Reviewed upstream commit b7d3e9d133fe29564ba801eedc6c838f715154f9.

## Scope and fit
OpenITC targets the 220 MHz ITC/PTC radio link. This is a separate protocol family from M.R.S.'s planned 452.9375/457.9375 MHz HOT/EOT telemetry.
The project documents native RTL-SDR reception and offline complex-float I/Q decoding, HR/FR pi/4-DQPSK, synchronization, header FEC, Reed-Solomon and CRC checking. The README also describes a Windows GUI, despite its older no-GUI introduction.

Source review confirms parse_event handles base beacons (ID, coordinates, local/channel list), TIME, CNTL and WIU types 0x33/0x34. Other packet types may be named without full payload interpretation. WIU interpretation uses a typical one-switch/three-signal layout; retain raw indicator bytes and require explicit site profiles before assigning operational names. Base coordinates are not locomotive tracking. CRC success does not establish authenticity; the project documents that it cannot verify the keyed HMAC.

## Proposed M.R.S. approach
Use the existing receiver-owned complex I/Q stream, initially one selected channel plus recorded I/Q. A separate decoder worker with structured output would fit the existing native-worker architecture. Native demodulation accepts complex sample arrays, but MonitorEngine currently creates and owns an RtlSource; this is not a ready-made shared-device adapter. HackRF support would require an input/resampling adapter and tests. Ordinary FM speaker audio is not a substitute for phase-preserving complex I/Q.

Feed packets into Data and the inspector, preserve raw payload/provenance and validation, group status by decoded ID, and expose timestamps/staleness. Add base maps and configurable WIU layouts later. Full-band monitoring should follow single-channel validation and CPU/drop testing, with each requested channel checked against actual capture coverage. The default engine lists 27 channels; do not treat that list as an exhaustive regulatory allocation.

## Engineering review points
- Engine sample rate must be an integer multiple of 192000; channel baseband uses 192 kS/s FR and 96 kS/s HR. Support arbitrary M.R.S. source rates through explicit resampling.
- MonitorEngine reads chunks, processes each channel/rate and calls its consumer on the worker thread. Check buffering, burst boundaries, filter state, CPU cost and cancellation before integration. A failed decode must not block reception/UI.
- PacketEvent retains decoded payload and sync-error count, not a complete received/corrected bit history. Extend provenance explicitly for the inspector rather than labeling decoded payload as original RF bits.
- Parse validity flags largely follow length checks. Independently validate dates, coordinates and allowed channel values; do not equate parsed fields with semantic validity.
- Current CMake requires RTL-SDR, ZeroMQ, GLFW and SQLite; it also builds a GUI and uses Windows libraries. Build a minimal worker target instead of bringing its GUI/map/database into M.R.S.
- README identifies GPL-2.0-or-later. Before distributing reused code/binaries, review exact file/dependency terms and provide required corresponding source, notices and build instructions. A separate process is not an automatic license exemption.
- Upstream how-itc-works has a quantitative inconsistency: 16 kbit/s for 30 ms is 480 bits (60 bytes before overhead), not roughly 500 bytes. Treat narrative documentation as a research lead, and validate against technical references and captured fixtures.

## Validation needed
Known-good independent recordings, generated framing/error fixtures, live local reception, HR/FR coverage, weak-signal behavior, rate conversion, chunk-boundary bursts and long runs. This research establishes feasibility, not M.R.S. compatibility or universal PTC decoding.

## Sources
- https://github.com/SarahRoseLives/OpenITC
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/src/engine.cpp
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/src/itc.h
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/src/model.h
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/src/parse.cpp
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/CMakeLists.txt
- https://github.com/SarahRoseLives/OpenITC/blob/b7d3e9d133fe29564ba801eedc6c838f715154f9/docs/how-itc-works.md
