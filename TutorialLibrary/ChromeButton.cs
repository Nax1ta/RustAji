using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace TutorialLibrary;

internal class ChromeButton : Button
{
    readonly Func<AppSettings> settings;
    readonly bool headerStyle;
    readonly System.Windows.Forms.Timer hoverTimer = new() { Interval = 16 };
    float hover;
    float target;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Graphics, Button>? GlyphPainter { get; set; }

    public ChromeButton(Func<AppSettings> settings, bool headerStyle = false)
    {
        this.settings = settings;
        this.headerStyle = headerStyle;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        hoverTimer.Tick += (_, _) =>
        {
            hover += (target - hover) * 0.32f;
            if (Math.Abs(target - hover) < 0.015f) { hover = target; hoverTimer.Stop(); }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); target = 1; hoverTimer.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); target = 0; hoverTimer.Start(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = settings();
        Color panel = Parse(headerStyle ? theme.PanelColor : theme.ButtonColor, Color.White);
        Color border = Parse(theme.BorderColor, Color.Gray);
        Color accent = Parse(theme.AccentColor, Color.DarkOliveGreen);
        Color ink = Parse(theme.TextColor, Color.Black);
        float amount = Pressed ? 0.15f : hover * 0.09f;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f), ChromeStyle.CornerRadius);
        using var fill = new SolidBrush(Blend(panel, accent, amount));
        using var line = new Pen(border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(line, path);
        if (GlyphPainter is null)
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ink : SystemColors.GrayText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        else GlyphPainter(e.Graphics, this);
    }

    bool Pressed => Capture && MouseButtons == MouseButtons.Left;

    protected override void Dispose(bool disposing)
    {
        if (disposing) hoverTimer.Dispose();
        base.Dispose(disposing);
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

    static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        float diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

