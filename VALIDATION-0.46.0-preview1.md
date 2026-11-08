# Validation — 0.46.0-preview1

November 4, 2026. Self-contained Windows x64 preview.

## Published executable

Published with the bundled .NET 10 SDK, single executable with separate decoder/receiver dependencies. Zero compilation errors; existing NU1900 warnings indicate the NuGet vulnerability metadata feed was unavailable. This is not a dependency security audit.

The published executable passed the five-receiver pool integration (including selective Allow/Hold release, cached-grant retirement and mixed tuning coverage), three mapped DMR following profiles, six generated-I/Q P25 voice/log cases, and Fox Hunt/Aviation offline-detail UI checks. Rendered Fox Hunt and dedicated P25 screens were inspected.

DMR scenarios use constructed decoded grants, independent full-LC identity fixtures with supplied slot/CC metadata and synthetic receiver tuning; they do not prove live AMBE following. P25 voice scenarios feed generated I/Q through the production downconverter, voice decoder, identity/encryption gates and audio/log pipeline; control grants and receiver workers are synthetic. Fox estimator routes are synthetic and map choices are isolated diagnostic preferences. Offline Aviation rendering checks assert zero tile network requests and local map-choice restore/removal.

Development details and pre-fix reproductions are retained in VALIDATION-PENDING-TRUNK-HISTORY.md, VALIDATION-PENDING-TRUNK-AUDIO.md, VALIDATION-PENDING-DMR-FOLLOWING.md, VALIDATION-PENDING-FLEET-LIFECYCLE.md, VALIDATION-PENDING-FOX-TOOLS.md, VALIDATION-PENDING-OFFLINE-MAPS.md and VALIDATION-PENDING-INTEGRATION.md. Their earlier no-release statements describe source-only work before this authorized build.

## Acceptance limits

No live SDR/GPS operation, RF transmission or live reception reporting was performed for this release. Software tests do not verify USB reliability, actual speaker output, field GPS accuracy, real-system DMR/P25 following or calibrated localization accuracy. Follow ACCEPTANCE-0.46.0-preview1.md for installed/station checks. Rough Fox source areas assume a stationary source and sufficiently comparable measurements; ambiguity/insufficient coverage must not be presented as a measured bearing or guaranteed location.

## Results and packaging

The final published executable passed **1,927 assertions**, zero FAIL lines, successful exit. All four targeted UI/integration checks completed successfully: receiver fleet, DMR following, P25 generated-I/Q voice/log and Fox Hunt/shared offline detail. Scoped whitespace checks passed. The generated self-test report ships under Docs/self-test-results.txt.

The package is assembled from an explicit runtime/dependency layout with only public bandplan references and allowlisted generated samples. Optional regional GeoJSON remains separate. Archive CRC, staged/extracted file hashes, final executable/source identity, helper hashes and operational-data exclusion checks are recorded in the adjacent MRS-0.46.0-preview1-PACKAGE-CHECKS.txt after packaging. Extracted-package launch results are recorded there separately.
