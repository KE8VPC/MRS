# M.R.S. 0.49.0-preview1

Single-receiver DMR LCN discovery, persistent site research and control/rest identification.

Extract the complete Windows ZIP into a new folder and run MRS.exe. Keep the support folders. Back up the saved frequency library before field testing.

- Right-click a saved DMR site folder > Discover LCN map (single receiver). Each Independent receiver can investigate its own system. Valid assignments trigger one candidate probe, acknowledged tuning, matching full LC checks and return/reacquisition of control/rest.
- Default three separate matching calls to pass. Repeated packets, elapsed gaps and restarting do not inflate confidence. Misses remain inconclusive; competing/contradictory matching evidence remains held for review. Wait for call end favors trustworthy call boundaries, with a configurable maximum.
- Candidate exclusions, search rotation, call identity and evidence persist per site/receiver. Restart loads stopped; Resume is explicit. Known foreign site/subtype/CC evidence is rejected. Control/rest reacquisition searches only selected saved candidates.
- DMR decoded channel roles are retained instead of always saving Observed carrier. Generated labels identify control/rest. Capacity Plus rest remains a last-observed role, distinct from advertised rest numbers in voice LC.
- Optional automatic saved-label/site-status updates default on. Existing channels are updated atomically without new duplicates, custom names/notes stay, and survey-owned passed labels can downgrade when conflicting evidence arrives. Manual definitive LCNs are protected. Folder status reflects unresolved observed carriers and never assumes the total inventory is known.
- Follow and learn supports passed mappings and an explicit tentative option. Use passed map for following creates a separate saved DMR profile. Evidence exports are JSON/CSV. Voice output still depends on successful reception and the bundled decoder.

See Help > DMR LCN discovery and ACCEPTANCE-0.49.0-preview1.md. All validation is local/synthetic; real SDR tuning, live site mapping, rest recovery and DMR audio require station testing. No live RF, hardware access or public reception reporting was used to create the build. No commit/push is included in this build request.

Conservative limitation: discovery currently expects the control and probed voice color codes to agree; mixed-color-code sites may remain unresolved.
