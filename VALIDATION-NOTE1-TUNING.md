# Note 1 — visible Tune feedback and validation ordering

November 22, 2026. Source usability/error-path correction, not a claim that every reported field tuning failure has been reproduced.

LiveTune now reports replay, stopped, awaiting-first-samples and out-of-range refusals through RtlStatus as well as Session log. The combined UI mirrors RtlStatus into the application-wide status bar. Validate these conditions before stopping scanners/trunk research, so rejected requests do not interrupt unrelated work.

TuneSaved likewise validates mode/replay/coverage/filter before stopping scanners and reports refusal in shared status. In-capture SetChannel and hardware Retune share error handling, including reflecting actual receiver controls on same-session failures. Show the pending tuning frequency; ordinary receiver acknowledgement still supplies actual applied status. Saved tuning retains existing automatic receiver-start behavior; manual LiveTune still asks for a running receiver rather than silently starting hardware.

Broadcast staging while stopped/replaying now explicitly says staged/not tuned in shared status and explains Start/Apply. It no longer leaves this distinction only in the log. No modal dialogs or additional confirmation steps.

Receive-modes smoke now checks visible refusal for a stopped receiver and unsupported saved mode without hardware. Evidence: work/tuning-fixes-check/receive-modes-ui.txt (the existing success message still describes receive modes; assertions also cover the tuning guards). Compilation/regression evidence recorded after completion.

Still requires field acceptance of Commercial/FLEX to Broadcast, actual receiver tuning acknowledgement, scanning/trunk ownership transitions and post-overflow receiver state. Shared backlog fix is separate note 17. No assertion of RF success from status text alone, no new public package.

Final validation: 2265 self-checks and the receive-modes/tuning-guard UI smoke passed in the self-contained development build. Compilation/publish succeeded. Local synthetic tests only; live receiver/workspace acceptance remains open.
