using System.Drawing;
using System.Windows.Forms;

namespace Quanta.WinFormsLite;

public sealed class ThemePalette
{
    public required Color Background { get; init; }
    public required Color Surface { get; init; }
    public required Color Foreground { get; init; }
    public required Color SecondaryText { get; init; }
}

public static class ThemeManager
{
    public static ThemePalette Resolve(string theme)
    {
        var dark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        return dark
            ? new ThemePalette
            {
                Background = Color.FromArgb(20, 20, 22),
                Surface = Color.FromArgb(33, 33, 36),
                Foreground = Color.FromArgb(230, 230, 232),
                SecondaryText = Color.FromArgb(150, 150, 155)
            }
            : new ThemePalette
            {
                Background = Color.White,
                Surface = Color.FromArgb(245, 245, 247),
                Foreground = Color.Black,
                SecondaryText = Color.DimGray
            };
    }

    public static void Apply(Form form, Control search, Control list, Control footer, ThemePalette palette)
    {
        form.BackColor = palette.Background;
        form.ForeColor = palette.Foreground;

        search.BackColor = palette.Surface;
        search.ForeColor = palette.Foreground;

        list.BackColor = palette.Surface;
        list.ForeColor = palette.Foreground;

        footer.BackColor = palette.Background;
        footer.ForeColor = palette.SecondaryText;
    }
}
