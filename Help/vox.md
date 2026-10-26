# Automatic MP3 VOX recording
Context: Recording
Keywords: MP3 VOX automatic audio record pre trigger tail stereo mono I/Q both threshold

Recording > Automatic recording / VOX offers I/Q (RF trigger), MP3 audio (VOX), and Both MP3 + I/Q (VOX). Existing I/Q preferences remain the default when upgrading.

## MP3 setup
Start a live receiver, audio input or file playback, choose MP3 audio, set a folder, and Arm MP3 VOX. Set Audio VOX threshold above quiet audio but below speech. RF squelch should already suppress receiver noise. VOX follows squelched listening audio before speaker volume, including decoded P25/DMR/NXDN voice; muted speakers and volume zero do not mute the recording.

Mono uses 64 kbps MP3; Stereo MP3 uses 128 kbps and is useful for FM broadcast. Pre-trigger and quiet-tail settings retain context. Continuous audio creates successive clips at approximately the maximum duration (within one processing block). Stop/disconnect disarms; retuning or changing audio source ends the old clip and starts fresh pre-roll.

## Combined mode
Both requires live SDR, records MP3 from audio and triggers the I/Q recorder from the same audio activity. Each recorder retains its own pre-buffer/tail; they are related captures, not byte-synchronized files. A continuous digital control channel without decoded audio should not trigger audio VOX. I/Q-only mode still uses the calibrated RF threshold.

## Files and limits
MP3 compression runs on a background worker using Windows Media Foundation. Complete MP3s have frequency (when available), UTC time, receiver slot and a unique suffix in their names. Temporary .pending.wav files are PCM spools, removed after successful encoding. An encoder failure preserves that WAV and reports its path; it does not pretend the MP3 succeeded. Windows must provide an MP3 encoder (Windows N editions may require the Media Feature Pack).

Storage limits apply per recorder. MP3 budget accounting includes pending PCM plus completed MP3s, so temporary data cannot grow without a limit. Encoder queues are bounded; overload stops recording with an explicit error. Wait for pending encoding before arming another MP3 session. Disarm / finish clip finalizes the current capture. Original I/Q and manual WAV functions remain available; MP3 is lossy and is not suitable for raw discriminator decoding.
