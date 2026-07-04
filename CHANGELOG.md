# Changelog

## v0.2.0 - Floating Icon Interaction

### Added

- Added an always-on-top floating folder icon as the default FileShelf entry point.
- Added double-click-to-open behavior for the floating icon and focus-loss collapse back to the icon.
- Added drag repositioning for the floating icon with screen bounds clamping and DPI-aware coordinate handling.
- Added file drop support directly on the floating icon, including a release-to-stage visual hint.
- Added external build metadata through `FileShelf.app.json`.
- Added About-window version display from external metadata with assembly fallback.
- Added one-shot GitHub Releases update checking with network failure caching.
- Added `FileShelf.app.example.json` documenting the app metadata schema.
- Added `-Repository` support to `scripts/publish-portable.ps1` so release builds can generate GitHub update URLs.
- Added automatic repository wiring in the GitHub release workflow for `lartpang/FileShelf`.
- Added path browsing buttons in Settings for the data folder and log file path.

### Changed

- Reworked the app from configurable global drag triggers to icon-based interaction.
- Reworked the Settings window to focus on actual configuration: language, shelf size, data path, and log path.
- Restyled Settings combo boxes, buttons, and path fields to match the FileShelf visual style.
- Made Settings path fields wrap across multiple lines instead of truncating long paths.
- Tightened Settings window height and removed excess empty space.
- Made Settings and About windows single-instance within the running app.
- Updated README documentation for the icon workflow, release metadata, and GitHub release automation.

### Removed

- Removed global hotkey registration.
- Removed global mouse hook trigger modes.
- Removed trigger mode, ignored app, and hotkey controls from Settings.
- Removed the Settings-window "Clear Unpinned" action because it is a shelf content operation, not configuration.

### Fixed

- Fixed panel positioning so the shelf opens inside the current screen work area.
- Fixed floating icon click handling so normal left/right clicks do not move or hide the icon.
- Fixed duplicate About windows from repeated tray menu activation.
- Fixed About version text showing assembly defaults instead of release metadata.
