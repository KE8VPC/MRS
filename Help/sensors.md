# Sensors, remotes and utility meters
Context: Sensors
Keywords: rtl_433 315 433 902 928 ERT SCM SCMplus IDM ORION R900 device label JSON

Analysis > Sensors / meters / bursts contains Sensors / remotes, Utility meters and Burst inspection. Start SDR or I/Q replay first, then Start analysis. Use a known device/transmission for acceptance; a quiet band is inconclusive.

Supported rtl_433 decoders cover selected sensor/remotes around 315/433 MHz. Meter profile selects ERT SCM/SCMplus/IDM and supported ORION/R900 formats; not every meter uses these protocols. Broad meter capture needs enough usable bandwidth; disable the HF hardware filter. Raw counters are not automatically billing units.

Labels are local annotations keyed to source frequency/model/ID; raw IDs and JSON are retained. Digital gain is a decoder input adjustment and can clip. Stop/restart after changes; dropped counts indicate processing pressure.

## Counters and retained observations
Current run counters restart when Start analysis begins a new run. Table rows are retained until Clear observations and their totals are shown separately. Old rows with a current-run count of zero do not indicate newly decoded messages. Zero or one envelope transition is insufficient to identify a protocol; those rows are labeled envelope activity.
