# Higher-detail RF spectrum and waterfall - 0.17.0-preview3

Open Display > RF spectrum detail. Select 4,096, 16,384, 32,768 (default), or 65,536 points. The choice applies live to SDR reception and I/Q replay, saves with display settings/presets, and does not retune hardware or change audio, decoding, gain, offset, or recorded I/Q.

The prior RF path used a 2,048-point FFT reduced to 1,024 displayed bins. The new path retains every bin of the selected transform, using consecutive real samples across USB blocks. Zooming now reveals retained frequency detail rather than stretching a 1,024-bin row. A Hann window is used; displayed Hz/bin describes bin spacing, not an absolute guarantee of resolving two signals at that spacing. Strong signals and window leakage still affect resolution.

At 9.6 MS/s, spacing is 2,343.75 Hz at 4K, 585.94 Hz at 16K, 292.97 Hz at 32K, and 146.48 Hz at 64K. At 20 MS/s, 64K spacing is 305.18 Hz. Higher detail requires a longer sample window and more CPU. The first frame waits for a full sample window; there is no zero padding. If the PC struggles, reduce points or refresh rate. RF frames are still limited to about 30 per second; this is not a continuous, guaranteed transient detector.

The waterfall raster follows panel size and Windows display scaling, capped at 4,096 columns and 1,600 rows instead of the previous 768 by 400. Peak-preserving pixel reduction keeps narrow strong signals visible when zoomed out. Existing time gaps remain blank. Changing resolution clears old display history and held peaks to prevent mixing different frequency grids. MPX/audio FFT resolution is unchanged.

Waterfall history stores one byte per frequency bin with 1 dB level steps; the current spectrum trace and peak hold retain float precision. Frequency bins are not discarded. History retention remains at most 125 seconds and 6,000 rows, additionally bounded to 256 MiB per view. Normal maximum 64K / 30 fps / 120-second history fits that budget. Clear/source changes release history. Relative levels are not calibrated RF power measurements.

Validation includes known positive/negative-frequency tones at every size, amplitude consistency, uneven USB block boundaries, input immutability, capture/resolution resets, close-carrier separation, history storage and display preference persistence. The 64K transform uses directly calculated double-precision roots to avoid a precision failure in the older FFT routine. Synthetic UI images are not off-air signals.

Station check: keep the existing sample rate and offset, select 32K or 64K, and revisit the same zoomed P25 channel view. Expect finer signal edges and narrower visible detail. Verify that audio stays smooth while zooming/panning and changing detail; compare 16K if CPU load becomes noticeable. This build has not been accepted on the user's live receiver yet.
