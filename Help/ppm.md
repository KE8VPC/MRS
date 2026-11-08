# RX frequency calibration / PPM
Context: PPM
Keywords: ppm calibration frequency correction oscillator offset drift RTL HackRF

Use Receiver > Frequency calibration / PPM to correct a receiver whose oscillator consistently places a known carrier at the wrong frequency. This release applies fractional frequency-command correction to RTL-SDR and HackRF RX. It does not change TX, resample the sample clock or automatically measure oscillator drift.

## Set and save a correction
1. Select the intended physical device using Find. Let it warm up before calibration.
2. Stop the receiver. Stop finalizes recordings and releases following assignments.
3. Enable RX frequency correction and enter signed PPM, between -200 and +200. Use a decimal point; for example 1.25 or -0.75.
4. Save correction, then Start / Apply. The running status shows applied PPM, nominal capture center and hardware frequency command.
5. Repeat using a stable known-frequency carrier. Set Reset to zero and Save correction to remove a saved adjustment.

Positive PPM increases the hardware tune command: at 100 MHz, +1.25 PPM requests 100,000,125 Hz. Negative requests a lower frequency. Choose the sign that moves a known carrier toward its correct marker. Carrier offset divided by reference frequency, multiplied by one million, estimates the magnitude; confirm direction experimentally. A transmitter itself can be off frequency, so compare stable references at more than one RF frequency.

## What stays separate
Saved channel frequencies and decoder targets stay on nominal RF coordinates. The correction is applied at the native tune boundary for every retune, including scanning and dedicated voice assignments. Capture offset remains a separate method to move the desired signal away from the hardware center spike. Tuning step and snapping select channel grids; neither calibrates an oscillator.

Correction is saved by unique device identity within the receiver workspace. If the serial is absent, generic or duplicated, Save explains that it cannot safely persist that identity; manual session correction is still possible. Moving a uniquely identified device to another workspace may require entering its correction there. Ambiguous devices are never automatically assigned another device's calibration.

Manual and automatic I/Q files record the applied PPM as provenance while preserving samples and nominal channel/capture metadata. Replay does not retune hardware or apply a live correction again. Old recordings default to zero when no correction metadata exists. Near hardware tuning limits a corrected command may be rejected rather than silently clamped.

## If it still drifts
Check antenna, overload, reference-transmitter stability, warm-up and sample-clock effects. This frequency correction does not promise laboratory frequency or time accuracy. Changes require Stop and a new Start; editing the field does not alter an active capture.
