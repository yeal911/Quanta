using System.Windows;
using System.Text.Json;
using Quanta.Core.Interfaces;
using Quanta.Core.Constants;
using Quanta.Models;

namespace Quanta.Helpers;

/// <summary>
/// 基于静态 ConfigLoader 的仓储适配层。
/// 所有写入经由 Update 统一提交，变更订阅返回可释放句柄。
/// </summary>
public sealed class ConfigRepository : IConfigRepository
{
    private readonly object _syncRoot = new();

    public AppConfig GetSnapshot()
    {
        lock (_syncRoot)
        {
            return Clone(ConfigLoader.Load());
        }
    }

    public AppConfig Reload()
    {
        lock (_syncRoot)
        {
            return Clone(ConfigLoader.Reload());
        }
    }

    public AppConfig Update(Func<AppConfig, AppConfig> updater)
    {
        lock (_syncRoot)
        {
            var current = Clone(ConfigLoader.Load());
            var updated = updater(current) ?? current;
            ConfigLoader.Save(updated);
            return Clone(updated);
        }
    }

    public IDisposable Subscribe(Action<AppConfig> onChanged, bool marshalToUiThread = true)
    {
        EventHandler<AppConfig> handler = (_, config) =>
        {
            var snapshot = Clone(config);
            if (marshalToUiThread && Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() => onChanged(snapshot));
                return;
            }

            onChanged(snapshot);
        };

        ConfigLoader.ConfigChanged += handler;
        return new Subscription(() => ConfigLoader.ConfigChanged -= handler);
    }

    private static AppConfig Clone(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonDefaults.Standard);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonDefaults.Standard) ?? new AppConfig();
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
