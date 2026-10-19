# Aviation receive preview — 0.21.0-preview1

Includes ACARS, VDL Mode 2, 1090 MHz ADS-B/Mode S and 978 MHz UAT through a bundled local Windows worker. No network feeding, MLAT or transmission. Each receiver runs one selected aviation channel; Receiver A/B can monitor different bands. Receiver acquisition remains under M.R.S. control.

## Use
1. Start an SDR or M.R.S. I/Q replay. Tune near the desired channel and choose a sample rate that contains its entire signal.
2. Open Aviation, select the protocol and enter the target frequency. Tune receiver changes the listening frequency through the existing tuning control. Start aviation begins decoding the current captured I/Q.
3. ACARS defaults to 131.550 MHz and VDL2 to 136.975 MHz; these are editable starting frequencies, not promises of local traffic. ADS-B uses 1090 MHz and UAT 978 MHz.
4. Use 2.4 MS/s for RTL-SDR ADS-B/UAT. ADS-B needs at least 2 MS/s; UAT needs at least 2,083,334 S/s. The usable captured span must contain the whole signal, including any capture offset. Larger HackRF rates are accepted but require additional CPU/USB throughput; monitor the drop count. The 25 kHz listening filter does not truncate the independent aviation I/Q input.
5. Full decoded observations appear in Data and can be inspected/exported. Aircraft have stable table rows with message counts and last-seen ages. The optional offline latitude/longitude map plots fresh decoded positions, with a draggable table/map divider. It is a coordinate map without street/satellite basemap tiles or an online provider.

Configuration changes and replay seeks stop decoding; start again after tuning or changing rate. At source end, queued data is processed. Worker queues are bounded and overruns reset acquisition, with a visible drop counter. Source/replay positions are approximate processing context, not precise RF frame timestamps.

## Coverage and boundaries
- ACARS: blocks, reported registration/flight, labels, text and decoder metadata. Multi-block contents remain individual blocks; no M.R.S. application-layer reassembly/history archive is claimed.
- VDL2: AVLC and ACARS-over-AVLC, with supported upstream higher-layer details retained in JSON. Not every message contains an aircraft identity or position.
- ADS-B/Mode S: validated frames, identity, callsign, altitude, speed and CPR positions when resolved. Surface positions needing a receiver-location reference remain unavailable in this first UI. DF18 source/control-field identities remain separate from ordinary ICAO identities.
- UAT: supported downlink aircraft fields and uplink/FIS-B decoded details in Data. Weather overlays, segmented product assembly and a complete aviation weather display are not included. Validation is RS-FEC, not a CRC; correction counts represent symbols.
- ACARS registrations, ICAO addresses, non-ICAO UAT addresses, protocols and receiver sessions remain separate. No callsign/registration database enrichment or cross-protocol identity inference.
- Invalid messages do not update aircraft. No position is invented from a Mode S identity or ACARS tail number. Table rows expire after 15 minutes; map positions and displayed altitude/speed expire after 60 seconds. Last known coordinates remain in the table with position age.
- Current input is 8-bit SDR/recorded complex I/Q. Conventional-radio ACARS audio input is a later adapter. Do not expect ordinary FM audio to work for VDL2/ADS-B/UAT.

## Validation
See release validation report. Native core tests and generated I/Q fixtures establish local software behavior, not local aviation reception or weak-signal performance. The included upstream off-air VDL2 fixture is public test evidence, not a local station test. No live SDR was opened by automated checks.

Bundled worker source, modifications, dependency licenses, pinned Cargo.lock and build instructions are under Decoders/Aviation. Engine: airframesio/xng commit 096c805278faa0a904de7d98066bf4cf395cb6c2, restricted to the four selected mode cores plus their common libraries.

## Preview2 ADS-B throughput fix
High-rate ADS-B capture is independently shifted, filtered and reduced to 4 MS/s for decoding; the main spectrum keeps its selected capture rate. This addresses the reported HackRF 19.2 MS/s, +750 kHz capture-offset overload. The live fractional-rate search uses fewer timing phases, trading some marginal-frame sensitivity for throughput. Buffering is still bounded and actual overload remains visible. Lowering the hardware rate can still help a busy or slower PC.

## Preview3 receiver DC correction
The ADS-B branch removes receiver DC before channel shifting and magnitude detection. This prevents the center artifact from dominating pulse detection when a small capture offset leaves it inside the ADS-B passband. A supplied HackRF recording at 9.6 MS/s and +250 kHz yielded 82 validated frames and eight position reports after correction, versus zero before. This is recorded-sample validation, not a new live hardware test.

## Zoomable aircraft map
The map now supports mouse-wheel zoom, drag to pan, Fit aircraft and Follow selected. Manual views persist through aircraft updates. Only fresh decoded positions are plotted; live dots are cyan and replay dots orange.

Online street map is off by default. Enabling it requests only visible OpenStreetMap tiles; the provider receives your IP and the viewed area, not aircraft messages. Tiles are cached locally with bounded storage and expiry. Attribution links to OpenStreetMap copyright. Offline mode retains a coordinate grid; missing network tiles do not stop decoding. No bulk download or online aircraft feed is provided.
