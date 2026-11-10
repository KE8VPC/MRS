# Station acceptance — 0.49.0-preview1

Back up the saved library, extract the entire ZIP to a new folder, and use one known DMR site first.

- [ ] On an Independent live receiver, select a single site folder and verify Control/rest MHz and candidate selection.
- [ ] Observe control > one probe > return, including a hardware retune outside the original capture. Confirm the displayed/applied frequencies agree.
- [ ] Check three separate calls against a known channel map. Repeated packets and restarting must not add calls. Try an unresolved quiet channel; misses must stay inconclusive.
- [ ] Verify TG/slot/CC/radio evidence and different-call contradictions. Competing candidates must not pass until reviewed. Excluded frequencies must stay excluded after restart.
- [ ] Check control roles and Capacity Plus rest recovery on a real site. Ordinary voice/another network must not become control. Unmapped advertised rest numbers must not become guessed frequencies.
- [ ] Stop/restart MRS, resume the same site and verify saved progress. Disconnect/reconnect the SDR; activity must remain stopped until explicitly resumed.
- [ ] Check saved custom names, notes, exact centers and entry counts. Tentative/pass/conflict transitions should update the same entry. Folder status must remain incomplete when appropriate. Undo should restore a metadata batch.
- [ ] Run a second receiver on a different system; verify independent search assignments and no voice-pool takeover.
- [ ] Test Follow and learn / optional tentative following and Use passed map for following. Confirm audible voice, correct attribution and return on end/timeout.
- [ ] Export research JSON/CSV and inspect retained evidence. No local system data should be present in a fresh portable archive.

Pending live acceptance: synthetic checks do not establish a real site's correct map, RF timing, AMBE voice, reliable rest reacquisition or universal protocol coverage. A passed mapping meets the configured evidence rule; inspect conflicts and independently check important results.
