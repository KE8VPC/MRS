# FINDINGS (analysis only): why DMR discoveries stop updating saved entries

**Author:** Claude, 2026-11-16 (corrected the same day after the user's review: §4 manual-entry upgrade; agreed starting point added). **Status:** findings and recommendations only. **No code was written and nothing was changed.**
**Code reviewed:** `outputs/MRS/src` at 0.53.0-preview1 (plus the uncommitted Weather changes).
**Data reviewed:** the user's live library `%LOCALAPPDATA%\MRS\frequencies.json`, **read-only**, with permission (290 items, 168 channels, Version 2).
**The user's layout review is still open.** Do not implement until the user says their notes are complete and authorizes the work.
Related: `CLAUDE-LAYOUT1-REVIEW-NOTES.md`, `CLAUDE-UI-HANDOFF.md`.

## User's complaint (their words, paraphrased)
- Organizing DMR discoveries is "a huge burden".
- An initial (often weak or incorrect) decode saves limited data, and **continued monitoring does not auto-update the entry** to help classify it.
- The user has deleted some entries to start over, so the library is partly cleaned by hand.

## Root cause (verified against the user's data)

### 1. Duplicate entries per carrier permanently block enrichment
`DiscoveryMatching.cs` `SaveSiteChannels` (line ~41) picks an entry to update only when there is **exactly one** match (line 48):
```
old = exact.Length==1 ? exact[0] : compatible.Length==1 ? compatible[0] : null
```
`compatible` (line 47) is every entry within 1 kHz with `CompatibleIdentity`. Provisional entries and unscoped manual entries are compatible with almost anything. Once a carrier has **two or more** entries, `old` is null and `nearby` is non-empty, so every new decode is **held** in Pending with "Possible duplicate or conflicting identity…" (line 51). It is never applied.

**Evidence in the user's library:**
- 50 DMR entries cover **36 carriers**. **7 carriers have 2–4 entries (21 entries in total).**
- `Pending` currently holds a good decode: 461.800 MHz · DMR Tier III · Network 5 · Site 1 · CC 0 · seen 2026-11-13. It is held because 461.800 already has 4 entries.

| Carrier (MHz) | Entries |
|---|---|
| 451.925 | 4: `Unassigned at 451.925000`, `Unassigned at 451.925296`, 2 × `Tier III / Network 5 / Site 1` |
| 461.800 | 4: `Unassigned at 461.800000`, `Unassigned at 461.799907`, 2 × `Network 5 / Site 1` |
| 452.300 | 4: manual "CC 6 - CAP+" (no scope), `Unassigned at 452.300439`, `Unassigned at 452.299844`, `Motorola rest signaling / Unassigned at 452.299799 / Site 4` |
| 462.300 | 3: manual "cc 0 net 5 site 1" (no scope), `Unassigned at 462.300000`, `Network 5 / Site 1` |
| 451.700 | 2: manual "CC 0", plus 1 provisional |
| 451.9875 | 2 provisional (`451.987761`, and `Motorola rest signaling … Site 4`) |
| 463.4875 | 2 provisional (`463.487000`, `463.487060`) |

### 2. The duplicates came in "provisional, then full" pairs seconds apart
`ChannelLastSeen` / notes timestamps on the same carrier show the pattern:
- 461.800: `03:53:57` "Unassigned" → `03:54:03` "Network 5 / Site 1" (2026-10-26), and again `00:23:18` → `00:23:23` (2026-11-02)
- 451.925: 2026-11-06 `01:43:02` → `01:43:15`
- 462.300: 2026-10-26 `04:02:00` → `04:02:46`

All of these are dated **26 Oct – 6 Nov**. That is most likely **before** the 0.47 "discovery sharing and reviewable frequency cleanup" matcher (git: `b8c60ee`), whose single-match upgrade should now handle the provisional→full case. **Not re-verified live:** please confirm with a synthetic test that a provisional entry followed by a full decode within seconds **upgrades in place**, including while a save is queued or deferred ("Frequency editors temporarily defer automatic writes").

Whatever created them, **legacy duplicates are still in users' libraries, and rule 1 means they never heal.**

### 3. Unidentified carriers are keyed by *measured* frequency
`SiteChannelSaving.cs` `FromDmr` (line 7) builds the network level as `Unassigned at {hz/1e6:F6}`, where `hz` is the **measured** center (`r.Context.FrequencyHz`). It becomes part of both the scope key and the folder name.

A slightly different measured center on another visit (451.925000 vs 451.925296) produces a **different scope and folder**. The user's library has **20 `Unassigned at …` folders, one per entry.** The newer organization rules can rename or move these, but the scope key itself still varies with the measurement.

### 4. Manual and RF-activity entries: upgraded only when uniquely matched *(corrected 2026-11-16 at the user's review)*
The matcher already supports this. **A uniquely matched manual or unscoped entry gains decoded `SiteScope` and signaling metadata, while keeping its folder.** On line 55, `richer` is true when `old.SiteScope` is empty. Around line 60, only the user's manual placement (the folder) is preserved.

In the user's library, 20 of the 50 DMR entries still have no `SiteScope`, for one of two reasons:
- they sit on carriers that also have other compatible entries, so they are **not uniquely matched** (rule 1), or
- they have not been observed by a site-saving path since they were created.

They also count as `compatible`, which **adds to the ambiguity in rule 1**. So the problem is ambiguity, not a missing upgrade path.

### 5. A single odd decode can't be outvoted
`CompatibleIdentity` (line 27) rejects a match on any regex mismatch of NET / SITE / CC / RAN / … between the stored text and the incoming decode. One weak decode with a wrong color code is held as a "conflict". There is no per-value tally, so twenty agreeing decodes do not outweigh one bad one, and the queue grows.

## Agreed starting point (user, 2026-11-16)
Start with these two, before anything larger:
1. **A reviewable cleanup for legacy duplicates.**
2. **An in-place upgrade for a uniquely matched provisional entry.**

In both:
- **Preserve the retained entry's ID, custom names and notes.**
- **Keep competing decoded identities available for review.** Never merge them silently and never discard them.

Items 3–5 below are later candidates.

## Recommendations (in order of payoff)
1. **One-time "Merge duplicates" cleanup** (preview → apply, reusing the existing timestamped backup, rollback receipt and revision lock):
   - For each carrier group within 1 kHz with compatible identity, keep the richest entry (decoded network/site > subtype > provisional > unscoped).
   - Fold in the others' `Signaling`, `ChannelIds`, `ChannelRoles` and latest `ChannelLastSeen`.
   - Keep any user-entered name and notes, and keep the stable ID of the kept entry.
   - Remove `Unassigned at …` folders left empty by the merge.
   - **Never merge** across different decoded network/site values; list those for review instead.
   - This alone re-enables automatic enrichment for the user.
2. **One entry per carrier going forward:**
   - Key unidentified carriers to the channel raster (6.25 / 12.5 kHz) or to the existing ≤1 kHz match, **not** the measured Hz. Keep the measured value in `LastObservedFrequencyHz`.
   - Put unidentified carriers in a single `DMR / Unidentified` folder (a color-code grouping can be a view or label, not a scope).
   - When network/site decodes, **upgrade the same entry and move it**.
   - If more than one compatible entry exists and exactly one of them is discovery-created with a scope, prefer that entry over unscoped manual entries instead of holding.
3. **Accumulate evidence for weak decodes:**
   - Keep per-entry counts of observed CC / network / site / subtype values, plus CRC-valid vs noisy counts.
   - Classify by the value with repeated agreeing valid observations (default 3, matching the existing "Calls to pass" philosophy).
   - Record a minority value as an outlier, not a conflict. Hold only when two values each have strong support (the existing "preserve conflicts" rule).
   - Show the confidence on the entry.
4. *(Mostly covered by the existing matcher once ambiguity is gone, see §4.)* Optionally, a right-click **"Let discoveries update this entry"** could bind a manual entry to a decoded site when several candidates exist, keeping its name, notes and folder.
5. **Then simplify organization** with:
   - a single **Discoveries inbox**, holding new carriers from all four save paths (direct DMR auto-save, site frequency saving, RF activity, LCN discovery)
   - **grouping by likely system** (confirmed: decoded network/site; strong: Cap+ rest set or shared LCN map; suggested: same CC plus time co-occurrence plus TG/radio-ID overlap)
   - **group-level actions:** New system / Add to existing / Ignore (remembered) / Later
   - **system cards** (type, CC, network/site, control/rest, LCN-map progress, talkgroups, last heard), with folders as **views** (by system / by band / by date)

## Tests worth adding
- Provisional then full decode within seconds (and while a save is queued): exactly 1 entry, upgraded scope and folder.
- Same unidentified carrier observed at ±300 Hz on three visits: 1 entry, 1 folder.
- Library with a legacy duplicate pair: after the merge preview/apply, the next decode enriches it with no Pending item.
- 20 agreeing decodes plus 1 wrong-CC decode: classification unchanged, outlier recorded, nothing held.
- A uniquely matched manual entry gains scope and signaling, keeps its folder, ID, name and notes (regression guard for existing behaviour).

## Privacy
These findings mention only the user's frequencies and decoded identifiers, for this local note. Do not copy library contents into public packages, shared bundles or the website.
