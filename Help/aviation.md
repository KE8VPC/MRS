# Aviation reception and map
Context: Aviation
Keywords: ADS-B Mode S UAT ACARS VDL2 offline map trails 1090 978

Select a protocol and frequency, then start Aviation after SDR/replay. ADS-B/Mode S is at 1090 MHz; UAT at 978 MHz. Check usable capture coverage: a high sample rate does not override the enabled 1.75 MHz HF hardware filter. HackRF 9.6 MS/s and +250 kHz offset with that HF filter off is an established starting configuration.

Decoded packets may lack positions. Fresh decoded position messages are needed for map points; stale positions expire. Select an aircraft for its trail or enable all trails. The offline Natural Earth basemap works without network access. Online street map is optional. No MLAT or online aircraft feeding is provided.
