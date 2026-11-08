# Meshtastic / MeshCore direct SDR reception
Context: Mesh
Keywords: Mesh Meshtastic MeshCore LoRa SDR raw IQ LongFast spreading factor SF bandwidth coding rate public encrypted

Open Analysis → Meshtastic / MeshCore. Start an SDR or MRS I/Q replay first. This uses raw I/Q and local receive-only helpers; a USB mesh node, internet gateway and GNU Radio installation are not required. Speaker audio/WAV cannot recover LoRa.

Enter the exact RF frequency and select the actual spreading factor, bandwidth, coding rate and sync word. One profile is decoded per receiver. Meshtastic US LongFast is supplied as a starting profile: 906.875 MHz, SF11, 250000 Hz bandwidth, CR +4 = 1 (4/5), sync decimal 43 (0x2B). Other regions, presets or channel slots differ. MeshCore is a separate protocol; selecting it sets SF8, 62500 Hz, 4/8 and sync decimal 18 (0x12), then clears the frequency so you enter your local network's settings. These are editable starting values, not automatic identification.

Start mesh reception does not tune hardware. Tune the receiver/capture close enough that the entire channel fits its usable I/Q bandwidth. Use at least twice the LoRa bandwidth as sample rate. A moderate capture rate, such as 1–2.4 MS/s where supported, reduces CPU load. Large offsets near the capture edge are rejected. Supported explicit-header profiles are SF7–12 and 62.5/125/250/500 kHz; this is an experimental receive implementation, not exhaustive support for every LoRa device or mesh configuration.

The view keeps header-only, CRC-failed and decoded frames distinct. Application interpretation requires a matching sync profile and a passed LoRa payload CRC. Raw bytes and helper JSON remain inspectable. Meshtastic shows sender/destination, packet ID, channel hash, remaining hops and relay. Its known default public key (channel hash 0x08) can expose supported text/protobuf ports; private channels and direct messages remain encrypted. A plausible public-key decode is not authenticated. Position/NodeInfo/Telemetry protobuf payloads are currently retained as hex rather than mapped into node history.

MeshCore shows routing/path fields and supported advertisements, names/locations when present, plus known Public-channel text with its short MAC check. Advertisement signatures are not verified; do not treat received coordinates/names as trusted. Unknown/private payloads remain opaque. There is no node configuration, message sending, private-key entry or automatic network bridging in this pass.

Clear, search, order, raw details and export work like the other observation views. Stop mesh reception before changing parameters. Retunes, seeks, missing helpers and overruns are visible and reset acquisition. Keep Decoders/Sensors and Decoders/RadioData with MRS.exe. About lists source/license references. Live over-the-air reception on your mesh networks remains an acceptance test.
