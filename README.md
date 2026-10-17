# RustAji（腐味）

[English](README_EN.md) · 简体中文

RustAji 是一款 Windows 本地网址收藏夹。资料可按文件夹整理，并附上标签、说明和封面；支持搜索、筛选、排序和拖动。项目使用 .NET 10 WinForms 开发。

![软件界面与内置入门资料](docs/images/welcome.png)

## 下载

[Releases](https://github.com/Nax1ta/RustAji/releases) 提供 Windows x64 可执行文件。目前没有 macOS 或 Linux 版本。

首次启动会建立一组可编辑的示例资料，介绍文件夹、标签、封面和备份功能。另有[文字版使用说明](docs/GETTING_STARTED.md)。

## 使用方法

文件夹支持多层嵌套。资料包含名称、网址、说明、标签和图片，可通过右键菜单建立，也可在文件夹之间拖动。列表提供类型与标签筛选；搜索支持名称、说明和标签。

资料、说明和图片保存在本机，可离线查看。打开网址及自动获取网页封面需要网络连接。程序没有账户或内置云同步。完整备份使用 `.tlb` 文件，可在另一台 Windows 电脑上导入；导入会替换目标资料库。

## 文件存放位置

默认资料目录为 `%LOCALAPPDATA%\RustAji\`，位置配置保存在 `%APPDATA%\RustAji\`。“设置 → 资料位置”可更改资料目录。

从旧版升级到 1.0.1 时，程序会将 `%LOCALAPPDATA%\腐味\` 中的资料复制到新目录。旧版自动建立的“腐味资料”目录会复制到同一父目录下的 `RustAjiLibrary`。原目录保留；手动选择的其他资料目录不变。迁移或导入前可通过“备份”菜单导出 `.tlb` 文件。

## 构建与许可

构建环境为 Windows 和 .NET 10 SDK。在仓库根目录运行：

```powershell
dotnet run --project Verification/Verification.csproj -c Release
dotnet publish TutorialLibrary/TutorialLibrary.csproj -c Release -o publish
```

生成文件为 `publish/RustAji.exe`。验证程序使用仓库内的临时资料。

源代码采用 [GNU GPL v2](LICENSE)。头像插画的使用范围见 [ASSETS.md](ASSETS.md)。开发约定见 [CONTRIBUTING.md](CONTRIBUTING.md)，后续计划见[路线图](docs/ROADMAP.md)。
