// ============================================================================
// 文件名: LanguageInfo.cs
// 描述: 语言定义配置类，统一管理所有支持的语言信息
// ============================================================================

namespace Quanta.Models;

/// <summary>
/// 语言定义信息
/// </summary>
public class LanguageInfo
{
    /// <summary>语言代码，如 "zh-CN", "en-US", "es-ES"</summary>
    public string Code { get; init; } = "";

    /// <summary>语言显示名称（本地化）</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>语言在菜单中的显示名称（使用自身语言）</summary>
    public string NativeName { get; init; } = "";

    /// <summary>语言是否启用</summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// 语言配置管理器，提供所有支持的语言列表和查找功能
/// </summary>
public static class LanguageManager
{
    /// <summary>
    /// 所有支持的语言列表
    /// </summary>
    public static readonly IReadOnlyList<LanguageInfo> SupportedLanguages = new List<LanguageInfo>
    {
        new() { Code = "zh-CN", DisplayName = "简体中文", NativeName = "中文" },
        new() { Code = "en-US", DisplayName = "English", NativeName = "English" },
        new() { Code = "es-ES", DisplayName = "Español", NativeName = "Español" },
        new() { Code = "ja-JP", DisplayName = "日本語", NativeName = "日本語" },
        new() { Code = "ko-KR", DisplayName = "한국어", NativeName = "한국어" },
        new() { Code = "fr-FR", DisplayName = "Français", NativeName = "Français" },
        new() { Code = "de-DE", DisplayName = "Deutsch", NativeName = "Deutsch" },
        new() { Code = "pt-BR", DisplayName = "Português", NativeName = "Português" },
        new() { Code = "ru-RU", DisplayName = "Русский", NativeName = "Русский" },
        new() { Code = "it-IT", DisplayName = "Italiano", NativeName = "Italiano" },
        new() { Code = "ar-SA", DisplayName = "العربية", NativeName = "العربية" }
    }.AsReadOnly();

    /// <summary>
    /// 根据语言代码获取语言信息
    /// </summary>
    public static LanguageInfo? GetLanguage(string code)
    {
        return SupportedLanguages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 检查语言代码是否有效
    /// </summary>
    public static bool IsValidLanguage(string code)
    {
        return SupportedLanguages.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有启用的语言列表
    /// </summary>
    public static IEnumerable<LanguageInfo> GetEnabledLanguages()
    {
        return SupportedLanguages.Where(l => l.Enabled);
    }
}
