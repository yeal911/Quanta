// ============================================================================
// 文件名：ConfigLoader.cs
// 文件用途：应用配置的加载、保存和管理工具类。
//          负责从 JSON 配置文件读取/写入应用配置，支持用户目录和本地目录双路径存储，
//          提供配置缓存、版本迁移、默认配置创建以及必要目录的自动创建功能。
// ============================================================================

using System.IO;
using System.Text.Json;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Helpers;

/// <summary>
/// 静态配置加载器，提供应用配置的加载、保存、导出和版本迁移功能。
/// 配置文件以 JSON 格式存储，优先从用户 AppData 目录读取，回退到应用程序本地目录。
/// </summary>
public static class ConfigLoader
{
    /// <summary>配置缓存，避免重复读取文件</summary>
    private static AppConfig? _cachedConfig;

    /// <summary>应用程序本地目录下的配置文件路径</summary>
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");

    /// <summary>用户 AppData/Roaming/Quanta 目录路径</summary>
    private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Quanta");

    /// <summary>用户目录下的配置文件路径（优先使用）</summary>
    private static readonly string UserConfigPath = Path.Combine(AppDataPath, "config.json");

    /// <summary>JSON 序列化选项：缩进格式、属性名大小写不敏感</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 获取用户 AppData 目录路径。
    /// </summary>
    /// <returns>AppData/Roaming/Quanta 目录路径</returns>
    public static string GetAppDataPath() => AppDataPath;

    /// <summary>
    /// 加载应用配置。优先从缓存返回，其次从用户目录读取，最后从本地目录读取。
    /// 如果配置文件不存在或读取失败，则创建默认配置。
    /// 加载后会执行版本迁移检查并确保必要目录存在。
    /// </summary>
    /// <returns>加载的应用配置对象</returns>
    public static AppConfig Load()
    {
        if (_cachedConfig != null) return _cachedConfig;

        try
        {
            // Try user config first (in AppData)
            var configPath = File.Exists(UserConfigPath) ? UserConfigPath : ConfigPath;

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? CreateDefaultConfig();

                // Migrate if needed
                _cachedConfig = MigrateConfig(_cachedConfig);
            }
            else
            {
                _cachedConfig = CreateDefaultConfig();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load config: {ex.Message}", ex);
            _cachedConfig = CreateDefaultConfig();
        }

        // Ensure directories exist
        EnsureDirectories();

        return _cachedConfig;
    }

    /// <summary>
    /// 保存应用配置到用户目录和本地目录（双份备份）。
    /// 同时更新内存缓存。
    /// </summary>
    /// <param name="config">要保存的应用配置对象</param>
    public static void Save(AppConfig config)
    {
        try
        {
            // Ensure AppData directory exists
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);

            // Save to user config path
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(UserConfigPath, json);

            // Also save to local directory as backup
            File.WriteAllText(ConfigPath, json);

            _cachedConfig = config;

            Logger.Log($"Config saved to: {UserConfigPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save config: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 将配置导出到指定路径。不影响缓存和默认配置路径。
    /// </summary>
    /// <param name="config">要导出的配置对象</param>
    /// <param name="path">导出目标文件路径</param>
    public static void SaveTo(AppConfig config, string path)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
            Logger.Log($"Config exported to: {path}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save config to {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清除缓存并重新加载配置。用于配置变更后强制刷新。
    /// </summary>
    /// <returns>重新加载的应用配置对象</returns>
    public static AppConfig Reload()
    {
        _cachedConfig = null;
        return Load();
    }

    /// <summary>
    /// 确保必要的目录结构存在（AppData 目录、插件目录、日志目录）。
    /// 在配置加载完成后自动调用。
    /// </summary>
    private static void EnsureDirectories()
    {
        try
        {
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);

            // Create plugin directory
            var pluginDir = Path.Combine(AppDataPath, "Plugins");
            if (!Directory.Exists(pluginDir))
                Directory.CreateDirectory(pluginDir);

            // Create logs directory
            var logsDir = Path.Combine(AppDataPath, "Logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create directories: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建并保存默认配置，包含默认快捷键（Alt+Space）、示例命令、
    /// 默认命令分组、插件设置和应用设置。
    /// </summary>
    /// <returns>新创建的默认配置对象</returns>
    private static AppConfig CreateDefaultConfig()
    {
        var config = new AppConfig
        {
            Version = "1.0",
            Theme = "Light",
            Hotkey = new HotkeyConfig { Modifier = "Alt", Key = "Space" },
            Commands = Services.CommandService.GenerateSampleCommands(),
            CommandGroups = Services.CommandService.GenerateDefaultGroups(),
            PluginSettings = new PluginSettings
            {
                Enabled = true,
                PluginDirectory = Path.Combine(AppDataPath, "Plugins")
            },
            AppSettings = new AppSettings
            {
                StartWithWindows = false,
                MinimizeToTray = true,
                CloseToTray = true,
                ShowInTaskbar = false,
                MaxResults = 10,
                AutoUpdate = true,
                CheckForUpdatesOnStartup = true
            }
        };

        // Save default config
        Save(config);

        return config;
    }

    /// <summary>
    /// 执行配置版本迁移。将旧版本（v0.x）配置升级到 v1.0 格式，
    /// 补充缺失的命令、分组、插件设置、应用设置等字段，
    /// 并为缺少 ID 的命令生成唯一标识符。
    /// </summary>
    /// <param name="config">待迁移的配置对象</param>
    /// <returns>迁移后的配置对象</returns>
    private static AppConfig MigrateConfig(AppConfig config)
    {
        // Migrate from older versions
        if (string.IsNullOrEmpty(config.Version))
        {
            // Migrate from v0.x to v1.0
            Logger.Log("Migrating config to v1.0...");

            // Add sample commands if empty
            if (config.Commands == null || config.Commands.Count == 0)
            {
                config.Commands = Services.CommandService.GenerateSampleCommands();
            }

            // Add command groups if not present
            if (config.CommandGroups == null || config.CommandGroups.Count == 0)
            {
                config.CommandGroups = Services.CommandService.GenerateDefaultGroups();
            }

            // Add plugin settings if not present
            if (config.PluginSettings == null)
            {
                config.PluginSettings = new PluginSettings
                {
                    Enabled = true,
                    PluginDirectory = Path.Combine(AppDataPath, "Plugins")
                };
            }

            // Add app settings if not present
            if (config.AppSettings == null)
            {
                config.AppSettings = new AppSettings
                {
                    StartWithWindows = false,
                    MinimizeToTray = true,
                    CloseToTray = true,
                    ShowInTaskbar = false,
                    MaxResults = 10,
                    AutoUpdate = true,
                    CheckForUpdatesOnStartup = true
                };
            }

            // Add new properties to commands if not present
            if (config.Commands != null)
            {
                foreach (var cmd in config.Commands)
                {
                    if (string.IsNullOrEmpty(cmd.Id))
                        cmd.Id = Guid.NewGuid().ToString();
                    if (!cmd.Enabled)
                        cmd.Enabled = true;
                }
            }

            config.Version = "1.0";

            // Save migrated config
            Save(config);
        }

        return config;
    }

    /// <summary>
    /// 获取用户配置文件的完整路径（AppData 目录下）。
    /// </summary>
    /// <returns>用户配置文件路径</returns>
    public static string GetConfigPath() => UserConfigPath;

    /// <summary>
    /// 获取本地配置文件的完整路径（应用程序目录下）。
    /// </summary>
    /// <returns>本地配置文件路径</returns>
    public static string GetLocalConfigPath() => ConfigPath;
}
