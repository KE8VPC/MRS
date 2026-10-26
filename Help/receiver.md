# Receiver setup and tuning
Context: Receiver
Keywords: HackRF RTL SDR sample rate gain offset HF frequency mode filter

Select the receiver family, Find, then the device. Set frequency, receive mode, filter width and sample rate before Start / Apply. Stop ends the source. Frequency and mode share a row.

## HackRF
LNA and VGA affect hardware gain. Raising them too far can overload the receiver. Capture offset moves the hardware center away from the listening frequency; it is independent of display centering. HF hardware filter limits usable capture to 1.75 MHz even at a higher sample rate. Disable it for broad aviation, meter or broadcast captures that need more coverage.

## Tuning
Enter a frequency in MHz and press Enter, or use the spectrum tuning controls. Saved entries keep their mode/filter. Click-to-tune requires a running SDR. I/Q replay follows recorded settings and does not retune hardware.
