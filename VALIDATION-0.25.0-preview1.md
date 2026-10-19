# Validation — HF preview 0.25.0-preview1

- Release publish succeeded. NU1900: dependency vulnerability-audit service unavailable; no completed vulnerability audit claimed.
- 1,233 regression assertions passed. New HF checks cover old/new settings tokens, 9.6 MS/s transport with 1.75 MHz hardware coverage, offset fit/rejection, input RMS/peak/rail calculations, worker acknowledgement and I/Q recording metadata.
- All UI smoke assertions passed, including stopped-only HF control, acknowledged 1.75 MHz span and input readout. Synthetic HF overview screenshot inspected; the scrollable sidebar places the new meter below the gain controls.
- Existing DMR/LTR and receiver checks passed in the same suite.
- Physical HackRF filter response, sensitivity and live HF intelligibility are not verified by generated fixtures. Compare a known station with the same gain/offset/filter settings and hardware filtering off/on.
- Input statistics inspect one full block at most ten times per second. They are not calibrated dBm, channel SNR or a comprehensive analog-overload detector.
- Package extraction verifies staged binary-package hashes. Only eleven allowlisted synthetic recordings are included; user recordings are excluded.
