# Website sync — ke8vpc.net

Keywords: website API token sync upload download review private parent LCN provenance revision conflict cursor dry run

Open Frequencies > Website sync. This is manual synchronization of the token owner's records. It never starts an SDR, publishes a receiver identity or runs a background upload.

## Connect read-only first

Create a separate device token in the website's My account > API access. Start with read access. Enter the full token directly in the masked M.R.S. field; do not paste it into chat, logs or an export. The default API address is https://ke8vpc.net/account/api.php. Only HTTPS is accepted; redirects are blocked so your header token cannot be forwarded to another address.

Save connection protects the token for the current Windows user. It is stored in the local application-data website-sync folder, not in the portable ZIP or frequency library. Copying settings to another Windows account/device does not transfer an unlocked credential. Forget device token removes the local copy; revoke it on the website to invalidate it there.

Opening this window makes no network request. Pull changes and Prepare upload selection validate the saved connection themselves; a separate Test connection click is optional. The account and scope are rechecked before network operations. Pull changes retrieves all pages with echo=1, including own-token side effects. It writes the downloaded review queue and next cursor in the same atomic file before declaring a page complete. Nothing is applied to your frequency list during a pull. Queues, bindings and history are separated by API address and account. Prepare automatically performs the first pull if needed, so existing website records can be checked before upload planning.

## Review downloads

The searchable queue shows MHz, protocol, system/site, identifiers, channel/slot and local comparison state. Selecting a row shows local and website values side by side, with conflicting edits highlighted. The Advanced JSON tab retains the full record and upload results. Select rows in Downloaded review queue, then Preview selected changes. The preview lists additions, updates and held conflicts. Apply this reviewed plan creates a local library backup and applies only the accepted entries. Frequency matching uses exact frequency plus known site scope or an established local/server binding; names alone never relink records. Existing custom folder placement stays for normal updates.

Concurrent local/website edits are held. Resolve using website values explicitly previews replacement of conflicting local fields. Link / keep local values can bind an existing unique frequency or known system/site while retaining local values. Applying private notes requires the separate Apply my private notes checkbox. Downloaded raw records remain in the private local cache, including fields the client does not edit.

Remote deletions never automatically delete the local frequency library. Reviewed deleted frequencies are retained unresolved, and their binding prevents accidental recreation. Missing system/site/target links remain unresolved; a similarly named system is never substituted. Linked records are queued for context review when their cached parent/target changes. General type other records stay in the review/cache; Keep 'other' records on website acknowledges those without inventing a local radio record type.

Systems, sites, talkgroups, radio IDs and channel mappings are stored in the discovery catalog, visible through Frequencies > Import / export > View systems / talkgroups. This preview does not automatically configure or start trunk-following profiles from downloaded mappings. Canonical numbering, targets, provenance/verification fields and conflict information are retained in the catalog/cache.

## Preview an upload

Switch to a write-capable device token when ready. Use Pull changes to validate and refresh the new connection. Select a frequency folder or entries, then choose Prepare upload selection. Entire library is an explicit alternative. System/site/frequency dependencies are included; their local folder names are not sent. Select the records to send. Required missing parents/targets are added automatically from the prepared list. Larger selections are split in dependency order into requests of at most 200 records and a conservative byte limit; preview and confirmation state the total and batch counts.

Use **Update type** to choose frequencies and channel mappings, transmission observations, talkgroup names and radio IDs, or system and site details. Transmission observations include the carrier in MHz and the last direct reception time. An advertised channel can be shared as a frequency without claiming that the receiver heard a transmission on it. A frequency carries its link to the site when one is known. The upload includes any missing site and system records automatically. Talkgroup, radio ID, system and site updates can be sent without a frequency.

New API records stay private because M.R.S. omits visibility. Existing website visibility is retained, so review labels/metadata when updating an already public record. The server may fill your profile ZIP/grid when area is omitted; inspect these on the website after the first small upload. Radio IDs and notes are excluded unless explicitly selected. Credentials, receiver serials/settings, local folder paths/names, GPS routes and watch/ignore rules are not collected for upload. Attrs contains mode/filter data, not a serialized local settings object. Existing server identifiers/attributes are retained when preparing partial updates.

Run Preview selected on server (dry run). Dry preview cannot resolve a parent/target created only in the same previewed selection, even if an earlier dry request previews that dependency. M.R.S. checks that dependency locally and distinguishes that specific error from unrelated failures. Dry results never become saved server IDs. Unknown enums are blocked locally to prevent the server silently blanking them.

Upload previewed selection requires a fresh preview of that exact selection/revisions, followed by a confirmation naming the account, host, total count and batch count. The preview holds a copied selection; selection/account/revision changes require a fresh preview. Option changes explain that preparation is needed again. An optional connection test does not invalidate an unchanged preview. Start with one or two systems/sites and inspect the website before expanding. M.R.S. saves every successful id/revision before processing another result. A partial batch failure stops subsequent batches. It then pulls with echo=1 to capture side effects. Review those results before another upload.

## Conflicts, interruptions and limits

Force and revive are never sent. Stale revisions and gone records are retained for review; a gone record is not automatically recreated. Partial failures retain the successful bindings and the error/uncertain journal. After an interrupted request, pull first. Matching stable refs can recover accepted creations without retrying the write; an absent ref requires a fresh preview. A rate limit shows Retry-After and stops, rather than automatically retrying a possibly accepted upload.

Mapping-conflict updates are processed even at an unchanged revision. Duplicate flags are informational, not a complete matching service. Known duplicate candidates with a different ref are held until downloaded/applied or linked. Partially identified systems are not merged merely because their names or incomplete identifiers resemble one another.

Requests use the documented API v1 contract: 200 items per push, a conservative 1.9 MB body cap, 500 changes per pull page and manual rate pacing. Responses/queues are bounded; errors or invalid state retain existing files. Multiple app windows have a shared operation guard and file-revision checks. If another process changes sync state or the library, reconnect/reload and preview again. Library application and private sync bookkeeping are separate files; if bookkeeping fails after application, the persisted queue allows idempotent review/retry.

DMR sends exact variant text and ids.network; color code is not system identity. P25 mappings translate decimal M.R.S. display components into four-digit uppercase hexadecimal p25-chan. DMR carrier LCNs use lcn-1 with no invented timeslot. Passed survey mappings include the actual separate-call rule/count; imported-only data is not described as over-air decoded. Unsupported/ambiguous numbering is preserved for review, not guessed.
