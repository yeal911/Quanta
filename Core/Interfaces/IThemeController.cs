namespace Quanta.Core.Interfaces;

/// <summary>
/// 主题控制抽象，屏蔽静态 ThemeService。
/// </summary>
public interface IThemeController
{
    string CurrentTheme { get; }
    void ApplyTheme(string theme);
}
