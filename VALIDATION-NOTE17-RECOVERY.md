# Note 17 — reconnect health and bandwidth diagnostics

November 22, 2026. Follow-up to shared DSP optimization. Development source only; original public roadmap1 package unchanged.

## Reconnect policy

Previously recoveryAttempts persisted until a new manual receiver start. The field log showed a later failure consuming attempt 3 after an earlier successful reopen. Now a monotonic live-I/Q arrival tracker qualifies a fresh retry budget after 30 seconds of reception without gaps over 1.5 seconds. This is input-delivery evidence, not evidence of successful decoding or good RF.

The tracker resets on Stop/session cancellation. Short reopen/fail cycles do not reset retries; device-open acknowledgement alone does not qualify. Manual Stop and explicit reconnect disabling retain existing behavior. The next failure after qualified reception starts a fresh three-attempt sequence and logs why. Rapid repeated failures still stop after three attempts. Exhaustion now appears in shared receiver status as well as the log. Recordings are still not restarted automatically.

Synchronization: the I/Q callback only updates a locked tracker; it does not touch WPF controls or mutate retry counters. End handling reads the tracker before Stop resets it. Existing same-device identity matching and no-substitution rules remain unchanged.

## Reduced-rate diagnostic

HackRFProtocol explicitly defines 1.75 MHz usable bandwidth at 2.4 MS/s, even when the optional HF filter is disabled. This is not a new accidental filter setting. ADS-B requires a 2 MHz span, so that rate remains unsupported for ADS-B in the current adapter. AviationPipeline now explains the rate limit rather than telling the user to disable an already disabled filter. Wider 9.6 MS/s capture remains accepted. No rate choices or capture bandwidths were enlarged or guessed.

## Verification

New tests exercise no-data, short run, 30-second qualification, gap restart, subsequent qualification, backwards test time and explicit reset. Diagnostic tests check reduced-rate explanation and accepted wide ADS-B capture. Existing discovery/reconnect identity, cancellation and retry-limit tests also run in the full suite.

Evidence: work/recovery-followup-check/self-test.txt and receive-modes-ui.txt. Local development compile/publish and UI checks are separate from hardware recovery acceptance. No physical receiver, power setting, signing provider or hosted service was operated.

## Field acceptance

Verify a healthy run longer than 30 seconds followed by a failure receives a new budget, while repeated short failures still stop. Confirm status explains retries and exhaustion. Test manual Stop, reconnect off and same-device recovery without restarting recording. A complete gap still requires decoder reacquisition; this policy does not hide lost samples or guarantee that persistent overload recovers.

Final validation: 2274 self-checks passed, including nine new health/diagnostic checks; receive/tuning UI smoke passed. Self-contained development publish completed. Hardware acceptance remains open.
