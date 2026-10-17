# RustAji（腐味）

[English](README_EN.md) · 简体中文

只存网址，过后可能想不起当时为什么收藏。RustAji 让你在链接旁边写说明、加标签、放封面，再用文件夹把它们整理起来。课程、视频和文章都可以这样收。

![软件内的入门资料](docs/images/welcome.png)

## 下载

在 [Releases](https://github.com/Nax1ta/RustAji/releases) 下载最新版 Windows x64 程序。1.0.1 使用 .NET 10 WinForms；目前没有 macOS 或 Linux 版本。RustAji 是软件名称，代码并非用 Rust 编写。

第一次打开会出现几条示例资料。点左边的“从这里开始”，再点“01 认识腐味”，右侧就能看到配图和操作说明。示例可以直接修改，也可以删掉。[这里有一份文字版教程](docs/GETTING_STARTED.md)。

## 平时怎么用

右键文件夹或列表空白处新建资料，填上网址和自己的说明。文件夹可以继续套文件夹，资料可以拖到别处。标签、搜索框和列表题头的筛选适合用来找回以前存过的内容。封面可以从网页获取，也可以换成本机图片。

资料保存在本机。已有的记录、说明和图片离线也能看；打开网址、自动寻找网页封面需要联网。软件没有账户和云同步。换电脑时，可在“备份”菜单导出 `.tlb` 文件，再在另一台 Windows 电脑导入。

## 资料放在哪里

默认目录是 `%LOCALAPPDATA%\RustAji\`。1.0.1 第一次启动时，会把旧版 `%LOCALAPPDATA%\腐味\` 中的资料复制过来，旧目录不会自动删除。以前若由软件创建了名为“腐味资料”的目录，也会复制到同一父目录下的 `RustAjiLibrary`。自己选择的其他资料位置保持原样。

“设置 → 资料位置”可以继续更改目录。挪动或导入资料前，建议先从“备份”菜单导出一份 `.tlb` 文件。

## 构建与许可

在 Windows 上安装 .NET 10 SDK，然后在仓库根目录运行：

```powershell
dotnet run --project Verification/Verification.csproj -c Release
dotnet publish TutorialLibrary/TutorialLibrary.csproj -c Release -o publish
```

程序生成在 `publish/RustAji.exe`。验证程序使用仓库内的临时资料，不会读取你的收藏。

源代码采用 [GNU GPL v2](LICENSE)。头像插画的使用范围见 [ASSETS.md](ASSETS.md)。发现问题或想参与开发，可以看 [CONTRIBUTING.md](CONTRIBUTING.md)；后续计划写在 [路线图](docs/ROADMAP.md)。
