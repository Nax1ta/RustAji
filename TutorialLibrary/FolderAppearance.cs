using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TutorialLibrary;

public static class FolderAppearance
{
    public static bool IsValidColor(string? value) => value is null || Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$");
    public static Color ColorOr(string? value, Color fallback) => value is null ? fallback : ColorTranslator.FromHtml(value);
    public static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static Color FromHsv(decimal hue, decimal saturation, decimal value)
    {
        double h = (double)hue % 360, s = (double)saturation / 100, v = (double)value / 100;
        double chroma = v * s, x = chroma * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - chroma;
        (double r, double g, double b) = h switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }

    public static (decimal Hue, decimal Saturation, decimal Value) ToHsv(Color color)
    {
        double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
        double hue = delta == 0 ? 0 : max == r ? 60 * (((g - b) / delta + 6) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        return ((decimal)Math.Round(hue), (decimal)Math.Round(max == 0 ? 0 : delta / max * 100), (decimal)Math.Round(max * 100));
    }

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (d < 2) { path.AddRectangle(bounds); return path; }
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SaturationValuePlane : Control
{
    Bitmap? bitmap;
    double hue, saturation = 1, value = 1;
    public event EventHandler? SelectionChanged;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Hue { get => hue; set { hue = Math.Clamp(value, 0, 359); Rebuild(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Saturation { get => saturation; set { saturation = Math.Clamp(value, 0, 1); Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Value { get => this.value; set { this.value = Math.Clamp(value, 0, 1); Invalidate(); } }

    public SaturationValuePlane()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Cross;
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Rebuild(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); Capture = true; Pick(e.Location); } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (Capture && e.Button == MouseButtons.Left) Pick(e.Location); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left) Saturation -= 0.01;
        else if (e.KeyCode == Keys.Right) Saturation += 0.01;
        else if (e.KeyCode == Keys.Up) Value += 0.01;
        else if (e.KeyCode == Keys.Down) Value -= 0.01;
        else return;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    void Pick(Point point)
    {
        Saturation = Math.Clamp(point.X / (double)Math.Max(1, Width - 1), 0, 1);
        Value = 1 - Math.Clamp(point.Y / (double)Math.Max(1, Height - 1), 0, 1);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    void Rebuild()
    {
        var previous = bitmap;
        bitmap = null;
        if (Width > 1 && Height > 1)
        {
            var next = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            var pixels = next.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(pixels.Stride);
                var bytes = new byte[stride * Height];
                int sector = Math.Min(5, (int)(hue / 60));
                double hueFraction = 1 - Math.Abs(hue / 60 % 2 - 1);
                for (int y = 0; y < Height; y++)
                {
                    double brightness = (Height - 1 - y) / (double)(Height - 1);
                    int row = (pixels.Stride > 0 ? y : Height - 1 - y) * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        double chroma = brightness * x / (Width - 1d);
                        double secondary = chroma * hueFraction, baseValue = brightness - chroma;
                        (double red, double green, double blue) = sector switch
                        {
                            0 => (chroma, secondary, 0d), 1 => (secondary, chroma, 0d),
                            2 => (0d, chroma, secondary), 3 => (0d, secondary, chroma),
                            4 => (secondary, 0d, chroma), _ => (chroma, 0d, secondary)
                        };
                        int offset = row + x * 4;
                        bytes[offset] = (byte)Math.Round((blue + baseValue) * 255);
                        bytes[offset + 1] = (byte)Math.Round((green + baseValue) * 255);
                        bytes[offset + 2] = (byte)Math.Round((red + baseValue) * 255);
                        bytes[offset + 3] = 255;
                    }
                }
                Marshal.Copy(bytes, 0, pixels.Scan0, bytes.Length);
            }
            finally { next.UnlockBits(pixels); }
            bitmap = next;
        }
        previous?.Dispose();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (bitmap is not null) e.Graphics.DrawImageUnscaled(bitmap, 0, 0);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float x = (float)(saturation * (Width - 1)), y = (float)((1 - value) * (Height - 1));
        using var dark = new Pen(Color.FromArgb(170, 20, 20, 20), 3);
        using var light = new Pen(Color.White, 1.5f);
        e.Graphics.DrawEllipse(dark, x - 6, y - 6, 12, 12);
        e.Graphics.DrawEllipse(light, x - 6, y - 6, 12, 12);
        if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }

    protected override void Dispose(bool disposing) { if (disposing) bitmap?.Dispose(); base.Dispose(disposing); }
}

internal sealed class HueStrip : Control
{
    double hue;
    public event EventHandler? HueChanged;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double Hue { get => hue; set { hue = Math.Clamp(value, 0, 359); Invalidate(); } }

    public HueStrip()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Focus(); Capture = true; Pick(e.X); } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (Capture && e.Button == MouseButtons.Left) Pick(e.X); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left) Hue -= 1;
        else if (e.KeyCode == Keys.Right) Hue += 1;
        else return;
        HueChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
    void Pick(int x) { Hue = Math.Clamp(x * 359d / Math.Max(1, Width - 1), 0, 359); HueChanged?.Invoke(this, EventArgs.Empty); }
    protected override void OnPaint(PaintEventArgs e)
    {
        for (int x = 0; x < Width; x++)
        {
            using var pen = new Pen(FolderAppearance.FromHsv((decimal)(x * 359d / Math.Max(1, Width - 1)), 100, 100));
            e.Graphics.DrawLine(pen, x, 3, x, Height - 4);
        }
        int mark = (int)Math.Round(hue * (Width - 1) / 359d);
        using var outline = new Pen(Color.FromArgb(45, 45, 45), 2);
        e.Graphics.DrawRectangle(outline, Math.Clamp(mark - 2, 0, Math.Max(0, Width - 5)), 1, 4, Math.Max(1, Height - 3));
        if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }
}

public sealed class FolderAppearanceEditor : Form
{
    static readonly string[] TargetNames = ["文字颜色", "背景填充", "文件夹图标"];
    readonly Color[] colors = new Color[3];
    readonly bool[] enabled = new bool[3];
    readonly ListBox targets = new() { Dock = DockStyle.Top, Height = 144, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 44, BorderStyle = BorderStyle.FixedSingle };
    readonly CheckBox custom = new() { Text = "使用自定义颜色", AutoSize = true };
    readonly SaturationValuePlane plane = new() { Dock = DockStyle.Fill };
    readonly HueStrip hue = new() { Dock = DockStyle.Fill };
    readonly TextBox hex = new() { Width = 100, CharacterCasing = CharacterCasing.Upper };
    readonly TrackBar opacity = new() { Minimum = 0, Maximum = 100, TickFrequency = 10, AutoSize = false, Height = 35, Dock = DockStyle.Fill };
    readonly TrackBar radius = new() { Minimum = 0, Maximum = 16, TickFrequency = 2, AutoSize = false, Height = 35, Dock = DockStyle.Fill };
    readonly Label opacityValue = new() { AutoSize = true };
    readonly Label radiusValue = new() { AutoSize = true };
    readonly Panel preview = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(3, 5, 3, 12) };
    readonly Color defaultText, defaultIcon, previewBackground;
    readonly string folderName;
    int selected;
    bool loading;

    public string? TextColor => enabled[0] ? FolderAppearance.Hex(colors[0]) : null;
    public string? FillColor => enabled[1] ? FolderAppearance.Hex(colors[1]) : null;
    public string? IconColor => enabled[2] ? FolderAppearance.Hex(colors[2]) : null;
    public int FillOpacity => opacity.Value;
    public int CornerRadius => radius.Value;

    public FolderAppearanceEditor(AppSettings settings, LibraryFolder folder)
    {
        Text = "文件夹外观与图标颜色";
        Width = 830; Height = 545; MinimumSize = new Size(740, 500);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Font = new Font(settings.FontFamily, Math.Clamp(settings.FontSize, 8, 24)); }
        catch { Font = new Font("Microsoft YaHei UI", 9); }
        defaultText = FolderAppearance.ColorOr(settings.TextColor, Color.FromArgb(32, 32, 32));
        defaultIcon = Color.FromArgb(210, 205, 190);
        previewBackground = FolderAppearance.ColorOr(settings.PanelColor, Color.White);
        folderName = folder.Name;
        string?[] saved = [folder.LabelTextColor, folder.LabelFillColor, folder.IconColor];
        Color[] fallback = [defaultText, Color.FromArgb(232, 232, 232), defaultIcon];
        for (int i = 0; i < 3; i++) { enabled[i] = saved[i] is not null; colors[i] = FolderAppearance.ColorOr(saved[i], fallback[i]); }
        opacity.Value = Math.Clamp(folder.LabelFillOpacity, 0, 100);
        radius.Value = Math.Clamp(folder.LabelCornerRadius, 0, 16);

        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(16, 12, 16, 10) };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        Controls.Add(shell);
        shell.Controls.Add(new Label { Text = "选择要调整的部分，然后在色盘上拖动取色。", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 292));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(body, 0, 1);

