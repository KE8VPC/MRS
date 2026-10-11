# CISA NIFOG interoperability reference layer

Included in 0.15.0-preview1. The older 0.14.0-preview1 ZIP does not contain this addition.

In **Display → band reference controls**, enable **Show CISA NIFOG channels**. It is independent of the U.S. and ARRL layers and of the U.S. service-marker checkbox. Its setting saves separately in each receiver's display profile and follows the existing above/below-spectrum placement. It appears only in RF views; compact windows temporarily suppress reference bars when they cannot fit safely.

The bar shows exact channel-center ticks, with labels suppressed where they would collide. Hover near a tick for every listed alias at that frequency, category/use, mobile RX/TX pair, analog/digital mode, tone or NAC, qualifications and printed/PDF page. Keep the pointer over a marker and use Alt+wheel to scroll long hover details without zooming. The TX suffix marks the mobile-transmit side of a pair. Both sides can be monitored; the label does not configure transmission. Direct/simplex entries share a tick with the associated output where appropriate.

These are published reference channels, not detected signals or a local repeater directory. The layer does not change receiver mode, decoder selection, squelch, saved frequencies or radio programming. No starter folders are added.

## Source and coverage

Dataset: **CISA NIFOG version 2.02, January 2025**, reviewed September 30, 2026. Source-page numbering is retained because PDF page numbers are seven greater than the printed numbers.

- [CISA guide download](https://www.cisa.gov/sites/default/files/2024-12/NIFOG%202.02_508%20FINAL%20VERSION%2012%2003%202024.pdf)
- [CISA edition factsheet](https://www.cisa.gov/sites/default/files/2024-12/NIFOG%20Factsheet_Final_2024-03.pdf)
- [Kentucky government-hosted copy used for review](https://kwiec.ky.gov/Documents/NIFOG%202.02_508%20FINAL%20VERSION%2012%2003%202024.pdf). CISA's guide endpoint returned HTTP 403 in this environment; the mirror's title page identifies version 2.02 / January 2025.
- Reviewed PDF SHA-256: `9f1fd1bffdd8d6ea6dc9ad9b7200ac70232e2549f26e947a9994ddf2c538f2a1`.

Included: **246 channel/plan entries grouped into 236 distinct RF centers**.

| Reference family | Printed pages |
| --- | --- |
| Low-band LLAW/LFIRE; VCALL/VTAC simplex, tactical repeater and inland | 28–31 |
| VHF public-safety mutual aid, federal IR and LE | 34–36 |
| Seven U.S./Canada weather-broadcast centers | 37 |
| UCALL/UTAC, federal UHF IR/LE and MED/EMS | 38–44 |
| 700 MHz calling, tactical, law, fire, medical and data | 45–50 |
| 700 MHz air-ground, low-power itinerant and deployable trunk frequencies | 51–54 |
| 800 MHz calling/tactical/direct | 59 |
| Federal/non-federal SAR command and operational references | 63–64 |

This is the channel-reference portion, not a transcription of the entire guide. The 25 Cities section names systems and contacts but does not publish a complete frequency list; no missing frequencies are inferred. Deployable talkgroup IDs are not drawn as RF frequencies. Other common-frequency and radio-service chapters remain separate from this layer.

## Interpretation choices

- Preserve mobile RX and mobile TX columns exactly, including the opposite split directions used by 700 and 800 MHz channels. Do not treat a table RX column as universal transmitter direction; air-ground notes explicitly allow aircraft on either side.
- Preserve CSQ and hexadecimal NACs. RX NAC F7E is a receive-any programming value, not evidence that a transmitter sends F7E. Published codes are not measured decoder results.
- Expand MED-channel ditto marks to the footnote's recommended 156.7 Hz, labeled **recommended**. Keep local-tone caveats. On p.34, retain VMED28's explicit RX CSQ even though the broader footnote recommends 156.7 Hz; do not erase that distinction.
- Mark the low-band fire entries that the source marks proposed. Retain VTAC17's inland restriction and source references for territorial, border, air-ground and licensing qualifications. The guide is a dated reference, not independent verification of current local use or permission.
- The SAR operations table gives reference/working frequencies rather than a radio-programming profile; its tone fields remain **See local plan** where unspecified. The marine designators follow this edition's printed aliases.
- `NifogChannels.tsv` is embedded in the executable for offline use. Updates require a reviewed dataset revision; there is no automatic download or external lookup.

## Checks

Check exact known channels and offsets, federal NACs, direct aliases, proposed/inland notes, source pages, shared-frequency grouping, independent checkbox save/reload, theme retention and absence from audio/MPX. Render dark and light views, both placement options and all three layers at short panel heights. The layer is reference-only and needs no RF signal to verify.

For station acceptance in the next packaged build: enable only NIFOG, view 155.7525, 453.2125, 769.24375 and 851.0125 MHz, hover the ticks, then view 799.24375 and 806.0125 MHz to check the input-side labels. Zoom/pan and switch placement; verify the other reference layers and Receiver B preferences remain independent.

Validation completed September 30, 2026: 401 automated checks, full UI smoke and visual review passed in an isolated development publish. The 227 standard table rows also matched independent layout-text extraction; SAR operations and weather references were reviewed against the rendered source pages. Existing release ZIPs were left unchanged.
