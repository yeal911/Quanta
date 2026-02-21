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

/// <summary>
/// 搜索引擎核心类
/// 负责统一调度各种搜索源（自定义命令、内置命令、命令路由等），
/// 并对搜索结果进行评分排序，最终返回给用户界面展示。
/// </summary>
public class SearchEngine
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
    /// Windows 系统内置命令列表
    /// 包含常用的系统工具（如命令提示符、计算器、任务管理器等）、网络诊断命令和系统控制命令。
    /// 这些命令无需用户配置即可直接使用，IsBuiltIn=true 标记，不会出现在用户设置界面中。
    /// </summary>
    private static readonly List<CommandConfig> BuiltInCommands = new()
    {
        // ── 常用系统工具 ──────────────────────────────────────────
        new() { Keyword = "cmd",       Name = "命令提示符",  Type = "Program", Path = "cmd.exe",      Arguments = "/k {param}", Description = "打开CMD",          IsBuiltIn = true },
        new() { Keyword = "powershell",Name = "PowerShell",  Type = "Program", Path = "powershell.exe",Arguments = "-NoExit -Command \"{param}\"", Description = "打开PowerShell", IsBuiltIn = true },
        new() { Keyword = "notepad",   Name = "记事本",      Type = "Program", Path = "notepad.exe",  Arguments = "{param}",    Description = "打开记事本",       IsBuiltIn = true },
        new() { Keyword = "calc",      Name = "计算器",      Type = "Program", Path = "calc.exe",                               Description = "打开计算器",       IsBuiltIn = true },
        new() { Keyword = "mspaint",   Name = "画图",        Type = "Program", Path = "mspaint.exe",                            Description = "打开画图",         IsBuiltIn = true },
        new() { Keyword = "explorer",  Name = "资源管理器",  Type = "Program", Path = "explorer.exe", Arguments = "{param}",    Description = "打开资源管理器",   IsBuiltIn = true },
        new() { Keyword = "taskmgr",   Name = "任务管理器",  Type = "Program", Path = "taskmgr.exe",                            Description = "打开任务管理器",   IsBuiltIn = true },
        new() { Keyword = "devmgmt",   Name = "设备管理器",  Type = "Program", Path = "devmgmt.msc",                            Description = "打开设备管理器",   IsBuiltIn = true },
        new() { Keyword = "services",  Name = "服务",        Type = "Program", Path = "services.msc",                           Description = "打开服务",         IsBuiltIn = true },
        new() { Keyword = "regedit",   Name = "注册表",      Type = "Program", Path = "regedit.exe",                            Description = "打开注册表",       IsBuiltIn = true },
        new() { Keyword = "control",   Name = "控制面板",    Type = "Program", Path = "control.exe",                            Description = "打开控制面板",     IsBuiltIn = true },
        // ── 网络诊断 ──────────────────────────────────────────────
        new() { Keyword = "ipconfig",  Name = "IP配置",      Type = "Shell",   Path = "ipconfig {param}",                       Description = "查看IP配置",       IsBuiltIn = true },
        new() { Keyword = "ping",      Name = "Ping",        Type = "Shell",   Path = "ping {param}",                           Description = "Ping命令",         IsBuiltIn = true },
        new() { Keyword = "tracert",   Name = "路由追踪",    Type = "Shell",   Path = "tracert {param}",                        Description = "追踪路由",         IsBuiltIn = true },
        new() { Keyword = "nslookup",  Name = "DNS查询",     Type = "Shell",   Path = "nslookup {param}",                       Description = "DNS查询",          IsBuiltIn = true },
        new() { Keyword = "netstat",   Name = "网络状态",    Type = "Shell",   Path = "netstat -an",                            Description = "查看网络状态",     IsBuiltIn = true },
        // ── 系统控制 ──────────────────────────────────────────────
        new() { Keyword = "lock",      Name = "锁屏",        Type = "Program", Path = "rundll32.exe", Arguments = "user32.dll,LockWorkStation", Description = "锁定计算机", IsBuiltIn = true, IconPath = "🔒", RunHidden = true },
        new() { Keyword = "shutdown",   Name = "关机",        Type = "Shell",   Path = "shutdown /s /t 10",                      Description = "10秒后关机",       IsBuiltIn = true, IconPath = "⏻", RunHidden = true },
        new() { Keyword = "restart",   Name = "重启",        Type = "Shell",   Path = "shutdown /r /t 10",                      Description = "10秒后重启",       IsBuiltIn = true, IconPath = "🔄", RunHidden = true },
        new() { Keyword = "sleep",     Name = "睡眠",        Type = "Shell",   Path = "rundll32.exe powrprof.dll,SetSuspendState 0,1,0", Description = "进入睡眠状态", IsBuiltIn = true, IconPath = "💤", RunHidden = true },
        new() { Keyword = "emptybin",  Name = "清空回收站",  Type = "Shell",   Path = "PowerShell -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"", Description = "清空回收站", IsBuiltIn = true, IconPath = "🗑", RunHidden = true },
        // ── 应用快捷命令 ──────────────────────────────────────────
        new() { Keyword = "setting",   Name = "打开设置",    Type = "SystemAction", Path = "setting", Description = "打开设置界面", IsBuiltIn = true, IconPath = "⚙" },
        new() { Keyword = "exit",      Name = "退出程序",    Type = "SystemAction", Path = "exit", Description = "退出 Quanta", IsBuiltIn = true, IconPath = "✕" },
        new() { Keyword = "about",     Name = "关于",        Type = "SystemAction", Path = "about", Description = "关于程序", IsBuiltIn = true, IconPath = "ℹ" },
        new() { Keyword = "english",   Name = "切换到英文",  Type = "SystemAction", Path = "english", Description = "切换界面语言为英文", IsBuiltIn = true, IconPath = "EN" },
        new() { Keyword = "chinese",   Name = "切换到中文",  Type = "SystemAction", Path = "chinese", Description = "切换界面语言为中文", IsBuiltIn = true, IconPath = "中" },
        new() { Keyword = "winrecord", Name = "Windows 录音机", Type = "SystemAction", Path = "winrecord", Description = "打开 Windows 内置录音机", IsBuiltIn = true, IconPath = "🎤" },
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

        LoadCustomCommands();
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
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetDefaultResultsAsync(cancellationToken);

        // ── 0. 剪贴板历史（clip 前缀短路，不混入其他结果）─────────
        var clipMatch = System.Text.RegularExpressions.Regex.Match(
            query, @"^clip(?:\s+(.*))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (clipMatch.Success)
        {
            string keyword = clipMatch.Groups[1].Value.Trim();
            return ClipboardHistoryService.Instance.Search(keyword);
        }

        // ── 0.5. 录音命令（record 前缀短路）────────────────────────
        var recordMatch = System.Text.RegularExpressions.Regex.Match(
            query, @"^record(?:\s+(.*))?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (recordMatch.Success)
        {
            string filePrefix = recordMatch.Groups[1].Value.Trim();
            return new List<SearchResult> { BuildRecordCommandResult(filePrefix) };
        }

        var results = new ConcurrentBag<SearchResult>();

        // ── 1. 搜索自定义命令和内置命令（同步，始终执行）──────────
        var customResults = SearchCustomCommands(query);
        foreach (var r in customResults) results.Add(r);

        // ── 2. 通过命令路由器处理特殊命令（计算、网页搜索、单位换算）──
        var commandResult = await _commandRouter.TryHandleCommandAsync(query);
        if (commandResult != null)
        {
            // 避免重复：如果 SearchCustomCommands 已经添加了同名的系统操作命令，则跳过
            bool alreadyExists = customResults.Any(r => 
                r.Type == SearchResultType.SystemAction && 
                r.Path?.Equals(commandResult.Subtitle, StringComparison.OrdinalIgnoreCase) == true);
            if (!alreadyExists)
            {
                // 根据类型设置分组标签：计算器为 Calc，二维码为 QRCode，系统操作为 System，网页搜索为 Web
                if (commandResult.Type == SearchResultType.Calculator)
                    commandResult.GroupLabel = "Calc";
                else if (commandResult.Type == SearchResultType.QRCode)
                    commandResult.GroupLabel = "QRCode";
                else if (commandResult.Type == SearchResultType.SystemAction)
                    commandResult.GroupLabel = LocalizationService.Get("GroupQuickCommands");
                else if (commandResult.Type == SearchResultType.WebSearch)
                    commandResult.GroupLabel = "Web";
                // Calculator 和 Web 结果应该排在最前面（GroupOrder=0），优先级高于 App/File/Window
                commandResult.GroupOrder = 0;
                // 如果没有设置 MatchScore，给一个默认高分确保显示
                if (commandResult.MatchScore <= 0)
                    commandResult.MatchScore = 1.0;
                results.Add(commandResult);
            }
        }

        // ── 2.5. 如果查询长度超过阈值，自动生成二维码 ──────────────────
        if (query.Length > _qrCodeThreshold && QRCodeService.CanGenerateQRCode(query))
        {
            var qrCodeResult = new SearchResult
            {
                Title = "生成二维码",
                Subtitle = query.Length > 50 ? query.Substring(0, 50) + "..." : query,
                Path = query,
                Type = SearchResultType.QRCode,
                GroupLabel = "QRCode",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "📱",
                QueryMatch = query,
                QRCodeContent = query,
                QRCodeImage = QRCodeService.GenerateQRCodeAutoSize(query)
            };
            results.Add(qrCodeResult);
        }
        // ── 2.6. 如果文本超过2000字符，显示提示信息 ─────────────────────
        else if (query.Length > 2000)
        {
            var hintResult = new SearchResult
            {
                Title = LocalizationService.Get("QRCodeTooLong"),
                Subtitle = "",
                Path = "",
                Type = SearchResultType.Command,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "⚠️",
                QueryMatch = ""
            };
            results.Add(hintResult);
        }

        // ── 3. 查询长度 >= 2 时并发搜索应用程序、文件和窗口 ────────
        if (query.Length >= 2)
        {
            var providerTasks = new List<Task>();

            // 3a. 搜索文件（桌面+下载目录）
            providerTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var fileResults = await _fileSearchProvider.SearchAsync(query, cancellationToken);
                    foreach (var r in fileResults)
                    {
                        r.GroupLabel = "File";
                        r.GroupOrder = 2;
                        r.IconText = "📄";
                        r.QueryMatch = query;
                        results.Add(r);
                    }
                }
                catch (Exception ex) { Logger.Warn($"File search failed: {ex.Message}"); }
            }, cancellationToken));

            // 3c. 搜索当前打开的窗口（同步快速）
            // 使用包含匹配（%keyword%），不使用模糊子序列匹配
            providerTasks.Add(Task.Run(() =>
            {
                try
                {
                    var windows = _windowManager.GetVisibleWindows();
                    var queryLower = query.ToLower();
                    
                    foreach (var w in windows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var titleLower = w.Title.ToLower();
                        
                        // 使用包含匹配
                        if (titleLower.Contains(queryLower))
                        {
                            // 完全匹配 = 1.0，前缀匹配 = 0.9，包含匹配 = 0.8
                            double score = titleLower == queryLower ? 1.0 
                                : titleLower.StartsWith(queryLower) ? 0.9 
                                : 0.8;
                            
                            w.MatchScore = score;
                            w.GroupLabel = "Window";
                            w.GroupOrder = 3;
                            w.IconText = "🪟";
                            w.QueryMatch = query;
                            results.Add(w);
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { Logger.Debug($"Window search failed: {ex.Message}"); }
            }, cancellationToken));

            await Task.WhenAll(providerTasks);
        }

        // ── 4. 每个分组内部按匹配分数降序排列，分组间按 GroupOrder 升序 ──
        var finalList = results
            .OrderBy(r => r.GroupOrder)
            .ThenByDescending(r => r.MatchScore)
            .ThenByDescending(r => _usageTracker.GetUsageCount(r.Id))
            .Take(_maxResults)
            .ToList();

        // 为每个结果设置索引和 QueryMatch
        for (int i = 0; i < finalList.Count; i++)
        {
            finalList[i].Index = i + 1;
            if (string.IsNullOrEmpty(finalList[i].QueryMatch))
                finalList[i].QueryMatch = query;
        }
        return finalList;
    }

    /// <summary>
    /// 在自定义命令和内置命令中搜索匹配项
    /// 匹配逻辑按优先级排序：完全匹配(1.0) > 前缀匹配(0.95) > 包含匹配(0.9) > 名称包含(0.85) > 描述包含(0.8)
    /// 用户自定义命令优先级高于内置命令（排列在前）。
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <returns>匹配的命令搜索结果列表</returns>
    private List<SearchResult> SearchCustomCommands(string query)
    {
        var results = new List<SearchResult>();
        int index = 0;

        // 将用户命令（优先级更高）与内置命令合并搜索
        var allCommands = _customCommands.Concat(BuiltInCommands);

        foreach (var cmd in allCommands)
        {
            // 系统操作命令使用独立的分组标签
            bool isSystemAction = cmd.Type.Equals("SystemAction", StringComparison.OrdinalIgnoreCase);
            string groupLabel = isSystemAction ? LocalizationService.Get("GroupQuickCommands") : "Command";

            if (string.IsNullOrEmpty(query))
            {
                // 查询为空时，返回所有命令（默认匹配分数 1.0）
                results.Add(new SearchResult
                {
                    Index = index++,
                    Id = $"cmd:{cmd.Keyword}",
                    Title = cmd.Keyword,
                    Subtitle = cmd.Name,
                    Path = cmd.Path,
                    IconText = GetIconText(cmd),
                    Type = isSystemAction ? SearchResultType.SystemAction : SearchResultType.CustomCommand,
                    CommandConfig = cmd,
                    MatchScore = 1.0,
                    GroupLabel = groupLabel,
                    GroupOrder = 0
                });
            }
            else
            {
                // 根据不同匹配方式计算分数
                double score = 0;

                if (query.Equals(cmd.Keyword, StringComparison.OrdinalIgnoreCase))
                    score = 1.0;    // 关键词完全匹配
                else if (cmd.Keyword.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.95;   // 关键词前缀匹配
                else if (cmd.Keyword.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.9;    // 关键词包含匹配
                else if (!string.IsNullOrEmpty(cmd.Name) && cmd.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.85;   // 命令名称包含匹配
                else if (!string.IsNullOrEmpty(cmd.Description) && cmd.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score = 0.8;    // 命令描述包含匹配

                if (score > 0)
                {
                    results.Add(new SearchResult
                    {
                        Index = index++,
                        Id = $"cmd:{cmd.Keyword}",
                        Title = cmd.Keyword,
                        Subtitle = cmd.Name,
                        Path = cmd.Path,
                        IconText = GetIconText(cmd),
                        Type = isSystemAction ? SearchResultType.SystemAction : SearchResultType.CustomCommand,
                        CommandConfig = cmd,
                        MatchScore = score,
                        GroupLabel = groupLabel,
                        GroupOrder = 0
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 获取默认搜索结果（当用户未输入任何查询时显示）。
    /// 优先展示最近使用过的命令，剩余位置用用户命令和内置命令补充，最多返回 MaxResults 条。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>默认展示的搜索结果列表（最近使用优先）</returns>
    private async Task<List<SearchResult>> GetDefaultResultsAsync(CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();
        var allCommands = _customCommands.Concat(BuiltInCommands).ToList();

        // 将所有命令按关键字索引，方便按使用记录 ID 查找
        var commandByKey = allCommands.ToDictionary(c => $"cmd:{c.Keyword}", c => c);

        // ── 1. 优先展示最近使用过的命令 ──────────────────────────
        var recentIds = _usageTracker.GetRecentItemIds(_maxResults);
        var addedKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in recentIds)
        {
            if (commandByKey.TryGetValue(id, out var cmd))
            {
                results.Add(BuildDefaultResult(cmd, results.Count + 1));
                addedKeywords.Add(cmd.Keyword);
            }
        }

        // ── 2. 用剩余命令填充，直到达到 MaxResults ───────────────
        foreach (var cmd in allCommands)
        {
            if (results.Count >= _maxResults) break;
            if (!addedKeywords.Contains(cmd.Keyword))
            {
                results.Add(BuildDefaultResult(cmd, results.Count + 1));
                addedKeywords.Add(cmd.Keyword);
            }
        }

        return results;
    }

    /// <summary>
    /// 构建默认显示状态下的搜索结果对象（无查询关键字时的展示格式）
    /// </summary>
    private SearchResult BuildDefaultResult(CommandConfig cmd, int index)
    {
        var typeName = cmd.Type.ToLower() switch
        {
            "url"        => "🌐 " + cmd.Name,
            "program"    => "📦 " + cmd.Name,
            "directory"  => "📁 " + cmd.Name,
            "shell"      => "⚡ " + cmd.Name,
            "calculator" => "🔢 " + cmd.Name,
            _            => cmd.Name
        };

        return new SearchResult
        {
            Index = index,
            Id = $"cmd:{cmd.Keyword}",
            Title = cmd.Keyword,
            Subtitle = typeName,
            Path = cmd.Path,
            IconText = GetIconText(cmd),
            Type = SearchResultType.CustomCommand,
            CommandConfig = cmd,
            MatchScore = 0.5,
            GroupLabel = "",
            GroupOrder = 0
        };
    }

    /// <summary>
    /// 计算模糊匹配分数
    /// 用于评估查询字符串与目标字符串的相似程度。
    /// 匹配逻辑：完全包含(1.0) > 前缀匹配(0.9) > 逐字符顺序匹配(按匹配比例 * 0.7 计算)
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="target">待匹配的目标字符串（如文件名、应用名称等）</param>
    /// <returns>匹配分数，范围 0.0 ~ 1.0，分数越高表示匹配度越好</returns>
    public static double CalculateFuzzyScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
            return 0;

        query = query.ToLower();
        target = target.ToLower();

        // 完全包含匹配，得分最高
        if (target.Contains(query)) return 1.0;

        // 前缀匹配
        if (target.StartsWith(query)) return 0.9;

        // 逐字符顺序模糊匹配：按顺序在目标中查找查询的每个字符
        int matchedChars = 0;
        int targetIndex = 0;
        foreach (char c in query)
        {
            int foundIndex = target.IndexOf(c, targetIndex);
            if (foundIndex >= 0)
            {
                matchedChars++;
                targetIndex = foundIndex + 1;
            }
        }

        // 按匹配字符占比计算分数，乘以 0.7 作为模糊匹配的权重折扣
        return matchedChars > 0 ? (double)matchedChars / query.Length * 0.7 : 0;
    }

    /// <summary>
    /// 执行搜索结果对应的操作
    /// 根据结果类型分派到不同的执行逻辑：文件启动、自定义命令执行等。
    /// </summary>
    public async Task<bool> ExecuteResultAsync(SearchResult result, string param = "")
    {
        switch (result.Type)
        {
            case SearchResultType.Application:
            case SearchResultType.File:
            case SearchResultType.RecentFile:
                return await LaunchFileAsync(result);

            case SearchResultType.Window:
                // 激活（切换到）对应的系统窗口
                return _windowManager.ActivateWindow(result);

            case SearchResultType.Calculator:
                // 将计算结果复制到剪贴板
                var calcOutput = "";
                if (result.Data is CommandResult cr && cr.Success)
                    calcOutput = cr.Output;
                else if (!string.IsNullOrEmpty(result.Subtitle))
                    calcOutput = result.Subtitle;

                if (!string.IsNullOrEmpty(calcOutput))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.Clipboard.SetText(calcOutput));
                    ToastService.Instance.ShowSuccess(LocalizationService.Get("CopiedToClipboard"));
                }
                return true;

            case SearchResultType.Command:
            case SearchResultType.WebSearch:
                return true;

            case SearchResultType.CustomCommand:
                return await ExecuteCustomCommandAsync(result, param);

            case SearchResultType.QRCode:
                // 将二维码图片复制到剪贴板
                if (result.QRCodeImage != null)
                {
                    try
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 将 BitmapImage 转换为 BitmapSource 并复制到剪贴板
                            System.Windows.Clipboard.SetImage(result.QRCodeImage);
                        });
                        ToastService.Instance.ShowSuccess("二维码已复制到剪贴板");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to copy QRCode to clipboard: {ex.Message}");
                        ToastService.Instance.ShowError("复制失败");
                    }
                }
                return true;

            case SearchResultType.SystemAction:
                return ExecuteSystemAction(result.Path);

            case SearchResultType.RecordCommand:
                // RecordCommand 的执行由 MainWindow 直接处理（需要 UI 层配合）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.MainWindow is Views.MainWindow mw)
                        mw.StartRecordingFromResult(result);
                });
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 构建录音命令的搜索结果，加载当前录音配置
    /// </summary>
    private static SearchResult BuildRecordCommandResult(string filePrefix)
    {
        var config = ConfigLoader.Load();
        var recSettings = config.RecordingSettings ?? new Models.RecordingSettings();

        var outputDir = string.IsNullOrEmpty(recSettings.OutputPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : recSettings.OutputPath;

        var recordData = new Models.RecordCommandData
        {
            FilePrefix = filePrefix,
            Source = recSettings.Source,
            Format = recSettings.Format,
            SampleRate = recSettings.SampleRate,
            Bitrate = recSettings.Bitrate,
            Channels = recSettings.Channels,
            OutputPath = recSettings.OutputPath
        };

        return new SearchResult
        {
            Index = 1,
            Id = "cmd:record",
            Title = string.IsNullOrEmpty(filePrefix) ? "record" : $"record {filePrefix}",
            Subtitle = LocalizationService.Get("RecordCommandDesc"),
            Path = "record",
            IconText = "🎙",
            Type = SearchResultType.RecordCommand,
            MatchScore = 1.0,
            GroupLabel = LocalizationService.Get("GroupQuickCommands"),
            GroupOrder = 0,
            QueryMatch = "record",
            RecordData = recordData
        };
    }

    /// <summary>
    /// 执行系统操作（设置、关于、切换语言）
    /// </summary>
    private bool ExecuteSystemAction(string action)
    {
        var app = System.Windows.Application.Current;
        var mainWindow = app.MainWindow;

        switch (action?.ToLower())
        {
            case "setting":
                // 打开设置窗口
                app.Dispatcher.Invoke(() =>
                {
                    var settingsWin = new Views.CommandSettingsWindow(this) { Owner = mainWindow };
                    // 获取当前主题状态
                    var config = ConfigLoader.Load();
                    bool isDark = config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false;
                    settingsWin.SetDarkTheme(isDark);
                    settingsWin.ShowDialog();
                });
                return true;

            case "about":
                // 显示关于信息（使用 Toast）
                app.Dispatcher.Invoke(() =>
                {
                    ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
                });
                return true;

            case "exit":
                // 退出程序
                app.Dispatcher.Invoke(() =>
                {
                    app.Shutdown();
                });
                return true;

            case "english":
                // 切换到英文
                LocalizationService.CurrentLanguage = "en-US";
                app.Dispatcher.Invoke(() =>
                {
                    // 通知主窗口刷新 UI
                    if (mainWindow is Views.MainWindow mw)
                    {
                        var config = Helpers.ConfigLoader.Load();
                        mw.RefreshLocalization();
                        mw.ApplyTheme(config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false);
                    }
                });
                ToastService.Instance.ShowSuccess("Language switched to English");
                return true;

            case "chinese":
                // 切换到中文
                LocalizationService.CurrentLanguage = "zh-CN";
                app.Dispatcher.Invoke(() =>
                {
                    // 通知主窗口刷新 UI
                    if (mainWindow is Views.MainWindow mw)
                    {
                        var config = Helpers.ConfigLoader.Load();
                        mw.RefreshLocalization();
                        mw.ApplyTheme(config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false);
                    }
                });
                ToastService.Instance.ShowSuccess("已切换到中文");
                return true;

            case "winrecord":
                // 打开 Windows 内置录音机
                Logger.Log("[winrecord] 开始尝试打开 Windows 录音机...");

                bool started = false;

                // 方法1: 使用 explorer.exe 打开 AppsFolder 中的录音机
                // 这是最可靠的方法，兼容性最好
                try
                {
                    Logger.Log("[winrecord] 方法1: explorer.exe shell:AppsFolder...");
                    var psi1 = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "shell:AppsFolder\\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe!App",
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi1);
                    Logger.Log("[winrecord] 方法1 启动成功!");
                    started = true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[winrecord] 方法1 失败: {ex.Message}");
                }

                // 方法2: 尝试 ms-voicesRecorder: URI
                if (!started)
                {
                    try
                    {
                        Logger.Log("[winrecord] 方法2: ms-voicesRecorder:");
                        var psi2 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ms-voicesRecorder:",
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi2);
                        Logger.Log("[winrecord] 方法2 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[winrecord] 方法2 失败: {ex.Message}");
                    }
                }

                // 方法3: 尝试 ms-soundrecorder: URI
                if (!started)
                {
                    try
                    {
                        Logger.Log("[winrecord] 方法3: ms-soundrecorder:");
                        var psi3 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ms-soundrecorder:",
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi3);
                        Logger.Log("[winrecord] 方法3 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[winrecord] 方法3 失败: {ex.Message}");
                    }
                }

                // 方法4: 通过 cmd start 命令
                if (!started)
                {
                    try
                    {
                        Logger.Log("[winrecord] 方法4: cmd /c start shell:AppsFolder\\...");
                        var psi4 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c start shell:AppsFolder\\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe!App",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        System.Diagnostics.Process.Start(psi4);
                        Logger.Log("[winrecord] 方法4 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[winrecord] 方法4 失败: {ex.Message}");
                    }
                }

                if (!started)
                {
                    Logger.Log("[winrecord] 所有方法都失败，显示提示");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        ToastService.Instance.ShowWarning("Windows 录音机未安装，请从 Microsoft Store 搜索「录音机」下载"));
                }
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 执行自定义命令
    /// 根据命令类型（url/program/directory/shell/calculator）执行不同的操作逻辑。
    /// 支持参数占位符替换（{param}、{query}、{%p}），支持管理员权限运行和隐藏窗口模式。
    /// </summary>
    /// <param name="result">包含命令配置的搜索结果</param>
    /// <param name="param">用户传入的参数，用于替换命令路径和参数中的占位符</param>
    /// <returns>命令执行是否成功</returns>
    public async Task<bool> ExecuteCustomCommandAsync(SearchResult result, string param)
    {
        Logger.Debug($"ExecuteCustomCommandAsync called: Keyword={result.CommandConfig?.Keyword}, Type={result.CommandConfig?.Type}, Param='{param}'");
        
        if (result.CommandConfig == null) 
        {
            Logger.Debug("ExecuteCustomCommandAsync: CommandConfig is null!");
            return false;
        }

        var cmd = result.CommandConfig;

        // 检查命令是否已启用
        if (!cmd.Enabled)
        {
            Logger.Warn($"Command is disabled: {cmd.Keyword}");
            return false;
        }

        Logger.Debug($"ExecuteCustomCommandAsync: Executing {cmd.Type} with Path='{cmd.Path}', Arguments='{cmd.Arguments}'");

        try
        {
            // 替换路径和参数中的参数占位符（支持自定义占位符和内置占位符）
            var placeholder = !string.IsNullOrEmpty(cmd.ParamPlaceholder) ? cmd.ParamPlaceholder : "{param}";
            var processedPath = cmd.Path
                .Replace(placeholder, param)
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("{%p}", param);

            var processedArgs = cmd.Arguments
                .Replace(placeholder, param)
                .Replace("{param}", param)
                .Replace("{query}", param)
                .Replace("{%p}", param);

            Logger.Debug($"ExecuteCustomCommandAsync: After replace - processedPath='{processedPath}', processedArgs='{processedArgs}'");

            switch (cmd.Type.ToLower())
            {
                // URL 类型：使用默认浏览器打开网址
                case "url":
                    var url = processedPath;
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                // 程序类型：启动可执行程序
                case "program":
                    var programPath = processedPath;

                    // 如果文件不存在且不是绝对路径，尝试在 PATH 环境变量中查找
                    if (!File.Exists(programPath) && !Path.IsPathRooted(programPath))
                    {
                        programPath = FindInPath(programPath);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = programPath,
                        Arguments = processedArgs,
                        UseShellExecute = !cmd.RunHidden,
                        WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory)
                            ? Path.GetDirectoryName(programPath)
                            : cmd.WorkingDirectory,
                        CreateNoWindow = cmd.RunHidden
                    };

                    // 以管理员身份运行
                    if (cmd.RunAsAdmin)
                    {
                        psi.Verb = "runas";
                    }

                    Process.Start(psi);
                    _usageTracker.RecordUsage(result.Id);
                    return true;

                // 目录类型：使用资源管理器打开文件夹
                case "directory":
                    var dirPath = processedPath;
                    if (System.IO.Directory.Exists(dirPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = dirPath,
                            UseShellExecute = true,
                            WorkingDirectory = cmd.WorkingDirectory
                        });
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }
                    Logger.Warn($"Directory not found: {dirPath}");
                    return false;

                // Shell 类型：通过 PowerShell 执行命令行命令
                case "shell":
                    {
                        var shellCmd = processedPath;
                        if (!string.IsNullOrEmpty(processedArgs))
                            shellCmd += " " + processedArgs;

                        Logger.Debug($"ExecuteCustomCommandAsync Shell: shellCmd='{shellCmd}', RunHidden={cmd.RunHidden}");

                        ProcessStartInfo shellPsi;
                        if (cmd.RunHidden)
                        {
                            // 隐藏窗口执行
                            shellPsi = new ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{shellCmd}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory)
                                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                    : cmd.WorkingDirectory
                            };
                        }
                        else
                        {
                            // 显示窗口执行（以便查看输出）
                            shellPsi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c {shellCmd}",
                                UseShellExecute = true,
                                CreateNoWindow = false,
                                WorkingDirectory = string.IsNullOrEmpty(cmd.WorkingDirectory)
                                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                    : cmd.WorkingDirectory
                            };
                        }

                        Logger.Debug($"ExecuteCustomCommandAsync Shell: Starting process with FileName='{shellPsi.FileName}', Arguments='{shellPsi.Arguments}'");

                        // 启动进程后不等待完成（即发即忘，提升响应速度）
                        var process = Process.Start(shellPsi);
                        Logger.Debug($"ExecuteCustomCommandAsync Shell: Process started, Id={process?.Id}");
                        _usageTracker.RecordUsage(result.Id);
                        return true;
                    }

                // 计算器类型：对表达式求值
                case "calculator":
                    var calcResult = CalculateInternal(processedPath);
                    Logger.Debug($"Calculator result: {calcResult}");
                    return true;

                // 系统操作类型：设置、关于、切换语言等
                case "systemaction":
                    return ExecuteSystemAction(processedPath);

                default:
                    Logger.Warn($"Unknown command type: {cmd.Type}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to execute command: {cmd.Keyword}", ex);
            return false;
        }
    }

    /// <summary>
    /// 在 PATH 环境变量中查找可执行文件（带缓存）
    /// </summary>
    private static readonly Dictionary<string, string?> PathCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 在系统 PATH 环境变量的各个目录中搜索指定的可执行文件
    /// 搜索结果会被缓存，避免重复遍历文件系统。
    /// </summary>
    /// <param name="executable">可执行文件名（如 "notepad.exe" 或 "notepad"）</param>
    /// <returns>找到的完整文件路径；未找到则返回 null</returns>
    private string? FindInPath(string executable)
    {
        if (string.IsNullOrEmpty(executable)) return null;

        // 优先从缓存中查找
        if (PathCache.TryGetValue(executable, out var cached))
            return cached;

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        string? result = null;
        foreach (var path in pathEnv.Split(';'))
        {
            try
            {
                if (string.IsNullOrEmpty(path)) continue;

                // 直接拼接路径检查是否存在
                var fullPath = Path.Combine(path, executable);
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }

                // 自动补充 .exe 扩展名再次查找
                fullPath = Path.Combine(path, executable + ".exe");
                if (File.Exists(fullPath))
                {
                    result = fullPath;
                    break;
                }
            }
            catch { }
        }

        // 将结果写入缓存（包括未找到的情况，避免重复搜索）
        PathCache[executable] = result;
        return result;
    }

    /// <summary>
    /// 内部计算器方法，对数学表达式进行求值
    /// 先通过正则过滤非法字符（仅保留数字和运算符），然后使用 DataTable.Compute 求值。
    /// </summary>
    /// <param name="expression">待计算的数学表达式字符串</param>
    /// <returns>计算结果的字符串表示；计算失败时返回 "Error"</returns>
    private string CalculateInternal(string expression)
    {
        try
        {
            // 过滤掉非数学字符，仅保留数字和运算符
            string sanitized = Regex.Replace(expression, @"[^0-9+\-*/().%^]", "");
            var computed = new System.Data.DataTable().Compute(sanitized, null);
            return computed.ToString() ?? "Error";
        }
        catch (Exception ex)
        {
            Logger.Warn($"Calculation error: {ex.Message}");
            return "Error";
        }
    }

    /// <summary>
    /// 启动文件或应用程序
    /// 使用系统默认程序打开指定路径的文件，并记录使用次数。
    /// </summary>
    /// <param name="result">包含文件路径的搜索结果</param>
    /// <returns>启动是否成功</returns>
    private async Task<bool> LaunchFileAsync(SearchResult result)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = result.Path, UseShellExecute = true };
            Process.Start(psi);
            _usageTracker.RecordUsage(result.Id);
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 应用程序搜索提供程序
/// 从 Windows 开始菜单目录中扫描已安装的应用程序（.lnk 快捷方式），
/// 并提供带缓存的模糊搜索功能。缓存有效期为 5 分钟。
/// </summary>
public class ApplicationSearchProvider : ISearchProvider
{
    /// <summary>
    /// 已安装应用程序的缓存列表
    /// </summary>
    private List<SearchResult>? _cachedApps;