        var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 3, 10, 0) };
        targets.Items.AddRange(TargetNames);
        targets.DrawItem += DrawTarget;
        targets.SelectedIndexChanged += (_, _) => SelectTarget(targets.SelectedIndex);
        left.Controls.Add(targets);
        custom.Top = 158;
        custom.CheckedChanged += (_, _) =>
        {
            if (loading) return;
            enabled[selected] = custom.Checked;
            targets.Invalidate(); preview.Invalidate();
        };
        left.Controls.Add(custom);
        left.Controls.Add(new Label { Text = "不勾选时跟随主题。", Top = 198, Width = 172, Height = 48, ForeColor = Color.DimGray });
        body.Controls.Add(left, 0, 0);

        var picker = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(4, 3, 10, 0) };
        picker.RowStyles.Add(new RowStyle(SizeType.Absolute, 264));
        picker.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        picker.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        picker.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        picker.Controls.Add(plane, 0, 0);
        picker.Controls.Add(hue, 0, 1);
        var hexRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        hexRow.Controls.Add(new Label { Text = "色值", Width = 44, Height = 25, TextAlign = ContentAlignment.MiddleLeft });
        hexRow.Controls.Add(hex);
        picker.Controls.Add(hexRow, 0, 2);
        body.Controls.Add(picker, 1, 0);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(8, 3, 0, 0) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.Controls.Add(new Label { Text = "实时预览", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        preview.Paint += DrawPreview;
        right.Controls.Add(preview, 0, 1);
        right.Controls.Add(opacityValue, 0, 2);
        right.Controls.Add(opacity, 0, 3);
        right.Controls.Add(radiusValue, 0, 4);
        right.Controls.Add(radius, 0, 5);
        right.Controls.Add(new Label { Text = "透明度只用于背景填充。", Dock = DockStyle.Fill, ForeColor = Color.DimGray }, 0, 6);
        body.Controls.Add(right, 2, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var save = new Button { Text = "保存", Width = 78, Height = 29, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", Width = 78, Height = 29, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => ApplyHex();
        actions.Controls.Add(save); actions.Controls.Add(cancel);
        shell.Controls.Add(actions, 0, 2);
        AcceptButton = save; CancelButton = cancel;

        plane.SelectionChanged += (_, _) => UpdatePickedColor();
        hue.HueChanged += (_, _) => { plane.Hue = hue.Hue; UpdatePickedColor(); };
        hex.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { ApplyHex(); e.SuppressKeyPress = true; } };
        hex.Leave += (_, _) => ApplyHex();
        opacity.ValueChanged += (_, _) => { UpdateSliderLabels(); preview.Invalidate(); };
        radius.ValueChanged += (_, _) => { UpdateSliderLabels(); preview.Invalidate(); };
        targets.SelectedIndex = 0;
        UpdateSliderLabels();
    }

    void SelectTarget(int index)
    {
        if (index < 0) return;
        selected = index;
        loading = true;
        custom.Checked = enabled[index];
        var hsv = FolderAppearance.ToHsv(colors[index]);
        hue.Hue = (double)hsv.Hue;
        plane.Hue = (double)hsv.Hue;
        plane.Saturation = (double)hsv.Saturation / 100;
        plane.Value = (double)hsv.Value / 100;
        hex.Text = FolderAppearance.Hex(colors[index]);
        opacity.Enabled = index == 1;
        loading = false;
        preview.Invalidate();
    }

    void UpdatePickedColor()
    {
        if (loading) return;
        colors[selected] = FolderAppearance.FromHsv((decimal)hue.Hue, (decimal)(plane.Saturation * 100), (decimal)(plane.Value * 100));
        enabled[selected] = true;
        custom.Checked = true;
        hex.Text = FolderAppearance.Hex(colors[selected]);
        targets.Invalidate(); preview.Invalidate();
    }

    void ApplyHex()
    {
        string value = hex.Text.Trim();
        if (!FolderAppearance.IsValidColor(value) || value.Length != 7)
        {
            hex.Text = FolderAppearance.Hex(colors[selected]);
            return;
        }
        var parsed = ColorTranslator.FromHtml(value);
        if (parsed.ToArgb() == colors[selected].ToArgb()) return;
        colors[selected] = parsed;
        enabled[selected] = true;
        SelectTarget(selected);
        custom.Checked = true;
        targets.Invalidate(); preview.Invalidate();
    }

    void UpdateSliderLabels()
    {
        opacityValue.Text = $"背景不透明度  {opacity.Value}%";
        radiusValue.Text = $"标签圆角  {radius.Value} 像素";
    }

    void DrawTarget(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        e.DrawBackground();
        int index = e.Index;
        var swatch = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 10, 22, 22);
        using var brush = new SolidBrush(colors[index]);
        e.Graphics.FillRectangle(brush, swatch);
        e.Graphics.DrawRectangle(Pens.Gray, swatch);
        Color ink = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : SystemColors.WindowText;
        TextRenderer.DrawText(e.Graphics, TargetNames[index], Font,
            new Rectangle(e.Bounds.Left + 40, e.Bounds.Top + 2, e.Bounds.Width - 44, 22), ink,
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(e.Graphics, enabled[index] ? FolderAppearance.Hex(colors[index]) : "跟随主题", Font,
            new Rectangle(e.Bounds.Left + 40, e.Bounds.Top + 22, e.Bounds.Width - 44, 20), ink,
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        e.DrawFocusRectangle();
    }

    void DrawPreview(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(previewBackground);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color iconColor = enabled[2] ? colors[2] : defaultIcon;
        using var iconBrush = new SolidBrush(iconColor);
        using var outline = new Pen(Color.FromArgb(100, 100, 100));
        e.Graphics.FillRectangle(iconBrush, 16, 47, 18, 13);
        e.Graphics.FillRectangle(iconBrush, 18, 43, 9, 5);
        e.Graphics.DrawRectangle(outline, 16, 47, 18, 13);
        Color textColor = enabled[0] ? colors[0] : defaultText;
        int width = Math.Max(12, Math.Min(preview.Width - 61, TextRenderer.MeasureText(folderName, Font).Width + 14));
        var bounds = new Rectangle(43, 40, width, 28);
        if (enabled[1])
        {
            using var path = FolderAppearance.RoundedRectangle(bounds, radius.Value);
            using var fill = new SolidBrush(Color.FromArgb((int)Math.Round(opacity.Value * 255d / 100), colors[1]));
            e.Graphics.FillPath(fill, path);
        }
        TextRenderer.DrawText(e.Graphics, folderName, Font, bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
