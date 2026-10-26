# Railroad CSV expansion validation — October 24, 2026

Data-only correction for the incomplete starter dataset; no M.R.S. executable or receiver changes.

- Eight layers contain 467 definitions at 281 unique centers: 97 original AAR, 90 splinter, 182 NXDN, 19 ATCS, 38 PTC, 14 ITC, 15 ACSES and 12 telemetry references.
- Standard AAR grids are generated in exact integer Hz from documented endpoints/steps. Assertions check layer counts, unique within-layer channel keys, frequency range, digital 488 endpoint, splinter 196 endpoint and integer-Hz parsing.
- A small diagnostic harness loaded the real MRS.CsvBandPlans type from the 0.34 build assembly and called its existing Reload method on this folder. Result: PASS, 8 plans, 467 channel definitions, 281 unique centers, 470 total markers including three band envelopes, zero parser errors. Detailed output is in work/railroad-csv-check/result.txt.
- ZIP contents were read back and byte-compared to every source file. SHA-256 is supplied separately. The pack contains only reference data and instructions.
- Verified installation adds only a new railroad-expanded folder under the configured working bandplans directory. Every staged file was hash-compared before the directory was moved into place. Eleven CSV files were installed, and zero existing files were overwritten. Original CSVs, labels, P25 collections and profile settings were not changed.
- The user still needs Bandplans > Reload CSV and can uncheck the old reference/railroad layer to avoid duplicated baseline markers. Actual spectrum appearance after reload and live railroad occupancy remain user checks.

Source caveats: analog/digital aliases intentionally overlap. The numeric NXDN center grid is 7.5 kHz despite the 6.25 kHz channel mode. PTC-7's published base/mobile mismatch is preserved and flagged; no corrected frequency or pair is guessed. Reference markers do not add decoder support or assert a local railroad/operator. See RAILROAD-BANDPLAN.md and the pack's SOURCES.json.

## Deferred at user request

On October 24, 2026, the user requested this expansion be included in the next update while continuing to test the last build. The newly installed folder was byte/hash-checked against the prepared reference pack and moved out of the active bandplan directory into work/deferred-railroad-bandplans-20261024. No existing bandplans or settings were changed. Source datasets and the standalone ZIP remain prepared for the next release; no immediate reload or update is requested.
