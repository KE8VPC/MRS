# M.R.S. 0.41.0-preview2

Adds decoder-only NIST time synchronization and received-station timing estimation to the current decoder preview. Expand Timing synchronization under Amateur → FT8 / FT4 / WSPR.

Use Sync decoder to NIST for a measured reference; enable Auto NIST refresh for periodic checks. Calculate RX offset shows a relative multi-station estimate. Apply RX estimate and Auto RX timing require sufficient consistent CRC-valid evidence. Manual offset and Reset timing remain available. Windows time is unchanged.

FT8/FT4 now use overlapping capture margins, wider timing search, approximate DT with software-delay compensation, ongoing live timeline tracking and explicit reacquisition after corrections or discontinuities. Replay timing stays separate from live NIST time. Native helpers and companion folders are required; extract the complete ZIP into a new folder and preserve your existing user data/recordings.

Includes all preview1 decoders: SAME/EAS, DTMF, MDC1200, FleetSync, FT8/FT4/WSPR and experimental direct-SDR Meshtastic/MeshCore. See Help/weak-timing.md and ACCEPTANCE-0.41.0-preview2.md. Fox Hunt (GPS signal-strength mapping) is queued for a future update and is not included here.
