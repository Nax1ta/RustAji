# RustAji (腐味)

[简体中文](README.md) · English

A URL alone may not tell you why you saved it. RustAji keeps the link with your notes, tags, and a cover image. You can put courses, videos, and articles into folders and search them later.

![The tutorial inside the app](docs/images/welcome.png)

## Download

Get the latest Windows x64 build from [Releases](https://github.com/Nax1ta/RustAji/releases). Version 1.0.1 is a .NET 10 WinForms app. There are no macOS or Linux builds yet. RustAji is the app's name; the code is not written in Rust.

On first launch, open the “从这里开始” folder and select “01 认识腐味”. The example entries walk through the interface with pictures and notes. You can edit or delete them.

## Using it

Right-click a folder or an empty part of the list to add a bookmark. Add a URL and a note you will understand later. You can nest folders, drag entries between them, attach tags, search, and filter the list. Covers can come from a web page or from an image on your computer.

Your saved entries, notes, and images remain available offline. Opening a URL or fetching a web cover needs an internet connection. There is no account or built-in cloud sync. To move the library to another Windows computer, export a `.tlb` file from the Backup menu and import it there.

## Data location

The default folder is `%LOCALAPPDATA%\RustAji\`. On first run, version 1.0.1 copies data from the older `%LOCALAPPDATA%\腐味\` folder if present; it leaves the old folder in place. A folder named “腐味资料” that an earlier version created is copied to a sibling named `RustAjiLibrary`. Other locations you chose yourself are left alone.

You can change the location under Settings. Export a `.tlb` backup before moving or importing a library.

## Build and license

On Windows with the .NET 10 SDK:

```powershell
dotnet run --project Verification/Verification.csproj -c Release
dotnet publish TutorialLibrary/TutorialLibrary.csproj -c Release -o publish
```

The executable is `publish/RustAji.exe`. The verification program uses temporary data inside the repository, not your personal library.

Source code is licensed under [GNU GPL v2](LICENSE). See [ASSETS.md](ASSETS.md) for the avatar artwork, [CONTRIBUTING.md](CONTRIBUTING.md) for contributions, and the [roadmap](docs/ROADMAP.md) for possible next steps.
