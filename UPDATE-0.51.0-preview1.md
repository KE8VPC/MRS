# M.R.S. 0.51.0-preview1

Saving and organization wizard for existing frequency lists and future automatic discoveries. No website/API contract changes.

Extract the entire Windows ZIP into a new folder and run MRS.exe. Keep the support folders. Back up your library and check its path/count before applying organization.

- Frequencies > Saving & organization: choose scope, layout, naming and future saving preferences, then preview the tree and before/after moves before applying.
- Separate generic DMR, Capacity Plus, Connect Plus and Tier III; group by color code when system identity is unknown and prefer known network/site where available. LTR saves display Area/Home identities without placeholder folder chains.
- Custom folder/name templates, selected-folder or entire-library reorganization, and optional empty generated-folder removal.
- Preserve all channel IDs and non-layout fields; no duplicate merge or channel deletion. User names/notes and identity-bound renamed system/site folders survive.
- Local rules persist across restart and guide new decoder/RF discovery saves; imported/manual placement remains under user control. Preferences are not sent through web sync or discovery exports.
- Full-library backup, guarded persistent rollback and session Undo. Stale previews and later edits block destructive replacement.
- Correct LTR saved labels and retain decoded Area/Home/Group/routing/free-channel fields without treating transient signaling as a confirmed carrier map.

The wizard does not enable auto-saving or start/tune a receiver. When satisfied with your preview, apply the organization, then enable the desired decoder/site auto-save checkboxes yourself.

The previously reported active-library mismatch was not repaired automatically. No operational saved list was replaced during this build. A temporary copy of the verified 196-channel backup was checked independently for identity/metadata preservation. See ACCEPTANCE-0.51.0-preview1.md and Help > Saving and organization.
