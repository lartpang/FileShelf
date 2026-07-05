# Changelog

## v0.3.0 - Interface Polish and Multi-Select

### Added

- Added synchronized Chinese/English language updates for the shelf panel and tray menu.
- Added multi-select shelf actions so selected entries can be dragged out or removed together.

### Changed

- Reworked the shelf panel to use one fixed default size based on the current screen height, with runtime-only border resizing and no persisted size presets.
- Restyled the shelf, Settings, and About windows around the current black-and-white line logo.
- Replaced the floating entry with the actual FileShelf logo instead of a colored icon container.
- Made the shelf add action explicit through the `+` button and removed the duplicated empty-panel right-click add menu.
- Tightened Settings layout while keeping enough bottom space for action buttons.
- Updated the GitHub Actions release workflow to publish with the Release configuration.

### Removed

- Removed shelf size preset/custom settings and related persisted configuration.
- Removed the old collapse button because the shelf already collapses on focus loss.
- Removed old logo assets and kept only the logo currently used by the application.

### Fixed

- Fixed tray and floating icon logo loading by copying the active icon resource into Release output.
- Fixed resize hit testing so panel resizing is limited to the visible panel boundary.
- Fixed visual drift from the old blue theme in the shelf, Settings, and About surfaces.

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
- Reworked the Settings window to focus on actual configuration: language, data path, and log path.
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
