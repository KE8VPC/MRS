# LTR feasibility research

Research date: 2026-10-18. Research only; no decoder or build changes.

## Findings

Logic Trunked Radio Standard combines analog FM voice with subaudible trunking signaling. Home repeaters distribute routing information rather than relying on a dedicated continuous control channel. Messages identify area, home repeater, group, destination channel and available repeater. The transmitted channel numbers require a separately established frequency table; they are not frequencies. A home/group combination identifies a fleet, not necessarily an individual radio or business name. [1]

sdrtrunk documents separate LTR Standard, LTR-Net and Passport signaling decoders and supports HackRF and RTL tuners. This demonstrates practical SDR feasibility, not M.R.S. compatibility or live acceptance. [2] Its decoder improvement history identifies short idle bursts and symbol timing as important engineering concerns. [3]

Kenwood's NXDN Type-D description uses the term digital LTR. Treat this as a separate digital protocol, not an analog LTR Standard decoder option. [4] Defer LTR-Net, Passport and MultiNet until their respective message formats and validation captures are available.

## M.R.S. integration proposal

The current roadmap already lists LTR evaluation. Inspection of src/FmDemodulator.cs confirms DataSamples are collected from the FM discriminator before the listening-audio high-pass stage. That provides a plausible input for subaudible decoding; actual filtering, timing recovery and signal scaling still need validation.

1. Start with LTR Standard repeater-output identification on one selected channel, while retaining FM listening audio. Preserve raw words, checksum outcomes, timestamps, receiver and frequency.
2. Display stable grouped observations for area/home/group, channel references, first/last seen and count. Keep invalid candidates separate from confirmed detections.
3. Allow aliases and optional scan annotations only after repeated valid observations. Scope identifiers by the user's system/site grouping; preserve original names and frequency observations. Do not infer an organization or unique system identity from an area bit.
4. Build an editable logical-channel-to-frequency table. Record confidence and evidence for learned associations.
5. Add conversation following later, using the verified table, receiver coverage, hold/ignore rules and reacquisition. Identification alone does not provide full trunk tracking.

Single-channel decoding should not need maximum HackRF bandwidth. Monitoring a whole system depends on the frequency span and available receiver resources. No local active LTR system has been established by this research.

## Validation before release

- Obtain known LTR Standard I/Q with both idle signaling and active voice.
- Verify exact framing, checksum, reserved fields and signaling rate against decoder source and captures before implementation. The operator article is useful for architecture but is not being treated as a bit-exact protocol specification.
- Test short bursts, stream boundaries, frequency/DC offset, weak signals, corrupted words and ordinary analog voice/CTCSS/DCS negative controls.
- Compare independent decoder results; inspect licensing before incorporating third-party code.
- Separate synthetic/replay checks from live RF and operator acceptance.

## Sources

1. MRA, Mark Abrams, LTR Trunking: https://mra-raycom.com/articles/ltr-trunking/
2. sdrtrunk project documentation: https://github.com/DSheirer/sdrtrunk/wiki
3. sdrtrunk LTR decoder enhancements: https://github.com/DSheirer/sdrtrunk/issues/1565
4. Kenwood system types: https://www.kenwood.com/usa/com/system/
