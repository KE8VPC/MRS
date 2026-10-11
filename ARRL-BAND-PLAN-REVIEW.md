# ARRL band-plan reference review

Reviewed September 29, 2026. Downloaded the live band-plan HTML and graphical-frequency-allocations HTML, followed the linked color U.S. amateur band chart, and visually examined that chart. Original references are retained under work/arrl-reference outside distribution packages.

Sources:
- https://www.arrl.org/band-plan
- https://www.arrl.org/graphical-frequency-allocations
- https://www.arrl.org/files/file/Regulatory/Band%20Chart/Hambands4_Color_11x8_5.pdf

The downloaded color chart is revised January 16, 2026 (copyright year 2023 is not its revision date). It summarizes license classes, emission types and privileges. It shows a 60-meter update effective February 13, 2026; therefore the March 2025 underlying data of the NTIA poster is not sufficient for the current 60-meter layer. Verify this change directly against effective FCC rules before coding the previously deferred 60-meter entry. Distinguish USB dial/reference frequency from signal/channel center frequency and band edges.

The operating-plan page provides recommendations within bands, whereas the graphical chart summarizes privileges. Keep these distinct from the FCC allocation layer. Preserve local-option and coordination notes; do not convert suggested repeater ranges into an asserted local channel assignment. The page specifically notes regional differences, including 146.40 MHz sometimes serving as a repeater input. Detailed proposed/adopted-plan wording and dates must be retained rather than assuming uniform status across bands.


Implemented in source: an independently selectable ARRL recommendations layer covering 28 band groups, from 2200 m through 1 mm. Detailed intervals and calling markers follow the current operating-plan page through 3 cm. LF/MF and bands above 10.5 GHz use outlines where no detailed subdivision is available. Regional alternatives and overlapping recommendations are retained. Two interval rows and one marker row suppress colliding labels; hover includes all matching entries, including overlaps that do not fit visually. Source, review date and regional caveats are shown on hover. The layer is available offline and does not change tuning or receiver coverage. Local coordination remains a separate future layer. All-band coverage replaces the earlier two-band starting scope.

Revision policy: treat allocation boundaries, license-class/emission privileges, and voluntary operating recommendations as distinct data. A newer publication date alone does not establish an allocation change. Compare the actual changed provisions; retain older corroborating allocation references when the underlying boundaries remain unchanged.


Implementation limits: concise labels summarize functional use; they do not encode every power, bandwidth, emission, sharing, geographic or license-class condition. The 9 cm layer stops at 3450 MHz and omits discontinued upper coverage. The 13 cm gap is preserved. The 60 m references distinguish USB dial frequencies from CW/data centers and RF edges. Current FCC privilege verification is still separate. Historical QST material is excluded.

