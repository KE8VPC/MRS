# Validation — 0.24.0-preview2

Release build succeeded; NuGet vulnerability-audit lookup remains unavailable (NU1900).

1,223 local regression assertions passed. New cases cover LTR repeats at eleven-second intervals, sixty-second expiration, continued confirmation, and pending progress. Existing DMR expiration remains ten seconds.

The supplied 39.407-second recording was demodulated offline at 48 kHz and fed to the application decoder: four CRC-valid LTR observations recovered. Separate field inspection found matching area 0, channel/home 4, idle group 255, free channel 4 at 5.441, 16.478, 27.515, 38.552 seconds. This is captured idle-signal evidence; live receiver acceptance and other LTR traffic types remain pending.

User recordings are excluded from distribution. The packaging script verifies extracted binary files against staged hashes and permits only the existing eleven synthetic sample recordings.

All UI smoke assertions passed on the final preview2 binary.
