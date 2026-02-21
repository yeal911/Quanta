// ============================================================================
// 文件名: UsageData.cs
// 文件用途: 定义用户使用数据的模型类，用于记录和持久化每条命令或搜索结果的
//          使用频率和最后使用时间，以便实现智能排序和使用习惯分析。
// ============================================================================

using System.Text.Json.Serialization;

namespace Quanta.Models;

/// <summary>
/// 使用数据根模型类，包含所有命令/结果的使用统计信息。
/// 键为命令或结果的唯一标识符，值为对应的使用统计项。
/// </summary>
public class UsageData
{
    /// <summary>使用统计项字典，键为命令/结果 ID，值为使用统计详情</summary>
    [JsonPropertyName("items")] public Dictionary<string, UsageItem> Items { get; set; } = new();
}

/// <summary>
/// 单条使用统计项，记录某个命令或搜索结果的使用频率和最后使用时间。
/// </summary>
public class UsageItem
{
    /// <summary>累计使用次数</summary>
    [JsonPropertyName("usageCount")] public int UsageCount { get; set; }

    /// <summary>最后一次使用的时间</summary>
    [JsonPropertyName("lastUsedTime")] public DateTime LastUsedTime { get; set; }
}
