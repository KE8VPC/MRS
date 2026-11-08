# Validation — 0.47.0-preview1

November 8, 2026. Self-contained Windows x64 saved-discovery management preview.

## Results

- Final published executable passed **1,965 assertions**, including **38 discovery-sharing/cleanup assertions**. Zero FAIL lines, successful exit.
- Targeted sharing tests verify exact-frequency/folder bundle, JSON and CSV round trips, independent catalog CSV, legacy frequencies.json import, default note/receiver-name redaction, full backup retention, preview without writes, atomic channel/catalog commit, restart persistence, idempotence, whole-batch Undo, measured-frequency preservation, informative labels, timestamped backup creation, stale in-memory and on-disk rejection, fill-only conflict handling, separate sites, compatible/conflicting duplicates, schema/cycle/ZIP-path rejection and selected-folder boundaries.
- Synthetic UI/integration checks passed for sharing/cleanup windows, bundle import, catalog persistence and refreshed markers, five-receiver pool lifecycle/coverage, three mapped DMR following protocols and Fox Hunt/shared offline maps. Rendered sharing and cleanup windows were inspected; button foreground is explicit for readability.
- Existing NXDN annotation regression exposed duplicate creation when converting an unscoped custom channel. Fixed exact-frequency adoption of a unique unscoped entry while retaining custom name, notes and signaling; ambiguous/other-site entries remain separate. The full suite passed afterward.
- Published with zero compilation errors and only the existing two NU1900 warnings (NuGet vulnerability-feed metadata unavailable). This is not a dependency security audit.

## Data and compatibility

No operational saved list was loaded for cleanup. All mutation/import tests use isolated synthetic libraries. Timestamped review backups and session Undo include the catalog. Document version 2 prevents older builds silently stripping imported catalog records; older version-one saved lists remain readable/importable. Share bundles include explicit IDs and scopes; website upload is not implemented. Imported metadata is visible in the shared catalog without being presented as live reception or automatically applied to a trunk profile.

No live SDR/GPS operation, RF transmission or public report/upload occurred. Existing P25/DMR/Fox Hunt field acceptance remains open. Follow ACCEPTANCE-0.47.0-preview1.md before treating the management workflows as accepted on an operational library.

## Package and output organization

Runtime/source archive CRCs, executable/source identity, current Help/docs, helper hashes, generated-sample allowlists and exclusion of operational settings/recordings/routes are verified in the adjacent MRS-0.47.0-preview1-PACKAGE-CHECKS.txt. Extracted-package launch is checked separately. Old versioned ZIPs/checksums/reports are moved into outputs/archive by version, preserving bytes and recording hashes/paths in a cleanup manifest. No files or user recordings are deleted; the source repository, optional maps and private analysis folders stay in place.
