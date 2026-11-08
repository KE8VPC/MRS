# Transmit authorization safeguards
Context: Receiver Amateur
Keywords: TX authorization license callsign arming APRS transmit locked declaration United States

TX is disabled until the operator completes a fresh authorization declaration and explicitly enables one burst in the transmit window. No authorization is restored from saved receive settings, presets or imported bandplans. The original short-test profile is U.S. amateur NFM test/APRS, for Technician, General, Advanced or Amateur Extra operators. Other jurisdictions, services and license classes remain blocked.

Enter the control-operator callsign without SSID and license class. Confirm current license authority, applicable location/frequency/operating restrictions and equipment/filtering/actual output compliance. This is operator-declared information, not independent license or identity verification. A valid-looking callsign does not prove authority. M.R.S. does not calibrate radiated power from HackRF gain or independently verify your location, license status, antenna or spectrum purity.

The shared policy checks the complete conservative 25 kHz emission envelope within 144.1–148 or 430–450 MHz. It intentionally excludes 420–430 MHz and other unreviewed bands. Permitted bands do not imply every location or channel is appropriate: local restrictions, shared/satellite/repeater use and other applicable obligations remain the operator's responsibility. References are listening guidance only. APRS source callsign must match the declared callsign (SSID allowed).

Changing frequency, gain, duration, declaration or packet contents disarms the burst. The worker receives a short-lived authorization bound to the exact frequency, gain, duration and device identity. Both the worker entry point (before opening hardware) and native RF-start path validate it. Missing, expired or mismatched authorization blocks transmission. Existing 1–3 second duration limits, low-gain bounds, RF amplifier off, Stop and watchdog remain. Each send consumes the UI arm and requires another deliberate arm. Closing the window disarms; no automatic repeat or restart.

This protects ordinary application workflows against accidental/unsupported TX. It is not tamper-proof access control, official certification or a guarantee that a self-declared operation is lawful. No feature for arbitrary transmit, commercial/aviation/marine TX or unrestricted ISM operation is enabled. A dummy load/test label does not bypass authorization. Receive exploration remains independent.

U.S. rule review sources: 47 CFR 97.5, 97.301, 97.303 and 97.307; Part 15 equipment requirements are separate. Consult current official rules for your actual operation. Validation for this update uses synthetic/no-RF tests; no transmission is necessary to check blocked paths.

The 0.44 digital workflow adds a separate conservative U.S. Region 2 data policy for FT8/FT4/WSPR. It does not relax the old test limits. See FT8, FT4 and WSPR transmit Help: exact message/slot/device permits, band edges, declared class, bounded sessions, no implicit RX-PPM-to-TX correction, Halt and receive restoration. JS8 sends use an external backend and a separate explicit one-message workflow; native SDR JS8 is not enabled.
