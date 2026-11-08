# JS8Call bridge
Context: Amateur JS8
Keywords: JS8 JS8Call bridge TCP API directed messages external backend transmit Normal

Amateur → JS8Call bridge connects only to a JS8Call TCP API on this computer (default port 2242). Install and configure JS8Call separately, enable its TCP API and configure its actual receiver/transceiver/audio connection there. No JS8Call executable is bundled. M.R.S. does not route its HackRF I/Q into this backend; native direct-SDR JS8 receive/transmit is still planned.

Connect explicitly. Received RX.ACTIVITY and RX.DIRECTED events appear in the M.R.S. monitoring view as external backend observations, not independently validated M.R.S. decodes. A missing connection, malformed event or disconnect is shown. There is no automatic network connection on startup.

The one-message transmit bridge requires a JS8Call 3.x backend with its Halt API. Only Normal speed is enabled for this first sending workflow. M.R.S. queries fresh version, callsign, frequency/offset, mode, PTT, text and transmit queue state before a send. Enter your matching callsign and license class, and explicitly confirm station authority, local permissions and equipment compliance. A conservative U.S. data-segment check applies to the backend's reported frequency. The backend must be idle with an empty text/queue. A valid-looking callsign is an operator declaration, not independent verification.

Send one message passes up to 80 printable characters to JS8Call for its next transmit cycle. JS8Call owns encoding, framing, time synchronization, radio/PTT and delivery. M.R.S. does not enable heartbeat, automatic replies or repeated messages. Halt requests the backend's TX stop and clears text; a 90-second watchdog requests Halt after a submitted message. API failure means external RF state may be unknown: use JS8Call or the radio's own stop control. Do not manipulate another radio application while a message is queued. Longer messages may be cut off by the watchdog.

The included automated check uses a local mock API server only. No installed JS8Call/radio interoperability or live JS8 transmission is claimed. Upstream software, releases, API and licensing: https://github.com/JS8Call-improved/JS8Call-improved ; https://github.com/JS8Call-improved/JS8Call-improved/blob/master/docs/API.md .
