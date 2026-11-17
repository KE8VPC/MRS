# 0.53.0-preview1 validation

- Windows x64 self-contained publish completed. NuGet vulnerability metadata was unavailable (NU1900).
- All 2,182 local self-test assertions and the WPF smoke test passed. The new checks cover railroad FFSK and BCH rejection, a word-spaced CW transcript, WEFAX grayscale and line timing, and cellular coverage/center-bin handling.
- The pre-release decoder was checked against the supplied off-air I/Q files through the production NFM discriminator. The stronger EOT recording yielded two valid frames for unit 5960 at 88 psig. The faint HOT recording yielded no valid frames.
- Live hardware checks remain open. The recordings are local test inputs and are excluded from distribution.
