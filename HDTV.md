# HDTV receive preview — ATSC 1.0

M.R.S. 0.18.0-preview2 includes an experimental Windows HDTV tab for receiving ATSC 1.0 (8-VSB) broadcasts with a sufficiently wide SDR capture. It decodes the complete physical 6 MHz TV channel into an MPEG transport-stream file. A separate player window can show the live broadcast or the saved file. This is receive only. ATSC 3.0 / NextGen TV is not supported.

Preview2 fixes first-time setup errors from `gdk-pixbuf-query-loaders.exe` about missing `glib-2.0-0.dll` or `gmodule-2.0-0.dll`. The pinned packages now install in dependency order, and readiness checks include that helper and both playback tools. If preview1 displayed those dialogs, dismiss them, let the old setup finish or stop it, close the old application, extract preview2 into a new folder, and run **Set up TV decoder** again. A healthy runtime is reused; an incomplete runtime is repaired. Do not run both setup versions at once.

## First-time setup

Extract the complete application ZIP. Keep `Decoders/ATSC` beside MRS.exe. In HDTV, select **Set up TV decoder**. Internet is needed for this one-time download of pinned GNU Radio, Python and playback packages from conda-forge. Allow roughly 1.3 GB for the runtime plus download cache. Setup installs into `%LOCALAPPDATA%\MRS\Decoders\ATSC`, without changing the system Python or radio drivers. Once installed, reception and playback work offline. Setup is shared by both receiver workspaces and subsequent copies of this release on the same Windows account.

The application ZIP includes the worker source, exact package lock and setup instructions; it does not include the large installed runtime. Another PC needs its own setup. If setup is interrupted, run it again. The Stop TV button also cancels setup.

## Receive a local station

1. Connect an appropriate VHF/UHF TV antenna. Put the PortaPack in HackRF mode and start the HackRF receiver normally.
2. Select **9.6, 19.2 or 20 MS/s**. Start at 9.6 MS/s to reduce processing and USB load. A 2.4 MS/s capture or RTL-SDR capture cannot contain a complete 6 MHz TV channel.
3. Open **HDTV**. Select the station's **physical RF channel**, then **Tune RF channel**. A station displayed on a television as “7.1” may use a different physical RF channel. The selector covers current US full-power RF channels 2–36.
4. Select **Decode current receiver**, then choose a `.ts` output file. Acquisition may take several seconds. Accepted/rejected packet counters show decoder activity; actual program discovery and playback establish usable TV reception.
5. Once packets arrive, select **Read programs**, select a program, and **Watch live**. The player opens separately. Press Space in the player to pause, `9`/`0` to adjust volume, or close its window. To change programs, choose a program and open Watch live again.
6. **Stop TV** stops TV decoding and saves recovered output. The SDR remains running. **Play saved TV** plays the just-saved stream. Saved `.ts` files can also be opened later with a compatible media player.

The ordinary AM/NFM/WFM audio mode and narrow receive filter do not define the TV channel. TV consumes raw I/Q before that filter. M.R.S. mutes its normal audio monitor while live TV decoding is active and restores the monitor setting afterward.

## Capture offset and performance

The existing HackRF capture offset remains supported. With a +1 MHz setting, the SDR capture center is 1 MHz above the desired TV-channel center; the decoder translates the selected channel back before demodulation. At 9.6 MS/s, HackRF's reported usable bandwidth is 9 MHz, so the allowed TV-channel-center offset is at most ±1.5 MHz. At 19.2/20 MS/s, usable bandwidth is 15 MHz, allowing up to ±4.5 MHz. The full 6 MHz channel must fit; the application rejects partial coverage.

More input bandwidth is not automatically better reception. The decoder selects the same TV channel, and higher sample rates increase USB and processing load. An 8-bit HackRF is susceptible to overload and strong adjacent signals. Adjust gain for a clean signal and use an appropriate antenna.

The TV queue is bounded and does not block the receiver. If decoding falls behind, TV stops with an overload message rather than silently dropping queued samples. For slower PCs, record continuous raw I/Q and use **Decode I/Q recording...**; file decoding can take longer than the recording duration. An I/Q capture at 20 MS/s uses about 40 MB per second before file overhead.

Retuning, changing receiver settings or encountering a replay gap stops TV decoding. Restart on the new capture. Replay seeking is not supported during TV acquisition. File decoding rejects a changed capture frequency/rate/channel or a large timestamp gap. It uses the listening frequency recorded in the file.

## Validation and remaining acceptance

The real GNU Radio worker recovered generated MPEG-2 video and AC-3 audio from synthetic ATSC I/Q at 9.6 and 20 MS/s with a 1 MHz offset. Tests cover Windows stdin streaming, decoder cancellation, receiver raw-I/Q events, localhost transport delivery, metadata/coverage validation, queue overflow, and UI rendering. These fixtures are generated data, not over-the-air reception. Live local-station reception, sustained real-time performance and antenna/gain suitability still require operator testing.

The program list currently uses transport-stream program IDs and service names when available. A full ATSC PSIP channel guide, EPG, integrated video window and ATSC 3.0 remain future work. No TV DRM or encryption bypass is implemented.

## Dependencies

The isolated worker uses GNU Radio 3.10.12.0 ATSC receive blocks. Playback/program inspection uses FFmpeg tools from the pinned conda-forge runtime. See `Decoders/ATSC/README.txt`, `GPL-3.0.txt` and `runtime-lock.txt` for source and package references.
