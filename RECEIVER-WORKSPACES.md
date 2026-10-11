# Receiver A and B — 0.11.0

M.R.S. now supports two independent receiver windows within one application. Open Receiver B from the button at the top of Receiver A. The corresponding button in B opens or brings A forward. Opening a window does not start reception.

Each window has its own source, frequency, sampling mode, sample rate, gain, receive mode/filter, squelch, volume, output device, spectrum/waterfall, recordings and packet history. Place the windows side by side or on different monitors. Select different RTL devices in the two windows, then start each. A window may instead use conventional-radio audio or a recording. Two physical dongles are required for simultaneous independent RTL captures; multiple software VFOs within one capture remain planned.

Device ownership is checked between windows in the same application process. Unique device identities are compared across enumeration changes; when identity is unavailable, the device index is used conservatively. The RTL library also reports native open failures. Other applications or separately launched M.R.S. processes are not coordinated by this window-level guard. Generic or duplicate serials still require manual selection and cannot enable automatic reconnect. Reconnect does not take a receiver owned by the other window. Stop releases that window's device. Updating the shared RTL library requires other live RTL reception to stop first.

Receiver A retains the existing `%LOCALAPPDATA%\MRS` settings, display and frequency library. Receiver B uses `%LOCALAPPDATA%\MRS\ReceiverB`. Preferences are saved on normal window closure. These are two workspace profiles, not automatically created profiles for every physical radio. Frequency libraries are separate to prevent concurrent edits overwriting one another; a shared library with coordinated editing remains future work. Packet export source labels and default recording filenames identify A or B. Do not run multiple application processes against the same profile directories.

Close either window independently; the application exits after its last window closes. Each window finalizes its own recordings. Both can use the same listening endpoint, in which case their audio may overlap; select different outputs or disable monitoring in one window as desired. Do not choose a radio transmit-audio endpoint for ordinary listening if that radio uses VOX.

## Station acceptance

1. Start A on a known signal. Open B, Find, select the other dongle and start a different signal.
2. Change frequency, gain, sample rate, squelch and volume in one window and confirm the other remains unchanged.
3. Make short recordings in both windows. Stop A and confirm B continues; replay the recordings independently.
4. Close and reopen B to verify its settings. Receiver selection is restored after Find only when identity is unique.
5. Attempt to select A's active dongle in B; the app should report ownership instead of disturbing A. Test unplug/reconnect separately once ordinary reception is stable.

Automated checks use isolated profiles and synthetic receiver processes. Physical two-dongle USB throughput, endpoint mixing, sustained recording and unplug/reconnect require hardware testing. This milestone does not yet implement control-channel roles, P25 decoding or automatic voice following.

Upcoming source change: shared frequency storage supersedes the separate frequency-library behavior described for the released 0.11.0 package. See FREQUENCY-MANAGER.md for migration and synchronization details. Receiver preferences remain separate.


## 0.12.0 shared frequency library

Both receivers now use the main frequencies.json. Legacy B entries are preserved and imported once under Imported from Receiver B; the original file remains untouched. Same-process changes refresh the other receiver. Separate running application processes require Reload; revision checks protect against stale saves. Receiver tuning, display and device settings remain independent.

