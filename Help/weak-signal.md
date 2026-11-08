# Weak signal reception: FT8, FT4 and WSPR
Context: Weak signal
Keywords: FT8 FT4 WSPR USB UTC clock window FEC CRC weak signal amateur

Open Amateur → FT8 / FT4 / WSPR. Select the mode and Start decoding. For SDR reception choose USB and a filter wide enough to include the desired audio, typically 3–4 kHz. Tune to the appropriate amateur dial frequency for your band and region. The decoder searches audio 100–4000 Hz for FT8/FT4. WSPR processes the narrow region around 1500 Hz; use the normal WSPR USB dial convention, not the transmitted RF center.

FT8 collects 15-second windows, FT4 7.5-second windows and WSPR 120-second windows. Wait for the next complete window and decoding time. The first partial window is discarded. FT8/FT4 include one-second overlapping margins and normally decode one second beyond the slot end. Live input uses PC UTC with optional decoder-only NIST/RX corrections; I/Q replay uses recorded UTC start plus playback position. WAV playback uses elapsed time starting at zero. A WAV must contain a suitably aligned full slot; arbitrary recordings may need a time offset or padding. Time offset adds seconds to the scheduling clock. Retunes, seeks and source changes discard incomplete windows.

FT8/FT4 reports basic supported text, audio frequency, approximate DT, raw window offset and sync score after LDPC/CRC checks. The compact view additionally shows a decoded-tone power SNR estimate (2500 Hz noise reference). Sync score remains separate in diagnostics; window offset is not a calibrated clock-error estimate. The lightweight library has experimental message support; hashed or unsupported message types may remain unresolved/absent. Contact logging remains planned. An operator-controlled HackRF FT8/FT4/WSPR transmit workflow is available through Transmit; see FT8, FT4 and WSPR transmit Help for exact supported formats, sequencing, timing and authorization. Optional PSK Reporter reception uploads are available in the collapsed PSK Reporter section and remain off until explicitly enabled; see PSK Reporter Help.

WSPR reports callsign, locator, power, audio frequency, SNR and drift. WSPR does not have a message CRC, so rows retain a no-CRC confidence label. The weak-signal view includes WSPR rows while retaining their no-CRC diagnostics in Inspect. Decode evidence does not authenticate a station.

See Weak signal timing for NIST synchronization, received-station consensus, clock-drift tracking and manual correction. Use stable receiver tuning/PPM. Listening volume and listening squelch do not set the decoder level. Overruns are shown explicitly; stop/restart after a backlog. Do not enable modes on every receiver unnecessarily.

Generated full-slot samples are in Docs/Samples: synthetic-ft8.wav, synthetic-ft4.wav and synthetic-wspr.wav. Enable the matching decoder before Open WAV in Audio. The WSPR sample lasts two minutes. These synthetic checks do not replace live HF acceptance.
