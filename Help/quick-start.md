# Quick start and workspace guide
Context: Quick start
Keywords: setup first start device family frequency rate gain mode filter decoder

## First reception
Extract the complete Windows ZIP. Select Receiver, choose RTL-SDR or HackRF/PortaPack USB mode, Find and select the physical device. If needed use Set up RTL library; check the appropriate USB driver. Enter MHz, select modulation, filter and sample rate, then Start / Apply. Wait for capture status and spectrum activity. Select listening output in Audio and enable monitoring only when wanted.

Use a known local station to establish reception. For FM broadcast use WFM, about 180–200 kHz receive width and enough sample rate to fit the channel. For narrow FM start around 12.5–16 kHz and adjust for actual occupied bandwidth. These are starting points, not universal settings or allocation rules. Excess gain may overload an SDR; more gain is not always better.

## Pick the right workspace
Spectrum shows RF and common navigation. Amateur contains APRS/AX.25. Commercial contains P25, DMR/LTR/NXDN, paging, trunk following and talkgroups. Analysis contains sensors/meters/bursts and other observations. Broadcast contains AM/FM/HD/TV. Aviation contains aircraft decoding and map. One receiver has one live/replay source; parallel workspaces do not create extra physical radios.

## Tune and inspect
Click-to-tune uses the configured snap/grid option. Follow tuned, Center lock and Free view control viewport behavior, not hardware calibration. Zoom changes visible span; receive filter changes decoder/audio bandwidth. Capture offset moves HackRF capture away from the desired channel to avoid the center spike. PPM corrects consistent RX oscillator error. Read Spectrum and PPM for details.

## When a decoder has zero results
Confirm the correct protocol/baud/profile, enough capture/filter width, appropriate signal level and a fresh stream. Voice-like sound alone does not establish a protocol. Inspect validation and dropped counts; a pulse burst is not a decoded device. Start the relevant decoder separately where its screen requires Start. Missing metadata remains unknown, never zero-filled. Real RF acceptance is separate from synthetic/replay tests.

F1 or ? opens searchable offline Help. Search all words such as 'control voice', 'PPM', 'MP3', 'ADS-B', 'FLEX' or 'missing helper'. Clear the search to browse every topic. About lists dependencies and their source notices.
