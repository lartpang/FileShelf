# Changelog

## v0.5.0 - Shelf Reliability and Recovery

FileShelf v0.5.0 tightens the shelf workflow around applied settings, safer drag-out cleanup, missing-path recovery, and in-shelf ordering.

### Added

- Added applied Settings behavior: edits are staged until the user clicks Apply.
- Added a Restore Original Defaults action in Settings.
- Added Settings path validation before data and log paths are accepted.
- Added an explicit data-path note that the current shelf state is saved to the selected data folder when applied.
- Added single-instance activation so launching FileShelf again opens the running shelf.
- Added drag reordering inside the shelf list.
- Added visible shelf notices for add/skip results, reordering, removal, and missing-path updates.
- Added staged-version checks so file and folder paths can be compared against their original staged state before drag-out.
- Added a shelf check button for detecting changed or missing staged items.
- Added floating drag hints for staging, drag-out, and in-shelf reordering.
- Added a panel pin control so the shelf can stay open when focus changes.
- Added missing-path actions to remove missing paths or relocate a missing file/folder.
- Added confirmation for bulk selected removal and clearing unpinned items.

### Changed

- Changed duplicate adds to report how many paths were added and how many were skipped.
- Changed drag-out cleanup to remove only paths that were actually sent; missing paths in partially valid groups are kept.
- Changed drag-out to block external file drops when staged paths changed or went missing.
- Changed folder version fingerprints to skip recursive traversal into reparse-point directories such as symbolic links, junctions, and mount points while still tracking the link entry itself.
- Made shelf rows more compact with simplified file/folder/group markers.
- Reset logging after the log path changes so a previous write failure does not permanently disable logging.

### Fixed

- Fixed selected shelf rows so all inline action buttons remain visible against the selected dark background.
- Fixed shelf selection clearing so clicking empty panel/list space clears the current selection without requiring Ctrl-click.
- Fixed the Settings Apply button hover state so it keeps the same dark-button interaction logic instead of fading into the background.
- Fixed icon-mode drag prompts so the floating drag hint no longer gets clipped by the 64px icon window or overlaps the icon tooltip.

### Removed

- Removed obsolete trigger and dock settings fields that no longer represented active UI behavior.

## v0.4.0 - Startup, Localization, and Release Metadata

FileShelf v0.4.0 adds opt-in start-with-Windows support, moves UI text into resource-based localization, and compiles version/release metadata into the app so portable releases no longer need a visible sidecar metadata file.

### Added

- Added an opt-in start-with-Windows setting backed by a single removable current-user Startup shortcut.

### Changed

- Moved UI localization text into English and Chinese `.resx` resource files.
- Moved app version and release metadata from `FileShelf.app.json` into compile-time assembly metadata.
- Updated local and GitHub release publishing to stamp version and repository metadata into the assembly.

### Removed

- Removed sidecar `FileShelf.app.json` generation from portable publish output.
- Removed the obsolete `FileShelf.app.example.json` sidecar metadata example.

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
