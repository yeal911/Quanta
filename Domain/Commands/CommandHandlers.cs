// ============================================================================
// 文件名：CommandHandlers.cs
// 文件用途：ICommandHandler 的各类实现，供 CommandRouter.TryHandleCommandAsync 路由调用。
//          包含：PowerShell、计算器、Google搜索、单位换算、货币换算、颜色转换、文本工具。
// ============================================================================

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

// ═════════════════════════════════════════════════════════════════════════════
// 命令处理器实现 - 插件化架构
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>PowerShell 命令处理器</summary>
public class PowerShellHandler : ICommandHandler
{
    private static readonly Regex Pattern = new(@"^>\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Name => "PowerShell";
    public int Priority => 40;

    public async Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var match = Pattern.Match(input);
        if (!match.Success) return null;

        var command = match.Groups[1].Value;
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
                usageTracker.RecordUsage($"cmd:{command}");
            }
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }
}

/// <summary>计算器（数学表达式）处理器</summary>
public class CalcHandler : ICommandHandler
{
    private static readonly Regex CalcPattern = new(@"^calc\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PureMathPattern = new(@"^[A-Za-z_\d\s\+\-\*\/%\^\(\)\.,]+$", RegexOptions.Compiled);
    public string Name => "Calculator";
    public int Priority => 50;

    public Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        SearchResult? result = null;

        var calcMatch = CalcPattern.Match(input);
        if (calcMatch.Success)
        {
            result = Calculate(calcMatch.Groups[1].Value);
        }
        else
        {
            var pureMatch = PureMathPattern.Match(input);
            if (pureMatch.Success && input.Trim().Length > 0 && LooksLikeMathExpression(input.Trim()))
            {
                result = Calculate(input.Trim());
            }
        }

        if (result != null)
        {
            usageTracker.RecordUsage($"calc:{input}");
        }

        return Task.FromResult(result);
    }

