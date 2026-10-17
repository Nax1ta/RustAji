namespace TutorialLibrary;

public sealed class EntryPropertiesForm : Form
{
    AppSettings appearance = new();
    public EntryPropertiesForm(LibraryData data, LibraryFolder folder, DataStore store)
    {
        Initialize(folder.Name, data.Settings);
        var descendants = new HashSet<Guid> { folder.Id };
        bool changed;
        do
        {
            int previous = descendants.Count;
            foreach (var child in data.Folders.Where(x => x.ParentId is Guid id && descendants.Contains(id))) descendants.Add(child.Id);
            changed = descendants.Count != previous;
        } while (changed);
        var dates = ContentView.LatestFolderUpdates(data);
        var rows = new (string, string)[]
        {
            ("名称", folder.Name), ("类型", "文件夹"), ("位置", FolderPath(data, folder.ParentId)),
            ("直接子文件夹", data.Folders.Count(x => x.ParentId == folder.Id).ToString()),
            ("直接资料", data.Items.Count(x => x.FolderId == folder.Id).ToString()),
            ("下级文件夹总数", (descendants.Count - 1).ToString()),
            ("全部资料总数", data.Items.Count(x => x.FolderId is Guid id && descendants.Contains(id)).ToString()),
            ("标签", folder.Tags.Count == 0 ? "无" : string.Join("、", folder.Tags)),
            ("封面", store.ImagePath(folder.ImageFile) ?? "无"),
            ("创建时间", FormatDate(folder.CreatedAt)), ("自身更新时间", FormatDate(folder.UpdatedAt)),
            ("包含内容的最新时间", FormatDate(dates[folder.Id])),
            ("文字颜色", folder.LabelTextColor ?? "跟随主题"),
            ("背景填充", folder.LabelFillColor ?? "无"),
            ("背景不透明度", folder.LabelFillColor is null ? "未使用" : folder.LabelFillOpacity + "%"),
            ("圆角", folder.LabelFillColor is null ? "未使用" : folder.LabelCornerRadius + " 像素"),
            ("图标颜色", folder.IconColor ?? "默认"),
            ("内部编号", folder.Id.ToString())
        };
        BuildTabs(rows, "说明", folder.Description);
    }

    public EntryPropertiesForm(LibraryData data, StudyItem item, DataStore store)
    {
        Initialize(item.Title, data.Settings);
        var rows = new (string, string)[]
        {
            ("名称", item.Title), ("类型", "资料"), ("位置", FolderPath(data, item.FolderId)),
            ("网址", string.IsNullOrWhiteSpace(item.Url) ? "无" : item.Url),
            ("标签", item.Tags.Count == 0 ? "无" : string.Join("、", item.Tags)),
            ("封面", store.ImagePath(item.ImageFile) ?? "无"),
            ("创建时间", FormatDate(item.CreatedAt)), ("更新时间", FormatDate(item.UpdatedAt)),
            ("内部编号", item.Id.ToString())
        };
        BuildTabs(rows, "学习要点", item.Notes);
    }

    void Initialize(string name, AppSettings settings)
    {
        appearance = settings;
        Text = name + " - 属性";
        Width = 620; Height = 570; MinimumSize = new Size(480, 420);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Font = new Font(settings.FontFamily, Math.Clamp(settings.FontSize, 8, 24)); } catch { Font = new Font("Microsoft YaHei UI", 9); }
        try { BackColor = ColorTranslator.FromHtml(settings.WindowColor); } catch { BackColor = SystemColors.Control; }
    }

    void BuildTabs(IEnumerable<(string Name, string Value)> properties, string contentTitle, string content)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(layout);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        layout.Controls.Add(tabs, 0, 0);
        var general = new TabPage("常规");
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
        list.Columns.Add("属性", 145);
        list.Columns.Add("内容", 420);
        foreach (var (name, value) in properties) list.Items.Add(new ListViewItem([name, value]));
        var rowMenu = new ContextMenuStrip();
        rowMenu.Items.Add("复制内容", null, (_, _) => CopySelectedValue());
        rowMenu.Items.Add("复制属性和内容", null, (_, _) =>
        {
            if (list.SelectedItems.Count == 1)
                Clipboard.SetText(list.SelectedItems[0].Text + "：" + list.SelectedItems[0].SubItems[1].Text);
        });
        ChromeStyle.Apply(rowMenu, appearance, Font);
        list.ContextMenuStrip = rowMenu;
        list.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            var row = list.GetItemAt(e.X, e.Y);
            if (row is null) { rowMenu.Items[0].Enabled = false; rowMenu.Items[1].Enabled = false; return; }
            row.Selected = true;
            row.Focused = true;
            rowMenu.Items[0].Enabled = true;
            rowMenu.Items[1].Enabled = true;
        };
        list.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C) { CopySelectedValue(); e.Handled = true; }
        };
        void CopySelectedValue()
        {
            if (list.SelectedItems.Count == 1) Clipboard.SetText(list.SelectedItems[0].SubItems[1].Text);
        }
        list.Resize += (_, _) => list.Columns[1].Width = Math.Max(160, list.ClientSize.Width - list.Columns[0].Width - 4);
        general.Controls.Add(list);
        tabs.TabPages.Add(general);
        var contentPage = new TabPage(contentTitle);
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = content, BorderStyle = BorderStyle.None, BackColor = SystemColors.Window };
        contentPage.Controls.Add(text);
        tabs.TabPages.Add(contentPage);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var close = new Button { Text = "关闭", Width = 82, Height = 29, DialogResult = DialogResult.OK };
        actions.Controls.Add(close);
        layout.Controls.Add(actions, 0, 1);
        AcceptButton = close; CancelButton = close;
    }

    static string FormatDate(DateTimeOffset? value) => value?.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "未记录";

    static string FolderPath(LibraryData data, Guid? folderId)
    {
        var names = new Stack<string>();
        var lookup = data.Folders.ToDictionary(x => x.Id);
        while (folderId is Guid id && lookup.TryGetValue(id, out var folder))
        {
            names.Push(folder.Name);
            folderId = folder.ParentId;
        }
        return names.Count == 0 ? "顶层" : "顶层 / " + string.Join(" / ", names);
    }
}
