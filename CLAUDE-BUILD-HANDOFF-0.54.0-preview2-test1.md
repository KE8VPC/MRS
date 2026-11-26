# Claude handoff — 0.54.0-preview2-test1

November 22, 2026. User requested two more updates followed by a portable test build, then this handoff. Read ACTIVE-BASE.md first. Only outputs/MRS is active; older integration/layout copies remain frozen references. Working source was clean at packaging. No GitHub push, release replacement, website/API update or signing setup performed.

## Artifact and provenance

- Portable ZIP: `outputs/MRS-0.54.0-preview2-test1-win-x64.zip` relative to the task workspace root (one directory above this source tree).
- ZIP size: 102,180,954 bytes; 613 entries.
- SHA-256: `2409BE5641901F931F869E60481ABB5AB71F83888FF3B5C4EBBD1E416FE19535`.
- Checksums: `outputs/MRS-0.54.0-preview2-test1-SHA256SUMS.txt`.
- Executable built from commit `27856ca0718650ada85516bcb4714daccd082eb8`; informational version carries this hash. Header and assembly version are 0.54.0-preview2-test1. Subsequent documentation-only commits do not change that artifact provenance.
- Publish/test evidence: `work/mrs-054-preview2-test1-publish`. Clean staging: `work/MRS-0.54.0-preview2-test1-portable`.
- Unsigned private evaluation, explicitly requested by the user; original GitHub roadmap1 assets remain unchanged. This is not fulfillment of note 16's signed public-release requirement. No certificate identity or provider selected.
- Extract all contents into a new folder. Existing drivers/optional backend requirements remain. Different executable folders share per-user M.R.S. settings/library; retain rollback build and back up data.

## Two latest updates to review

### fe6db4a — note 22 prepared TX symbols

Arm previously encoded and discarded symbols; RF worker encoded again during the tight handoff. Now a bounded defensive-copy cache keyed by mode/message prepares symbols during receive and passes them in the existing authorized permit. Worker checks authorization and symbol shape/range. Waveform phase/IQ still uses the actual plan's parameters. Old permits can fall back to encoding. No late-start tolerance widened, no authorization/cancellation removed.

First-burst handoff is four seconds. Repeated FT8 retains 1.3 seconds to preserve reply capture. Logs include handoff/ready time margin; missed-slot diagnostics include lateness and worker setup time. This is a targeted preparation mitigation, not proof of correct physical slot timing. Read VALIDATION-NOTE22-PREPARED-TX.md and review git diff.

### 3bbc063 — note 19 overload evidence

New ADC INPUT card, near-full-scale warning at -3 dBFS with -6 dBFS recovery hysteresis, and I/Q component rail counts. Fox logging measures input quality per processed block; outside logging the existing periodic meter remains. Raw channel power stays unchanged. New points append quality fields to CSV and M.R.S. GPX extensions; legacy unknown evidence stays blank/absent.

Flagged points remain stored/displayed but are excluded from source fitting with explained counts; trend windows containing flagged points become uncertain. High/flat readings across substantial movement can suggest lower gain without diagnosing the cause. Counts refer to the retained measurement window, not guaranteed complete physical capture history. ADC evidence is not a calibrated RF power or analog-front-end health guarantee. Read VALIDATION-NOTE19-OVERLOAD.md.

This implements only overload-awareness step A of PLAN-NOTE19-FOX-GAIN-AND-ELEVATED-SITES.md. No stepped AGC, measured gain correction, elevated-site model, new estimator confidence, peak-point navigation, heat maps or multi-user fusion added.

## Earlier fixes included since public roadmap1

- 43f4b44: recurrent RF mixer and SIMD FM FIRs reduce shared receive CPU/backlog pressure without silent sample dropping or inflated queues.
- d7f317a: receiver channel boundaries reset Aviation acquisition without replacing its target; explicit Tune controls its target.
- f3ab289: shared Tune failure/staging status and validation before scanner interruption. Live Commercial/FLEX-to-Broadcast acceptance remains open.
- 0dfa08b: fresh reconnect budget after 30 seconds of continuous live I/Q; rapid failures still exhaust three attempts; visible exhaustion; correct reduced-rate/HF-filter advice.
- b59924d: independent signal/GPS/logging cards with saved Compact/Large/Extra large choices. Valid NMEA or a valid fix does not imply active point recording.
- 27856ca: test-build metadata/header and version-aware reporting test; release update document.

## Validation actually performed

- Final committed self-contained executable passed **2,310 self-checks**.
- Final executable passed `--fox-tools-smoke`, `--current-ui-smoke` and `--receive-modes-smoke`.
- Fox UI synthetic render inspected, including correct version and fourth ADC card. Broad minimum-size/DPI matrix was not rerun; existing cramped layouts remain open.
- New tests cover prepared cache mutation/invalidation, tone validation/permit transfer, overload hysteresis/counts, raw-vs-channel distinction, CSV/GPX fields and estimator exclusions.
- An initial new CSV assertion incorrectly expected unquoted fields; corrected to respect the existing quoting, then full checks rerun successfully. No export quoting behavior changed.
- Receive microbenchmark at 9.6 MS/s: ProcessBlock ~0.163 s per second of synthetic input without per-block ADC checks, ~0.198 s with them on this machine. Machine/load specific; does not establish laptop stability.
- ZIP executable hash matched final tested publish; required docs/Help present; no generated profiles, raw recordings, GPX, PCM, PDB or screenshots in archive. Public reference bandplans only.
- No live RF/GPS, transmitter operation, hosted API or power-setting change during validation. All user recordings/routes remain outside source/package.

## Field checks and review priorities

1. Sustained ADS-B/HDTV at 9.6 MS/s and receiver logs if overflow persists. AC-power reproduction already reported; battery is not required for failure.
2. Reconnect after a healthy run versus repeated short failures; no automatic recording restart implied.
3. Aviation defaults/manual targets and Commercial-to-Broadcast actual Tune, including stopped/staged states.
4. Fox GPS/no-fix/stale/manual, recording/waiting distinction, size persistence, ADC evidence and exported fields. Actual overload thresholds/performance need hardware acceptance.
5. FT8 first/repeated parity slots and handoff/ready margin logs; a prepared message is not confirmation of transmission or reception.
6. Review these changes through git, not by editing frozen source copies. Preserve the known-source estimator limitations in VALIDATION-FOX-KNOWN-SOURCE-2026-11-22.md.

Still pending: GPS auto-connect/reconnect and sleep resume, run import/append, offline saved analysis/reporting, KML/KMZ signal styling, measured gain segments, evidence weighting/elevated-site awareness, heat maps, sharing, remaining UI/Settings/Help/P25/ACARS milestone items and signing. SSTV remains Martin M1/M2; expanded modes are planned.
