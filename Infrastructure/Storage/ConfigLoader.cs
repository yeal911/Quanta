// ============================================================================
// 文件名：ConfigLoader.cs
// 文件用途：应用配置的加载、保存和管理工具类。
//          配置文件保存在 %APPDATA%\Quanta\config.json 中（受限安装目录不可写）。
//          首次启动时若发现 exe 目录下的旧配置文件，自动迁移到新位置（保留原文件）。
//          写入采用原子写（临时文件 + File.Move），解析失败时先备份损坏文件再重建默认配置。
//          支持热重载：配置文件变更时自动重新加载。
// ============================================================================

using System.IO;
using System.Text.Json;
using Quanta.Core.Constants;
using Quanta.Models;
using Quanta.Services;
using Application = System.Windows.Application;

namespace Quanta.Helpers;

/// <summary>
/// 静态配置加载器，提供应用配置的加载、保存、导出功能。
/// 配置文件保存在 %APPDATA%\Quanta\config.json 中。
/// 写入采用原子写（临时文件 + File.Move 覆盖），崩溃/断电不会产生截断的损坏文件；
/// 解析失败时先把损坏文件改名为 config.json.bak-&lt;yyyyMMddHHmmss&gt; 再生成默认配置。
/// 支持热重载：配置文件变更时自动重新加载。
/// </summary>
public static class ConfigLoader
{
    /// <summary>配置缓存，避免重复读取文件</summary>
    private static AppConfig? _cachedConfig;

    /// <summary>缓存读写锁，消除 UI 线程与 FileSystemWatcher 回调线程的竞态</summary>
    private static readonly object ConfigLock = new();

    /// <summary>保存失败提示是否已展示（成功保存后重置，避免重复弹窗）</summary>
    private static bool _saveFailureNotified;

    /// <summary>文件系统监视器，用于监听配置文件变更</summary>
    private static FileSystemWatcher? _configWatcher;

    /// <summary>配置变更事件，当配置文件被外部修改时触发</summary>
    public static event EventHandler<AppConfig>? ConfigChanged;

