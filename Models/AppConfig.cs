using System.Text.Json.Serialization;

namespace Quanta.Models;

public class AppConfig
{
    [JsonPropertyName("Hotkey")] public HotkeyConfig Hotkey { get; set; } = new();
    [JsonPropertyName("Theme")] public string Theme { get; set; } = "Light";
    [JsonPropertyName("Commands")] public List<CommandConfig> Commands { get; set; } = new();
}

public class HotkeyConfig
{
    [JsonPropertyName("Modifier")] public string Modifier { get; set; } = "Alt";
    [JsonPropertyName("Key")] public string Key { get; set; } = "Space";
}

public class CommandConfig
{
    [JsonPropertyName("Keyword")] public string Keyword { get; set; } = string.Empty;
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("Type")] public string Type { get; set; } = "Url";
    [JsonPropertyName("Path")] public string Path { get; set; } = string.Empty;
}
