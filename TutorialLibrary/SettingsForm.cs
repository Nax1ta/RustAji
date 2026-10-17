namespace TutorialLibrary;

public sealed class SettingsForm : Form
{
    readonly AppSettings working;
    readonly DataStore store;
    readonly Label fontDisplay = new();
    readonly ComboBox themePreset = new();
    readonly Dictionary<string, Button> colorButtons = [];
    readonly Label wallpaperDisplay = new();
    readonly Label decorationDisplay = new();
    readonly NumericUpDown decorationSize = new();
    readonly CheckBox animateDecoration = new() { Text = "点击装饰时播放轻微弹动", AutoSize = true };
    string? wallpaperPath;
    string? decorationPath;
    bool clearWallpaper;
    bool clearDecoration;
    public AppSettings Result => working;

    public SettingsForm(AppSettings current, DataStore store)
    {
        this.store = store;
        working = new AppSettings
        {
            EntryLevels = current.EntryLevels,
            LevelLabels = [.. current.LevelLabels],
            FontFamily = current.FontFamily,
            FontSize = current.FontSize,
            ThemeName = current.ThemeName,
            WindowColor = current.WindowColor,
            PanelColor = current.PanelColor,
            TextColor = current.TextColor,
            AccentColor = current.AccentColor,
            ButtonColor = current.ButtonColor,
            BorderColor = current.BorderColor,
            WallpaperFile = current.WallpaperFile,
            DecorationFile = current.DecorationFile,
            DecorationSize = current.DecorationSize,
            AnimateDecoration = current.AnimateDecoration
        };
        Text = "设置";
        Width = 600;
        Height = 555;
        MinimumSize = new Size(520, 500);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        Controls.Add(tabs);
        tabs.TabPages.Add(CreateAppearanceTab());
        tabs.TabPages.Add(CreatePicturesTab());
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
        var save = new Button { Text = "保存设置", Width = 90, Height = 28, Anchor = AnchorStyles.Right | AnchorStyles.Top };
        var cancel = new Button { Text = "取消", Width = 72, Height = 28, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Right | AnchorStyles.Top };
        bottom.Controls.Add(save);
        bottom.Controls.Add(cancel);
        bottom.Resize += (_, _) => { cancel.Left = bottom.ClientSize.Width - 82; save.Left = cancel.Left - 98; };
        save.Top = cancel.Top = 9;
        save.Click += (_, _) => Commit();
        Controls.Add(bottom);
        AcceptButton = save;
        CancelButton = cancel;
    }

