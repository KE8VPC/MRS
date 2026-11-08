# PSK Reporter reception reports
Context: Amateur FT8 FT4
Keywords: PSK Reporter reporting upload callsign grid rig antenna Using M.R.S. queued UDP

Reporting is optional and OFF on startup. Open PSK Reporter under the weak-signal controls, enter your receiver callsign and 4/6-character grid, optionally enter rig/antenna information, then explicitly enable while receiving live SDR USB. Identity fields are locked while enabled; disable to change them. Your identity fields are saved, but enabling is never restored automatically. Receiving metadata reports are Internet traffic, not radio transmissions; no RF TX is activated.

Enabling shares receiver callsign/grid, M.R.S. version, optional rig/antenna, and eligible transmitting callsign/mode/signal frequency/time/SNR and reported grid. Blank rig uses a generic selected SDR family, not a serial number. A receiver/device change requires re-enabling. Rig/audio setup descriptions can be edited, but initial upload eligibility requires live SDR with known RF frequency. Service display/acceptance still requires live verification; UDP sent is not confirmation that a map updated.

Only validated live FT8/FT4 decodes with a resolved transmitting callsign, known RF frequency and plausible slot time are queued. The actual RF frequency includes USB audio offset. Replay, generated diagnostic workspaces, unknown-frequency audio, unresolved senders and stale observations are excluded. SNR is optional: missing values are omitted rather than replaced with sync score. WSPR/other-mode reporting is not enabled in this first integration.

Reports queue continuously throughout listening. The queue is independent of FT cycles, display clearing and tracking. Repeated stations are coalesced before sending; sent callsign/mode/band combinations are suppressed for at least five minutes. A band/mode change can produce a separate report. Uploads use a randomized approximately five-minute timer, or earlier when a packet fills. This is neither one report per decode cycle nor one upload at the end of a session.

Rules / colors has a separate Exclude ignored senders from PSK Reporter option (on by default and stated beside Enable). Display hiding alone must not be confused with reporting policy. Eligibility is evaluated when queued; already-sent reports cannot be recalled. Disabling clears pending reports. Failed sends retain bounded, fresh queued records for a later retry; there is no automatic retry flood. Status shows enabled, queued, failed or UDP sent/delivery unconfirmed. Unsent records are session-only and not saved on exit.

Implementation uses the documented IPFIX/UDP protocol at report.pskreporter.info:4739. Templates and receiver software/rig/antenna fields are included. Primary source: https://pskreporter.info/pskdev.html . Local packet/mock transport tests do not establish live PSK Reporter acceptance. No live reports are sent by automated tests.

Each receiver workspace currently owns a separate reporting session. Initially enable one reporting workspace per station/mode/band to avoid duplicate reports from overlapping receivers. Combined multi-receiver report coordination and live service rendering of multiple rigs remain future validation/work.
