using System.Text.Json.Serialization;

namespace Quanta.Models;

public class UsageData
{
    [JsonPropertyName("items")] public Dictionary<string, UsageItem> Items { get; set; } = new();
}

public class UsageItem
{
    [JsonPropertyName("usageCount")] public int UsageCount { get; set; }
    [JsonPropertyName("lastUsedTime")] public DateTime LastUsedTime { get; set; }
}
