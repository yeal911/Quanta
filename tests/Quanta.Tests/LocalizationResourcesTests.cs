// ============================================================================
// 文件名: LocalizationResourcesTests.cs
// 文件描述: i18n 资源完整性测试（POLA-29）。
//           验证 11 个语言文件 key 集一致、无空值，
//           且除明确豁免（语言中立技术词 / 目标语言同形词）外
//           不存在"值 = 英文原文"的未翻译残留条目。
// ============================================================================

using System.Text.Json;
using Quanta.Core;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// 本地化资源文件的完整性测试。
/// 资源文件随测试工程复制到 Resources/Strings/，直接反序列化扫描。
/// </summary>
public class LocalizationResourcesTests
{
    /// <summary>全部支持的语言代码，与 LocalizationManager 支持列表一致</summary>
    private static readonly string[] LanguageCodes =
    {
        "zh-CN", "en-US", "es-ES", "ja-JP", "ko-KR", "fr-FR",
        "de-DE", "pt-BR", "ru-RU", "it-IT", "ar-SA"
    };

    /// <summary>
    /// 语言中立 key：en-US 文案为专有名词、技术术语或语言原生名称，
    /// 所有语言按设计保持原文（如 PowerShell、m4a、各语言的原生名称）。
    /// </summary>
    private static readonly IReadOnlySet<string> LanguageNeutralKeys = new HashSet<string>
    {
        // 语言菜单中的各语言原生名称
        "TrayChinese", "TrayEnglish", "TraySpanish", "TrayJapanese", "TrayKorean",
        "TrayFrench", "TrayGerman", "TrayPortuguese", "TrayRussian", "TrayItalian", "TrayArabic",
        // 录制格式与码率（技术单位）
        "RecordFormatM4a", "RecordFormatMp3",
        "RecordBitrate32", "RecordBitrate64", "RecordBitrate96", "RecordBitrate128", "RecordBitrate160",
        // 专有名词 / 品牌名
        "BuiltinCmd_powershell", "BuiltinCmd_ping", "GroupQuanta", "TrayTooltip"
    };

    /// <summary>
    /// 各语言中与英文拼写相同的合法同形词 key（该语言的标准写法恰好与英文一致，
    /// 如德语 Name/Format、法语 Type/Source、各语言的 Web/Cache/Euro/Paint）。
    /// 除此之外的"值 = 英文原文"条目视为未翻译残留，测试失败。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> SameWordKeysByLanguage =
        new Dictionary<string, IReadOnlySet<string>>
        {
            ["zh-CN"] = new HashSet<string>(),
            ["es-ES"] = new HashSet<string>
            {
                "BuiltinCmd_mspaint", "Currency_EUR", "GeneralSettings", "GroupWeb", "MenuGeneral",
                "RecordChannelMono", "RecordChannelsMono", "RecordEstimatedSizeUnit", "RecordEstimatedSizeUnitMb"
            },
            ["ja-JP"] = new HashSet<string> { "BuiltinCmd_mspaint" },
            ["ko-KR"] = new HashSet<string> { "BuiltinCmd_mspaint" },
            ["fr-FR"] = new HashSet<string>
            {
                "BuiltinCmd_mspaint", "BuiltinCmd_services", "Currency_EUR", "Email", "ExchangeRateFromCache",
                "GroupQRCode", "GroupWeb", "RecordChannelMono", "RecordChannelsMono",
                "RecordFormat", "RecordSource", "Type"
            },
            ["de-DE"] = new HashSet<string>
            {
                "BuiltinCmd_mspaint", "Currency_EUR", "ExchangeRateFromCache", "GroupApp", "GroupSystem",
                "GroupWeb", "Name", "RecordBitrate", "RecordChannelMono", "RecordChannelStereo",
                "RecordChannelsMono", "RecordChannelsStereo", "RecordEstimatedSizeUnit",
                "RecordEstimatedSizeUnitMb", "RecordFormat"
            },
            ["pt-BR"] = new HashSet<string>
            {
                "BuiltinCmd_mspaint", "Currency_EUR", "ExchangeRateFromCache", "GroupWeb",
                "RecordChannelMono", "RecordChannelsMono", "RecordEstimatedSizeUnit", "RecordEstimatedSizeUnitMb"
            },
            ["ru-RU"] = new HashSet<string> { "BuiltinCmd_mspaint", "Email" },
            ["it-IT"] = new HashSet<string>
            {
                "BuiltinCmd_mspaint", "Currency_EUR", "Email", "ExchangeRateFromCache", "GroupWeb",
                "RecordBitrate", "RecordChannelMono", "RecordChannelStereo", "RecordChannelsMono",
                "RecordChannelsStereo", "RecordEstimatedSizeUnit", "RecordEstimatedSizeUnitMb"
            },
            ["ar-SA"] = new HashSet<string> { "BuiltinCmd_mspaint" },
        };

