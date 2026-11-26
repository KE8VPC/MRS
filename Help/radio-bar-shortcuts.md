# Radio bar, status bar and keyboard shortcuts
Context: Receiver
Keywords: radio bar frequency digits wheel tune start stop record volume squelch shortcut keyboard F5 Ctrl status bar RX TX

The radio bar under the title is always visible, whichever tab is open. It mirrors the Receiver and Audio tab controls, so a change in either place shows in both.

- **Frequency readout:** hover a digit and turn the mouse wheel to tune by that digit. While a receiver is running, tuning applies live. Double-click the readout, or press Ctrl+F, to type a frequency in MHz. Press Enter to tune or Esc to cancel.
- **Mode:** the same receive mode as the Receiver tab.
- **Start / Apply:** starts the selected SDR, or applies changed settings while it runs.
- **Stop:** stops the current source.
- **Rec:** records demodulated audio to WAV. Click it again to finish the recording.
- **Vol / Sql:** monitor volume and RF squelch threshold.
- **RX / TX pill:** green while receiving, red while transmitting.

The status bar at the bottom shows the active source, RF gate, audio level, any recording in progress and the latest receiver message.

## Keyboard shortcuts

| Key | Action |
|---|---|
| F5 | Start / Apply receiver |
| Shift+F5 | Stop source |
| Ctrl+F | Type a frequency |
| Ctrl+R | Start or finish WAV recording |
| PgUp / PgDn | Step up or down by the spectrum step size (when no text field or list has focus) |
| Ctrl+1 … Ctrl+9 | Open workspace tab 1–9 (Spectrum, Amateur, P25 …) |
| Ctrl+0 | Open the last workspace tab (Session log) |
| F1 | Search Help |
