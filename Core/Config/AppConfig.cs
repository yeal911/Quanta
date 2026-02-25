// ============================================================================
// 文件名: AppConfig.cs
// 文件用途: 定义应用程序的所有配置模型类，包括主配置、快捷键配置、命令配置、
//          命令分组、插件设置和应用程序通用设置。这些模型用于 JSON 序列化/反序列化，
//          对应配置文件 config.json 的数据结构。
// ============================================================================

using System.Text.Json.Serialization;

namespace Quanta.Models;

/// <summary>
/// 汇率转换设置类
/// </summary>
public class ExchangeRateSettings
{
    /// <summary>Exchangerate-API 的 API Key</summary>
    [JsonPropertyName("ApiKey")] public string ApiKey { get; set; } = "f02c1174e7cdfb412a48337c";

    /// <summary>缓存时间（分钟）</summary>
    [JsonPropertyName("CacheMinutes")] public int CacheMinutes { get; set; } = 60;
}

/// <summary>
/// 应用程序主配置类，作为整个配置文件的根节点，包含所有子配置项。
/// </summary>
public class AppConfig
{
    /// <summary>配置文件版本号</summary>
    [JsonPropertyName("Version")] public string Version { get; set; } = "1.0";

    /// <summary>全局快捷键配置，用于唤起搜索窗口</summary>
    [JsonPropertyName("Hotkey")] public HotkeyConfig Hotkey { get; set; } = new();

    /// <summary>界面主题，如 "Light" 或 "Dark"</summary>
    [JsonPropertyName("Theme")] public string Theme { get; set; } = "Light";

    /// <summary>用户自定义命令列表</summary>
    [JsonPropertyName("Commands")] public List<CommandConfig> Commands { get; set; } = new();

    /// <summary>命令分组列表，用于对命令进行归类管理</summary>
    [JsonPropertyName("CommandGroups")] public List<CommandGroup> CommandGroups { get; set; } = new();

    /// <summary>插件相关设置</summary>
    [JsonPropertyName("PluginSettings")] public PluginSettings PluginSettings { get; set; } = new();

    /// <summary>应用程序通用设置</summary>
    [JsonPropertyName("AppSettings")] public AppSettings AppSettings { get; set; } = new();

    /// <summary>录音设置</summary>
    [JsonPropertyName("RecordingSettings")] public RecordingSettings RecordingSettings { get; set; } = new();

    /// <summary>汇率API设置</summary>
    [JsonPropertyName("ExchangeRateSettings")] public ExchangeRateSettings ExchangeRateSettings { get; set; } = new();

    /// <summary>文件搜索设置</summary>
    [JsonPropertyName("FileSearchSettings")] public FileSearchSettings FileSearchSettings { get; set; } = new();
}

/// <summary>
/// 快捷键配置类，定义唤起应用程序搜索窗口的全局热键组合。
/// </summary>
public class HotkeyConfig
{
    /// <summary>修饰键，如 "Alt"、"Ctrl"、"Shift" 等</summary>
    [JsonPropertyName("Modifier")] public string Modifier { get; set; } = "Alt";

    /// <summary>主键，如 "Space"、"Q" 等</summary>
    [JsonPropertyName("Key")] public string Key { get; set; } = "R";
}

/// <summary>
/// 自定义命令配置类，表示用户创建的单条命令（如打开网址、启动程序等）。
/// </summary>
public class CommandConfig
{
    /// <summary>命令唯一标识符（GUID）</summary>
    [JsonPropertyName("Id")] public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>触发命令的关键字，用户在搜索栏中输入该关键字即可匹配</summary>
    [JsonPropertyName("Keyword")] public string Keyword { get; set; } = string.Empty;

    /// <summary>命令显示名称</summary>
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;

    /// <summary>命令类型，如 "Url"（网址）、"App"（应用程序）等</summary>
    [JsonPropertyName("Type")] public string Type { get; set; } = "Url";

    /// <summary>命令执行路径或 URL 地址</summary>
    [JsonPropertyName("Path")] public string Path { get; set; } = string.Empty;

    /// <summary>所属分组的 ID，为空表示未分组</summary>
    [JsonPropertyName("GroupId")] public string? GroupId { get; set; }

    /// <summary>显示序号，仅用于界面展示，不参与序列化</summary>
    [JsonIgnore] public int Index { get; set; }

    /// <summary>命令行启动参数</summary>
    [JsonPropertyName("Arguments")] public string Arguments { get; set; } = string.Empty;

    /// <summary>工作目录路径</summary>
    [JsonPropertyName("WorkingDirectory")] public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>是否以管理员权限运行</summary>
    [JsonPropertyName("RunAsAdmin")] public bool RunAsAdmin { get; set; }

    /// <summary>是否隐藏窗口运行</summary>
    [JsonPropertyName("RunHidden")] public bool RunHidden { get; set; }

    /// <summary>自定义图标文件路径</summary>
    [JsonPropertyName("IconPath")] public string IconPath { get; set; } = string.Empty;

    /// <summary>命令专属快捷键</summary>
    [JsonPropertyName("Hotkey")] public string? Hotkey { get; set; }

    /// <summary>命令是否启用</summary>
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;

    /// <summary>命令描述信息</summary>
    [JsonPropertyName("Description")] public string Description { get; set; } = string.Empty;

    /// <summary>最后修改时间</summary>
    [JsonPropertyName("ModifiedAt")] public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>是否为系统预置命令（预置命令不会被保存到配置文件）</summary>
    [JsonPropertyName("IsBuiltIn")] public bool IsBuiltIn { get; set; } = false;

