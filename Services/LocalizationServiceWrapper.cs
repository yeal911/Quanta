using Quanta.Interfaces;

namespace Quanta.Services;

/// <summary>
/// <see cref="ILocalizationService"/> 的默认实现，委托到静态 <see cref="LocalizationService"/>。
/// 静态 LocalizationService 的所有调用站点无需修改，此类仅为 DI 提供可注入的实例。
/// </summary>
public sealed class LocalizationServiceWrapper : ILocalizationService
{
    public string CurrentLanguage
    {
        get => LocalizationService.CurrentLanguage;
        set => LocalizationService.CurrentLanguage = value;
    }

    public void LoadFromConfig()                          => LocalizationService.LoadFromConfig();
    public string Get(string key)                         => LocalizationService.Get(key);
    public string Get(string key, params object[] args)   => LocalizationService.Get(key, args);
}
