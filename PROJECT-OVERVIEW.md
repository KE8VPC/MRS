# M.R.S. project overview

Updated November 1, 2026. This describes product direction; it does not claim that every feature below is implemented.

## Purpose

M.R.S. is a radio listening, decoding and signal-discovery workbench for radio enthusiasts. It should be enjoyable both for listeners monitoring familiar services and for people exploring unknown signals. Help users move from “what did I just hear?” to a useful starting point for listening, decoding, recording and learning, without first needing to research every frequency outside the application.

## Frequency references as a core feature

Provide an approachable, offline-usable library of common frequencies, allocated channels and service bands alongside the spectrum/waterfall. Include HF aviation voice, VOLMET, VHF ACARS and VDL2, WWV/time-standard stations, ISM bands and published channel plans across supported services. Expand coverage in sourced, manageable datasets rather than implying the reference is already exhaustive. Keep worldwide, national, regional and local applicability clear.

A reference should explain what a listener might encounter: service/channel name, station or network where known, frequency or band limits, region, usual mode, channel spacing/bandwidth where applicable, and a short plain-language description. Provide suggested listening/decoder setup, whether a suitable decoder exists in M.R.S., useful Help links and source/review information. Reference entries should be easy to find from the spectrum and available for optional addition to saved-frequency folders or scan lists. Avoid filling the display with every reference at once; use selectable layers and compact labels with details on demand.

Distinguish exact channel centers, allocated/shared bands, published station assignments and observed/decoded identities. A signal inside an ISM band could belong to many different devices or protocols. Matching a frequency provides a lead, not proof of a transmitter or protocol. Show candidates, unknowns, conflicts and stale observations plainly; use actual decoder evidence when available to strengthen identification. Do not hide useful tentative observations solely because identification is incomplete.

## Exploration workflow

Listen or browse the spectrum, see relevant channel/service references, inspect likely modes and settings, then try a supported decoder or record a sample for later analysis. Let users label findings and organize their own lists while retaining original frequency, source and evidence. Keep unanswered questions visible so users can return to them as decoder coverage grows. Provide concise explanations that encourage further exploration, with deeper searchable Help available when wanted.

## Transmission safeguards — planned requirement

Keep reception and discovery available independently of transmission. TX should default to disabled and require explicit operator authorization setup and deliberate session arming. Initially limit enabled functions to supported, reviewed amateur operations. Record jurisdiction, callsign and license class, distinguishing self-declared information from independently checked records. A callsign match alone does not establish the current operator's identity or authority for a particular transmission.

Use a dedicated TX permissions policy separate from editable receive bandplans and frequency lists. Check actual frequency, occupied bandwidth and band edges, emission type, license privileges, jurisdiction and applicable equipment/power restrictions. Gain is not a calibrated power measurement. Missing or unsupported authorization/rule coverage must leave TX blocked. An ISM reference or license-free service does not automatically permit arbitrary SDR transmission.

Apply one shared authorization guard to APRS, manual tests and all RF/helper start paths; disabling a button is insufficient. Imported frequencies, tuning, scanning and saved profiles must never grant TX permission. Include a visible TX state, immediate Stop, maximum duration and disarming on startup or device/profile changes. A dummy-load/test setting must not bypass the guard. Validate blocked paths without emitting RF.

This is a queued design requirement. The current preview has not been modified to implement it. Review applicable rules during implementation, including U.S. Part 97 station authorization, band/emission privileges and Part 15 equipment requirements. Implementation and packaging wait for an explicit build instruction.

## Data and delivery

Use editable CSV reference datasets and preserve source provenance, review dates, regional scope and user edits. Clean installs may include common public/national/international channel references, but must not include the developer's localized monitoring history, private captures, system maps or station labels. Local station packs can be optional imports. Reference coverage and decoder support are separate: a channel may be identified and useful for listening even before its data format can be decoded.

Existing implementation, queued work and research must remain distinguishable. Synthetic/replay/package checks do not establish live reception. This overview update authorizes documentation/planning only; implementation and release packaging wait for an explicit build instruction.
