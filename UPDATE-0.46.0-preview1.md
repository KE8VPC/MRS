# M.R.S. 0.46.0-preview1

Windows x64 portable preview — trunk following, Fox Hunt and offline map improvements.

Extract the entire ZIP into a new folder and launch MRS.exe. Keep Help, Decoders, Receivers and bandplans with the executable. Existing user settings remain in the Windows local application-data folder. Do not copy an old executable over this release or discard your existing installation before testing.

## Changes

- P25 dedicated voice: active TG/frequency header across tabs; separate assignment, acknowledged tune, first decoded audio, failure, end and release history mirrored to control and voice receivers; hexadecimal NAC and CSV metadata.
- DMR Capacity Plus, Connect Plus and Tier III: protocol/slot/color-code attribution, optional per-carrier color codes in saved profiles and CSV, matching full-LC/terminator identity checks. A color code alone does not identify a system or supply its carrier map.
- Dedicated voice workspaces show assignment status, history and pause/resume/skip controls; configuration stays on the control receiver.
- Receiver pool: call-end/max expiry retires cached grants before dispatch; Allow/Ignore/Hold changes release excluded active calls; an unavailable channel no longer blocks later supported grants.
- Fox Hunt has its own primary tab, smoothed strength trends, experimental opt-in rough source-area modeling with coverage/quality/ambiguity checks, and fuller RF/GPS context in GPX exports. Increasing strength is not a measured bearing or proof of getting closer.
- Fox Hunt and Aviation can load bounded local WGS84 GeoJSON roads, places and boundary outlines, retain independent map choices and show provenance/coverage. Missing files leave the built-in base map usable.

## Offline detail

Use Load offline detail in Fox Hunt or Aviation to select a supported regional GeoJSON file. Maps are optional and stay separate from the base install; no private local system, receiver definition, transmitter coordinate or route is preloaded. Aviation detail import switches online street tiles off. The optional public Auglaize County road pack is a separate download. This release does not implement MBTiles or automatic regional map downloads.

## Testing

See Docs/VALIDATION-0.46.0-preview1.md and Docs/ACCEPTANCE-0.46.0-preview1.md. Software/synthetic tests do not establish live RF, USB reliability, speaker audibility, moving GPS accuracy or calibrated transmitter-location accuracy. DMR clear AMBE following, real-system rest handoffs and Fox Hunt field estimates require station testing. No RF transmission, live device operation or reception report was performed to produce this release. Existing transmit safeguards remain in place.
