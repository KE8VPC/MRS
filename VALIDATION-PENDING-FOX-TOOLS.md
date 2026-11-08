# Pending Fox Hunt source improvements

November 4, 2026. No release/version increment, ZIP packaging, installation, commit/push, GPS-port access, SDR access, live transmission or location reporting. Development compilation and synthetic diagnostics only.

## Implemented source

Primary Fox Hunt workspace; robust recent-window strength trend with stale/unknown-noise/near-noise/clipping withholding; local background rough-source-area calculation with cancellation, receiver/gain/GPS/usable-signal checks, spatial thinning, route geometry/variation checks, robust unknown-level/path-loss fitting, bounded search, alternative-area/subset ambiguity checks, and a separate gold model-spread overlay. New measurement/scope changes clear prior estimates. No confidence percentage or calibrated accuracy claim. Multiple emitters, changing transmitter power, antenna changes and terrain can defeat the model; this remains experimental.

Optional bounded WGS84 GeoJSON road/line/place/boundary import; plain-text names, source/licence/version/size and coverage status; public regional map files kept outside the base application; no network map services. The initial pass did not restore map choices after restart; the subsequent shared-map follow-up adds private local-file restoration for Fox Hunt and Aviation (see VALIDATION-PENDING-OFFLINE-MAPS.md). MBTiles, a complete nationwide road installation and navigation engine remain unimplemented. Extended GPX metadata retains RF gain/filter/capture/PPM/device, mean/peak/noise/squelch evidence and available GPS quality/time/speed/course.

## Evidence

- Development compilation passed with zero errors; existing NU1900 warnings concern unavailable NuGet vulnerability metadata, not compile failures or an audit pass.
- Complete local regression suite passed 1,847 assertions, including 32 new Fox tools assertions. Synthetic tests cover stronger/weaker/steady, one/five-second intervals, stale/gapped/noisy data, known-source model route, constant power-offset invariance, flat/noise/random-power/poor-GPS/manual/mixed-gain rejection, single-road and stationary-repeat withholding, cancellation, GPX metadata, valid local geometries and malformed/custom-CRS/empty map failures.
- Dedicated Fox tools UI smoke passed. Verifies primary navigation, background calculation/clear, visible estimate status/map, and local detail loading. ui-fox-tools.png was visually inspected. No physical devices were opened.
- Optional U.S. Census 2025 Auglaize County road file: 4,411 public road features, 2,427,114 bytes. Transformed from NAD83 to WGS84 using an available EPSG operation with nominal 4 m transformation accuracy; total source accuracy is not established. Source archive SHA256 and coordinate-operation metadata retained in GeoJSON. ui-fox-offline-roads.png was visually inspected.
- Scoped Git whitespace check passed.

## Remaining acceptance

Next explicitly requested release package must include these source changes and Help. Field checks must compare the trend and model-area behavior against real reception/known sources, inspect geometry/noise withholding, test offline map coverage and road-name usability, and verify extended GPX with external viewers. A synthetic successful fit is not real-world localization acceptance. The earlier private field tracks/known transmitter were not used as shipped fixtures, defaults or map content.
