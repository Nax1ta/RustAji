namespace TutorialLibrary;

internal static class ChromeStyle
{
    public const int CornerRadius = 2;
    public const int HeaderHeight = 32;
    public const int ControlGap = 7;

    public static void DrawQuietBands(Graphics graphics, Rectangle bounds, Color baseColor)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var baseBrush = new SolidBrush(baseColor);
        using var light = new SolidBrush(Blend(baseColor, Color.White, 0.08f));
        using var shade = new SolidBrush(Blend(baseColor, Color.Black, 0.02f));
        graphics.FillRectangle(baseBrush, bounds);
        graphics.FillRectangle(light, bounds.Left, bounds.Top, bounds.Width, Math.Min(9, bounds.Height));
        if (bounds.Height > 22)
            graphics.FillRectangle(shade, bounds.Left, bounds.Top + 22, bounds.Width, Math.Min(6, bounds.Height - 22));
        if (bounds.Height > 36)
            graphics.FillRectangle(light, bounds.Left, bounds.Top + 36, bounds.Width, Math.Min(11, bounds.Height - 36));
    }

    public static void SetQuietBackground(Panel panel)
    {
        var previous = panel.BackgroundImage;
        var picture = new Bitmap(1, Math.Max(1, panel.Height));
        using (var graphics = Graphics.FromImage(picture))
            DrawQuietBands(graphics, new Rectangle(0, 0, picture.Width, picture.Height), panel.BackColor);
        panel.BackgroundImage = picture;
        panel.BackgroundImageLayout = ImageLayout.Stretch;
        previous?.Dispose();
    }

    static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));

    public static void Apply(ToolStripDropDown menu, AppSettings settings, Font font)
    {
        menu.Font = font;
        menu.BackColor = Parse(settings.PanelColor, Color.White);
        menu.ForeColor = Parse(settings.TextColor, Color.Black);
        menu.Padding = new Padding(2);
        menu.Renderer = new HeaderMenuRenderer(() => settings);
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem) item.Padding = new Padding(6, 3, 8, 3);
    }

    static Color Parse(string value, Color fallback)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }
}
