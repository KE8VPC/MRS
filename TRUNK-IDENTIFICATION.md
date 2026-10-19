# DMR / Capacity Plus / LTR identification — 0.24.0-preview2

Open DMR / LTR and enable Decode DMR / Cap+ and LTR. Select NFM on the receiver, normally with a 12.5–16 kHz filter. Both decoders consume flat discriminator data before listening-audio filtering. Listening volume does not control these decoders.

## Included

- Existing standard DMR full-link-control identifiers.
- Capacity Plus manufacturer full-link-control group/private call identifiers, radio ID and rest logical slot number.
- CRC-checked Capacity Plus CSBK channel-status rest-slot fields and adjacent-site observations. Multi-block activity lists are preserved as raw bursts, not reassembled into assignments in this preview.
- Short-link-control recovery from four consecutive received CACH fragments: Connect Plus 12-bit network and 8-bit site, Tier III model/network/site, and Motorola rest-channel/site signaling. Acquisition currently uses base-station data-sync bursts; voice-only intervals may not supply all fragments. Gaps discard incomplete assembly.
- Tier III ALOHA network/site fields. NET and SITE are the raw wire values; numbering conventions and location-area subdivisions may differ in other software.
- Experimental LTR Standard outbound (repeater) 300-symbol/s signaling with sync, CRC-7 and field-range checks. Displays area/home/group, channel, available channel, idle and call-end observations. Inbound subscriber messages, LTR-Net, Passport and MultiNet are not supported.

## Identity and persistence

Rows are scoped by protocol, frequency, identity, receiver source and live/replay status. Equal color codes never merge separate frequencies. Known system types and observed network/site values are retained in bounded sets; changing values on one frequency remain visible rather than silently replacing them.

Unknown network/site values remain explicit. A Motorola short rest message is described as compatible signaling, not conclusive Capacity Plus classification: fuller CSBK/call evidence can identify Capacity Plus. Neighbor site numbers never become the current site. LTR area and a DMR color code are not globally unique network identifiers. LTR group 255 idle observations are not subscriber talkgroups.

The main table requires three matching acceptable observations within ten seconds for DMR or sixty seconds for LTR. Isolated color codes, failed payload checks, and heavily corrected slot types cannot confirm a row. Optional live auto-save needs three matching confirmed updates and preserves names/notes. Evidence is appended to the existing frequency metadata; replay never creates live discoveries. Use Frequencies to assign your own labels. This preview does not create a reliable automatic cross-frequency system association or global radio-ID database.

Data receives confirmed raw messages and structured JSON for inspection/export. Group DMR / LTR repeats keeps repeated observations together. A short-LC raw field is the corrected 36-bit message padded with four trailing zero bits; a normal DMR raw field is the received sliced 264-bit burst. LTR supplies the sliced 40-bit word.

No digital voice decoder or automatic trunk following is added. Short-LC reception, weak-signal LTR and local system classification require live acceptance; generated tests are not proof of local RF performance.

## Protocol references and test provenance

The implementation uses independently written C# parsing and modem code. Wire-field definitions, parity/checksum constants and expected layouts were checked against:

- ETSI TS 102 361-1/4, DMR burst, product-code, short LC, CSBK and trunking formats.
- https://github.com/DSheirer/sdrtrunk — CRCLTR, LTRStandardMessageProcessor, SyncPattern, CACH, CapacityPlusRestChannel and ConnectPlusControlChannel (public source inspected 2026-10-18).
- https://github.com/lwvmobile/dsd-fme — dmr_csbk.c and dmr_flco.c for manufacturer CSBK/call layouts. Where short-LC field widths differed, current sdrtrunk layouts were used; live acceptance remains required.
- https://github.com/g4klx/MMDVMHost — test-only BPTC19696, DMRSlotType, RS129 and DMRShortLC encoders generated independent encoded burst/product-code vectors. No upstream encoder executable is bundled. Existing DMR test provenance remains in DMR-TEST-PROVENANCE.md.

LTR modem tests are synthetic and cover polarity, DC offset, noise, arbitrary block boundaries, repeated words and checksum damage. Independent captured LTR acceptance is still pending. No user recordings or private decoded messages are included in the release.

## LTR cadence correction

The supplied 39.407-second live recording produced four CRC-valid LTR Standard idle words at approximately 5.441, 16.478, 27.515 and 38.552 seconds: area 0, channel/home 4, group 255, free channel 4. The earlier ten-second confirmation window expired between words. Preview2 uses sixty seconds for LTR and shows pending counts. Confirmed LTR evidence remains active while matching words continue within sixty seconds. DMR timing remains unchanged. This validates this captured idle signaling, not all LTR voice/routing cases.
