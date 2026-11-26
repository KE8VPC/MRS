# 0.52.0-preview1 validation

- Windows x64 self-contained publish completed. NuGet vulnerability metadata was unavailable (NU1900).
- All 2,175 self-test assertions passed. New checks cover the two ACARS text layouts reported by the user, coordinate signs and ranges, invalid and unrelated payloads, worker fields, original text retention and separate ACARS/ADS-B tracks.
- The full WPF smoke check passed. The HDTV render showed Diagnostics collapsed below the decoder status.
- All nine existing signed/unsigned and paced Aviation I/Q scenarios passed. The label 16/30 parsing cases used worker JSON composed from the observed text, with no live station or HDTV playback test.
