using System.Diagnostics;

namespace TutorialLibrary;

public sealed class AllItemsForm : Form
{
    readonly LibraryData data;
    readonly ListView list = new();
    readonly TextBox search = new();
    readonly Label count = new();

    public AllItemsForm(LibraryData data)
    {
        this.data = data;
        Text = "查看所有资料";
        Width = 1180; Height = 670; MinimumSize = new Size(760, 440);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        try { Font = new Font(data.Settings.FontFamily, Math.Clamp(data.Settings.FontSize, 8, 24)); }
        catch { Font = new Font("Microsoft YaHei UI", 9); }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(layout);
        search.PlaceholderText = "搜索名称、位置、链接或标签";
        search.Width = 300;
        search.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        search.TextChanged += (_, _) => FillRows();
        layout.Controls.Add(search, 0, 0);

        list.Dock = DockStyle.Fill;
        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        list.MultiSelect = false;
        list.Columns.Add("名称", 225);
        list.Columns.Add("位置", 230);
        list.Columns.Add("链接", 330);
        list.Columns.Add("更新时间", 145);
        list.Columns.Add("标签", 190);
        list.DoubleClick += (_, _) => OpenSelectedLink();
        var context = new ContextMenuStrip();
        context.Items.Add("打开链接", null, (_, _) => OpenSelectedLink());
        context.Items.Add("复制名称", null, (_, _) => CopyColumn(0));
        context.Items.Add("复制位置", null, (_, _) => CopyColumn(1));
        context.Items.Add("复制链接", null, (_, _) => CopyColumn(2));
        context.Items.Add("复制整行", null, (_, _) =>
        {
            if (list.SelectedItems.Count == 1)
                Clipboard.SetText(string.Join("\t", list.SelectedItems[0].SubItems.Cast<ListViewItem.ListViewSubItem>().Select(x => x.Text)));
        });
        ChromeStyle.Apply(context, data.Settings, Font);
        list.ContextMenuStrip = context;
        list.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            var row = list.GetItemAt(e.X, e.Y);
            if (row is not null) { row.Selected = true; row.Focused = true; }
            foreach (ToolStripItem action in context.Items) action.Enabled = row is not null;
            context.Items[0].Enabled = row?.Tag is StudyItem item && !string.IsNullOrWhiteSpace(item.Url);
        };
        layout.Controls.Add(list, 0, 1);
        count.TextAlign = ContentAlignment.MiddleLeft;
        count.Dock = DockStyle.Fill;
        layout.Controls.Add(count, 0, 2);
        FillRows();
    }

    void FillRows()
    {
        var folders = data.Folders.ToDictionary(x => x.Id);
        string FolderPath(Guid? id)
        {
            var parts = new Stack<string>();
            var seen = new HashSet<Guid>();
            while (id is Guid value && seen.Add(value) && folders.TryGetValue(value, out var folder))
            {
                parts.Push(folder.Name);
                id = folder.ParentId;
            }
            return parts.Count == 0 ? "顶层" : string.Join(" / ", parts);
        }

        string query = search.Text.Trim();
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var item in data.Items.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            string path = FolderPath(item.FolderId);
            string tags = string.Join("、", item.Tags);
            if (query.Length > 0 && !new[] { item.Title, path, item.Url ?? "", tags }.Any(x => x.Contains(query, StringComparison.CurrentCultureIgnoreCase))) continue;
            string date = (item.UpdatedAt == default ? item.CreatedAt : item.UpdatedAt).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            list.Items.Add(new ListViewItem([item.Title, path, item.Url ?? "", date, tags]) { Tag = item });
        }
        list.EndUpdate();
        count.Text = $"显示 {list.Items.Count} 条资料，共 {data.Items.Count} 条。双击资料可打开链接。";
    }

    void CopyColumn(int column)
    {
        if (list.SelectedItems.Count == 1) Clipboard.SetText(list.SelectedItems[0].SubItems[column].Text);
    }

    void OpenSelectedLink()
    {
        if (list.SelectedItems.Count != 1 || list.SelectedItems[0].Tag is not StudyItem item || string.IsNullOrWhiteSpace(item.Url)) return;
        try { Process.Start(new ProcessStartInfo(item.Url) { UseShellExecute = true }); }
        catch (Exception error) { MessageBox.Show(this, error.Message, "无法打开链接", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }
}
