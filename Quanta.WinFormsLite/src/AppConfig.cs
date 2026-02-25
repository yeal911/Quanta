using System.Text.Json.Serialization;

namespace Quanta.WinFormsLite;

public sealed class AppConfig
{
    [JsonPropertyName("Hotkey")]
    public HotkeyConfig Hotkey { get; set; } = new();

    [JsonPropertyName("Theme")]
    public string Theme { get; set; } = "Light";

    [JsonPropertyName("Commands")]
    public List<CommandItem> Commands { get; set; } = new();

    [JsonPropertyName("AppSettings")]
    public AppSettings AppSettings { get; set; } = new();
}

public sealed class HotkeyConfig
{
    [JsonPropertyName("Modifier")]
    public string Modifier { get; set; } = "Alt";

    [JsonPropertyName("Key")]
    public string Key { get; set; } = "R";
}

public sealed class AppSettings
{
    [JsonPropertyName("StartWithWindows")]
    public bool StartWithWindows { get; set; }

    [JsonPropertyName("MaxResults")]
    public int MaxResults { get; set; } = 8;

    [JsonPropertyName("EnableFileSearch")]
    public bool EnableFileSearch { get; set; } = true;

    [JsonPropertyName("FileSearchRoots")]
    public List<string> FileSearchRoots { get; set; } = new();

    [JsonPropertyName("FileScanLimit")]
    public int FileScanLimit { get; set; } = 300;
}

public sealed class CommandItem
{
    [JsonPropertyName("Keyword")]
    public string Keyword { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = "Url";

    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;
}
