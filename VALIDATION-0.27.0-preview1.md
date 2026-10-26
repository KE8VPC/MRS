# Validation — manual APRS transmit 0.27.0-preview1

- Release publish succeeded. Dependency vulnerability audit lookup unavailable (NU1900); no completed audit claimed.
- 1,259 regression assertions passed, including AX.25/FCS/address validation, position rounding, status/position AFSK exact-frame round trips, FM I/Q demodulation, native-rate waveform size and prior receiver tests.
- Independent validation: scipy demodulated the generated 8 MS/s signed I/Q, then multimon-ng AFSK1200 recovered source N0CALL-7, destination APZMRS and the exact synthetic status payload. Direct AFSK audio and built-in-demodulated I/Q also decoded independently.
- All UI smoke checks passed. Dialog screenshot inspected: scrollable composition area and fixed send/stop/status/log controls. Window starts unarmed, frequency blank, gain zero; synthetic preview test never invokes Send.
- No RF transmission performed. Previous user's FM tone test was accepted at 8 dB gain; this does not establish hardware APRS reception or validate the new packet-specific flush callback on their unit.
- Native flush handling reviewed against bundled libhackrf 2024.02.1. Over-the-air packet decode and stop behavior remain in the acceptance checklist.
- Packaging verifies extracted hashes and includes only eleven existing allowlisted synthetic recordings. Personal recordings, station configuration and test waveform artifacts are excluded.