    TabPage CreateAppearanceTab()
    {
        var page = new TabPage("字体与颜色") { Padding = new Padding(18), AutoScroll = true };
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        themePreset.DropDownStyle = ComboBoxStyle.DropDownList;
        themePreset.Width = 160;
        themePreset.Items.AddRange(["经典灰", "暖纸", "夜间", "自定义"]);
        AddRow(table, "主题预设", themePreset);
        var fontButton = new Button { Text = "选择字体和字号…", AutoSize = true };
        fontDisplay.Text = $"{working.FontFamily}  {working.FontSize:0.#} 磅";
        fontDisplay.AutoSize = true;
        var fontRow = new FlowLayoutPanel { Height = 36, Dock = DockStyle.Fill };
        fontRow.Controls.Add(fontButton);
        fontRow.Controls.Add(fontDisplay);
        fontDisplay.Margin = new Padding(8, 6, 0, 0);
        fontButton.Click += (_, _) =>
        {
            using var dialog = new FontDialog { Font = new Font(working.FontFamily, working.FontSize), ShowEffects = false, MinSize = 8, MaxSize = 24 };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                working.FontFamily = dialog.Font.FontFamily.Name;
                working.FontSize = dialog.Font.Size;
                fontDisplay.Text = $"{working.FontFamily}  {working.FontSize:0.#} 磅";
            }
        };
        AddRow(table, "界面字体", fontRow);
        AddColorRow(table, "窗口底色", nameof(AppSettings.WindowColor));
        AddColorRow(table, "内容底色", nameof(AppSettings.PanelColor));
        AddColorRow(table, "文字颜色", nameof(AppSettings.TextColor));
        AddColorRow(table, "强调颜色", nameof(AppSettings.AccentColor));
        AddColorRow(table, "按钮颜色", nameof(AppSettings.ButtonColor));
        AddColorRow(table, "边框颜色", nameof(AppSettings.BorderColor));
        themePreset.SelectedItem = themePreset.Items.Contains(working.ThemeName) ? working.ThemeName : "自定义";
        themePreset.SelectedIndexChanged += (_, _) => ApplyPreset(themePreset.SelectedItem?.ToString());
        page.Controls.Add(table);
        return page;
    }

    void ApplyPreset(string? name)
    {
        working.ThemeName = name ?? "自定义";
        string[]? colors = name switch
        {
            "经典灰" => ["#ECECEC", "#FFFFFF", "#202020", "#74836E", "#E8E8E8", "#A0A0A0"],
            "暖纸" => ["#E9E5DC", "#FFFDF7", "#292722", "#8A745C", "#E8E2D6", "#A59A88"],
            "夜间" => ["#292B2D", "#373A3C", "#F0F0ED", "#A9BE9A", "#44484A", "#72797A"],
            _ => null
        };
        if (colors is null) return;
        string[] properties = [nameof(AppSettings.WindowColor), nameof(AppSettings.PanelColor), nameof(AppSettings.TextColor), nameof(AppSettings.AccentColor), nameof(AppSettings.ButtonColor), nameof(AppSettings.BorderColor)];
        for (int i = 0; i < properties.Length; i++) colorButtons[properties[i]].BackColor = ColorTranslator.FromHtml(colors[i]);
    }

    void AddColorRow(TableLayoutPanel table, string label, string property)
    {
        var button = new Button { Width = 110, Height = 28, Text = "选择颜色…", BackColor = ColorTranslator.FromHtml((string)typeof(AppSettings).GetProperty(property)!.GetValue(working)!) };
        button.Click += (_, _) =>
        {
            using var dialog = new ColorDialog { FullOpen = true, Color = button.BackColor };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                button.BackColor = dialog.Color;
                themePreset.SelectedItem = "自定义";
            }
        };
        colorButtons[property] = button;
        AddRow(table, label, button);
    }

    TabPage CreatePicturesTab()
    {
        var page = new TabPage("壁纸与装饰") { Padding = new Padding(18) };
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
        var wallpaperRow = new FlowLayoutPanel { Height = 36, Width = 500, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        var chooseWallpaper = new Button { Text = "选择壁纸…", AutoSize = true };
        var removeWallpaper = new Button { Text = "清除壁纸", AutoSize = true };
        wallpaperDisplay.Text = working.WallpaperFile is null ? "未设置" : "已设置";
        wallpaperDisplay.Margin = new Padding(8, 7, 0, 0);
        wallpaperDisplay.AutoSize = true;
        chooseWallpaper.Click += (_, _) => PickImage(path => { wallpaperPath = path; clearWallpaper = false; wallpaperDisplay.Text = Path.GetFileName(path); });
        removeWallpaper.Click += (_, _) => { wallpaperPath = null; clearWallpaper = true; wallpaperDisplay.Text = "未设置"; };
        wallpaperRow.Controls.AddRange([chooseWallpaper, removeWallpaper, wallpaperDisplay]);
        AddPictureSection(table, "窗口壁纸", "壁纸显示在窗口外框和未设置资料图片时的预览区。", wallpaperRow);

        var decorationRow = new FlowLayoutPanel { Height = 36, Width = 500, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        var chooseDecoration = new Button { Text = "选择装饰图片…", AutoSize = true };
        var removeDecoration = new Button { Text = "清除装饰", AutoSize = true };
        decorationDisplay.Text = working.DecorationFile is null ? "未设置" : "已设置";
        decorationDisplay.Margin = new Padding(8, 7, 0, 0);
        decorationDisplay.AutoSize = true;
        chooseDecoration.Click += (_, _) => PickImage(path => { decorationPath = path; clearDecoration = false; decorationDisplay.Text = Path.GetFileName(path); });
        removeDecoration.Click += (_, _) => { decorationPath = null; clearDecoration = true; decorationDisplay.Text = "未设置"; };
        decorationRow.Controls.AddRange([chooseDecoration, removeDecoration, decorationDisplay]);
        AddPictureSection(table, "自定义装饰", "建议使用透明 PNG。装饰显示在工具栏角落，不遮挡资料内容。", decorationRow);
        decorationSize.Minimum = 20;
        decorationSize.Maximum = 80;
        decorationSize.Value = Math.Clamp(working.DecorationSize, 20, 80);
        decorationSize.Width = 80;
        AddPictureSection(table, "装饰大小", "单位：像素", decorationSize);
        animateDecoration.Checked = working.AnimateDecoration;
        AddPictureSection(table, "装饰动效", "可随时关闭；动画只作用于右上角装饰。", animateDecoration);
        page.Controls.Add(table);
        return page;
    }

    void PickImage(Action<string> onPicked)
    {
        using var dialog = new OpenFileDialog { Title = "选择图片", Filter = "图片|*.png;*.jpg;*.jpeg;*.gif;*.bmp|所有文件|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) onPicked(dialog.FileName);
    }

    static void AddPictureSection(TableLayoutPanel table, string heading, string note, Control content)
    {
        var panel = new Panel { Height = 92, Dock = DockStyle.Top };
        panel.Controls.Add(new Label { Text = note, Top = 61, Left = 0, AutoSize = true, ForeColor = Color.DimGray });
        content.Top = 23;
        panel.Controls.Add(content);
        panel.Controls.Add(new Label { Text = heading, Top = 0, Left = 0, AutoSize = true, Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold) });
        table.Controls.Add(panel);
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
    }

    static void AddRow(TableLayoutPanel table, string caption, Control control)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        table.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        table.Controls.Add(control, 1, row);
    }

    void Commit()
    {
        foreach (var (property, button) in colorButtons)
            typeof(AppSettings).GetProperty(property)!.SetValue(working, ColorTranslator.ToHtml(button.BackColor));
        working.DecorationSize = (int)decorationSize.Value;
        working.AnimateDecoration = animateDecoration.Checked;
        try
        {
            if (clearWallpaper) working.WallpaperFile = null;
            else if (wallpaperPath is not null) working.WallpaperFile = store.SaveAsset(wallpaperPath);
            if (clearDecoration) working.DecorationFile = null;
            else if (decorationPath is not null) working.DecorationFile = store.SaveAsset(decorationPath);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, "图片无法保存：" + error.Message, "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
