# M.R.S. 0.41.0-preview1

Adds receive workspaces for SAME/EAS headers, DTMF, MDC1200 and FleetSync under Public Safety / Business; FT8, FT4 and WSPR under Amateur; and experimental direct-SDR Meshtastic/MeshCore under Analysis. Existing 0.40 receiver assignments, PPM, FLEX display and Help improvements are included.

Extract the complete ZIP into a new folder. Keep the companion Decoders, Help, Receivers and bandplans folders. Preserve your existing user data and recordings. This release does not seed local systems, talkgroups, private channels or local frequency lists.

See Help/signaling-alerts.md, Help/weak-signal.md and Help/mesh-sdr.md for operation and limitations. Mesh uses raw I/Q; audio decoders use receiver/audio input. FT modes require complete timed windows; WSPR takes two minutes. Private mesh payloads stay encrypted. No transmission or automatic spot/network upload is added.

Generated-signal, replay and package checks are separate from live RF acceptance. Use ACCEPTANCE-0.41.0-preview1.md for testing and report the source, tuning/filter parameters, protocol, status and redacted/raw sample where appropriate.
