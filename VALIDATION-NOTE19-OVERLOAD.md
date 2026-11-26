# Note 19 — overload evidence and estimator exclusions

November 22, 2026. Implements the small overload-awareness step of Claude's gain/elevated-site plan. Does not implement AGC, gain calibration, elevated-site fitting or new localization guidance.

ReceiverInputLevel now retains input component/rail counts. While Fox logging is active, every processed I/Q block is measured for ADC evidence; otherwise the existing periodic input meter remains. FoxReading carries its block's evidence into the selected measurement window. FoxPoint appends ADC peak dBFS, I/Q component counts and an overload flag. CSV and M.R.S. GPX extensions retain these fields without altering raw channel-power values. Unmeasured legacy quality remains blank/absent rather than invented. Counts describe the retained measurement window, not a guarantee of every physical sample since session start.

ADC INPUT card warns for near-full-scale peak (-3 dBFS) or rail components and holds until below -6 dBFS. Display details concern the recent measured block. Flat/high-input readings across at least 500 m within the recent minute get a cautious lower-gain suggestion; varying readings or missing quality do not. ADC saturation evidence is distinct from channel-relative peak, analog front-end overload and calibrated RF power. A normal ADC reading cannot prove absence of all analog distortion.

Flagged points remain in logs/maps but are excluded from the existing source estimator; the result reports excluded count. Trend windows containing flagged points are uncertain. Strongest measured point remains explicitly not a transmitter location, with an overload annotation where applicable. This does not solve the known-source elevated-antenna ambiguity.

Tests cover actual component counts, hysteresis, unknown legacy evidence, estimator exclusion and explanation, flat/varying routes, CSV header/value alignment and GPX quality export. Private user tracks are not copied into tests. Field ADC/route and performance acceptance remains required; synthetic tests do not validate physical overload thresholds for every receiver.

Combined test-package validation: 2310 self-checks and Fox/digital-TX/receive UI smoke passed on the final self-contained executable. Render inspected with correct preview2-test1 header. Local synthetic tests only. Full-rate ADC measurement microbenchmark added about 0.035 seconds per second of 9.6 MS/s input on this machine (0.163 to 0.198); not a field guarantee. Evidence: work/mrs-054-preview2-test1-publish.
