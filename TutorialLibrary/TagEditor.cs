namespace TutorialLibrary;

public sealed class TagEditor : Form
{
    readonly FlowLayoutPanel chips = new();
    readonly TextBox input = new();
    readonly List<string> tags;

    public List<string> Result => [.. tags];

    public TagEditor(AppSettings settings, IEnumerable<string> existing)
    {
        tags = existing.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        Text = "编辑标签";
        Width = 500;
        Height = 340;
        MinimumSize = new Size(400, 280);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Font = new Font(settings.FontFamily, Math.Clamp(settings.FontSize, 8, 24)); }
        catch { Font = new Font("Microsoft YaHei UI", 9); }
        try { BackColor = ColorTranslator.FromHtml(settings.WindowColor); }
        catch { BackColor = SystemColors.Control; }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(16, 12, 16, 10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "点击标签右侧的 × 可删除", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        chips.Dock = DockStyle.Fill;
        chips.WrapContents = true;
        chips.AutoScroll = true;
        chips.BorderStyle = BorderStyle.FixedSingle;
        chips.Padding = new Padding(5);
        layout.Controls.Add(chips, 0, 1);

        var entry = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        input.Dock = DockStyle.Fill;
        input.MaxLength = 32;
        input.PlaceholderText = "输入标签，按回车添加";
        input.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { AddTag(); e.SuppressKeyPress = true; } };
        entry.Controls.Add(input, 0, 0);
        var add = new Button { Text = "添加", Dock = DockStyle.Fill };
        add.Click += (_, _) => AddTag();
        entry.Controls.Add(add, 1, 0);
        layout.Controls.Add(entry, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var save = new Button { Text = "保存", Width = 78, Height = 29 };
        var cancel = new Button { Text = "取消", Width = 78, Height = 29, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) =>
        {
            if (input.Text.Trim().Length > 0 && !AddTag()) return;
            DialogResult = DialogResult.OK;
            Close();
        };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 3);
        CancelButton = cancel;
        DrawTags();
    }

    bool AddTag()
    {
        string tag = input.Text.Trim();
        if (tag.Length == 0) return true;
        if (tag.IndexOfAny(['\r', '\n', ',', '，']) >= 0)
        {
            MessageBox.Show(this, "一个标签只能写一个词组，不要包含逗号或换行。", "标签格式", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        if (tags.Any(x => string.Equals(x, tag, StringComparison.CurrentCultureIgnoreCase)))
        {
            input.Clear();
            return true;
        }
        if (tags.Count >= 20)
        {
            MessageBox.Show(this, "每条内容最多添加 20 个标签。", "标签数量", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        tags.Add(tag);
        input.Clear();
        DrawTags();
        return true;
    }

    void DrawTags()
    {
        chips.SuspendLayout();
        foreach (Control control in chips.Controls.Cast<Control>().ToArray())
        {
            chips.Controls.Remove(control);
            control.Dispose();
        }
        foreach (string tag in tags)
        {
            var chip = new Button { Text = tag + "  ×", AutoSize = true, Height = 27, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            chip.FlatAppearance.BorderColor = Color.FromArgb(170, 170, 170);
            chip.BackColor = Color.FromArgb(239, 239, 239);
            chip.Click += (_, _) => { tags.Remove(tag); DrawTags(); };
            chips.Controls.Add(chip);
        }
        chips.ResumeLayout();
    }
}
