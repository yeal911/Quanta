// ============================================================================
// 文件名: UnitConverter.cs
// 文件描述: 单位换算静态工具类
//           支持长度、重量、速度和温度的常见单位互转
// ============================================================================

using System.Collections.Generic;

namespace Quanta.Services;

/// <summary>
/// 单位转换工具类，支持长度、重量、速度和温度单位的相互转换。
/// </summary>
internal static class UnitConverter
{
    // ── 长度（基准单位：米）──────────────────────────────────
    private static readonly Dictionary<string, double> _length = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m"] = 1,
        ["meter"] = 1,
        ["meters"] = 1,
        ["米"] = 1,
        ["km"] = 1000,
        ["kilometer"] = 1000,
        ["kilometers"] = 1000,
        ["千米"] = 1000,
        ["公里"] = 1000,
        ["cm"] = 0.01,
        ["centimeter"] = 0.01,
        ["厘米"] = 0.01,
        ["mm"] = 0.001,
        ["millimeter"] = 0.001,
        ["毫米"] = 0.001,
        ["ft"] = 0.3048,
        ["foot"] = 0.3048,
        ["feet"] = 0.3048,
        ["英尺"] = 0.3048,
        ["in"] = 0.0254,
        ["inch"] = 0.0254,
        ["inches"] = 0.0254,
        ["英寸"] = 0.0254,
        ["mi"] = 1609.344,
        ["mile"] = 1609.344,
        ["miles"] = 1609.344,
        ["英里"] = 1609.344,
        ["yd"] = 0.9144,
        ["yard"] = 0.9144,
        ["yards"] = 0.9144,
        ["nm"] = 1852,
        ["nautical mile"] = 1852,
    };

    // ── 重量（基准单位：千克）────────────────────────────────
    private static readonly Dictionary<string, double> _weight = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kg"] = 1,
        ["kilogram"] = 1,
        ["kilograms"] = 1,
        ["千克"] = 1,
        ["公斤"] = 1,
        ["g"] = 0.001,
        ["gram"] = 0.001,
        ["grams"] = 0.001,
        ["克"] = 0.001,
        ["mg"] = 0.000001,
        ["milligram"] = 0.000001,
        ["毫克"] = 0.000001,
        ["t"] = 1000,
        ["tonne"] = 1000,
        ["ton"] = 1000,
        ["吨"] = 1000,
        ["lb"] = 0.453592,
        ["pound"] = 0.453592,
        ["pounds"] = 0.453592,
        ["磅"] = 0.453592,
        ["oz"] = 0.0283495,
        ["ounce"] = 0.0283495,
        ["ounces"] = 0.0283495,
        ["盎司"] = 0.0283495,
        ["jin"] = 0.5,
        ["斤"] = 0.5,
        ["liang"] = 0.05,
        ["两"] = 0.05,
    };

    // ── 速度（基准单位：米/秒）───────────────────────────────
    private static readonly Dictionary<string, double> _speed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["m/s"] = 1,
        ["ms"] = 1,
        ["米每秒"] = 1,
        ["km/h"] = 1.0 / 3.6,
        ["kph"] = 1.0 / 3.6,
        ["kmh"] = 1.0 / 3.6,
        ["公里每小时"] = 1.0 / 3.6,
        ["mph"] = 0.44704,
        ["英里每小时"] = 0.44704,
        ["knot"] = 0.514444,
        ["knots"] = 0.514444,
        ["节"] = 0.514444,
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
        var nTo = NormalizeTemp(to);
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
