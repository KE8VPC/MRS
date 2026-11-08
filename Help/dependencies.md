# About, external components and third-party sources
Context: Dependencies
Keywords: about licenses source dependencies DLL EXE helper driver missing install external files

Open About at the top of M.R.S. Select a component row to see its purpose, version/revision, location, official source and notices. Official source opens the browser only when clicked. Open component folder / notices opens the local folder. Present means the expected file exists; it is not a runtime health check or license audit. Use the relevant setup/probe for an operational check.

## Keep the complete ZIP
Extract the entire release into a new folder. Do not copy MRS.exe alone. Keep Receivers, Decoders, Help and bandplans in their original relative locations. Bundled native helpers run locally; their sources/build notes and license notices remain alongside them. Some features also need a Windows USB driver or a separately installed runtime.

HackRF uses Receivers/HackRF, host source v2024.02.1, plus libusb/winpthreads. Source: https://github.com/greatscottgadgets/hackrf . See SOURCE-AND-BUILD.txt and its Licenses/Sources folders.
RTL uses the explicitly installed rtl-sdr-blog v1.3.6 library in local application data. Source: https://github.com/rtlsdrblog/rtl-sdr-blog . Set up RTL library handles its pinned download; a usable Windows USB driver remains separate.
DMR/NXDN voice uses Decoders/Digital/dsd-fme.exe plus companion Cygwin DLLs, revision 69d3115. Source: https://github.com/lwvmobile/dsd-fme . COPYRIGHT, DEPENDENCIES.txt, corresponding source and dependency notices describe the mixed upstream licenses.
P25 Phase I voice uses Decoders/Voice/mrs_voice.dll, with DSD revision 59423fa and mbelib 9a04ed5. Sources: https://github.com/szechyjs/dsd and https://github.com/szechyjs/mbelib . Read the exact per-file notices and bridge build notes; unrelated files in a source tree are not proof they were linked.
Paging uses Decoders/Paging/multimon-ng.exe, 1.6.2 with M.R.S. changes. GPL-2.0-or-later; source: https://github.com/EliasOenal/multimon-ng . Corresponding modified source and notices are included.
Aviation uses Decoders/Aviation/mrs-aviation.exe, based on xng revision 096c805. Source: https://github.com/airframesio/xng . Decoder core license is MIT OR Apache-2.0; additional crate notices and pinned source are included.
Sensors/meters use Decoders/Sensors/rtl_433.exe, revision 406f9a3 with a Windows stdin change, plus mrs-iq-channel.exe. rtl_433 is GPL-2.0-or-later: https://github.com/merbanan/rtl_433 . Both modified rtl_433 source and original M.R.S. bridge source are provided. Mayhem embedded apps are not loaded as Windows plugins.
HD Radio uses Decoders/nrsc5/nrsc5.exe and its companion DLLs. nrsc5 revision 9beb2c7 is GPL-3.0-or-later: https://github.com/theori-io/nrsc5 . FAAD2 and other dependencies have their own retained notices/source.
ATSC uses Decoders/ATSC/atsc_worker.py and a separate pinned runtime installed through Set up TV decoder. Python, GNU Radio, FFmpeg/player and dependencies are recorded in runtime-lock.txt; sources include https://github.com/gnuradio/gnuradio and https://github.com/mamba-org/micromamba-releases . Setup initially requires internet; runtime files live under local application data.
Audio uses NAudio 2.2.1 (MIT), https://github.com/naudio/NAudio . MP3 VOX uses Windows Media Foundation, not a separate FFmpeg/LAME installation. Windows media components must be available.

## Maps and reference data
Natural Earth basemap data is embedded in MRS.exe; it works offline. Public-domain source: https://www.naturalearthdata.com/about/terms-of-use/ . Maps/SOURCE.json records the pinned source and hashes. Optional online street tiles carry OpenStreetMap/provider attribution; see the map attribution action and https://www.openstreetmap.org/copyright . National bandplan CSVs document reference sources and do not imply local activity.

## Missing files
Re-extract the matching full ZIP if a bundled helper is missing. Do not mix DLLs from unrelated decoder releases. Use receiver/TV setup for separately installed components. Keep license notices and corresponding source with redistributed helpers. About summarizes the inventory; exact retained notices remain authoritative for each component. Configuration, aliases and recordings are user data, not required third-party decoder software.

## JS8Call bridge
The optional local JS8 bridge needs a separately installed JS8Call; 3.x is required for the one-message sending workflow and Halt API. Configure its TCP API and radio/audio independently. No JS8 executable or HackRF-to-JS8Call I/Q bridge is bundled. Sources, releases and license: https://github.com/JS8Call-improved/JS8Call-improved . Native direct-SDR JS8 remains planned.