    /// <summary>
    /// 缓存创建时间
    /// </summary>
    private DateTime _cacheTime;

    /// <summary>
    /// 缓存有效时长（默认 5 分钟）
    /// </summary>
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 搜索提供程序名称
    /// </summary>
    public string Name => "Applications";

    /// <summary>
    /// 根据查询关键词搜索已安装的应用程序
    /// 如果缓存过期或为空，会重新扫描开始菜单目录加载应用列表。
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按匹配分数降序排列的应用搜索结果（最多 8 条）</returns>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_cachedApps == null || DateTime.Now - _cacheTime > _cacheDuration)
        {
            _cachedApps = await Task.Run(LoadInstalledApplications, cancellationToken);
            _cacheTime = DateTime.Now;
        }

        return _cachedApps
            .Select(app => { app.MatchScore = SearchEngine.CalculateFuzzyScore(query, app.Title); return app; })
            .Where(r => r.MatchScore > 0)
            .OrderByDescending(r => r.MatchScore)
            .Take(8)
            .ToList();
    }

    /// <summary>
    /// 从系统开始菜单目录加载已安装的应用程序
    /// 扫描公共开始菜单和用户开始菜单中的 .lnk 快捷方式文件，
    /// 每个目录最多加载 500 个应用以避免性能问题。
    /// </summary>
    /// <returns>已安装应用程序的搜索结果列表</returns>
    private List<SearchResult> LoadInstalledApplications()
    {
        var apps = new List<SearchResult>();

        // Windows 开始菜单的两个目录：公共（所有用户）和当前用户
        var startMenuPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
        };

        foreach (var path in startMenuPaths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    // 递归搜索 .lnk 快捷方式文件，限制最多 500 个
                    foreach (var file in Directory.GetFiles(path, "*.lnk", SearchOption.AllDirectories).Take(500))
                    {
                        try
                        {
                            apps.Add(new SearchResult
                            {
                                Title = Path.GetFileNameWithoutExtension(file),
                                Path = file,
                                Type = SearchResultType.Application,
                                Id = file
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"Failed to load app: {file} - {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to scan start menu: {path}", ex);
                }
            }
        }

        return apps;
    }
}

