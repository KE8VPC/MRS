# Reference inspections — September 29, 2026

These are local read-only inspections and a limited official-web review, not demonstrations of every referenced feature. No reference application was launched, no radio was keyed, and no proprietary implementation was copied. Local configuration observations are snapshots, not portable device settings or proof of successful operation. Reference installations remain outside M.R.S. packages.

## SDRSharp

Inspected `C:\OLD\sharp 1732` and `C:\SDRSharp`. The newer main executable reports 1.0.0.1918 and has 36 plugin directories plus two saved profiles. Plugin presence does not establish loading or compatibility. Numeric sampling/gain/rate selections are not verified mappings and must not be copied as literal physical values.

Refine planned scanning with detection thresholds, hysteresis, dwell, temporary lockouts and activity logs. Track multiple software VFOs within one capture separately from multiple hardware receivers. Add rolling replay, recording-stage labels (audio/IF/baseband), notches, equalization, optional MPX output, receiver calibration and selectable de-emphasis. Evaluate network audio/control, satellite tracking and additional plugin capabilities individually. The older frequency XML has 20 memories in 11 groups; an import preview can map groups, frequency, mode, bandwidth, shift and favorites while retaining unsupported fields.

## DSD+ 1.101 folder

Inspected `C:\OLD\DSDPlus1p101`. The guide describes separate control/voice roles, priorities/holds/lockouts, channel mapping, per-call recordings and protected manual aliases. Preserve those distinctions in M.R.S. Imported observations must retain source and age. Found 102 group records but no populated channel-frequency map or networks/sites/radios files. The folder is an import-format reference, not a complete ready-to-follow system. Its guide lists P25 Phase I and describes D-Star/dPMR without audio; do not generalize its capabilities to later releases or full protocol coverage.

## PDW

Inspected `C:\OLD\pdw`: executable reports 3.1; bundled manual is September 2010; archive filename says 3.12. Add precise paging-workspace requirements: POCSAG 512/1200/2400 and separately validated FLEX rates/submodes, capcode/function filters, wildcard/keyword matching, labels, filtered/all-message views, duplicate audit, daily logs, hit/last-seen statistics, character maps and error indicators. POCSAG text interpretation can be ambiguous: support per-capcode overrides and both numeric/alphanumeric views. Decoder quality must not be presented as calibrated RF strength. Explicitly track ERMES and Mobitex research. Legacy slicer drivers are not a Windows 11 integration plan. Paging network time remains display-only unless explicitly selected in a separately validated timing feature. Alert files in the distribution are not assumed to be RF test captures.

## WSJT-X 2.6.1

Inspected `C:\WSJT\wsjtx` and its standard local profile. The profile stored FT8 at 7.074 MHz with virtual-cable input and no CAT rig. This does not identify the SDR branch or prove transmission. Preserve FT8/FT4, JT4/JT9/JT65, Q65, FST4, MSK144 and WSPR plans; add explicit FST4W and Echo research entries. Specify decode timing/SNR/frequency fields, ADIF exchange, worked-before views, named station profiles and independent receive audio, TX audio, CAT and PTT settings. Evaluate versioned UDP status/logging separately from control requests. Timed modes require continuous scheduling; retain the user's NIST, signal-consensus and PC-only choices, with no silent OS clock changes. Signal alignment is not absolute UTC. Review applicable licenses before any component reuse.

## VARA FM

Inspected `C:\VARA FM`: executable 4.03.0002, changelog 4.3.2, older 4.0 PDF guides. Saved KISS and command/scan ports establish configuration examples, not tested connections. Track optional version-checked external modem interoperability separately from native modem development. KISS carriage of AX.25 does not make VARA's RF waveform compatible with the current AFSK1200 decoder. Capture radio-level requirements for drive calibration, clipping, PTT delay, RX recovery, busy-channel detection, route tests and connection progress. Do not infer the numeric radio/PTT/FM mode mappings or distribute registration information. External transmission, digipeating, messaging and reporting remain explicit operator actions.

## RT Systems FT-8800

Inspected `C:\RT Systems V5\Yaesu\FT8800_V5` and extracted a working copy of its compiled help. No actual channel file was present. Prefer CSV preview/import as the initial interchange; export covers the displayed section, not necessarily the complete radio. Preserve memory types and many-to-many bank membership: the help describes 10 banks and six Hyper Memory operating configurations over shared memories. Add scan lists alongside folders, profile-specific scan behavior, and global versus channel-specific settings. The documented clone read/write workflow does not establish live CAT tuning. Some bundled pages refer to Version 4 and require verification before hardware integration.

## Unitrunker

Official 2.1 features were available through an indexed copy of https://www.unitrunker.com/download.html; direct site access failed with an expired TLS certificate. Do not claim a verified latest release or infer P25 Phase II voice support from a general P25 label. Refine receiver assignment, control-channel recovery, rank/tag listening, per-entity history, log rotation/compression and reversible edits/imports.

