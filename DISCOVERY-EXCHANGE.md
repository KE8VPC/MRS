# M.R.S. discovery exchange — version 1

The .mrsdiscovery extension identifies a ZIP with discoveries.json (authoritative), channels.csv and catalog.csv. No files are extracted during import. A plain JSON document is an equivalent exchange. ZIP import is limited to these three unique entries, 32 MB archive size and 16 MB uncompressed JSON; CSV is limited to 4 MB/10,000 rows. Up to 20,000 channels/folders and 20,000 catalog records are accepted; folder cycles, missing parents, excessive depth, duplicate IDs, unknown format/version and invalid records are rejected before changes.

## Authoritative JSON

Top-level fields: Format = "MRS discoveries", Version = 1, Channels (array), Records (array).

Channels use stable UUID Id and nullable UUID ParentId, IsFolder, Name, exact integer FrequencyHz, optional OriginalFrequencyHz, Mode, BandwidthHz, Modulation, Protocols, Signaling, Notes, Tags, Color, Visible, ScannerFolder, DetectionDate, ChannelLastSeen, ChannelRoles, SiteScope, ChannelIds, UplinkHz, EvidenceStatus, DmrIdentifiers and optional Activity. Activity carries FirstSeen, LastSeen, Hits, OccupiedHz, AboveFloorDb and Receiver (redacted for shared exports). Signaling and SiteScope preserve saved identity text/scope; ChannelIds and per-channel DMR identifiers remain independent of TG records. Unknown values remain blank/null rather than inferred identifiers.

Records use UUID Id, Kind, Scope, Identifier, Label, Protocol and Fields (string-to-string object). Kind is system, site, talkgroup, radio, bandplan or channel_map. Identity is the tuple Kind + Scope + Identifier; it is independent of the display label. Generated catalog UUIDs derive from this identity, while imported stable UUIDs are retained. Protocol-specific Scope values remain opaque and must not be collapsed by frequency or color code.

Observed talkgroups use their recorded system scope. Saved trunk-profile systems/maps/TG labels use Profile:<profile UUID>, preserving their relationship when on-air identity is unknown. Channel-map Fields include frequency_hz, confirmed, color_code and evidence when present. Live TG records include last_seen_utc and observations. Inferred system/site catalog rows describe saved channel scope, not proof of a freshly received control identity. Receiver-specific live catalogs are collected from the window used for export; imported shared records persist in frequencies.json.

## CSV

channels.csv: id,parent_id,is_folder,name,frequency_mhz,mode,protocol,site_scope,identifiers,notes,details_json. details_json is the complete channel object. Visible columns override their corresponding object fields on read; keep metadata columns for complete round trips. Simple channel CSV may omit id/parent_id/details_json; new UUIDs are then generated. Parent IDs must reference folder rows included in the import. Absolute integer-Hz storage avoids locale and display-rounding loss. Use invariant decimal points and quoted RFC-style CSV fields, including multiline notes.

catalog.csv: id,record_kind,scope,identifier,label,protocol,fields_json. Record-specific fields are carried by the JSON object; CSV imports are independent of channel imports.

## Merge contract

Imports are additive/conflict-preserving and require review. A known UUID identifying a different channel/scope is not overwritten. Same-frequency channels at different sites remain separate. Unknown/unscoped entries match only in the same destination folder with compatible protocol; multiple matches are skipped. Missing metadata may be filled, while conflicting nonblank fields remain local and are reported. Catalog identity matches may fill missing labels/fields but do not replace existing conflicting values. No imported catalog implies live RF acceptance. The shared-library document advances to version 2 only when catalog records exist; old version-one documents remain readable.

## Future sharing service

This format provides a versioned exchange foundation; no website, upload, account, authentication or public publishing feature is included in 0.47. A future service should validate this contract, keep contributor/source attribution and verification separate from decoded identity, enforce its own access/content limits, and escape all user text for display. Never treat labels, notes or uploaded JSON as instructions or executable markup. Clients should preserve stable IDs and request review of conflicting evidence instead of silently overwriting it. Shared exports omit notes by default and activity receiver names, but explicit labels and opted-in notes may still identify the contributor.
