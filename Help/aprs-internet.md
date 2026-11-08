# APRS-IS and aprs.fi reporting
Context: APRS Amateur
Keywords: APRS APRS-IS aprs.fi iGate Internet upload qAO passcode verified reporting

Open Amateur → AX.25 / APRS and expand APRS-IS / aprs.fi reporting. Start live NFM SDR reception, enable AX.25/APRS decoding, enter your iGate callsign/SSID and APRS-IS passcode, then enable reporting. The default server is rotate.aprs2.net TCP 14580. Identity/server settings are saved locally; the passcode stays in memory. Reporting starts disabled after each launch and stops when the source is stopped or changes to replay. No location beacon is generated.

The client waits for a verified APRS-IS login before uploading. A passcode of -1 is read-only at the server and cannot upload; it is not the same thing as a receive-only RF iGate. M.R.S. uses qAO to identify this receive-only iGate. No Internet-to-RF gating or RF digipeating is implemented.

Eligible frames must be valid, fresh, live SDR AX.25 UI frames with PID F0. The original FCS is checked again. Replay, invalid frames, generic queries, TCPIP/TCPXX, NOGATE and RFONLY paths are excluded. Allowed RF third-party packets are unwrapped and checked again; Internet-origin third-party packets are blocked. Original RF body/path bytes are retained, with the receive-only qAO iGate suffix appended. Line length includes CRLF and is capped at 512 bytes; embedded line breaks cannot become server commands.

Duplicates are suppressed for 30 seconds by source, destination and payload, independently of repeater path changes. Queues are bounded; frames older than 30 seconds are discarded. Reconnect uses a bounded backoff and drops disconnected backlog. Server input is never forwarded to RF. Received private/nonpublic traffic is not automatically eligible merely because it appears elsewhere in M.R.S.

Sent counts mean the TCP stream accepted a write. They do not prove downstream delivery or map visibility. aprs.fi consumes APRS-IS; it is not a direct upload API for this feature. Verify your station on the site after an authorized live trial. No live upload was performed during this build's automated tests.

Protocol sources: https://www.aprs-is.net/Connecting.aspx ; https://www.aprs-is.net/IGateDetails.aspx ; https://www.aprs-is.net/q.aspx . aprs.fi API information: https://en.aprs.fi/page/api .
