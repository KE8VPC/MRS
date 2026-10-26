# DMR, Capacity Plus and LTR
Context: DMR
Keywords: MOTOTRBO color code talkgroup radio slot rest channel network trunk

Select DMR / MOTOTRBO for voice. Slot selection controls which TDMA slots are heard. DMR / Cap+ and LTR identification can observe signaling alongside voice. A color code alone does not identify a system; keep frequencies and decoded network/site data separate.

Only validated supported link/control payloads supply IDs. Repeated matching evidence confirms identifiers; isolated corrupt color codes should not be treated as new systems. Existing labels survive automatic metadata updates. Capacity Plus may expose rest-channel information without an explicit network ID. The separate Trunk following tab adds experimental mapped Capacity Plus, Connect Plus and standard Tier III group following. Use an explicit carrier map; Con+/Tier III imported network/site must match decoded identity. Read Receiver fleet and Trunk following Help for scope and setup. Encrypted voice is muted by the backend; the interface is not evidence of authorization or encryption-key support.

## Optional confirmation and noisy-signal discovery (0.38 preview2)

Channel identification offers Confirmed observations only, Include tentative observations, and Include noisy / CRC-failed. Tentative results appear on their first qualifying observation. Noisy/CRC-failed rows remain UNVERIFIED regardless of count. Raw payload and validation are available in Analysis. This preference is saved per receiver. Switching views retains existing candidates. Auto-save still requires repeated qualifying evidence and excludes noisy rows.

In Trunk following, uncheck Require repeat / map confirmation while stopped, then save the profile, to try single CRC-valid Cap+ group/rest observations using an entered unconfirmed map. This overrides the repeat/map-confirmation requirements described above for that profile; it never marks the map confirmed. Unresolved/missing mappings stay unresolved. Voice identity/encryption checks and the separate hardware-retune permission still apply.
