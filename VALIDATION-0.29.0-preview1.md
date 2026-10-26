# Validation — 0.29.0-preview1

- Final check4 Release/win-x64 self-contained publish succeeded. NU1900 remains: NuGet vulnerability metadata could not be fetched; no security-audit claim is made.
- Final check4 self-test exited 0 with 1,281 passing assertions, including 14 new CW/P25 voice cases. Independent OP25 vectors recovered 18 IMBE frames and a 1012 Hz reference tone. AES/unknown-state mute, FEC roundtrip/repair, noisy CW at 12/20/25 WPM and broadband-noise rejection passed.
- Final check4 UI smoke exited 0 and reported PASS. It exercised CW candidates through the bounded worker/Data table and generated P25 I/Q through the receiver/discriminator/voice pipeline. New CW and P25 screenshots were visually reviewed.
- Native DLL dependency inspection found only KERNEL32.dll and msvcrt.dll. Per-stream vocoder state and serialized shared metadata FEC avoid cross-receiver native-state sharing.
- No live receiver, transmitter or audio output hardware was exercised. Real speech intelligibility, off-air CW identification and long-duration simultaneous operation require user acceptance. No AES decryption, automatic trunk following or automatic CW label updates are claimed.
- Runtime package contains 13 allowlisted generated recordings, no personal recordings. ZIP extraction is compared with every staged file hash. Source package includes native bridge/upstream sources and notices but excludes generated object files.
- Prior acceptance carried forward: user reported live APRS success; intermittent APRS gain/start behavior was deferred. Saved-folder scanning acceptance is partial.
