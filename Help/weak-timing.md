# Weak signal timing: NIST and received-station estimates
Context: Weak signal timing
Keywords: FT8 FT4 WSPR NIST NTP UTC synchronize time sync clock automatic timing DT drift offset

Open Amateur → FT8 / FT4 / WSPR and expand Timing synchronization. These are session options; automatic network refresh and automatic RX adjustment start off. All corrections affect the decoder's scheduling clock. MRS does not set Windows time, change Windows time-service settings or require clock-setting privileges.

Sync decoder to NIST performs an NTP exchange with time.nist.gov on UDP/123 and enables Use NIST timing after success. The display shows the correction to PC time, round-trip delay, estimated network uncertainty, sample age and status. Ordinary public NTP is not authenticated and network asymmetry affects accuracy. This is a measured reference, not a guarantee of a precise hardware timestamp. DNS/UDP failures, invalid replies and cooldowns remain visible. Positive correction means adding seconds to the PC clock used for decoder scheduling.

Auto NIST refresh checks about every 15–16 minutes while this application is running. Receiver windows in the same process share a reference and a minimum 64-second request interval. A reference older than one hour becomes STALE and is excluded. A failed refresh retains a still-fresh prior reference with its age/status shown. It does not claim a new synchronization. Turn off Use NIST timing to use PC timing with manual/RX corrections only.

Live reception uses the selected reference and tracks the incoming audio timeline. Large clock discontinuities discard incomplete acquisition. Accumulated stream-clock phase drift triggers reacquisition; this does not calibrate RF frequency or the SDR sample oscillator. I/Q replay uses its recorded start/position, and WAV uses elapsed time. Live NIST correction is excluded from both replay sources.

Calculate RX offset displays a relative estimate based on CRC-valid FT8/FT4 decodes. The native helper reports an approximate DT after accounting for mode start conventions and software analysis/filter delay. Radio/input buffering, propagation and transmitting stations' clock errors remain part of the measurement. Raw window offset and raw decoder JSON remain inspectable. WSPR has no message CRC and does not supply automatic station timing evidence.

The estimator weights stations equally, counts a station only once per slot, expires evidence after five minutes and rejects station outliers. Ready status requires at least three identifiable callsigns, six qualifying decodes across two slots and a spread no greater than 0.20 seconds. A single station, unresolved hashed message or CRC-failed result cannot authorize automatic adjustment. The result is labeled station consensus, not verified UTC.

Apply RX estimate applies a ready estimate as a separate RX trim. Auto RX timing moves toward a ready estimate by at most 0.10 second per minute, with a total RX trim bounded to ±2 seconds. Manual offset remains available and is applied when Start weak-signal decode is pressed. A timing change discards incomplete acquisition and waits for a fresh complete slot, so a slot can be skipped during adjustment. Reset timing clears manual/RX adjustments and disables NIST use/auto options.

FT8/FT4 windows include one second of overlap before and after the nominal slot, with a wider candidate timing search. This can recover transmissions crossing ordinary slot boundaries, but cannot reconstruct data missing from a recording. Decoding normally waits about one second beyond the slot end. EOF may pad only the margin with silence when the full central slot is available. Very large timing errors still require a good UTC reference or manual correction before usable station timing exists.

References:
https://www.nist.gov/pml/time-and-frequency-division/time-distribution/internet-time-service-its
https://tf.nist.gov/tf-cgi/servers.cgi
https://wsjtx.github.io/wsjtx/guide-full.html
https://github.com/WSJTX/wsjtx/blob/master/lib/ft4_decode.f90
https://github.com/WSJTX/wsjtx/blob/master/lib/ft8/ft8b.f90

NIST/FT timing does not make independent SDRs phase coherent and does not provide RF arrival-time synchronization for triangulation/TDOA.
