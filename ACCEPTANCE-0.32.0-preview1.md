# Acceptance — 0.32.0-preview1

## DMR status — October 24, 2026

**DMR voice: confirmed by user.** The user heard decoded voice. Slot-selection variants and other individual checks are not separately inferred from this report. **Overall DMR sign-off remains pending completion and acceptance of trunk following**, as explicitly requested by the user. Current tuned-channel voice reception does not establish trunk-following capability.

## New checks

- Extract the entire ZIP. Verify the Bandplans tab can open its working folder, show the four reference datasets, and reload a changed label without a rebuild. Check an invalid frequency produces a visible file error; correct it and reload.
- With live P25 decoding enabled, enable site collection. Check `p25/<WACN>-<system>/site-<RFSS>-<site>/` against the actual control-channel identity. Verify the LCN, downlink and uplink pairs against the P25 messages.
- Leave collection running: repeated grants should not create duplicate rows. Add a label with collection paused, reload, resume, and confirm the label remains.
- Select/hide individual sites. Check overlapping channels retain each site's details. Show uplinks and compare the paired frequency. Turn on calculated grid and confirm those markers are clearly distinct from observed channels.
- Retune to another control channel, including one with the same NAC if available. Confirm unidentified or stale information is not attributed to the new site. Replay an I/Q recording and confirm it does not add live site rows.
- Check green RX with live samples and gray Idle after Stop/disconnect or during replay. On your next authorized manual TX/APRS test, confirm red TX during the actual burst and gray after completion/cancellation/failure. No RF TX was performed in local validation.
- Check spectrum/waterfall layout, zoom, reference hover and the existing Display toggles with the CSV layers.

## Carried forward — 0.31.0-preview1 (partially confirmed)

- Clear DMR voice intelligibility; Slot 1, Slot 2 and Both; independent voice gain.
- Known NXDN48 and NXDN96 voice channels; verify RAN/IDs against known information. Weak-input sync/CRC errors remain unverified evidence.
- Concurrent CW ID on a digital repeater that actually transmits Morse; verify the candidate by ear and check digital audio resumes afterward.
- Retuning, stop/restart, mode changes and I/Q seeks must not carry old speech/identifiers into the next channel. Watch dropped counts during a longer run.
- CTCSS/DCS stays above Spectrum rather than Data; P25 and paging workspaces remain usable.

The complete prior checklist is [ACCEPTANCE-0.31.0-preview1.md](ACCEPTANCE-0.31.0-preview1.md), which also links the older 0.30 checks. Those remain pending unless you already validated them. The APRS TX gain/start inconsistency remains deferred; the indicator does not fix it.
