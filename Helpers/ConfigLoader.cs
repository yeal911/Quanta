using System.IO;
using System.Text.Json;
using Quanta.Models;

namespace Quanta.Helpers;

public static class ConfigLoader
{
    private static AppConfig? _cachedConfig;
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");

    public static AppConfig Load()
    {
        if (_cachedConfig != null) return _cachedConfig;
        try { if (File.Exists(ConfigPath)) _cachedConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig(); else _cachedConfig = new AppConfig(); } catch { _cachedConfig = new AppConfig(); }
        ResolvePathPlaceholders(_cachedConfig);
        return _cachedConfig;
    }

    private static void ResolvePathPlaceholders(AppConfig config)
    {
        var username = Environment.UserName;
        for (int i = 0; i < config.SearchDirectories.Count; i++) config.SearchDirectories[i] = config.SearchDirectories[i].Replace("{username}", username);
    }

    public static void Save(AppConfig config) { try { File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })); _cachedConfig = config; } catch { } }
    public static AppConfig Reload() { _cachedConfig = null; return Load(); }
}
