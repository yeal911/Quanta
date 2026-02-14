using System.IO;
using System.Text.Json;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Helpers;

public static class ConfigLoader
{
    private static AppConfig? _cachedConfig;
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Quanta");
    private static readonly string UserConfigPath = Path.Combine(AppDataPath, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string GetAppDataPath() => AppDataPath;

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

    public static AppConfig Reload()
    {
        _cachedConfig = null;
        return Load();
    }

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

    private static AppConfig MigrateConfig(AppConfig config)
    {
        // Migrate from older versions
        if (string.IsNullOrEmpty(config.Version))
        {
            // Migrate from v0.x to v1.0
            Logger.Log("Migrating config to v1.0...");
            
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
    /// Get the config file path
    /// </summary>
    public static string GetConfigPath() => UserConfigPath;

    /// <summary>
    /// Get the local config file path
    /// </summary>
    public static string GetLocalConfigPath() => ConfigPath;
}
