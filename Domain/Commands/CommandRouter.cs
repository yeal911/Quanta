// ============================================================================
// 文件名: CommandRouter.cs
// 文件描述: 命令路由服务，负责解析用户输入并将其分发到对应的命令处理器。
//           支持 PowerShell 命令执行、数学表达式计算和浏览器搜索三种命令类型。
//           采用 ICommandHandler 插件架构，便于扩展新的命令类型。
// ============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

// ─────────────────────────────────────────────────────────────
// 命令处理器接口 - 插件架构基础
// ─────────────────────────────────────────────────────────────
/// <summary>
/// 命令处理器接口，所有命令处理器需实现此接口。
/// </summary>
public interface ICommandHandler
{
    /// <summary>处理器名称</summary>
    string Name { get; }

    /// <summary>
    /// 尝试处理输入，返回搜索结果；如果不匹配则返回 null。
    /// </summary>
    Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker);
}

// ═════════════════════════════════════════════════════════════════════════════
// 数学表达式解析器（递归下降），支持 +、-、*、/、%、^ 和括号
// 优先级（由低到高）：加减 < 乘除模 < 幂 < 一元符号 < 括号/数字
// ═════════════════════════════════════════════════════════════════════════════
internal static class MathParser
{
    public static double Evaluate(string expression)
    {
        string expr = expression.Replace(" ", "");
        int pos = 0;
        double result = ParseAddSub(expr, ref pos);
        if (pos != expr.Length)
            throw new FormatException($"Unexpected character '{expr[pos]}' at position {pos}");
        return result;
    }

