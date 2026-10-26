# Validation - 0.37.0-preview1

Release candidate: work/mrs-0370-preview1-check4, October 25, 2026 (America/New_York).

- Windows x64 single-file publish succeeded. NU1900 reports unavailable NuGet vulnerability data; no dependency audit is claimed.
- 1,391 local assertions passed, exit 0. Seventeen new assertions cover click grid/offset/tolerance behavior, common spectrum/waterfall tuning, Ctrl marker separation, mono/stereo PCM handling, gated recording independent of speaker volume, real MP3 encoding and independent Windows decoder playback, pre-trigger/tail timing, continuous segmentation and source boundaries.
- Full UI/replay smoke passed, exit 0. It arms MP3 VOX from synthetic audio and produces a finalized MP3; it also arms Both with the synthetic live receiver, produces both MP3 and I/Q, and verifies the receiver process did not restart. Existing I/Q, aviation, digital/paging, bandplan and workspace workflows remain covered.
- Aviation centered field, MP3 controls and snapping screenshots were reviewed. Added 6.25 kHz to the navigation choices so the selected step matches the actual snapping grid. Retained burst table totals are displayed separately from run counters.
- Audio capture enqueues bounded PCM copies; file writing and MP3 encoding are background workers. Windows Media Foundation performs compression (64 kbps mono, 128 kbps stereo). No new third-party native encoder is bundled. Manual WAV and original I/Q capabilities remain.
- Source/tuning boundaries finalize a clip and discard old pre-roll. Decode/listening gates apply to MP3, while speaker volume/monitoring do not. Both uses audio activity for I/Q triggering; files have separate pre-roll/tail timing.
- Existing automatic-I/Q mode remains the upgrade default. MP3 options persist with automatic-recording settings. Queues/storage are bounded; encoder failures report and retain intermediate WAV evidence. These failure-handling paths are implemented, but an unavailable-codec condition was not induced on this host.
- External Help now includes 22 editable topics, including MP3 VOX and snapping instructions. Those files were refreshed in the publish directory after compilation; no executable change is required for Help edits.
- Runtime/source ZIP extraction and per-file comparisons are performed by the packaging script. Final CRC/executable/native-hash/content checks are recorded separately.

No user recording was modified or bundled; only generated fixtures are packaged. No installed running application, live receiver settings or RF transmitter was changed. Live acceptance of MP3/digital VOX, Both and click snapping is still pending; see ACCEPTANCE-0.37.0-preview1.md. Prior user passes are carried forward.
