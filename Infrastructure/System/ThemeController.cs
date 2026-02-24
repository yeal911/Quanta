using Quanta.Core.Interfaces;
using Quanta.Services;

namespace Quanta.Infrastructure.System;

public sealed class ThemeController : IThemeController
{
    public string CurrentTheme => ThemeService.CurrentTheme;
    public void ApplyTheme(string theme) => ThemeService.ApplyTheme(theme);
}
