# Acceptance — 0.31.0-preview1

## DMR status — October 24, 2026

**DMR voice: confirmed by user.** The user heard decoded voice. Slot-selection variants and other individual checks are not separately inferred from this report. **Overall DMR sign-off remains pending completion and acceptance of trunk following**, as explicitly requested by the user. Current tuned-channel voice reception does not establish trunk-following capability.

Carry forward [the previous acceptance checklist](ACCEPTANCE-0.30.0-preview1.md), including workspace, P25 audio, frequency-list, scan and receiver checks not yet completed.

1. Tune a known clear DMR voice channel, NFM, 12.5–16 kHz filter. Select DMR in DMR / LTR / NXDN. Confirm intelligible speech; try Slot 1, Slot 2 and Both. Confirm independent digital voice gain and normal receiver volume.
2. Leave CW station ID enabled on a repeater that actually sends an audible Morse ID. Confirm its candidate appears in Data with the correct frequency. Verify by ear before using it as a label. Check digital voice resumes after the ID.
3. Test known NXDN 6.25 and 12.5 kHz channels separately, selecting the corresponding mode. Confirm clear voice and compare reported RAN/IDs against a known source. Try inverted polarity only if needed. Do not treat CRC ERR or noise sync as a verified channel identity.
4. Retune during speech, stop/restart, change DMR/NXDN/P25 modes, and seek I/Q replay. Confirm no stale speech or identifiers carry into the next channel and no continuing helper audio after stop.
5. Check CTCSS/DCS only above Spectrum; no tone observations/controls in Data. Confirm the existing P25 and paging tabs still work.
6. Watch dropped count during a longer session. Report mode, receiver rate, RF filter, slot and last diagnostic lines if decoding fails. A control-only channel may provide identifiers without any voice.

Known boundaries: single tuned channel; no new trunk following, key input, automatic NXDN labels or callsign auto-association. Digital voice selections start off on launch. APRS TX gain/start issue remains deferred.
