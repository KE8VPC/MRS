# 0.42 live/user acceptance

Local automation and packaging checks do not establish live reception or lawful RF transmission. Carry previous pending checks forward.

1. With live ACARS running, scan an ACARS-only folder with suitable dwell. Confirm decoding resumes on each frequency, the field follows tuning and message source frequencies are correct. Test Frequency Manager Tune/double-click, spectrum click and scan stop/resume. An explicit Aviation Stop must remain stopped after tuning.
2. Check Aviation Messages / Inspector for ACARS and ADS-B/Mode S, plus VDL2/UAT when available. Test filter, newest-first, clear, reset and export; preserve aircraft/map access.
3. Tune APRS and FT8/FT4/WSPR presets. Check receive mode/filter, region/dial labels and actual-frequency synchronization. Add/edit/delete a custom local/repeater/simplex preset and restart. Mode browsing must not tune; weak-signal retunes require a new complete slot.
4. Review discovery overlays at HF/VHF and ISM frequencies. Add selected reference entries to Frequency Manager, repeat import, and check labels/duplicates. Candidate ACARS entries should start unselected. Verify an existing edited working CSV is preserved. Reference coverage is partial.
5. Click the volume bar at several positions: volume should follow the pointer rather than jumping between minimum and maximum. Check wheel adjustment and monitoring on/off. Review the compact Receiver and Recording sidebar at normal/narrow widths. Setup selector and Apply must remain; useful status/errors must remain. Manage bandplans must have no corrupted glyph.
6. Without transmitting, confirm TX opens locked, missing declarations fail, unrelated services and band edges fail, and parameter edits disarm. Review the declared-versus-verified wording and unsupported jurisdiction limits. Any later RF test requires separate actual operator authority and suitable setup; it is not required for these denial checks.
7. Cross-check paging, APRS, DMR/LTR/NXDN, P25, sensors and weak-signal decoding across compatible retunes. Verify pause/no-source states and no incorrect channel attribution. HD Radio/HDTV retain restart requirements.

Pending prior acceptance includes live FT timing/long-run drift, new signaling/mesh modes and receiver/trunk-following workflows. Previously user-accepted features are not reopened solely by this release.
