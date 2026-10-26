# Validation — manual TX test 0.26.0-preview1

- Release build completed. NuGet vulnerability-audit lookup was unavailable (NU1900); this is not a completed dependency audit.
- 1,243 regression assertions passed before the final dialog-height/footer-only adjustment. Includes TX parameter limits, signed waveform amplitude/period/phase checks and existing RX/HF/decoder tests.
- All UI smoke checks passed on the final binary. TX dialog starts with blank frequency, zero gain, one-second duration and arming disabled. Screenshot inspected: inputs, send, stop and status visible.
- Tests do not call native start_tx. No RF transmission performed. Native hardware output, frequency accuracy, spectrum, reliable cutoff and actual on-air reception require operator acceptance.
- Firmware/libhackrf ABI and stop behavior reviewed against bundled upstream 2024.02.1 source. Host-side watchdog and bounded worker are implemented but hardware failure behavior is not certified by these tests.
- Release packaging verifies extracted file hashes and excludes user recordings; only existing allowlisted synthetic recordings are included.
