namespace TutorialLibrary;

public sealed class FolderEditor : Form
{
    readonly TextBox nameBox = new();
    readonly TextBox descriptionBox = new();

    public string FolderName { get; private set; } = "";
    public string Description { get; private set; } = "";

    public FolderEditor(AppSettings settings, LibraryFolder? folder)
    {
        Text = folder is null ? "新建文件夹" : "编辑文件夹";
        Width = 520;
        Height = 390;
        MinimumSize = new Size(420, 300);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Font = new Font(settings.FontFamily, Math.Clamp(settings.FontSize, 8, 24)); }
        catch { Font = new Font("Microsoft YaHei UI", 9); }
        try { BackColor = ColorTranslator.FromHtml(settings.WindowColor); }
        catch { BackColor = SystemColors.Control; }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(18, 14, 18, 10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "文件夹名称", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        nameBox.Dock = DockStyle.Fill;
        nameBox.MaxLength = 100;
        nameBox.Text = folder?.Name ?? "新建文件夹";
        layout.Controls.Add(nameBox, 0, 1);
        layout.Controls.Add(new Label { Text = "说明", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        descriptionBox.Dock = DockStyle.Fill;
        descriptionBox.Multiline = true;
        descriptionBox.AcceptsReturn = true;
        descriptionBox.ScrollBars = ScrollBars.Vertical;
        descriptionBox.Text = folder?.Description ?? "";
        layout.Controls.Add(descriptionBox, 0, 3);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var save = new Button { Text = "保存", Width = 78, Height = 29 };
        var cancel = new Button { Text = "取消", Width = 78, Height = 29, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => Save();
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 4);
        AcceptButton = save;
        CancelButton = cancel;
        Shown += (_, _) => { nameBox.Focus(); if (folder is null) nameBox.SelectAll(); };
    }

    void Save()
    {
        string name = nameBox.Text.Trim();
        if (name.Length == 0 || name.IndexOfAny(['/', '\\']) >= 0)
        {
            MessageBox.Show(this, "名称不能为空，也不能包含斜杠。", "名称无效", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        FolderName = name;
        Description = descriptionBox.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }
}
