# Current 0.39 following scope

P25 Phase I C4FM, Capacity Plus, Connect Plus and standard Tier III mapped group grants are supported experimentally. Read Receiver fleet and Site channel saving Help for dedicated receivers/imports. Con+/Tier III profiles can specify expected network/site; decoding must match before using that imported map. Carrier ranges: Cap+ 1-8, Con+ 1-15, Tier III 1-4094. Private/data/absolute-MBC and Phase II/CQPSK are unsupported.

# Trunk following - P25 Phase I and Capacity Plus

Keywords: trunk follow control rest grant talkgroup hold skip ignore LCN LSN map carrier return

Open P25 > Trunk following for P25, or Commercial > Trunk following for DMR. Start a live SDR in quadrature mode first. Create a named profile, choose P25 Phase I, Capacity Plus, Connect Plus or DMR Tier III, enter the control/rest frequency, and save. Existing site-bandplan/frequency entries are preserved. Following profiles are stored in the receiver working data folder as trunk-following.json.

P25 uses CRC-valid group grants and received identifier updates. No frequency is guessed before the identifier table arrives. This preview supports Phase I C4FM group voice only; TDMA/Phase II, CQPSK and encrypted voice are outside its scope. Voice must match the grant's NAC and talkgroup and pass existing clear-encryption checks.

Capacity Plus requires a manually researched carrier map. Expand Capacity Plus channel map / discovery research. Carrier LCN 1 represents LSN 1 and 2; carrier LCN 2 represents LSN 3 and 4, and so on. Enter MHz, evidence, and mark Confirmed only after verifying the association. CSV import uses lcn,frequency_mhz,evidence and always imports unconfirmed. Export includes confirmation status for reference; importing does not silently trust it. Color code alone never identifies a system. A rest number advertised at a frequency is not proof that the number belongs to that frequency.

Repeated CRC-valid group status supplies candidate calls. A matching validated full voice link-control talkgroup/slot/color code is required before audio. Late entry without a matching full voice LC remains muted, even if the backend can synchronize to voice. The preview follows supported 8-bit group status entries; private/data calls, LTR/NXDN and absolute-MBC following are not implemented. Repeated advertised rest LSNs can move monitoring only through confirmed carrier mappings. Missed rest changes may require a manual restart on the current rest carrier.

Control and voice decode concurrently when both fit the same usable I/Q capture. Allow hardware retunes outside capture is off by default. Enabling it allows one receiver to leave control for voice, suspending control updates; hang/maximum timers return to control/rest. That paragraph describes Independent/single-receiver mode. For dedicated control and voice SDRs, use Receiver fleet roles with matching trunk groups; the control SDR stays receiving while voice workspaces tune. An out-of-span call without retune permission remains silent until timeout. Stop / return restores control; manual tuning or starting another scanner cancels following.

Export history CSV saves the currently retained event rows; history is bounded and is not automatically restored after restart.

Allow TGs, Ignore TGs and Hold TG use decimal talkgroup IDs. Blank Allow permits any supported group. Apply filters updates the current profile rules; Save profile persists them. Skip suppresses the current group for five seconds. Save TG label stores a label within this profile. Hang defaults to three seconds; maximum call duration to sixty seconds. Voice gain uses the existing P25 or DMR voice gain control. Automatic MP3 VOX receives followed decoded audio with the followed frequency; I/Q records the actual captured span.

This preview requires live acceptance on your systems. Local synthetic/control replay tests cannot establish RF sensitivity, rest-channel discovery, USB/CPU performance or real call completeness. Overrun stops following and reports it; reduce sample rate or other decoder load before restarting.

## Optional confirmation and noisy-signal discovery (0.38 preview2)

Channel identification offers Confirmed observations only, Include tentative observations, and Include noisy / CRC-failed. Tentative results appear on their first qualifying observation. Noisy/CRC-failed rows remain UNVERIFIED regardless of count. Raw payload and validation are available in Analysis. This preference is saved per receiver. Switching views retains existing candidates. Auto-save still requires repeated qualifying evidence and excludes noisy rows.

In Trunk following, uncheck Require repeat / map confirmation while stopped, then save the profile, to try single CRC-valid Cap+ group/rest observations using an entered unconfirmed map. This overrides the repeat/map-confirmation requirements described above for that profile; it never marks the map confirmed. Unresolved/missing mappings stay unresolved. Voice identity/encryption checks and the separate hardware-retune permission still apply.

## DMR slot and color-code attribution

The current-call header, retained trunk history, Session log and CSV export include the protocol and DMR slot. Color code is shown when supplied by the carrier map or established by validated matching full link control. Assigned/Tuned records reflect the expected map values; Decoded audio and call-end records retain available confirmed voice identifiers. A matching full LC can also supply the current source radio ID. Unknown values stay blank; color code is not a network identifier.

The DMR carrier map editor has an optional CC column (decimal 0–15). A blank value allows the voice assignment to bind to its first matching validated full LC. The established code stays fixed for that assignment; a header from another color code does not switch it. A terminator must match the selected TG, slot and established/expected color code before ending the call. With unknown color code and no matching full LC, late-entry termination is not accepted as identified call-end evidence; hang/maximum timers still apply.

CSV carrier maps optionally include color_code. Older files without that column remain compatible. Import retains the supplied code while keeping the map unconfirmed; saving/reloading preserves it. Known control-carrier codes are checked against received control observations, including tentative-following mode. Expected network/site checks remain separate.

For Capacity Plus, rest-channel movement can require a hardware retune on the control SDR. If the mapped new rest carrier falls outside its usable capture, enable Allow hardware retunes outside capture on that control profile when you want it to make those receive-only retunes. A dedicated voice SDR does not remove this control/rest capture limit. Keep the map verified; unsupported or missing mappings remain unresolved.
