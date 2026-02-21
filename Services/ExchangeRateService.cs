// ============================================================================
// 文件名: ExchangeRateService.cs
// 文件用途: 汇率转换服务，调用 exchangerate-api.com 获取汇率，支持缓存和持久化
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 汇率转换结果
/// </summary>
public class ExchangeRateResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>转换结果（如 "¥736.50 CNY"）</summary>
    public string Result { get; set; } = string.Empty;
    
    /// <summary>1单位源货币对应的目标货币金额（如 "1 CNY = 0.1371 USD"）</summary>
    public string UnitRate { get; set; } = string.Empty;
    
    /// <summary>汇率数据获取时间</summary>
    public string FetchTime { get; set; } = string.Empty;
    
    /// <summary>是否使用了缓存数据</summary>
    public bool IsFromCache { get; set; }
}

/// <summary>
/// 汇率服务类，提供货币转换功能
/// </summary>
public class ExchangeRateService
{
    private static ExchangeRateService? _instance;
    public static ExchangeRateService Instance => _instance ??= new ExchangeRateService();

    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CacheEntry> _memoryCache = new();
    private readonly object _cacheLock = new();
    
    /// <summary>汇率数据文件路径</summary>
    private readonly string _ratesFilePath;

    private class CacheEntry
    {
        public Dictionary<string, double> Rates { get; set; } = new();
        public DateTime Expiry { get; set; }
        
        /// <summary>汇率数据的实际更新时间（来自API）</summary>
        public DateTime ApiUpdateTime { get; set; }
    }

    /// <summary>
    /// 文件缓存的汇率数据（支持多币种）
    /// </summary>
    private class FileCacheData
    {
        /// <summary>所有缓存的货币汇率，key为货币代码</summary>
        [JsonPropertyName("currencies")]
        public Dictionary<string, CurrencyCacheData> Currencies { get; set; } = new();
    }

    /// <summary>
    /// 单个货币的缓存数据
    /// </summary>
    private class CurrencyCacheData
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, double> Rates { get; set; } = new();
        
        /// <summary>汇率数据的实际更新时间（来自API）</summary>
        [JsonPropertyName("api_update_time")]
        public DateTime ApiUpdateTime { get; set; }
        
