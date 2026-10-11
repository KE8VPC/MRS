# WFM broadcast reception — 0.9.0

The analog broadcast path provides pilot-locked stereo FM, composite/MPX analysis, and experimental RDS/RBDS reception. Version 0.13.0-preview1 adds a separate experimental HD Radio recording decoder; see HD-RADIO.md. Version 0.13.0-preview2 adds experimental internal HD listening from recordings and centered receiver I/Q, plus structured HD information. Local station and speaker-output acceptance remain required.

## Listen

Select an RTL receiver, Quadrature/tuner sampling, a local FM broadcast frequency and Wide FM. Start with the 190 kHz receive filter and a suitable gain. All three exposed acquisition rates are supported by local synthetic tests. Select the intended Windows speakers/headphones and enable monitoring. Disable RF squelch for the initial weak-signal comparison.

The broadcast strip appears above the spectrum for active WFM SDR reception or WFM I/Q replay. It distinguishes pilot detection, decoder lock and actual stereo output. Force mono preserves pilot tracking while blending the difference channel out. Loss of pilot lock blends back to mono. Left/right meters and clipping status describe decoded audio before listening volume and squelch. De-emphasis is currently fixed at 75 microseconds; a 50-microsecond option remains future work.

## Views

RF retains tuning and capture-navigation controls. MPX shows the discriminator composite before the audio filter, with a 0–120 kHz full span and a legend identifying audio, pilot, stereo difference and RDS regions. MPX levels are relative; seeing energy near 57 kHz is not proof of valid RDS. Audio (L+R) shows the downmixed audio spectrum/waterfall; opposing-phase left/right signals can cancel in that view. Waveform L/R shows separate channels over up to 20 ms with a full-scale reference. The displayed waveform is a block preview, not a triggered oscilloscope.

Alternate plots have their own view state; wheel zoom, drag/pan and Home/full-span do not retune RF. Display theme and level changes apply to the auxiliary spectrum too. The RF navigation row is hidden while inspecting a different signal domain. Ordinary radio speaker audio is not a full MPX source; a dedicated composite/discriminator input path is not implemented.

## RDS/RBDS

The first decoder provides PI, numeric PTY with selectable US RBDS or RDS names, Program Service name (0A/0B), and RadioText (2A/2B). Acquisition uses a separate 57 kHz carrier loop and timing hypotheses, so the RDS path does not require a stereo pilot. All four block checks and their group offsets must validate. No speculative error correction is performed. Name/text publication requires two matching complete assemblies. Unknown extended characters are shown as replacement characters; full RDS character mapping is not yet implemented.

Retuning, source/mode changes and replay seeking reset decoder state. PI changes reset station fields; RadioText A/B changes reset the previous message. If no valid group arrives for ten seconds of processed samples, the station strip returns to searching. Paused replay retains its decoded snapshot. Group counts count accepted complete groups, not estimated RF signal quality. AF, CT, EON, traffic announcement handling, ODA, RT+, raw group export and other RDS services are outside this initial decoder. Broadcast clock data never adjusts the PC clock.

## Recordings

SDR listening and demodulated WAV recordings now use 48 kHz, 16-bit, two-channel PCM. WFM produces stereo when locked; other SDR modes duplicate mono into both channels. Windows audio-input and ordinary WAV-file analysis remain mono. Recordings remain independent of listening volume and squelch. The two-channel SDR format stays constant through mode changes, allowing one valid WAV file to contain them. Force mono affects decoded stereo WAV output.

I/Q version 3 is unchanged. Replay re-decodes broadcast content from the recorded raw samples, including earlier compatible WFM I/Q files. Force mono and PTY region are saved listening/display preferences, not per-frame I/Q metadata. Replay therefore uses the current preference. No recording/replay speed change is added.

## Validation and remaining acceptance

Tests cover stereo separation and pilot offsets/loss, mono fallback, continuity across uneven sample blocks, ungated stereo WAV data, strict RDS framing, repeated text assembly, station/text resets, signal loss, noise rejection, noisy composite recovery and simultaneous stereo/RDS through unsigned-8 I/Q at 240/960/1920 kS/s. Offscreen WPF checks cover broadcast views, replay, Force mono, seek reset and source-stop cleanup. These fixtures establish the tested behavior, not standards certification, weak-signal sensitivity, multipath performance or live audio-device compatibility.

Technical references: [FM stereo transmission description](https://www.govinfo.gov/content/pkg/CFR-2007-title47-vol4/pdf/CFR-2007-title47-vol4.pdf), [NRSC-4-A physical/link-layer specification](https://www.nrscstandards.org/standards-and-guidelines/documents/archive/nrsc-4-a-standard-2005.pdf), and [NRSC-4-B RBDS updates](https://www.nrscstandards.org/standards-and-guidelines/documents/archive/nrsc-4-b-standard-2005.pdf). These references informed the implementation; no standards document is bundled or compliance claimed.

## MPX reference markers

The MPX spectrum now draws labeled dashed reference lines at 19 kHz (stereo pilot), 38 kHz (L-R channel center), and 57 kHz (RDS/RBDS). Brackets identify L+R audio at 0-15 kHz and the L-R stereo band at 23-53 kHz. Lines extend faintly through the waterfall, track frequency zoom/pan, and are absent from RF and audio views. These are expected frequency locations, not detection indicators; the 38 kHz center does not imply a transmitted carrier. Labels that cannot fit the visible span are omitted to avoid overlap. HD sidebands are not MPX markers.

