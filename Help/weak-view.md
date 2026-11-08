# Weak-signal view, colors and station tracking
Context: Amateur FT8 FT4 WSPR
Keywords: CQ callsign watch ignore highlighting colors SNR dB DT local UTC cycle auto-clear timing

The compact table shows UTC/local time, mode, estimated dB SNR, DT, audio frequency and decoded message. Label/type/CRC/sync diagnostics are kept in expandable Selected decode details and Inspect, not repeated on every row. Time represents the decoded slot when available; saved/exported timestamps remain UTC. Choose Local time to display your Windows local time. All protocol monitoring views now default to newest first; the initial upgrade replaces legacy sort choices, after which your explicit choice is saved.

DT is estimated timing offset relative to the expected mode start. Audio Hz is the decoded base frequency above the USB dial. FT8/FT4 dB uses a decoded-tone signal/noise power estimate normalized to 2500 Hz noise bandwidth; it is not synchronization score. It is approximate and not guaranteed numerically identical to WSJT-X, especially with interference, filtering, clipping or marginal signals. WSPR retains its backend SNR estimate. Missing estimates are blank. Validation/raw helper fields remain available in Inspect and JSON.

CQ is a general call, not a destination callsign. Directed messages normally place destination before sender; grids are Maidenhead location fields, not callsigns. CQ DX and numeric CQ modifiers are recognized. Hashed/unresolved callsigns remain unresolved; free text cannot establish a transmitting identity. A reported grid describes general claimed location, not verified position.

## Display clearing

Auto-clear off retains the bounded recent list. Every cycle / Every 2 cycles replaces the previous rows just before a new completed decode batch, using FT8 15 s, FT4 7.5 s or WSPR 120 s timing. Completed empty cycles also clear an old batch. Reception continues. Tracking history, watch/ignore rules and queued reports are independent. Display clearing removes the bounded displayed message history; export desired rows first. JS8 speed-specific handling remains future work because its decoder is not implemented.

## Highlighting and ignore

Rules / colors configures CQ color and a persistent callsign rule list. Add/edit a row and use Move rule up/down for first-match watch priority. Colors accept names or #RRGGBB; foreground contrast is selected automatically, and selected rows use a separate selection color. Watch matches sender, destination or either. BaseCall explicitly treats portable variants as the same base callsign. Ignore takes precedence over display highlighting; Show ignored reveals retained rows. Unvalidated/unresolved and ignored are different states.

Right-click a decode to watch/ignore/unignore its sender, follow that sender, or return to all senders. Ignore and follow filter the view without stopping decoding or deleting its raw evidence. Watchlist and ignore rules survive restart. Tracked stations opens session history: transmitting callsign, source, mode, dial Hz, reported grid, first/last seen UTC, count and latest message. Destination mentions do not count as transmissions. Tracking is bounded and session-only; Clear tracking history is explicit. Watched calls only filters this history.

## Timing panel

The compact panel shows NIST status, a PC-offset gauge, applied decoder correction, cycle progress and counters. Green means a fresh reference is available, not that the uncorrected PC offset is zero. Replay excludes NIST correction. Timing details / controls expands manual offset, NIST/RX controls and diagnostics. Windows clock is unchanged. New reception after a retune needs a full mode window. Consult Weak timing for estimator confidence and limits.
