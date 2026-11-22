# Note 17 — shared receive throughput fix, November 22, 2026

User authorized fixing the buffer issue. Latest field clarification: overflow also persists on AC power; battery power is not required. Shared 9.6 MS/s overflows observed on aviation and HDTV, while CW at 2.4 MS/s worked. See REVIEW-FIELD-2026-11-22.md for earlier evidence.

## Changes and limits

The receiver pump performs audio DSP synchronously alongside raw I/Q dispatch, including during wideband decoder monitoring. Its frequency mixer previously evaluated sine, cosine and floating-point remainder for every RF sample (9.6 million times per second at the reported rate). IqDecimator now uses a complex recurrent oscillator with analytic re-anchoring every 4096 samples to bound drift. Phase remains continuous across blocks and resets with a new decimator.

FmDemodulator's three 129-tap FIRs now use mirrored ring histories and padded SIMD dot products instead of scalar loops with per-tap wrapping. Coefficients and filter response are retained. This improves WFM and NFM paths; the shared mixer improves AM/CW/SSB too.

The fix reduces processing demand rather than enlarging queues or silently dropping samples. HackRF backlog overflow remains a hard, explicit discontinuity. No automatic gain/rate changes, device access or changes to recordings. This is a targeted performance correction, not proof that all overflow causes are eliminated. Finite queues can still overflow under sustained overload.

## Local measurements

Same-machine Release benchmarks, original roadmap1 versus updated assembly; synthetic input, no hardware. Roughly one second of 9.6 MS/s I/Q processed through ReceiveDemodulator:

| Path | Before seconds | After seconds |
| --- | --- | --- |
| AM | 0.356 | 0.239 |
| WFM | 0.558 | 0.352 |
| NFM | 0.330 | 0.163 |

Isolated offset decimator, three seconds of 9.6 MS/s input: 0.925 s before, 0.446 s after. Across 2.4, 3.2, 9.6 and 20 MS/s, comparison against the pre-change implementation had maximum output component error below 2.5e-11 for the tested recording-length synthetic blocks. These timings are machine/load dependent, not a field guarantee. Local reproduction harnesses: work/buffer-benchmark and work/receive-path-bench.

New regression tests compare FIR impulse/noise output to an independent scalar reference, oscillator output to analytic phase across re-anchors, and monolithic versus split blocks at integer/fractional input rates. Existing RF/voice/aviation tests remain required. Build output: work/buffer-fix-check. Development compilation is validation only; no new portable ZIP, signing or public release is produced here.

## Field acceptance and remaining follow-up

Test sustained ADS-B and HDTV at 9.6 MS/s with the same device/settings; record duration and Session logs on AC/battery. Continue reporting any explicit overflow. Verify recordings/decoders retain data and ordinary AM/NFM/WFM audio remains correct. No RF accuracy or hardware acceptance claimed from local tests.

Reconnect retry-budget reset after healthy reception and the misleading 2.4 MS/s/HF-filter setup advice remain separate note 17 follow-ups; they were not changed by this DSP optimization. Do not describe them as fixed. The current user installation remains unchanged until a separately supplied test build is run.

Framework-dependent validation initially launched via dotnet reached the HF worker-fixture check and failed to acknowledge its child worker. That invocation does not preserve the standalone worker executable entry point. Final validation uses the self-contained apphost, matching portable operation; do not misreport the initial failure as a passed full suite.

Final self-contained validation: 2262 self-checks passed (including 10 new DSP reference/continuity checks); receive-modes UI smoke passed. Release build completed with zero warnings/errors. Evidence: work/buffer-fix-portable-check/self-test.txt and receive-modes-ui.txt. No hardware operated.
