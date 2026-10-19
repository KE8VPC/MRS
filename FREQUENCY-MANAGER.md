# Frequency manager

User clarification after the 0.3.0 test passed: the example tree below illustrates hierarchy only. Do not create those folders automatically. From 0.3.1 onward, new libraries contain only the empty Frequency list root, and users create their own folders.

New explicit exception requested September 30, 2026 (TODO, not implemented): supply a default **Meshtastic / LoRa (US)** folder with all documented Meshtastic US RF slots, grouped by modem preset/bandwidth, including exact frequencies, slot numbers and modem parameters. LongFast's 104 slots are one preset's list, not the complete multi-preset catalog. Seed once without duplicating entries or replacing user data; preserve deletions and offer explicit restoration. Keep the folder initially collapsed and use existing marker visibility/density controls. Do not restore the older illustrative starter folders. Full decoder, dataset provenance, update and acceptance scope is recorded under “Meshtastic / LoRa receive decoding and U.S. frequency folder” in ROADMAP.md. Existing 0.15.0-preview1 packages remain unchanged.

Status: first implementation in 0.3.0. Nested folders, sidebar browsing/editing/search, drag-and-drop/reordering, saving current or pointed RF frequency, descriptive metadata, local persistence/backup/Undo, color/visibility, spectrum/waterfall markers and explicit supported-mode tuning are implemented for testing.

Still planned: import/export and RadioReference integration, bandwidth fields/shading, preferred decoder/observation status, structured protocol-specific fields and automatic metadata enrichment. Signaling/system identifiers currently use a freeform field so their notation is preserved. Folder nesting currently supports up to 64 levels. The requirements below describe the full target, including these remaining items.

## Operator workflow

Save an interesting signal from the current tuned frequency or a selected point on the RF spectrum/waterfall. Enter a name and notes, select or create a folder, and store the observed signal details. The resulting entry appears in the frequency-manager sidebar and, when within the displayed RF span, as a labeled frequency marker on the RF spectrum/waterfall.

## Saved entry

- Stable entry ID and frequency stored as integer Hz; display in Hz, kHz or MHz without losing precision.
- Friendly name, freeform notes, tags and optional color.
- Receiver mode/demodulation (for example AM, NFM, WFM, USB, LSB or CW), separate from signal protocol/service (for example RDS, HD Radio, DMR/MOTOTRBO, P25 or packet). Allow multiple service labels at one frequency.
- Optional channel bandwidth, modulation details, preferred decoder, and observed/confirmed/unknown status. Preserve unknown values explicitly rather than assuming defaults.
- Protocol-specific optional fields: P25 NAC, DMR color code/timeslot, CTCSS/DCS, system/site identity and talkgroup references where relevant. Preserve the notation/base of identifiers; for example, do not silently interpret `NAC 348` as decimal or hexadecimal without a defined field convention.
- Optional first-seen/last-seen times and origin (manual entry, observation, recording or later authorized database import).
- Folder membership independent of protocol. A user can put any entry in any folder without validation forcing a particular taxonomy.

Saving a protocol label does not imply that a corresponding decoder is available or that the protocol was automatically identified. Frequency storage is independent of currently attached receiver coverage, so HF entries remain valid even when the active NESDR cannot tune them directly.

## Folder sidebar

- Arbitrarily nested folders, with user-selected names and ordering.
- Create, rename, move and delete folders/entries; drag-and-drop organization.
- Search across names, frequencies, notes and protocol fields; filter by mode, protocol or tags.
- Single-click a saved signal to select it without tuning. Edit or F2 opens the selected entry. Double-click (using the Windows double-click timing) recalls its frequency, supported receive mode and saved filter width in that receiver window. The Tune button remains available. Ctrl/Shift selection does not tune. Folders, drag-and-drop organization, search/reloads and programmatic marker selection do not tune. Unsupported modes/decoders are visibly identified rather than silently replaced. Replay and hardware coverage checks still apply.
- Selection, hovering and saving do not transmit. Mere selection should not unexpectedly retune an active recording.
- Allow multiple distinct entries on the same frequency for different sites, systems or observations; flag possible duplicates instead of automatically merging them.
- Local persistence, portable export/import and a recoverable deletion workflow. Carry stable IDs and provenance forward into future RadioReference imports.

## Spectrum and waterfall overlay

- Labeled markers at saved frequencies within the current RF span, with optional bandwidth shading.
- Hover shows the entry name, frequency, mode/protocol and notes summary; selection reveals full details in the sidebar.
- Folder/entry visibility controls and color coding; manage label collisions and overlapping entries.
- Keep marker positions correct after tuning, resizing and future zoom/pan operations.
- Treat markers as saved-frequency annotations, not proof that a signal is currently active.
- For waterfall history, associate rows with their captured frequency span or clear/rebase history on retuning; never place annotations against an incorrect historical frequency axis.

