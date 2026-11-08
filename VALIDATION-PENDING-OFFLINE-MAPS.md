# Pending shared offline map follow-up

November 4, 2026. Development/source validation only. No release version change, ZIP, installation, commit/push, live GPS/SDR access, transmission or location upload.

## Behavior

- Aviation now loads the same optional local WGS84 GeoJSON road/place/boundary detail as Fox Hunt. Aircraft/trails stay above the detail layer; pan/zoom/follow/freshness/trail limits remain in place.
- An explicit Aviation offline-map import disables Online street map. Private saved local file references restore independently for Aviation and Fox Hunt on each receiver; no map data is copied into fresh-install defaults. Restore never enables online maps or starts a receiver/decoder.
- Missing/invalid/corrupt preferences and missing source files report unavailable detail while retaining the embedded Natural Earth basemap. Failed map replacements retain the prior detail and preference. Removing detail persists an empty choice.
- Preference files are bounded; automatic restore rejects UNC paths. Latest selection wins over delayed imports; remove/close invalidates in-progress loading. A preference write failure keeps the usable layer and reports that saving failed.
- Source/licence/version/size/coverage information remains visible. Labels appear at appropriate zoom; original imported provenance is user-supplied.

## Checks

- Development compilation passed, zero errors. Existing NU1900 warning: NuGet vulnerability metadata unavailable; not an audit pass.
- Full local regression suite passed 1,863 assertions.
- Sixteen new map-choice checks passed: fresh empty defaults, explicit load/save, restore, invalid replacement retention, missing file without preference rewrite, remove/restart, corrupt/oversized preferences, rejected UNC restore, removal/load race, latest-load wins, dispose/load race, persistence failure and independent Fox/Aviation selections.
- Extended offline UI smoke passed. Aviation restored local detail in a new map instance, rendered optional public Census roads and a synthetic aircraft, removed detail, and recorded zero network tile requests. ui-aviation-local-detail.png was visually inspected.
- Fox Hunt's existing offline detail and source-area UI smoke also passed through the shared selection code.
- Scoped whitespace validation passed.

Optional road detail remains outside the base application/repository release data. The public Auglaize County file contains no user route, signal measurements or transmitter reference. Map accuracy, real aircraft reception, file restoration on the user's installed system and continued live decoder behavior remain field acceptance items for the next explicitly requested package.
