# 0.12.0 station acceptance

Extract the compact ZIP into a new folder and open MRS.exe. Confirm the header says 0.12.0. Existing settings remain in Windows application data. Back up %LOCALAPPDATA%\MRS before testing.

1. Open Receiver B. Check that your saved frequency folders appear in both windows. Save a temporary test entry in A and confirm B updates; edit it in B and confirm A updates. Remove the test entry afterward. If an older B library existed, confirm its entries appear under Imported from Receiver B.
2. In Display, enable the U.S. band reference bar and service channel markers. View two meters, CB or a local weather channel. Confirm the bar follows tuning, zoom and pan. Hover to read frequencies and sources.
3. Enable ARRL recommendations. Check an HF band if your input supports it, plus two meters and 70 centimeters. Recommendations and calling markers should appear. Unsupported receiver coverage is not unlocked by the overlay.
4. Switch Reference bar position between Above spectrum and Below spectrum. Confirm labels remain readable and spectrum/waterfall clicks still tune to the indicated frequency. Narrow blocks reveal their complete text on hover.
5. Change display theme, close and reopen. Confirm overlay choices and position remain saved independently for A and B.
6. Select WFM on a strong broadcast station. Confirm the broadcast heading, stereo status and L/R labels are readable.
7. With two distinct dongles selected, briefly verify independent reception, audio and stopping in A/B. These remain separate receivers; this release does not merge their bandwidth.

Report a failed step with the receiver, frequency, selected mode and screenshot if useful. Automated package checks cover synthetic software paths; station reception remains a user acceptance gate.
