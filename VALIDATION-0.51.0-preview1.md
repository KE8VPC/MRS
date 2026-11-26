# Validation — 0.51.0-preview1

Local build of the saving/organization wizard. No live receiver, operational library write, API request or website deployment occurs in the tests.

## Results

- Final self-contained published suite: **2,164 PASS assertions**, including **42 organization assertions**; no FAIL lines.
- Six published UI/integration scenarios passed: organization wizard, website sync, independent P25/Commercial workspaces, discovery sharing/cleanup, LCN survey and six generated-I/Q voice/logging cases.
- The final executable retained all 196 channels and catalog metadata in the isolated copied-library check; the original backup was unchanged.
- Rules and responsive before/after preview screens were visually inspected, including dark-mode text contrast.
- Self-contained publish completed without compiler errors. Existing NU1900 warnings reported unavailable vulnerability metadata. CRLF-aware whitespace checks passed.

## Coverage

- Organization-specific model tests cover opt-in legacy compatibility, non-mutating preview, subtype/CC separation at identical frequencies, stable IDs and all non-layout fields, protected custom names/notes/folders, LTR labels, exact backups, future-policy persistence, repeated automatic saves, stronger identity relocation, manual placement, custom/missing/invalid fields, selected scopes, ambiguous evidence, stale local/disk revisions, tampered-plan rejection, no merges/deletions, shared-export privacy, guarded rollback after restart and Undo of both list and rules.
- WPF smoke uses a four-channel synthetic library to render rules and tree/move previews, explicitly apply and verify saved policy and metadata. Screens are visually inspected.
- Copied-library validation runs on an isolated temporary copy of the verified 196-channel backup. It compares IDs, tuning/evidence/notes and catalog, then verifies the original source is unchanged. It is not applied to the operational list.
- Full published suite and regression UI scenarios passed before packaging. Archive and extracted-copy checks are recorded in the adjacent PACKAGE-CHECKS file.

## Packaging and limits

Portable runtime/source archives are checked for CRC, staged/extracted byte identity, executable/source version, public bandplans, generated samples and native helper hashes. Operational frequency libraries, organization settings/rollback receipts, protected tokens, sync queues, private maps/routes and recordings are excluded.

NU1900 may indicate unavailable NuGet vulnerability metadata, not a successful dependency-security audit. Hosted sync and live radio acceptance remain separate. The active two-entry versus 196-entry library mismatch is left unresolved; the wizard displays path/count and uses preview/revision guards rather than restoring a guessed library. Rules are local and optional; old libraries retain legacy automatic saving until explicitly configured. No source commit/push or website/API change is part of this build.
