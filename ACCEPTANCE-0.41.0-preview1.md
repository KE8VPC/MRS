# 0.41 preview1 live acceptance

All new live-RF items below remain pending. Synthetic/replay checks do not mark hardware reception accepted.

1. SAME / EAS: enable on a NOAA NFM weather channel or appropriate AM/FM broadcast source. Capture a real test header and verify event, area, sender and times. Confirm no alert relay/transmit action occurs.
2. DTMF: verify digits and silence boundaries from a known source; check false detections during ordinary speech. Keep tone candidates distinguished from validated packet protocols.
3. MDC1200 / FleetSync: verify unit IDs and command fields against known radios, including multi-frame/FleetSync II variants. Preserve unknown payloads and confirm retunes/seek do not combine bursts.
4. FT8 / FT4 / WSPR: use USB, the correct dial frequency/filter and synchronized PC UTC. Wait through a complete slot. Compare text/frequencies with a trusted receiver/decoder, including faint signals, unsupported/hashed messages, source loss and replay. WSPR remains no-CRC evidence.
5. Direct Meshtastic / MeshCore: match the local node's exact RF parameters. Confirm LoRa CRC, metadata, public text and raw export against your own known packets. Verify private/unknown payloads remain encrypted, wrong sync is unverified, and a retune/seek reacquires cleanly. Test HackRF and RTL paths separately; monitor overruns/CPU. No node or Internet connection should be needed.
6. Check all three new pages at narrow/wide window sizes and both themes. Clear/search/newest-first/export should work. Verify old paging, P25/DMR/NXDN, scanning, receiver assignment and recordings remain usable.

The 0.40 receiver assignment, PPM, FLEX and broad real-radio trunk-following acceptance checklist remains applicable. Prior user passes remain recorded; none are revoked by adding this batch. This release adds no claim of newly accepted live P25/DMR following, FT reception or mesh reception.
