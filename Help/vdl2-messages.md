# Readable VDL2 messages

Context: VDL2
Keywords: Aviation VDL2 VDL-M2 XID AVLC CPDLC ACARS addresses destination airport

Aviation → Messages / Inspector shows a readable summary for validated VDL2 messages. From and To show decoded link addresses. The type describes link settings, acknowledgements, a busy receiver, retransmission requests, setup, disconnect and frame-reject messages.

XID messages exchange data-link settings. The summary can show the reported destination airport, a nearest-airport identifier, advertised tuning and supported channel frequencies when those fields are present. An advertised frequency remains a reported link setting.

Decoded CPDLC elements show their text. An element whose arguments were only partly decoded is marked partial. Network fragments show that remaining segments are needed.

The inspector retains the original decoder JSON, information payload and raw frame. Unknown message types retain their type and available payload length. Failed-validation messages retain raw information and cannot create aircraft records.

ACARS carried over VDL2 uses the existing ACARS summaries, message labels and supported position parsing. A link address alone supplies no registration, flight number or geographic position.

The mapping follows the bundled decoder's structured AVLC/XID and ATN/CPDLC fields. Signal Identification Wiki is a general reference for signals, recordings and decoder discovery across M.R.S. protocols. Exact field interpretation is checked against decoder source and protocol references.

https://github.com/szpajder/dumpvdl2
https://www.sigidwiki.com/
