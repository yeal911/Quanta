using Quanta.Core.Interfaces;
using Quanta.Models;
using Quanta.Services;

namespace Quanta.Infrastructure.System;

public sealed class LocalizationProvider : ILocalizationProvider
{
    public string CurrentLanguage
    {
        get => LocalizationService.CurrentLanguage;
        set => LocalizationService.CurrentLanguage = value;
    }

    public string Get(string key) => LocalizationService.Get(key);
    public string Get(string key, params object[] args) => LocalizationService.Get(key, args);
    public IReadOnlyList<LanguageInfo> GetSupportedLanguages() => LocalizationService.GetSupportedLanguages();
}
