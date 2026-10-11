# M.R.S. native I/Q format

## HackRF extension in 0.17.0-preview1

The frame layout remains version 3. Optional JSON `Backend` defaults to `RTL` when absent; `HackRF` identifies normalized HackRF I/Q and its gain encoding. HackRF requires version 3 and SamplingMode 0. Gain fields are a control token rather than tenths of dB: `0x01000000 | LNA_dB | (VGA_dB << 8) | (RF_amplifier_on ? 0x00010000 : 0)`. LNA is 0–40 in steps of 8; VGA is 0–62 in steps of 2. Invalid tokens are rejected. The token records selected settings, not measured total analog gain. Older M.R.S. versions cannot correctly interpret these gain fields or wider rates.

HackRF signed 8-bit components are converted losslessly to unsigned 8-bit with XOR 0x80. Supported playback rates now also include 2400000, 9600000, 19200000 and 20000000. Center and channel frequencies are Int64, including the 6 GHz upper tuning endpoint. Usable bandwidth records the nominal analog filter (1.75, 9 or 15 MHz), independently of full sampled span. The HackRF spectrum view displays the full sample-rate span, with the analog filter width in its readout; selected demodulator filters must fit the usable capture coverage.

HackRF live IPC uses a separate 32-byte header: Int32 magic 0x4D525310, Int32 count, Int64 frequency, Int32 rate, Int32 requested gain token, Int32 applied gain token, Int32 analog bandwidth. Unsigned 8-bit payload follows. Frames are accepted only after matching requested frequency/rate/gain. Control changes restart the USB stream and discard queued old samples; wall-clock recording timestamps retain the observed gap. Native queue overflow stops reception explicitly. Hardware timestamps, phase-coherent multi-receiver capture and silent gap repair are not implemented.

## Compatibility

Extension .mrsiq; all numeric fields are little endian. 0.8.1 writes version 3 and reads versions 1, 2 and 3. Older executables cannot read version 3. This is not SigMF or an I/Q WAV format.

All versions begin with Int64 magic 0x31514953524D, Int32 JSON byte length (2–8192), then UTF-8 JSON. Fields: Version, StartedUtc, WideFm, RequestedGain (tenths of dB; -1000 means automatic), nullable ReceiverIdentity and optional Mode. Mode is NFM/WFM/AM/USB/LSB; when absent the legacy WideFm flag selects WFM or NFM. Unsupported Mode values are rejected. UTC is the PC clock; relative timestamps are monotonic host observations, not hardware timestamps.

Each frame starts with Int64 elapsed ticks (100 ns units). They must be nonnegative, nondecreasing and no greater than seven days. Int64 -1 replaces the next timestamp for clean completion; no data may follow. There is no frame checksum. Missing/invalid tails are reported after complete preceding frames are recovered.

## Version 1 frame after timestamp

1. Int32 magic 0x4D525306.
2. Int32 byte count, even and 4096–32768.
3. UInt32 center Hz, 25,000,000–1,750,000,000.
4. Int32 rate: 240000, 960000 or 1920000 complex samples/second.
5. Unsigned 8-bit I then Q payload. Normalize a component as (byte - 127.5) / 128.

## Version 2 frame after timestamp

1. Int32 payload byte count, 1–32768, aligned to a whole complex sample.
2. Int64 nonnegative capture center Hz.
3. Int32 positive complex sample rate.
4. Float64 usable bandwidth Hz, finite, positive and no greater than sample rate.
5. Int32 format: 0 unsigned 8-bit, 1 signed 8-bit, 2 signed 16-bit little endian, 3 signed 24-bit little endian, 4 signed 32-bit little endian.
6. Interleaved I then Q payload, no padding; signed values use two's complement.

There is no per-frame magic in versions 2 or 3. Unknown formats and invalid/alignment-exceeding byte counts are rejected before allocating a payload.

## Version 3 frame after timestamp

Begins with the same five metadata fields as version 2. Before the payload, it adds:

6. Int64 channel frequency Hz, independent of capture center.
7. Int32 mode index: 0 NFM, 1 WFM, 2 AM, 3 USB, 4 LSB.
8. Float64 nominal channel filter width Hz, one of the supported widths for that mode.
9. Byte audio-AGC flag, 0 or 1.
10. Int32 applied tuner gain, tenths of dB; -1000 for automatic.
11. Int32 requested tuner gain using the same convention.
12. Payload bytes.

The complete channel passband must fit within recorded usable bandwidth. AM/FM passbands are centered; USB spans +300 through +(300 + width) Hz and LSB spans -(300 + width) through -300 Hz. All channel fields apply to the associated frame. Gain is reported by the adapter as the selected supported setting; automatic gain does not claim a continuously measured tuner gain value.

## Streaming, playback and boundaries

The writer owns copies of queued sample bytes. Its 64-frame bounded queue holds at most about 2 MiB of payload; overload or storage failure stops recording with an incomplete-file report. Required raw payload throughput equals sample rate times the complex-sample byte width. No rotation/retention is implemented.

Native RTL settling samples are discarded during hardware changes. Accepted samples are recorded before software mixing/filtering, demodulation and listening squelch. Channel-only changes leave the capture center unchanged and store new per-frame settings. Successful gain/rate changes also remain within one version 3 file. Stop/source changes/loss close recording; reconnect does not resume it automatically.

Replay builds a sparse validated frame index, at most roughly one entry per recorded second, and scans payloads with bounded memory. Pause stops timeline advancement; seeking starts at a validated frame boundary at or following the selected time and resets DSP/listening buffers. Metadata and gaps follow recorded time. Stop and EOF retain the loaded file in the UI. UI seeking closes an active WAV export; audio exports omit paused time and reception gaps. No arbitrary user-provided byte offsets are exposed.

Current DSP supports unsigned 8-bit at 240/960/1920 kS/s; higher-precision storage is not a claim of processing support. Unsupported playback formats/rates stop explicitly. Display cropping uses usable bandwidth while FFT mapping retains the original sample-rate span. Capture references above 4 GHz are retained in storage/replay; the RTL native API remains adapter-specific UInt32 tuning.

Live RTL worker IPC is separate from recording versions: 28-byte header (Int32 0x4D525309, Int32 count, UInt32 capture Hz, Int32 rate, Int32 requested gain, Int32 applied gain, Int32 sampling mode), followed by unsigned 8-bit I/Q. The application accepts data only when requested frequency/rate/gain/branch match the accompanying acknowledgement.

## Sampling branch metadata (0.8.1)

Optional JSON SamplingMode is 0 (quadrature), 1 (direct I), or 2 (direct Q); missing values default to 0 and unknown values are rejected. This addition does not change version 3 frame layout. The branch is constant within a recording: changing it finalizes the file. In direct modes, gain -1000 means tuner gain is unavailable, rather than an automatic tuner gain measurement. Older applications can ignore the extra JSON field and therefore cannot reliably label direct-sampling recordings. The live worker and UI must be from the same release.


## Broadcast replay in 0.9.0

The recording layout remains version 3. WFM I/Q is re-decoded for stereo/RDS during replay. Force mono and RDS/RBDS naming region use current saved preferences, not recorded per-frame settings. SDR WAV exports now have two channels throughout mode changes; mono SDR modes duplicate their samples. See BROADCAST.md.

