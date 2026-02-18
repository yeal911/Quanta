// ============================================================================
// 文件名: CommandRouter.cs
// 文件描述: 命令路由服务，负责解析用户输入并将其分发到对应的命令处理器。
//           支持 PowerShell 命令执行、数学表达式计算和浏览器搜索三种命令类型。
// ============================================================================

using System.Diagnostics;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

// ─────────────────────────────────────────────────────────────
// 单位换算静态工具类
// 支持长度、重量、速度和温度的常见单位互转
// ─────────────────────────────────────────────────────────────
internal static class UnitConverter
{
    // ── 长度（基准单位：米）──────────────────────────────────
    private static readonly Dictionary<string, double> _length = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m"] = 1, ["meter"] = 1, ["meters"] = 1, ["米"] = 1,
        ["km"] = 1000, ["kilometer"] = 1000, ["kilometers"] = 1000, ["千米"] = 1000, ["公里"] = 1000,
        ["cm"] = 0.01, ["centimeter"] = 0.01, ["厘米"] = 0.01,
        ["mm"] = 0.001, ["millimeter"] = 0.001, ["毫米"] = 0.001,
        ["ft"] = 0.3048, ["foot"] = 0.3048, ["feet"] = 0.3048, ["英尺"] = 0.3048,
        ["in"] = 0.0254, ["inch"] = 0.0254, ["inches"] = 0.0254, ["英寸"] = 0.0254,
        ["mi"] = 1609.344, ["mile"] = 1609.344, ["miles"] = 1609.344, ["英里"] = 1609.344,
        ["yd"] = 0.9144, ["yard"] = 0.9144, ["yards"] = 0.9144,
        ["nm"] = 1852, ["nautical mile"] = 1852,
    };

    // ── 重量（基准单位：千克）────────────────────────────────
    private static readonly Dictionary<string, double> _weight = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kg"] = 1, ["kilogram"] = 1, ["kilograms"] = 1, ["千克"] = 1, ["公斤"] = 1,
        ["g"] = 0.001, ["gram"] = 0.001, ["grams"] = 0.001, ["克"] = 0.001,
        ["mg"] = 0.000001, ["milligram"] = 0.000001, ["毫克"] = 0.000001,
        ["t"] = 1000, ["tonne"] = 1000, ["ton"] = 1000, ["吨"] = 1000,
        ["lb"] = 0.453592, ["pound"] = 0.453592, ["pounds"] = 0.453592, ["磅"] = 0.453592,
        ["oz"] = 0.0283495, ["ounce"] = 0.0283495, ["ounces"] = 0.0283495, ["盎司"] = 0.0283495,
        ["jin"] = 0.5, ["斤"] = 0.5,
        ["liang"] = 0.05, ["两"] = 0.05,
    };

    // ── 速度（基准单位：米/秒）───────────────────────────────
    private static readonly Dictionary<string, double> _speed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m/s"] = 1, ["ms"] = 1, ["米每秒"] = 1,
        ["km/h"] = 1.0 / 3.6, ["kph"] = 1.0 / 3.6, ["kmh"] = 1.0 / 3.6, ["公里每小时"] = 1.0 / 3.6,
        ["mph"] = 0.44704, ["英里每小时"] = 0.44704,
        ["knot"] = 0.514444, ["knots"] = 0.514444, ["节"] = 0.514444,
    };

    /// <summary>
    /// 尝试进行单位换算。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="from">源单位</param>
    /// <param name="to">目标单位</param>
    /// <param name="result">换算结果（带单位的字符串）</param>
    /// <returns>换算是否成功</returns>
    public static bool TryConvert(double value, string from, string to, out string result)
    {
        result = "";

        // 温度特殊处理
        var tempResult = ConvertTemperature(value, from, to);
        if (tempResult.HasValue)
        {
            result = $"{value} {from} = {FormatNumber(tempResult.Value)} {to}";
            return true;
        }

        // 标准单位换算（长度、重量、速度）
        foreach (var table in new[] { _length, _weight, _speed })
        {
            if (table.TryGetValue(from, out double fromFactor) && table.TryGetValue(to, out double toFactor))
            {
                double converted = value * fromFactor / toFactor;
                result = $"{value} {from} = {FormatNumber(converted)} {to}";
                return true;
            }
        }
        return false;
    }

    private static double? ConvertTemperature(double value, string from, string to)
    {
        // 归一化温度单位别名
        string NormalizeTemp(string s) => s.ToLower().Trim(new[] { '°', ' ' }) switch
        {
            "c" or "celsius" or "摄氏" or "摄氏度" => "c",
            "f" or "fahrenheit" or "华氏" or "华氏度" => "f",
            "k" or "kelvin" or "开" or "开尔文" => "k",
            _ => s.ToLower()
        };

        var nFrom = NormalizeTemp(from);
        var nTo   = NormalizeTemp(to);
        if (!new[] { "c", "f", "k" }.Contains(nFrom) || !new[] { "c", "f", "k" }.Contains(nTo))
            return null;
        if (nFrom == nTo) return value;

        // 先转为摄氏度
        double celsius = nFrom switch
        {
            "c" => value,
            "f" => (value - 32) * 5.0 / 9,
            "k" => value - 273.15,
            _ => double.NaN
        };
        if (double.IsNaN(celsius)) return null;

        // 从摄氏度转为目标单位
        return nTo switch
        {
            "c" => celsius,
            "f" => celsius * 9.0 / 5 + 32,
            "k" => celsius + 273.15,
            _ => double.NaN
        };
    }

    private static string FormatNumber(double n)
    {
        if (Math.Abs(n) >= 1e9 || (Math.Abs(n) < 0.0001 && n != 0))
            return n.ToString("G6");
        // 最多显示 6 位有效数字，去掉末尾零
        return n.ToString("G6").TrimEnd('0').TrimEnd('.');
    }
}

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
    /// 匹配纯数学表达式的正则表达式（无 calc 前缀）。
    /// 例如: 2+2, 100*5, 2^3, 10%3 等
    /// </summary>
    private static readonly Regex PureMathRegex = new(@"^[\d\s\+\-\*\/%\^\(\)\.]+$", RegexOptions.Compiled);

    /// <summary>
    /// 匹配 Google 搜索的正则表达式，格式为: g 关键字
    /// </summary>
    private static readonly Regex GoogleSearchRegex = new(@"^g\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配单位换算的正则表达式，格式为: {数字} {源单位} to/in {目标单位}
    /// 例如: 100 km to mile, 30 c to f
    /// </summary>
    private static readonly Regex UnitConvertRegex = new(
        @"^(-?\d+\.?\d*)\s*([a-zA-Z°/]+|[\u4e00-\u9fff]+)\s+(?:to|in|转|换)\s+([a-zA-Z°/]+|[\u4e00-\u9fff]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        DebugLog.Log("Input: '{0}'", input);

        // PowerShell 命令（> command）
        var psMatch = PowerShellRegex.Match(input);
        DebugLog.Log("PowerShellRegex: {0}", psMatch.Success);
        if (psMatch.Success) return await ExecutePowerShellAsync(psMatch.Groups[1].Value);

        // 数学计算（calc expression）
        var calcMatch = CalcRegex.Match(input);
        DebugLog.Log("CalcRegex: {0}, Groups[1]: '{1}'", calcMatch.Success, calcMatch.Groups[1].Value);
        if (calcMatch.Success) return Calculate(calcMatch.Groups[1].Value);

        // 纯数学表达式（无 calc 前缀，例如 2+2）
        var pureMathMatch = PureMathRegex.Match(input);
        DebugLog.Log("PureMathRegex: {0}", pureMathMatch.Success);
        if (pureMathMatch.Success)
        {
            var trimmed = input.Trim();
            if (trimmed.Length > 0) return Calculate(trimmed);
        }

        // 单位换算（优先于 Google 搜索，避免被 "g" 误匹配）
        var unitMatch = UnitConvertRegex.Match(input);
        DebugLog.Log("UnitConvertRegex: {0}", unitMatch.Success);
        if (unitMatch.Success)
        {
            var converted = ConvertUnit(unitMatch.Groups[1].Value, unitMatch.Groups[2].Value, unitMatch.Groups[3].Value);
            if (converted != null) return converted;
        }

        // Google 搜索（g keyword）
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
        catch (Exception ex) { 
            result.Subtitle = $"Error: {ex.Message}"; 
            result.Data = new CommandResult { Success = false, Error = ex.Message }; 
        }
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
    /// 执行单位换算并返回结果。
    /// </summary>
    /// <param name="valueStr">数值字符串</param>
    /// <param name="fromUnit">源单位</param>
    /// <param name="toUnit">目标单位</param>
    /// <returns>换算结果的搜索结果对象；无法识别单位时返回 null</returns>
    private SearchResult? ConvertUnit(string valueStr, string fromUnit, string toUnit)
    {
        if (!double.TryParse(valueStr, out double value)) {
            return null;
        }
        if (!UnitConverter.TryConvert(value, fromUnit, toUnit, out string converted)) {
            return null;
        }

        return new SearchResult
        {
            Title = $"= {converted}",
            Subtitle = $"{valueStr} {fromUnit} → {toUnit}",
            Type = SearchResultType.Calculator,
            Path = converted,
            IconText = "📐",
            MatchScore = 2.0,
            Data = new CommandResult { Success = true, Output = converted }
        };
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
