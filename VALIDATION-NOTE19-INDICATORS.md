# Note 19 — Fox Hunt glance indicators

November 22, 2026. Adds three high-contrast cards above the map: signal trend, GPS/position and actual logging state. Shapes and words accompany colours; automation names and tooltips explain each state. Saved size choices are Compact (18), Large (22) and Extra large (28) in fox-indicators.json under the existing local receiver settings folder. Invalid preference data falls back and is logged. No private samples/settings are included in release defaults.

GPS uses a typed connection state, not parsing diagnostic text. Off, opening, connected/no-fix, fresh fix, stale fix and error are distinguished. Manual positions remain explicit. Existing detailed GPS counters are under an expander. The log indicator separately checks active session, fresh position, live RF context and recent saved point; valid NMEA or a fresh fix alone cannot show logging green. A waiting/squelched log turns amber rather than suggesting samples were saved. Updates use the existing one-second Fox UI tick.

Trend uses the existing smoothed/quality-gated FoxStrengthTrend. Stopped sessions show paused immediately on the next UI tick, regardless of retained recent measurements. Stronger/weaker threshold equality is consistent at +/-2 dB. Detailed explanations remain expandable. This is not a transmitter-direction arrow, nor a calibrated location/confidence indicator.

New tests cover all major GPS/logging/trend distinctions, recent/stale points, manual position, retained data after Stop and threshold boundaries. Fox tools smoke renders independent synthetic strength/GPS/logging states and checks they remain separate. Screenshot: work/fox-indicators-check/ui-fox-tools.png. No live GPS/receiver port opened; coordinates in the smoke are synthetic. Render inspected at the diagnostic window size; broader DPI/minimum-size matrix and physical display acceptance remain open.

Not included: auto GPS connect/reconnect, suspend/resume recovery, run import/resume, custom colours, audio cues, preliminary localization, heat maps or new map layout. The existing map-space constraints are not declared solved. Original public roadmap1 assets remain unchanged.

Acceptance: verify visibility and size retention, GPS disconnection/no-fix/stale states, actual recording versus armed/waiting, and stopped trend on the station setup. Do not infer field acceptance from synthetic labels.

Final validation: 2291 self-checks passed, including 17 new indicator cases; Fox tools UI smoke passed and its synthetic screenshot was inspected. Release-mode self-contained development compilation succeeded. No hardware acceptance or release publication claimed.
