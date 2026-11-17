# M.R.S. 0.50.0-preview2

Website-sync usability and provisional P25 identity fixes. The ke8vpc.net API v1 contract is unchanged; the website changes reported by its builder were visual only.

Extract the complete Windows ZIP into a fresh folder and run MRS.exe. Keep the support folders. Back up your saved frequency library before applying imports.

- Pull changes and Prepare upload validate the saved connection themselves. A separate Test connection click is optional. Opening the window still makes no network request.
- Download review shows MHz, protocol, system/site, identifiers, channel/slot and local comparison state. Search the queue and compare local/website fields side by side; conflicting edits are highlighted. Advanced JSON and upload results remain available.
- Large upload selections automatically include missing prepared dependencies and split into dependency-ordered requests of at most 200 records. Preview and confirmation show total/batch counts. Exact-selection/account/revision checks replace the confusing dry-run loop; option changes explain when preparation is needed again.
- Per-item results remain durable. Batch failures stop subsequent batches; echoed pulls retain results for review. New records remain private by default; existing visibility is retained. Force/revive are never sent.
- P25 talkgroups show readable full or partial identities, NAC and observed MHz instead of raw fallback strings. Supported same-source/session observations can consolidate after identification while preserving labels/counts. Ambiguous legacy history and conflicting labels remain separate for review.
- Fixes provisional P25 fallback frequencies being misread as site identifiers during sync mapping.

Includes the manual sync features from 0.50.0-preview1: Windows-protected device tokens, persistent account-specific review queues, stable references and relationships, canonical P25/DMR mappings, conflict review and local library backups. No background sync or automatic radio/profile activation.

Validation uses local fixtures and synthetic RF only. Hosted sync and live P25 identity reconciliation still require acceptance testing. No real token, private saved list, route or recording is included. See ACCEPTANCE-0.50.0-preview2.md and Help > Website sync.
