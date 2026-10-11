# NTIA United States Frequency Allocations chart review

Inspected September 29, 2026. Source: https://www.ntia.gov/sites/default/files/2025-09/ntia-us-frequency-allocations.pdf

## Verified document identity and layout

Single-page vector poster, page box 1944 by 2808 points, displayed in landscape orientation. Title: United States Frequency Allocations - The Radio Spectrum. Publisher: NTIA Office of Spectrum Management, U.S. Department of Commerce. Printed edition September 2025. The lower-left note explicitly says the chart represents the FCC Table of Frequency Allocations as of March 31, 2025, and omits information including international allocations and footnotes. Publication date and underlying allocation date must be stored separately.

Viewed the complete poster and enlarged its legend, 117.975-161.575 MHz segment, and portions of the 420-851 MHz segment. This is a structural review, not a complete transcription or validation of every allocation boundary. Text extraction returned glyph-number sequences rather than usable service names; it must not be used as a blind import. Narrow boxes and rotated boundary labels require visual cross-checks against authoritative table records. Do not derive numeric frequency widths from poster pixel widths.

## Legend semantics to preserve

- Service color denotes one of 30 radio-service categories (including separate satellite services); it does not denote a specific modulation or decoder.
- Uppercase service names indicate primary status; initial-capital/lowercase service names indicate secondary status. A normalized display label must retain status in a separate field.
- The narrow strip below the service cells identifies Federal exclusive (red), non-Federal exclusive (blue), or Federal/non-Federal shared (white) rights. This is a different attribute from primary/secondary status.
- Stacked services apply to the same frequency interval. Do not flatten them to a single exclusive service.
- Hatched key Wi-Fi markers form an additional annotation, not an allocation-service replacement.
- Rows change units and scale, from kHz through MHz to GHz, up to 300 GHz. Use explicit numeric endpoints and units.

## Comparison with M.R.S.

The current 144-148 MHz amateur outline is a useful operating-band summary. The chart adds detail: 144-146 MHz includes AMATEUR and AMATEUR-SATELLITE, while 146-148 MHz shows AMATEUR. Preserve the existing 2-meter summary as a separate layer rather than replacing it with an incomplete single service classification.

The 462/467 MHz region shows general service allocations; named FRS/GMRS channel centers still require Part 95. Likewise, CB channel numbers, NOAA weather channel names and amateur operating conventions cannot be inferred from broad allocation colors. The chart complements the existing channel reference.

## Planned allocation-data model and presentation

Use separate selectable layers: (1) broad U.S. allocations, with stacked services, status and Federal/non-Federal rights; (2) named bands such as Amateur 2 m; (3) exact service channel centers from Parts 95/other service rules. Keep saved personal frequencies separate.

Each verified allocation record needs lower/upper Hz, one or more service names and priority statuses, government-use category, direction/qualifier when applicable, official footnote identifiers and links, source edition, underlying effective/as-of date, review date and verification status. Do not infer individual transmit privileges from an allocation.

Start structured expansion with HF/VHF/UHF relevant to the user's receivers. Cross-check exact boundaries and sharing notes against FCC 47 CFR 2.106 and applicable service rules before enabling a region as verified. Use the poster for visual comparison and color organization. Unreviewed regions stay explicitly incomplete. A compact bar can show broad categories; hover/expanded details can show the full service stack. Optional Federal/non-Federal filters and status legend follow after the underlying data is validated.

No new allocation rows were automatically imported into the application from this poster. The original file is preserved under work/ntia-reference; reference screenshots and extraction attempts remain outside release packages.

Source SHA-256: c96f3c0b2df4c4d847d64358b9707f81a66203e1aed736d9fe7c55b551ce8d29