        [JsonPropertyName("expiry")]
        public DateTime Expiry { get; set; }
    }

    public ExchangeRateService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        
        // 汇率数据保存在exe目录下
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(Environment.ProcessPath))
        {
            exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? exeDir;
        }
        _ratesFilePath = Path.Combine(exeDir, "exchange_rates.json");
    }

    /// <summary>
    /// 获取支持的货币代码列表
    /// </summary>
    public static readonly Dictionary<string, string> SupportedCurrencies = new()
    {
        // 主要货币
        ["USD"] = "美元",
        ["CNY"] = "人民币",
        ["EUR"] = "欧元",
        ["GBP"] = "英镑",
        ["JPY"] = "日元",
        ["KRW"] = "韩元",
        ["HKD"] = "港币",
        ["TWD"] = "新台币",
        ["SGD"] = "新加坡元",
        ["AUD"] = "澳大利亚元",
        ["CAD"] = "加拿大元",
        ["CHF"] = "瑞士法郎",
        ["INR"] = "印度卢比",
        ["MXN"] = "墨西哥比索",
        ["BRL"] = "巴西雷亚尔",
        ["RUB"] = "俄罗斯卢布",
        ["ZAR"] = "南非兰特",
        ["SEK"] = "瑞典克朗",
        ["NOK"] = "挪威克朗",
        ["DKK"] = "丹麦克朗",
        ["NZD"] = "新西兰元",
        ["THB"] = "泰铢",
        ["MYR"] = "马来西亚林吉特",
        ["IDR"] = "印尼盾",
        ["PHP"] = "菲律宾比索",
        ["VND"] = "越南盾",
        ["AED"] = "阿联酋迪拉姆",
        ["SAR"] = "沙特里亚尔",
        ["TRY"] = "土耳其里拉",
        ["PLN"] = "波兰兹罗提",
        ["ILS"] = "以色列谢克尔",
        ["CZK"] = "捷克克朗",
        ["HUF"] = "匈牙利福林",
        ["CLP"] = "智利比索",
        ["COP"] = "哥伦比亚比索",
        ["PEN"] = "秘鲁索尔",
        ["ARS"] = "阿根廷比索",
        ["NGN"] = "尼日利亚奈拉",
        ["EGP"] = "埃及镑",
        ["PKR"] = "巴基斯坦卢比",
        ["BDT"] = "孟加拉塔卡",
        ["UAH"] = "乌克兰格里夫纳",
        ["RON"] = "罗马尼亚列伊",
        ["BGN"] = "保加利亚列弗",
        ["HRK"] = "克罗地亚库纳",
        ["ISK"] = "冰岛克朗"
    };

    /// <summary>
    /// 异步获取汇率并转换为目标货币
    /// </summary>
    public async Task<ExchangeRateResult> ConvertAsync(double amount, string fromCurrency, string toCurrency)
    {
        Logger.Debug($"[ExchangeRate] ConvertAsync called: {amount} {fromCurrency} -> {toCurrency}");
        
        var config = ConfigLoader.Load();
        var apiKey = config.ExchangeRateSettings?.ApiKey ?? "f02c1174e7cdfb412a48337c";
        var cacheMinutes = config.ExchangeRateSettings?.CacheMinutes ?? 60;

        Logger.Debug($"[ExchangeRate] API Key: {(string.IsNullOrWhiteSpace(apiKey) ? "EMPTY" : apiKey.Substring(0, 8) + "...")}");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Logger.Debug("[ExchangeRate] No API key configured");
            return new ExchangeRateResult
            {
                Success = false,
                Result = LocalizationService.Get("ExchangeRateNoApiKey")
            };
        }

        fromCurrency = NormalizeCurrencyCode(fromCurrency);
        toCurrency = NormalizeCurrencyCode(toCurrency);

        Logger.Debug($"[ExchangeRate] Normalized: {fromCurrency} -> {toCurrency}");

        // 优先使用缓存数据（1小时内）
        Dictionary<string, double>? rates = null;
        DateTime apiUpdateTime = DateTime.MinValue;
        bool isFromCache = false;
        
        var (cachedRates, cachedApiTime, cachedIsFromCache) = await GetRatesAsync(fromCurrency, apiKey, cacheMinutes);
        
        if (cachedRates != null)
        {
            rates = cachedRates;
            apiUpdateTime = cachedApiTime;
            isFromCache = cachedIsFromCache;
        }
        
        if (rates == null)
        {
            // 缓存没有或过期，尝试从文件缓存获取（1小时内）
            Logger.Warn("[ExchangeRate] Memory cache expired/missed, trying file cache...");
            var fileData = LoadFromFileCache(fromCurrency);
            if (fileData != null && fileData.Currencies.TryGetValue(fromCurrency.ToUpper(), out var currencyData))
            {
                rates = currencyData.Rates;
                apiUpdateTime = currencyData.ApiUpdateTime;
                isFromCache = true;
                Logger.Debug("[ExchangeRate] Using file cache data");
            }
        }

        if (rates == null)
        {
            // 文件缓存也没有或过期，调用API
            Logger.Warn("[ExchangeRate] All cache failed, calling API...");
            var (apiRates, apiTime) = await FetchFromApiAsync(fromCurrency, apiKey, cacheMinutes);
            
            if (apiRates != null)
            {
                rates = apiRates;
                apiUpdateTime = apiTime;
                isFromCache = false;
            }
        }

        if (rates == null)
        {
            Logger.Debug("[ExchangeRate] All sources failed, no rate data available");
            return new ExchangeRateResult
            {
                Success = false,
                Result = LocalizationService.Get("ExchangeRateApiError")
            };
        }

        if (!rates.TryGetValue(toCurrency.ToUpper(), out double rate))
        {
            Logger.Debug($"[ExchangeRate] Currency {toCurrency} not supported");
            return new ExchangeRateResult
            {
                Success = false,
                Result = string.Format(LocalizationService.Get("ExchangeRateNotSupported"), toCurrency)
            };
        }

        // 获取反向汇率 (to -> from)
        double reverseRate = rate > 0 ? 1.0 / rate : 0;

        double result = amount * rate;
        Logger.Debug($"[ExchangeRate] Conversion: {amount} {fromCurrency} * {rate} = {result} {toCurrency}");
        
        string formattedResult = FormatCurrency(result, toCurrency);
        string unitRate = FormatUnitRate(1, fromCurrency, rate, toCurrency, reverseRate);
        string fetchTimeStr = FormatFetchTime(apiUpdateTime);
        
        Logger.Debug($"[ExchangeRate] Result: {formattedResult}, Unit: {unitRate}, Time: {fetchTimeStr}");
        
        return new ExchangeRateResult
        {
            Success = true,
            Result = formattedResult,
            UnitRate = unitRate,
            FetchTime = fetchTimeStr,
            IsFromCache = isFromCache
        };
    }

    /// <summary>
    /// 异步获取汇率（仅检查内存缓存，不调用API）
    /// </summary>
    private Task<(Dictionary<string, double>? Rates, DateTime FetchTime, bool IsFromCache)> GetRatesAsync(string baseCurrency, string apiKey, int cacheMinutes)
    {
        var cacheKey = baseCurrency.ToUpper();
        var now = DateTime.Now;

        // 检查内存缓存（任意时间内的都返回，不过期判断交给上层）
        lock (_cacheLock)
        {
            if (_memoryCache.TryGetValue(cacheKey, out var entry))
            {
                // 如果在缓存有效期内
                if (entry.Expiry > now)
                {
                    Logger.Debug($"[ExchangeRate] Memory cache HIT (valid) for {cacheKey}, expires at {entry.Expiry}");
                    return Task.FromResult<(Dictionary<string, double>?, DateTime, bool)>((entry.Rates, entry.ApiUpdateTime, true));
                }
                else
                {
                    // 过期但还有数据，返回过期标记
                    Logger.Debug($"[ExchangeRate] Memory cache HIT (expired) for {cacheKey}, expired at {entry.Expiry}");
                    return Task.FromResult<(Dictionary<string, double>?, DateTime, bool)>((entry.Rates, entry.ApiUpdateTime, true));
                }
            }
        }
        
        Logger.Debug($"[ExchangeRate] Memory cache MISS for {cacheKey}");
        return Task.FromResult<(Dictionary<string, double>?, DateTime, bool)>((null, now, false));
    }

    /// <summary>
    /// 直接从API获取汇率（不检查缓存）
    /// </summary>
    private async Task<(Dictionary<string, double>? Rates, DateTime ApiUpdateTime)> FetchFromApiAsync(string baseCurrency, string apiKey, int cacheMinutes)
    {
        var cacheKey = baseCurrency.ToUpper();
        var now = DateTime.Now;
        
        Logger.Debug($"[ExchangeRate] Fetching from API for {cacheKey}...");

        try
        {
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/{cacheKey}";
            Logger.Debug($"[ExchangeRate] Request URL: {url}");
            
            var response = await _httpClient.GetStringAsync(url);
            Logger.Debug($"[ExchangeRate] Response received, length: {response?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(response))
            {
                Logger.Warn("[ExchangeRate] API returned empty response");
                return (null, now);
            }
            
            Logger.Debug($"[ExchangeRate] Response preview: {response.Substring(0, Math.Min(200, response.Length))}...");
            
            var apiResult = JsonSerializer.Deserialize<ExchangeRateApiResponse>(response);

            if (apiResult == null)
            {
                Logger.Warn("[ExchangeRate] Failed to deserialize API response");
                return (null, now);
            }
            
            Logger.Debug($"[ExchangeRate] API Result: {apiResult.Result}, BaseCode: {apiResult.BaseCode}");

            if (apiResult?.Result != "success" || apiResult.ConversionRates == null)
            {
                Logger.Warn($"[ExchangeRate] API returned error result: {apiResult?.Result}");
                return (null, now);
            }

            // 从API响应获取汇率更新时间
            DateTime apiUpdateTime;
            if (apiResult.TimeLastUpdateUnix > 0)
            {
                apiUpdateTime = DateTimeOffset.FromUnixTimeSeconds(apiResult.TimeLastUpdateUnix).LocalDateTime;
            }
            else
            {
                apiUpdateTime = now;
            }
            
            // 存入内存缓存
            var rates = apiResult.ConversionRates;
            lock (_cacheLock)
            {
                _memoryCache[cacheKey] = new CacheEntry
                {
                    Rates = rates,
                    Expiry = now.AddMinutes(cacheMinutes),
                    ApiUpdateTime = apiUpdateTime
                };
            }
            
            Logger.Debug($"[ExchangeRate] Cached {rates.Count} rates for {cacheKey}, API update time: {apiUpdateTime}");

            // 保存到文件缓存
            SaveToFileCache(cacheKey, rates, apiUpdateTime, now.AddMinutes(cacheMinutes));

            return (rates, apiUpdateTime);
        }
        catch (HttpRequestException hex)
        {
            Logger.Error($"[ExchangeRate] HTTP请求失败: {hex.Message}", hex);
            return (null, now);
        }
        catch (TaskCanceledException tex)
        {
            Logger.Error($"[ExchangeRate] 请求超时: {tex.Message}", tex);
            return (null, now);
        }
        catch (Exception ex)
        {
            Logger.Error($"[ExchangeRate] API异常: {ex.Message}", ex);
            return (null, now);
        }
    }

    /// <summary>
    /// 保存汇率数据到文件（支持多币种）
    /// </summary>
    private void SaveToFileCache(string baseCode, Dictionary<string, double> rates, DateTime apiUpdateTime, DateTime expiry)
    {
        try
        {
            FileCacheData cacheData;
            
            // 读取现有缓存
            if (File.Exists(_ratesFilePath))
            {
                try
                {
                    var existingJson = File.ReadAllText(_ratesFilePath);
                    cacheData = JsonSerializer.Deserialize<FileCacheData>(existingJson) ?? new FileCacheData();
                }
                catch
                {
                    cacheData = new FileCacheData();
                }
            }
            else
            {
                cacheData = new FileCacheData();
            }
            
            // 更新或添加该币种的数据
            cacheData.Currencies[baseCode] = new CurrencyCacheData
            {
                Rates = rates,
                ApiUpdateTime = apiUpdateTime,
                Expiry = expiry
            };
            
            var newJson = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_ratesFilePath, newJson);
            Logger.Debug($"[ExchangeRate] Saved {baseCode} rates to file, total currencies: {cacheData.Currencies.Count}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[ExchangeRate] Failed to save rates to file: {ex.Message}");
        }
    }

    /// <summary>
    /// 从文件加载汇率数据（1小时内可用）
    /// </summary>
    private FileCacheData? LoadFromFileCache(string baseCode)
    {
        const int cacheThresholdMinutes = 60; // 1小时内认为是有效的
        
        try
        {
            if (!File.Exists(_ratesFilePath))
            {
                Logger.Debug("[ExchangeRate] File cache not found");
                return null;
            }

            var json = File.ReadAllText(_ratesFilePath);
            var cacheData = JsonSerializer.Deserialize<FileCacheData>(json);
            
            if (cacheData == null || cacheData.Currencies.Count == 0)
            {
                Logger.Debug("[ExchangeRate] File cache is empty or invalid");
                return null;
            }

            // 检查请求的货币代码是否存在
            var upperCode = baseCode.ToUpper();
            if (!cacheData.Currencies.TryGetValue(upperCode, out var currencyData))
            {
                Logger.Debug($"[ExchangeRate] File cache does not contain {upperCode}");
                return null;
            }

            // 检查是否在1小时内
            var age = DateTime.Now - currencyData.ApiUpdateTime;
            if (age.TotalMinutes > cacheThresholdMinutes)
            {
                Logger.Debug($"[ExchangeRate] File cache for {upperCode} too old: {age.TotalMinutes:F1} minutes");
                return null;
            }

            Logger.Debug($"[ExchangeRate] File cache HIT for {upperCode}: {currencyData.Rates.Count} rates, age: {age.TotalMinutes:F1} minutes");
            
            // 构建只包含请求币种的数据返回
            var result = new FileCacheData
            {
                Currencies = new Dictionary<string, CurrencyCacheData>
                {
                    [upperCode] = currencyData
                }
            };
            return result;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[ExchangeRate] Failed to load file cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 标准化货币代码
    /// </summary>
    private string NormalizeCurrencyCode(string code)
    {
        code = code.Trim().ToUpper();
        
        // 处理中文货币名称
        foreach (var kvp in SupportedCurrencies)
        {
            if (kvp.Value.Contains(code) || code.Contains(kvp.Value))
            {
                return kvp.Key;
            }
        }
        
        return code;
    }

    /// <summary>
    /// 格式化货币显示
    /// </summary>
    private string FormatCurrency(double amount, string currencyCode)
    {
        string symbol = currencyCode.ToUpper() switch
        {
            "CNY" => "¥",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "KRW" => "₩",
            _ => ""
        };

        string formatted = amount >= 1000 ? amount.ToString("#,##0.00") : amount.ToString("0.00");
        return $"{symbol}{formatted}";
    }

    /// <summary>
    /// 格式化单位汇率显示（双向）
    /// </summary>
    private string FormatUnitRate(double amount, string fromCurrency, double rate, string toCurrency, double reverseRate)
    {
        string rateStr = rate.ToString("0.0000");
        string reverseRateStr = reverseRate.ToString("0.0000");
        
        // 双向展示: 1 CNY = 0.1371 USD · 1 USD = 7.2950 CNY
        return $"1 {fromCurrency} = {rateStr} {toCurrency} · 1 {toCurrency} = {reverseRateStr} {fromCurrency}";
    }

    /// <summary>
    /// 格式化获取时间显示（使用国际化）
    /// </summary>
    private string FormatFetchTime(DateTime apiUpdateTime)
    {
        if (apiUpdateTime == DateTime.MinValue)
        {
            return "";
        }
        
        // 使用国际化：今天是"今天 HH:mm"，昨天是"昨天 HH:mm"，其他是"MM-dd HH:mm"
        var now = DateTime.Now;
        if (apiUpdateTime.Date == now.Date)
        {
            return $"{LocalizationService.Get("ExchangeRateToday")} {apiUpdateTime:HH:mm}";
        }
        else if (apiUpdateTime.Date == now.Date.AddDays(-1))
        {
            return $"{LocalizationService.Get("ExchangeRateYesterday")} {apiUpdateTime:HH:mm}";
        }
        else
        {
            return apiUpdateTime.ToString("MM-dd HH:mm");
        }
    }
}

/// <summary>
/// ExchangeRate-API 响应结构
/// </summary>
internal class ExchangeRateApiResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }
    
    [JsonPropertyName("base_code")]
    public string? BaseCode { get; set; }
    
    [JsonPropertyName("conversion_rates")]
    public Dictionary<string, double>? ConversionRates { get; set; }
    
    /// <summary>汇率数据更新时间（Unix时间戳）</summary>
    [JsonPropertyName("time_last_update_unix")]
    public long TimeLastUpdateUnix { get; set; }
    
    /// <summary>汇率数据更新时间（UTC字符串）</summary>
    [JsonPropertyName("time_last_update_utc")]
    public string? TimeLastUpdateUtc { get; set; }
}
