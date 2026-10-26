# Acceptance - 0.37.0-preview1

## New checks

1. Spectrum/waterfall snapping: start a live receiver, choose 6.25 or 12.5 kHz step, select Snap: tuning step and click slightly off a known center in both plots. Both must tune to the same channel grid. Turn Snap off and verify free tuning. Ctrl marker selection and marker correction must remain separate.
2. Bandplan snapping: enable a plan with explicit channel points (FM broadcast or a known custom/site channel plan), select Snap: bandplan and click close to a point. It must select that center; distant points must not attract a click. Outside the eight-pixel tolerance, selected-step snapping applies. Verify persistence after reopening M.R.S. Manual entry retains full precision.
3. MP3 VOX: start a listening source, open Recording > Automatic recording / VOX, choose MP3 audio, set a folder, threshold, pre-trigger, quiet tail and maximum duration, then Arm MP3 VOX. Speak/receive a known voice transmission. Wait for quiet tail/encoding, then play the .mp3 in another application. Test muted monitoring/volume zero: recording must still contain the gated audio.
4. Digital voice: receive a clear P25/DMR/NXDN voice call. It should record decoded voice, not the raw digital buzz. On a control channel without decoded audio, VOX should stay waiting. Review VOX threshold and RF squelch if background noise keeps it open. NXDN voice acceptance remains separate from this recording feature.
5. Stereo/continuous recording: enable Stereo MP3 for FM broadcast; check both channels. A long transmission must split into bounded clips. Stop/disarm and retune must finalize old clips without joining different channels. Pre-trigger/tails are approximate within processing-block timing.
6. Both: with a live SDR choose Both MP3 + I/Q, arm, receive audio and verify playable MP3 plus .mrsiq files. They use related audio triggers but separate buffers/tails and are not exact synchronized pairs. Each recorder has its own storage limit.
7. Check MP3 file count/status and folder. Temporary .pending.wav files should disappear after successful encoding. If encoding fails, the error must name the retained WAV; do not delete diagnostic evidence until reviewed. Windows Media Foundation MP3 support is required. Pending encoding must finish before rearming.
8. Aviation frequency entry: confirm the text is horizontally and vertically centered.
9. Sensor inspector: confirm Current run counters and retained table totals are clearly separate after Start/Stop/Clear. Zero/one-transition events must be described as envelope activity, not an identified protocol.

## Carried forward

Continue unfinished 0.36 checks: Broadcast AM/FM/MPX layout, Audio sidebar, offline searchable Help, custom bandplan import/edit/export/delete, structured NXDN display/save workflow. Also retain supported sensor/meter live decode, burst inspector, P25 workspace/site collection, concurrent real CW ID, known NXDN voice, decoder setup helper and folder scan squelch/trigger/hold review.

Already user accepted: ADS-B/offline map/trails, main auto squelch, automatic I/Q, paging, railroad bandplan, workspace contrast, spectrum navigation, frequency manager, P25 gain, DMR voice heard, and NXDN48 control data (system 289/site 12/RAN 12). Reopen only if a regression appears. New click snapping and MP3/Both recording have not yet received live user acceptance.

P25/DMR trunk following remains unfinished. Broader sidebar cleanup and APRS TX intermittency remain deferred. No live hardware or transmission was used for build verification.

Build: __________ Last completed check: __________ Settings/result: __________
