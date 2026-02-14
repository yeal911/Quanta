using System.Linq;
using System.Windows;

namespace Quanta.Services;

public static class ThemeService
{
    private const string ThemeLight = "Light";
    private const string ThemeDark = "Dark";

    public static string CurrentTheme { get; private set; } = ThemeLight;

    public static void ApplyTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme)) theme = ThemeLight;
        if (CurrentTheme == theme) return;

        // Use System.Windows.Application explicitly to avoid ambiguity with WinForms/App framework
        var dicts = System.Windows.Application.Current!.Resources.MergedDictionaries;
        // Remove existing theme dictionaries
        var toRemove = dicts.Where(d => d.Source != null && (d.Source.OriginalString.Contains("LightTheme") || d.Source.OriginalString.Contains("DarkTheme"))).ToList();
        foreach (var d in toRemove) dicts.Remove(d);

        // Add new theme dictionary
        var newDict = new System.Windows.ResourceDictionary();
        string path = theme == ThemeDark ? "Resources/Themes/DarkTheme.xaml" : "Resources/Themes/LightTheme.xaml";
        newDict.Source = new System.Uri($"pack://application:,,,/{path}", System.UriKind.Absolute);
        dicts.Add(newDict);

        CurrentTheme = theme;
    }
}