Read-only inspection of `%APPDATA%\Unitrunker` found an Ohio MARCS-IP XML snapshot with 218 sites, 1,085 channel records, 5,144 groups and 10,504 radio-ID records. The format version is 2.4, not proof of application version. Treat counts as historical/imported records, not independently observed current assignments. Cross-file identifiers require explicit format conversion and relationship validation; do not resolve sites by frequency alone. Preserve labels, timestamps, band plans, priorities and conflicts in an import preview. Credentials are excluded. The saved two-Realtek/four-VFO configuration is a useful organizational example, not a verified physical receiver test for M.R.S.

## fldigi saved working directory

Inspected `%USERPROFILE%\fldigi.files` read-only on September 29, 2026. `fldigi.prefs` records version 4.1.15; no executable or populated local help/rig-definition directory establishes an installed binary version or complete feature documentation. `fldigi_def.xml` contains 824 setting elements after removing XML-invalid control characters in memory for inspection only. The original was unchanged. Future import must diagnose malformed input and preview any repair instead of silently rewriting it. Stored settings and mode-state entries are feature clues, not proof of active use or RF interoperability. Personal contact logs and service credentials are not needed for this feature inventory.

Evidence includes 16 frequency entries, 27 palette files, a macro definition file, per-mode/per-band state, and a frequency-analysis CSV with UTC, frequency, frequency error, peak amplitude and dBV fields. Preserve RF dial frequency, rig sideband and audio carrier offset as separate frequency-memory fields. These saved frequencies are historical examples, not current allocation guidance.

Add or refine these planned capabilities:

- **Mode inventory:** explicit Contestia, DominoEX, Hell variants, MFSK, MT63, THOR, THROB/ThrobX, FSQ, IFKP, NAVTEX/SITOR-B and WEFAX 288/576 entries alongside CW, RTTY, Olivia and PSK families. Saved state also names multicarrier/robust PSK and OFDM variants; validate exact submodes separately. FT8/FT4/JS8 remain separately researched implementations, not inferred from this folder.
- **Identification and browsing:** receive/transmit RSID with notify-only versus operator-enabled mode/frequency switching; mode exclusions and lock retention; multi-signal text browser with frequency, history, callsign/regex highlights and bounded AFC. RSID identifies a cooperating transmission; it is not universal unknown-signal identification.
- **CW/RTTY detail:** CW speed tracking, Farnsworth, prosigns, keying envelope and separate key/PTT routing; RTTY shift, baud, character width, parity, stop bits, reversal, mark/space scope and AFSK versus hardware FSK. Numeric selection indexes require version-specific mappings. Hardware keying and PTT are separate from audio decoding.
- **Operating workspace:** receive transcript and transmit composition panels, editable macro banks, timestamps, QSO fields, duplicate checks, contest exchanges and ADIF workflows. Imported macros remain inert text until reviewed; transmit actions must honor explicit TX enable, timeout and PTT release. Local examples contain TX/RX control tokens.
- **Messaging ecosystem:** evaluate NBEMS/FLMSG extraction and FLAMP-related workflows, FSQ directed/heard/audit views, optional flrig/XML-RPC, ARQ and KISS interfaces. Configuration hooks do not prove those companion programs are installed or establish native protocol compatibility. External lookup, spotting and log submission are separate opt-in functions.
- **Audio and analysis:** independent input/output rates and sample-clock corrections, RX/TX channel routing, AFC/frequency lock and audio sweet spots, waveform/mark-space views, audio-history replay, WWV/analysis/FMT research and time-stamped CSV measurements. The CSV demonstrates a data format, not calibrated accuracy; frequency measurement does not establish UTC synchronization.
- **Image/weather decoding:** WEFAX image alignment, slant correction, auto-centering and export; NAVTEX/SYNOP structured views and optional map/KML records with provenance and retention.

These are roadmap refinements only. No fldigi code, personal profile, macros or credentials were bundled; no application was launched and no transmission or service connection was attempted. Keep per-mode RX/TX/interoperability acceptance separate from broad fldigi feature parity.

## Implementation order

Packet robustness and independent A/B workspaces first; then Unitrunker import with preview/rollback and P25 control-channel inspection; then validated voice following/decoding. POCSAG/FLEX, tone signaling, weak-signal amateur modes, external modem/radio interfaces and the remaining all-in-one scope stay separate, explicitly tracked milestones. Hardware acceptance and component licensing remain feature-specific gates, not blanket parity claims.



## rtl_433 and Mayhem receiver inventory — October 24, 2026

See [RTL433-MAYHEM-RESEARCH.md](RTL433-MAYHEM-RESEARCH.md) and research/receiver-decoders/SOURCES.json for pinned upstream revisions, inspected source paths, meter/drone distinctions, licensing and Windows integration limits. rtl_433 is the first requested sensor backend; Mayhem ERT, SubGhzD and FPV detection are adaptation references. No runtime integration or build is claimed by this research update.
