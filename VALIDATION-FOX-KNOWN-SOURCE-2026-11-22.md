# Fox Hunt known-source accuracy test - November 22, 2026

Claude analysis for note 19. The user ran a blind test: he exported Fox Hunt GPX from two drives on a known fixed 700 MHz transmitter (a tower site), asked for a location estimate, and only then revealed the true site. Documentation only. This note deliberately contains no coordinates, routes, device serials or the user's home location. The GPX files and analysis stay outside the repository.

**Corrections.**
- Commit d49f425 reported a 31 m error. That was wrong: the "true" coordinates used were Claude's own estimate, passed back in degrees-minutes-seconds, and Claude compared the estimate with itself.
- Commit e53d524 used an approximate site the user marked by eye. That mark was about 190 m from the real site.
- The figures below use the tower pad position the user read from map imagery. The site position is therefore known to within a few metres.

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
| Both runs combined | **1,578 m** (site lies about 328° from the estimate) |
| Morning run alone | 0.68-0.74 km (two fitting variants) |
| Afternoon run alone | 1,104 m |
| Strongest gain-normalized reading | 2,435 m |

- The true site is **outside the +0.5 and +1 dB zones and inside the +2 dB zone**. It scores +1.21 dB, and 198 of 9,409 grid points (about 12 km²) fit better.
- The zones were overconfident by roughly a factor of two.

## Why: the elevated-antenna effect, confirmed in the data
- The user pointed out that an elevated antenna with a ground plane or downtilt directs energy outward, so reception right below it can be weaker.
- The morning run's closest logged point was **192 m from the tower** (the user independently measured 634 ft on the map). Its level there was -8.2 dB, while the run maximum of -5.1 dB occurred elsewhere.
- Morning-run median level by distance from the site:

  | 0-300 m | 300-500 m | 0.5-0.75 km | 0.75-1 km | 1-1.5 km | 1.5-2 km | 2-3 km | 3-5 km | 5-8 km |
  | --- | --- | --- | --- | --- | --- | --- | --- | --- |
  | -15.6 | -15.4 | -22.2 | -13.7 | -15.7 | -19.6 | -23.6 | -23.4 | -30.3 |

  The level is flat out to about 1.5 km and only falls beyond about 2 km. A plain distance model assumes a monotonic rise toward the transmitter, so it was pulled toward the strong ring.
- Adding antenna height (40-120 m) and a cos^m elevation pattern (m up to 24) did not fix it blindly.
  - Combined fit: the error stays about 1.6 km for every setting.
  - Morning run alone: the residual varies only 3.67-4.05 dB across settings, so the data cannot identify height or pattern.
  - The lowest-residual choice is 737 m off. A 120 m cos^24 setting gives 351 m, but it has the *worst* residual, so choosing it would be hindsight.
- Far-field-only fitting (ignoring cells near each candidate) did not help.
- The afternoon run contributes most of the cells (739 of 1,007). None of them is within 1.3 km, and their residual is 8.6 dB, so this run dominates the combined fit without seeing the near-site structure.

## Coverage
- The closest logged points were 192 m (morning) and 1,333 m (afternoon).
- Sector coverage around the true site, within 6 km:
  - east and northeast sampled close (0.19-0.23 km)
  - north from 0.5 km (11 cells only)
  - southeast from 1.5 km, south from 2.9 km
  - southwest and northwest only from 4.8-5.6 km
  - west not at all

## Implications for note 19
1. **Do not present the plain path-loss estimate as reliable near an elevated fixed site.** Passing 192 m from the tower did not produce the strongest reading. A ring of strongest readings around a quieter centre is a recognizable signature. The display should be able to show it, instead of putting the estimate on the ring.
2. **Antenna height and pattern should be optional, labelled model inputs** (for example "known elevated site"), not silent assumptions. This data could not estimate them, so they need either user input or richer coverage.
3. **The ambiguity zones were too tight.** Keep evidence labels qualitative, as note 19 already requires, and validate them against more known sources before trusting them.
4. **The "strongest reading" heuristic failed badly here** (2.4 km). Guidance arrows should not point at the peak reading.
5. **Run weighting.** A far, noisy run with many cells can outvote a closer, cleaner one. Weight by evidence quality and proximity, not cell count, and show per-run estimates side by side.
6. **Better near-site evidence comes from changing the observation, not refitting.** Options: a directional antenna or body-shielding for bearings, an attenuator to avoid saturation near the site, and closing the loop on all sides (west and south were missing).
7. **Keep this case for replay testing** (with the user's permission, since it contains his routes). It has a known source, two gain segments, an elevated-site ring pattern and one-sided close coverage.
