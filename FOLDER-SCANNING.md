# Saved-folder scanning — 0.28.0-preview1

Start a live SDR receiver, open Frequencies, right-click a saved folder (for example Railroad), and choose **Scan folder**. The scanner cycles through a snapshot of its saved channels in list order, using each channel's frequency, modulation and receive filter. A zero/legacy filter uses that mode's default. Unsupported entries are skipped and counted in the session log. Labels do not turn on protocol decoders automatically.

The collapsible **Saved-folder scan controls** offer:
- Include subfolders (on by default).
- Skip hidden entries (off by default; marker visibility remains independent unless selected).
- Dwell: seconds to listen after tuning/settling (default 1.5).
- Quiet: seconds after the last activity before advancing (default 1.5).
- Max hold: advance even if a control channel remains active (default 15).
- Hold current channel, Skip to the next channel, and Stop scan.

The existing RF threshold determines activity. Scanning enables the RF squelch gate; set its threshold in Receiver, or enable receiver auto squelch before starting. Auto squelch needs additional calibration time after capture/mode changes. Dwell starts after settling/calibration. Manual Hold overrides maximum hold until released. Skip advances now; it does not delete or permanently exclude a channel.

The folder name, channel name, index, frequency, mode, filter and state appear below the controls. Hidden parent folders are honored when skipping hidden entries. Library edits take effect on the next scan start. Frequencies are not discovered, added, renamed or modified by this scanner. Unsupported entries and empty folders produce explicit messages.

Folder scanning and fixed-span activity discovery are mutually exclusive. Manual tuning, receiver Stop, changing mode/filter/gain/rate, receiver loss or closing stops the scan. It never restarts automatically. Tuning uses the existing receiver pipeline and preserves HackRF capture-offset behavior. Channels outside the current capture move capture center. Missing fresh RF data or tuning timeout stops scanning instead of silently advancing. Replay scanning is unavailable.

Live RF acceptance remains pending. Synthetic tests cover traversal, exclusion, snapshot stability, dwell/quiet/max-hold/manual-hold timing; UI smoke checks reject starting without live hardware. RF thresholds and tune latency depend on receiver/rate and local signals. This is conventional saved-channel scanning, not trunked voice following.
