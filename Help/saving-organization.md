# Saving and organization

Keywords: saving wizard organize reorganize folders DMR Capacity Plus CC LTR custom template backup rollback

Open Frequencies > Saving & organization. Check the library path and loaded channel count at the top. If they do not match the saved list you expect, close the wizard and resolve the correct library before applying anything.

## Choose rules

Choose Subtype and color code, Known system/site, Simple protocol folders, or Custom templates. The default separates generic DMR, Capacity Plus, Connect Plus, Tier III and unclassified Motorola rest signaling. Known network/site identities can take precedence. LTR defaults to direct entries with Area/Home labels. Equal color codes are browsing groups, not proof that frequencies belong to one system.

Choose Entire library or Selected entries/folders. Selecting a folder includes its descendants. Reorganize existing entries is separate from Use these rules for future decoder and RF discoveries. You can configure future saving without moving the existing list. Unchecking future rules restores legacy automatic placement; it does not stop decoding or turn any auto-save checkbox on/off.

Custom folders use slash-separated display labels, not filesystem paths. Tokens: {protocol}, {subtype}, {cc}, {network}, {site}, {area}, {home}, {channel}, {identity}, {frequency}. Values such as color code and site include their labels (CC 1, Site 22). Missing values omit that folder level. Example: {protocol}/{subtype}/{cc}. The generated-name template defaults to {protocol} · {identity}. Custom channel names are always retained. Invalid templates are rejected before applying.

## Preview and apply

Preview changes shows the proposed folder tree, before/after moves and labels, exact MHz and stable record IDs. Ambiguous identifier evidence is held. Options changed after preview require a fresh preview. Nothing is saved until Apply reviewed organization.

Apply changes placement and generated names only. It never changes exact or original frequency, channel IDs, scopes, evidence, notes, tags, activity, visibility or catalog relationships, and never merges/deletes channels. Empty generated folders left by moves can be removed; user-created or renamed custom folders are preserved. Existing identity-bound custom system/site names are reused.

A timestamped full-library backup and rollback receipt are stored beside the library. The commit uses the same lock and revision checks as other library operations. Another window's newer edits block an outdated plan.

## Future saves

Chosen rules are local preferences in this library. New decoder/site discoveries and RF activity discoveries use them after restart. Managed decoder entries can move as stronger system/site information arrives; their IDs and exact frequencies stay the same. Manually placed channels keep their custom folder. Manual additions and website imports are not automatically rearranged; use the wizard explicitly after importing if desired. The existing cleanup action also respects the configured organization rules.

Automatic saving must still be enabled with the existing decoder/site checkboxes. Organization does not enable saving, start receivers or send website requests. Preferences are not included in shared discovery bundles or API attributes. Older M.R.S. versions may discard these preferences when saving; use the new release to retain them.

## Rollback

The Rollback tab lists this library's organization backups. Review and roll back restores the original list and saving preferences and retains a backup of the current state. It works after restart only while the library is unchanged since that apply. Later edits/discoveries block rollback so they are not lost; use Import/export review to recover selected records instead. Undo in Frequency Manager can also undo an organization apply during the same loaded session.
