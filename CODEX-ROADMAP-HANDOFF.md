# M.R.S. pending source handoff

> **Superseded in part (November 21, 2026):** the integration described here has been promoted into outputs/MRS (commit 112d3d1). Do not re-run work/prepare-roadmap-integration.ps1. See `ACTIVE-BASE.md`.

November 19, 2026. The rollback checkpoint remains 336dc8d. Claude's 0.54.0-preview1 combined candidate lives in work/mrs-0540p1-combined and awaits station acceptance. The work described here follows that snapshot and needs to be brought forward when the approved UI candidate is promoted.

## Completed source areas

- Existing frequency names and folders are preserved when the protocol is blank or Unknown. This covers FM, mesh and manually filed aviation discoveries. Unfiled scanner discoveries can still use Unknown signal.
- Generated DMR rest/control/voice placeholders gain the known identifiers in their labels. Custom names stay intact.
- Website sync has an update-type selector for frequencies/mappings, direct observations, identifier/name changes, system/site details and all records. Observations include their carrier and direct-reception time. Missing parents are included. The download review shows update type too.
- LastReceivedOnFrequencyUtc retains direct reception independently of advertised channel updates. Existing P25 timestamps lacking this evidence are kept out of the direct-observation selection.
- VDL2 messages have readable AVLC/XID and supported CPDLC summaries, with source/destination fields. Raw payload and JSON remain available.
- Amateur SSTV supports Martin M1 and M2, automatic VIS, manual line sync, tone/slant settings, retained partial images and PNG plus JSON metadata. Sync loss remains explicit in the saved metadata.
- Amateur RTTY supports five-bit US ITA2 at 45.45, 50 and 75 baud, tone/shift selection, reverse, transcript display and text export.
- All smoke flags use explicit application shutdown, correcting the false-success pattern Claude identified.

## Integration

The integration copy is work/mrs-roadmap-integration, compiled into work/mrs-roadmap-integration-check. It uses the combined UI candidate and overlays the changed and new source files from outputs/MRS. Its version label is 0.54.0-preview1-roadmap1. It is a development check with no release ZIP.

The new source areas use partial MainWindow files. AdditionalReceiveControls.cs initializes SSTV/RTTY, routes demodulated or Windows/WAV audio, ticks the views, and stops their decoders at source boundaries. Existing images/transcripts remain visible after a boundary.

App.xaml.cs has the new diagnostic flags and shutdown policy. Preserve the combined UI's spectrum-layout smoke hook when applying this file. work/prepare-roadmap-integration.ps1 shows the exact integration procedure. SpectrumLayoutSmoke.cs in the development integration also includes SSTV and RTTY in the size matrix.

The new modes add Amateur sub-tabs and preserve top-level workspace ordering. The UI candidate's single-row main navigation and spectrum placement have their existing small-window limits.

Help additions are sstv.md, rtty.md and vdl2-messages.md. Signal Identification Wiki is listed in REFERENCE-RESEARCH.md as a general source across protocols.

## Website coordination

The existing API contract is sufficient for the implemented sync changes. An observed carrier is a frequency record with mhz, evidence, last_seen and its parent site link. Missing system/site dependencies travel with that record. A system or site can have several associated carrier records. The website can show those linked frequencies on its system/site pages using the existing relationship fields. Talkgroup and radio-name updates keep their metadata record types and need no frequency.

No API or website code was changed in this pass. Confirm the website display after the next explicitly confirmed test upload. Keep station-library details and credentials out of public handoffs.

## Verification and acceptance

See VALIDATION-PENDING-ROADMAP.md for local evidence. Independent SSTV recordings use pySSTV Martin encoders with public synthetic color bars, generated under work/sstv-reference. Source recordings, third-party encoder code and private receiver libraries stay outside release inputs.

Station checks still need live SSTV and RTTY audio, decoded VDL2 comparison, DMR autosave and the refined website-sync flow. Hardware RF, a real website upload, DPI and physical-monitor behavior require their own acceptance.