    /// <summary>所有语言文件的反序列化结果（懒加载缓存）</summary>
    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> Translations = new(() =>
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        foreach (var code in LanguageCodes)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Strings", $"{code}.json");
            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? throw new InvalidOperationException($"Failed to load {code}.json test resource");
            result[code] = dict;
        }
        return result;
    });

    /// <summary>
    /// 11 个语言文件的 key 集合必须完全一致（数量与内容）。
    /// </summary>
    [Fact]
    public void AllLanguageFiles_HaveIdenticalKeySets()
    {
        var reference = Translations.Value["en-US"].Keys.ToHashSet();

        foreach (var (code, dict) in Translations.Value)
        {
            var missing = reference.Except(dict.Keys).ToList();
            var extra = dict.Keys.Except(reference).ToList();
            Assert.True(
                missing.Count == 0 && extra.Count == 0,
                $"{code}: key set mismatch. missing=[{string.Join(", ", missing)}] extra=[{string.Join(", ", extra)}]");
        }
    }

    /// <summary>
    /// 任何语言的任何 key 都不允许空值（空值会在运行时表现为空白文案）。
    /// </summary>
    [Fact]
    public void AllValues_AreNotEmpty()
    {
        var empty = new List<string>();
        foreach (var (code, dict) in Translations.Value)
        {
            empty.AddRange(dict.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => $"{code}: {kv.Key}"));
        }

        Assert.True(empty.Count == 0, $"Empty values found: {string.Join("; ", empty)}");
    }

    /// <summary>
    /// 非英语语言中，"值 = en-US 原文"的条目必须属于豁免清单
    /// （语言中立技术词或该语言的合法同形词），否则判定为未翻译的英文残留。
    /// </summary>
    [Fact]
    public void NonEnglishLanguages_HaveNoUntranslatedEnglishResidue()
    {
        var en = Translations.Value["en-US"];
        var residue = new List<string>();

        foreach (var (code, dict) in Translations.Value)
        {
            if (code == "en-US") continue;

            SameWordKeysByLanguage.TryGetValue(code, out var sameWord);
            foreach (var (key, value) in dict)
            {
                if (!en.TryGetValue(key, out var enValue) || string.IsNullOrWhiteSpace(enValue)) continue;
                if (!string.Equals(value, enValue, StringComparison.Ordinal)) continue;
                if (LanguageNeutralKeys.Contains(key)) continue;
                if (sameWord != null && sameWord.Contains(key)) continue;

                residue.Add($"{code}: {key} = \"{value}\"");
            }
        }

        Assert.True(
            residue.Count == 0,
            $"Untranslated English residue found (value equals en-US text outside the allowlist):\n{string.Join("\n", residue)}");
    }

    /// <summary>
    /// 语言元数据中仅 ar-SA（阿拉伯语）标记为 RTL 布局语言，
    /// 且支持语言总数为 11，与资源文件一一对应。
    /// </summary>
    [Fact]
    public void OnlyArabic_IsMarkedRightToLeft()
    {
        Assert.Equal(LanguageCodes.Length, LanguageManager.SupportedLanguages.Count);

        foreach (var lang in LanguageManager.SupportedLanguages)
        {
            if (lang.Code == "ar-SA")
                Assert.True(lang.IsRightToLeft, "ar-SA must be marked RightToLeft for RTL layout");
            else
                Assert.False(lang.IsRightToLeft, $"{lang.Code} must not be marked RightToLeft");
        }
    }
}
