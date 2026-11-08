# 0.41 preview2 timing acceptance

1. Open Amateur → FT8 / FT4 / WSPR → Timing synchronization. Sync decoder to NIST and verify correction, delay, uncertainty and age. Windows time must remain unchanged. Confirm an unavailable network reports failure instead of a fresh sync. Check periodic refresh, shared receiver-window behavior and stale-reference status.
2. Receive live FT8 and FT4 with the correct USB dial/filter. Compare estimated DT and successful messages with a trusted decoder. Confirm the full-slot wait plus overlap margin. Test received signals close to slot boundaries.
3. Calculate RX offset after multiple stations/slots. Confirm one station alone remains insufficient. With three or more consistent identifiable stations, verify Apply RX estimate and Auto RX timing, adjustment limits and feedback. A bad-clock station should be excluded. This is relative station consensus and includes receiver/input latency.
4. Test retune, source change, mode change, manual offset, Reset timing, Windows clock adjustments and long-running sample-clock drift. Incomplete windows should be discarded/reacquired visibly. No transmitter or clock-setting API is involved.
5. Replay I/Q and generated WAV samples. NIST correction must be excluded; source-time alignment and manual correction should still work. WSPR should continue decoding without contributing no-CRC station timing.

All new live FT timing/long-duration reception acceptance remains pending. A single successful NIST network exchange is recorded separately from RF acceptance. Carry forward ACCEPTANCE-0.41.0-preview1.md and prior receiver/trunk-following checks.
