// ============================================================================
// 文件名: CommandRouterTests.cs
// 文件描述: 命令路由器单元测试。
//           通过公共入口 TryHandleCommandAsync 验证单位换算命令的
//           InvariantCulture 解析与格式化（逗号小数 locale 下 "1.5" 不被误读）。
//           UsageTracker 通过注入临时目录路径隔离文件系统，不在测试里真起进程。
// ============================================================================

using System.Globalization;
using Quanta.Domain.Commands;
using Quanta.Domain.Search;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// <see cref="CommandRouter"/> 单位换算路径的单元测试。
/// </summary>
public class CommandRouterTests : IDisposable
{
    private readonly string _dir;
    private readonly UsageTracker _tracker;

    public CommandRouterTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "quanta-tests-" + Guid.NewGuid().ToString("N"));
        // 注入临时目录路径，避免写入 %LOCALAPPDATA% 中的真实使用数据
        _tracker = new UsageTracker(Path.Combine(_dir, "usage.json"));
    }

    public void Dispose()
    {
        _tracker.Dispose();
        try { Directory.Delete(_dir, recursive: true); }
        catch { /* 尽力清理临时目录 */ }
    }

    [Fact]
    public async Task UnitConversion_ParsesDotDecimalUnderCommaDecimalLocale()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // P1 修复回归点：逗号小数 locale（如 de-DE）下，
            // "1.5" 必须按 InvariantCulture 解析为 1.5，而不是 15 或解析失败
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var router = new CommandRouter(_tracker);

            var result = await router.TryHandleCommandAsync("1.5 km to mi");

            Assert.NotNull(result);
            // 结果同样以 InvariantCulture 格式化（小数点恒为 "."）
            Assert.Equal("0.93 mi", result!.Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("1 nautical mile to km", "1.85 km")]   // 多词单位别名
    [InlineData("1 nmi to km", "1.85 km")]             // 海里缩写别名
    [InlineData("30 c to f", "86 f")]                  // 温度仿射换算
    public async Task UnitConversion_ProducesInvariantFormattedResult(string input, string expectedTitle)
    {
        var router = new CommandRouter(_tracker);

        var result = await router.TryHandleCommandAsync(input);

        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result!.Title);
    }

    [Fact]
    public async Task UnitConversion_UnknownUnit_ReturnsNull()
    {
        var router = new CommandRouter(_tracker);

        var result = await router.TryHandleCommandAsync("1 lightyear to km");

        Assert.Null(result);
    }
}
