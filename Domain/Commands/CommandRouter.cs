// ============================================================================
// 文件名: CommandRouter.cs
// 文件描述: 命令路由服务，负责解析用户输入并将其分发到对应的命令处理器。
//           基于 ICommandHandler + 优先级 的 pipeline 装配，支持可扩展命令处理。
// ============================================================================

using System.Linq;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 命令处理器接口，所有命令处理器需实现此接口。
/// </summary>
public interface ICommandHandler
{
    /// <summary>处理器名称</summary>
    string Name { get; }

    /// <summary>优先级（数值越小优先级越高）</summary>
    int Priority { get; }

    /// <summary>
    /// 尝试处理输入，返回搜索结果；如果不匹配则返回 null。
    /// </summary>
    Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker);
}

/// <summary>
/// 命令路由器（编排层）：按优先级顺序调用处理器 pipeline。
/// </summary>
public class CommandRouter
{
    private readonly UsageTracker _usageTracker;
    private readonly IReadOnlyList<ICommandHandler> _handlers;

    // 文本命令建议列表（用于输入提示，不参与执行）
    private static readonly (string Keyword, string Description, string Icon)[] TextCommandSuggestions =
    {
        ("base64", "Base64 编码", "B"),
        ("base64d", "Base64 解码", "B"),
        ("md5", "MD5 哈希", "M"),
        ("sha256", "SHA-256 哈希", "S"),
        ("url", "URL 编码/解码", "U"),
        ("json", "JSON 格式化", "J")
    };

    /// <summary>
    /// 默认构造：内置处理器注册表。
    /// </summary>
    public CommandRouter(UsageTracker usageTracker)
        : this(usageTracker, BuildDefaultHandlers())
    {
    }

    /// <summary>
    /// 可测试构造：允许注入自定义处理器集合。
    /// </summary>
    public CommandRouter(UsageTracker usageTracker, IEnumerable<ICommandHandler> handlers)
    {
        _usageTracker = usageTracker;
        var resolved = handlers.OrderBy(h => h.Priority).ToList();
        _handlers = resolved.Count == 0 ? BuildDefaultHandlers() : resolved;
    }

    public async Task<SearchResult?> TryHandleCommandAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        foreach (var handler in _handlers)
        {
            var result = await handler.HandleAsync(input, _usageTracker);
            if (result != null) return result;
        }

        return null;
    }

    public List<SearchResult> GetTextCommandSuggestions(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<SearchResult>();

        var suggestions = new List<SearchResult>();
        var lowerInput = input.ToLowerInvariant();

        foreach (var cmd in TextCommandSuggestions)
        {
            if (!cmd.Keyword.StartsWith(lowerInput, StringComparison.OrdinalIgnoreCase)) continue;

            suggestions.Add(new SearchResult
            {
                Title = cmd.Keyword,
                Subtitle = cmd.Description + " - " + LocalizationService.Get("TextToolHint"),
                Type = SearchResultType.Command,
                IconText = cmd.Icon,
                GroupLabel = LocalizationService.Get("GroupText"),
                GroupOrder = 6,
                MatchScore = 0.9
            });
        }

        return suggestions;
    }

    public static List<SearchResult> GetCommandSuggestions() => new()
    {
        new() { Title = "> command", Subtitle = "Execute PowerShell command", Type = SearchResultType.Command },
        new() { Title = "calc expression", Subtitle = "Calculate expression", Type = SearchResultType.Calculator },
        new() { Title = "g keyword", Subtitle = "Search in browser", Type = SearchResultType.WebSearch }
    };

    private static IReadOnlyList<ICommandHandler> BuildDefaultHandlers()
    {
        return new List<ICommandHandler>
        {
            new CurrencyConvertHandler(),
            new ColorConvertHandler(),
            new UnitConvertHandler(),
            new PowerShellHandler(),
            new CalcHandler(),
            new TextToolHandler(),
            new GoogleSearchHandler()
        };
    }
}