    /// <summary>参数替换占位符，用于在路径或参数中动态替换用户输入</summary>
    [JsonPropertyName("ParamPlaceholder")] public string ParamPlaceholder { get; set; } = "{param}";

    /// <summary>分组标识键（国际化 key），仅内置命令使用，不序列化到配置文件</summary>
    [JsonIgnore] public string GroupKey { get; set; } = "";
}

/// <summary>
/// 命令分组类，用于将多个命令归类到同一分组中，便于管理和展示。
/// </summary>
public class CommandGroup
{
    /// <summary>分组唯一标识符（GUID）</summary>
    [JsonPropertyName("Id")] public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>分组显示名称</summary>
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;

    /// <summary>分组图标（Emoji 或图标路径）</summary>
    [JsonPropertyName("Icon")] public string Icon { get; set; } = "\U0001f4c1";

    /// <summary>分组主题颜色（十六进制颜色值）</summary>
    [JsonPropertyName("Color")] public string Color { get; set; } = "#0078D4";

    /// <summary>排序顺序，数值越小越靠前</summary>
    [JsonPropertyName("SortOrder")] public int SortOrder { get; set; }

    /// <summary>分组是否展开显示</summary>
    [JsonPropertyName("Expanded")] public bool Expanded { get; set; } = true;
}

/// <summary>
/// 插件设置类，控制插件系统的启用状态和加载行为。
/// </summary>
public class PluginSettings
{
    /// <summary>是否启用插件系统</summary>
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;

    /// <summary>插件存放目录路径</summary>
    [JsonPropertyName("PluginDirectory")] public string PluginDirectory { get; set; } = "Plugins";

    /// <summary>已加载的插件名称列表</summary>
    [JsonPropertyName("LoadedPlugins")] public List<string> LoadedPlugins { get; set; } = new();
}

/// <summary>
/// 录音设置类，存储录音功能的所有配置选项。
/// 默认参数针对会议录音优化：AAC 16kHz 单声道 32kbps ≈ 0.24MB/min。
/// </summary>
public class RecordingSettings
{
    /// <summary>录制源：Mic（麦克风）、Speaker（扬声器回环）、Mic&amp;Speaker（混合）</summary>
    [JsonPropertyName("Source")] public string Source { get; set; } = "Mic";

    /// <summary>输出格式：m4a 或 mp3</summary>
    [JsonPropertyName("Format")] public string Format { get; set; } = "m4a";

    /// <summary>采样率（Hz）：8000、16000、22050、44100、48000</summary>
    [JsonPropertyName("SampleRate")] public int SampleRate { get; set; } = 16000;

    /// <summary>码率（kbps）：32、64、96、128、160</summary>
    [JsonPropertyName("Bitrate")] public int Bitrate { get; set; } = 32;

    /// <summary>声道数：1（单声道）或 2（立体声）</summary>
    [JsonPropertyName("Channels")] public int Channels { get; set; } = 1;

    /// <summary>默认输出路径，空字符串表示使用桌面</summary>
    [JsonPropertyName("OutputPath")] public string OutputPath { get; set; } = "";
}

/// <summary>
/// 应用程序通用设置类，控制应用的启动行为、界面表现和更新策略。
/// </summary>
public class AppSettings
{
    /// <summary>是否随 Windows 系统启动</summary>
    [JsonPropertyName("StartWithWindows")] public bool StartWithWindows { get; set; }

    /// <summary>最小化时是否收入系统托盘</summary>
    [JsonPropertyName("MinimizeToTray")] public bool MinimizeToTray { get; set; } = true;

    /// <summary>关闭窗口时是否收入系统托盘而非退出</summary>
    [JsonPropertyName("CloseToTray")] public bool CloseToTray { get; set; } = true;

    /// <summary>是否在任务栏显示图标</summary>
    [JsonPropertyName("ShowInTaskbar")] public bool ShowInTaskbar { get; set; } = false;

    /// <summary>搜索结果最大显示条数</summary>
    [JsonPropertyName("MaxResults")] public int MaxResults { get; set; } = 10;

    /// <summary>自动生成二维码的文本长度阈值，超过此长度自动生成二维码</summary>
    [JsonPropertyName("QRCodeThreshold")] public int QRCodeThreshold { get; set; } = 20;

    /// <summary>是否启用自动更新</summary>
    [JsonPropertyName("AutoUpdate")] public bool AutoUpdate { get; set; } = true;

    /// <summary>启动时是否检查更新</summary>
    [JsonPropertyName("CheckForUpdatesOnStartup")] public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>界面语言，默认为简体中文</summary>
    [JsonPropertyName("Language")] public string Language { get; set; } = "zh-CN";

    /// <summary>UI 模式：WPF（默认）或 WinFormsLite（轻量低内存界面）</summary>
    [JsonPropertyName("UIMode")] public string UIMode { get; set; } = "WPF";
}

/// <summary>
/// 文件搜索设置类，控制文件搜索功能的启用状态、搜索目录和行为。
/// </summary>
public class FileSearchSettings
{
    /// <summary>是否启用文件搜索</summary>
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;

    /// <summary>搜索目录列表，空列表表示使用默认目录（桌面+下载）</summary>
    [JsonPropertyName("Directories")] public List<string> Directories { get; set; } = new();

    /// <summary>每个目录最多扫描文件数</summary>
    [JsonPropertyName("MaxFiles")] public int MaxFiles { get; set; } = 300;

    /// <summary>最多返回结果数</summary>
    [JsonPropertyName("MaxResults")] public int MaxResults { get; set; } = 8;

    /// <summary>是否递归搜索子目录</summary>
    [JsonPropertyName("Recursive")] public bool Recursive { get; set; } = false;
}
