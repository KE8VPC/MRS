# Site channel discovery and saved frequencies

Keywords: auto save site map frequency manager P25 DMR network RFSS channel LCN uplink downlink talkgroup import

In P25 Systems enable Auto-save site channels. Include provisional / tentative is optional. Save existing site discoveries processes retained live catalog entries without waiting for another announcement. DMR channel identification exposes the same options under Site frequency saving and offers Save existing DMR site channels. Existing DMR Auto-save also uses site-scoped channel records.

P25 folders use P25 / WACN-System / RFSS - Site / frequency. DMR uses DMR / system type / Network / Site / frequency when the on-air identifiers are decoded. Unknown identities remain explicitly provisional/unassigned, scoped without assuming that a shared color code identifies a system. Neighbor-channel announcements belong to their advertised target site and are labeled advertised. A different advertised system does not inherit an untransmitted WACN from the receiving system.

Only resolved carrier frequencies become frequency entries. Received identifier tables resolve P25 grants; unknown channels stay in the discovery catalog. DMR logical channels need a map before a voice frequency is known. Supported mapped DMR voice assignments can add their resolved carriers, labeled according to confirmation evidence. Control receivers feed discovery independently of the currently selected visible tab.

Channel records retain channel IDs/LCNs, site/system identity, roles, last-seen evidence and paired uplink when known. Repeated observations update one scoped frequency entry. User names/notes are preserved. The same frequency can belong to separate sites. Talkgroups and radio IDs are not generated frequency names or permanent carrier attributes. Call history retains temporary associations. The Talkgroups tab stores system-scoped group IDs, editable labels, reception counts and last-seen time separately; trunk profile Allow/Ignore/Hold/labels remain separate follow rules.

Status reports added/updated entries, pending saves, and unresolved/provisional observations. Frequency editors temporarily defer automatic writes; queued saves resume afterward. Replay does not automatically populate live frequencies. Malformed alias files are left unchanged rather than silently replaced.

Import P25 channel data reads a user-selected DSDPlus.P25data file into explicitly historical site/channel references. Import DSDPlus map in Trunk following reads Con+, Cap+ and supported TIII frequency tables, lets you choose the system/site, and imports carrier mappings as unconfirmed. DSDPlus OTA slot-channel numbers are converted to carrier numbers; original numbers are retained in evidence. Unknown zero frequencies remain unresolved and conflicting slot-pair frequencies are rejected. Tier III area.site notation requires explicit mapping instead of being guessed. Configured Con+/Tier III network/site IDs must match decoded identity before an imported map is used for following.

Imports are explicit. Fresh installations contain nationwide reference bandplans/allocations, without local systems, site maps, aliases, paging assignments, receiver definitions or reception logs. Upgrades preserve your existing data. Private development backups are not release defaults or live-RF acceptance.
