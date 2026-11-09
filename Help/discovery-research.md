# Discovery matching, overlaps and channel research

Keywords: duplicate auto-save research existing only pending held rename system site folder identity LCN LSN carrier overlap center lock snap

## Auto-save without repeated entries

Frequencies has Auto-save: new + updates and Research: update existing only. The preference is shared with the saved library. P25 and DMR site auto-save/tentative switches are separate in their primary workspaces; the DMR identifier panel also retains its existing direct auto-save checkbox. Every site-saving path uses the same library matcher.

A unique compatible discovery within 1 kHz can enrich an existing channel while keeping its saved exact frequency, stable ID, custom name, notes and manual placement. LastObservedFrequencyHz retains the latest observed center. More-specific classification/identity can replace provisional scope; later incomplete observations do not erase known site/subtype information. Conflicting identities or several possible matches are held instead of generating another marker. Potential matches up to 5 kHz away are held rather than silently merged. In Research mode, genuinely new site channels are held too; RF activity auto-saving cannot add new list entries in this mode.

Open Frequencies > Review discoveries to inspect the bounded local queue (up to 500 latest pending entries). Select a compatible saved entry to enrich, or deliberately Keep as separate channel. Explicit merge review is limited to a 5 kHz separation and still rejects conflicting known identifiers. Closer compatible pending observations coalesce. An outdated held item requires Reload. The queue and research preference survive restart in the library file; shared bundles/library-data exports contain saved channels/catalog, not this transient queue. File-level frequency-library backups retain the queue.

Renamed discovered system/site folders are matched by identity bindings, not their editable labels. Older folders can adopt bindings from saved descendant scope evidence. A folder without identity evidence is not guessed from a custom name. User-initiated cleanup/import also respects known bindings. Existing duplicates are not removed automatically; use Library backup and the cleanup preview, which keeps conflicting records for review.

## Overlapping spectrum/waterfall entries

Hover near saved markers for names, exact frequencies, folder paths, identifiers, channel IDs and saved IDs. Right-click > Saved entries here opens the entries within the current marker hit area. Ctrl-click also opens the chooser when several markers overlap. Select the exact record to Edit, Show in frequency list, Tune or Hide marker. Records can share an exact frequency or merely overlap at the current zoom; overlap alone is not proof of a duplicate. The saved-entry ID determines which record is edited.

## Firm center lock and click snapping

Center lock stays selected through dragging, Shift+wheel, arrow panning, zoom, full span and tuning. Pan attempts are ignored while locked; deliberately choose Free view to pan. The visible center follows the tuned channel as far as capture boundaries allow. Scan interaction lock remains a separate constraint on the capture view.

Snap: 500 Hz is the new default for spectrum and waterfall clicks, giving four decimal places with the fourth digit 0/5. Saved/text-entered frequencies retain full precision. Snap off permits fine tuning; tuning-step and bandplan modes remain available. Existing off settings migrate once to the new 500 Hz policy; choosing off afterward remains saved. Channel plans requiring five/six decimals remain supported, and bandplan snapping can select their exact centers.

## LCN research

Right-click one saved DMR site folder > Research LCN map. Commercial > Channel research shows its candidate frequencies. Check Control MHz and Start research; keep one receiver decoding control and other receivers decoding the saved voice candidates. No SDR is started, scanned or tuned automatically by this tool.

The tool correlates validated group grants (Tier III/Con+) or CRC-valid Cap+ group status with a matching talkgroup/slot full voice LC within 1.5 seconds. Known network/site mismatches and failed validation are rejected. Rapid repeated packets are counted no more than once per candidate per five seconds. Correlation counts are samples, not proof of independent calls; all associations remain unconfirmed. Competing LCN/frequency associations are marked conflicting. One site is researched per running application session; starting another clears the previous research session, so export CSV first if needed.

Use candidate in DMR map adds an unconfirmed carrier map row; a conflicting candidate is blocked. Label saved channel as candidate stores LCN ?number while preserving the custom name; candidate IDs appear alongside custom labels and in details. Neither action marks the mapping confirmed. Independently verify the carrier/slot association before checking Confirmed for following. Stop retains the current session's results; export CSV to preserve them across application exit. Absolute-frequency/MBC decoding and automatic definitive mapping remain outside this backend.
