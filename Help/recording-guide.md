# Recording choices and playback troubleshooting
Context: Recording guide
Keywords: record vox automatic MP3 I/Q Both arm pretrigger tail storage playback digital

## Choose evidence or listening audio
Manual/automatic I/Q preserves captured RF samples for decoder research. Another ordinary audio player cannot play .mrsiq as sound. Open I/Q in M.R.S. restores the recording's capture/channel metadata. MP3 audio is suitable for ordinary media players. Both records MP3 plus RF evidence when audio VOX triggers. None of these modes recovers packets lost before capture or makes an encrypted call clear.

## Arm automatic recording
Open Recording, choose I/Q (RF trigger), MP3 audio (VOX) or Both MP3 + I/Q (VOX), select a folder and set thresholds/timers/storage budget. Arm, then monitor status. Arming waits for a trigger; it does not immediately record everything. I/Q uses the calibrated RF trigger; MP3/Both use selected listening-audio level before speaker volume, including supported decoded voice.

Start with a modest pre-trigger (for example 1 s), quiet tail (2 s) and bounded maximum clip length. Adjust threshold above the actual noise floor while retaining wanted signals; these are examples, not guaranteed values. MP3 FM stereo is optional. Check a short known signal in the saved file before relying on unattended collection.

## Files and errors
MP3 encoding runs in the background through Windows Media Foundation. Temporary PCM/WAV counts toward the budget. Failed encoding retains WAV evidence and reports an error. Disarm finishes the current clip; stopping the source and retuning can end a segment. Manual live I/Q and automatic I/Q include RX PPM provenance. Replay never applies the live-device correction again.

Check clip boundaries, expected followed frequency/TG context, decoded audio and stereo mode. Five receivers may exceed disk/USB/CPU capacity at large sample rates. Keep source recordings unchanged when making derived analysis/audio files. Clearing a monitoring view does not delete recording files or disarm recorders.
