using System.Text.Json.Serialization;

namespace Quanta.Models;

public enum CommandType
{
    Url,        // 打开网页
    Program,    // 启动程序
    Directory,  // 打开目录
    Shell,      // 执行命令
    Calculator  // 计算器
}

public class AppConfig
{
    [JsonPropertyName("Hotkey")] public HotkeyConfig Hotkey { get; set; } = new();
    [JsonPropertyName("SearchDirectories")] public List<string> SearchDirectories { get; set; } = new();
    [JsonPropertyName("MaxRecentFiles")] public int MaxRecentFiles { get; set; } = 30;
    [JsonPropertyName("AutoStart")] public bool AutoStart { get; set; } = false;
    [JsonPropertyName("Commands")] public List<CommandConfig> Commands { get; set; } = new();
    [JsonPropertyName("Theme")] public string Theme { get; set; } = "Dark";
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
    [JsonPropertyName("Arguments")] public string Arguments { get; set; } = string.Empty;
}
