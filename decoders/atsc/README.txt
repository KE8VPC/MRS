M.R.S. ATSC 1.0 isolated receive worker

atsc_worker.py is supplied in source form under GPL-3.0-or-later; see GPL-3.0.txt.
The flowgraph uses GNU Radio's public ATSC blocks and follows its ATSC receive reference pipeline:
https://github.com/gnuradio/gnuradio/blob/v3.10.12.0/gr-dtv/python/dtv/atsc_rx.py
https://github.com/gnuradio/gnuradio/blob/v3.10.12.0/gr-dtv/python/dtv/atsc_rx_filter.py
GNU Radio source: https://github.com/gnuradio/gnuradio/tree/v3.10.12.0
GNU Radio license: GPL-3.0-or-later.

No precompiled TV decoder runtime is redistributed in this folder. Setup downloads the exact Windows/noarch packages in runtime-lock.txt directly from conda-forge. Package hashes are included in that lock. Packages retain their individual licenses and source references in conda-meta and their package info/licenses and info/recipe entries in the setup cache.

Preserve the dependency order of runtime-lock.txt: GLib and other runtime libraries must be linked before GDK's post-link helper runs. Alphabetically sorting this explicit lock causes DLL-not-found setup failures. Preview2 corrects the order without changing package versions or hashes. Probe checks the GLib DLLs, GDK helper and playback tools before reporting ready.

Runtime components include GNU Radio, Python, NumPy, FFmpeg/ffplay/ffprobe and their dependencies. Feedstock recipes identify upstream source versions and build changes:
https://github.com/conda-forge/gnuradio-feedstock
https://github.com/conda-forge/ffmpeg-feedstock
https://github.com/conda-forge/python-feedstock
https://github.com/conda-forge/numpy-feedstock
https://ffmpeg.org/download.html

Setup helper: micromamba 2.9.0 (BSD-3-Clause), downloaded by the application, not bundled.
https://github.com/mamba-org/mamba/tree/2.9.0
Download SHA-256: 97a336f4ab794bd96a6a4da5e6ed63e75a1d31830414a182419b23d3b36f3fe0

The main application sends unsigned 8-bit interleaved I/Q to the worker's stdin. The worker owns no physical receiver. JSON status uses stdout; decoded MPEG-TS is written to the selected file and optionally sent to a loopback-only UDP playback endpoint. No RF transmission is performed.

This is ATSC 1.0, not ATSC 3.0. See Docs/HDTV.md for operation, acceptance and limitations.
