# M.R.S. client fix development record — November 10, 2026

Release follow-up: these fixes are included in 0.50.0-preview2. The no-packaging status below describes the preceding fix-only pass; see VALIDATION-0.50.0-preview2.md for release validation.

Status: source fixes and local validation complete; not packaged, installed, committed or pushed in this pass. Existing 0.50.0-preview1 ZIPs are unchanged. Claude retains website/API ownership; no API contract change is required for these fixes.

## Changes

1. Saved connection initialization: Pull and Prepare initialize and validate the saved endpoint/token. Prepare performs the initial pull when required. Opening the window remains offline; Test is optional and does not invalidate an unchanged preview. Endpoint/token changes invalidate the session and plan. Read/write scope is rechecked before requests.
2. Download review: searchable MHz/protocol/label/system/site/identifier/channel/slot/comparison columns, local-versus-website field table, highlighted conflicting edits, advanced JSON and server results. Mapping frequency and parent identities resolve from cached record IDs. Missing links remain unresolved; no name-based relinking.
3. Upload preview/batching: missing selected dependencies are added from the prepared list, ordered system/site/frequency/mapping, split at 200 records and 1.8 MB planning size (1.9 MB request cap). Preview stores copied records and an account/revision fingerprint. Selection or option changes have explicit guidance. All dry requests precede confirmation. Real requests preserve per-item durable results, stop later batches on failure, and pull echoed records for review. Revision bindings are checked again after reauthentication before sending a batch. Force/revive remain unused.
4. P25 talkgroup history: readable complete/partial/unknown identity, NAC and observed MHz instead of raw fallback keys. New provisional observations can be promoted within the same source/session, exact observed frequency, NAC and compatible identity epoch. Retunes, resets and conflicting identities break association. Label conflicts retain both evidence groups for review; matching groups preserve counts/labels/first-last times. Old rows without supporting capture context remain separate. The sync mapper no longer mistakes a provisional fallback frequency for a site number.

## Local verification

- Release configuration compilation: passed, zero compiler errors. NU1900 warns that NuGet vulnerability metadata was unavailable; this is not a dependency-security audit.
- Full synthetic suite: **2,121 passing assertions** in `work/mrs-sync-fixes-check/self-test.txt` (before final comparison sizing, known-WACN/unknown-system display case and read-only confirmation guard; final targeted/UI checks cover the final source).
- Targeted website/client fixes: **72 passing assertions** in `work/mrs-sync-fixes-final/website-sync-test.txt`.
- Final WPF workflow: `work/mrs-sync-fixes-final/website-sync-ui.txt` passed. Saved-connection Prepare without Test, >200-record selection, automatic two-batch dry/fixture upload, exact-selection eligibility, changed-selection rejection, optional Test preserving preview, saved bindings and echoed queue. Read-only Pull/Prepare and no request on opening/reopening also passed. The fixture write helper runs after checking the production confirmation boundary; it does not dismiss a real user confirmation or contact the hosted API.
- Final P25/Commercial UI integration: `work/mrs-sync-fixes-final/update048-ui.txt` passed, including independent flows, histories, profile migration and synthetic voice-receiver dispatch.
- Final rendered review window inspected: `work/mrs-sync-fixes-final/ui-050-website-sync.png`; local/website columns widened after visual review.
- Scoped CRLF-aware whitespace check passed.

All HTTP traffic used an in-memory handler at mrs-fixture.invalid with synthetic credentials. No live website request/upload, receiver/GPS operation, operational library migration or RF transmission was performed. Diagnostic libraries and protected test tokens live only in isolated smoke profiles.

## Acceptance after packaging

- Reopen Web Sync with saved credentials and Pull without Test; repeat Prepare without Test.
- Review a real P25/DMR download and compare MHz, parent identity, verification and local/website fields before applying.
- Preview a large selection and verify total/batch counts. Start hosted writes with a small confirmed private selection; inspect website results before expanding.
- Confirm live same-context P25 identity promotion and retained conflicting/historical provisional rows. Do not automatically merge older ambiguous history.
