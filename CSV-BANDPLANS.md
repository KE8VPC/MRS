# 0.42 discovery references and saved-channel import

New selectable aviation, HF/VOLMET/HFDL, Amateur operating, WWV and U.S. ISM layers are included. Missing supplied discovery files are added without overwriting edited CSVs. Use **Add reference channels to frequencies**, choose a channels.csv, and select rows in the preview. Candidate rows start unselected. Existing matching receive frequencies/modes and labels are preserved; imported entries retain protocol/source notes. Imports use the Frequency Manager undo/persistence path. No transmit permission is granted. See AVIATION-FREQUENCIES.md and Help/discovery.md.

# 0.33 railroad addition

reference/railroad adds an independently selectable AAR/HOT/EOT dataset. Existing working folders can add its missing files with Create missing reference CSVs. Existing CSV files remain untouched. See RAILROAD-BANDPLAN.md for coverage, source references and mode limits.

# CSV bandplans and P25 site collection — 0.32.0-preview1

Open the **Bandplans** tab. The default directory is `bandplans/` beside MRS.exe. **Open folder** opens its location; **Choose folder** selects an existing working directory. The selected location is remembered in your receiver profile, so you can retain it when extracting a newer build. Do not replace your edited working folder with the defaults from a new ZIP.

**Reload CSV** reads changes without rebuilding or restarting. Each listed folder can be hidden independently. The four supplied reference layers share their selection state with the existing Display checkboxes: U.S., ARRL, NIFOG and TV. Reference datasets retain their prior source/review dates; this release migrates their spectrum display into editable files, not a new regulatory review. Existing automatic modulation suggestions remain separate from this CSV display feature.

**Create missing reference CSVs** restores absent reference files only; it does not overwrite existing files. Newly chosen empty folders can be initialized this way. Validation errors identify the affected file. Invalid files are omitted from the overlay, not silently accepted. Reparse-point directories are not followed. Limits: 4 MB and 10,000 rows per file, 1,000 CSV files and 50,000 markers and spacing rules per root.

## Collecting P25 channels

1. Start the live receiver on the P25 control channel and enable P25 decoding in P25 Systems.
2. In Bandplans, enable **Collect validated live P25 site bandplans**.
3. Wait for the control channel to supply its WACN, system, RFSS/site identity and identifier definitions. Collection writes updates about every five seconds.
4. Select the desired site folders. Observed downlinks appear by default. **Show uplinks** adds the paired uplink markers.

Example:

```
bandplans/p25/BEE00-348/site-001-022/channels.csv
bandplans/p25/BEE00-348/site-001-022/spacing.csv
```

Only CRC-validated live observations contribute. I/Q replay does not populate live site files. An adjacent-site announcement goes to its explicitly advertised site when its system can be identified, not automatically to the monitored site. Announcements for a different system without a known WACN remain unresolved.

Unknown-site/frequency observations remain separate. Up to 512 pending observations are retained in memory and written under `p25/unresolved/<session>.json`. They can be associated only within the matching reception context. A retune/reset prevents old pending evidence from attaching to a new site. Unresolved files are diagnostic evidence, not loaded spectrum plans, and are not re-associated after restart.

Repeated channel observations update existing rows. User labels and extra columns are preserved. First/last observation times, validation/source and raw message evidence are recorded. Different observed mappings for the same LCN are retained and marked as conflicts. Resolve conflicts manually with collection paused; repeated contradictory broadcasts may reintroduce the conflicting row.

Collection does **not** add stations to the frequency manager, change tuning, follow voice grants, or assign permanent talkgroup names to frequencies. Control-channel mappings describe advertised channels, not proof that every mapped transmitter is presently on air.

## Editable schemas

CSV files use UTF-8, a header row, comma separators and standard quoted fields. Quotes, commas and newlines in labels are supported. Preserve LCNs as text (for example `01-1968`) when editing in a spreadsheet; do not allow date conversion.

- **bands.csv:** `start_mhz,end_mhz,label,source`
- **channels.csv:** `lcn,downlink_mhz,uplink_mhz,slot,role,label,first_seen,last_seen,source,conflict`
- **spacing.csv:** `identifier,base_mhz,spacing_khz,offset_mhz,slots_per_carrier,first_channel,last_channel`

`label`, `source` and uplink are optional in channel rows. An unknown uplink stays blank. Slot numbers retain the decoder's zero-based convention. Identifier numbers are decimal, 0–15. Channel-number bounds default to 0–4095; slots per carrier default to 1.

Frequency columns may use `_hz`, `_khz` or `_mhz` suffixes with invariant decimal values (decimal point, no thousands separators). Use one unit column per field. Values must resolve to whole Hz. Negative offsets are supported. Labels apply to display only and do not alter channel identity.

Pause collection before manually editing an actively collected site's CSV, save your edits, then Reload CSV. Collection reads the existing file before merging and refuses to overwrite malformed files.

## Spectrum display

Overlapping selected sites share a marker; its compact label includes an additional-entry count. Hover over the bandplan strip for the contributing site details and paired frequencies. Site and custom markers obey Display's channel-marker visibility and global hide-bandplans setting.

**Show calculated grid** adds dashed spacing markers explicitly labeled `grid`. These are possible frequencies calculated from definitions, not observed channels. A rule is omitted from the calculated grid when more than 300 carriers would appear in the current view; zoom in. Observed channels remain independent of this grid. At most 600 distinct channel lines are drawn in a view to keep the display responsive.

## RX/TX light

The compact 10-pixel indicator is beside the clock. Green RX means the live SDR is delivering samples; red TX means the TX worker reported active transmission and takes priority. Gray Idle covers stopped/awaiting data and I/Q replay. RX becomes idle after about 1.5 seconds without samples. TX clears on worker completion, cancellation, failure or window closure. The light reports software/device state, not independently measured radiated RF.
