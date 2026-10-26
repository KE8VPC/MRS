# M.R.S. 0.37.0-preview1 - click snapping and MP3 VOX

- Optional spectrum/waterfall click snapping: Off, selected tuning step, or nearby explicit bandplan channel with step fallback. Added 6.25 kHz to the navigation step list. Entry/storage precision is retained.
- Automatic recording formats: existing I/Q, MP3 audio VOX, or Both MP3 + I/Q driven by audio activity. Mono 64 kbps and stereo 128 kbps; pre-trigger, quiet tail, bounded clips/queues/storage, background encoding and explicit error recovery with retained PCM evidence.
- Aviation frequency entry centered horizontally and vertically.
- Sensor status separates current-run counters from retained table rows. Sparse envelope events no longer suggest an identified OOK protocol.

Extract the complete ZIP into a new folder; keep Help, bandplans, Receivers and Decoders beside MRS.exe. Existing profile settings and frequency data remain. Do not replace files in a running installation. See ACCEPTANCE-0.37.0-preview1.md for new and carried checks. Windows Media Foundation supplies the MP3 codec; no external encoder download is required on the tested Windows installation.

No trunk following or broader sidebar redesign is added. MP3 records listening audio, not raw I/Q or discriminator data. Both produces related MP3 and I/Q captures with separate buffers/tails. User recordings are not bundled. The running installation was not replaced.
