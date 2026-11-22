# Claude review request — field reception and decoder evidence

November 22, 2026. Documentation only. User requested notes and reference data for Claude feedback. Raw recordings and derived sample files remain outside the repository and release inputs. See local `work/field-review-2026-11-22.md` for paths, hashes and reproduction scripts. Do not publish intercepted payloads, device serials, routes or credentials. ACTIVE-BASE.md controls edits.

## Sensor/burst investigation

User's goal is any valid sensor/remote/meter decode, not specifically TPMS. Forget the earlier TPMS attribution; unidentified band activity is not protocol identification.

Two complete user MRSIQ v3 recordings were tested read-only with the packaged roadmap1 `mrs-iq-channel.exe` and `rtl_433.exe` (reports version 25.12):

| Recording | Duration | Rate | Capture centre | Selected channel | Tests/result |
| --- | --- | --- | --- | --- | --- |
| bursts6.mrsiq | 18.507 s | 2.4 MS/s | 434.170 MHz | 433.920 MHz | Sensors default protocols, channel-corrected 1.2 MS/s, gain 0/+12 dB: zero decoded records |
| 911bursts.mrsiq | 18.220 s | 2.4 MS/s | 911.850 MHz | 911.600 MHz | Sensors default protocols at 1.2 MS/s and selected meter protocols at 2.4 MS/s, gain 0/+12 dB: zero decoded records |

Both headers report unsigned 8-bit I/Q despite HackRF source; extraction follows the recorded format. Capture width is 1.75 MHz in each file. Capture-to-channel shift is -250 kHz; bridge input uses unsigned flag 0. Float bridge output is quantized using the same scaling as SensorPipeline.DecoderInput. Neither original was rewritten. Helper/decoder exits were zero, which does not establish RF/protocol correctness.

For the 911 recording, rtl_433 pulse analysis detected activity and generated modulation guesses, but no validated packet. These guesses and generic envelope/audio-bin indicators must not be represented as decoded messages or TPMS proof. No custom flex decoder was installed or claimed working.

Screenshot profile was Burst / tone inspection: SensorPipeline skips rtl_433 entirely for this profile. Sensors / remotes enables default decoders; Utility meters selects IDs 149,154,160,223,228,282,290. Merely selecting the lower result tab does not change the running profile. This UI distinction may warrant clearer feedback.

### Requested Claude cross-check

1. Review the extraction/channel shift, sample format and quantization independently; inspect local scripts and diagnostics before trusting the negative results.
2. Validate a known supported independent recording through the normal M.R.S. replay/worker/parser/table path, with an expected known decoded record. This remains pending; do not claim current end-to-end sensor acceptance.
3. Compare against direct decoder output to isolate processing/integration versus unrecognized/corrupted signals. Consider defaults versus disabled protocol coverage only with evidence; do not blindly enable everything.
4. Assess spectrum/centering/filter/decimation and burst thresholds. Spectrum trigger and Burst margin are not equivalent to protocol decoder sensitivity. Raw activity alone is inconclusive.
5. Return evidence-backed feedback before proposing code changes. A known source is more diagnostic than repeated random-band gain adjustments.

## Note 17 — stronger receiver-backlog evidence

User logs explicitly show HackRF receive backlog overflow at 9.6 MS/s on ADS-B 1090 MHz, aviation 136.975 MHz and HDTV testing 479 MHz. Receiver actually stops. Reconnect reopens the same receiver but overflow repeats and three retries exhaust; the initial impression that reconnect never ran is superseded.

Relevant supplied log times are UTC: 20:53:29/33 overflows at 136.975 MHz; reopened 20:53:39. ADS-B 21:07:35 overflow, retry 3, reopened 21:07:46, overflow 21:07:56 and limit reached. Investigate whether a healthy period should reset retries; the log has a roughly 13-minute interval but does not prove uninterrupted reception throughout it.

HDTV 479 MHz at 9.6 MS/s: reopened 21:17:10, overflow 21:17:14; reopened 21:17:20, overflow 21:17:27; reopened 21:17:38, overflow 21:17:44, retry limit. Log reports WFM audio mode; review consumer workload without assuming TV receives demodulated rather than raw input.

User reduced CW to 2.4 MS/s: decoding worked and observed dropouts ceased. This is field-reported acceptance with imperfect characters, not calibrated decoder accuracy. A 15-minute Fox Hunt run did not stop. Map online fetching was off in a failing ADS-B screenshot. Lower LNA/VGA and AM/NFM did not solve failure; changing screen/sleep timeouts also did not solve it. Laptop was on battery.

ADS-B fallback 2.4 MS/s fails setup with 1.75 MHz usable versus 2 MHz required; HF hardware filter was explicitly already disabled. Investigate capture-bandwidth calculation and misleading advice rather than repeatedly asking the user to disable it.

EVTX contains Modern Standby and HackRF USB power/re-enumeration events around 15:42 EST. User requested assuming correlation without confirmation. These are real separate power events, not proof they caused all stops. Explicit backlog logs supersede a sleep-only explanation. No more repeated field reproductions needed before source investigation.

## Note 22 — FT8 missed transmit slots

Several attempts report TX stopped: Missed digital slot; no transmission started. Add scheduler/preparation/timing-source and RX-to-TX handover diagnostics; compare processing pressure without assuming same cause as receive overflow. Proposed mitigation: pre-generate/cache message waveform when message/parameters change, invalidate stale preparation and show TX ready only when complete. Never start a late mistimed burst. Preparation bottleneck is unconfirmed. This is a new active numbered item: 21 active items, notes 1–22 excluding rescinded 3.

## Note 19 — additional presentation/analysis proposals

Large high-contrast trend indicator: stronger/steady/weaker/paused/uncertain, adjustable sizing/colours with symbols and text, smoothing/hysteresis and optional audio cues. Separate trend from model direction. GPS light must distinguish not started, connected/no fix, fresh fix, stale/disconnected/error and manual position; recording gets its own indicator. Valid NMEA does not prove valid position or logging.

For cooperative localization, compare independent per-group candidate-score areas and agreement rather than averaging estimated coordinates equally. Preserve equipment/settings groups and duplicated-data lineage. Later joint fitting/group offsets require validation; shared environmental bias prevents equating user count with confidence.

Latest afternoon hunt rejection was an actual model-fit rejection; user deliberately stopped tracking before screenshot. Paused/historical status is expected, not dropout evidence. Current fit threshold rejects best robust residual score above 4 dB. Partial candidates, uncertainty views and thresholds remain plans, not shipped features.

No application code, build, signing, hosted upload or release change was performed for this review note.
