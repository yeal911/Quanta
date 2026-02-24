/// <summary>
/// 搜索引擎核心模块
/// 负责处理用户输入的搜索查询，匹配自定义命令、内置命令、应用程序、文件和最近使用的文件。
/// 提供模糊匹配评分、命令执行、文件启动等功能。
/// </summary>

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 搜索提供程序接口
/// 所有搜索提供程序（如应用搜索、文件搜索、最近文件搜索）均需实现此接口。
/// </summary>
public interface ISearchProvider
{
    /// <summary>
    /// 根据查询字符串异步执行搜索
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="cancellationToken">取消令牌，用于支持搜索取消操作</param>
    /// <returns>匹配的搜索结果列表</returns>
    Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索提供程序的名称标识
    /// </summary>
    string Name { get; }
}

// ═════════════════════════════════════════════════════════════════════════════
// 辅助方法
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>
/// 根据关键字获取对应的语言代码
/// </summary>
internal static partial class SearchEngineHelper
{
    public static string? GetLanguageCodeFromKeyword(string keyword)
    {
        return keyword.ToLower() switch
        {
            "english" or "en" or "eng" => "en-US",
            "chinese" or "zh" or "中文" => "zh-CN",
            "spanish" or "espanol" or "español" or "西班牙语" => "es-ES",
            _ => null
        };
    }
}

/// <summary>
/// 搜索引擎核心类
/// 负责统一调度各种搜索源（自定义命令、内置命令、命令路由等），
/// 并对搜索结果进行评分排序，最终返回给用户界面展示。
/// </summary>
public partial class SearchEngine
{
    /// <summary>
    /// 使用频率追踪器，用于记录和查询命令/文件的使用次数，辅助搜索结果排序
    /// </summary>
    private readonly UsageTracker _usageTracker;

    /// <summary>
    /// 命令路由器，负责处理特殊命令（如数学计算、网页搜索等）
    /// </summary>
    private readonly CommandRouter _commandRouter;

    /// <summary>
    /// 窗口管理器，负责枚举并切换到系统中的可见应用窗口
    /// </summary>
    private readonly WindowManager _windowManager;

    /// <summary>
    /// 文件搜索提供程序，在桌面和下载目录中搜索文件
    /// </summary>
    private readonly FileSearchProvider _fileSearchProvider;

    /// <summary>
    /// 用户自定义命令列表，从配置文件 config.json 中加载
    /// </summary>
    private List<CommandConfig> _customCommands = new();

    /// <summary>
    /// 搜索结果最大显示条数，从配置文件 AppSettings.MaxResults 读取
    /// </summary>
    private int _maxResults = 10;

    /// <summary>
    /// 自动生成二维码的文本长度阈值，超过此长度自动生成二维码
    /// </summary>
    private int _qrCodeThreshold = 20;

    /// <summary>
    /// 搜索结果评分器
    /// </summary>
    private readonly ISearchResultScorer _scorer;

    /// <summary>
    /// 可执行文件路径缓存
    /// </summary>
    private readonly IExecutablePathCache _pathCache;

