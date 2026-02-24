using Quanta.Models;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 应用配置仓储，统一负责配置快照读取、事务更新与变更订阅。
/// </summary>
public interface IConfigRepository
{
    AppConfig GetSnapshot();
    AppConfig Reload();
    AppConfig Update(Func<AppConfig, AppConfig> updater);
    IDisposable Subscribe(Action<AppConfig> onChanged, bool marshalToUiThread = true);
}
