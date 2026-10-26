# Aircraft trails and offline basemap — 0.34.0-preview1

In Aviation, enable Show aircraft map. The default offline map is embedded in MRS.exe and does not require network access, an account or previously viewed tiles. Wheel or +/− zoom; drag pans; Fit aircraft fits fresh positions. Follow selected centers on the selected aircraft while it has a fresh position. Clicking a map dot selects its table row; selecting a row highlights its dot and trail.

Selected trail is enabled by default. All trails shows paths for every retained aircraft. Paths contain only validated received positions: no extrapolation. Each row retains up to 300 distinct positions from the last ten minutes. Reports separated by more than 30 seconds, replay rewinds and dateline crossings do not get a connecting line. Identical repeated positions do not consume extra points. Clear aircraft removes trails. History is held in memory during the session and included in an aircraft JSON snapshot; it is not automatically persisted across restarts.

Trail drawing uses one path per aircraft and a 30,000-segment display budget, prioritizing the selected aircraft and recent traffic. A visible status reports when that display limit is reached; stored trail data remains unchanged.

Timed map updates are limited to twice per second while the Aviation tab is visible; pan/zoom and selection remain immediate.

Live points/trails are cyan, replay orange, selected lime. Map dots disappear after 60 seconds without a position, while selected/all history can remain visible for the ten-minute trail window. Rows expire after 15 minutes. Live/replay, receiver/source identity, protocol and address classes remain separate, as in the existing aircraft table. Table and trail times use local processing UTC, with replay position used to detect recording gaps/rewinds.

## Offline map coverage

Made with Natural Earth. Worldwide country/land outlines and lakes, state/province boundaries for the countries included by the upstream 1:50m layer, and 7,342 populated places. Cities are revealed by zoom and label collision rules. Includes regional context around Ohio, such as Lima, Dayton and Columbus. Detail is regional: streets, county lines, detailed airport diagrams and airspace data are not included. Zooming farther in does not create additional detail.

The offline data is a pinned snapshot, updated through application releases. Maps/SOURCE.json records exact upstream commit, URLs, original SHA-256 hashes and transformation details; Maps/README.txt records coverage. Source distribution also includes the derived compressed dataset. The runtime uses the embedded copy, so removing the separate provenance documents does not remove offline coverage.

Source: [Natural Earth](https://www.naturalearthdata.com/) and its [public-domain terms](https://www.naturalearthdata.com/about/terms-of-use/). Four layers were retrieved from nvkelso/natural-earth-vector commit ca96624a56bd078437bca8184e78163e5039ad19 on October 24, 2026. Coordinates were rounded for compact storage; polygon outer rings and a separate lake layer are used. Original place rank/population determines label priority.

Online street map remains an explicit optional checkbox. Only that option downloads OpenStreetMap tiles; when tiles are unavailable the offline reference remains underneath. No bulk tile downloading, aircraft feeding or accounts are added. Online attribution remains © OpenStreetMap contributors. Runtime offline drawing contains no HTTP requests.
