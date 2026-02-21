namespace Quanta.Interfaces;

/// <summary>
/// 本地化服务接口，供依赖注入使用。
/// 默认实现由 <see cref="Services.LocalizationServiceWrapper"/> 提供，委托到静态 <see cref="Services.LocalizationService"/>。
/// </summary>
public interface ILocalizationService
{
    string CurrentLanguage { get; set; }
    void LoadFromConfig();
    string Get(string key);
    string Get(string key, params object[] args);
}
