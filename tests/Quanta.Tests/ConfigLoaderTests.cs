// ============================================================================
// 文件名: ConfigLoaderTests.cs
// 文件描述: 配置加载器单元测试。
//           通过路径参数化的内部方法在临时目录中隔离文件系统，
//           覆盖原子写入、损坏文件备份、v1.2→v1.3 版本迁移。
// ============================================================================

using System.Text.Json;
using Quanta.Helpers;
using Quanta.Models;
using Xunit;

namespace Quanta.Tests;

/// <summary>
/// <see cref="ConfigLoader"/> 文件读写与迁移逻辑的单元测试。
/// 全部用例使用临时目录隔离文件系统，不触碰 %APPDATA% 中的真实配置。
/// </summary>
public class ConfigLoaderTests : IDisposable
{
    private readonly string _dir;

    public ConfigLoaderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "quanta-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch { /* 尽力清理临时目录 */ }
    }

    private string ConfigPath => Path.Combine(_dir, "config.json");

    /// <summary>持久化回调：把配置写回测试目录（隔离真实配置路径）。</summary>
    private void PersistToTestDir(AppConfig config) => ConfigLoader.SaveToPath(ConfigPath, config);

    // ── 原子写入 ──────────────────────────────────────────────────

    [Fact]
    public void SaveToPath_WritesAtomically_NoTempFileLeftBehind()
    {
        var config = new AppConfig { Version = "1.3", Theme = "Dark" };

        ConfigLoader.SaveToPath(ConfigPath, config);

        Assert.True(File.Exists(ConfigPath));
        // 临时文件必须已被 Move 消费，不残留
        Assert.False(File.Exists(ConfigPath + ".tmp"));

        var saved = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
        Assert.NotNull(saved);
        Assert.Equal("1.3", saved!.Version);
        Assert.Equal("Dark", saved.Theme);
    }

    [Fact]
    public void SaveToPath_OverwritesExistingFileCompletely()
    {
        File.WriteAllText(ConfigPath, "{\"Version\":\"1.0\",\"Theme\":\"Light\"}");

        ConfigLoader.SaveToPath(ConfigPath, new AppConfig { Version = "1.3", Theme = "Dark" });

        var saved = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
        Assert.NotNull(saved);
        Assert.Equal("1.3", saved!.Version);
        Assert.Equal("Dark", saved.Theme);
    }

    // ── 损坏文件备份 ──────────────────────────────────────────────

    [Fact]
    public void LoadFromFile_CorruptJson_IsBackedUpAndDefaultCreated()
    {
        const string corrupt = "{ this is not valid json";
        File.WriteAllText(ConfigPath, corrupt);

        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        // 原损坏内容被完整保留到 .bak 备份，绝不直接覆盖
        var bakFiles = Directory.GetFiles(_dir, "config.json.bak-*");
        var bak = Assert.Single(bakFiles);
        Assert.Equal(corrupt, File.ReadAllText(bak));

        // 返回默认配置，且默认配置已写回 config.json
        Assert.NotNull(config);
        Assert.NotEmpty(config.Commands); // 默认配置包含示例命令
        var onDisk = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
        Assert.NotNull(onDisk);
        Assert.Equal(config.Version, onDisk!.Version);
    }

    [Fact]
    public void LoadFromFile_NullContent_IsBackedUpAndDefaultCreated()
    {
        // 内容为 "null" 等反序列化为 null 的无效配置同样走备份流程
        File.WriteAllText(ConfigPath, "null");

        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        Assert.NotNull(config);
        Assert.Single(Directory.GetFiles(_dir, "config.json.bak-*"));
    }

    [Fact]
    public void BackupCorruptConfig_MovesFileToDatedBackup()
    {
        File.WriteAllText(ConfigPath, "corrupt");

        var bakPath = ConfigLoader.BackupCorruptConfig(ConfigPath, "test");

        Assert.NotNull(bakPath);
        Assert.StartsWith(ConfigPath + ".bak-", bakPath);
        Assert.True(File.Exists(bakPath!));
        Assert.False(File.Exists(ConfigPath));
    }

    [Fact]
    public void BackupCorruptConfig_MissingFile_ReturnsNull()
    {
        Assert.Null(ConfigLoader.BackupCorruptConfig(ConfigPath, "test"));
    }

    // ── v1.2 → v1.3 迁移 ─────────────────────────────────────────

    [Fact]
    public void LoadFromFile_MigratesV12ToV13()
    {
        File.WriteAllText(ConfigPath, "{\"Version\":\"1.2\",\"Theme\":\"Dark\"}");

        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        Assert.Equal("1.3", config.Version);
        Assert.Equal("Dark", config.Theme); // 迁移不覆盖用户设置
        // 迁移结果已持久化到磁盘
        var onDisk = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
        Assert.NotNull(onDisk);
        Assert.Equal("1.3", onDisk!.Version);
    }

    [Fact]
    public void LoadFromFile_CurrentVersion_DoesNotRemigrate()
    {
        File.WriteAllText(ConfigPath, "{\"Version\":\"1.3\",\"Theme\":\"Dark\"}");

        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        Assert.Equal("1.3", config.Version);
        Assert.Equal("Dark", config.Theme);
    }

    // ── 旧版本迁移链（v1.0 → v1.3，录音参数回归点）──────────────

    [Fact]
    public void LoadFromFile_MigratesLegacyRecordingDefaults()
    {
        // v1.0 旧默认：44100Hz / 128kbps / 立体声
        File.WriteAllText(ConfigPath,
            "{\"Version\":\"1.0\",\"RecordingSettings\":{\"SampleRate\":44100,\"Bitrate\":128,\"Channels\":2}}");

        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        Assert.Equal("1.3", config.Version);
        // v1.0 → v1.1：会议录音默认值（16kHz / 32kbps / 单声道）
        Assert.Equal(16000, config.RecordingSettings.SampleRate);
        Assert.Equal(32, config.RecordingSettings.Bitrate);
        Assert.Equal(1, config.RecordingSettings.Channels);
    }

    // ── 缺失文件 ──────────────────────────────────────────────────

    [Fact]
    public void LoadFromFile_MissingFile_CreatesAndPersistsDefault()
    {
        var config = ConfigLoader.LoadFromFile(ConfigPath, PersistToTestDir);

        Assert.NotNull(config);
        Assert.NotEmpty(config.Commands);
        Assert.True(File.Exists(ConfigPath));
        Assert.False(File.Exists(ConfigPath + ".tmp"));
    }
}
