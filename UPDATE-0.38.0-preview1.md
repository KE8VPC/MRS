# M.R.S. 0.38.0-preview1

Adds Public Safety / Business > Trunk following with saved per-system profiles.

- P25 Phase I C4FM group following uses CRC-valid control grants and received identifier tables, with NAC/talkgroup and clear-voice checks.
- First Capacity Plus workflow adds manual confirmed carrier LCN/frequency maps, CSV import/export, bounded discovery history, repeated group-status selection, selected-slot voice checks and mapped rest changes. Unresolved mappings remain explicit; color code never merges systems.
- Shared Allow / Ignore / Hold, Skip, hang and maximum timers, active call display and per-profile talkgroup labels.
- Separate control and voice decoders operate inside one captured I/Q span. Optional hardware retunes outside capture suspend control until return; off by default.
- Followed audio feeds monitoring and MP3 VOX with the call frequency. Existing I/Q recording preserves the captured RF span.

Extract the entire ZIP into a new folder and keep Decoders, Receivers, Help and bandplans beside MRS.exe. Start a live quadrature receiver; open Trunk following, create a profile, enter control/rest MHz and save. For Capacity Plus, research and confirm the carrier map first. Read Help > Trunk following and ACCEPTANCE-0.38.0-preview1.md.

Scope: P25 Phase I C4FM only; supported Capacity Plus group-status calls only. No Phase II/CQPSK, private/data following, Connect Plus, Tier III, LTR or NXDN following. Voice remains muted for encrypted/unknown/mismatched identity. This is an experimental preview and live trunk-following acceptance remains pending.

All 0.37 features and acceptance checks carry forward, including snap-to tuning, MP3 VOX/Both, centered Aviation entry and sensor counter clarity. Previous accepted features remain closed unless a regression appears. Broader sidebar cleanup and APRS TX intermittency remain deferred.
