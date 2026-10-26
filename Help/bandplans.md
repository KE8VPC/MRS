# Bandplan manager and editable CSV
Context: Bandplans
Keywords: import export edit delete custom duplicate spacing color channels.csv bands.csv

Display > Bandplans contains Show bandplans, Show channel markers and Manage bandplans. Analysis > Bandplans lists individual plans and P25 collection options. Per-plan visibility is remembered. Reload CSV applies external file edits.

New creates a custom plan. Import CSV previews channels.csv, bands.csv and/or spacing.csv in an editor. Edit custom plans in the table; add rows at the bottom and Delete selected rows. Save validates the data before applying. Duplicate makes an editable custom copy of supplied/collected data. Export writes CSVs into a chosen folder without overwriting existing files. Delete custom asks for confirmation.

channels.csv uses downlink_mhz, optional uplink_mhz, label, lcn, role, color and source. bands.csv uses start_mhz, end_mhz, label, color and source. spacing.csv uses base_mhz, spacing_khz, optional offset_mhz, identifier, slots_per_carrier, first_channel and last_channel. Color accepts #RRGGBB. Use decimal points. Quoted commas are supported. Extra imported columns survive editing.

Supplied reference and collected-site plans are protected from in-app editing/deletion; duplicate them first. User CSVs are not replaced by reference seeding. Calculated grids are explicitly unobserved. Keep backups before external edits. Railroad references can overlap intentionally; PTC-7 source discrepancy remains flagged.
