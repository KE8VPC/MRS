# Test all four features — 0.19.0-preview1

Extract the entire ZIP into a new folder and run MRS.exe. Keep Decoders and Receivers beside it. Existing settings remain in the Windows user profile. Your existing TV runtime does not need reinstalling.

1. **Saved-frequency clicks:** click one saved signal in receiver A and confirm frequency, mode and filter apply. Repeat in B; A should stay unchanged. Folder clicks and drag-to-organize should not tune. Click the same signal again after tuning elsewhere and confirm it returns.
2. **TV references:** in Display > U.S. band reference, enable TV RF channels and optional ATSC 1.0 pilot references. At RF 15, expect boundaries 476–482 MHz, center 479 MHz and nominal pilot 476.309441 MHz. Check zoom, hover, light/dark colors and below-spectrum placement. These references do not certify that a station is present.
3. **Automatic RF squelch:** start live reception, open Scan and check Automatic RF squelch. Wait for calibration, then watch the estimated floor and gate threshold. Enable the lines and vary the margin. Move the Receiver tab RF threshold slider to confirm manual override; re-enable automatic control afterward. Test with an idle channel and a real signal.
4. **Locked activity scanner:** choose an NFM mode/filter and a span containing two or more known channels, preferably including intermittent traffic. Set the correct grid or leave estimated center, then Start scanner. Verify the capture-frequency readout and spectrum viewport stay fixed while the listening frequency changes. On HackRF, leave your working capture offset enabled for this check.

During step 4:

- Confirm it holds while a signal is active, then moves after the quiet delay. Use Skip on a persistent control channel to let other signals be heard. Hold should prevent hopping even when the channel goes quiet.
- Check Discovered signals in Frequencies after several seconds. Repeat activity should update the same entry, with counts/timestamps in its details. Existing names, notes and folders must remain intact.
- Confirm a saved-frequency click or manual tuning stops scanning. Gain/rate/filter changes should also stop scanning and recalibrate; Start explicitly resumes it.
- Stop scanning and verify zoom/pan work again. Close/reopen and check the saved Scan options; the scanner should remain stopped until you press Start.

The discovery estimate excludes capture-center artifacts and channels whose filter would cross usable edges. It is not a sweep beyond the captured bandwidth and does not identify modulation/protocol. See ACTIVITY-SCANNER.md for limits, exact controls and metadata behavior. Report the receiver, sample rate, FFT detail, mode/filter, margin, and whether a signal was missed, duplicated or tuned incorrectly if field behavior needs adjustment.
