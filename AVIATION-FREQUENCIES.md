# Aviation known-frequency references — 0.42.0-preview1

These editable CSV layers are bundled with the spectrum bandplans and can be added selectively to Frequency Manager using Analysis > Bandplans > Add reference channels to frequencies. Existing matching receive frequencies/modes and user labels are preserved. Candidate rows start unselected; Ctrl/Shift selects rows in the preview. No local station history is shipped and no saved frequency library is replaced.

| Layer | Entries | Coverage |
| --- | ---: | --- |
| aviation-acars | 20 | Deduplicated user-requested VHF ACARS references/candidates |
| aviation-vdl2 | 1 | 136.975 MHz common signaling reference |
| aviation-hf-voice | 26 | New York NAT A/E and San Francisco Central East Pacific 1/2 networks |
| aviation-hf-volmet | 4 | New York 3485, 6604, 10051 and 13270 kHz |
| aviation-hf-hfdl | 46 | San Francisco, Riverhead, Molokai and Barrow system-table entries |

Each folder is under bandplans/reference/ and independently selectable. The VHF lists distinguish conventional ACARS and VDL2: ACARS messages can travel over VDL2, but the RF decoders differ. HF voice and VOLMET use USB; HFDL remains reference-only with no HFDL decoder. HF published dial/channel references must not be treated as AM carrier centers or automatically shifted for an unimplemented backend.

Nine ACARS entries have supporting monitoring references: 129.125, 130.025, 130.425, 130.450, 131.125, 131.550, 136.700, 136.750 and 136.800 MHz. Eleven additional submitted entries retain candidate status without asserting current North American provider, assignment or terminal/en-route role. Airframes lists 131.725/131.850 in Europe; that does not confirm North American use.

Sources are recorded per CSV row: Airframes acarsdec documentation, WAVECOM ACARS documentation, FAA AIP GEN 3.4, a VDL2 civil-aviation notice, and dumphfdl system table v52. User-supplied candidates are identified as such. Review date: November 1, 2026. These are starting references, not complete national/global coverage or a live active-channel list. Shared HF frequencies retain separate station/network rows in the reference; saved-frequency import deduplicates by frequency/mode.

Missing supplied discovery CSVs are seeded into the chosen working directory without overwriting existing files. Reload CSV applies manual edits. Existing installed ZIPs remain unchanged; this data is included in the 0.42 package. See Help/discovery.md for presets, import, scan/retune behavior and current decoder limits.
