# Claude review - 0.54.0-preview2-test1 source changes

November 22, 2026. Answers `CLAUDE-BUILD-HANDOFF-0.54.0-preview2-test1.md`. This review covers the git diffs of 43f4b44, d7f317a, f3ab289, 0dfa08b, b59924d, fe6db4a and 3bbc063. It is a code review only: Claude did not rerun the build or the self-tests, and did no hardware testing. No source was changed.

## Verdict
All seven changes are scoped to their notes, keep raw evidence, and add tests that would fail on the old behaviour. Claude found no correctness or safety defects. The findings below are a performance interaction, a small UI-thread issue, and open items to keep visible.

## Per change
- **43f4b44 (note 17, DSP).**
  - The FIR now uses a mirrored double-length history with zero-padded coefficients. The vector reads beyond the duplicated region only touch zero coefficients, and the newest-first ordering is preserved.
  - The mixer uses a rotation recurrence that re-anchors to the exact phase every 4,096 samples.
  - Both are checked against independent scalar or analytic oracles (1e-12 and 1e-9) across wraparound and re-anchoring. Correct.
- **d7f317a (note 8).** `ChannelChanged` now calls `Aviation.Boundary` instead of `Follow`, so receiver changes no longer overwrite the target. Explicit Tune retargets a running pipeline. The test covers presets, a manual target and a mode change. This fixes the root cause Claude identified.
- **f3ab289 (note 1).** Tune now validates before it interrupts a scanner or trunk assignment. Every refusal goes to the shared status bar as "Tune not applied: <reason>", including the new awaiting-first-samples case, and stale results are suppressed with the session check. Good.
  - Remaining: when a Tune *does* stop a scanner, folder scan or trunk follower, check that the status says so. If it doesn't, add it.
- **0dfa08b (note 17, recovery).** The health check uses monotonic I/Q arrival with a 1.5 s gap tolerance, and needs 30 s of sustained reception to refill the budget. Rapid failures still exhaust it, and exhaustion is visible. The ADS-B 2.4 MS/s advice is now accurate.
  - **Still open from REVIEW-CLAUDE-FIELD:** a backlog overflow is still treated as a lost device. M.R.S. reopens at the same rate, overflows again within seconds and burns the budget. Classify overflow separately and offer a lower rate (or an intermediate rate) instead of a same-rate reconnect.
  - Per-consumer queues off the read loop also remain open. The DSP speed-up treats the symptom; field data will show whether it is enough.
- **b59924d (note 19 indicators).** Separate trend/GPS/logging cards, with "valid NMEA is not a fix is not recording" kept distinct. Matches the plan.
- **fe6db4a (note 22).**
  - Authorization is still re-read at RF start, and the 80 ms late-start limit is unchanged.
  - Prepared tones are validated against the plan's length and alphabet both when the permit is issued and in the worker. The cache returns defensive copies.
  - Older permits fall back to encoding. Safe.
  - **Minor:** `WeakWaveform.Encode` runs inside the transmit window's display timer (the UI thread) while holding the cache lock. The first preparation of each new message launches `mrs-weak.exe` and waits synchronously, normally briefly but up to the 10 s timeout. Arm already behaved this way, so this is not a regression, but preparation now repeats on every new message. Suggestion: prepare on a background task, and show "Preparing…" until it finishes.
- **3bbc063 (note 19 overload).**
  - The hysteresis is -3 dBFS to raise and -6 dBFS to clear. Legacy points stay blank.
  - Flagged points stay displayed but are excluded from the fit, with a count. The trend shows "uncertain" when a window contains a flagged point.
  - The flat hint requires overload evidence, at least 500 m of movement and less than 2 dB of spread. It matches plan step A.

## Interaction to address: Fox Hunt at 9.6 MS/s
- **Extra load on the stage that overflowed.** The user's known-source GPX runs were logged at **9.6 MS/s** (a 9 MHz capture for a 12.5 kHz channel). Per-block ADC checks add about 21% to ProcessBlock at that rate (0.163 to 0.198 s per second in Codex's benchmark). That load lands on the same read loop that overflowed in note 17.
- **Wideband ADC evidence.** At 9.6 MS/s, the ADC evidence describes the whole 9 MHz capture. A strong signal anywhere in it can raise "HIGH INPUT" and exclude points even when the target channel itself is fine. Only clipping of the shared ADC truly corrupts every channel. A near-full-scale peak without clipping is a conservative exclusion, not proof of corruption.
- **Suggestion:** when Fox Hunt starts at 9.6 MS/s or above, recommend 2.4 MS/s for narrowband targets, showing the reason (CPU load, and out-of-band signals affecting the overload evidence). Consider excluding only clipped points from the fit, and down-weighting high-but-unclipped ones, with the counts shown either way.
- Do the hardware overload acceptance at the rate users will actually hunt with.

## Not reviewed
27856ca (version/metadata) beyond the file list, the packaged ZIP contents, and the minimum-size/DPI layouts. As the handoff says, live RF, GPS, FT8 slot timing and overload thresholds need station acceptance.
