# FileShelf.Win

FileShelf.Win is a lightweight Windows file shelf built with C# and WPF.

The app stores file paths in a small always-on-top shelf. It does not copy,
move, delete, modify, upload, or read file contents.

FileShelf focuses on file and folder transfer staging only. Clipboard history is
out of scope.

## Features

- Drag files or folders into the Shelf; files dropped together are kept as one shelf group.
- Add files or a folder manually from the Shelf title bar.
- Stack selected Shelf items into one group from the item context menu or with `Ctrl + G`.
- Split a shelf group from the item context menu when the grouped paths need separate handling.
- Restore recently removed shelf items from the title-bar add menu or with `Ctrl + Z` during the current app session.
- Drag Shelf items out to another app using standard Windows file drag/drop; after a successful drop, unpinned entries are removed from FileShelf only.
- Select all Shelf items with `Ctrl + A` before dragging them out as one batch.
- Drag the item-count handle to drag selected items, or every staged item when nothing is selected.
- Restore staged paths after restart from the portable data folder.
- Show or hide the Shelf from the tray icon.
- Open the Shelf with `Ctrl + Alt + Space`.
- Collapse the Shelf to the tray with `Esc`.
- New portable profiles default to showing the Shelf near the configured screen edge; Settings can switch this to manual, immediate drag, Alt-drag, or Shelf-zone triggering.
- Edge triggers use the screen where the pointer is currently dragging, which keeps multi-monitor use predictable.
- Configure Shelf position and size presets from Settings.
- Suppress automatic Shelf triggers for ignored foreground apps.
- Ignore the app that most recently triggered the Shelf from the Shelf action menu.

## Portability and safety

- No installer is required.
- No registry keys or startup entries are created.
- Runtime state defaults to `FileShelfData` next to the executable.
- Shelf state is stored as path metadata in `FileShelfData\shelf.json`; source files are not touched.
- Removing an item from the Shelf only removes FileShelf's saved path metadata.
- Global hotkey and optional mouse hook are process-owned and released when the process exits.
- Ignored apps are stored as process names in settings and only suppress automatic trigger display.
- Logs default to `FileShelfData\logs\fileshelf.log`; the path can be edited in Settings.

## Run

```powershell
dotnet run --project src\FileShelf.Win\FileShelf.Win.csproj
```

## Build

```powershell
dotnet build FileShelf.sln
```

## Publish

```powershell
.\scripts\publish-portable.ps1
```

The portable publish script writes a clean app folder under
`artifacts\FileShelf-portable-win-x64` and excludes runtime `FileShelfData` state.
