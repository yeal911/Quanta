namespace Quanta.Core.Interfaces;

/// <summary>
/// 主题服务接口，供依赖注入使用。
/// 默认实现由 <see cref="Quanta.Presentation.Helpers.ThemeServiceWrapper"/> 提供，委托到静态 <see cref="Quanta.Presentation.Helpers.ThemeService"/>。
/// </summary>
public interface IThemeService
{
    string CurrentTheme { get; }
    void ApplyTheme(string theme);
}
