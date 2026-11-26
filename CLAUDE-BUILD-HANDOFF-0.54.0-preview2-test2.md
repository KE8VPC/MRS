# Claude build handoff — 0.54.0-preview2-test2

November 26, 2026. Read ACTIVE-BASE.md first. User requested the next portable testing update; no public GitHub publication, website/API change or signing setup performed.

## Artifact

- ZIP: outputs/MRS-0.54.0-preview2-test2-win-x64.zip (workspace-relative).
- Size: 99,550,094 bytes; 615 file entries.
- SHA256: 64FA12AE3F49E7FC9F036A7D11C6FA9B3B349AF846D03E0250E5375E9DABBEF8.
- Checksums: outputs/MRS-0.54.0-preview2-test2-SHA256SUMS.txt.
- Built from 4c160198af3f97e5e183667c37f6d2758bd14c4b; executable informational version 0.54.0-preview2-test2+4c160198af3f97e5e183667c37f6d2758bd14c4b.
- Publish/test evidence: work/mrs-054-preview2-test2-publish. Clean stage/extraction: work/MRS-0.54.0-preview2-test2-portable and work/MRS-0.54.0-preview2-test2-extracted.
- Separate private unsigned evaluation package. Test1 and public GitHub assets are unchanged. Different folders still share per-user settings/library; retain rollback and backup station data.

## Changes since test1

- 7bfdfd0 (note 23): removes ADS-B/UAT exact-frequency receive guards, changes fixed profile frequencies to defaults and preserves explicit setup target. Physical range/sample format/rate/capture-span constraints remain. Worker already accepts arbitrary RF target and offset; no Rust replacement. HOT/EOT stays on standard channels by user preference.
- af4d732 (note 24): mesh workspace Tune receiver uses its entered MHz and shared LiveTune; SF/BW/CR/sync untouched. All three mesh selections supported. Start reception/decoding remain separate. Shared status reports stopped/replay/range errors.
- f22b3aa (note 9): shared offline-map region manager is planning only.
- 4c16019: version/header and UPDATE-0.54.0-preview2-test2.md.

All test1 DSP/reconnect/target/tuning/Fox indicators/overload/prepared-TX changes carry forward. Commercial > Railroad HOT/EOT already included; EOT fields/BCH accepted, HOT detailed meanings unresolved, DPU only bandplan references.

## Checks performed

- Self-contained Release publish succeeded; 2,317 self-checks passed.
- Aviation worker replay: 17 standard/custom-frequency, signed/unsigned and offset/stress cases passed with frequency attribution.
- Sensor worker control fixtures: Acurite-609TXC, ERT-SCM, SCMplus and IDM passed. This is not proof of live sensor/keyfob compatibility.
- All staged decoder, receiver, public reference and generated sample files matched current publish by hash.
- ZIP CRC check passed; archived and extracted executable hashes match tested publish.
- Fresh extracted executable passed 54 additional receive checks including EOT BCH/changed-bit rejection and HOT frame interpretation separation.
- Full --ui-smoke and --radio-data-smoke passed. Mesh render inspected: correct test2 header and visible Tune receiver control. Synthetic UI only; hardware acceptance remains open.

No live receiver/GPS or physical transmitter was operated by these checks. No user recordings, routes, credentials, diagnostic screenshots or PDBs bundled. Package uses the previous clean curated support layout checked against this publish, updated executable/Help/release docs.

## Pending work and field priorities

Custom Aviation targets and mesh Tune need station acceptance. Continue sustained ADS-B/HDTV, reconnect, Fox quality/logging and FT8 timing tests. HOT/EOT live coverage is not claimed expanded. Sensor filtering/partial transmission inspector, selected-burst I/Q export, Silverado-specific decoding and saved offline regions are not implemented.

Latest private recording findings: work/mw1-analysis/REPORT.md, work/433-analysis/REPORT.md, work/315-analysis/REPORT.md. Silverado 2009 factory fob sample yielded OOK pulse packages after narrower offline filtering but no recognized protocol. Raw samples and guessed bits remain local. Do not interpret the private research as a delivered sensor fix or copy it into releases.