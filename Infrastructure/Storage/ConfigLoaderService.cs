using Quanta.Interfaces;
using Quanta.Models;

namespace Quanta.Helpers;

/// <summary>
/// <see cref="IConfigLoader"/> 的默认实现，委托到静态 <see cref="ConfigLoader"/>。
/// 静态 ConfigLoader 的所有调用站点无需修改，此类仅为 DI 提供可注入的实例。
/// </summary>
public sealed class ConfigLoaderService : IConfigLoader
{
    public AppConfig Load()              => ConfigLoader.Load();
    public void Save(AppConfig config)   => ConfigLoader.Save(config);
    public AppConfig Reload()            => ConfigLoader.Reload();
}
