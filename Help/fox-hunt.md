# Fox Hunt — GPS signal-strength mapping
Context: Fox Hunt
Keywords: fox hunt GPS u-blox NMEA COM baud RSSI signal strength route map CSV GPX direction finding

Fox Hunt records where the receiver measured stronger or weaker reception. Open the main Fox Hunt tab. It reads a separate USB GPS dongle's NMEA position messages; it does not decode GPS signals with the SDR. It is receive-only and never transmits, uploads positions or changes the receiver's tune/gain automatically.

## Quick start

1. Start your SDR on the signal's frequency and choose a suitable mode/filter. Use fixed RF gain. RTL automatic RF gain is blocked for logging; select a manual gain value. HackRF LNA/VGA settings remain as you set them. Listening volume and audio AGC are separate from the channel-power measurement.
2. Select the GPS COM port and its configured baud, then Connect GPS. This release defaults to 57600 baud; 4800, 9600, 19200, 38400 and 115200 are also available. A configured u-blox 7 on COM6 at 57600 can use those exact selections. Other devices may differ. Close another program holding the same COM port before connecting. Port/baud choices are saved privately after a successful connection, but the GPS is not reconnected automatically on startup.
3. Wait for a valid position. The status shows bytes, checksum-valid RMC/GGA messages, fix age, satellites and HDOP when supplied. Unsupported binary-only output or the wrong baud does not create a position; the dongle must supply NMEA RMC/GGA. M.R.S. does not send configuration commands to the GPS.
4. Choose a new CSV file or use the unique default under Documents/MRS Recordings/Fox Hunt. Click Start Fox Hunt log. Default sampling is once per second; two/five-second intervals are available. Configure the session while parked; a passenger can monitor it during travel.
5. Stop / finish log flushes the session. Export GPX creates a route file with relative-power/frequency extensions. Existing files are never silently overwritten. Saved CSVs remain even if you clear the view.

## Reading the map

The map works offline using the embedded Natural Earth basemap. Use Load offline detail to add a local WGS84 GeoJSON road/place/boundary file. Optional regional files stay separate from the base application. The selected local file path is remembered privately for this receiver and restored after restarting. If the file is moved or unavailable, the base map remains usable and the map reports the missing detail. Remove detail clears both the active layer and its saved choice. Automatic restore accepts local absolute file references and rejects UNC network paths. Map sources shows user-supplied source, licence, version and size; missing metadata is explicitly shown as not supplied. No map data is fetched from the internet by this feature. The view reports when its center is outside the loaded detailed features. Wheel zoom, drag pan, Fit route and Follow position are available, with north and approximate distance scale. Blue is weaker, red stronger within the displayed session's relative scale; the legend shows the current range. The strongest measured point is your receiver's position at that observation, not a calculated transmitter location.

The most recent 5,000 map points and 300 table rows are displayed. CSV and GPX preserve the entire current session up to 50,000 points / 50 MB. A new session starts a new file. Gaps longer than ten seconds form separate route segments rather than a false continuous trail.

## Measurement behavior

Power is integrated channel power in relative dB, not calibrated dBm. Each observation averages available RF readings in linear power and retains a separate peak, sample count and squelch-open flag. Noise-floor/above-floor information is included only when the current receiver's matching, recent calibration is available; unknown values stay blank. Noise-only samples do not prove the target is transmitting. Optional Only while RF squelch is open samples open readings only and requires RF squelch enabled first.

Keep the same antenna, orientation, gain and filter for comparisons. Multipath, terrain, vehicle shielding, intermittent transmissions and changing transmitter output affect readings. GPS freshness and accuracy also limit the position assigned to a sample. The trail records measured receiver positions. The optional rough-area calculation described below is a model-based estimate; directional bearings, triangulation and TDOA remain separate future work.

Fox Hunt pauses point collection when GPS is missing/stale (more than three seconds), or no recent RF readings are available. Valid no-fix messages immediately clear the previous fix. Checksums, coordinate ranges and supported fix status are checked; simulated/dead-reckoned-only NMEA is not accepted as a live GPS fix. A receiver/frequency/gain/filter/capture/PPM change stops the session to avoid mixing incomparable readings. Start a new log after setting the new configuration. Stopping/restarting reception also requires a new session. Disk failures stop logging and retain already flushed rows.

