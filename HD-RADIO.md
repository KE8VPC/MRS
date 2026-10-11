# HD Radio / Data preview — 0.13.0-preview2

This Windows preview offers internal HD audio playback from an M.R.S. I/Q recording or the current centered WFM receiver, plus optional WAV export. It displays synchronization, station name, selected-program title/artist and advertised audio service numbers, and publishes changed metadata to the Data tab. The decoder is a separate locally built NRSC-5 process. HD1–HD8 choices are manual; advertised services do not guarantee that every service will decode.

HD playback uses the Windows default audio output and the HD tab's volume control. Analog monitoring in the same receiver window is temporarily suspended while HD playback is active, then restored according to the existing monitoring controls. Other receiver windows remain independent. Stop HD before selecting a different service. No automatic analog/HD blending, artwork, AM HD or independent HD output-device selection is implemented.

## Test without a local HD station

Open the local test executable in `work/mrs-hd-stream-check`. In **HD Radio**, choose **HD1**, then **Listen to I/Q recording** and select `work/hd-fixture-3200.mrsiq`. This file is generated from the upstream known sample and contains historical programming, not a current local broadcast. The file is streamed through the converter and decoder at its sample-clock rate; there is no intermediate raw file or mandatory WAV export. Expect an acquisition delay. The station and service fields update as information is received; **Stop HD** cancels playback. This test will use your default speakers/headphones when you initiate it.

The receiver path can also be exercised by opening that file through normal I/Q replay, then choosing **Listen to current receiver** while it is playing. Retune, source stop, replay pause/seek/gap or channel-setting changes stop this HD session and clear stale metadata; restart HD after the change. Live HD reception remains unverified at this station.

## Station test

1. Use Quadrature / tuner mode. Tune a known FM HD broadcast station and center that station in the RF capture. Use WFM and **1.92 MS/s** for the first test. 960 kS/s and experimental 3.2 MS/s conversion are also supported. Analog audio bandwidth settings do not determine the raw I/Q recording bandwidth.
2. Record **30–60 seconds of I/Q**, without changing frequency, sample rate, or receiver settings. Stop recording cleanly. An audio-only WAV will not work: HD requires the digital sidebands in raw I/Q.
3. Open the **HD Radio** tab. The local test build finds its decoder in `Decoders/nrsc5`; another compatible Windows decoder can be selected manually.
4. Select **HD1** and choose **Listen to I/Q recording** for internal playback, or **Listen to current receiver** during reception. Optional **Decode I/Q recording** still exports a WAV; that export path needs approximately 179 MB per minute of temporary raw sample storage, plus decoded audio.
5. When decoding succeeds, select **Open decoded WAV**. Check the text log for synchronization, station name and available program information. Stop playback before trying another advertised service.

Please retain the recording for diagnosis if decoding fails. No recovered audio does not establish that HD is absent; capture quality, frequency error, decoder compatibility, service selection and signal strength can all affect the result. The preview rejects narrow coverage, off-center version-3 recordings, rate/frequency changes, incomplete files, and large arrival gaps. Software checks cannot prove the USB stream contained no dropped samples.

## Engineering and validation

The converter consumes unsigned 8-bit interleaved I/Q at 960,000 through 3,200,000 complex samples/s, preserves digital sidebands with a 128-tap fractional-delay low-pass filter, and emits 1,488,375 complex samples/s for the NRSC-5 CU8 input. The UI starts a separate decoder process with an explicit recording path, chosen program index and temporary WAV path; it never opens a receiver through that process. Cancellation kills the owned decoder process and removes temporary outputs. A failed decode does not replace an existing destination WAV. Source and destination equality is rejected.

Upstream: https://github.com/theori-io/nrsc5 — tested revision `9beb2c77171472c707f285f51a1385ac4c4a8be6`, GPL-3.0-or-later. Windows build uses SSSE3, patched FAAD2 2.11.2, rtl-sdr 2.0.2, FFTW, libao, libusb and winpthreads. The local build and dependency source trees are retained in `work/nrsc5` and its build prefixes; MSYS2 tools/dependency packages are under `work/tooling/msys64`. The preview2 package includes the native backend, dependency licenses and matching backend source archives under Decoders/nrsc5. SOURCE-AND-BUILD.txt identifies versions and build commands.

Native build command from the workspace MSYS2 MINGW64 shell:

```
cmake -G "MSYS Makefiles" -S . -B build -DUSE_SSE=ON -DUSE_SYSTEM_RTLSDR=OFF -DCMAKE_POLICY_VERSION_MINIMUM=3.5
cmake --build build -j4
```

On September 30, 2026, the upstream sample decoded into a 2,028,508-byte WAV with KUT station-name and title messages. A separate fixture interpolated that sample to 3.2 MS/s, wrapped it in MRSIQ format, passed it through the production converter and recovered a WAV of the same length plus metadata. This verifies the file-processing path, not local RF reception or every metadata field. The deleted earlier local recording could not be tested. Automated checks cover converter sample counts, block continuity, passband/stopband behavior and invalid input; the full application regression suite and UI smoke test are also run. Known sample audio is historical test material, not current station programming.

Current validation: 350 synthetic tests and the full UI smoke passed. Direct stdin/stdout streaming recovered 2,015,232 stereo PCM bytes, station/title and HD1/HD2 service metadata from the simulated 3.2 MS/s fixture. Owned-process cancellation completed within five seconds. Speaker-output acceptance and physical live HD reception remain pending. A 32-frame live queue fails explicitly on overflow instead of silently dropping I/Q; the receiver remains independent. Native errors are contained in the HD session. Structured metadata parsing is tied to the pinned decoder CLI format; unknown messages remain in the diagnostic log. The receiver replay/raw-I/Q event path also recovered 2,015,232 PCM bytes. Physical reception and speaker-output checks remain user acceptance items.
