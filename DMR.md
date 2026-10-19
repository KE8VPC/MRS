# DMR identification preview

Enable Decode DMR identifiers in DMR Channels, start an SDR or I/Q recording, and select NFM with a 12.5–16 kHz receive filter. One tuned channel per receiver is decoded. The broad waterfall is not a simultaneous multi-channel decoder.

Stable rows show frequency, color code, available time slots, and standard full-link-control talkgroup/radio IDs. Validated slot Golay and payload BPTC are required. IDs additionally require the link-control Reed-Solomon check. Unknown fields stay blank. Unsupported bursts are omitted; absence of results does not identify NXDN or prove that a signal is not DMR.

Voice playback, embedded voice link control, trunk following, and proprietary system/site interpretation are not included. MOTOTRBO branding or agency ownership cannot be established from a color code alone.

Auto-save/update is optional and live-only. Three matching observations are required. Existing frequency names and notes are preserved; decoded metadata is added. New frequencies go into dated discovery folders. Replay cannot add live discoveries. The existing scanner can supply the currently tuned channel; allow sufficient dwell to receive headers. Identification is frequency-based, not a unique transmitter identity.

The Data tab retains per-burst observations and inspector provenance. Use DMR Channels for a stable overview. User sound-based labels remain useful annotations.

Implementation reference: ETSI TS 102 361-1 v2.6.1. Golay/BPTC/RS numeric test vectors were independently generated with MMDVMHost reference encoders; no reference encoder source is linked into M.R.S. Local vectors do not substitute for live receiver acceptance.
