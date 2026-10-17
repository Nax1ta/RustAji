using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TutorialLibrary;

/// <summary>Small, editable example library created only when no library file exists.</summary>
public static class WelcomeLibrary
{
    public static bool Ensure(DataStore store, AppSettings? settings = null)
    {
        if (File.Exists(Path.Combine(store.Folder, "library.json"))) return false;
        var data = Create(store, settings);
        store.Save(data);
        return true;
    }

    public static LibraryData Create(DataStore store, AppSettings? settings = null)
    {
        var data = new LibraryData { Settings = settings ?? new AppSettings() };
        var start = Folder("从这里开始", null, "建议从右侧的 01 开始。这里的每条资料都可以编辑、移动或删除；它们只是示例。", "#9918AB", "#FAEBFF", 100, 3, null);
        var organize = Folder("整理与查找", null, "用文件夹、标签和搜索整理收藏。右键或拖动，试试看。", "#8962FF", "#E8F9FF", 60, 5, "#E6B5B5");
        var color = Folder("配色灵感", null, "保留了本机已有的几组文件夹配色。右键文件夹，打开“外观与图标颜色”即可查看或改动。", "#28846D", "#D9FDD1", 100, 3, null);
        var nested = Folder("试着移动到这里", organize.Id, "可以把资料拖到这个文件夹，也可以通过右键复制、剪切和粘贴。", "#00BAD2", "#ECECEC", 100, 3, null);
        data.Folders.AddRange([start, organize, color, nested]);

        Add("01 认识腐味", "先看左侧文件夹，再点中间资料，右侧会显示图片、链接和说明。\r\n\r\n腐味保存的是网址及你的笔记，不会把网页或视频下载到电脑。浏览收藏、阅读说明和整理资料可以离线进行；打开网址或获取网页封面时需要联网。\r\n\r\n这些示例都可自由修改或删除。", ["入门", "离线"], "认识界面", "文件夹  →  资料列表  →  详情", "从左到右，先选文件夹，再选资料。", start);
        Add("02 收藏第一个网址", "右键左侧文件夹，选择“新建资料”。也可以右键中间的空白处。\r\n\r\n填写名称和以 https:// 开头的网址；说明里写下为什么收藏、要找什么。保存后双击资料即可用浏览器打开。\r\n\r\n可先把这个示例改成自己的第一个收藏。", ["入门", "网址"], "收藏网址", "名称 + 链接 + 自己的说明", "右键文件夹或列表空白处，选择“新建资料”。", start, "https://example.com");
        Add("03 文件夹与拖动", "左侧可以建多层文件夹。右键文件夹可新建子文件夹或资料。\r\n\r\n把中间的资料拖到左侧另一个文件夹，就能改变位置。文件夹也能拖动，但不能放进自己的子文件夹。\r\n\r\n右键菜单还提供复制、剪切、粘贴和属性。", ["整理", "文件夹"], "层级整理", "文件夹  /  子文件夹  /  资料", "拖放可改变位置；右键可复制、剪切、粘贴。", organize);
        Add("04 标签、筛选和搜索", "右键资料选择“编辑标签”，输入自己的词，例如 画画、教程、待看。\r\n\r\n搜索框可按名称、说明或 #标签查找。中间列表的“类型”和“标签”题头可以筛选；点击多个勾选项后，点菜单外侧即可关闭。\r\n\r\n右键资料查看属性，可以逐行复制。", ["整理", "标签"], "多种找法", "文件夹  +  标签  +  搜索", "用不同维度找到同一条收藏。", organize);
        Add("05 图片与封面", "网址资料保存后，软件会尝试寻找网页封面；这一步需要联网，而且部分网站可能不提供可读取的封面。\r\n\r\n在右侧预览区域点击“更换图片”，可选择本机图片。右键图片可重新获取网页封面。文件夹也可以设置封面。\r\n\r\n图片会复制进当前资料目录；备份时会一起打包。", ["图片", "封面"], "图文预览", "自动寻找  →  手动更换", "使用自己的图片，预览在离线时也能显示。", nested);
        Add("06 备份与换设备", "“备份 → 导出完整备份”会生成一个 .tlb 文件，包含资料、设置和使用中的图片。\r\n\r\n在另一台 Windows 电脑安装腐味后，用“备份 → 导入备份”读取它。导入会替换当前资料库，先为另一台电脑已有内容做备份。\r\n\r\n“设置 → 资料位置”可将本机资料移到别的磁盘。目前桌面程序支持 Windows；macOS 和 Linux 仍在计划中。", ["备份", "换设备"], "带走资料", "导出 .tlb  →  在另一台电脑导入", "先导出备份，再迁移或换设备。", nested);
        return data;

        void Add(string title, string notes, string[] tags, string heading, string diagram, string caption, LibraryFolder location, string url = "")
        {
            data.Items.Add(new StudyItem
            {
                FolderId = location.Id, Title = title, Notes = notes, Url = url, Tags = [.. tags],
                ImageFile = store.SaveImage(DrawCard(heading, diagram, caption))
            });
        }
    }

    static LibraryFolder Folder(string name, Guid? parent, string description, string text, string fill, int opacity, int radius, string? icon) => new()
    {
        Name = name, ParentId = parent, Description = description,
        LabelTextColor = text, LabelFillColor = fill, LabelFillOpacity = opacity,
        LabelCornerRadius = radius, IconColor = icon
    };

    static byte[] DrawCard(string heading, string diagram, string caption)
    {
        using var bitmap = new Bitmap(960, 540);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.Clear(Color.FromArgb(238, 239, 237));
        using var stripe = new SolidBrush(Color.FromArgb(226, 228, 225));
        graphics.FillRectangle(stripe, 0, 0, 960, 22);
        graphics.FillRectangle(stripe, 0, 518, 960, 22);
        using var ink = new SolidBrush(Color.FromArgb(43, 47, 43));
        using var muted = new SolidBrush(Color.FromArgb(91, 98, 91));
        using var titleFont = new Font("Microsoft YaHei UI", 33, FontStyle.Regular, GraphicsUnit.Pixel);
        using var diagramFont = new Font("Microsoft YaHei UI", 27, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bodyFont = new Font("Microsoft YaHei UI", 20, FontStyle.Regular, GraphicsUnit.Pixel);
        graphics.DrawString(heading, titleFont, ink, 66, 68);
        using var card = new SolidBrush(Color.FromArgb(251, 251, 249));
        using var border = new Pen(Color.FromArgb(167, 173, 166), 2);
        graphics.FillRectangle(card, 66, 161, 828, 195);
        graphics.DrawRectangle(border, 66, 161, 828, 195);
        graphics.DrawString(diagram, diagramFont, ink, new RectangleF(98, 225, 770, 85));
        graphics.DrawString(caption, bodyFont, muted, new RectangleF(70, 399, 820, 75));
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}

