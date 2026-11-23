# Note 8 — Aviation target isolation

November 22, 2026. Source fix in active outputs/MRS. Previously PacketControls subscribed every receiver ChannelChanged event to Aviation.Follow, which overwrote both decoder target and editable target even when another workspace initiated the retune. That included receiver settings changes, not merely explicit Aviation actions.

Receiver channel changes now notify Aviation.Boundary: reset acquisition while preserving mode/target and explicit stopped state. Pipeline coverage checks still reject an out-of-capture target. The editable target stays separate from actual receiver frequency. An explicit Aviation Tune updates an already enabled pipeline to the entered target; Start uses the entered target; mode selection restores the appropriate default without tuning hardware. The obsolete panel Follow method that asynchronously overwrote the field is removed. Existing pipeline Follow remains for deliberate target changes and compatibility tests.

New UI-control tests exercise VDL2 defaults through repeated boundaries, preserved manual targets on explicit Tune, and ADS-B default after a mode change. Full validation evidence is work/tuning-fixes-check/self-test.txt. Shared receiver event wiring reviewed directly. This does not add per-mode persistent custom preset storage or change other workspace tuning policies.

Field acceptance: switch from unrelated frequencies/settings back to Aviation and confirm intended target remains; mode selection alone must not retune. Check active reception, stopped state, replay boundaries and deliberate Aviation Tune. No hardware operated, no new public package.

Final validation: 2265 self-checks and the receive-modes/tuning-guard UI smoke passed in the self-contained development build. Compilation/publish succeeded. Local synthetic tests only; live receiver/workspace acceptance remains open.
