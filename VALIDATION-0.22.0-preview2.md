# 0.22.0-preview2 — Data-tab hang correction

Windows recorded the reported incident on October 17, 2026 at 22:16 local time as AppHangB1 / event 1002 (application stopped responding). No matching .NET unhandled-exception record was found. The report does not identify an exact stalled stack.

Changes:
- DMR Data observations group by source, live/replay, frequency, message kind and decoded information. Raw-byte changes update the latest details and change count instead of adding another row.
- Group DMR repeats is independent of P25 and paging grouping, enabled by default. Disable it to see bounded individual history. Group rebuilding uses the latest 500 observations, as with other groups.
- DMR UI refresh is limited to five times per second; the channel grid is refreshed only on new observations.
- Each DMR UI drain is capped at 128 observations, preventing a producer from extending one UI drain indefinitely. Existing bounded queues/drop reporting remain in place.

Validation: 1,030 local self-tests passed, including 10,000 matching DMR observations, repeat counts, bounded raw history, frequency/replay separation and switching grouping off. The WPF smoke suite additionally exercises 10,000 DMR observations with repeated Spectrum/Data switching. This is a synthetic UI stress regression, not proof that the original live hang has been reproduced or that hardware acceptance is complete.

No DSP or protocol decoding changes. Operator retest of the same busy live channel remains required. Carry-forward acceptance checklist: ACCEPTANCE-0.22.0-preview2.md.

Additional UI pressure correction: repeated row property-change notifications are coalesced into one pending dispatcher notification. Selected details refresh at most five times per second during repeated reception; selecting a different row still refreshes immediately. An initial stress run with grouping alone remained too busy and was stopped; the final regression runs include these additional corrections.

Final result: complete WPF UI smoke passed with the 10,000-observation Data-tab flood and tab-switch assertions after notification/detail coalescing. Final self-test count: 1,030 passed.

Final detail-pane refresh uses a 200 ms UI timer while visible so the final observation is displayed even when incoming traffic stops. Final self-tests and UI flood smoke passed with this implementation.
