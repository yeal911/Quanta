// ============================================================================
// 文件名: LocalizationService.cs
// 描述: 本地化（国际化）服务，提供多语言文本翻译功能。
//       支持简体中文（zh-CN）、英文（en-US）、西班牙语（es-ES）等多种语言。
//       语言设置会自动持久化到应用配置文件中。
//       翻译文本存储在 Resources/Strings/*.json 文件中。
//       支持外部语言包：exe 同目录下 Resources/Strings/xx-XX.json
// ============================================================================

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 本地化服务（静态类），提供应用程序的多语言翻译支持。
/// 通过从嵌入的 JSON 文件加载翻译字典管理多语言文本，支持语言切换和从配置文件加载语言设置。
/// 当找不到当前语言的翻译时，会自动回退到中文（zh-CN）。
/// 支持外部语言包：exe 同目录下 Resources/Strings/xx-XX.json
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// 多语言翻译字典缓存，外层 Key 为语言代码（如 zh-CN、en-US），
    /// 内层 Key 为翻译键名，Value 为对应的翻译文本
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> _translations = new();

    /// <summary>
    /// 已加载的语言代码列表
    /// </summary>
    private static readonly string[] SupportedLanguageCodes = { "zh-CN", "en-US", "es-ES", "ja-JP", "ko-KR", "fr-FR", "de-DE", "pt-BR", "ru-RU", "it-IT", "ar-SA" };

    /// <summary>
    /// 静态构造函数 - 启动时加载所有语言文件
    /// </summary>
    static LocalizationService()
    {
        LoadAllLanguages();
    }

    /// <summary>
    /// 从嵌入的 JSON 资源中加载所有语言
    /// 优先级：外部文件 > 嵌入式资源
    /// </summary>
    private static void LoadAllLanguages()
    {
        _translations = new Dictionary<string, Dictionary<string, string>>();

        foreach (var langCode in SupportedLanguageCodes)
        {
            try
            {
                var dict = LoadLanguage(langCode);
                if (dict != null)
                {
                    _translations[langCode] = dict;
                    Logger.Debug($"[LocalizationService] Loaded language: {langCode} ({dict.Count} keys)");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warn($"[LocalizationService] Failed to load language {langCode}: {ex.Message}");
            }
        }

        // 确保至少加载了默认语言
        if (!_translations.ContainsKey("zh-CN"))
        {
            throw new System.InvalidOperationException("Failed to load default language (zh-CN)");
        }

        Logger.Debug($"[LocalizationService] Total languages loaded: {_translations.Count}");
    }

    /// <summary>
    /// 加载指定语言的翻译字典（外部文件优先）
    /// </summary>
    private static Dictionary<string, string>? LoadLanguage(string languageCode)
    {
        // 1. 优先尝试从外部文件加载
        var externalPath = GetExternalLanguageFilePath(languageCode);
        if (File.Exists(externalPath))
        {
            try
            {
                var json = File.ReadAllText(externalPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
                Logger.Debug($"[LocalizationService] Loaded external: {externalPath}");
                return dict;
            }
            catch (System.Exception ex)
            {
                Logger.Warn($"[LocalizationService] Failed load external {externalPath}: {ex.Message}");
            }
        }

        // 2. 回退到嵌入式资源
        return LoadFromEmbeddedResource(languageCode);
    }

    /// <summary>
    /// 从嵌入的 JSON 资源加载指定语言
    /// </summary>
    private static Dictionary<string, string>? LoadFromEmbeddedResource(string languageCode)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Quanta.Resources.Strings.{languageCode}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Logger.Warn($"[LocalizationService] Resource not found: {resourceName}");
            return null;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
    }

    /// <summary>
    /// 获取外部语言文件路径
    /// 格式：exe目录/Resources/Strings/xx-XX.json
    /// </summary>
    private static string GetExternalLanguageFilePath(string languageCode)
    {
        return Path.Combine(AppContext.BaseDirectory, "Resources", "Strings", $"{languageCode}.json");
    }

    /// <summary>
    /// 重新加载所有语言（用于刷新外部语言包）
    /// </summary>
    public static void ReloadAllLanguages()
    {
        LoadAllLanguages();
    }

    /// <summary>
    /// 当前使用的语言代码，默认为简体中文
    /// </summary>
    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 获取或设置当前语言代码。
    /// 设置时会验证语言是否受支持，并自动将选择持久化到配置文件中。
    /// </summary>
    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_translations.ContainsKey(value))
            {
                _currentLanguage = value;
                // 将语言设置保存到配置文件
                var config = ConfigLoader.Load();
                if (config.AppSettings == null) config.AppSettings = new AppSettings();
                config.AppSettings.Language = value;
                ConfigLoader.Save(config);
            }
        }
    }

    /// <summary>
    /// 从应用配置文件中加载语言设置。
    /// 在应用启动时调用，以恢复用户之前选择的语言。
    /// </summary>
    public static void LoadFromConfig()
    {
        var config = ConfigLoader.Load();
        if (config.AppSettings != null && !string.IsNullOrEmpty(config.AppSettings.Language))
        {
            _currentLanguage = config.AppSettings.Language;
        }
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本。
    /// 如果当前语言中找不到对应翻译，会回退到中文（zh-CN）。
    /// 如果中文中也找不到，则直接返回键名本身。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <returns>对应的翻译文本</returns>
    public static string Get(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        // 回退到中文翻译
        if (_translations.TryGetValue("zh-CN", out var fallbackDict))
        {
            if (fallbackDict.TryGetValue(key, out var fallback))
            {
                return fallback;
            }
        }
        return key;
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本，并使用参数进行格式化。
    /// 适用于包含占位符（如 {0}、{1}）的翻译模板。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的翻译文本</returns>
    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // 动态语言支持 - 统一语言管理
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取所有支持的语言列表
    /// </summary>
    public static IReadOnlyList<LanguageInfo> GetSupportedLanguages()
    {
        return LanguageManager.SupportedLanguages;
    }

    /// <summary>
    /// 获取语言显示名称的翻译键名
    /// </summary>
    public static string GetLanguageDisplayKey(string languageCode)
    {
        return languageCode switch
        {
            "zh-CN" => "TrayChinese",
            "en-US" => "TrayEnglish",
            "es-ES" => "TraySpanish",
            "ja-JP" => "TrayJapanese",
            "ko-KR" => "TrayKorean",
            "fr-FR" => "TrayFrench",
            "de-DE" => "TrayGerman",
            "pt-BR" => "TrayPortuguese",
            "ru-RU" => "TrayRussian",
            "it-IT" => "TrayItalian",
            "ar-SA" => "TrayArabic",
            _ => "TrayLanguage"
        };
    }

    /// <summary>
    /// 设置语言（带验证）
    /// </summary>
    public static bool TrySetLanguage(string languageCode)
    {
        if (!_translations.ContainsKey(languageCode))
            return false;

        CurrentLanguage = languageCode;
        return true;
    }
}
