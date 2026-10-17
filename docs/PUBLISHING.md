# GitHub 发布资料

建议仓库名：`RustAji`。描述：`Local bookmark organizer for courses, videos and articles. Folders, tags, notes, covers and offline backups. Windows 1.0.`

建议话题：`bookmarks`, `offline-first`, `windows`, `winforms`, `dotnet`, `open-source`, `knowledge-management`。

当前公开仓库为 `https://github.com/Nax1ta/RustAji`。在本目录推送后续提交：

```powershell
git push origin main
```

随后创建 `v1.0.0` Release，正文使用 `docs/RELEASE_1.0.md`，上传 `RustAji-1.0-windows-x64.exe` 和 `SHA256.txt`。源码可由 GitHub 自动生成，也可上传 `RustAji-1.0-source.zip`。

发布前检查仓库公开内容：不要上传 `archive/`、`outputs/` 中的旧版本、个人 `library.json`、`.tlb` 备份或本机日志。当前项目的原始头像插画不在 GNU GPL v2 代码许可范围内，详见 `ASSETS.md`。
