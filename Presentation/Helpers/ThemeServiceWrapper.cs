using Quanta.Interfaces;

namespace Quanta.Services;

/// <summary>
/// <see cref="IThemeService"/> 的默认实现，委托到静态 <see cref="ThemeService"/>。
/// 静态 ThemeService 的所有调用站点无需修改，此类仅为 DI 提供可注入的实例。
/// </summary>
public sealed class ThemeServiceWrapper : IThemeService
{
    public string CurrentTheme => ThemeService.CurrentTheme;
    public void ApplyTheme(string theme) => ThemeService.ApplyTheme(theme);
}
