# RustAji (腐味)

[简体中文](README.md) · English

RustAji is a local bookmark manager for Windows. Entries can be organized in folders and include tags, notes, and cover images. The app supports search, filtering, sorting, and drag and drop. It is built with .NET 10 WinForms.

![App interface and built-in tutorial entries](docs/images/welcome.png)

## Download

[Releases](https://github.com/Nax1ta/RustAji/releases) provides a Windows x64 installer. Setup offers separate choices for the program folder and the data folder. Reinstalling keeps an existing library location. The installer creates a Start menu shortcut and an uninstall entry. Earlier single-file builds remain available from older releases. macOS and Linux builds are not available.

The first launch creates editable example entries covering folders, tags, covers, and backups. A [written guide](docs/GETTING_STARTED.md) is also available in Chinese.

## Usage

Folders can be nested. Entries contain a name, URL, notes, tags, and an image. They can be created from the context menu and moved between folders by dragging. The list supports type and tag filters; search covers names, notes, and tags.

Entries, notes, and images are stored locally and remain available offline. Opening a URL or fetching a cover from the web requires a network connection. The app has no account or built-in cloud sync. A complete backup uses a `.tlb` file and can be imported on another Windows computer; importing replaces the destination library.

## File location

If the data folder is not changed during setup, it defaults to `%LOCALAPPDATA%\RustAji\`. The location setting is stored under `%APPDATA%\RustAji\` and can later be changed in Settings. Uninstalling removes program files and shortcuts, but leaves the library in place.

When upgrading from an older version, 1.0.1 copies data from `%LOCALAPPDATA%\腐味\` to the new folder. An older automatically created folder named “腐味资料” is copied to a sibling named `RustAjiLibrary`. Original folders remain in place; other manually selected locations are unchanged. The Backup menu can export a `.tlb` file before moving or importing a library.

## Build and license

Build requirements: Windows and the .NET 10 SDK. From the repository root:

```powershell
dotnet run --project Verification/Verification.csproj -c Release
./build-installer.ps1
```

The script builds `publish/RustAji.exe` and then packages it with NSIS 3. NSIS must be installed separately. The verification program uses temporary data inside the repository.

Source code is licensed under [GNU GPL v2](LICENSE). The avatar artwork is described in [ASSETS.md](ASSETS.md). Development guidelines are in [CONTRIBUTING.md](CONTRIBUTING.md), and planned work is in the [roadmap](docs/ROADMAP.md).
