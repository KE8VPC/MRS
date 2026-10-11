# P25 Systems discovery catalog

Included in 0.16.0-preview1, packaged October 4, 2026. Earlier 0.15.0-preview3 ZIPs do not contain these additions.

The **P25 Systems** tab retains discoveries separately from the Data message history. It collects decoded band plans, current control-channel status, talkgroup/channel assignments, secondary control channels and adjacent-site announcements. It does not interpret undecoded TSBKs or NID headers as system discoveries.

Choose **Live discoveries · saved** or **Replay · this session only**. Live entries are stored per receiver in `p25-discoveries.json` alongside that receiver's settings under LocalAppData/MRS (ReceiverB for B). Changes save about every ten seconds and on window close. A successful save replaces a complete temporary file atomically. Replay entries remain separate and are not automatically written to the live catalog. Export can save either catalog explicitly.

Columns show WACN/system/NAC, receiving RFSS/site, discovery type, frequency, entity label, last seen and announcement count. Select a row for first/last seen, monitored input frequency, latest announcement text, validation and raw bytes. Search matches identity, label, frequency and decoded text. Export writes the entire selected live or replay catalog, even if search currently hides some rows.

## Identity and limits

- Fully identified live entries use received WACN, system, RFSS, site, NAC and monitored input frequency. Matching entries accumulate across application sessions. A and B have separate catalogs.
- Missing identity fields are shown as unknown/provisional. Such entries remain scoped to their reception epoch and are not automatically merged into later fully identified rows. This can leave a provisional entry alongside a later identified discovery; it avoids guessing system membership.
- Neighbor/secondary identities describe advertised targets; the receiving system/site context is kept separately. These announcements do not prove a site is reachable or a call is still active.
- A dual-grant message creates two channel discoveries but remains one received Data observation. Catalog counts are repeated announcements, not calls or radio counts.
- The displayed frequency reflects the latest observation; a new observation with no matching identifier table is unresolved. No inferred mapping from the receiver tuning frequency is used.
- Catalog rows remain after Data history rolls over or Data is cleared. Data visibility filters do not gate discovery collection.
- The catalog is bounded to 5,000 entries. At the limit, a visible message reports that new entries are skipped while existing entries continue updating. There is no automatic deletion of older discoveries.
- An unreadable/unsupported saved file is preserved and cannot be overwritten by autosave in that session. The status area reports the issue; incoming discoveries can still be viewed/exported in memory. Save failures are reported and retried.
- This first catalog does not import files, merge receivers, apply system-wide aliases, delete discoveries, promote provisional identities, follow calls, tune receivers or automatically modify the saved-frequency library. Explicit frequency review is described below.

## Acceptance

1. Enable P25 on a live control channel. Open P25 Systems and leave **Live discoveries · saved** selected.
2. Check that discovered types, identities and frequencies agree with the Data observations. Missing identifiers must remain visibly unknown.
3. Hide message types or clear Data history; the catalog should remain intact and continue collecting.
4. Close M.R.S. normally and reopen the same receiver workspace. Live discoveries and timestamps should return.
5. Play a generated P25 I/Q sample. Switch to **Replay · this session only** to see its discoveries; live entries should not gain fictional sample identities/counts.
6. Export each catalog if desired. Reopen the application: replay discoveries should be empty, while live discoveries remain.

Automated tests exercise serialization, reopen/update counts, identity/frequency separation, provisional epochs, replay isolation, out-of-order timestamps, corrupted/unsupported-file preservation, entry limits, and full replay-to-UI collection. Real RF and unexpected power-loss acceptance remain separate.

## Names, notes and frequency-library review — source update

Select a discovery and choose **Name / notes**. A name overrides the row's display label only; the original decoded label remains visible in details. Names and notes apply to that individual discovery entry, not automatically to every channel for a talkgroup or every site in a system. Incoming observations preserve them. Live annotations save with the catalog; replay annotations remain session-only unless explicitly exported. Clearing a name restores the decoded label. Search includes both names, original identifiers and notes.

For a resolved live channel, **Save to frequencies** opens the existing frequency editor with the selected downlink, P25 protocol, identity information, name, notes and source/time provenance. Choose a destination folder and review receive mode/filter width before Save. Cancel writes nothing. This action does not tune a receiver. Saving creates a new entry and uses the frequency manager's existing duplicate-frequency notice and undo behavior; it does not overwrite a prior saved station automatically.

Unresolved/nonpositive frequencies, band-plan base entries, and replay entries cannot use this live-station action. A neighbor or secondary-channel draft uses advertised target RFSS/site information and explicitly leaves the target NAC unknown; receiving-system identity remains in the notes. An announcement does not establish present activity or voice-decoder support.

Names are limited to 200 characters and notes to 8,000. Existing catalog files without those fields remain readable. Catalog export includes annotations.

Acceptance: name a live discovery, add notes, keep receiving and then restart; verify the annotation survives while counts update. Clear the name and confirm the decoded label returns. Open Save to frequencies, select the folder and review/cancel once; verify no entry was created. Then save and confirm the sidebar entry and RF marker contain the intended frequency/metadata. Test a neighboring-site entry to verify its target site differs from receiving context where appropriate.


