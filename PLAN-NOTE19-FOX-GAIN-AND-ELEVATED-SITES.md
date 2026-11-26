# Note 19 plan addendum - gain handling, overload and elevated sites

November 22, 2026. Claude, recording discussion with the user so Codex can plan implementation. Planning and documentation only: no code, build or release change. The evidence comes from the known-source test in `VALIDATION-FOX-KNOWN-SOURCE-2026-11-22.md`. No coordinates, routes or serials are recorded here.

These proposals extend note 19. They do not replace its existing requirements: qualitative evidence labels, no invented confidence percentages, raw values preserved, segments flagged when settings change, and no silent inference of missing measurements.

## Discoveries from the known-source test

1. **Elevated-antenna ring effect** (user insight, confirmed in data). Driving 192 m from a tower site gave a median level of -15.6 dB within 300 m, the same as 1-1.5 km out. The run maximum came elsewhere, and the level only fell beyond about 2 km. A plain log-distance fit was pulled about 0.7 km (one run) to 1.6 km (both runs) toward the strong ring. Height and elevation-pattern terms could not be identified from these routes.
2. **Overload near the site** (user insight, partly confirmed). Morning-run instantaneous peaks reached -0.4 dB of full scale, and its averaged levels reached -5 dB. There was no hard pile-up (11 of 1,326 points within 3 dB of the maximum), so overload likely flattened the top of the range without explaining the whole flat zone. The afternoon run, with about 10 dB less gain, peaked at -11.7 dB.
3. **The noise-floor estimate tracks the signal** (correlation 0.69 and 0.79 across the two runs). Strong signal appears to leak into the noise measurement, so SNR near a strong source is overstated or unreliable.
4. **Gain differences forced free per-run offsets.** With nothing linking the two gain settings, the fit could not compare levels across runs. The far, noisy afternoon run (739 of 1,007 cells, none within 1.3 km, 8.6 dB residual) outvoted the morning run that passed the site.
5. **The strongest single reading was 2.4 km from the site.** The ±0.5/1 dB ambiguity zones excluded the site; only the 2 dB zone contained it.
6. **Duplicate exports.** Two of the three GPX files were byte-identical. Import and resume should detect duplicate samples (note 19 already requires deduplication).

## Proposed features

### A. Overload awareness (small; do first)
- Show an overload indicator when instantaneous peaks approach full scale, with a hysteresis threshold near -3 dBFS, plus a clipped-sample count per interval.
- Mark overloaded samples in the log and the GPX. The estimator should exclude or down-weight them, and say that it did.
- Show a "readings flat, try lower gain" hint when the level has varied little while the position moved substantially and the peaks are high. Phrase it as a suggestion, not a diagnosis.

### B. Stepped automatic gain (AGC), accounted for in calculations
- **Stepped only.** Use the HackRF's real steps: LNA 8 dB, VGA 2 dB, RF amp on/off. Never change gain continuously.
- **Target window with hysteresis.** Step down when peaks come within about 10 dB of full scale. Step up only when well below that, and enforce a minimum dwell of several seconds between changes. Never switch during a burst.
- **Measure each step instead of trusting nominal values.** Steps are not exact and vary with frequency. With a continuous carrier, measure the same signal in windows of a few hundred ms immediately before and after each switch; the difference is the actual step at that frequency. Record the step's uncertainty from the before/after window scatter.
- **Burst-only sources.** Switch only while the carrier is present, or fall back to bench or nominal values with a wider stated uncertainty. Unknown steps must remain unknown.
- **Record per sample:** raw level, gain state, measured step, step uncertainty and a segment ID. Calculations use an input-referred level (raw minus measured gain change). Raw values are never overwritten.
- **Noise floor and SNR thresholds are per gain state**, because the noise floor moves with gain.
- **Estimator.** Each gain segment gets an offset tied to its measured step within the step's uncertainty. A segment with an unknown step gets a free offset, and the UI says so. This keeps cross-segment geometry that free offsets discard (discovery 4).
- **Map.** Mark gain changes on the track, so a colour jump is never read as a signal jump.
- **Manual changes during a hunt** use the same measurement path whenever the carrier allows it.

### C. Elevated / known-site awareness
- An optional, labelled input: "suspected elevated fixed site", with an approximate antenna height if known. It is never a silent assumption.
- Recognise and display the ring signature: strong readings around a quieter centre, or a level that stays flat over a wide moving distance. When it appears, do not place the estimate on the ring. Report it as an ambiguity ("possible elevated antenna; the centre of the strong area is a candidate") instead.
- If height and pattern terms are offered, show the per-setting results and how well each fits. Never choose a setting because its location looks plausible.

### D. Evidence weighting and guidance
- Weight runs and segments by evidence quality and proximity, not by cell count. Show per-run estimates side by side, with a combined estimate only when they agree.
- **Guidance must never point at the peak reading** (discovery 5). Suggest closing coverage gaps instead (here, west and south were unsampled).
- Near-site guidance should recommend changing the observation, not refitting:
  - a directional antenna or body shielding for bearings
  - an attenuator or lower gain
  - circling the candidate area on all sides
- Ambiguity zones were overconfident by about a factor of two in this single test. Keep them qualitative and validate them against more known sources before any wording implies reliability.

## Suggested order and acceptance
1. **A** (overload indicator, flat-reading hint, overload marking). Acceptance: synthetic clipped and unclipped replays; peaks near full scale raise the indicator, and the hint stays quiet while level varies.
2. **B** (stepped AGC with measured steps). Acceptance:
   - synthetic continuous carrier with known injected steps: measured steps within ±0.5 dB
   - bursty source: falls back with a wider uncertainty
   - no oscillation under a fluctuating level (dwell plus hysteresis)
   - the raw/input-referred/segment fields survive GPX export and import
   - the estimator uses tied offsets
   - hardware acceptance stays separate
3. **D** (weighting and guidance), alongside note 19's partial-estimate and direction work.
4. **C** (elevated-site display), using this known-source case as a replay test if the user permits storing it.

Record any further known-source hunt like `VALIDATION-FOX-KNOWN-SOURCE-2026-11-22.md` before the evidence labels are tuned.
