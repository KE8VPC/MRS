# Paging setup and readable FLEX messages
Context: Paging
Keywords: POCSAG FLEX escaped newline line break checksum reassembled fragment detail 512 1200 2400

Tune the paging signal in NFM and use a receive filter appropriate to its occupied bandwidth. For a narrow POCSAG512 signal, a 6 kHz filter previously improved reception in user testing; it is not the correct width for every pager channel or FLEX mode. Start paging decoding and check protocol, validation, fragment status and dropped counters. Audio-input paging needs flat discriminator-like audio rather than heavily filtered/de-emphasized speaker sound.

POCSAG BCH results are not a message CRC. FLEX can report K checksum, signature and fragment sequence status. Complete and reassembled are message states; their presence alone does not mean every address/content field is reliable. Keep uncertain observations identifiable, especially on weak/noisy signals.

## Literal backslash-n in older previews
The bundled FLEX_NEXT add_ch routine converts LF, CR and TAB to printable backslash sequences before JSON serialization. Readable alphanumeric FLEX detail now expands only those supported sequences. Table summaries stay compact. Raw Information/DecoderJson and exports preserve original worker text. Unknown escapes are not generically decoded, and POCSAG/numeric messages are not rewritten. Literal transmitted backslash sequences can be ambiguous; inspect the raw worker fields if interpretation matters.

Newest on top uses last receipt, so an updated grouped message moves appropriately. Clear view hides retained rows without resetting decoder state or deleting paging rules/recordings. Reset message history is a different action. Select a row for validation, recipients and raw fields. Avoid sharing personal message contents when reporting bugs; a redacted example with the same formatting is sufficient.
