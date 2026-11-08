# Saved discoveries: import, export and cleanup

Open Frequencies > Import / export. Use Entire library, or select entries/folders in the tree and choose Selected entries / folders. Selected exports include ancestors needed for the folder tree and only catalog records linked by system/site scope. Use Entire library to include profile-only and unassigned catalog records.

## Exchange and backup

- Export share bundle writes a .mrsdiscovery ZIP containing discoveries.json, channels.csv and catalog.csv. JSON export carries the same complete data.
- Export channels CSV supports spreadsheet editing of frequencies, names, modes, scope and identifiers. Keep ID, parent ID and details_json columns to retain less-visible metadata. Frequency units are explicit: frequency_mhz, with optional frequency_hz accepted on import. Blank parent IDs mean root entries.
- Export catalog CSV exchanges systems, sites, talkgroups, radio labels and channel-map records independently. fields_json retains record-specific identifiers/evidence; it is a JSON object, not executable content.
- Library backup exports the complete library plus the current receiver's available system/TG/map catalog, including notes, regardless of the selection. Credentials, receiver configuration, GPS tracks and recordings are not part of a library backup.
- Shared export excludes notes by default and removes the receiver name from saved activity metadata. Labels, identifiers, timestamps and any notes you opt into remain potentially personal; review them before public sharing.

Import accepts .mrsdiscovery/.zip, JSON discovery documents, older/current saved frequencies.json files, channels CSV and catalog CSV. It shows a preview before writing. New records are added; matching records can fill missing values. Conflicting nonblank values/custom labels stay unchanged and are listed as conflicts. Ambiguous matches/ID collisions are skipped. Exact frequency, site scope and protocol matter; color code alone never groups systems. Imports do not start a decoder, tune a receiver, enable a transmitter or automatically activate a trunk profile.

Imported systems/talkgroups/radio/map metadata is retained in the shared frequency-library catalog and is visible through View systems / talkgroups. Live monitoring tables retain their existing per-receiver histories; imported catalog records are not claimed as live observations or applied as operating profiles. Current live TG labels and saved trunk maps are included when exporting from that receiver. Back up/export from each receiver if you want every receiver's separate live catalog.

A timestamped .before-review-*.json snapshot is kept alongside frequencies.json before an approved import/cleanup. Undo restores the last session batch, including imported catalog records. Reload or closing clears session Undo; the timestamped file remains importable. Once catalog data is saved, the library uses document version 2 so older applications reject it rather than silently discarding catalog fields. Keep the backup before using an older release.

## Clean up

Open Frequencies > Clean up. Choose Entire library or Selected entries / folders. Options replace only recognized generic automatic names, organize entries with existing site scope, and merge exact compatible duplicates. Conflicting duplicates, custom names/notes, unresolved entries and empty folders are retained. The preview lists every proposed action and preserved conflict; Export review CSV saves that report.

Keep exact frequencies is the default. Only select a channel grid when appropriate to the chosen channels. Snapping uses an explicit zero-origin grid and maximum correction tolerance, which cannot exceed half a channel. It is a normalization aid, not evidence of the protocol or allocation. OriginalFrequencyHz retains the first pre-correction value. Select a specific folder when working with mixed channel plans. Four decimals in MHz resolve 100 Hz; 6.25 kHz channels may need five decimals. Display trims unnecessary zeros without losing exact integer-Hz storage.

Spectrum labels use existing identifiers for generic entries, for example DMR · Net 5 · Site 11 · CC 4. Custom labels take priority. Older generic saved names can be updated through the cleanup preview without monitoring them again.
