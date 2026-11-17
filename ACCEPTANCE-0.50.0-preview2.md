# Station and website acceptance — 0.50.0-preview2

Back up the saved frequency library. Extract the complete ZIP to a new folder. Local tests use only an in-memory API handler and synthetic data; the live website has not been contacted by this build.

- [ ] Create a dedicated read-only device token on the website; enter it directly in Frequencies > Website sync, save and Pull changes without Test connection. Confirm account and read scope.
- [ ] Pull changes. Close/reopen the sync window and verify that the review queue persists and the cursor resumes. No frequency-list changes should occur before applying a preview.
- [ ] Preview a small system/site/frequency set. Check identifiers, exact frequencies, existing custom names/notes, folder placement and unresolved relationships. Apply only the reviewed subset; inspect the local backup and Undo behavior.
- [ ] Check a renamed system and a changed site/target relationship. Stable identity must survive renaming; missing links must stay unresolved. No name-based reattachment.
- [ ] Configure a write token for the same account and Pull again without a separate Test step. Prepare one or two systems/sites and run a dry preview. Check its exact field list and expected new-parent dependency errors. There should be no live write during dry run.
- [ ] Explicitly confirm the first small real upload. Inspect the records on the website before sending more. New records must be private; no local folder/receiver/watch settings should be present.
- [ ] Confirm returned IDs/revisions survive restart and echoed own changes/side effects appear for review. Check later edits update the same records, not new duplicates.
- [ ] Cause a controlled stale-revision conflict and review it. Keep-local and website-values options must retain both versions until selected. A deleted ref must remain gone; M.R.S. must not force/revive it.
- [ ] Verify P25 channel canonical values, DMR variant/network identity and LCN verification rule/count against a known small dataset. Review conflicting mappings even if their revision did not change.
- [ ] Check optional private notes and radio IDs separately. Unknown general records should remain in website/cache rather than become invented frequency entries.
- [ ] Check reconnect/interrupted-request recovery and rate-limit messaging with an approved test account. Avoid repeated real uploads while a result is uncertain; pull and review first.
- [ ] Reopen the window and Prepare upload without Test. Confirm the first pull happens automatically if needed, and opening the window itself sends no request.
- [ ] Inspect MHz, parent identity, identifiers, verification and local/website field differences. Search for a known channel and check deletion/unresolved labels.
- [ ] Dry-preview a selection larger than 200 records. Check total/batch counts and required dependencies. Change selection/options and verify a clear fresh-preview/preparation message. Inspect a small hosted upload before expanding.
- [ ] Observe P25 partial identity becoming known during one capture context. Counts/names must survive promotion. Historical ambiguous rows and conflicting labels must remain separate; retunes/resets must break unsafe association.
- [ ] Recheck existing SDR/DMR/P25/LCN behavior as needed. The sync client does not automatically enable radio decoding or following.

Live acceptance remains open: account authentication, deployed schema/API response shapes, actual hosted pushes/pulls, website inspection and error paths must be confirmed with user-configured credentials. This build contains no real token and does not claim live deployment or service acceptance.
