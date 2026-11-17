# Acceptance — 0.51.0-preview1

- [ ] Extract the complete package. Open Frequencies > Saving & organization; verify the displayed library path and channel count match your intended list. Do not apply to an unexpected two-entry or otherwise mismatched library.
- [ ] Preview selected entries/folders and Entire library separately. Generic DMR and Capacity Plus should be separate, with CC groups below them. Known systems/sites should retain useful custom names.
- [ ] Inspect LTR Area/Home labels and missing/ambiguous-field handling. Color-code and Area/Home grouping must not assert a unique system or confirmed LCN mapping.
- [ ] Check before/after MHz, record IDs, names, folders and held rows. Cancel/close must change nothing.
- [ ] Apply a small reviewed scope. Check notes, exact/original frequencies, identifiers, channel mapping, activity, visibility and stable IDs remain. Empty user/custom folders must stay.
- [ ] Verify backup creation, same-session Undo and Rollback after restart. A later edit/discovery must block full rollback and retain that newer data.
- [ ] Save future rules without reorganizing existing entries; restart and enable the desired auto-save checkboxes. New DMR/Cap+/LTR/NXDN/P25 and RF discoveries should follow the policy where applicable. No tuning or saving is enabled just by opening/applying the wizard.
- [ ] Observe stronger network/site identity enriching a managed entry without creating a duplicate. Check a manually placed channel stays in its chosen folder.
- [ ] Try a custom folder/name template and missing fields. Change an option after preview; Apply must require a fresh preview.
- [ ] Check shared discovery exports and website sync retain radio identity/context without uploading local organization preferences.

Live acceptance is open. Local synthetic tests and copied-library checks do not establish live decoder/RF behavior. The website/API contract is unchanged.
