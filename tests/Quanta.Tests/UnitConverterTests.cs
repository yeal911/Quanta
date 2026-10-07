// ============================================================================
// 文件名: UnitConverterTests.cs
// 文件描述: 单位换算工具单元测试。
//           覆盖常见单位互转、nm/nmi 别名（纳米 vs 海里）、温度仿射换算、
//           InvariantCulture 解析/格式化（逗号小数 locale 下小数点恒为 "."）。
// ============================================================================

using System.Globalization;
using Quanta.Services;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// <see cref="UnitConverter"/> 的单元测试。
/// </summary>
public class UnitConverterTests
{
    [Theory]
    [InlineData(1, "nmi", "km", 1.852)]              // 海里别名（P1 修复回归点）
    [InlineData(1, "nautical mile", "km", 1.852)]
    [InlineData(1, "nm", "m", 0.000000001)]          // nm = 纳米，不再误作海里
    [InlineData(1, "nanometer", "m", 0.000000001)]
    [InlineData(1, "km", "m", 1000)]
    [InlineData(100, "cm", "m", 1)]
    [InlineData(1, "mi", "km", 1.609344)]
    [InlineData(1, "kg", "g", 1000)]
    [InlineData(1, "lb", "kg", 0.453592)]
    [InlineData(1, "斤", "kg", 0.5)]                 // 中文别名
    [InlineData(1, "m/s", "km/h", 3.6)]
    [InlineData(1, "knot", "m/s", 0.514444)]
    [InlineData(30, "c", "f", 86)]                   // 温度仿射换算
    [InlineData(0, "c", "k", 273.15)]
    [InlineData(212, "f", "c", 100)]
    [InlineData(0, "k", "c", -273.15)]
    [InlineData(25, "celsius", "c", 25)]             // 同单位恒等
    public void TryConvert_ConvertsKnownUnits(double value, string from, string to, double expected)
    {
        Assert.True(UnitConverter.TryConvert(value, from, to, out var result));
        Assert.Equal(expected, result, precision: 6);
    }

    [Theory]
    [InlineData("KM", "M", 1000)]    // 大小写不敏感：KM 与 m 均可解析
    [InlineData("km", "KM", 1)]      // 同单位不同大小写 → 恒等换算
    public void TryConvert_IsCaseInsensitive(string from, string to, double expected)
    {
        Assert.True(UnitConverter.TryConvert(1, from, to, out var result));
        Assert.Equal(expected, result, precision: 6);
    }

    [Theory]
    [InlineData(1, "lightyear", "m")]   // 未知单位
    [InlineData(1, "km", "kg")]         // 跨类别（长度 → 重量）
    [InlineData(1, "km", "c")]          // 长度 → 温度（仿射变换不可比例换算）
    public void TryConvert_ReturnsFalse_ForUnknownOrCrossCategoryUnits(double value, string from, string to)
    {
        Assert.False(UnitConverter.TryConvert(value, from, to, out _));
    }

    [Theory]
    [InlineData("c", true)]
    [InlineData("C", true)]
    [InlineData("°C", true)]
    [InlineData("celsius", true)]
    [InlineData("f", true)]
    [InlineData("k", true)]
    [InlineData("开尔文", true)]
    [InlineData("km", false)]
    [InlineData("kg", false)]
    public void IsTemperature_DetectsTemperatureAliases(string unit, bool expected)
    {
        Assert.Equal(expected, UnitConverter.IsTemperature(unit));
    }

    [Fact]
    public void FormatNumber_UsesInvariantCulture_EvenUnderCommaDecimalLocale()
    {
        // P1 修复回归点：逗号小数 locale（如 de-DE）下，格式化输出的小数点仍恒为 "."
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("1.5", UnitConverter.FormatNumber(1.5));
            Assert.Equal("62.14", UnitConverter.FormatNumber(62.1371192237));
            Assert.Equal("-2.5", UnitConverter.FormatNumber(-2.5));
            Assert.Equal("0.001", UnitConverter.FormatNumber(0.001));   // 极小值科学计数法
            Assert.Equal("1E+10", UnitConverter.FormatNumber(1e10));    // 极大值科学计数法
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
