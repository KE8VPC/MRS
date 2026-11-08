# Acceptance — 0.44.0-preview1

Prior acceptance carries forward as recorded in earlier files. In particular, the user's 0.43 PSK Reporter screenshot confirms ingestion and M.R.S./rig metadata display; it does not validate the new APRS or digital transmit paths.

## Live APRS reporting accepted — November 1, 2026

User reports APRS reporting works and supplies an aprs.fi screenshot showing KE8VPC-15, repeated updates, and the path APID98 via WIDE1-1,WIDE2-2,qAO,KE8VPC. This confirms the tested live RF-to-APRS-IS path and downstream aprs.fi ingestion/display through the configured receive-only iGate. Displayed position, altitude and voltage are present; precise location values are not copied into this acceptance record.

Acceptance: basic live APRS reporting / downstream map display PASS. Disable/restart, reconnect, duplicate suppression and NOGATE/RFONLY/replay exclusion retain local automated evidence but have not separately received user live sign-off. The APRS Tune-field issue is separate, fixed in source and pending the next authorized build. Released ZIPs remain unchanged.

## Low-band presets accepted — November 1, 2026

User confirms 80 m and 160 m. Mark the missing low-band preset restoration PASS. This confirms the preset item, not RF transmission, a completed contact, WSPR output power or unrelated HF reference packs.

## Digital TX trial — reception unconfirmed — November 1, 2026

User reports that transmission appeared to occur, but could not confirm TX operation because of limited HackRF output power; no reception reports were received. Record this as a user-observed apparent transmit attempt, not a confirmed RF decode, completed contact or TX acceptance pass. The user attributes the test limitation to low output power; the cause has not been independently measured. Mode-specific coverage was not specified, so do not infer separate FT8, FT4 or WSPR passes.

TX acceptance stays OPEN / UNCONFIRMED. This report alone establishes neither a software failure nor successful RF delivery. Actual output power, frequency/timing, signal quality, decoded reception and the earlier gain/start behavior remain unverified. No code, gain defaults, output limits, build or release package changed for this report.

## Remaining acceptance

1. Verify the tracking/rules checkbox labels and new dialogs on the user's display/scaling.
2. 80/160 m presets: PASS by user confirmation. IBP/HFGCS reference display/import still needs acceptance. Regional beacon catalogs remain planned. Numbers-station references are links, not a copied schedule dataset.
3. With suitable non-radiating equipment and a separate monitor, measure HackRF digital RF frequency, waveform purity and slot-start timing. RX PPM is not TX correction. Confirm Halt, missed-slot refusal, backend-error disarm and restoration of the same USB receive channel. A waveform decode is not RF verification. Recheck the previously reported gain/start instability; this build does not claim that hardware issue is resolved.
4. Verify an operator-supervised FT8 and FT4 standard exchange, parity, bounded retries and optional automatic progression with the selected peer. This preview deliberately permits an extra receive cycle for half-duplex decoder turnaround; test that pace. Verify no other station/replay advances the contact.
5. Measure WSPR output power, use the matching reported dBm, verify a complete beacon and same-device cooldown. No unattended repeating schedule is provided.
6. APRS live reporting and aprs.fi display: PASS as documented above. Remaining extended checks: disable/restart, reconnect, duplicate/repeated-path behavior and NOGATE/RFONLY/replay exclusion.
7. With a separately installed/configured JS8Call 3.x and suitable radio/audio connection, verify local received events, fresh state checks, one-message Normal-speed submission and Halt. The bridge has local mock API evidence only. Native direct-SDR JS8 reception/transmission remains future work.

No credentials, local station data, captured private recordings, live upload or RF test are included in the release validation. Clear failed/unknown status remains visible rather than being treated as a pass.
