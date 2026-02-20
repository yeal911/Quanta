// ============================================================================
// 文件名：ConfigLoader.cs
// 文件用途：应用配置的加载、保存和管理工具类。
//          配置文件直接保存在程序运行目录下的 config.json 中。
// ============================================================================

using System.IO;
using System.Text.Json;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Helpers;

/// <summary>
/// 静态配置加载器，提供应用配置的加载、保存、导出功能。
/// 配置文件保存在程序运行目录下的 config.json 中。
/// </summary>
public static class ConfigLoader
{
    /// <summary>配置缓存，避免重复读取文件</summary>
    private static AppConfig? _cachedConfig;

    /// <summary>
    /// 配置文件路径（程序运行目录下的 config.json）
    /// 单文件发布时使用实际 exe 所在目录
    /// </summary>
    private static string ConfigPath
    {
        get
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            // 尝试获取实际 exe 路径（单文件发布时更准确）
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                exeDir = Path.GetDirectoryName(processPath) ?? exeDir;
            }
            return Path.Combine(exeDir, "config.json");
        }
    }

    /// <summary>JSON 序列化选项：缩进格式、属性名大小写不敏感</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 加载应用配置。优先从缓存返回，其次从 config.json 读取。
    /// 如果配置文件不存在或读取失败，则创建默认配置。
    /// </summary>
    /// <returns>加载的应用配置对象</returns>
    public static AppConfig Load()
    {
        if (_cachedConfig != null)
        {
            Logger.Debug("Using cached config");
            return _cachedConfig;
        }

        try
        {
            // 获取绝对路径并打印
            var fullPath = Path.GetFullPath(ConfigPath);
            Logger.Debug($"Config file path: {fullPath}");
            Logger.Debug($"File exists: {File.Exists(ConfigPath)}");

            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Logger.Debug($"Config file content length: {json.Length} characters");
                
                _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                
                if (_cachedConfig != null)
                {
                    Logger.Log($"Deserialized config - Commands count: {_cachedConfig.Commands?.Count ?? 0}");
                    if (_cachedConfig.Commands != null && _cachedConfig.Commands.Count > 0)
                    {
                        var commandKeywords = string.Join(", ", _cachedConfig.Commands.Select(c => $"{c.Keyword}({c.Name})"));
                        Logger.Log($"Commands from file: {commandKeywords}");
                    }
                    else
                    {
                        Logger.Log("No commands found in file");
                    }
                    
                    // Migrate if needed
                    _cachedConfig = MigrateConfig(_cachedConfig);
                }
                else
                {
                    Logger.Log("Failed to deserialize config, creating default");
                    _cachedConfig = CreateDefaultConfig();
                }
            }
            else
            {
                Logger.Log("Config file not found, creating default config");
                _cachedConfig = CreateDefaultConfig();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load config: {ex.Message}", ex);
            _cachedConfig = CreateDefaultConfig();
        }

        return _cachedConfig;
    }

    /// <summary>
    /// 保存应用配置到 config.json。
    /// 同时更新内存缓存。
    /// </summary>
    /// <param name="config">要保存的应用配置对象</param>
    public static void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);

            _cachedConfig = config;

            Logger.Log($"Config saved to: {ConfigPath}");
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
    /// 创建并保存默认配置，包含默认快捷键（Alt+Space）、示例命令、
    /// 默认命令分组、插件设置和应用设置。
    /// </summary>
    /// <returns>新创建的默认配置对象</returns>
    private static AppConfig CreateDefaultConfig()
    {
        var config = new AppConfig
        {
            Version = "1.2",
            Theme = "Light",
            Hotkey = new HotkeyConfig { Modifier = "Alt", Key = "Space" },
            Commands = Services.CommandService.GenerateSampleCommands(),
            CommandGroups = Services.CommandService.GenerateDefaultGroups(),
            PluginSettings = new PluginSettings
            {
                Enabled = true,
                PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins")
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
                    PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins")
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

        // v1.0 → v1.1：录音参数优化为会议录音默认值（≤0.24MB/min）
        if (config.Version == "1.0")
        {
            Logger.Log("Migrating config to v1.1: updating recording defaults...");

            if (config.RecordingSettings == null)
            {
                config.RecordingSettings = new RecordingSettings();
            }
            else
            {
                // 只重置仍为旧默认值的参数，避免覆盖用户有意修改的设置
                // 旧默认：44100Hz / 128kbps / 2声道（立体声）
                if (config.RecordingSettings.SampleRate == 44100)
                    config.RecordingSettings.SampleRate = 16000;
                if (config.RecordingSettings.Bitrate == 128)
                    config.RecordingSettings.Bitrate = 32;
                if (config.RecordingSettings.Channels == 2)
                    config.RecordingSettings.Channels = 1;
            }

            config.Version = "1.1";
            Save(config);
        }

        // v1.1 → v1.2：SampleRate 字段已加入 UI，强制将未经用户修改的旧值 44100 迁移到 16000。
        // 旧版本 UI 不显示采样率，用户无法主动设置，故 44100 均为遗留默认值可安全覆盖。
        if (config.Version == "1.1")
        {
            Logger.Log("Migrating config to v1.2: normalizing SampleRate...");

            if (config.RecordingSettings == null)
            {
                config.RecordingSettings = new RecordingSettings();
            }
            else if (config.RecordingSettings.SampleRate == 44100)
            {
                config.RecordingSettings.SampleRate = 16000;
                Logger.Log("RecordingSettings.SampleRate: 44100 → 16000");
            }

            config.Version = "1.2";
            Save(config);
        }

        return config;
    }

    /// <summary>
    /// 获取配置文件路径。
    /// </summary>
    /// <returns>配置文件路径</returns>
    public static string GetConfigPath() => ConfigPath;
}
