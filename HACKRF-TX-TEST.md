# HackRF manual TX test — 0.26.0-preview1

This preview adds an explicitly triggered FM tone test, not APRS or microphone transmission. It opens unarmed, with no default frequency, TX gain 0 dB and duration one second. The limited test ranges are 144–148 and 420–450 MHz; these hardware limits are not authorization to use every frequency in those ranges.

## Try it

1. Select HackRF / PortaPack, put Mayhem in HackRF USB mode, and Find/select your device.
2. Open Receiver > HackRF offset / gain > Manual TX test. This stops the source in that receiver workspace, including recording/scanning. The other receiver workspace can monitor with a different device.
3. Enter a clear frequency you are authorized to use and tune your separate radio to the same frequency in FM. Use a suitable antenna or dummy-load arrangement. Do not connect HackRF output directly to another receiver input.
4. Leave gain at 0 dB and duration at one second. Enable one burst, then click Transmit test tone. Listen for a one kHz tone. Increase test gain in small steps only if needed; gain is not calibrated output power.
5. STOP TX or closing the test window stops the owned worker. Each new burst needs enabling again. Close the window and use Start / Apply to resume reception.

## Behavior

- Fixed one kHz FM tone with nominal ±2.5 kHz deviation. Signed I/Q amplitude is limited to 32 of 127; 8 MS/s and 1.75 MHz analog baseband filter.
- TX gain 0–20 dB, duration 1–3 seconds. RF amplifier and antenna power are forced off. RX gains and capture offset are not TX controls. The entered frequency is the transmitted center.
- Unique device identity is checked again before opening USB. Shared receiver ownership prevents simultaneous RX/TX on that device within the application; external applications are handled by the native USB open error.
- Native worker has a bounded sample count and elapsed-time stop, and stops on stdin STOP or parent-pipe closure. UI watchdog terminates an unresponsive worker after eight seconds. Stop/close waits briefly for shutdown then terminates only its owned worker if necessary.
- No automatic repeat, scheduled transmission, APRS, PTT/microphone input or automatic receive restart. TX parameters and arming are not saved.

## Validation

Software checks validate allowed settings, signed I/Q waveform, tone phase/deviation, initial unarmed UI state, and all existing receiver tests. Native transmit start/stop, RF frequency, output spectrum and successful reception still require hardware acceptance. TX_STARTED means the native API accepted start, not proof of RF output. A received tone is the first hardware acceptance milestone; clean emissions require separate RF measurement.

Native API references: Great Scott Gadgets libhackrf hackrf_start_tx, hackrf_stop_tx, hackrf_set_txvga_gain and hackrf_transfer callback contracts. Existing native library and licenses remain bundled.
