using System.Drawing.Drawing2D;

namespace TutorialLibrary;

internal sealed class HeaderMenuRenderer(Func<AppSettings> settings) : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var theme = settings();
        Color panel = Parse(theme.PanelColor, Color.White);
        Color border = Parse(theme.BorderColor, Color.Gray);
        Color accent = Parse(theme.AccentColor, Color.DarkOliveGreen);
        bool topLevel = e.Item.Owner is MenuStrip;
        bool active = e.Item.Selected || (e.Item is ToolStripMenuItem item && item.DropDown.Visible);
        Color fill = active ? Blend(panel, accent, 0.09f) : panel;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = topLevel
            ? new Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
            : new Rectangle(2, 1, Math.Max(1, e.Item.Width - 5), Math.Max(1, e.Item.Height - 3));
        using var path = RoundedRectangle(bounds, ChromeStyle.CornerRadius);
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);
        if (topLevel)
        {
            using var pen = new Pen(border);
            e.Graphics.DrawPath(pen, path);
        }
    }

    static Color Parse(string value, Color fallback)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }

    static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));

    static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
