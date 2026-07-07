# FileShelf.Win

FileShelf.Win is a lightweight Windows file shelf built with C# and WPF.

The app stores file paths in a small always-on-top shelf. It does not copy,
move, delete, modify, upload, or read file contents.

FileShelf focuses on file and folder transfer staging only. Clipboard history is
out of scope.

## Features

- Drag files or folders into the Shelf; files dropped together are kept as one shelf group.
- Add files or a folder manually from the Shelf title bar.
- Stack selected Shelf items into one group from the item context menu.
- Split a shelf group from the item context menu when the grouped paths need separate handling.
- Restore recently removed shelf items from the title-bar add menu during the current app session.
- Drag Shelf items out to another app using standard Windows file drag/drop; after a successful drop, unpinned entries are removed from FileShelf only.
- Drag the item-count handle to drag selected items, or every staged item when nothing is selected.
- Restore staged paths after restart from the portable data folder.
- The app starts as an always-on-top floating folder icon at the right-center screen edge.
- Double-click the floating folder icon to open the Shelf panel; focus change collapses it back to the icon.
- Drag the floating folder icon to reposition it.
- Drop files onto the floating folder icon to stage them; moving away cancels the interaction.
- Show or collapse the Shelf panel from the tray icon.
- Configure language, startup behavior, and data path from Settings.

## Portability and safety

- No installer is required.
- No registry keys are created. Optional startup uses one current-user Startup shortcut and removes it when disabled.
- Runtime state defaults to `FileShelfData` next to the executable.
- Shelf state is stored as path metadata in `FileShelfData\shelf.json`; source files are not touched.
- Removing an item from the Shelf only removes FileShelf's saved path metadata.
- FileShelf does not register global hotkeys or mouse hooks.
- About version text and GitHub Release update checks use metadata compiled into the assembly.

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
When `-Version` is omitted, it uses the project version from `Directory.Build.props`.
Pass `-Version 0.5.0 -Repository lartpang/FileShelf` to stamp a specific About version and enable update checks from GitHub Releases.
