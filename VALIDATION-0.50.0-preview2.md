# Validation — 0.50.0-preview2

Release compilation, published tests and package checks are performed locally. API traffic uses an injected in-memory handler at mrs-fixture.invalid with synthetic credentials. No real website/token, operational library, RF receiver or public report is accessed.

- Final published full suite: **2,122 PASS assertions**, including **72 website/client-fix assertions**; no FAIL lines.
- Four published WPF/integration scenarios passed (website sync, P25/Commercial workspaces, six generated-I/Q voice/logging cases and discovery sharing). WPF checks: saved-connection Pull/Prepare without Test, no automatic request on opening, read-only gating, searchable/structured review, >200-record preview and fixture upload in two batches, changed-selection rejection, optional Test preserving an unchanged preview, durable bindings/echoed queue.
- P25/Commercial workspace and generated-I/Q voice/logging regression checks, plus extracted-package sync checks, are recorded in the package validation log.
- Runtime/source ZIP integrity, staged/extracted byte identity, source/executable identity, public-reference bandplans, generated sample allowlists and native helper hashes are checked. No operational settings, protected tokens, account queues, private routes/maps or user recordings are included.
- Compilation may report NU1900 because NuGet vulnerability metadata is unavailable. A successful compile does not establish dependency-security audit results.

The source baseline is preserved on baseline/0.49-before-website-sync (262c4a9); development is on feature/website-sync-0.50. This release request does not commit/push source or deploy the website/API.

Hosted authentication, actual dry/real uploads, website inspection and live P25 promotion remain open on the acceptance checklist. There is no automatic merging of unsupported historical P25 scopes, force/revive, background synchronization or automatic radio/profile activation from imported mappings. See VALIDATION-PENDING-WEBSITE-SYNC-FIXES.md for implementation details from the preceding fix pass; its earlier packaging status is superseded by this release.
