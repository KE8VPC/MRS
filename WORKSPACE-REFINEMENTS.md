# Workspace and P25 audio — 0.30.0-preview1

## P25 voice gain

P25 Systems now contains P25 Phase I data and P25 I voice switches plus a separate voice gain slider. Default +12 dB (about four times the sample amplitude), adjustable 0–30 dB in 1 dB increments or with the mouse wheel. A smooth limiter compresses peaks above 24,000 PCM units toward a 30,000-unit ceiling. Gain is saved with receiver preferences and affects decoded P25 voice only. Existing output volume still applies. This improves level, not weak-signal decoding or intelligibility. Encrypted/unknown-state calls remain muted; no AES key support is added.

## Spectrum workspace

The header is one compact row. The static RX/manual-TX banner and redundant spectrum heading are removed. CTCSS/DCS enable and status are on the left of the spectrum status strip; audio level is on the right. Tone observations are no longer listed in Data.

Drag the horizontal six-pixel bar just above the waterfall to resize the spectrum/waterfall split. Its ratio is stored in display preferences, including per-receiver profiles. Minimum/maximum proportions retain both plots. The divider does not tune or pan. Frequency and time axes remain.

Only matching enabled band-plan layers consume space. Compact mode uses at most two rows; hover for overlapping references. Right-click within the RF plot to show/hide all strips or expand band-plan details. Per-service toggles remain in Display. Dense labels may be omitted rather than overlap; hover retains applicable details. Expanded rows are bounded to retain plot space.

Permanent footer instructions/statistics are removed from beneath the waterfall. Plot help explains gestures. Right-click the RF plot for bin resolution/history statistics. Existing navigation status retains span, center and filter details. CSV plans are not implemented in this release.

## Decoder organization

POCSAG / FLEX is a dedicated tab with its own decoder switch, grouping, capcode/source labels, ignore/restore rules, wrapped messages, inspector, filters and JSON export. Existing paging-rules.json is reused. No user labels are reset.

P25 data is routed exclusively to P25 Systems. The upper pane retains the saved live catalog and separate session replay catalog, system/site/channel discoveries, annotations, frequency review and export. Selected-system details are collapsed initially. Drag the main horizontal splitter to allocate height.

The lower pane has Channel activity and Messages / inspector tabs. Channel activity shows validated grant observations with resolved frequency when available, decimal identifier/channel number, talkgroup, radio ID, encryption service flag, source and last-seen time. Rows are scoped by receiver source/session, tuned channel, identity, channel and slot. Missing identity/frequency stays unknown. After five seconds without a new grant, or on source boundaries, rows are marked stale. This is grant activity, not a guaranteed call-duration or audio-following indicator. At most 500 rows are retained.

TG label and Radio label cells can be edited directly and are saved separately in p25-activity-labels.json per receiver, keyed to decoded identity and ID. Labels for provisional identities remain separate from later fully identified systems. Existing catalog names/notes remain untouched. Unitrunker label import remains future work. The main P25 pane split is adjustable for the open window; spectrum ratio and voice gain persist.

P25 Messages / inspector retains grouping, message-type visibility, filtering, full fields/raw bytes and JSON export. The general Data tab retains AX.25/APRS, CW IDs and other existing shared observations. P25/paging decoder preferences and paging rules migrate through their existing settings fields.

## Scope

No new RF protocol, AES decryption, Phase II, CQPSK/LSM support, or trunk following is introduced. Existing APRS TX gain/start issue remains deferred. Live gain/listening and interaction acceptance are required in addition to local synthetic checks.
