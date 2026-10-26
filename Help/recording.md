# Manual and armed I/Q recording
Context: Recording
Keywords: arm automatic pre trigger tail storage maximum IQ replay wav

Manual Record I/Q saves the captured RF span to .mrsiq. Finish I/Q finalizes it. WAV recording saves demodulated audio. Keep original I/Q files for decoder debugging; WAV loses RF information.

Arm automatic I/Q recording to retain pre-trigger samples, trigger on activity and continue through the quiet tail. Maximum clip duration segments continuous activity. Storage limits prevent unlimited growth. Threshold/margin and capture-span settings affect triggers; test with a known signal.

Open I/Q recording to replay without hardware. Pause, seek and stop reset partial decoder context so fragments from different points do not join. Replay observations are labeled separately from live observations.