    private static bool LooksLikeMathExpression(string input)
    {
        if (string.Equals(input, "pi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input, "e", StringComparison.OrdinalIgnoreCase))
            return true;

        if (input.IndexOfAny(new[] { '+', '-', '*', '/', '%', '^' }) >= 0)
            return true;

        if (Regex.IsMatch(input, @"^(?:abs|sign|floor|ceil|round|min|max|sqrt|log10?|sin|cos|tan|asin|acos|atan|rad|deg)\s*\(", RegexOptions.IgnoreCase))
            return true;

        // 纯数字/小数/科学计数法，避免影响单位、颜色、汇率等其它输入类型
        return Regex.IsMatch(input, @"^[\d\s\.eE+\-]+$") && Regex.IsMatch(input, @"\d");
    }

    private static SearchResult Calculate(string expression)
    {
        var result = new SearchResult { Title = expression, Type = SearchResultType.Calculator, Path = expression };
        try
        {
            string sanitized = expression;
            double computed = MathParser.Evaluate(sanitized);
            string computedStr = double.IsPositiveInfinity(computed) ? "∞"
                               : double.IsNegativeInfinity(computed) ? "-∞"
                               : double.IsNaN(computed) ? "NaN"
                               : (Math.Abs(computed) >= 1e9 || (Math.Abs(computed) < 0.005 && computed != 0))
                                 ? computed.ToString("G4")
                                 : Math.Round(computed, 2, MidpointRounding.AwayFromZero).ToString("0.##");
            result.Title = computedStr;
            result.Data = new CommandResult { Success = true, Output = computedStr };
        }
        catch (Exception ex)
        {
            result.Subtitle = $"Error: {ex.Message}";
            result.Data = new CommandResult { Success = false, Error = ex.Message };
        }
        return result;
    }
}

/// <summary>Google 搜索处理器</summary>
public class GoogleSearchHandler : ICommandHandler
{
    private static readonly Regex Pattern = new(@"^g\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Name => "GoogleSearch";
    public int Priority => 80;

    public async Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var match = Pattern.Match(input);
        if (!match.Success) return null;

        var keyword = match.Groups[1].Value;
        var result = new SearchResult { Title = $"Search: {keyword}", Subtitle = "Open in browser", Type = SearchResultType.WebSearch, Path = keyword };
        try
        {
            Process.Start(new ProcessStartInfo { FileName = $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}", UseShellExecute = true });
            result.Data = new CommandResult { Success = true };
            usageTracker.RecordUsage($"search:{keyword}");
        }
        catch (Exception ex) { result.Data = new CommandResult { Success = false, Error = ex.Message }; }
        return result;
    }
}

/// <summary>单位换算处理器</summary>
public class UnitConvertHandler : ICommandHandler
{
    private static readonly Regex Pattern = new(
        @"^(-?\d+\.?\d*)\s*([a-zA-Z°/]+|[\u4e00-\u9fff]+)\s+(?:to|in|转|换)\s+([a-zA-Z°/]+|[\u4e00-\u9fff]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Name => "UnitConvert";
    public int Priority => 30;

    public Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var match = Pattern.Match(input);
        if (!match.Success) return Task.FromResult<SearchResult?>(null);

        var valueStr = match.Groups[1].Value;
        var fromUnit = match.Groups[2].Value;
        var toUnit = match.Groups[3].Value;

        if (!double.TryParse(valueStr, out double value))
            return Task.FromResult<SearchResult?>(null);
        if (!UnitConverter.TryConvert(value, fromUnit, toUnit, out double convertedValue))
            return Task.FromResult<SearchResult?>(null);

        string formatted = UnitConverter.FormatNumber(convertedValue);
        string symbol = IsFormattedApprox(convertedValue, formatted) ? "≈" : "=";
        string title = $"{formatted} {toUnit}";

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

        return Task.FromResult<SearchResult?>(new SearchResult
        {
            Title = title,
            Subtitle = subtitle,
            Type = SearchResultType.Calculator,
            Path = title,
            IconText = "📐",
            MatchScore = 2.0,
            Data = new CommandResult { Success = true, Output = title }
        });
    }

    private static bool IsFormattedApprox(double original, string formatted)
        => double.TryParse(formatted, System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out double back)
           && Math.Abs(back - original) > 1e-10 * Math.Abs(original) + 1e-12;
}

/// <summary>货币换算处理器</summary>
public class CurrencyConvertHandler : ICommandHandler
{
    private static readonly Regex Pattern = new(
        @"^(-?\d+\.?\d*)\s*([A-Za-z]{3})\s+(?:to|in|转|换)\s+([A-Za-z]{3})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Name => "CurrencyConvert";
    public int Priority => 10;

    public async Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var match = Pattern.Match(input);
        if (!match.Success) return null;

        var amountStr = match.Groups[1].Value;
        var fromCurrency = match.Groups[2].Value;
        var toCurrency = match.Groups[3].Value;

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

        var rateResult = await ExchangeRateService.Instance.ConvertAsync(amount, fromCurrency, toCurrency);

        if (rateResult.Success)
        {
            // Subtitle：单位汇率 + 时间戳（小字）
            var subtitle = rateResult.UnitRate;
            if (!string.IsNullOrEmpty(rateResult.FetchTime))
                subtitle += $"  ·  {rateResult.FetchTime}";
            if (rateResult.IsFromCache)
                subtitle += $"  {LocalizationService.Get("ExchangeRateFromCache")}";

            return new SearchResult
            {
                Title = $"{amount} {fromCurrency.ToUpper()} → {toCurrency.ToUpper()}",
                SubtitleSmall = rateResult.Result,   // 结果数字（大字显示）
                Subtitle = subtitle,                  // 单位汇率 + 时间（小字）
                Type = SearchResultType.Calculator,
                GroupLabel = "",
                GroupOrder = 0,
                MatchScore = 1.0,
                IconText = "💱",
                QueryMatch = rateResult.Result       // 点击复制结果数字
            };
        }
        else
        {
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
}

/// <summary>颜色转换处理器</summary>
public class ColorConvertHandler : ICommandHandler
{
    private static readonly Regex HexPattern = new(@"^#?([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RgbPattern = new(@"^(?:rgb\s*\(\s*)?(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HslPattern = new(@"^(?:hsl\s*\(\s*)?(\d{1,3})\s*,\s*(\d{1,3})%?\s*,\s*(\d{1,3})%?\s*\)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string Name => "ColorConvert";
    public int Priority => 20;

    public Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var hexMatch = HexPattern.Match(input);
        if (hexMatch.Success)
        {
            var result = ConvertColor(hexMatch.Value);
            if (result != null) return Task.FromResult<SearchResult?>(result);
        }

        var rgbMatch = RgbPattern.Match(input);
        if (rgbMatch.Success)
        {
            var result = ConvertColor(rgbMatch.Value);
            if (result != null) return Task.FromResult<SearchResult?>(result);
        }

        var hslMatch = HslPattern.Match(input);
        if (hslMatch.Success)
        {
            var result = ConvertColor(hslMatch.Value);
            if (result != null) return Task.FromResult<SearchResult?>(result);
        }

        return Task.FromResult<SearchResult?>(null);
    }

    private SearchResult? ConvertColor(string colorInput)
    {
        try
        {
            int r, g, b;
            string input = colorInput.Trim();
            string inputLower = input.ToLower();

            if (inputLower.StartsWith("rgb"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, @"rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    r = int.Parse(match.Groups[1].Value);
                    g = int.Parse(match.Groups[2].Value);
                    b = int.Parse(match.Groups[3].Value);
                }
                else return null;
            }
            else if (inputLower.StartsWith("hsl"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, @"hsl\s*\(\s*(\d+)\s*,\s*(\d+)%?\s*,\s*(\d+)%?\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    double hVal = double.Parse(match.Groups[1].Value);
                    double sVal = double.Parse(match.Groups[2].Value) / 100.0;
                    double lVal = double.Parse(match.Groups[3].Value) / 100.0;
                    HslToRgb(hVal, sVal, lVal, out r, out g, out b);
                }
                else return null;
            }
            else
            {
                string hex = input;
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (hex.Length == 4)
                {
                    hex = $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
                }
                if (hex.Length != 7) return null;

                r = Convert.ToInt32(hex.Substring(1, 2), 16);
                g = Convert.ToInt32(hex.Substring(3, 2), 16);
                b = Convert.ToInt32(hex.Substring(5, 2), 16);
            }

            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
                return null;

            RgbToHsl(r, g, b, out double h, out double s, out double l);

            string hexStr = $"#{r:X2}{g:X2}{b:X2}";
            string rgbStr = $"rgb({r}, {g}, {b})";
            string hslStr = $"hsl({h:F0}, {s:F0}%, {l:F0}%)";

            var colorBitmap = CreateColorPreview((byte)r, (byte)g, (byte)b);

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
                ColorInfo = new ColorInfo { Hex = hexStr, Rgb = rgbStr, Hsl = hslStr }
            };
        }
        catch (Exception ex)
        {
            Logger.Warn($"[ColorConvert] Error: {ex.Message}");
            return null;
        }
    }

    private void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
    {
        double rNorm = r / 255.0, gNorm = g / 255.0, bNorm = b / 255.0;
        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        l = (max + min) / 2;
        if (delta == 0) { h = 0; s = 0; }
        else
        {
            s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);
            if (max == rNorm) h = ((gNorm - bNorm) / delta + (gNorm < bNorm ? 6 : 0)) * 60;
            else if (max == gNorm) h = ((bNorm - rNorm) / delta + 2) * 60;
            else h = ((rNorm - gNorm) / delta + 4) * 60;
        }
    }

    private void HslToRgb(double h, double s, double l, out int r, out int g, out int b)
    {
        if (s == 0) { r = g = b = (int)(l * 255); return; }
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

    private System.Windows.Media.Imaging.BitmapImage CreateColorPreview(byte r, byte g, byte b)
    {
        int width = 40, height = 40;
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        using var stream = new System.IO.MemoryStream();
        var bmpData = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        byte[] pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            int offset = i * 4;
            pixels[offset] = b; pixels[offset + 1] = g; pixels[offset + 2] = r; pixels[offset + 3] = 255;
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
        return bitmap;
    }
}

/// <summary>文本工具处理器（Base64、MD5、SHA256、URL、JSON）</summary>
public class TextToolHandler : ICommandHandler
{
    private static readonly Regex Base64Regex = new(@"^base64\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Base64DecodeRegex = new(@"^base64d\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Md5Regex = new(@"^md5\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex Sha256Regex = new(@"^sha256\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex UrlToolRegex = new(@"^url\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex JsonToolRegex = new(@"^json\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    public string Name => "TextTool";
    public int Priority => 70;

    public Task<SearchResult?> HandleAsync(string input, UsageTracker usageTracker)
    {
        var base64Match = Base64Regex.Match(input);
        if (base64Match.Success) return Task.FromResult<SearchResult?>(TextBase64Encode(base64Match.Groups[1].Value));

        var base64dMatch = Base64DecodeRegex.Match(input);
        if (base64dMatch.Success) return Task.FromResult<SearchResult?>(TextBase64Decode(base64dMatch.Groups[1].Value));

        var md5Match = Md5Regex.Match(input);
        if (md5Match.Success) return Task.FromResult<SearchResult?>(TextHash(md5Match.Groups[1].Value, "MD5"));

        var sha256Match = Sha256Regex.Match(input);
        if (sha256Match.Success) return Task.FromResult<SearchResult?>(TextHash(sha256Match.Groups[1].Value, "SHA256"));

        var urlMatch = UrlToolRegex.Match(input);
        if (urlMatch.Success) return Task.FromResult<SearchResult?>(TextUrl(urlMatch.Groups[1].Value));

        var jsonMatch = JsonToolRegex.Match(input);
        if (jsonMatch.Success) return Task.FromResult<SearchResult?>(TextJson(jsonMatch.Groups[1].Value));

        return Task.FromResult<SearchResult?>(null);
    }

    private static SearchResult MakeTextResult(string output, string label, string icon) => new()
    {
        Title = label,
        Subtitle = output,
        Type = SearchResultType.Calculator,
        IconText = icon,
        GroupLabel = LocalizationService.Get("GroupText"),
        GroupOrder = 6,
        MatchScore = 2.0,
        Data = new CommandResult { Success = true, Output = output }
    };

    private static SearchResult TextBase64Encode(string input)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        return MakeTextResult(encoded, LocalizationService.Get("TextBase64EncodeResult"), "B");
    }

    private static SearchResult TextBase64Decode(string input)
    {
        string trimmed = input.Trim();
        try
        {
            byte[] bytes = Convert.FromBase64String(trimmed);
            string decoded = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            return MakeTextResult(decoded, LocalizationService.Get("TextBase64DecodeResult"), "B");
        }
        catch
        {
            return new SearchResult
            {
                Title = LocalizationService.Get("TextBase64DecodeFail"),
                Subtitle = LocalizationService.Get("TextBase64DecodeFailHint"),
                Type = SearchResultType.Calculator,
                IconText = "B",
                GroupLabel = LocalizationService.Get("GroupText"),
                GroupOrder = 6,
                Data = new CommandResult { Success = false }
            };
        }
    }

    private static SearchResult TextHash(string input, string algo)
    {
        byte[] hash = algo == "SHA256"
            ? SHA256.HashData(Encoding.UTF8.GetBytes(input))
            : MD5.HashData(Encoding.UTF8.GetBytes(input));
        string result = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return MakeTextResult(result, algo, "#");
    }

    private static SearchResult TextUrl(string input)
    {
        if (Regex.IsMatch(input, @"%[0-9A-Fa-f]{2}"))
        {
            string decoded = Uri.UnescapeDataString(input);
            return MakeTextResult(decoded, LocalizationService.Get("TextUrlDecodeResult"), "U");
        }
        string encoded = Uri.EscapeDataString(input);
        return MakeTextResult(encoded, LocalizationService.Get("TextUrlEncodeResult"), "U");
    }

    private static SearchResult TextJson(string input)
    {
        try
        {
            using var doc = JsonDocument.Parse(input.Trim());
            string formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            int lines = formatted.Split('\n').Length;
            string preview = formatted.ReplaceLineEndings(" ");
            if (preview.Length > 120) preview = preview[..120] + "…";
            return new SearchResult
            {
                Title = LocalizationService.Get("TextJsonResult", lines),
                Subtitle = preview,
                Type = SearchResultType.Calculator,
                IconText = "{",
                GroupLabel = LocalizationService.Get("GroupText"),
                GroupOrder = 6,
                MatchScore = 2.0,
                Data = new CommandResult { Success = true, Output = formatted }
            };
        }
        catch (JsonException ex)
        {
            return new SearchResult
            {
                Title = LocalizationService.Get("TextJsonError"),
                Subtitle = ex.Message,
                Type = SearchResultType.Calculator,
                IconText = "{",
                GroupLabel = LocalizationService.Get("GroupText"),
                GroupOrder = 6,
                Data = new CommandResult { Success = false, Error = ex.Message }
            };
        }
    }
}
