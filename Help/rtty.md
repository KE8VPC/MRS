# RTTY text reception

Context: RTTY
Keywords: Amateur RTTY radioteletype Baudot ITA2 mark space shift reverse text audio WAV

Open Amateur → RTTY. The decoder receives five-bit US ITA2 at 45.45, 50 or 75 baud. Choose the baud rate and the signal's mark tone. The space tone is the mark tone plus the selected 170, 425 or 850 Hz shift.

The initial settings are 45.45 baud, 2125 Hz mark and 170 Hz shift. Reverse swaps the mark and space interpretation. Match the settings to the station. USB, LSB, AM and NFM demodulated receiver audio, Windows audio input and WAV playback can feed the decoder. Use a passband wide enough for both tones.

Select Start decoding. The transcript follows the received letter and figure shifts. The status shows decoded characters and rejected frames. RTTY has no checksum in this format, so text can contain errors even when framing passes.

Stop retains the transcript. Clear text clears it. Save text exports the current visible transcript. A retune, seek or input change stops the decoder, and Start decoding starts it again for the new source.

The receiver supports the US ITA2 figures alphabet. ASCII, national figures variants, SITOR/NAVTEX error correction and SYNOP field interpretation need separate support.

https://www.w1hkj.org/FldigiHelp/rtty_page.html
