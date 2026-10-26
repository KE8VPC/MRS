# Expanded railroad correction — October 24, 2026

The original starter plan below was incomplete. A separate CSV-only pack now supplies **467 channel definitions at 281 unique frequency centers**, in eight independently selectable folders under `railroad-expanded/`:

| Layer | Definitions |
| --- | ---: |
| Original AAR 001–097 | 97 |
| AAR splinter 107–196 | 90 |
| AAR NXDN 307–488 | 182 |
| ATCS reported reference centers | 19 |
| PTC base/mobile references | 38 |
| ITC references | 14 |
| ACSES references | 15 |
| HOT/EOT, distributed power, remote control and HLCS | 12 |

Compared with the 93 original markers, this adds 374 channel definitions and 188 additional distinct frequency centers. Analog/NXDN aliases and PTC/ITC overlaps intentionally remain separate records/layers. They are not hundreds of newly confirmed local stations.

Install the new `railroad-expanded` folder in the selected working bandplans directory, then click **Reload CSV**. Uncheck the old `reference/railroad` layer to avoid baseline duplicates. Choose the expanded layers relevant to the task. Existing CSVs and custom labels are preserved, and the current 0.34 executable can load this data without rebuilding or restarting reception. The expanded files are also in the project bandplans tree for future packaging. The old embedded Create missing reference CSVs action still creates the starter dataset; use this data pack for expanded coverage.

Important source details:

- Primary comparison: [RadioReference railroad reference](https://www.radioreference.com/db/aid/7747), checked October 24, 2026. Standard numeric grids were generated from endpoints/steps with original concise labels, rather than copying editorial descriptions or operator assignments.
- [JEM railroad radio manufacturer guide](https://www.jemcom.com/uploads/1/1/8/9/118921460/jem_radio_user_guide.pdf) corroborates early and splinter channel centers through its indexed channel chart; direct PDF access was unavailable in the web tool.
- [FRA Wireless Digital Train Line Phase 3](https://railroads.dot.gov/sites/fra.dot.gov/files/2023-02/WiDTL%20Phase3.pdf) corroborates the additional channel families, ATCS reported centers and HOT/EOT roles.
- [FRA PTC desense report](https://railroads.dot.gov/sites/fra.dot.gov/files/fra_net/15787/PTC%20Desense%20Mitigation%20Phase%201_final.pdf) supplies independent ITC/ACSES frequency tables.
- The NXDN table has **7.5 kHz between adjacent listed centers**. Its 6.25 kHz radio channel-mode width is a different value. The last listed entry is 488 / 161.572500 MHz, even though the page's heading says 307–487.
- Published PTC-7 values are inconsistent with a simple one-MHz base/mobile offset: base 220.767500 and mobile 221.762500 MHz. Both are retained independently and explicitly flagged for verification. No pairing/offset is invented.
- ATCS, PTC, ITC, ACSES and telemetry records are references whose local use can vary. They do not add decoding support or claim a local operator. The generic CSV `downlink_mhz` column holds the marker center even for rows labeled mobile/MCP; no uplink is inferred.

Validation checks exact-Hz conversion, leading-zero identifiers, expected layer counts, grid endpoints, duplicate keys, and ZIP byte integrity. The data is additionally checked through the actual 0.34 CsvBandPlans loader; see the generated validation report.

---

# Railroad reference CSV — 0.33.0-preview1

The Bandplans tab now includes reference/railroad. Its channels.csv contains 91 AAR reference centers 007–097: 160.215000 through 161.565000 MHz in 15 kHz steps, with channel 073 / 161.205000 marked Railroad Police Mutual Aid. It also includes separate HOT 452.937500 MHz and EOT 457.937500 MHz telemetry markers. bands.csv supplies one compact VHF reference strip. Its displayed edges extend half a channel spacing beyond the first/last centers; they are visualization boundaries, not a regulatory allocation claim.

Sources checked October 24, 2026:
- [CISA NIFOG 2.0 railroad reference](https://www.cisa.gov/sites/default/files/publications/NIFOG%20Ver%202.0_508%20version_FINAL%20-%2011%2015%2021_1%20%28002%29.pdf): AAR 007–097 centers and police mutual aid. CISA's indexed text was available; direct PDF fetch returned HTTP 403 during this check.
- [NTIA railroad spectrum study](https://its.ntia.gov/publications/details?pub=2376): confirms 91 channels and the 160.215–161.565 MHz range.
- [FRA Wireless Digital Train Line, Phase 3](https://railroads.dot.gov/sites/fra.dot.gov/files/2023-02/WiDTL%20Phase3.pdf), section 3.2.5: HOT/EOT frequency and FSK direction assignments.

These are reference channels, not a list of locally active stations, operator labels or a new railroad decoder. Analog and digital use can differ; the plan does not force NFM or NXDN. Expanded digital AAR numbering and other railroad data allocations remain outside this verified initial dataset. Keep local railroad names in your frequency library or custom CSV labels.

Select/hide reference/railroad independently. Edit CSV labels and Reload CSV to apply. Existing customized data is preserved by Create missing reference CSVs. If an older working folder was retained across upgrades, use that button once to add the missing railroad files; it does not overwrite existing files.
