// ============================================================================
// 文件名: ThemeService.cs
// 文件用途: 提供应用程序主题切换服务，支持在亮色主题（Light）和暗色主题（Dark）
//          之间动态切换。通过替换 WPF 应用程序的合并资源字典来实现主题变更。
// ============================================================================

using System.Linq;
using System.Windows;

namespace Quanta.Services;

/// <summary>
/// 静态主题服务类，负责管理和切换应用程序的 UI 主题。
/// 支持亮色（Light）和暗色（Dark）两种主题模式，
/// 通过动态加载和替换 WPF 资源字典实现主题切换。
/// </summary>
public static class ThemeService
{
    /// <summary>
    /// 亮色主题的名称常量
    /// </summary>
    private const string ThemeLight = "Light";

    /// <summary>
    /// 暗色主题的名称常量
    /// </summary>
    private const string ThemeDark = "Dark";

    /// <summary>
    /// 获取当前正在使用的主题名称。默认为亮色主题。
    /// </summary>
    public static string CurrentTheme { get; private set; } = ThemeLight;

    /// <summary>
    /// 应用指定的主题到应用程序。
    /// 该方法会先移除当前已加载的主题资源字典，然后加载并添加新主题的资源字典。
    /// 如果传入的主题与当前主题相同，则不做任何操作。
    /// </summary>
    /// <param name="theme">要应用的主题名称（"Light" 或 "Dark"）。如果为空或空白则默认使用亮色主题。</param>
    public static void ApplyTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme)) theme = ThemeLight;
        if (CurrentTheme == theme) return;

        // 使用 System.Windows.Application 显式引用，避免与 WinForms/App 框架的命名冲突
        var dicts = System.Windows.Application.Current!.Resources.MergedDictionaries;
        // 移除现有的主题资源字典（包含 LightTheme 或 DarkTheme 的资源字典）
        var toRemove = dicts.Where(d => d.Source != null && (d.Source.OriginalString.Contains("LightTheme") || d.Source.OriginalString.Contains("DarkTheme"))).ToList();
        foreach (var d in toRemove) dicts.Remove(d);

        // 根据主题名称构建对应的 XAML 资源路径，并添加新的主题资源字典
        var newDict = new System.Windows.ResourceDictionary();
        string path = theme == ThemeDark ? "Resources/Themes/DarkTheme.xaml" : "Resources/Themes/LightTheme.xaml";
        newDict.Source = new System.Uri($"pack://application:,,,/{path}", System.UriKind.Absolute);
        dicts.Add(newDict);

        CurrentTheme = theme;
    }
}
