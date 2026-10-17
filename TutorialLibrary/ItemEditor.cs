namespace TutorialLibrary;

public sealed class ItemEditor : Form
{
    readonly ComboBox folderChoice = new();
    readonly TextBox titleBox = new();
    readonly TextBox urlBox = new();
    readonly TextBox notesBox = new();
    readonly Label imageLabel = new();
    readonly CheckBox autoCover = new();
    readonly StudyItem? original;
    public StudyItem? Result { get; private set; }
    public string? ChosenImagePath { get; private set; }
    public bool AutoFetchCover => autoCover.Checked;

    public ItemEditor(LibraryData data, StudyItem? item, Guid? initialFolderId = null)
    {
        original = item;
        Text = item is null ? "添加新资料" : "修改资料";
        Width = 650;
        Height = 700;
        MinimumSize = new Size(520, 550);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font(data.Settings.FontFamily, Math.Max(8, data.Settings.FontSize));
        BackColor = ColorTranslator.FromHtml(data.Settings.WindowColor);

        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 51));
        Controls.Add(shell);
        var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 14, 18, 8) };
        shell.Controls.Add(panel, 0, 0);
        var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, Padding = new Padding(0), Margin = new Padding(0) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(content);

        folderChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        folderChoice.Items.Add(new FolderOption(null, "顶层"));
        foreach (var folder in data.Folders.OrderBy(x => FolderPath(data, x), StringComparer.CurrentCultureIgnoreCase))
            folderChoice.Items.Add(new FolderOption(folder.Id, FolderPath(data, folder)));
        Guid? chosenFolder = item?.FolderId ?? initialFolderId;
        folderChoice.SelectedIndex = folderChoice.Items.Cast<FolderOption>().ToList().FindIndex(x => x.Id == chosenFolder);
        if (folderChoice.SelectedIndex < 0) folderChoice.SelectedIndex = 0;
        AddField(content, "所在文件夹", folderChoice, 57);

        titleBox.MaxLength = 150;
        titleBox.Text = item?.Title ?? "";
        AddField(content, "资料名称 *", titleBox, 57);

        urlBox.Text = item?.Url ?? "";
        AddField(content, "参考网页或视频链接", urlBox, 57);

        notesBox.Multiline = true;
        notesBox.ScrollBars = ScrollBars.Vertical;
        notesBox.Height = 105;
        notesBox.Text = item?.Notes ?? "";
        AddField(content, "学习要点、关键步骤或易错点", notesBox, 132);

        var imageRow = new FlowLayoutPanel { Height = 36, WrapContents = false, AutoSize = false };
        var chooseImage = new Button { Text = "选择本机图片…", AutoSize = true, Height = 29 };
        chooseImage.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "图片|*.png;*.jpg;*.jpeg;*.gif;*.bmp|所有文件|*.*", Title = "选择资料配图" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ChosenImagePath = dialog.FileName;
                imageLabel.Text = Path.GetFileName(dialog.FileName);
            }
        };
        imageLabel.AutoSize = true;
        imageLabel.Margin = new Padding(10, 7, 0, 0);
        imageLabel.Text = item?.ImageFile is null ? "未选择图片" : "已保留当前图片";
        imageRow.Controls.Add(chooseImage);
        imageRow.Controls.Add(imageLabel);
        AddField(content, "配图", imageRow, 63);

        autoCover.Text = "如未选图片，保存后自动尝试获取网页封面";
        autoCover.Checked = item is null;
        autoCover.AutoSize = true;
        autoCover.Dock = DockStyle.Top;
        content.Controls.Add(autoCover);
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Height = 40, WrapContents = false, Padding = new Padding(12, 9, 12, 6) };
        var save = new Button { Text = "保存资料", Width = 92, Height = 29 };
        var cancel = new Button { Text = "取消", Width = 72, Height = 29, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => Save();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        shell.Controls.Add(buttons, 0, 1);
        AcceptButton = save;
        CancelButton = cancel;
    }

    static void AddField(TableLayoutPanel table, string caption, Control control, int height)
    {
        var wrapper = new Panel { Dock = DockStyle.Fill, Height = height };
        var label = new Label { Text = caption, Top = 0, Left = 0, Height = 23, AutoSize = true, TextAlign = ContentAlignment.BottomLeft };
        control.Dock = DockStyle.None;
        control.Top = 26;
        control.Left = 0;
        control.Width = Math.Max(100, table.Width - 20);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        wrapper.Controls.Add(control);
        wrapper.Controls.Add(label);
        table.Controls.Add(wrapper);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        wrapper.SizeChanged += (_, _) => control.Width = Math.Max(100, wrapper.ClientSize.Width);
    }

    void Save()
    {
        string title = titleBox.Text.Trim(), url = urlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(this, "请填写资料名称。", "内容不完整", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (url.Length > 0 && !DataStore.IsWebUrl(url))
        {
            MessageBox.Show(this, "网址应以 http:// 或 https:// 开头。", "网址无效", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Result = new StudyItem
        {
            Id = original?.Id ?? Guid.NewGuid(),
            FolderId = (folderChoice.SelectedItem as FolderOption)?.Id,
            Categories = original?.Categories ?? [],
            Title = title,
            Url = url,
            Notes = notesBox.Text.Trim(),
            Tags = original?.Tags is null ? [] : [.. original.Tags],
            ImageFile = original?.ImageFile,
            CreatedAt = original?.CreatedAt ?? DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now
        };
        DialogResult = DialogResult.OK;
        Close();
    }

    static string FolderPath(LibraryData data, LibraryFolder folder)
    {
        var parts = new List<string>();
        LibraryFolder? current = folder;
        while (current is not null)
        {
            parts.Add(current.Name);
            current = current.ParentId is Guid id ? data.Folders.FirstOrDefault(x => x.Id == id) : null;
        }
        parts.Reverse();
        return string.Join(" / ", parts);
    }

    sealed record FolderOption(Guid? Id, string Label)
    {
        public override string ToString() => Label;
    }
}
