// ============================================================================
// 文件名: LoggerServiceTests.cs
// 文件用途: 日志保留策略（CleanupExpiredLogs）的单元测试：
//          过期文件被删除、未过期与非匹配文件保留、保留期<=0 不清理。
// ============================================================================

using System;
using System.IO;
using Quanta.Infrastructure.Logging;
using Xunit;

namespace Quanta.Tests;

public class LoggerServiceTests
{
    /// <summary>测试用日志目录（LoggerService.Default 实际使用的 logs 目录）</summary>
    private readonly string _logDir;

    public LoggerServiceTests()
    {
        _logDir = LoggerService.Default.LogDirectory;
        Directory.CreateDirectory(_logDir);
    }

    [Fact]
    public void CleanupExpiredLogs_DeletesFilesOlderThanRetention()
    {
        var oldFile = CreateLogFile("quanta_20200101.log", lastWrite: DateTime.Now.AddMonths(-8));

        var deleted = LoggerService.Default.CleanupExpiredLogs(6);

        Assert.Equal(1, deleted);
        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public void CleanupExpiredLogs_KeepsFilesWithinRetention()
    {
        var recentFile = CreateLogFile("quanta_recent.log", lastWrite: DateTime.Now.AddMonths(-1));

        var deleted = LoggerService.Default.CleanupExpiredLogs(6);

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(recentFile));
        File.Delete(recentFile);
    }

    [Fact]
    public void CleanupExpiredLogs_DoesNothingWhenRetentionDisabled()
    {
        var oldFile = CreateLogFile("quanta_20200102.log", lastWrite: DateTime.Now.AddMonths(-8));

        var deleted = LoggerService.Default.CleanupExpiredLogs(0);

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(oldFile));
        File.Delete(oldFile);
    }

    [Fact]
    public void CleanupExpiredLogs_NeverDeletesNonMatchingFiles()
    {
        var oldOther = CreateLogFile("other_20200101.log", lastWrite: DateTime.Now.AddMonths(-8));
        var oldPrefix = CreateLogFile("quanta_backup.txt", lastWrite: DateTime.Now.AddMonths(-8));

        var deleted = LoggerService.Default.CleanupExpiredLogs(6);

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(oldOther));
        Assert.True(File.Exists(oldPrefix));
        File.Delete(oldOther);
        File.Delete(oldPrefix);
    }

    /// <summary>在日志目录内创建指定最后写入时间的测试文件，并清理可能残留的同名旧文件</summary>
    private string CreateLogFile(string name, DateTime lastWrite)
    {
        var path = Path.Combine(_logDir, name);
        File.WriteAllText(path, "test");
        File.SetLastWriteTime(path, lastWrite);
        return path;
    }
}
