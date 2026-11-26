# Validation — 0.50.0-preview1

Manual website sync preview for Windows x64. Tests use an injected, in-memory HTTPS-shaped API handler; no real website, token, public reception report or operational radio data is accessed.

## Results

- Published full suite: **2,100 PASS assertions**, no FAIL lines, including **50 website-sync assertions**.
- Website tests cover HTTPS/header credentials; token format and Windows user-bound encryption; no automatic request on construction; explicit owner/scope checks; first-pull gating; echoed pagination; persistent review/cursor state; same-revision mapping flags; malformed-page and failed-write cursor preservation; stale-writer protection; account partitioning; native-to-API graph planning; exact P25 canonical channel conversion; DMR network/variant identity independent of color code; stable refs across renaming; private/no-notes/no-receiver defaults; real survey verification rule/count; downloaded provenance/confidence preservation; imported site/mapping labels and verification; exact enum validation; token-shaped content rejection; dry dependency limitations; durable per-item results; local/remote label conflict review; keep-local/website-value resolution; unresolved deletion retention; read-only upload rejection; credential echo scrubbing; Retry-After and redirect handling; conflict/gone no-retry paths; uncertain creation recovery; opaque server IDs/attrs preservation; linked-record invalidation without name-based reattachment.
- Website window integration renders the real WPF controls with a synthetic library and API handler. Connection is explicit, read-only upload controls are disabled, pulls persist the queue, local selection planning does not send a push, and record details remain readable. The rendered screen was visually inspected.
- Seven integration scenarios pass: website sync UI, single-receiver LCN survey, separate P25/Commercial flows, mapped DMR following, five-receiver fleet, generated-I/Q P25 voice/logging, and discovery exchange/sharing.
- Self-contained publish completed with zero compilation errors. Two existing NU1900 warnings report unavailable NuGet vulnerability metadata; compilation success is not a dependency-security audit. Tracked CRLF-aware whitespace checks pass.

## Packaging and isolation

The complete portable runtime and source ZIPs retain support folders, public reference bandplans, generated sample allowlists and current Help/docs. They are checked for ZIP integrity, extracted byte identity, source/executable identity and absence of operational settings, tokens, website-sync/account queues, private routes/maps and user recordings. Final extracted tests are recorded in the adjacent PACKAGE-CHECKS file.

0.49 was preserved in local commit 262c4a9 on baseline/0.49-before-website-sync; sync implementation is isolated on feature/website-sync-0.50. No branch was pushed and no website files, live database or operational saved library were changed.

## Acceptance limits

The supplied API contract is implemented and tested with local fixtures. Live hosted response shapes, authentication, deployed schema behavior, real owner-private record access, actual dry/real uploads and website inspection still need the acceptance checklist with user-configured device tokens. M.R.S. contains no real credential and performs no startup/background API request.

Imports preserve catalog relationships/verification and reviewed frequency fields; they do not configure/start radios or trunk profiles. General other records stay in the website/cache. Existing website visibility is retained on updates, and the server may fill profile area defaults. No force, revive or automatic deletion propagation is implemented. Existing live RF/DMR/P25 acceptance remains separate.
