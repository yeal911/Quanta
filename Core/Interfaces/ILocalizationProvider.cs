using Quanta.Models;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 本地化读取抽象，屏蔽静态 LocalizationService。
/// </summary>
public interface ILocalizationProvider
{
    string CurrentLanguage { get; set; }
    string Get(string key);
    string Get(string key, params object[] args);
    IReadOnlyList<LanguageInfo> GetSupportedLanguages();
}
