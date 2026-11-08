# Pending receiver-pool lifecycle fixes — November 4, 2026

## Reproduced maximum-timer fault

The control receiver previously took an available-call snapshot before expiring voice leases. Retiring the expired grant updated the model, but the old snapshot still contained that call and could dispatch it again during the same update.

An added assertion in the actual synthetic DMR receiver-window path reproduced this before the fix: `Maximum timer immediately redispatched the retired cached grant in the same tick`. The dispatcher now retires expired leases before taking the snapshot used for new assignments. A new qualifying control observation can still offer the same call again.

## Existing assignments and control filters

Previously, changing the control receiver's Allow/Ignore/Hold rules prevented new offers but did not check calls already assigned to separate voice receivers. Pool updates now use the same talkgroup eligibility rule for existing leases, release only calls excluded by current control filters, and leave eligible calls running. Filter release does not pause the receiver. Retiring the excluded cached assignment means a fresh qualifying grant is required after filters allow it again.

## Verification scope

The DMR receiver-window scenario exercises Capacity Plus, Connect Plus and Tier III timer expiry without same-update cached redispatch, new-grant renewal, Ignore release and recovery, in addition to existing assignment, metadata and pause/resume checks. The five-receiver P25 scenario applies Allow and Hold changes while two voice receivers have simultaneous calls, checking selective release, retained permitted calls, unpaused eligibility and fresh-grant recovery. Five new model assertions check the shared eligibility rule, including Ignore taking precedence over Hold.

The receiver workers and control grants are synthetic. No live SDR, USB/GPS operation, RF transmission or reception report was performed. DMR AMBE and real-system following acceptance remain open. No version change, release ZIP, installation, commit or push was performed.

## Final local results

- Development compilation: zero errors; two existing NU1900 warnings because NuGet vulnerability-feed metadata was unavailable. This is not a dependency security audit.
- Full suite: **1,927 PASS assertions**, zero FAIL lines, successful exit.
- DMR following UI: **three mapped protocol profiles passed**, including added maximum-timer and filter-change checks.
- Five-receiver P25 pool UI: passed, including selective Allow/Hold release and fresh-grant recovery.
- P25 generated-I/Q voice/log regression: **six cases passed**; clear matching audio, muted identity/encryption mismatches and terminator lifecycle remain correct.
- Scoped whitespace checks passed.

Final development artifacts: work/fleet-lifecycle-final/self-test.txt, dmr-follow-ui.txt, trunk-audio-ui.txt and ui-smoke-result.txt. The pre-fix reproduction is retained separately under work/fleet-lifecycle-check/ui-smoke-error.txt.

## Mixed-coverage dispatch follow-up

An additional synthetic five-receiver check reproduced an unsupported first grant blocking a later supported call: a 3 GHz grant with only RTL-range voice receivers was followed by a supported 153 MHz grant, but dispatch stopped at the first unavailable target. Changed dispatch to continue checking subsequent grants. The skipped grant stays subject to ordinary freshness and eligibility rules and is not falsely recorded as tuned.

The new assertion failed before the fix (work/fleet-dispatch-repro/ui-smoke-error.txt) and passed afterward with the existing five-receiver lifecycle/UI checks (work/fleet-dispatch-final/ui-smoke-result.txt). Development compilation passed with zero errors and the existing two NU1900 feed warnings; scoped whitespace checks passed. The earlier full-suite count of 1,927 is from the preceding lifecycle revision; this one-line dispatch follow-up was verified with the targeted receiver-pool integration check, without repeating unrelated decoder tests. No live devices or release packaging were used.
