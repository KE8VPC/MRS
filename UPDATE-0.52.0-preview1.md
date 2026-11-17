# M.R.S. 0.52.0-preview1

- Aviation → Messages / Inspector shows readable ACARS report descriptions, flight information, and supported departure, arrival and gate times. The Label column shows the received ACARS label. The inspector and JSON export retain the original text and worker data.
- The Aircraft / map view uses worker-decoded ACARS coordinates and recognizes the observed decimal-degree layouts in labels 16 and 30. Map markers identify ACARS, with tracks scoped separately from ADS-B.
- H1/DF reports identify the acquisition system and expand the recognized gear, flap and slat terms without interpreting them as a recorded fault or aircraft state.
- Broadcast → HDTV keeps the decoder status and packet counts visible. Expand Diagnostics for the startup and worker log.
