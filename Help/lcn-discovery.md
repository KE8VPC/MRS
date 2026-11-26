# DMR LCN discovery — one receiver per system

Keywords: single receiver LCN discovery scan carrier channel map rest control three calls resume incomplete tentative conflict

## Start

Start a live quadrature SDR. In Frequencies, right-click a single saved DMR site folder and choose **Discover LCN map (single receiver)**. Commercial > LCN discovery opens. The folder must contain one site scope; it is not enough that several unrelated channels share a color code.

Check Control/rest MHz. A saved decoded control/rest role is preferred; otherwise choose the known control frequency from this site's saved candidates. Manual selection remains user-assigned unless decoded role evidence is received. Calls to pass defaults to 3. Candidate frequencies lets you exclude entries without deleting them. Exclusions and progress persist.

Choose Discover or Follow and learn, then Start / resume discovery. Each receiver has its own site research and tuning state. Use Independent receivers; voice-pool receivers and receivers already following another system cannot be taken over. Two independent receivers can investigate two systems. The older Channel research tab still supports passive cross-receiver correlation.

## What an attempt means

The receiver waits for a validated assignment, remembers carrier/slot/TG and radio ID when supplied, and tests one saved candidate. Hardware tuning must be acknowledged, followed by a settling interval and a fresh raw-I/Q decoder context. It then requires valid group full link control matching the assigned slot, talkgroup, color code and radio ID when both sides supply it. An active frequency by itself proves nothing.

Repeated packets and repeated grants for the same unresolved conversation count once. Separate calls require a matching observed terminator, a complete Cap+ idle transition, or changed TG/source identity on the assigned slot. Wall-clock gaps, restarting, and learning a previously missing source ID do not create another call. This deliberately favors accuracy; an unchanged call identity may stay at 1/3 if no trustworthy ending was received.

Wait for call end defaults on, with a 20-second limit after matching identification; increase End limit s up to 60 seconds when useful. Probe seconds limits an unidentified candidate. Silence, a missed short call or failed decoding is inconclusive. A valid different call is retained as contradictory evidence. Contradiction-only candidates do not block the search, but contradiction on a matching candidate or competing matches holds that mapping for review and removes Passed status.

Three distinct matching calls pass a unique unconflicted mapping by default. Calls to pass is adjustable. Passed means it met the selected evidence rule; it is not independently guaranteed correct. Inspect the selected row's evidence. Stop discovery before rejecting a candidate; rejected evidence remains available for review/export.

## Control and rest recovery

After an identification window, terminator or timeout, return to control/rest and reacquire valid signaling. If it is lost, try the saved candidate frequencies in bounded turns. Ordinary voice is not accepted as control; known foreign network/site, subtype or color code is rejected. Capacity Plus rest is a changing last-observed role, not a permanent control allocation. An advertised rest number in voice LC does not prove that the tuned voice frequency is rest. An unmapped rest number cannot be converted into a frequency by guessing.

During a probe, control messages are missed. Missed assignments and unknown inventory are shown explicitly. Absolute-frequency/MBC messages and new vendor protocols are not added by this feature. No frequency outside the site's selected candidate list is searched.

## Saved organization

Update saved labels / status defaults on. Research updates existing exact-frequency entries in the selected site scope; it does not add frequency duplicates. Custom names, notes and positions remain. Candidate labels use LCN ?n, passed labels use LCN n, and decoded/manual control/rest roles remain distinguishable. If later evidence conflicts, survey-owned passed labels are downgraded. A different manually assigned definitive LCN is protected and requires review. Save map to frequencies explicitly applies the current state if automatic library updates are off. Each metadata batch is atomic and can be undone.

The site's folder shows **LCN map incomplete** when an observed carrier remains unresolved, or **All observed carriers mapped — total inventory unknown**. It never guesses the site's full inventory from a quiet listening session. Candidate frequencies with no assignment association remain in Candidate frequencies.

Use passed map for following creates a separate saved DMR profile from passed mappings, with network/site when known, and opens Trunk following. In Follow and learn, passed mappings may be followed during research; Try tentative explicitly permits a matching tentative call. Only matching voice is allowed into the listening path; conflicting identity returns to control. Audio depends on the bundled voice backend and still requires live acceptance.

## Resume and export

Progress, search order, call identities, evidence and settings are written to the receiver's local lcn-discovery directory, one hashed file per site. Opening the receiver again loads the previous site stopped; press Start / resume when ready. A source interruption stops activity and retains research. It never automatically opens hardware after restart. Stop / return to control returns to the last known control/rest if reception is still available. Manual tuning, source changes and closing cancel discovery.

Export evidence saves a full research JSON or a flat evidence CSV. Full local research may contain radio IDs and talkgroups; choose what you share. It is separate from the saved-frequency exchange, which contains applied mapping/status metadata. Malformed or oversized local research is left unchanged. A save or tune failure stops discovery rather than claiming success. Retain backups before manually editing files.

Current conservative boundary: probe color code must match the control assignment source. Sites using different color codes on different carriers can remain unresolved in this preview. This is not evidence that their channels are unrelated.
