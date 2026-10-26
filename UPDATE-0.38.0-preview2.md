# M.R.S. 0.38.0-preview2 - optional confirmations

Adds selectable evidence filtering for faint/noisy signal searches without changing existing defaults.

DMR / LTR / NXDN > channel identification has a saved discovery selector:
- Confirmed observations only: existing repeated-evidence behavior.
- Include tentative observations: show the first qualifying observation.
- Include noisy / CRC-failed: also show candidates the confirmation filter rejected. These stay explicitly UNVERIFIED, even if repeated. Raw decoder payload/validation is available in Analysis.

Trunk following has Require repeat / map confirmation, saved per profile. Uncheck it while stopped to try single CRC-valid Capacity Plus group/rest observations with an entered unconfirmed carrier map. Candidate maps remain unconfirmed; tentative follows are labeled in history. Missing maps are still unresolved. Hardware-retune permission remains separate.

Existing profiles and discovery settings retain their prior confirmation defaults. CRC-failed evidence is displayed for investigation; it does not become a validated control grant, saved channel identity or an authorization to bypass voice identity/encryption checks. Automatic frequency saving still requires repeated qualifying evidence.

Includes all 0.38-preview1 features and all pending 0.37 acceptance. Read ACCEPTANCE-0.38.0-preview2.md. Extract the complete ZIP into a fresh folder; keep all helper folders alongside MRS.exe.
