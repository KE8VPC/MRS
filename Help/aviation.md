# Aviation reception and map
Context: Aviation
Keywords: ADS-B Mode S UAT ACARS VDL2 offline map trails 1090 978

Select a protocol and frequency, then start Aviation after SDR/replay. ADS-B/Mode S is at 1090 MHz; UAT at 978 MHz. Check usable capture coverage: a high sample rate does not override the enabled 1.75 MHz HF hardware filter. HackRF 9.6 MS/s and +250 kHz offset with that HF filter off is an established starting configuration.

Decoded packets may lack positions. Fresh decoded position messages are needed for map points; stale positions expire. Select an aircraft for its trail or enable all trails. The offline Natural Earth basemap works without network access. Online street map is optional. No MLAT or online aircraft feeding is provided.

## Local offline detail

Load offline detail accepts the same regional WGS84 GeoJSON roads, places and boundary outlines as Fox Hunt. Loading a local file turns Online street map off. The detail layer appears below aircraft and trails; existing aircraft freshness, trail gaps, follow, pan and zoom behavior remain in effect. Road/place names appear when sufficiently zoomed in.

Each receiver remembers its selected Aviation detail file privately, independently of Fox Hunt's map choice. The map restores that local file after restart while online tiles remain off by default. Moving/deleting a referenced file shows an unavailable-detail message; Natural Earth still works. Invalid replacement files retain the previous detail layer and preference. Remove detail clears the active layer and saved reference. No map file is copied into a release/default dataset.

Map attribution shows the loaded file's source, licence, version and size as supplied by its author; missing provenance is shown as not supplied. Detailed coverage is limited to the selected file, and the view reports when its center is outside those features. Local import is bounded to 64 MB, 50,000 features and 500,000 coordinates. Automatic restore rejects UNC network paths and malformed/oversized preferences. It does not enable online maps, start a decoder or change receiver tuning.

Regional road files remain optional, separate downloads/imports. This is a reference overlay; it does not provide navigation, guarantee map accuracy or verify aircraft position. See Fox Hunt Help for GeoJSON metadata/format details.
