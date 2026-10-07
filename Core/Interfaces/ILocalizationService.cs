namespace Quanta.Interfaces;

/// <summary>
/// 本地化服务接口，供依赖注入使用。
/// 默认实现为 <see cref="Services.LocalizationManager"/>；
/// 静态门面 <see cref="Services.LocalizationService"/> 委托到容器解析的本接口实例。
/// </summary>
public interface ILocalizationService
{
    string CurrentLanguage { get; set; }
    void LoadFromConfig();
    string Get(string key);
    string Get(string key, params object[] args);
}
