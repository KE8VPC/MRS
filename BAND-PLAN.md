# U.S. spectrum band reference

Implemented in source; pending the next milestone package.

Enable Display > U.S. band reference > Show U.S. band reference bar. Service channel markers have their own checkbox. The setting is saved independently for each receiver. A two-row bar at the top of the RF spectrum tracks zoom, pan, tuning and resize. Band blocks use their actual endpoints; channel ticks mark center frequencies, not occupied bandwidth. Labels are suppressed when they would overlap; hover near a tick for its name, exact frequency and source. Empty areas say no listed entry, not unallocated. Audio and MPX views do not display RF allocations.

## Initial coverage

- Region 2 amateur band outlines: 2200 m, 630 m, 160 m, 80/75 m, 40 m, 30 m, 20 m, 17 m, 15 m, 12 m, 10 m, 6 m, 2 m, both 1.25 m segments, 70 cm, 33 cm and 23 cm.
- Forty CB channel centers, preserving the gaps and nonsequential frequencies of channels 23–25.
- Twenty-two shared FRS/GMRS channel centers plus eight separately labeled 467 MHz GMRS repeater-input centers.
- Five MURS channel centers.
- Seven NOAA weather channel centers, labeled by frequency rather than a potentially ambiguous WX numbering convention.
- FM broadcast band outline, 88–108 MHz.

This selected service reference is not the complete 47 CFR 2.106 Table of Frequency Allocations. It does not describe every sharing service, footnote, geographic restriction, license-class privilege, emission restriction or local channel assignment. Amateur outlines span the union of applicable privileges, not a particular operator's privileges. The 219–220 MHz segment is labeled limited data. The 60 m entry is deferred pending a dedicated current-rule review; no blanket continuous allocation is inferred. ARRL voluntary sub-band recommendations are a separate selectable layer; local repeater coordination remains a future layer. Neither is an FCC rule.

## Research provenance — reviewed September 29, 2026

- Amateur: 47 CFR 97.301 and sharing context in 97.303, official 2024 edition, table on PDF page 23: https://www.govinfo.gov/content/pkg/CFR-2024-title47-vol5/pdf/CFR-2024-title47-vol5-part97.pdf
- CB, FRS, GMRS and MURS: official Part 95 annual edition returned as October 1, 2025, sections 95.963, 95.563, 95.1763 and 95.2763. PDF pages 24, 16, 30 and 62 respectively: https://www.govinfo.gov/link/cfr/47/95?link-type=pdf&year=mostrecent . This link can advance to later editions; the implementation uses the inspected 2025 tables.
- NOAA channel centers and numbering explanation: https://www.weather.gov/marine/wxradio . NOAA is a federal weather service reference, not an FCC licensee allocation record.
- FM broadcast outline: FCC DA 99-500, referencing 47 CFR 73.201: https://docs.fcc.gov/public/attachments/DA-99-500A1.pdf . The band outline was available from the indexed official source.

Direct current eCFR requests were inaccessible during this inspection. Annual-edition references are explicitly dated; this data is not claimed to be fully verified against all September 2026 amendments. Source dates are included in hover details. Future updates should review amendments before changing the bundled table.

## Remaining layers

Full federal/non-federal allocations under 2.106, aviation/maritime, public-safety/business channel tables, AM/TV broadcast, additional microwave bands, category filters, alternate regions, optional bottom placement and user-defined overlays require separate sourced expansions. This reference layer does not modify the saved frequency library, receiver tuning, squelch or decoder settings.

## NTIA poster reference

Reviewed the user-supplied September 2025 NTIA U.S. Frequency Allocations poster. Its underlying FCC table is dated March 31, 2025; footnotes and international allocations are omitted. See NTIA-ALLOCATION-REVIEW.md for legend semantics, source hash, layered-overlay design and verification requirements. This reference has not been blindly imported into the band table.


ARRL cross-check: reviewed the operating-plan page, current linked color chart (revision January 16, 2026). See ARRL-BAND-PLAN-REVIEW.md. The ARRL layer now includes the current page's four 60-meter USB dial markers and 15 kHz RF segment, explicitly identified as ARRL references. The separate FCC 60-meter entry remains deferred pending direct current-rule verification.




## ARRL operating layer

Enable Display > Show ARRL operating recommendations independently of the U.S. reference and service channel markers. Coverage includes 2200/630/160/80/60/40/30/20/17/15/12/10/6/2/1.25 m, 70/33/23/13/9/5/3/1.2 cm, and 6/4/2.5/2/1 mm. Detailed guidance follows available ARRL tables; LF/MF and higher microwave bands use outlines where no subdivision is supplied. See ARRL-BAND-PLAN-REVIEW.md for sources and limitations. Hover exposes overlapping recommendations; at most two interval rows are drawn to preserve spectrum space. This reference does not extend connected hardware tuning limits.


Reference bar position: choose Above spectrum (default) or Below spectrum in Display. Below places the enabled reference rows after the frequency axis and before the waterfall. Placement is saved per receiver and in display presets; changing a color theme preserves it. Spectrum/waterfall click tuning and hover use the relocated layout.


Crowded views: short segment labels use an ellipsis; segments too narrow for useful text keep their colored block without a partial label. Hover retains the full recommendation, and zooming reveals more labels. Individual ARRL markers show a single frequency rather than a duplicate start/end range.

For the aviation, marine and AM broadcast additions implemented after the 0.12.0 package, see SERVICE-OVERLAYS.md for exact coverage and sources.

## NIFOG interoperability layer (next packaged milestone)

Display now includes a separate Show CISA NIFOG channels checkbox, independent of U.S./ARRL visibility. It uses exact channel-center markers, preserves mobile RX/TX pairs and direct aliases, and supplies tone/NAC and source details on hover. See NIFOG-OVERLAY.md for the NIFOG 2.02 source, coverage, source qualifications and validation. The 0.14.0-preview1 ZIP predates this change.
