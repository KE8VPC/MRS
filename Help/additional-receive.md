# Railroad, amateur CW, weather fax and cellular survey

## Commercial → Railroad

Tune HOT 452.9375 MHz or EOT 457.9375 MHz, then enable Decode HOT / EOT. The tune buttons select 12.5 kHz NFM on a running receiver. For an M.R.S. I/Q recording, open the capture through Recording and enable decoding. The receiver uses the recorded NFM channel and retains live/replay attribution. Folder scanning visits one channel at a time.

The view lists frames that pass the BCH check. EOT displays the transmitted unit number, pressure, charge field, motion and marker bits. A unit number alone does not identify a train. HOT shows the frame type bits and raw data with unresolved field meanings. The strong EOT recording produced two valid frames for the same unit at 88 psig. The faint HOT recording produced no BCH-valid frames.

Wire-format references: [FRA report](https://railroads.dot.gov/sites/fra.dot.gov/files/2023-02/WiDTL%20Phase3.pdf), [PyEOT](https://github.com/ereuter/PyEOT) and [SDRangel receive documentation](https://github.com/f4exb/sdrangel/blob/master/plugins/channelrx/demodendoftrain/readme.md).

## Amateur → CW

Start the SDR in CW, USB or LSB, or start Windows audio or WAV playback. Set the detected audio tone (Use receiver pitch copies the CW pitch), character WPM, and spacing WPM. For Farnsworth spacing set Spacing WPM lower than Character WPM. Start decoding to see a running transcript with letter and word spacing. The decoder uses the entered speed and pitch. A source change stops decoding and keeps the transcript. Clear text starts a clean view. The station-ID decoder is independent.

## Weather → WEFAX

Start an audio source or tune USB so the fax tones are near 1500 Hz for black and 2300 Hz for white. Select IOC and lines per minute, then click Start image at the beginning of image data. Stop preserves the captured lines. Use Shift pixels and Align to move the horizontal seam, or set Slant ppm and Tone offset Hz before starting the next image. Save PNG saves the aligned image. This preview uses manual start and stop and retains up to 2,400 lines.

Enable **reference/marine-wefax** in **Analysis → Bandplans** for U.S. marine radiofax markers. Markers are at the assigned RF center. The marker details give the USB dial frequency 1.9 kHz lower. For example, tune Boston NMF at 9.108100 MHz USB for its 9.110000 MHz assignment. **Add reference channels to frequencies** imports the USB dial values into **Marine / WEFAX**. Check the [NOAA station schedules](https://ocean.weather.gov/fax_schedules.php) for UTC broadcast times.

Reference: [Fldigi WEFAX manual](https://www.w1hkj.org/FldigiHelp/wefax_page.html).

In **Broadcast schedule and tuning**, choose a station and USB dial frequency. Select a NOAA session start or open **Station schedule** to find a chart and enter its UTC time. **Arm auto-tune** tunes the running receiver once, 30 seconds before the next occurrence. Start the image manually when the picture data begins.

## Weather → NOAA Weather Radio

Choose one of the seven channels and click **Tune NOAA** on a running SDR. Enable **Decode SAME headers** to see alert event codes, areas, issue time and duration. Enable **Detect 1050 Hz tone** to log a sustained attention tone. Both options follow the tuned NOAA channel during live SDR reception or I/Q replay.

[NWS guide to NOAA Weather Radio and SAME](https://www.weather.gov/nwr/usingsame)

## Analysis → Cellular survey

Start the HackRF, tune within the selected U.S. reference band, and select Start survey. The panel measures the intersection of that band and the usable RF capture. It shows ranges above the lower-bin floor estimate, peak dBFS per bin, and coverage. The few DC-center bins are excluded. The estimate can be biased upward when signals occupy most of the captured span. Measurements are relative and gain-dependent. The survey measures spectrum energy and does not identify LTE/NR cells or individual phones.

References: [FCC spectrum chart, Appendix B](https://docs.fcc.gov/public/attachments/FCC-20-77A1.pdf) and [FCC 3.45 GHz Service](https://auctiondata.fcc.gov/public/projects/auction110).
