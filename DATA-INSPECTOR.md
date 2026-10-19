# Data inspector — 0.20.0-preview2

Select an observation in Data and click **Inspect selected**. This opens a separate, resizable window containing a frozen snapshot. Reception continues; later rows or repeated-message updates do not replace the snapshot. Opening another inspection replaces the previous inspector for that receiver.

- **Decoded fields / bits:** source, frequency, time, approximate replay position, labels, recipient set, validation and decoder fields. Select a field with a bit range to highlight its bits. Offsets are zero-based, MSB first, relative to the byte representation described at the top—not RF waveform sample positions.
- **Bytes:** hex and printable ASCII for bytes actually supplied by the decoder.
- **Message:** full decoded text and information.
- **Snapshot JSON:** the observation captured when you opened the inspector, including decoder JSON and available structured P25 metadata.
- **Export snapshot JSON:** saves that frozen observation to your chosen file. Grouped rows include their count and recipient set, with the latest individual decoded fields.

## What the data represents

P25 NID: the retained eight bytes are received NID bits after status-symbol removal, before BCH correction. The inspector distinguishes received NAC/DUID bit fields from BCH-decoded NAC/DUID and correction count. Header validity does not validate an undecoded payload.

P25 TSBK: the retained twelve bytes are after deinterleaving/trellis decoding, with CRC validation performed by the receiver pipeline. Basic header, opcode, manufacturer and CRC ranges are mapped. Standard unprotected group-voice-grant fields are also mapped. Protected or manufacturer-specific payloads are not interpreted as clear grants. Other structured P25 properties are shown as decoded/resolved metadata without claiming bit positions.

POCSAG/FLEX: the worker supplies decoded message fields and validation information. It does not supply received codewords, correction locations or waveform samples to this observation. The inspector therefore has no raw-bit or waveform view for paging. Decoder timestamps remain processing time; approximate replay positions are not exact frame-start links.

The packaged preview2 inspector does not implement frame comparisons; the next source increment below adds them. Waveform/symbol timing, seeking to exact frame starts, complete protocol field maps, and a persistent message archive remain future work. It is an inspection view, not an additional decoder or a new validation guarantee.

## Next source increment: compare with a pinned reference

Choose **Pin as reference** in an inspector, then inspect another Data row in the same receiver and open **Compare**. The reference remains frozen in memory even when the inspector is closed. **Clear reference** removes it; closing the receiver discards it. Receiver A and B keep independent references.

Reference/current fields and full message text are compared by field identity, including added/removed fields. **Only changed fields** hides identical values. Metadata such as timestamps and reception counts may change independently of the payload. Compatible mapped frame types of equal byte length/provenance also list differing bit positions. These differences are not an error rate or a correction count. Paging and incompatible frame types retain field/text comparison without inventing bit comparisons. No disk persistence is introduced.