## Illustrative organization

The following is a suggested starting structure based on the user's example, not a verified frequency database or fixed set of folders. Labels remain editable; numeric examples do not establish on-air activity or supported decoding.

```text
Frequency list/
├── WFM Stations/
│   ├── 102.100 MHz — WFM — RDS / HD
│   ├── 96.600 MHz
│   └── 98.900 MHz
├── CW/
├── MOTOTRBO/
│   ├── Site 1/
│   ├── Site 2/
│   └── Unknown/
├── Amateur/
│   ├── HF/
│   ├── 2 meter/
│   └── 440 MHz/
└── P25/
    ├── Control channels/
    │   └── 774.306250 MHz — NFM — P25 — NAC 348 [notation to confirm]
    └── Voice channels/
```

The supplied example also placed `7.20 MHz LSB` under `CW`; retain the ability to organize folders this way without equating CW and LSB. The final voice-channel example ended at `80`, so no complete frequency is inferred or seeded from it.

## Delivery stages

1. Local entry model, nested folders, sidebar browsing/editing/search, and save-current-frequency workflow.
2. RF spectrum/waterfall annotations and entry recall with receiver capability checks.
3. Protocol metadata enrichment, discovery-assisted saving, import/export and integration with the planned RadioReference importer.

Acceptance should cover persistence across restarts, precise fractional-MHz frequencies, deep folder moves, duplicate frequencies, multiple labels per signal, unsupported receiver coverage, marker alignment and readable overlapping labels.
# 0.8.0 update

Saved entries now include optional receive-filter width in Hz (0 means mode default). NFM, WFM, AM, USB and LSB can be recalled within current receiver coverage. Unsupported widths/modes remain stored and are reported instead of silently substituted. In-range recall uses the software channel; changing sources or recalling outside the available capture may reopen/retune the receiver. New libraries still contain no starter folders. The earlier planning notes below remain historical context.

## Next milestone: shared frequency library (implemented in source)

Receiver A and Receiver B now read the main MRS frequencies.json library. Saving, moving, hiding, deleting or undoing an entry refreshes both open windows and their spectrum markers in the same application process. Clicking a saved signal or selecting Tune controls only the window used. Receiver settings, audio, capture and display settings remain independent.

On first use, an existing ReceiverB/frequencies.json is imported under an "Imported from Receiver B" folder, preserving nested folders and metadata and assigning new IDs to avoid collisions. The legacy file is left unchanged. A migration flag is saved atomically with the shared library, so later launches do not repeat the import. Entries at the same frequency are retained rather than guessed to be duplicates. Invalid files stop loading with an error instead of being overwritten.

Each panel retains its own selection and search. An update from another panel reloads its library and clears its local Undo history. If an edit dialog was opened before another window saved a change, its stale save is rejected; reopen the editor to use the latest entries. Separate application processes retain file-lock/revision protection, but need Reload to see outside changes.

The compact 0.11.0 ZIP still has separate libraries. This change and the broadcast contrast fix await the next milestone package. No user profile files were migrated during development tests.

## Selection and editing update (source change pending next package)
- Single-click selects only. Double-click uses Windows timing and tunes only a signal; folders and modifier-assisted selection do not tune.
- Edit or F2 opens the single selected entry. Selecting multiple entries gives a clear message rather than editing an arbitrary entry.
- Shift + Up/Down extends or contracts a range from its anchor. Ctrl-click toggles entries; Ctrl + Up/Down moves focus without changing the group; Ctrl + Space toggles the focused entry.
- Right-click offers Edit, Delete and Tune. Right-clicking a selected entry preserves its selected group; right-clicking an unselected entry selects that entry. Edit/Tune require one entry; Delete acts on the group.
- A single custom selection highlight avoids disagreement with the TreeView's separate focused-item highlight.

Acceptance: select an entry without retuning, open/cancel/save Edit, double-click to tune, select a range with Shift/arrows and delete/undo, and verify right-click actions while DMR auto-save is active. Changes must appear in the list and RF markers and survive restarting.

Marker visibility correction (pending next package): separate Show markers / Hide markers actions apply to all highlighted entries as one undoable save. Both actions are also available on right-click. Saved entries remain in the frequency list; hiding changes spectrum/waterfall markers. A hidden parent folder continues to suppress its children. No selection produces an explanatory status instead of silently doing nothing.
