# SSTV image reception

Context: SSTV
Keywords: Amateur SSTV slow scan television image Martin M1 M2 VIS slant PNG WAV

Open Amateur → SSTV. Start the receiver in USB or LSB for HF, or AM/NFM for a compatible audio source. The passband must include 1100–2300 Hz. Windows audio input and WAV playback use the same 48 kHz audio decoder.

Choose Auto for Martin M1 and Martin M2 VIS detection, or choose one mode manually to acquire line sync during an ongoing image. Martin M1 receives 320×256 pixels and Martin M2 receives 160×256 pixels. Other VIS modes display their number as unsupported. A failed VIS parity check cannot start automatic reception.

Select Start receiving before the header arrives. The image appears progressively. Slant ppm corrects timing within each line. Tone offset Hz compensates an audio frequency offset. Apply either setting by starting reception again. Shift pixels and Align adjust the displayed image horizontally.

Stop retains the image. A receiver retune, replay seek, input change or source stop ends the current decoder and retains the received rows. Start receiving again after the change. Clear image stops reception and removes the displayed pixels.

Save PNG stops reception to save a stable image. The JSON file beside it records the mode, UTC start, source, receive frequency when available, replay position, row count, completion state, and alignment settings. A partial image retains its partial status. Ordinary WAV input has no RF frequency unless the source supplies one.

The initial decoder supports Martin M1 and M2. Robot, Scottie, PD and other SSTV modes need separate implementations and acceptance checks. WEFAX has its own decoder under Weather.

Timing and independent synthetic audio checks use the pySSTV Martin encoder as a reference. M.R.S. implements its own receiver.

https://github.com/dnet/pySSTV/blob/master/pysstv/color.py
https://github.com/dnet/pySSTV/blob/master/pysstv/sstv.py
