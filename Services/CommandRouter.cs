// ============================================================================
// 文件名: CommandRouter.cs
// 文件描述: 命令路由服务，负责解析用户输入并将其分发到对应的命令处理器。
//           支持 PowerShell 命令执行、数学表达式计算和浏览器搜索三种命令类型。
// ============================================================================

using System.Diagnostics;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 命令路由器，负责解析用户输入的文本并路由到对应的命令处理逻辑。
/// 支持以下命令格式：
/// <list type="bullet">
///   <item><description><c>&gt; command</c> — 执行 PowerShell 命令</description></item>
///   <item><description><c>calc expression</c> — 计算数学表达式</description></item>
///   <item><description><c>g keyword</c> — 在浏览器中进行 Google 搜索</description></item>
/// </list>
/// </summary>
public class CommandRouter
{
    /// <summary>
    /// 使用记录跟踪器，用于记录命令的使用频率
    /// </summary>
    private readonly UsageTracker _usageTracker;

    /// <summary>
    /// 匹配 PowerShell 命令的正则表达式，格式为: &gt; 命令内容
    /// </summary>
    private static readonly Regex PowerShellRegex = new(@"^>\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配计算表达式的正则表达式，格式为: calc 表达式
    /// </summary>
    private static readonly Regex CalcRegex = new(@"^calc\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配 Google 搜索的正则表达式，格式为: g 关键字
    /// </summary>
    private static readonly Regex GoogleSearchRegex = new(@"^g\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 初始化命令路由器
    /// </summary>
    /// <param name="usageTracker">使用记录跟踪器实例</param>
    public CommandRouter(UsageTracker usageTracker) => _usageTracker = usageTracker;

    /// <summary>
    /// 尝试将用户输入解析为命令并异步执行。
    /// 依次匹配 PowerShell 命令、计算表达式和 Google 搜索。
    /// </summary>
    /// <param name="input">用户输入的原始文本</param>
    /// <returns>如果匹配到命令则返回对应的搜索结果，否则返回 null</returns>
    public async Task<SearchResult?> TryHandleCommandAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var psMatch = PowerShellRegex.Match(input);
        if (psMatch.Success) return await ExecutePowerShellAsync(psMatch.Groups[1].Value);
        var calcMatch = CalcRegex.Match(input);
        if (calcMatch.Success) return Calculate(calcMatch.Groups[1].Value);
        var gMatch = GoogleSearchRegex.Match(input);
        if (gMatch.Success) return await SearchInBrowserAsync(gMatch.Groups[1].Value);
        return null;
    }

    /// <summary>
    /// 异步执行 PowerShell 命令并返回执行结果。
    /// 使用无窗口模式启动 powershell.exe，捕获标准输出和错误输出。
    /// </summary>
    /// <param name="command">要执行的 PowerShell 命令字符串</param>
    /// <returns>包含命令执行结果的搜索结果对象</returns>
    private async Task<SearchResult> ExecutePowerShellAsync(string command)
    {
        var result = new SearchResult { Title = $"PowerShell: {command}", Type = SearchResultType.Command, Path = command };
        try
        {
            var psi = new ProcessStartInfo { FileName = "powershell.exe", Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                result.Data = new CommandResult { Success = process.ExitCode == 0, Output = output, Error = error };
                _usageTracker.RecordUsage($"cmd:{command}");
            }
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    /// <summary>
    /// 计算数学表达式并返回计算结果。
    /// 会先对表达式进行安全过滤，仅保留数字和基本运算符。
    /// </summary>
    /// <param name="expression">要计算的数学表达式字符串</param>
    /// <returns>包含计算结果的搜索结果对象</returns>
    private SearchResult Calculate(string expression)
    {
        var result = new SearchResult { Title = $"= {expression}", Type = SearchResultType.Calculator, Path = expression };
        try
        {
            string sanitized = Regex.Replace(expression, @"[^0-9+\-*/().%^]", "");
            var computed = new System.Data.DataTable().Compute(sanitized, null);
            result.Subtitle = computed.ToString() ?? "Error";
            result.Data = new CommandResult { Success = true, Output = computed.ToString() ?? "" };
            _usageTracker.RecordUsage($"calc:{expression}");
        }
        catch (Exception ex) { result.Subtitle = $"Error: {ex.Message}"; result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    /// <summary>
    /// 在默认浏览器中打开 Google 搜索页面。
    /// 使用 <see cref="Uri.EscapeDataString"/> 对关键字进行 URL 编码。
    /// </summary>
    /// <param name="keyword">搜索关键字</param>
    /// <returns>包含搜索操作结果的搜索结果对象</returns>
    private async Task<SearchResult> SearchInBrowserAsync(string keyword)
    {
        var result = new SearchResult { Title = $"Search: {keyword}", Subtitle = "Open in browser", Type = SearchResultType.WebSearch, Path = keyword };
        try
        {
            Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}", UseShellExecute = true });
            result.Data = new CommandResult { Success = true };
            _usageTracker.RecordUsage($"search:{keyword}");
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }

    /// <summary>
    /// 获取所有支持的命令建议列表，用于在搜索界面中显示可用命令提示。
    /// </summary>
    /// <returns>命令建议的搜索结果列表</returns>
    public static List<SearchResult> GetCommandSuggestions() => new()
    {
        new() { Title = "> command", Subtitle = "Execute PowerShell command", Type = SearchResultType.Command },
        new() { Title = "calc expression", Subtitle = "Calculate expression", Type = SearchResultType.Calculator },
        new() { Title = "g keyword", Subtitle = "Search in browser", Type = SearchResultType.WebSearch }
    };
}
