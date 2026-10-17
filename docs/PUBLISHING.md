# GitHub 发布资料

仓库名：`RustAji`。建议描述：`Local bookmark manager for Windows. Folders, tags, notes, covers, and offline backups.`

建议话题：`bookmarks`, `offline-first`, `windows`, `winforms`, `dotnet`, `open-source`, `knowledge-management`。

公开仓库：`https://github.com/Nax1ta/RustAji`。后续提交可在本目录推送：

```powershell
git push origin main
```

`v1.0.1` Release 已发布。发布正文保存在 `docs/RELEASE_1.0.1.md`，附件包括 `RustAji-1.0.1-windows-x64.exe`、`RustAji-1.0.1-source.zip` 和 `RustAji-1.0.1-SHA256.txt`。后续版本应使用对应版本的源码和校验文件。

从 1.1.0 开始，Windows 发布文件是安装包。先在 Windows 上安装 NSIS 3，再运行 `./build-installer.ps1`。发布正文见 `docs/RELEASE_1.1.0.md`。安装包与源码包应对应同一个 Git 标签；发布前核对 SHA-256。

发布前检查仓库公开内容：不要上传 `archive/`、`outputs/` 中的旧版本、个人 `library.json`、`.tlb` 备份或本机日志。当前项目的原始头像插画不在 GNU GPL v2 代码许可范围内，详见 `ASSETS.md`。
