// ============================================================================
// 文件名: TestResources.cs
// 文件描述: 测试辅助类。从 i18n 资源文件按键取值，供断言使用。
//           测试中的用户可见文案断言一律通过资源 key 取值，禁止硬编码文案。
// ============================================================================

using System.Text.Json;

namespace Quanta.Tests;

/// <summary>
/// i18n 资源读取辅助类，加载随测试工程复制的 zh-CN.json 并按键取值。
/// </summary>
public static class TestResources
{
    private static readonly Lazy<Dictionary<string, string>> _zhCn = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Strings", "zh-CN.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
               ?? throw new InvalidOperationException("Failed to load zh-CN.json test resource");
    });

    /// <summary>
    /// 获取 zh-CN 资源中指定 key 的文案；key 不存在时返回 key 本身（与 LocalizationService 行为一致）。
    /// </summary>
    /// <param name="key">资源 key</param>
    /// <returns>对应的文案</returns>
    public static string GetZhCn(string key) =>
        _zhCn.Value.TryGetValue(key, out var value) ? value : key;
}
