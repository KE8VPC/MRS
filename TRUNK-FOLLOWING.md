# 0.39 implementation

Adds in-process dynamic receiver workspaces, dedicated voice leases with applied-capture acknowledgement, scope invalidation, manual pause/resume and a receiver-priority pool. Adds CRC-validated Connect Plus FID 6/opcode 3 group grants (options 2), and standard Tier III FID 0/opcode 49 mapped group grants. Private/data/absolute-MBC calls remain unsupported. Imported Con+/Tier III expected network/site blocks following until decoded identity matches; wrong identity stops the profile. Full group/slot voice identity gates apply to all DMR variants.

Primary field reference: https://raw.githubusercontent.com/lwvmobile/dsd-fme/audio_work/src/dmr_csbk.c ; implementation checked against the already pinned bundled 69d3115 source. The independent Con+ example payload in that source is included as a regression vector. No native dependency was changed. Prior 0.38 design notes below remain historical where superseded.

# Trunk-following design and scope - 0.38

The follower has separate control and voice downconverters attached to the receiver-owned raw I/Q stream. It never opens a second device. The input queue is bounded and nonblocking; overrun stops following. Epochs discard old capture blocks after stop/restart. Voice contexts include the selected protocol, RF carrier, slot, group, NAC and call start. Audio boundaries prevent clips joining different calls; automatic MP3 uses the followed carrier frequency.

P25Pipeline retains received identifier tables across intentional capture reacquisition, but a new follower session clears them. CRC-valid assignments on the selected control frequency can authorize only resolved FDMA group calls. The voice decoder checks expected NAC/group and retains its existing FEC and clear-encryption metadata gates. Phase II and CQPSK are unsupported.

Capacity Plus uses CRC-masked CSBK opcode 0x3E / manufacturer 0x10, bounded first/continuation/final assembly, two activity banks and supported 8-bit group entries. It requires repeated status and a user-confirmed carrier map. Before PCM is released, full voice LC must match the selected group/slot and bound control CC. Late entry without a matching full voice LC remains muted; embedded-LC-only late-entry authorization is not implemented. The existing DSD-FME backend continues to mute encrypted voice. The manually selected profile scopes observations; CC alone is not a network identity. Imported maps are unconfirmed. No inferred LSN-to-frequency pairing is automatically promoted. Data/private entries and other trunk variants are unsupported.

The Capacity Plus wire-field reference was checked against the pinned bundled DSD-FME source (commit 69d3115, src/dmr_csbk.c), including fragment flags, seven-byte continuation content and both group activity masks. Primary upstream references:

- https://github.com/lwvmobile/dsd-fme/blob/audio_work/src/dmr_csbk.c
- https://github.com/lwvmobile/dsd-fme/blob/audio_work/examples/Example_Usage.md

The native backend and its existing license/source bundle are unchanged. Managed follower/parser logic was implemented in this pass; no additional native dependency was introduced.

Known limits: this is a first experimental following implementation. One capture only; optional physical retunes suspend control reception and may miss grants/rest updates. Cap+ rest loss requires manual reacquisition. No automatic multi-site discovery, no second physical receiver coordination, no private/data call following and no recording/replay hardware control. Managed DSP load at the user's actual HackRF sample rate requires live acceptance. Bounded discovery history holds up to 500 visible events; export CSV before older rows expire. Profile/maps/labels are persisted, live history is not.

0.38 preview2: RequireConfirmations is per-profile and defaults true for existing profiles. When false, Cap+ uses an entered candidate map and one CRC-valid group/rest observation; history explicitly labels tentative calls. No CRC-failed control data authorizes a follow. Discovery can independently expose tentative/rejected rows, with validation state isolated from good rows and automatic saves.
