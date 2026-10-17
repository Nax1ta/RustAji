# RustAji · 腐味

**A local home for links, videos, and the notes that make them worth keeping.**

RustAji is an open-source desktop bookmark organizer for courses, art references, articles, and long-running projects. Arrange links in folders, add tags and your own notes, save cover images, and find things again with search and filters.

> **Platform status:** Version 1.0 runs on Windows x64 and is built with .NET 10 WinForms. macOS and Linux builds do not exist yet. RustAji is a name, not a claim that this app is written in Rust.

## Get started

Download `RustAji-1.0-windows-x64.exe` from GitHub Releases. On a fresh installation, the app creates an editable illustrated tutorial. Open the “从这里开始” folder and select “01 认识腐味”.

![First-run tutorial](docs/images/welcome.png)

The tutorial covers saving a link, moving items between folders, tags, cover images, and backups. You can edit or delete every example.

## What works offline

Your records, notes, saved covers, folders, tags, and search are local and work without an account. Opening external links or fetching website cover images requires internet access. This release has no built-in cloud sync or browser extension.

Data is stored in `%LOCALAPPDATA%\腐味\` by default; you can change the location in Settings. Export a `.tlb` backup to move the complete library, including used images and settings, to another Windows computer.

## Build

On Windows with the .NET 10 SDK:

```powershell
dotnet run --project Verification/Verification.csproj -c Release
dotnet publish TutorialLibrary/TutorialLibrary.csproj -c Release -o publish
```

The executable will be `publish/RustAji.exe`. The verification program uses temporary data under the repository and does not read your personal library.

Read the [Chinese getting started guide](docs/GETTING_STARTED.md), [roadmap](docs/ROADMAP.md), and [contribution guide](CONTRIBUTING.md). Source code is [MIT licensed](LICENSE). The original avatar artwork has separate rights; see [ASSETS.md](ASSETS.md).
