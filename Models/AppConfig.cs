using System.Text.Json.Serialization;

namespace Quanta.Models;

public class AppConfig
{
    [JsonPropertyName("Version")] public string Version { get; set; } = "1.0";
    [JsonPropertyName("Hotkey")] public HotkeyConfig Hotkey { get; set; } = new();
    [JsonPropertyName("Theme")] public string Theme { get; set; } = "Light";
    [JsonPropertyName("Commands")] public List<CommandConfig> Commands { get; set; } = new();
    [JsonPropertyName("CommandGroups")] public List<CommandGroup> CommandGroups { get; set; } = new();
    [JsonPropertyName("PluginSettings")] public PluginSettings PluginSettings { get; set; } = new();
    [JsonPropertyName("AppSettings")] public AppSettings AppSettings { get; set; } = new();
}

public class HotkeyConfig
{
    [JsonPropertyName("Modifier")] public string Modifier { get; set; } = "Alt";
    [JsonPropertyName("Key")] public string Key { get; set; } = "Space";
}

public class CommandConfig
{
    [JsonPropertyName("Id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("Keyword")] public string Keyword { get; set; } = string.Empty;
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("Type")] public string Type { get; set; } = "Url";
    [JsonPropertyName("Path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("GroupId")] public string? GroupId { get; set; }
    
    // Advanced capabilities
    [JsonPropertyName("Arguments")] public string Arguments { get; set; } = string.Empty;
    [JsonPropertyName("WorkingDirectory")] public string WorkingDirectory { get; set; } = string.Empty;
    [JsonPropertyName("RunAsAdmin")] public bool RunAsAdmin { get; set; }
    [JsonPropertyName("RunHidden")] public bool RunHidden { get; set; }
    [JsonPropertyName("IconPath")] public string IconPath { get; set; } = string.Empty;
    [JsonPropertyName("Hotkey")] public string? Hotkey { get; set; }
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("ModifiedAt")] public DateTime ModifiedAt { get; set; } = DateTime.Now;

    // For parameter substitution
    [JsonPropertyName("ParamPlaceholder")] public string ParamPlaceholder { get; set; } = "{param}";
}

public class CommandGroup
{
    [JsonPropertyName("Id")] public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("Icon")] public string Icon { get; set; } = "📁";
    [JsonPropertyName("Color")] public string Color { get; set; } = "#0078D4";
    [JsonPropertyName("SortOrder")] public int SortOrder { get; set; }
    [JsonPropertyName("Expanded")] public bool Expanded { get; set; } = true;
}

public class PluginSettings
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("PluginDirectory")] public string PluginDirectory { get; set; } = "Plugins";
    [JsonPropertyName("LoadedPlugins")] public List<string> LoadedPlugins { get; set; } = new();
}

public class AppSettings
{
    [JsonPropertyName("StartWithWindows")] public bool StartWithWindows { get; set; }
    [JsonPropertyName("MinimizeToTray")] public bool MinimizeToTray { get; set; } = true;
    [JsonPropertyName("CloseToTray")] public bool CloseToTray { get; set; } = true;
    [JsonPropertyName("ShowInTaskbar")] public bool ShowInTaskbar { get; set; } = false;
    [JsonPropertyName("MaxResults")] public int MaxResults { get; set; } = 10;
    [JsonPropertyName("AutoUpdate")] public bool AutoUpdate { get; set; } = true;
    [JsonPropertyName("CheckForUpdatesOnStartup")] public bool CheckForUpdatesOnStartup { get; set; } = true;
    [JsonPropertyName("Language")] public string Language { get; set; } = "zh-CN";
}
