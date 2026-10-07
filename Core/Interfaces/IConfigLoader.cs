using Quanta.Models;

namespace Quanta.Interfaces;

/// <summary>
/// 配置加载服务接口，供依赖注入使用。
/// 默认实现为 <see cref="Helpers.ConfigLoaderService"/>，委托到静态 <see cref="Helpers.ConfigLoader"/>。
/// </summary>
public interface IConfigLoader
{
    AppConfig Load();
    void Save(AppConfig config);
    AppConfig Reload();
}
