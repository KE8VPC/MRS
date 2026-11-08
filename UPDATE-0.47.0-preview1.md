# M.R.S. 0.47.0-preview1

Saved discoveries, library cleanup and portable exchange for Windows x64.

Extract the entire ZIP into a new folder and run MRS.exe. Keep Help, Decoders, Receivers and bandplans beside it. Existing user files remain in local application data. This release does not automatically rewrite your saved list.

- Frequencies > Import / export: selected entries/folders or whole-library discovery bundles, JSON, channel CSV, catalog CSV and complete library backups. Systems/sites, channels, talkgroups, radio labels and map relationships remain independent and exportable for a future sharing website.
- Import existing frequencies.json without starting over. Review new/matching/conflicting records before applying. Fill missing values while preserving local labels/notes/identifiers. Catalog metadata is stored in the shared library and visible through View systems / talkgroups; it does not automatically become live monitor history or activate operating profiles.
- Frequencies > Clean up: preview generic-name improvements, known-site folder organization, compatible duplicate consolidation and optional selected-grid frequency correction. Select folders when channel plans differ. Unknown entries, custom text and conflicting duplicates stay intact. Empty folders remain for manual review.
- Exact integer-Hz storage and concise frequency display retain necessary fifth/sixth decimals. Optional correction keeps the original measured frequency. Nothing is rounded silently by default.
- Spectrum markers replace generic automatic labels with available network/site/color-code/RAN identifiers. Custom labels remain authoritative. NXDN observations retain exact frequency/site scope and existing custom annotations.
- Approved review batches create timestamped backups; Undo includes catalog metadata. Libraries containing catalog records use document version 2; older applications reject them to avoid data loss. Backups remain importable.

Start by exporting a Library backup. Then select your DMR folder > Clean up > Preview cleanup, with Keep exact frequencies initially. Review conflicts and custom labels before applying. See Help > Saved discoveries and Docs/DISCOVERY-EXCHANGE.md for the format.

Software/synthetic/package verification is documented in VALIDATION-0.47.0-preview1.md; follow ACCEPTANCE-0.47.0-preview1.md on your station. No live RF, GPS access, transmission, reception reporting or public upload was performed to produce this release. No existing operational frequency list was cleaned automatically.
