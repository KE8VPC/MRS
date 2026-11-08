# Discovery references and receive presets
Context: Bandplans Amateur Aviation
Keywords: Discovery WWV ISM ACARS HF VOLMET HFDL reference import Tune presets repeater simplex FT8 FT4 WSPR

Common references give a starting point for identifying a signal. A frequency match is a candidate, not proof of a transmitter or protocol. Shared ISM bands contain many different uses. Reference labels do not enable transmission or add decoders.

## Amateur quick tuning

AX.25 / APRS and FT8 / FT4 / WSPR each have a preset selector, editable MHz field and Tune button. Select a preset, then click Tune with a live SDR running. Browsing presets does not tune. Mode and receive filter settings follow the chosen preset. The displayed MHz field follows subsequent receiver tuning, including Frequency Manager and scans. USB weak-signal presets are dial frequencies, not the frequency of an individual audio-offset signal. Incomplete weak-signal windows reset after tuning; wait for a full slot before expecting results.

Edit presets opens your own persistent table. Use Protocol APRS, Local, FT8, FT4 or WSPR; Local entries appear alongside APRS for repeater/simplex listening. Enter the receive frequency, mode, supported bandwidth in Hz, label and notes (including any tone information). Add in the blank bottom row and select a row then Delete to remove it. Save validates before applying. Cancel/closing leaves saved presets unchanged. Bundled common references remain editable CSVs; your local presets are stored separately in the receiver profile. JS8 presets/decoder remain future work.

## Add reference channels to Frequency Manager

Open Analysis > Bandplans > Add reference channels to frequencies. Choose a channels.csv file from the working reference folder. Select desired rows (Ctrl/Shift for multiple); candidate/unverified rows start unselected. Add selected creates a service/reference folder. Existing matching frequency, mode and protocol entries are preserved, including labels. An unsupported/invalid batch is not imported. Undo remains available in Frequency Manager. No data is silently added to the saved station library at startup.

The working bandplan folder receives missing supplied discovery CSVs without replacing existing files. Use Reload CSV after edits. If the app is installed in a read-only folder, choose a writable bandplan directory and use Create missing reference CSVs. Check Help > Bandplans for schemas and editing.

## Included discovery additions

Separate layers cover VHF ACARS, VDL2, HF aviation voice, VOLMET, HFDL references, Amateur operating presets, WWV and selected U.S. ISM ranges. Coverage is a starter, not exhaustive. Source/region/mode notes are retained in CSVs and marker details. ACARS candidates are distinguished from published references. 136.975 MHz uses VDL2, not conventional ACARS. HFDL channel references do not imply an HFDL decoder. HF reception depends on propagation and working frequencies; WWV 25 MHz is an experimental reference. U.S. ISM ranges are not a list of all sensor/remote frequencies; 315/433 MHz use and terminology vary by jurisdiction.

## Aviation scans and messages

Aviation > Messages / Inspector holds ACARS, VDL2, ADS-B/Mode S and UAT messages, with filters, newest-first, clear, raw details and export. Aircraft / map retains surveillance/map controls. Decoder selection survives channel changes and reacquires on compatible captures. Explicit Stop remains stopped. Incompatible sample rate, capture bandwidth or fixed ADS-B/UAT target shows a reason rather than claiming reception. Scanning a single receiver visits one channel at a time; it cannot keep receiving a channel it has left. Use an adequate scan dwell for packet acquisition; FT/WSPR need full slots. HD Radio and HDTV retain explicit restart requirements on receiver changes.

## Receiver and recording details

Receiver selection/reconnect needs a unique non-default serial; Stop cancels retries. HackRF uses quadrature I/Q, without an RTL direct-sampling branch. Gain selections apply live. PortaPack requires HackRF USB mode; RF antenna power remains off and hardware changes briefly pause USB reception.

Decoder setup checks keeps the protocol selector and Apply recommended settings. Applying stops reception, finalizes recordings and disables automatic modulation changes. It preserves gain and does not start a decoder. Start / Apply the receiver afterward. Errors and application results remain visible; verbose diagnostic/recommendation text was removed from the sidebar.

Automatic I/Q uses the calibrated RF trigger; MP3 uses squelched listening audio before speaker volume, including decoded voice. MP3 mono is 64 kbps; stereo is 128 kbps. Both uses audio VOX for both recorders. Settings apply at Arm; Stop/disconnect disarms and retuning starts another clip. Temporary PCM counts toward storage; failed encodes retain WAV evidence and show an error. These details moved out of the permanent sidebar; controls and status remain.
