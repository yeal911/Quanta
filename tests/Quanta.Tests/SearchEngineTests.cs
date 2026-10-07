// ============================================================================
// 文件名: SearchEngineTests.cs
// 文件描述: 搜索引擎单元测试。
//           覆盖重复关键字不崩溃（冲突安全索引）、分组排序规则、内置命令生成
//           （文案通过 i18n 资源 key 取值断言）。
// ============================================================================

using Quanta.Models;
using Quanta.Services;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// <see cref="SearchEngine"/> 纯逻辑部分的单元测试（不依赖配置文件、进程或 UI）。
/// </summary>
public class SearchEngineTests
{
    // ── 分组排序权重 ──────────────────────────────────────────────

    [Fact]
    public void GetGroupOrder_ReturnsKnownGroupWeights()
    {
        Assert.Equal(0, SearchEngine.GetGroupOrder("GroupCalc"));
        Assert.Equal(0, SearchEngine.GetGroupOrder("GroupQRCode"));
        Assert.Equal(1, SearchEngine.GetGroupOrder("GroupCommand"));
        Assert.Equal(2, SearchEngine.GetGroupOrder("GroupApp"));
        Assert.Equal(3, SearchEngine.GetGroupOrder("GroupSystem"));
        Assert.Equal(4, SearchEngine.GetGroupOrder("GroupNetwork"));
        Assert.Equal(5, SearchEngine.GetGroupOrder("GroupPower"));
        Assert.Equal(6, SearchEngine.GetGroupOrder("GroupFeature"));
        Assert.Equal(7, SearchEngine.GetGroupOrder("GroupQuanta"));
        Assert.Equal(8, SearchEngine.GetGroupOrder("GroupFile"));
        Assert.Equal(9, SearchEngine.GetGroupOrder("GroupWindow"));
        // 未知分组排在最后
        Assert.Equal(10, SearchEngine.GetGroupOrder("GroupUnknown"));
    }

    // ── 重复关键字不崩溃（P0 修复回归点）─────────────────────────

    [Fact]
    public void BuildCommandIndex_DuplicateKeywords_DoNotThrow_AndFirstCommandWins()
    {
        // 用户自定义命令与内置命令关键字冲突（含大小写差异）时，
        // 索引构造必须行为确定且不抛 ArgumentException
        var commands = new List<CommandConfig>
        {
            new() { Keyword = "cmd", Path = "custom.exe" },     // 用户自定义命令（优先）
            new() { Keyword = "cmd", Path = "cmd.exe" },        // 内置命令同关键字
            new() { Keyword = "CMD", Path = "another.exe" },    // 仅大小写不同
            new() { Keyword = "notepad", Path = "notepad.exe" },
        };

        var index = SearchEngine.BuildCommandIndex(commands);

        // 大小写不敏感去重：cmd + notepad 共 2 个键
        Assert.Equal(2, index.Count);
        // 先出现的自定义命令优先
        Assert.Equal("custom.exe", index["cmd:cmd"].Path);
        Assert.Equal("notepad.exe", index["cmd:notepad"].Path);
    }

    [Fact]
    public void BuildCommandIndex_EmptyList_ReturnsEmptyIndex()
    {
        Assert.Empty(SearchEngine.BuildCommandIndex(Array.Empty<CommandConfig>()));
    }

    // ── 分组排序 ──────────────────────────────────────────────────

    [Fact]
    public void OrderResults_SortsByScoreDescThenGroupOrderAsc()
    {
        var results = new List<SearchResult>
        {
            new() { Id = "file",   MatchScore = 0.8, GroupOrder = 8 },
            new() { Id = "app",    MatchScore = 0.8, GroupOrder = 2 },
            new() { Id = "calc",   MatchScore = 1.0, GroupOrder = 0 },
            new() { Id = "custom", MatchScore = 0.5, GroupOrder = 1 },
        };

        var ordered = SearchEngine.OrderResults(results, 10);

        // 分数降序优先；同分按分组权重升序（Calculator/QRCode 置顶，文件靠后）
        Assert.Equal(new[] { "calc", "app", "file", "custom" }, ordered.Select(r => r.Id));
    }

    [Fact]
    public void OrderResults_SameScoreAndGroup_UsageCountBreaksTie()
    {
        var results = new List<SearchResult>
        {
            new() { Id = "rare",  MatchScore = 0.9, GroupOrder = 1 },
            new() { Id = "often", MatchScore = 0.9, GroupOrder = 1 },
        };
        var usage = new Dictionary<string, int> { ["rare"] = 1, ["often"] = 7 };

        var ordered = SearchEngine.OrderResults(results, 10, id => usage.GetValueOrDefault(id));

        // 同分同组时，使用次数多者优先
        Assert.Equal("often", ordered[0].Id);
        Assert.Equal("rare", ordered[1].Id);
    }

    [Fact]
    public void OrderResults_LimitsOutputToMaxResults()
    {
        var results = Enumerable.Range(0, 5)
            .Select(i => new SearchResult { Id = $"r{i}", MatchScore = 1.0 - i * 0.1 });

        var ordered = SearchEngine.OrderResults(results, 3);

        Assert.Equal(3, ordered.Count);
        Assert.Equal(new[] { "r0", "r1", "r2" }, ordered.Select(r => r.Id));
    }

    // ── 内置命令生成 ──────────────────────────────────────────────

    [Fact]
    public void GetBuiltInCommands_GeneratesLocalizedCommandsFromResourceKeys()
    {
        var commands = SearchEngine.GetBuiltInCommands();

        Assert.NotEmpty(commands);

        // 关键字唯一且非空
        Assert.True(commands.All(c => !string.IsNullOrEmpty(c.Keyword)));
        Assert.Equal(
            commands.Count,
            commands.Select(c => c.Keyword).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var cmd in commands)
        {
            Assert.True(cmd.IsBuiltIn);
            Assert.False(string.IsNullOrEmpty(cmd.GroupKey));
            // 分组 key 必须是已识别的分组（未知分组权重为 10）
            Assert.True(SearchEngine.GetGroupOrder(cmd.GroupKey) < 10,
                $"Unknown group key: {cmd.GroupKey}");

            // 用户可见文案必须来自 i18n 资源 key（BuiltinCmd_/BuiltinDesc_），禁止硬编码
            Assert.Equal(TestResources.GetZhCn($"BuiltinCmd_{cmd.Keyword}"), cmd.Name);
            Assert.Equal(TestResources.GetZhCn($"BuiltinDesc_{cmd.Keyword}"), cmd.Description);
        }
    }

    [Fact]
    public void GetBuiltInCommands_IncludesCoreSystemCommands()
    {
        var keywords = SearchEngine.GetBuiltInCommands()
            .Select(c => c.Keyword)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("cmd", keywords);
        Assert.Contains("calc", keywords);
        Assert.Contains("setting", keywords);
        Assert.Contains("record", keywords);
    }
}