    /// <summary>
    /// Windows 系统内置命令列表（静态模板，不含本地化文本）
    /// Keyword 为唯一标识，Name/Description 通过 LocalizationService 动态获取
    /// GroupKey 用于确定搜索结果分组（国际化 key）
    /// </summary>
    private static readonly List<CommandConfig> BuiltInCommandsTemplate = new()
    {
        // ── 常用系统工具（GroupCommand） ──────────────────────────
        new() { Keyword = "cmd",       Type = "Program", Path = "cmd.exe",      Arguments = "/k {param}", IsBuiltIn = true, GroupKey = "GroupCommand" },
        new() { Keyword = "powershell",Type = "Program", Path = "powershell.exe",Arguments = "-NoExit -Command \"{param}\"", IsBuiltIn = true, GroupKey = "GroupCommand" },
        new() { Keyword = "notepad",   Type = "Program", Path = "notepad.exe",  Arguments = "{param}",    IsBuiltIn = true, GroupKey = "GroupApp" },
        new() { Keyword = "calc",      Type = "Program", Path = "calc.exe",                               IsBuiltIn = true, GroupKey = "GroupApp" },
        new() { Keyword = "mspaint",   Type = "Program", Path = "mspaint.exe",                            IsBuiltIn = true, GroupKey = "GroupApp" },
        new() { Keyword = "explorer",  Type = "Program", Path = "explorer.exe", Arguments = "{param}",    IsBuiltIn = true, GroupKey = "GroupFile" },
        new() { Keyword = "taskmgr",   Type = "Program", Path = "taskmgr.exe",                            IsBuiltIn = true, GroupKey = "GroupSystem" },
        new() { Keyword = "devmgmt",   Type = "Program", Path = "devmgmt.msc",                            IsBuiltIn = true, GroupKey = "GroupSystem" },
        new() { Keyword = "services",  Type = "Program", Path = "services.msc",                           IsBuiltIn = true, GroupKey = "GroupSystem" },
        new() { Keyword = "regedit",   Type = "Program", Path = "regedit.exe",                            IsBuiltIn = true, GroupKey = "GroupSystem" },
        new() { Keyword = "control",   Type = "Program", Path = "control.exe",                            IsBuiltIn = true, GroupKey = "GroupSystem" },
        // ── 网络诊断（GroupNetwork） ──────────────────────────────
        new() { Keyword = "ipconfig",  Type = "Shell",   Path = "ipconfig {param}",                       IsBuiltIn = true, GroupKey = "GroupNetwork" },
        new() { Keyword = "ping",      Type = "Shell",   Path = "ping {param}",                           IsBuiltIn = true, GroupKey = "GroupNetwork" },
        new() { Keyword = "tracert",   Type = "Shell",   Path = "tracert {param}",                        IsBuiltIn = true, GroupKey = "GroupNetwork" },
        new() { Keyword = "nslookup",  Type = "Shell",   Path = "nslookup {param}",                       IsBuiltIn = true, GroupKey = "GroupNetwork" },
        new() { Keyword = "netstat",   Type = "Shell",   Path = "netstat -an",                            IsBuiltIn = true, GroupKey = "GroupNetwork" },
        // ── 系统控制（GroupPower） ────────────────────────────────
        new() { Keyword = "lock",      Type = "Program", Path = "rundll32.exe", Arguments = "user32.dll,LockWorkStation", IsBuiltIn = true, IconPath = "🔒", RunHidden = true, GroupKey = "GroupPower" },
        new() { Keyword = "shutdown",   Type = "Shell",   Path = "shutdown /s /t 10",                      IsBuiltIn = true, IconPath = "⏻", RunHidden = true, GroupKey = "GroupPower" },
        new() { Keyword = "restart",   Type = "Shell",   Path = "shutdown /r /t 10",                      IsBuiltIn = true, IconPath = "🔄", RunHidden = true, GroupKey = "GroupPower" },
        new() { Keyword = "sleep",     Type = "Shell",   Path = "rundll32.exe powrprof.dll,SetSuspendState 0,1,0", IsBuiltIn = true, IconPath = "💤", RunHidden = true, GroupKey = "GroupPower" },
        new() { Keyword = "emptybin",  Type = "Shell",   Path = "PowerShell -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"", IsBuiltIn = true, IconPath = "🗑", RunHidden = true, GroupKey = "GroupSystem" },
        // ── Quanta 应用功能（GroupQuanta） ───────────────────────
        new() { Keyword = "setting",   Type = "SystemAction", Path = "setting",   IsBuiltIn = true, IconPath = "⚙",  GroupKey = "GroupQuanta" },
        new() { Keyword = "exit",      Type = "SystemAction", Path = "exit",      IsBuiltIn = true, IconPath = "✕",  GroupKey = "GroupQuanta" },
        new() { Keyword = "about",     Type = "SystemAction", Path = "about",     IsBuiltIn = true, IconPath = "ℹ",  GroupKey = "GroupQuanta" },
        new() { Keyword = "english",   Type = "SystemAction", Path = "english",   IsBuiltIn = true, IconPath = "EN", GroupKey = "GroupQuanta" },
        new() { Keyword = "chinese",   Type = "SystemAction", Path = "chinese",   IsBuiltIn = true, IconPath = "中", GroupKey = "GroupQuanta" },
        new() { Keyword = "spanish",   Type = "SystemAction", Path = "spanish",   IsBuiltIn = true, IconPath = "ES", GroupKey = "GroupQuanta" },
        new() { Keyword = "winrecord", Type = "SystemAction", Path = "winrecord", IsBuiltIn = true, IconPath = "🎤", GroupKey = "GroupApp" },
        // ── Quanta 特色功能（GroupFeature）──────────────────────
        // record/clip 加入模板仅用于模糊匹配发现；实际执行逻辑由 SearchAsync 短路处理
        // clip 使用 SystemAction，点击无效果但不会报错；用户需输入 "clip" 进入剪贴板历史
        new() { Keyword = "record",    Type = "RecordCommand", Path = "record",   IsBuiltIn = true, IconPath = "🎙", GroupKey = "GroupFeature" },
        new() { Keyword = "clip",      Type = "SystemAction",  Path = "clip",     IsBuiltIn = true, IconPath = "📋", GroupKey = "GroupFeature" },
    };

    /// <summary>
    /// 获取本地化后的内置命令列表
    /// </summary>
    private List<CommandConfig> GetBuiltInCommands()
    {
        return BuiltInCommandsTemplate.Select(cmd =>
        {
            var localized = new CommandConfig
            {
                Keyword = cmd.Keyword,
                Type = cmd.Type,
                Path = cmd.Path,
                Arguments = cmd.Arguments,
                IconPath = cmd.IconPath,
                RunHidden = cmd.RunHidden,
                IsBuiltIn = true,
                GroupKey = cmd.GroupKey,
                Name = LocalizationService.Get($"BuiltinCmd_{cmd.Keyword}"),
                Description = LocalizationService.Get($"BuiltinDesc_{cmd.Keyword}")
            };
            return localized;
        }).ToList();
    }

    /// <summary>
    /// 根据分组 key 返回分组排序权重
    /// </summary>
    private static int GetGroupOrder(string groupKey) => groupKey switch
    {
        "GroupCalc"    => 0,
        "GroupQRCode"  => 0,
        "GroupCommand" => 1,
        "GroupApp"     => 2,
        "GroupSystem"  => 3,
        "GroupNetwork" => 4,
        "GroupPower"   => 5,
        "GroupFeature" => 6,
        "GroupQuanta"  => 7,
        "GroupFile"    => 8,
        "GroupWindow"  => 9,
        _              => 10,
    };

    /// <summary>
    /// 搜索引擎构造函数，通过 DI 注入所有依赖
    /// </summary>
    /// <param name="usageTracker">使用频率追踪器实例</param>
    /// <param name="commandRouter">命令路由器实例</param>
    /// <param name="fileSearchProvider">文件搜索提供程序</param>
    public SearchEngine(UsageTracker usageTracker, CommandRouter commandRouter, FileSearchProvider fileSearchProvider)
    {
        _usageTracker = usageTracker;
        _commandRouter = commandRouter;
        _windowManager = new WindowManager();
        _fileSearchProvider = fileSearchProvider;
        _scorer = SearchResultScorer.Instance;
        _pathCache = ExecutablePathCache.Instance;

        LoadCustomCommands();
        ConfigLoader.ConfigChanged += OnConfigChanged;
    }


    /// <summary>
    /// 从配置文件加载用户自定义命令到内存
    /// 使用 ConfigLoader.Load() 读取（带缓存）
    /// </summary>
    private void LoadCustomCommands()
    {
        var config = ConfigLoader.Load();
        _customCommands = config.Commands ?? new List<CommandConfig>();
        _maxResults = config.AppSettings?.MaxResults > 0 ? config.AppSettings.MaxResults : 10;
        _qrCodeThreshold = config.AppSettings?.QRCodeThreshold > 0 ? config.AppSettings.QRCodeThreshold : 20;
    }

    /// <summary>
    /// 重新加载命令到内存（强制清除配置缓存后重新读取文件）
    /// 通常在关闭设置界面后调用，确保内存中的命令与配置文件同步
    /// </summary>
    public void ReloadCommands()
    {
        var config = ConfigLoader.Reload();
        _customCommands = config.Commands ?? new List<CommandConfig>();
        _maxResults = config.AppSettings?.MaxResults > 0 ? config.AppSettings.MaxResults : 10;
        _qrCodeThreshold = config.AppSettings?.QRCodeThreshold > 0 ? config.AppSettings.QRCodeThreshold : 20;
    }


    /// <summary>
    /// 配置变更时同步刷新内存中的命令与搜索参数。
    /// </summary>
    private void OnConfigChanged(object? sender, AppConfig config)
    {
        _customCommands = config.Commands ?? new List<CommandConfig>();
        _maxResults = config.AppSettings?.MaxResults > 0 ? config.AppSettings.MaxResults : 10;
        _qrCodeThreshold = config.AppSettings?.QRCodeThreshold > 0 ? config.AppSettings.QRCodeThreshold : 20;
    }

    /// <summary>
    /// 获取命令的显示图标文本。
    /// 优先使用命令自定义的 IconPath（如果是 emoji 字符串），否则根据命令类型返回默认图标。
    /// </summary>
    private static string GetIconText(CommandConfig cmd)
    {
        // 如果 IconPath 非空且看起来是 emoji（短字符串，不是文件路径），直接使用
        if (!string.IsNullOrEmpty(cmd.IconPath) && cmd.IconPath.Length <= 4 && !cmd.IconPath.Contains('.'))
            return cmd.IconPath;

        return cmd.Type.ToLower() switch
        {
            "url" => "\U0001f310",       // 🌐
            "program" => "\U0001f4e6",   // 📦
            "directory" => "\U0001f4c1", // 📁
            "shell" => "\u26a1",         // ⚡
            "calculator" => "\U0001f522",// 🔢
            _ => "\u2699"                // ⚙
        };
    }

    /// <summary>
    /// 执行异步搜索的核心方法。
    /// 当查询为空时返回最近使用的命令；否则并发搜索自定义命令、应用程序、文件和窗口，
    /// 最终按分组优先级和匹配分数排序，返回前 N 条结果（N 由配置决定）。
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按分组+分数排序的搜索结果列表</returns>

}