CSV contains PC UTC, GPS receive time and GPS UTC when supplied, coordinates, altitude/speed/course/quality fields when known, position source, channel power/peak/count/noise evidence, route segment and receiver/gain/filter/capture metadata. GPS coordinates are received claims, not independent geolocation verification. The GPS timing does not synchronize independent RF sample clocks.

## Manual fallback and privacy

Manual stationary position is an explicit fallback for a parked receiver. Enter decimal latitude and longitude, Apply manual position, then enable that source. It stays at the entered position until you update it; it does not track vehicle movement. Its CSV/GPX source is labeled Manual stationary. Changing between GPS/manual sources requires a new session.

Logs and exports contain your location history. They remain local in the folder you choose and are not included in application releases. No online map, position reporting, APRS beacon or PSK Reporter location update is triggered by Fox Hunt. Live GPS serial reception and live SDR field measurements have separate acceptance boundaries.

## Signal trend

Stronger / Weaker / Steady compares medians of recent signal windows, requiring temporal coverage and comparable receiver settings within the same route segment. It supports one-, two- and five-second logging intervals. A gap makes the previous trend stale. Near-noise, unknown-noise and possible clipping conditions withhold guidance. Stronger does not establish that the receiver is closer to the source; multipath, shielding, orientation and transmitter power can also change reception.

## Rough source area

Calculate rough source area explicitly processes a snapshot of the current session in the background. Stopping the Fox Hunt log first keeps the snapshot stable; continued logging invalidates the previous area as new points arrive. Clear estimate removes the overlay. Status includes the number of points and the last measurement time. A stopped log is labeled as a historical session snapshot. While logging, an estimate whose measurements have paused for more than twelve seconds hides its area; calculate again after fresh measurements are available. A backwards clock change also withholds the previous area. This feature assumes one sufficiently stationary transmitter with stable output and comparable receiver/antenna settings. It cannot reliably detect every case of changing transmitter power or multiple emitters.

The calculation excludes manual/stale/poor or unknown-quality GPS, near-floor and unknown-floor RF observations, possible clipping and mixed RF settings. It groups points into 100 m areas so repeated stationary readings do not inflate coverage, then requires meaningful spatial spread, direction diversity and signal variation. A mostly straight road yields Not enough coverage.

The experimental local search fits unknown power level and several path-loss exponents using robust residuals, compares spatial subsets and checks alternative candidate areas and search-boundary ambiguity. Collecting / Not enough coverage / Ambiguous / Unavailable explains why a marker is withheld. A successful result displays a gold square and a dashed model-spread area. This is distinct from the receiver crosshair and colored measurements. The reported spread is not a calibrated confidence radius or promised accuracy. No source bearing, automatic tune, transmit, upload or routing action is triggered. Live known-source field acceptance remains required.

## Offline detail file format

Load offline detail accepts a GeoJSON FeatureCollection, using RFC 7946 WGS84 coordinates in longitude, latitude order. Supported features: Point places, LineString / MultiLineString roads or water lines, Polygon / MultiPolygon boundary outlines. Property name (or FULLNAME / NAME) supplies a plain-text label. Optional top-level mrs metadata contains name, source, license and version. Geometry collections, custom coordinate systems, imagery, MBTiles, building fills and turn-by-turn navigation are not implemented by this importer.

Files are bounded to 64 MB, 50,000 input features and 500,000 coordinates. Split larger datasets into regional files. Parsing occurs in the background; an invalid replacement retains the prior detail layer. Remove detail restores the base map. Detail outside the file's coverage remains unavailable. Source/accuracy and licence claims in imported metadata are supplied by the file's author, not independently verified by M.R.S.

Optional public U.S. Census TIGER/Line roads can be prepared as regional WGS84 GeoJSON. Reference: https://www.census.gov/geographies/mapping-files/time-series/geo/tiger-line-file.html . GeoJSON specification: https://datatracker.ietf.org/doc/html/rfc7946 .

## GPX context

GPX exports now retain the relative mean/peak/noise power, sample count, squelch state, receiver/device/gain token/filter/sample-rate/capture/PPM metadata, GPS receive time and available GPS time/quality/speed/course in M.R.S. extensions. The gain token includes backend-specific settings such as the HackRF amplifier state. Generic GPX viewers may ignore these extensions; the companion CSV remains useful. Exports contain private device and location information and are never included in public default datasets.
