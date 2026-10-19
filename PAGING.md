# POCSAG and FLEX paging — 0.20.0-preview1

Receive-only paging is available in **Data > POCSAG / FLEX**, independently in receiver A and B. The switch is remembered per receiver. Processing is offline after extracting the complete release; no paging decoder download is needed.

## Using it

1. Start the SDR or replay an MRS I/Q recording. Tune to the center of the paging signal and select **Narrow FM**.
2. Start with a **16 kHz** receive filter; compare **20 or 25 kHz** if the signal requires it. A filter that admits more noise can recover fewer messages. Avoid clipping the receiver.
3. Enable **POCSAG / FLEX** in Data. Expand **Decoder status and help** to see worker status, decoded/unvalidated counts, queue drops and FLEX BCH statistics.
4. Select an observation for capcode (To), POCSAG function, rate, FLEX phase/cycle/frame, message text, fragment status and the worker's structured fields. **Export JSON** follows the visible view/filter.
5. Enable **Show unvalidated** to inspect incomplete, failed-checksum, address-only and other unchecked observations. They are hidden by default.

The decoder receives the NFM discriminator before listening volume and audio squelch. Audio input/WAV replay is also supported, but needs flat discriminator/baseband audio; ordinary speaker audio, de-emphasis and filtering can prevent reliable decoding. WFM/AM/SSB modes do not feed the paging SDR path.

## Interpretation

- POCSAG: 512, 1200 and 2400 baud; normal/inverted polarity. Standard function interpretation: function 0 numeric, others alphanumeric. Capcode/function and recovered text are shown. A normal synchronized message is labelled **BCH decoded · no message CRC**; this does not guarantee every character is correct. There is no end-to-end POCSAG message checksum.
- POCSAG address-only/tone observations remain **unvalidated**. Constant/silent input can generate apparently valid zero-data BCH words; these must not be presented as confirmed messages. They remain available for inspection.
- FLEX: automatic rate/level/phase detection by FLEX_NEXT. Complete messages and fragments retain the decoder's explicit status, K checksum and available signature/sequence results. A checksum-passing fragment is not a complete page. Missing initial/final fragments cannot be reconstructed reliably.
- FLEX BCH counts are diagnostic totals for decoded phase blocks, including uncorrectable codewords. The main list excludes diagnostic-only BCH rows to avoid unnecessary scrolling. Other unchecked metadata is inspectable with Show unvalidated.
- Observed UTC is M.R.S. processing time; replay position is approximate and attached to the latest input block delivered to the worker. The native decoder's timestamp is also processing time, not the original transmission timestamp.
- The list is bounded to 500 recent rows. Group paging duplicates combines identical validated complete pages across capcodes on the same frequency and receiver session; incomplete/failed observations remain separate. P25 grouping is independent. This is not yet a long-term paging archive or full bit/frame inspector.

## Capture behavior

Each receiver uses a separate bundled multimon-ng process, with bounded input/output queues and a streaming 48,000-to-22,050 Hz conversion. Capture callbacks do not wait for decoding or pipe writes. Tuning, source changes, replay gaps, disable and Clear reset decoder state. A queue overrun drops the discontinuous stream and reacquires rather than silently joining it. Stop can discard a partially received page. Worker failure is visible; toggle paging after correcting the problem to retry.

## Validation scope

Generated POCSAG fixtures cover all three rates plus inverted numeric reception; generated FLEX covers 1600-symbol/s 2-FSK. Noise-only and trailing-silence cases are checked. The supplied local 42-second FLEX I/Q capture exercises 1600-symbol/s 4-FSK through M.R.S.'s production demodulator, with its original 25 kHz filter: 29 observations, 12 checksum-passing, no queue drops. An earlier separate offline analysis used a narrower 7.5 kHz complex low-pass and recovered more passing fragments; these are different processing paths and results.

Higher-rate FLEX, difficult RF, complete multi-fragment page reassembly and sustained simultaneous hardware operation still need field validation. Your recording and its message contents are not included in the package. Only generated samples are distributed in Docs/Samples.

Worker license, corresponding source and reproducible build instructions are under Decoders/Paging. This preview does not transmit paging signals.


## Paging message management (0.20.0-preview2)

- **Group paging duplicates** keeps identical validated complete/reassembled messages in one fixed row, updating the count and last-seen time. All recipient capcodes are retained in the To column, selected details and JSON `PagingRecipients` array. Different receivers/sessions, frequencies, protocols, payloads and live/replay sources stay separate. Incomplete fragments and failed-checksum rows are not collapsed into confirmed pages.
- **Summary wraps** within its column; resize the column or the existing data/details divider as needed.
- Select a paging row and use **Paging labels / ignore…**. Label the selected capcode(s), or label that exact displayed message. Giving multiple capcodes the same label creates a searchable source group. Labels are operator annotations; capcodes identify recipients and do not prove the transmitting organization.
- **Ignore selected capcode(s)** hides those recipients; other recipients carrying the same payload remain visible. **Ignore this exact message** hides every copy of that displayed payload on this protocol/frequency. Ignoring is a display/export filter, not a decoder or RF block.
- **Show ignored** reveals retained ignored observations. **Manage labels and ignored items…** lets you edit labels, clear Ignore, or remove selected rules. Restored data is limited to the recent history still retained.
- Labels and ignore rules are saved atomically in `paging-rules.json` in each receiver's profile, independently for A/B. Rules are scoped to protocol and exact frequency; audio without RF metadata has its own no-frequency scope. The paging grouping preference is remembered. Changing grouping or ignore settings rebuilds counts from the latest 500 individual observations; this is not a permanent archive. Clear removes observations, not saved rules.
- Export follows the visible filter and ignore settings. Grouped exports preserve the latest decoded fields, aggregate labels, reception count and all capcodes seen. Disable paging grouping for individual receptions.
