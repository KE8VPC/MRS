# M.R.S. 0.50.0-preview1

Manual website synchronization with ke8vpc.net API v1.

Extract the complete Windows ZIP into a fresh folder and run MRS.exe. Keep all support folders. Back up the frequency library before testing imports.

- Frequencies > Website sync: masked device token, Windows user-protected credential storage, explicit account/scope check and read-only pull workflow. Opening the window makes no API request.
- Persistent account/server-specific review queue and cursor. Pulls always use echo=1. Same-revision mapping-conflict and linked-record context changes remain reviewable.
- Selected frequency/folder upload planning includes system/site/channel-map dependencies. Stable refs, explicit protocol identifiers, canonical P25 numbering and actual survey verification rule/count. Radio IDs and notes are opt-in; local folders, receiver settings and watch/ignore rules are excluded.
- Initial pull required, dry preview before upload, exact-selection/revision preview stamp and final account/host/count confirmation. New uploads remain private; existing website visibility is retained. No force, revive, background upload or automatic deletion propagation.
- Per-item durable upload results, partial failures, gone/revision-conflict handling and interrupted-creation recovery by stable ref after a pull. No automatic upload retry.
- Reviewed incoming frequencies update/link existing records or add deterministic new ones; local conflicts are held. Explicit keep-local / use-website resolution, local backups and unresolved retention of deleted/unlinked records. Systems, sites, talkgroups, radios and channel mappings enter the catalog without starting/configuring radios.

Source isolation: validated 0.49 checkpoint is on local baseline/0.49-before-website-sync (262c4a9); this work is on feature/website-sync-0.50. No source push, website deployment, real API access or operational library update was performed for this release. Local in-memory API and synthetic tests are not live acceptance. See ACCEPTANCE-0.50.0-preview1.md and Help > Website sync.
