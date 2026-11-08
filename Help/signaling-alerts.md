# SAME / EAS, DTMF, MDC1200 and FleetSync
Context: Signaling alerts
Keywords: SAME EAS NOAA weather warning alert FIPS county DTMF tones MDC1200 FleetSync ANI unit ID

Open Public Safety / Business → Signaling / alerts. Enable the decoders you want; Stop all disables this group. NFM uses the discriminator before listening squelch. AM and WFM use receiver audio. Windows audio input and WAV playback also work. Starting another source, tuning, or seeking resets acquisition. These decoders can run alongside existing channel decoders.

SAME / EAS reports the originator, event code, six-digit area codes, duration HHMM, UTC Julian day/time JJJHHMM, and sender. The bundled multimon-ng helper requires two of three matching headers. The original header remains in decoder JSON. End markers are shown separately. This is a monitor; it does not authenticate a broadcaster, relay alerts, sound an alarm, or guarantee emergency notification. Keep a dedicated weather/emergency alert receiver for that purpose.

DTMF displays detected digits 0–9, A–D, star and pound. DTMF has no checksum. Tone candidates can be false detections in speech; no commands are sent.

MDC1200 displays unit ID in decimal/hex, opcode, argument and single/double frame fields. FleetSync displays source and destination fleet/unit, raw command/subcommand, FleetSync II flag and payload bytes. Packet checks belong to the decoder; an ID alone does not establish an organization or person. Unimplemented application payloads remain raw bytes. This pass does not send acknowledgements or radio commands.

Each view includes Clear, keyword search, oldest/newest order, raw details and export. Show unverified is enabled initially so tone/no-CRC results remain visible. Clearing history does not stop reception. Data must be intelligible in the selected receiver passband; start with the appropriate NFM filter, stable gain and no clipping.

Generated examples are in Docs/Samples: synthetic-same.wav, synthetic-dtmf.wav, synthetic-mdc1200.wav and synthetic-fleetsync.wav. Start the relevant decoder, then Open WAV in Audio. These are generated test signals, not local transmissions.
