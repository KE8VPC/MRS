M.R.S. 0.22.0-preview1 validation — October 17, 2026 (America/New_York)

Source publish: Windows x64 self-contained, no compile warnings/errors.
Local self-tests: 1,027 passed, including 32 independent DMR vectors, noisy polarity/block-boundary checks, negative noise cases and frequency metadata preservation.
Aviation: 9 regression fixture cases passed, including high-rate signed input and 19.2 MS/s offset input; no drops in these cases.
UI smoke: complete existing suite plus stable DMR table assertions and aviation map rendering. Optional online map tile endpoint returned HTTP 200 (37,500 bytes) with the application User-Agent; actual interactive online map/hardware acceptance remains for the operator.

Supplied I/Q, processed offline with the production NFM discriminator and 16 kHz filter:
- dmr MRS-IQ-A-20261017-215451.mrsiq: 857 accepted slot/BPTC observations, repeated CC 0, both slots; zero verified full-link-control IDs.
- cntrl -MRS-IQ-A-20261017-215844.mrsiq: zero DMR detections. Protocol remains unidentified; this does not establish NXDN/NEXEDGE.
- ctrl 2 -MRS-IQ-A-20261017-220014.mrsiq: 1,254 accepted slot/BPTC observations; CC 0 dominates both slots. One isolated CC 6 observation is not corroborated. No verified full-link-control IDs.

Slot/BPTC checks do not validate every payload type end-to-end. Auto-save requires three matching observations. CC/slot alone cannot identify an agency, vendor or unique transmitter. Voice audio, embedded voice LC and trunk following are out of scope for this preview.

User-provided recordings remain outside all packages. Prior user-confirmed live ADS-B reception is preserved as historical acceptance, not a claim that the new DMR decoder has passed live hardware testing. Carried-forward paging/inspector checks accompany the new checklist.
