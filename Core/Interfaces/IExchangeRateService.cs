using Quanta.Services;

namespace Quanta.Core.Interfaces;

/// <summary>
/// 汇率转换服务接口
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// 异步获取汇率并转换为目标货币
    /// </summary>
    Task<ExchangeRateResult> ConvertAsync(double amount, string fromCurrency, string toCurrency);

    /// <summary>
    /// 获取支持的货币代码列表
    /// </summary>
    Dictionary<string, string> SupportedCurrencies { get; }
}
