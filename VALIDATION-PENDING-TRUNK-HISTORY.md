# Pending dedicated trunk voice history fix

Source-only changes continued November 4, 2026. Version remains 0.45.0-preview1; no new release ZIP, installation, Git commit/push or live RF/device access in this pass.

## Behavior

- External assignments emit an Assigned event with TG/frequency and set the model to assigned-voice status, without a second model Start.
- Assignment requested, acknowledged Tuned, first Decoded audio, assignment failure and Released events are mirrored into control/voice histories and their Session logs. Metadata includes receiver/control names, trunk-profile TG label and available source radio/NAC. Tune acknowledgement is not represented as voice reception; decoded audio delivery is not speaker audibility acceptance.
- Current-call header remains visible outside the Trunk following tab. The control header lists actual voice assignments instead of describing a received grant as a currently followed local call.
- Newest-first bounded 500-row history; the existing Clear view control remains view-only; CSV export includes the new metadata. The setup area scrolls within 300 device-independent pixels, retaining a visible history region.
- Ended/hung voice calls release their voice receiver for further assignments instead of waiting for the maximum lease timeout. Manual stop still pauses eligibility; failures remain paused for review.

## Local checks

- Development compilation: PASS, no errors. Existing NU1900 warnings report the unavailable NuGet vulnerability metadata feed; this is not a dependency audit result.
- Local regression suite: PASS, 1,815 assertions (including 5 dedicated external-assignment/model history assertions).
- Synthetic receiver UI/dispatch check: PASS. Exercises the real BeginFleetVoice path with synthetic receiver workers, control capture unchanged, independent workers excluded, dedicated TG/frequency/receiver records on both sides, Session log records, current-call header, visible history space, manual stop/resume, call termination/release, failed unsupported tune without false Tuned event, bounded newest-first retention and visible timestamp ordering.
- Voice-window screenshot rendered and visually inspected: work/trunk-history-final/ui-dedicated-voice-history.png, synthetic TG/frequency only. Does not prove live RF or audio acceptance.
- Final synthetic receiver UI/dispatch rerun: PASS after ordering asynchronously delivered records by their original event timestamps; visible newest-first ordering was checked as well as bounded storage.
- Scoped Git whitespace check: PASS.

Hardware acceptance remains open: install the next explicitly requested package, verify real calls produce matching TG/frequency and receiver records, confirm idle/end transitions and continued conversations on the user's system. Prior user report of audible voice from the second SDR remains separate live evidence.