/// <summary>
/// 文件搜索提供程序
/// 在用户桌面和下载目录中搜索文件，支持模糊匹配。
/// 仅搜索顶层目录（不递归子目录），每个目录最多扫描 300 个文件。
/// </summary>
public class FileSearchProvider : ISearchProvider
{
    /// <summary>
    /// 默认搜索目录列表：桌面和下载文件夹
    /// </summary>
    private readonly List<string> _searchDirectories = new()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads"
    };

    /// <summary>
    /// 搜索提供程序名称
    /// </summary>
    public string Name => "Files";

    /// <summary>
    /// 在桌面和下载目录中搜索匹配的文件
    /// 仅匹配文件名（不搜索内容），使用包含匹配（%keyword%），不使用模糊子序列匹配。
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按匹配分数降序排列的文件搜索结果（最多 8 条）</returns>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<SearchResult>();
        
        if (string.IsNullOrWhiteSpace(query))
            return results.ToList();

        // 转小写用于不区分大小写匹配
        var queryLower = query.ToLower();

        // 并行搜索各个目录
        var tasks = _searchDirectories.Select(async dir =>
        {
            if (!Directory.Exists(dir)) return;
            try
            {
                // 仅搜索顶层目录，每个目录最多取 300 个文件
                var files = await Task.Run(() =>
                    Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).Take(300), cancellationToken);

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fileName = Path.GetFileName(file);
                    var fileNameLower = fileName.ToLower();

                    // 使用包含匹配（%keyword%），不使用模糊子序列匹配
                    if (fileNameLower.Contains(queryLower))
                    {
                        // 完全匹配 = 1.0，前缀匹配 = 0.9，包含匹配 = 0.8
                        double score = fileNameLower == queryLower ? 1.0 
                            : fileNameLower.StartsWith(queryLower) ? 0.9 
                            : 0.8;

                        results.Add(new SearchResult
                        {
                            Title = fileName,
                            Path = file,
                            Subtitle = Path.GetDirectoryName(file) ?? "",
                            Type = SearchResultType.File,
                            Id = file,
                            MatchScore = score
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to search directory: {dir} - {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        return results.OrderByDescending(r => r.MatchScore).Take(8).ToList();
    }
}

/// <summary>
/// 最近文件搜索提供程序
/// 维护一个最近使用过的文件列表（最多 30 条），支持模糊搜索。
/// 当有新文件被添加时，通过回调通知外部更新 UI。
/// </summary>
public class RecentFileSearchProvider : ISearchProvider
{
    /// <summary>
    /// 使用频率追踪器
    /// </summary>
    private readonly UsageTracker _usageTracker;

    /// <summary>
    /// 最近文件列表更新时的回调函数，用于通知 UI 刷新
    /// </summary>
    private readonly Action<List<SearchResult>> _onRecentFilesUpdated;

    /// <summary>
    /// 最近使用的文件列表（最多保留 30 条记录）
    /// </summary>
    private readonly List<SearchResult> _recentFiles = new();

    /// <summary>
    /// 搜索提供程序名称
    /// </summary>
    public string Name => "Recent Files";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="usageTracker">使用频率追踪器</param>
    /// <param name="onRecentFilesUpdated">最近文件列表更新时的回调，用于通知 UI 层刷新显示</param>
    public RecentFileSearchProvider(UsageTracker usageTracker, Action<List<SearchResult>> onRecentFilesUpdated)
    {
        _usageTracker = usageTracker;
        _onRecentFilesUpdated = onRecentFilesUpdated;
    }

    /// <summary>
    /// 在最近使用的文件列表中搜索匹配项
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按匹配分数降序排列的最近文件搜索结果（最多 5 条）</returns>
    public async Task<List<SearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return _recentFiles
                .Select(file => { file.MatchScore = SearchEngine.CalculateFuzzyScore(query, file.Title); return file; })
                .Where(r => r.MatchScore > 0)
                .OrderByDescending(r => r.MatchScore)
                .Take(5)
                .ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// 将文件添加到最近使用列表
    /// 如果文件已存在则移动到列表顶部；列表超过 30 条时自动移除最旧的记录。
    /// 添加完成后通过回调通知 UI 更新。
    /// </summary>
    /// <param name="filePath">要添加的文件完整路径</param>
    public void AddRecentFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var existing = _recentFiles.FirstOrDefault(f => f.Path == filePath);
        if (existing != null) _recentFiles.Remove(existing);
        _recentFiles.Insert(0, new SearchResult
        {
            Title = Path.GetFileName(filePath),
            Path = filePath,
            Subtitle = Path.GetDirectoryName(filePath) ?? "",
            Type = SearchResultType.RecentFile,
            Id = filePath
        });
        // 限制列表最大长度为 30 条
        while (_recentFiles.Count > 30) _recentFiles.RemoveAt(_recentFiles.Count - 1);
        _onRecentFilesUpdated?.Invoke(_recentFiles);
    }
}
