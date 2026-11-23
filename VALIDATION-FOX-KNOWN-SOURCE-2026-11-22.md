# Fox Hunt known-source accuracy test - November 22, 2026

Claude analysis for note 19. The user ran a blind test: he exported Fox Hunt GPX from two drives on a known fixed 700 MHz transmitter, asked for a location estimate, and only then revealed the true site. Documentation only. This note deliberately contains no coordinates, routes, device serials or the user's home location. The GPX files and analysis stay outside the repository.

**Correction.** An earlier version of this file (commit d49f425) reported a 31 m error. That was wrong. The "true" coordinates used then were Claude's own estimate, converted to degrees-minutes-seconds and passed back. Claude compared the estimate with itself and did not notice. The user then marked the real site on Claude's map. That position was registered to the original plot (template match 0.975) and read to about ±50 m. All figures below use it.

## Input
- Three GPX exports, two of them byte-identical, so there were two real drives.
  - Morning run: 23 min, 1,326 points.
  - Afternoon run: 44 min, 1,269 points.
- Squelch was open on every point, every GPS fix was at most 1 s old, and there were no time gaps over 10 s.
- **The gain tokens differ between the runs** (noise floors about 10 dB apart), so each run was fitted with its own offset.

## Method (offline, outside M.R.S.)
- Points were averaged into 60 m cells per run.
- Model: level = run offset - 10·n·log10(distance), with n fitted within 1.5-5.
- Grid search over 24 × 24 km in 250 m steps, scoring by robust residual.
- Ambiguity zones: grid points within +0.5, +1 and +2 dB of the best residual.

## Result
| Estimate | Error to true site |
| --- | --- |
| Both runs combined | **1,681 m** (site lies about 320° from the estimate) |
| Morning run alone | 848 m (n pinned at its 1.5 lower bound) |
| Afternoon run alone | 1,083-1,331 m (depending on fit variant) |
| Strongest gain-normalized reading | 2,609 m |

- The true site is **outside the +0.5 and +1 dB zones and inside the +2 dB zone**. It scores +1.21 dB, and 193 of 9,409 grid points (about 12 km²) fit better.
- The zones were overconfident by roughly a factor of two.

## Why: the elevated-antenna effect, confirmed in the data
- The user pointed out that an elevated antenna with a ground plane or downtilt directs energy outward, so reception right below it can be weaker.
- The morning run shows this. Its median level is roughly flat from 0 to 2 km (-15 to -20 dB), and its strongest readings form a ring 1-1.5 km out. Readings within 500 m of the site are not stronger than those 1-1.5 km away.
- A plain distance model assumes a monotonic rise toward the transmitter, so it was pulled toward that ring.
- Adding antenna height (40-120 m) and a cos^m elevation pattern (m up to 24) did not fix it blindly.
  - Combined fit: the error stays 1.5-1.7 km for every setting.
  - Morning run alone: the residual varies only 3.67-4.05 dB across settings, so the data cannot identify height or pattern.
  - The lowest-residual choice is still 848 m off. One setting gives 431 m, but it has a *worse* residual, so choosing it would be hindsight.
- Ignoring cells near each candidate (far-field-only fitting) did not help either: errors stayed 1.5-1.8 km.
- The afternoon run contributes most of the cells (739 of 1,007). None of them is within 1.5 km, and their residual is 8.6 dB, so this run dominates the combined fit without seeing the near-site structure.

## Coverage
- The closest logged point is 385 m (morning run). The afternoon run's closest is 1,526 m.
- The user reports passing about 193 m (634 ft) from the site on the morning run. The nearest logged point is 385 m, so the closest stretch is either not in the export or the user's measurement is to a nearby road. Unresolved.
- Sector coverage around the true site, within 6 km:
  - northeast and east sampled (nearest 0.39-0.43 km)
  - southeast from 1.7 km, south from 2.9 km
  - southwest, west and northwest only from 4.8-6 km
  - north not at all

## Implications for note 19
1. **Do not present the plain path-loss estimate as reliable near an elevated fixed site.** A ring of strongest readings around a quieter centre is a recognizable signature. The display should be able to show it, instead of putting the estimate on the ring.
2. **Antenna height and pattern should be optional, labelled model inputs** (for example "known elevated site"), not silent assumptions. This data could not estimate them, so they need either user input or richer coverage.
3. **The ambiguity zones were too tight.** Keep evidence labels qualitative, as note 19 already requires, and validate them against more known sources before trusting them.
4. **The "strongest reading" heuristic failed badly here** (2.6 km). Guidance arrows should not point at the peak reading.
5. **Run weighting.** A far, noisy run with many cells can outvote a closer, cleaner one. Weight by evidence quality and proximity, not cell count, and show per-run estimates side by side.
6. **Better near-site evidence comes from changing the observation, not refitting.** Options: a directional antenna or body-shielding for bearings, an attenuator to avoid saturation near the site, and closing the loop on all sides.
7. **Keep this case for replay testing** (with the user's permission, since it contains his routes). It has a known source, two gain segments, an elevated-site ring pattern and one-sided close coverage.