    private static double ParseAddSub(string expr, ref int pos)
    {
        double result = ParseMulDiv(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '+' || expr[pos] == '-'))
        {
            char op = expr[pos++];
            double right = ParseMulDiv(expr, ref pos);
            result = op == '+' ? result + right : result - right;
        }
        return result;
    }

    private static double ParseMulDiv(string expr, ref int pos)
    {
        double result = ParsePow(expr, ref pos);
        while (pos < expr.Length && (expr[pos] == '*' || expr[pos] == '/' || expr[pos] == '%'))
        {
            char op = expr[pos++];
            double right = ParsePow(expr, ref pos);
            result = op == '*' ? result * right
                   : op == '/' ? result / right
                   : result % right;
        }
        return result;
    }

    private static double ParsePow(string expr, ref int pos)
    {
        double result = ParseUnary(expr, ref pos);
        if (pos < expr.Length && expr[pos] == '^')
        {
            pos++;
            double exp = ParsePow(expr, ref pos);
            result = Math.Pow(result, exp);
        }
        return result;
    }

    private static double ParseUnary(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '-') { pos++; return -ParseFactor(expr, ref pos); }
        if (pos < expr.Length && expr[pos] == '+') { pos++; }
        return ParseFactor(expr, ref pos);
    }

    private static double ParseFactor(string expr, ref int pos)
    {
        if (pos < expr.Length && expr[pos] == '(')
        {
            pos++;
            double val = ParseAddSub(expr, ref pos);
            if (pos < expr.Length && expr[pos] == ')') pos++;
            return val;
        }
        int start = pos;
        while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.')) pos++;
        if (pos == start) throw new FormatException($"Expected number at position {pos}");
        return double.Parse(expr.Substring(start, pos - start),
                            System.Globalization.CultureInfo.InvariantCulture);
    }
}

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
    /// 尝试进行单位换算，返回原始 double 值由调用方格式化。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="from">源单位</param>
    /// <param name="to">目标单位</param>
    /// <param name="result">换算后的原始数值</param>
    /// <returns>换算是否成功</returns>
    public static bool TryConvert(double value, string from, string to, out double result)
    {
        result = 0;

        // 温度特殊处理
        var tempResult = ConvertTemperature(value, from, to);
        if (tempResult.HasValue)
        {
            result = tempResult.Value;
            return true;
        }

        // 标准单位换算（长度、重量、速度）
        foreach (var table in new[] { _length, _weight, _speed })
        {
            if (table.TryGetValue(from, out double fromFactor) && table.TryGetValue(to, out double toFactor))
            {
                result = value * fromFactor / toFactor;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 判断单位是否为温度单位。温度转换是仿射变换，不存在线性基准换算率。
    /// </summary>
    internal static bool IsTemperature(string unit)
    {
        string norm = unit.ToLower().Trim('°', ' ');
        return norm is "c" or "celsius" or "摄氏" or "摄氏度"
                    or "f" or "fahrenheit" or "华氏" or "华氏度"
                    or "k" or "kelvin" or "开" or "开尔文";
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

    /// <summary>
    /// 将 double 格式化为简洁字符串（最多 2 位小数，超大/超小数用科学计数法）。
    /// </summary>
    internal static string FormatNumber(double n)
    {
        if (Math.Abs(n) >= 1e9 || (Math.Abs(n) < 0.005 && n != 0))
            return n.ToString("G4");
        return Math.Round(n, 2, MidpointRounding.AwayFromZero).ToString("0.##");
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
    /// 匹配货币换算的正则表达式，格式为: {数字} {货币代码} to/in {货币代码}
    /// 例如: 100 USD to CNY, 100 usd to cny
    /// </summary>
    private static readonly Regex CurrencyConvertRegex = new(
        @"^(-?\d+\.?\d*)\s*([A-Za-z]{3})\s+(?:to|in|转|换)\s+([A-Za-z]{3})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配颜色输入（HEX格式）
    /// </summary>
    private static readonly Regex ColorHexRegex = new(
        @"^#?([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配RGB格式
    /// </summary>
    private static readonly Regex ColorRgbRegex = new(
        @"^(?:rgb\s*\(\s*)?(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 匹配HSL格式
    /// </summary>
    private static readonly Regex ColorHslRegex = new(
        @"^(?:hsl\s*\(\s*)?(\d{1,3})\s*,\s*(\d{1,3})%?\s*,\s*(\d{1,3})%?\s*\)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── 文本工具 ──────────────────────────────────────────────
    private static readonly Regex Base64Regex      = new(@"^base64\s+(.+)$",  RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Base64DecodeRegex = new(@"^base64d\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Md5Regex         = new(@"^md5\s+(.+)$",     RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Sha256Regex      = new(@"^sha256\s+(.+)$",  RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex UrlToolRegex     = new(@"^url\s+(.+)$",     RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex JsonToolRegex    = new(@"^json\s+(.+)$",    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

        Logger.Debug($"Input: '{input}'");

        // PowerShell 命令（> command）
        var psMatch = PowerShellRegex.Match(input);
        Logger.Debug($"PowerShellRegex: {psMatch.Success}");
        if (psMatch.Success) return await ExecutePowerShellAsync(psMatch.Groups[1].Value);

        // 数学计算（calc expression）
        var calcMatch = CalcRegex.Match(input);
        Logger.Debug($"CalcRegex: {calcMatch.Success}, Groups[1]: '{calcMatch.Groups[1].Value}'");
        if (calcMatch.Success) return Calculate(calcMatch.Groups[1].Value);

        // 纯数学表达式（无 calc 前缀，例如 2+2）
        var pureMathMatch = PureMathRegex.Match(input);
        Logger.Debug($"PureMathRegex: {pureMathMatch.Success}");
        if (pureMathMatch.Success)
        {
            var trimmed = input.Trim();
            if (trimmed.Length > 0) return Calculate(trimmed);
        }

        // 货币换算（优先于单位换算，例如 100 USD to CNY）
        var currencyMatch = CurrencyConvertRegex.Match(input);
        Logger.Debug($"CurrencyConvertRegex: {currencyMatch.Success}");
        if (currencyMatch.Success)
        {
            return await ConvertCurrencyAsync(currencyMatch.Groups[1].Value, currencyMatch.Groups[2].Value, currencyMatch.Groups[3].Value);
        }

        // 颜色转换（例如 #E67E22, rgb(255,0,0), hsl(0,100%,50%)）
        // 尝试HEX格式
        var hexMatch = ColorHexRegex.Match(input);
        if (hexMatch.Success)
        {
            var colorResult = ConvertColor(hexMatch.Value);
            if (colorResult != null) return colorResult;
        }
        
        // 尝试RGB格式
        var rgbMatch = ColorRgbRegex.Match(input);
        if (rgbMatch.Success)
        {
            var colorResult = ConvertColor(rgbMatch.Value);
            if (colorResult != null) return colorResult;
        }
        
        // 尝试HSL格式
        var hslMatch = ColorHslRegex.Match(input);
        if (hslMatch.Success)
        {
            var colorResult = ConvertColor(hslMatch.Value);
            if (colorResult != null) return colorResult;
        }

        // 单位换算（优先于 Google 搜索，避免被 "g" 误匹配）
        var unitMatch = UnitConvertRegex.Match(input);
        Logger.Debug($"UnitConvertRegex: {unitMatch.Success}");
        if (unitMatch.Success)
        {
            var converted = ConvertUnit(unitMatch.Groups[1].Value, unitMatch.Groups[2].Value, unitMatch.Groups[3].Value);
            if (converted != null) return converted;
        }

        // 文本工具（base64 / base64d / md5 / sha256 / url / json）
        var base64Match = Base64Regex.Match(input);
        if (base64Match.Success) return TextBase64Encode(base64Match.Groups[1].Value);

        var base64dMatch = Base64DecodeRegex.Match(input);
        if (base64dMatch.Success) return TextBase64Decode(base64dMatch.Groups[1].Value);

        var md5Match = Md5Regex.Match(input);
        if (md5Match.Success) return TextHash(md5Match.Groups[1].Value, "MD5");

        var sha256Match = Sha256Regex.Match(input);
        if (sha256Match.Success) return TextHash(sha256Match.Groups[1].Value, "SHA256");

        var urlMatch = UrlToolRegex.Match(input);
        if (urlMatch.Success) return TextUrl(urlMatch.Groups[1].Value);

        var jsonMatch = JsonToolRegex.Match(input);
        if (jsonMatch.Success) return TextJson(jsonMatch.Groups[1].Value);

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
        var result = new SearchResult { Title = expression, Type = SearchResultType.Calculator, Path = expression };
        try
        {
            string sanitized = Regex.Replace(expression, @"[^0-9+\-*/().%^]", "");
            double computed = MathParser.Evaluate(sanitized);
            string computedStr = double.IsPositiveInfinity(computed) ? "∞"
                               : double.IsNegativeInfinity(computed) ? "-∞"
                               : double.IsNaN(computed) ? "NaN"
                               : (Math.Abs(computed) >= 1e9 || (Math.Abs(computed) < 0.005 && computed != 0))
                                 ? computed.ToString("G4")
                                 : Math.Round(computed, 2, MidpointRounding.AwayFromZero).ToString("0.##");
            // 结果作为主标题；表达式在搜索框已可见，副标题留空
            result.Title = computedStr;
            result.Data = new CommandResult { Success = true, Output = computedStr };
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
    /// 异步执行货币换算
    /// </summary>
    private async Task<SearchResult> ConvertCurrencyAsync(string amountStr, string fromCurrency, string toCurrency)
    {
        if (!double.TryParse(amountStr, out double amount))
        {
            return new SearchResult
            {
                Title = LocalizationService.Get("CalcError"),
                Subtitle = LocalizationService.Get("ExchangeRateInvalidAmount"),
                Type = SearchResultType.Calculator,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "💱"
            };
        }

        // 显示"正在获取汇率"提示（绿色字体）
        var fetchingResult = new SearchResult
        {
            Title = LocalizationService.Get("ExchangeRateFetching"),
            Subtitle = $"{amount} {fromCurrency.ToUpper()} → {toCurrency.ToUpper()}...",
            Type = SearchResultType.Calculator,
            GroupLabel = "",
            GroupOrder = 0,
            MatchScore = 1.0,
            IconText = "💱",
            QueryMatch = LocalizationService.Get("ExchangeRateFetching")
        };

        // 异步获取汇率
        var rateResult = await ExchangeRateService.Instance.ConvertAsync(amount, fromCurrency, toCurrency);

        if (rateResult.Success)
        {
            // 成功，显示转换结果
            // SubtitleSmall: 双向汇率（小字体）
            // Subtitle: 时间 + 缓存标记（正常字体）
            var subtitleSmall = rateResult.UnitRate;  // 1 CNY = 0.1371 USD · 1 USD = 7.2950 CNY
            
            var subtitle = "";
            if (!string.IsNullOrEmpty(rateResult.FetchTime))
            {
                subtitle = rateResult.FetchTime;
            }
            if (rateResult.IsFromCache)
            {
                var cacheLabel = LocalizationService.Get("ExchangeRateFromCache");
                subtitle += string.IsNullOrEmpty(subtitle) ? cacheLabel : $" · {cacheLabel}";
            }
            
            return new SearchResult
            {
                Title = rateResult.Result,
                Subtitle = subtitle,
                SubtitleSmall = subtitleSmall,
                Type = SearchResultType.Calculator,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "💱",
                QueryMatch = rateResult.Result
            };
        }
        else
        {
            // 错误信息（红色显示）
            return new SearchResult
            {
                Title = rateResult.Result,
                Subtitle = "",
                Type = SearchResultType.Calculator,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "💱",
                QueryMatch = ""
            };
        }
    }

    /// <summary>
    /// 颜色转换：HEX ↔ RGB ↔ HSL
    /// 支持输入格式: #RRGGBB, rgb(r,g,b), hsl(h,s%,l%)
    /// </summary>
    private SearchResult? ConvertColor(string colorInput)
    {
        try
        {
            int r, g, b;
            string input = colorInput.Trim();
            string inputLower = input.ToLower();
            
            // 解析输入颜色
            if (inputLower.StartsWith("rgb"))
            {
                // RGB格式: rgb(255, 0, 0) 或 255,0,0
                var match = System.Text.RegularExpressions.Regex.Match(input, @"rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    r = int.Parse(match.Groups[1].Value);
                    g = int.Parse(match.Groups[2].Value);
                    b = int.Parse(match.Groups[3].Value);
                }
                else
                {
                    return null;
                }
            }
            else if (inputLower.StartsWith("hsl"))
            {
                // HSL格式: hsl(0, 100%, 50%) 或 0,100%,50%
                var match = System.Text.RegularExpressions.Regex.Match(input, @"hsl\s*\(\s*(\d+)\s*,\s*(\d+)%?\s*,\s*(\d+)%?\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    double hVal = double.Parse(match.Groups[1].Value);
                    double sVal = double.Parse(match.Groups[2].Value) / 100.0;
                    double lVal = double.Parse(match.Groups[3].Value) / 100.0;
                    HslToRgb(hVal, sVal, lVal, out r, out g, out b);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // HEX格式: #RRGGBB, RRGGBB, #RGB, RGB
                string hex = input;
                if (!hex.StartsWith("#")) hex = "#" + hex;
                
                // 处理3位Hex
                if (hex.Length == 4)
                {
                    hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
                }
                
                if (hex.Length != 7) return null;
                
                r = Convert.ToInt32(hex.Substring(1, 2), 16);
                g = Convert.ToInt32(hex.Substring(3, 2), 16);
                b = Convert.ToInt32(hex.Substring(5, 2), 16);
            }
            
            // 验证RGB值
            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            {
                return null;
            }
            
            // RGB转HSL
            double h, s, l;
            RgbToHsl(r, g, b, out h, out s, out l);
            
            // 构建三种格式的输出
            string hexStr = $"#{r:X2}{g:X2}{b:X2}";
            string rgbStr = $"rgb({r}, {g}, {b})";
            string hslStr = $"hsl({h:F0}, {s:F0}%, {l:F0}%)";
            
            // 创建颜色预览
            var colorBitmap = CreateColorPreview((byte)r, (byte)g, (byte)b);
            
            // 主标题显示HEX，副标题显示完整转换结果
            return new SearchResult
            {
                Title = hexStr,
                Subtitle = $"{rgbStr} · {hslStr}",
                Type = SearchResultType.Calculator,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "🎨",
                QueryMatch = hexStr,
                ColorPreviewImage = colorBitmap,
                ColorInfo = new ColorInfo
                {
                    Hex = hexStr,
                    Rgb = rgbStr,
                    Hsl = hslStr
                }
            };
        }
        catch (Exception ex)
        {
            Logger.Warn($"[ColorConvert] Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// RGB转HSL
    /// </summary>
    private void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;
        
        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;
        
        l = (max + min) / 2;
        
        if (delta == 0)
        {
            h = 0;
            s = 0;
        }
        else
        {
            s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);
            
            if (max == rNorm)
                h = ((gNorm - bNorm) / delta + (gNorm < bNorm ? 6 : 0)) * 60;
            else if (max == gNorm)
                h = ((bNorm - rNorm) / delta + 2) * 60;
            else
                h = ((rNorm - gNorm) / delta + 4) * 60;
        }
    }

    /// <summary>
    /// HSL转RGB
    /// </summary>
    private void HslToRgb(double h, double s, double l, out int r, out int g, out int b)
    {
        if (s == 0)
        {
            r = g = b = (int)(l * 255);
            return;
        }
        
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;
        double hNorm = h / 360.0;
        
        r = (int)(HueToRgb(p, q, hNorm + 1.0 / 3) * 255);
        g = (int)(HueToRgb(p, q, hNorm) * 255);
        b = (int)(HueToRgb(p, q, hNorm - 1.0 / 3) * 255);
    }

    private double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    /// <summary>
    /// 创建颜色预览位图
    /// </summary>
    private System.Windows.Media.Imaging.BitmapImage CreateColorPreview(byte r, byte g, byte b)
    {
        int width = 40;
        int height = 40;
        
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        using (var stream = new System.IO.MemoryStream())
        {
            // 创建BMP图像
            var bmpData = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            byte[] pixels = new byte[width * height * 4];
            
            for (int i = 0; i < width * height; i++)
            {
                int offset = i * 4;
                pixels[offset] = b;     // Blue
                pixels[offset + 1] = g; // Green
                pixels[offset + 2] = r; // Red
                pixels[offset + 3] = 255; // Alpha
            }
            
            bmpData.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmpData));
            encoder.Save(stream);
            stream.Position = 0;
            
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
        }
        
        return bitmap;
    }

    /// <summary>
    /// 执行单位换算并返回结果。
    /// </summary>
    /// <param name="valueStr">数值字符串</param>
    /// <param name="fromUnit">源单位</param>
    /// <param name="toUnit">目标单位</param>
    /// <returns>换算结果的搜索结果对象；无法识别单位时返回 null</returns>
    /// <summary>
    /// 判断格式化后的字符串与原始值是否存在精度损失（用于选择 ≈ 或 =）。
    /// </summary>
    private static bool IsFormattedApprox(double original, string formatted)
        => double.TryParse(formatted, System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out double back)
           && Math.Abs(back - original) > 1e-10 * Math.Abs(original) + 1e-12;

    private SearchResult? ConvertUnit(string valueStr, string fromUnit, string toUnit)
    {
        if (!double.TryParse(valueStr, out double value))
            return null;
        if (!UnitConverter.TryConvert(value, fromUnit, toUnit, out double convertedValue))
            return null;

        // 实际换算结果
        string formatted = UnitConverter.FormatNumber(convertedValue);
        string symbol = IsFormattedApprox(convertedValue, formatted) ? "≈" : "=";
        string title = $"{formatted} {toUnit}";

        // 温度是仿射变换（非线性比例），不存在有意义的基准换算率
        string subtitle = string.Empty;
        if (!UnitConverter.IsTemperature(fromUnit) && !UnitConverter.IsTemperature(toUnit))
        {
            UnitConverter.TryConvert(1, fromUnit, toUnit, out double fwdRate);
            UnitConverter.TryConvert(1, toUnit, fromUnit, out double bwdRate);
            string fmtFwd = UnitConverter.FormatNumber(fwdRate);
            string fmtBwd = UnitConverter.FormatNumber(bwdRate);
            string symFwd = IsFormattedApprox(fwdRate, fmtFwd) ? "≈" : "=";
            string symBwd = IsFormattedApprox(bwdRate, fmtBwd) ? "≈" : "=";
            subtitle = $"1 {fromUnit} {symFwd} {fmtFwd} {toUnit}  |  1 {toUnit} {symBwd} {fmtBwd} {fromUnit}";
        }

        return new SearchResult
        {
            Title = title,
            Subtitle = subtitle,
            Type = SearchResultType.Calculator,
            Path = title,
            IconText = "📐",
            MatchScore = 2.0,
            Data = new CommandResult { Success = true, Output = title }
        };
    }

    // ═════════════════════════════════════════════════════════════
    // 文本工具处理器
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// 构建文本工具通用结果。
    /// label（短标签）放 Title（蓝字窄列），output（实际结果）放 Subtitle（宽列 Width=*），
    /// Data.Output 存放完整结果供剪贴板复制。
    /// </summary>
    private static SearchResult MakeTextResult(string output, string label, string icon) => new()
    {
        Title      = label,
        Subtitle   = output,
        Type       = SearchResultType.Calculator,
        IconText   = icon,
        GroupLabel = "Text",
        GroupOrder = 5,
        MatchScore = 2.0,
        Data       = new CommandResult { Success = true, Output = output }
    };

    /// <summary>Base64 编码（始终编码，无歧义）。</summary>
    private static SearchResult TextBase64Encode(string input)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        return MakeTextResult(encoded, "Base64 编码", "B");
    }

    /// <summary>
    /// Base64 解码（始终解码）。
    /// 使用严格 UTF-8 解码器：字节序列不合法时返回错误提示而非乱码。
    /// </summary>
    private static SearchResult TextBase64Decode(string input)
    {
        string trimmed = input.Trim();
        try
        {
            byte[] bytes = Convert.FromBase64String(trimmed);
            // throwOnInvalidBytes=true：遇到非法 UTF-8 字节序列直接抛异常，而非替换为乱码
            string decoded = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            return MakeTextResult(decoded, "Base64 解码", "B");
        }
        catch
        {
            return new SearchResult
            {
                Title      = "Base64 解码失败",
                Subtitle   = "输入不是有效的 Base64 或无法解码为 UTF-8 文本",
                Type       = SearchResultType.Calculator,
                IconText   = "B",
                GroupLabel = "Text",
                GroupOrder = 5,
                Data       = new CommandResult { Success = false }
            };
        }
    }

    /// <summary>计算字符串的 MD5 或 SHA256 哈希（小写十六进制）。</summary>
    private static SearchResult TextHash(string input, string algo)
    {
        byte[] hash = algo == "SHA256"
            ? SHA256.HashData(Encoding.UTF8.GetBytes(input))
            : MD5.HashData(Encoding.UTF8.GetBytes(input));

        string result = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return MakeTextResult(result, algo, "#");
    }

    /// <summary>
    /// URL 编码/解码。
    /// 自动检测：含 %XX 转义序列 → 解码；否则编码。
    /// </summary>
    private static SearchResult TextUrl(string input)
    {
        if (Regex.IsMatch(input, @"%[0-9A-Fa-f]{2}"))
        {
            string decoded = Uri.UnescapeDataString(input);
            return MakeTextResult(decoded, "URL 解码", "U");
        }
        string encoded = Uri.EscapeDataString(input);
        return MakeTextResult(encoded, "URL 编码", "U");
    }

    /// <summary>
    /// JSON 格式化（缩进美化）。格式错误时返回错误提示。
    /// Subtitle 显示单行压缩预览，Data.Output 存放完整格式化内容供剪贴板复制。
    /// </summary>
    private static SearchResult TextJson(string input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input.Trim());
            string formatted = JsonSerializer.Serialize(
                doc.RootElement,
                new JsonSerializerOptions { WriteIndented = true });

            int lines   = formatted.Split('\n').Length;
            string preview = formatted.ReplaceLineEndings(" ");
            if (preview.Length > 120) preview = preview[..120] + "…";

            return new SearchResult
            {
                Title      = $"JSON ({lines} 行)",
                Subtitle   = preview,
                Type       = SearchResultType.Calculator,
                IconText   = "{",
                GroupLabel = "Text",
                GroupOrder = 5,
                MatchScore = 2.0,
                Data       = new CommandResult { Success = true, Output = formatted }
            };
        }
        catch (JsonException ex)
        {
            return new SearchResult
            {
                Title      = "JSON 格式错误",
                Subtitle   = ex.Message,
                Type       = SearchResultType.Calculator,
                IconText   = "{",
                GroupLabel = "Text",
                GroupOrder = 5,
                Data       = new CommandResult { Success = false, Error = ex.Message }
            };
        }
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
