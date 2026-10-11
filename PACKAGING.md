# Compact Windows distribution

0.18.0-preview1 retains the same root layout. `Decoders/ATSC` contains worker source, GPL license, pinned package lock and dependency notes. The large TV runtime downloads through the HDTV setup button into the Windows user's local application-data directory. It is not bundled in MRS.exe or the ZIP. Source packages include `decoders` so rebuilding retains the TV setup assets.

0.17.0-preview1 adds one support folder, Receivers, for the HackRF DLLs and their licenses/source archives. The root contains MRS.exe, README.txt, Docs, Licenses, Decoders and Receivers. Keep both support folders with the executable. Application publishing copies `native/HackRF` into `Receivers/HackRF`; the source ZIP includes that native directory so normal source publishing retains its receiver dependency. The package still uses only the five explicitly allowlisted generated sample recordings; temporary UI fixtures and local RF captures are excluded.

The compact 0.11.0 package contains MRS.exe, README.txt, Docs and Licenses. Application libraries and the .NET runtime are bundled into the executable; native runtime components are extracted automatically into the Windows user temporary area on launch. No separate .NET installation is required. This reduces visible files, not necessarily download size.

Extract the entire archive into a new folder and run MRS.exe. Existing settings remain under %LOCALAPPDATA%\MRS, with Receiver B under ReceiverB. RTL-SDR support retains its existing library location under that application-data folder. Do not remove the old installation until the replacement works on your station.

The original 0.11.0 ZIP is retained. This is a packaging revision with the same application version and receiver features. Source publishing now defaults to a single bundled executable, with trimming disabled for WPF compatibility.

Automated self-tests and UI checks cover software behavior. Physical radio reception and actual audio devices still require station testing.

0.12.0 retains the compact four-item root layout and includes the spectrum-reference milestone. The 0.11.0 packages remain available. Receiver settings remain separate; frequency libraries are shared in 0.12.0.


0.14.0-preview1 retains MRS.exe, README.txt, Docs, Licenses and Decoders at the root. The synthetic P25 test recording is under Docs/Samples. Keep Decoders beside MRS.exe for HD Radio; it contains the existing native backend, licenses and corresponding source archives.

0.15.0-preview1 retains that five-item root layout. Docs/Samples contains generated CTCSS WAV, DCS I/Q and P25 I/Q fixtures. The native decoder payload uses an explicit file allowlist plus its licenses and source archives; local user recordings are excluded. An old local 90.7 MHz recording was found alongside the development decoder and had been copied into the previous 0.14.0-preview1 package. It is not part of the new package. Existing ZIPs and original recordings are left unchanged.
