# M.R.S. 0.39.0-preview1

This preview adds multiple receiver workspaces, dedicated control-to-voice assignments, automatic site-channel saving and consistent monitoring controls.

- Receivers opens Add / Open / Remove / Save assignment management with names, roles, trunk groups and receiver priorities. Existing A/B settings are preserved; additional workspaces have independent receiver settings.
- A control receiver can keep receiving while it dispatches separate group calls to eligible live voice receivers. Frequency, protocol, TG, slot and NAC checks travel with assignments. Applied capture is acknowledged before voice decode; manual tuning/Stop pauses pool eligibility until Resume voice pool. Stop/close and serving-identity changes release obsolete assignments.
- P25 site discovery can auto-save resolved channels into system/RFSS/site folders in Frequency Manager. DMR site saving groups on decoded network/site rather than CC alone. Save existing actions process retained live discoveries. Names/notes survive updates and duplicate grants update one carrier entry. Neighbor announcements retain target identity and advertised status.
- Save to frequencies no longer generates talkgroup names/attributes for RF channels. Channel IDs/LCNs, roles, identity, uplinks and last-seen evidence belong to channel records. System-scoped talkgroup records/labels live separately.
- Initial mapped Connect Plus and standard Tier III group-grant support joins Capacity Plus. Import a DSDPlus frequency map or P25data explicitly; imported evidence remains historical/unconfirmed. Logical channels without a frequency mapping stay unresolved. Imported Con+/Tier III identity must match decoded network/site.
- Clear view and persistent Newest on top / Oldest on top controls cover monitoring tables. Grouped repeats use last received time. View clearing preserves saved data and decoder state. HD/TV, voice diagnostics and session logs also have clear-view actions.

Start: extract the complete ZIP, open Receivers, add/select/start SDR workspaces, save matching Trunk control / Trunk voice roles and group, then configure/start following in the control workspace. Enable site-channel saving where desired. Read the new offline Help topics and ACCEPTANCE-0.39.0-preview1.md.

Scope: P25 Phase I C4FM group voice; supported mapped Cap+/Con+/standard Tier III group voice. No Phase II/CQPSK, LTR/NXDN trunk following, private/data or absolute-MBC following. Late entry without supported identity evidence remains muted. Hardware sensitivity, USB/CPU load, reconnect and live call completion still need user acceptance.

Fresh installs contain nationwide reference allocations only. Local site maps, saved lists, aliases, paging assignments and receiver definitions come from user reception, entry or explicit import. Private development backups and comparisons are excluded from runtime/source packages. Previous accepted features stay accepted unless a regression appears; outstanding 0.37/0.38 checks carry forward. Broader sidebar cleanup and APRS TX intermittency remain deferred.
