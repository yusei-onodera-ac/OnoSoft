using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using OnoSoft.Shared.Theme;

namespace OnoSoft.Shared.Tray;

/// <summary>
/// Draws consistent, brand-colored tray icons at runtime so individual apps don't
/// need to ship a separate .ico resource just to look like they belong to the
/// おのソフト series. A single glyph character (e.g. "C" for Clipboard, "P" for
/// Pomodoro) is enough to give each app a distinct icon.
/// </summary>
public static class IconFactory
{
    /// <summary>Creates a 32x32 rounded-square icon with the given glyph centered on it.</summary>
    public static Icon CreateGlyphIcon(string glyph, string? accentHex = null)
    {
        var accent = ColorTranslator.FromHtml(accentHex ?? BrandColors.Accent);
        var panel = ColorTranslator.FromHtml(BrandColors.Panel);

        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            using var accentBrush = new SolidBrush(accent);
            FillRoundedRectangle(g, accentBrush, 2, 2, 28, 28, 7);

            using var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(panel);
            var size = g.MeasureString(glyph, font);
            g.DrawString(glyph, font, textBrush, (32 - size.Width) / 2, (32 - size.Height) / 2 - 1);
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
