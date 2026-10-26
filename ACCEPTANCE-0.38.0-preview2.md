# Acceptance - 0.38.0-preview2

1. Under DMR / LTR / NXDN channel identification select Include tentative observations. Monitor a weak channel: first qualifying observations should appear as Tentative / single observation.
2. Select Include noisy / CRC-failed. Candidates rejected by repeat/quality filtering should appear with UNVERIFIED / noisy evidence and corrections. Repetition must not relabel a CRC-failed row as verified. Check raw payload/validation in Analysis. The modem still needs enough sync/structure to produce an observation; pure noise need not yield a row.
3. Switch to Confirmed observations only. Tentative/noisy rows must be hidden, not promoted. Switch back to recover retained candidates. Restart M.R.S. and verify discovery preference persistence. Keep auto-save enabled if desired and confirm unverified rows do not create saved frequencies.
4. Stop following, uncheck Require repeat / map confirmation, enter a candidate Capacity Plus carrier map without checking its Confirmed boxes, save the profile and start. A supported single CRC-valid status can try the mapped group/slot. History must label tentative selection. It must not silently mark the map confirmed. Test single-observation mapped rest changes with the same setting.
5. Re-enable confirmation and verify previous repeat/map requirements. Reload the profile and confirm the saved setting. Existing profiles missing this new setting should retain confirmation enabled. Hardware retunes outside capture must still obey their separate permission checkbox.
6. Failed-CRC grants must not be treated as verified control assignments; existing voice group/NAC/slot and encryption behavior remains. All original 0.38-preview1 following checks remain pending until tested live.

Carry ACCEPTANCE-0.38.0-preview1.md and ACCEPTANCE-0.37.0-preview1.md with this update; no untested item is marked passed by this build.
