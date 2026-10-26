# CW IDs and P25 Phase I voice — 0.29.0-preview1

## CW IDs alongside DMR

Tune the channel in NFM, leave DMR identifiers enabled, then open Data and enable **CW IDs**. When a Morse tone ID occurs, its decoded text appears as a CW / Morse observation with frequency, time, source, estimated WPM and tone frequency. CW mode is also supported. Expand Decoder status and help for worker status and dropped-input counts.

These are **unverified candidates**, visible without Show unvalidated. Morse has no checksum. Check the result by ear before using it as a station label. The original frequency-list name is not changed; automatic repeated-ID association is a future step. The decoder listens for tones from 300 to 1600 Hz and is tested at 12, 20 and 25 WPM. Noise, fading, mixed speech and digital bursts can cause missing or incorrect text.

This input path supports SDR reception and I/Q replay in NFM/CW. Generic microphone/audio-file CW decoding is not wired into this preview. The included synthetic-cw-id.wav is a listening reference, not an end-to-end file-input acceptance test.

## P25 clear voice

Tune an actual **Phase I C4FM voice channel**, select NFM and normally 12.5–16 kHz bandwidth. Enable **P25 I voice** in Data and enable audio monitoring/output in the receiver. P25 Phase I control-data decoding can remain enabled independently. Enabling voice replaces NFM discriminator listening audio with decoded voice; uncheck it to hear ordinary NFM again. Other modulation modes keep normal audio.

The decoder extracts IMBE voice, uses mbelib for synthesis and validates HDU/link-control/encryption-sync metadata with forward-error correction. Clear algorithm 0x80 is required. Unknown encryption state, unsupported/encrypted algorithms (including AES-256 0x84), bad metadata and severely damaged voice frames remain muted. Status shows algorithm/key identifiers where available. **No encryption keys are accepted and AES decryption is not implemented.**

This is manually tuned Phase I C4FM voice: no automatic trunk following, Phase II, DMR voice, or CQPSK/LSM equalizer. A control channel alone generally does not carry the conversation. Late entry may remain muted until valid encryption metadata arrives. Retuning, stopping and replay gaps discard old decoder/audio state. Processing uses a bounded worker per receiver; overruns reset acquisition and increment the dropped count.

Native support must remain beside MRS.exe under Decoders/Voice. Source, upstream copyright notices and build instructions are included there. The synthesizer's 8 kHz output is repeated to the existing 48 kHz stereo audio path.

## Local evidence and remaining acceptance

Independent OP25 p25craft-generated clear HDU/LDU1/LDU2 vectors recovered exactly 18 IMBE frames and a roughly 1012 Hz tone from the 1011 Hz reference codeword. AES-marked and unknown-encryption-state cases produced no audio. Noisy synthetic CW recovered KE8VPC at 12/20/25 WPM; broadband noise produced no ID in the test. UI smoke exercised bounded CW delivery and actual receiver I/Q replay for P25 voice.

These checks do not establish live speech intelligibility, real DMR/CW coexistence, simulcast support or long-duration performance. Please use ACCEPTANCE-0.29.0-preview1.md with your local channels.

Fixture provenance: https://github.com/boatbod/op25/blob/master/op25/gr-op25_repeater/apps/tx/p25craft.py (Michael Ossmann and contributors, GPL-2.0-or-later). Only generated protocol vectors are embedded, not OP25 decoder code. Parameters: NAC 0x293, TG 123, source 456, IMBE codeword 38928490d433c0be1b91844ff058a589d839, clear ALG 0x80/key 0 and encrypted-state negative test ALG 0x84/key 7. All bundled new recordings are synthetic.