    /// <summary>
    /// 配置文件路径（%APPDATA%\Quanta\config.json）。
    /// exe 目录（如 Program Files）通常不可写，配置统一存放在用户目录。
    /// 若无法获取 APPDATA（异常环境），回退到 exe 目录。
    /// </summary>
    private static string ConfigPath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
            {
                return Path.Combine(appData, "Quanta", "config.json");
            }
            return LegacyConfigPath;
        }
    }

    /// <summary>
    /// 旧版配置文件路径（程序运行目录下的 config.json）。
    /// 仅用于首次启动时的一次性迁移；单文件发布时使用实际 exe 所在目录。
    /// </summary>
    private static string LegacyConfigPath
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
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Standard;

    /// <summary>
    /// 加载应用配置。优先从缓存返回，其次从 config.json 读取。
    /// 如果配置文件不存在，则创建默认配置；如果解析失败，
    /// 先把损坏文件改名为 config.json.bak-&lt;yyyyMMddHHmmss&gt; 再生成默认配置，绝不直接覆盖。
    /// </summary>
    /// <returns>加载的应用配置对象</returns>
    public static AppConfig Load()
    {
        lock (ConfigLock)
        {
            if (_cachedConfig != null)
            {
                Logger.Debug("Using cached config");
                return _cachedConfig;
            }

            try
            {
                // 首次启动：迁移 exe 目录下的旧配置到 %APPDATA%（保留原文件）
                MigrateLegacyConfigIfNeeded();

                // 获取绝对路径并打印
                var fullPath = Path.GetFullPath(ConfigPath);
                Logger.Debug($"Config file path: {fullPath}");
                Logger.Debug($"File exists: {File.Exists(ConfigPath)}");

                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Logger.Debug($"Config file content length: {json.Length} characters");

                    AppConfig? parsedConfig = null;
                    var parseFailed = false;
                    try
                    {
                        parsedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        parseFailed = true;
                        Logger.Error($"Failed to parse config: {ex.Message}", ex);
                    }

                    if (parsedConfig != null)
                    {
                        _cachedConfig = parsedConfig;
                        Logger.Debug($"Deserialized config - Commands count: {_cachedConfig.Commands?.Count ?? 0}");
                        if (_cachedConfig.Commands != null && _cachedConfig.Commands.Count > 0)
                        {
                            var commandKeywords = string.Join(", ", _cachedConfig.Commands.Select(c => $"{c.Keyword}({c.Name})"));
                            Logger.Debug($"Commands from file: {commandKeywords}");
                        }
                        else
                        {
                            Logger.Debug("No commands found in file");
                        }

                        // Migrate if needed
                        _cachedConfig = MigrateConfig(_cachedConfig);
                    }
                    else
                    {
                        // 解析失败或内容为 "null" 等无效配置：
                        // 先把损坏文件改名为 .bak 备份，再生成默认配置，绝不直接覆盖
                        if (!parseFailed)
                        {
                            Logger.Debug("Failed to deserialize config, creating default");
                        }
                        BackupCorruptConfig(parseFailed ? "parse error" : "empty config");
                        _cachedConfig = CreateDefaultConfig();
                    }
                }
                else
                {
                    Logger.Debug("Config file not found, creating default config");
                    _cachedConfig = CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load config: {ex.Message}", ex);
                // 读取失败时同样不覆盖原文件：先尝试备份再重建
                BackupCorruptConfig("load error");
                _cachedConfig = CreateDefaultConfig();
            }

            // 启动配置文件监视器（热重载）
            StartFileWatcher();

            return _cachedConfig!;
        }
    }

    /// <summary>
    /// 启动文件系统监视器，监听配置文件变更。
    /// </summary>
    private static void StartFileWatcher()
    {
        if (_configWatcher != null) return;

        try
        {
            var configDir = Path.GetDirectoryName(ConfigPath);
            if (string.IsNullOrEmpty(configDir) || !Directory.Exists(configDir))
            {
                Logger.Warn($"[ConfigLoader] Config directory does not exist: {configDir}");
                return;
            }

            _configWatcher = new FileSystemWatcher(configDir)
            {
                Filter = "config.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            // 使用 Debounce 避免连续触发
            DateTime _lastChanged = DateTime.MinValue;
            _configWatcher.Changed += (sender, e) =>
            {
                var now = DateTime.Now;
                // 500ms 内不重复处理
                if ((now - _lastChanged).TotalMilliseconds < 500) return;
                _lastChanged = now;

                Logger.Debug("[ConfigLoader] Config file changed, reloading...");

                // 延迟一点再读取，确保文件写入完成
                Task.Delay(100).ContinueWith(_ =>
                {
                    try
                    {
                        Reload();
                        var newConfig = Load();
                        ConfigChanged?.Invoke(null, newConfig);
                        Logger.Debug("[ConfigLoader] Config reloaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[ConfigLoader] Failed to reload config: {ex.Message}", ex);
                    }
                });
            };

            Logger.Debug("[ConfigLoader] FileSystemWatcher started");
        }
        catch (Exception ex)
        {
            Logger.Error($"[ConfigLoader] Failed to start FileSystemWatcher: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 停止文件系统监视器。
    /// </summary>
    public static void StopFileWatcher()
    {
        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Dispose();
            _configWatcher = null;
            Logger.Debug("[ConfigLoader] FileSystemWatcher stopped");
        }
    }

    /// <summary>
    /// 保存应用配置到 config.json。
    /// 采用原子写：先写临时文件，再 File.Move 覆盖目标文件，
    /// 崩溃/断电不会产生截断的损坏文件。
    /// 同时更新内存缓存。保存失败时记录 Error 日志并通过 Toast 提示一次。
    /// </summary>
    /// <param name="config">要保存的应用配置对象</param>
    public static void Save(AppConfig config)
    {
        try
        {
            // 保存时暂时禁用监视器，避免触发自身的 Changed 事件
            var wasEnabled = _configWatcher?.EnableRaisingEvents ?? false;
            if (_configWatcher != null) _configWatcher.EnableRaisingEvents = false;

            try
            {
                var json = JsonSerializer.Serialize(config, JsonOptions);
                AtomicWrite(ConfigPath, json);
            }
            finally
            {
                // 恢复监视器
                if (_configWatcher != null) _configWatcher.EnableRaisingEvents = wasEnabled;
            }

            lock (ConfigLock)
            {
                _cachedConfig = config;
                _saveFailureNotified = false;
            }

            Logger.Debug($"Config saved to: {ConfigPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save config: {ex.Message}", ex);
            NotifySaveFailed();
        }
    }

    /// <summary>
    /// 原子写入文件：先写入同目录下的临时文件，再通过 File.Move（overwrite）替换目标文件。
    /// 同目录保证同一卷，Move 在同一卷上是原子操作；写入中途崩溃最多残留临时文件，
    /// 目标文件要么是旧内容、要么是完整新内容，不会损坏。
    /// </summary>
    /// <param name="path">目标文件路径</param>
    /// <param name="contents">要写入的内容</param>
    private static void AtomicWrite(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        try
        {
            File.WriteAllText(tempPath, contents);
            File.Move(tempPath, path, true);
        }
        catch
        {
            // 清理残留的临时文件（可能被占用，尽力而为）
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch
            {
                // 忽略清理失败
            }
            throw;
        }
    }

    /// <summary>
    /// 把损坏的配置文件改名为 config.json.bak-&lt;yyyyMMddHHmmss&gt;，
    /// 保证生成默认配置时绝不直接覆盖用户的原文件。
    /// </summary>
    /// <param name="reason">损坏原因描述，用于日志</param>
    private static void BackupCorruptConfig(string reason)
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            // 同一秒内多次损坏时追加序号，避免重名
            var basePath = $"{ConfigPath}.bak-{DateTime.Now:yyyyMMddHHmmss}";
            var bakPath = basePath;
            var suffix = 1;
            while (File.Exists(bakPath))
            {
                bakPath = $"{basePath}-{suffix++}";
            }

            File.Move(ConfigPath, bakPath);
            Logger.Error($"[ConfigLoader] Corrupt config ({reason}) backed up to: {bakPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"[ConfigLoader] Failed to back up corrupt config: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 首次启动时把 exe 目录下的旧配置迁移到 %APPDATA%\Quanta\config.json。
    /// 仅在新位置尚无配置时复制，原文件保留不动。
    /// </summary>
    private static void MigrateLegacyConfigIfNeeded()
    {
        try
        {
            var legacyPath = LegacyConfigPath;
            if (ConfigPath == legacyPath || !File.Exists(legacyPath)) return;

            if (!File.Exists(ConfigPath))
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.Copy(legacyPath, ConfigPath);
                Logger.Debug($"[ConfigLoader] Migrated legacy config from {legacyPath} to {ConfigPath} (original kept)");
            }
        }
        catch (Exception ex)
        {
            // 迁移失败不阻断启动，按“配置文件不存在”流程生成默认配置
            Logger.Warn($"[ConfigLoader] Failed to migrate legacy config: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存失败时向用户提示一次（Toast + i18n），成功保存后重置，
    /// 避免受限目录下每次保存都弹窗打扰。日志始终记录 Error。
    /// </summary>
    private static void NotifySaveFailed()
    {
        if (_saveFailureNotified) return;
        _saveFailureNotified = true;

        try
        {
            var app = Application.Current;
            if (app == null) return; // UI 尚未初始化（极早期启动），仅记录日志

            // 异步派发到 UI 线程，避免与配置锁互相等待
            app.Dispatcher.BeginInvoke(() =>
                ToastService.Instance.ShowError(LocalizationService.Get("ConfigSaveFailed"), 3));
        }
        catch (Exception ex)
        {
            Logger.Warn($"[ConfigLoader] Failed to show save-failure toast: {ex.Message}");
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
            Logger.Debug($"Config exported to: {path}");
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
        lock (ConfigLock)
        {
            _cachedConfig = null;
        }
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
            Hotkey = new HotkeyConfig { Modifier = "Alt", Key = "R" },
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
            Logger.Debug("Migrating config to v1.0...");

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
            Logger.Debug("Migrating config to v1.1: updating recording defaults...");

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
            Logger.Debug("Migrating config to v1.2: normalizing SampleRate...");

            if (config.RecordingSettings == null)
            {
                config.RecordingSettings = new RecordingSettings();
            }
            else if (config.RecordingSettings.SampleRate == 44100)
            {
                config.RecordingSettings.SampleRate = 16000;
                Logger.Debug("RecordingSettings.SampleRate: 44100 → 16000");
            }

            config.Version = "1.2";
            Save(config);
        }

        // v1.2 → v1.3：添加汇率 API 设置
        if (config.Version == "1.2")
        {
            Logger.Debug("Migrating config to v1.3: adding exchange rate settings...");

            if (config.ExchangeRateSettings == null)
            {
                // 不注入默认 API Key，由用户在设置页自行配置
                config.ExchangeRateSettings = new Models.ExchangeRateSettings();
            }

            config.Version = "1.3";
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
