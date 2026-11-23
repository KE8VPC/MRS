# Fox Hunt known-source accuracy test - November 22, 2026

Claude analysis for note 19. The user ran a blind test: he exported Fox Hunt GPX from two drives on a known 700 MHz fixed transmitter, asked for a location estimate, and only then revealed the true site. Documentation only. This note deliberately contains no coordinates, routes, device serials or the user's home location. The GPX files and analysis stay outside the repository.

## Input
- Three GPX exports, two of them byte-identical, so there were two real drives.
  - Morning run: 23 min, 1,326 points.
  - Afternoon run: 44 min, 1,269 points.
- Same frequency on both runs. Squelch was open on every point, every GPS fix was at most 1 s old, and there were no time gaps over 10 s.
- **The gain tokens differ between the runs** (noise floors about 10 dB apart), so raw levels are not comparable across runs.

## Method (offline, outside M.R.S.)
- Points were averaged into 60 m cells per run (median level, weighted by sample count). This stops stops and slow stretches from dominating.
- Model: level = run offset - 10·n·log10(distance). The offset is fitted separately for each run, and n is fitted within 1.5-5.
- Grid search over 24 × 24 km in 250 m steps, scoring each point by its robust residual (scaled median absolute deviation).
- Ambiguity zones: grid points within +0.5, +1 and +2 dB of the best residual.

## Result
| Estimate | Error to true site |
| --- | --- |
| Both runs combined (per-run offsets) | **31 m.** The true site lies in the best-scoring of 9,409 grid cells. |
| Morning run alone | 2,208 m |
| Afternoon run alone | 1,023 m |
| Strongest normalized reading on either run | 1,432 m |

- Fitted path-loss exponent n = 2.01. The robust residual at the best point is 7.0 dB.
- The +0.5 dB zone is about 1.2 × 1.8 km, and it contains the true site.

## Coverage the estimate was made from
- **No logged point came closer than 736 m.** The morning run's nearest point was 1,268 m. No point was within 300 m.
- Sector coverage around the true site, within 6 km:
  - north, northeast, east and southeast were well sampled (nearest 0.74-1.3 km)
  - south was sampled from 1.5 km
  - southwest only from 3.4 km
  - west not at all
- **Open question.** The user reports a pass about 193 m (634 ft) from the site, and it is not in these exports. Possible explanations are the roughly 30 minutes lost to the unnoticed GPS disconnect (note 19), or a separate run. Do not assume which until the user confirms.

## What this supports, and what it does not
- **Combining runs separated by gain offsets was decisive.** Each run alone was off by 1-2.2 km; together they bracketed the site from several sides. This supports note 19's resume/import/append workflow and its per-segment settings record. Runs with different gains must not be pooled on a single absolute scale.
- **Heading for the strongest reading would have misled.** The strongest reading was 1.4 km from the site. Guidance should come from the model fit and its consistency, not from the peak alone.
- **A 31 m error from 250 m grid steps is partly luck.** The honest claim is "within one grid cell (about 250 m)". One success does not calibrate the ambiguity zones. Do not present them, or any of these figures, as a confidence percentage. Repeat the test with other known sources, other terrain and incomplete routes before the zones become evidence labels.
- **Usable test.** The setup has a known source, two gain segments, missing west/southwest coverage and a nearest approach of 0.74 km. That makes it a good replay case for note 19's planned partial-estimate, sector-coverage and direction-arrow features. A stored copy would need the user's permission, because it contains his routes.
