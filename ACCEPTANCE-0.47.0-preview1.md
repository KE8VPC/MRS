# Acceptance — 0.47.0-preview1

- Extract the entire ZIP into a fresh folder. Export Library backup before reviewing an operational list.
- Compare generic marker labels with known saved identifiers; custom names must remain visible. Review long/overlapping labels and hover details.
- Select a folder, open Clean up, keep exact frequencies and preview. Check custom notes/labels, unresolved entries and conflicting duplicates remain. Apply only after reviewing. Confirm a timestamped backup and Undo; export/import that backup in a separate test library if needed.
- For a selected set with a known channel grid, preview snapping with a suitable tolerance. Confirm no unrelated channels change, and OriginalFrequencyHz is retained. Check a 6.25 kHz channel retains needed precision.
- Export share bundle/JSON, channels CSV and catalog CSV. Verify channel folders, exact frequencies, DMR/network/site/CC text, P25 identifiers, TG labels and map relationships. Review labels and any included notes before sharing.
- Import into an isolated/test library; check preview counts, conflicts, custom-label preservation and repeated-import behavior. Verify legacy frequencies.json imports. Malformed/unsupported files must fail before applying.
- View systems / talkgroups must show imported catalog data. Imports must not automatically start decoders, tune receivers, enable transmission or activate trunk profiles. Existing live histories remain separate.
- Test approved changes while multiple M.R.S. windows are open; an outdated preview must fail instead of overwriting newer edits. Verify state after restart, including version-two catalog retention.
- Continue the existing P25/DMR following and Fox Hunt station checks from ACCEPTANCE-0.46.0-preview1.md. This release does not close those live acceptance items.
